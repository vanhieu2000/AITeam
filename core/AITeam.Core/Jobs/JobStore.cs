using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AITeam.Core.Jobs;

/// <summary>Trạng thái 1 task (thiết kế: docs/design/V4-MULTI-AI.md mục B4).</summary>
public enum JobState
{
    Created, Planned, Ready, Running, Verifying, Reviewing, Approval, Merged, Done,
    NeedsFix, Paused, Cancelled, Failed, Interrupted, Blocked,
}

/// <summary>Phân loại lỗi để không retry mù: hạ tầng ≠ provider ≠ build ≠ code ≠ thiết kế ≠ nghiệp vụ.</summary>
public enum ErrorKind { None, Infrastructure, Provider, Build, Code, Design, Business }

public sealed record JobRecord(
    string Project,
    string Task,
    JobState State,
    int Attempt,
    string? ProviderId,
    int? Pid,
    string Machine,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ErrorKind LastErrorKind,
    string? LastError);

public sealed record JobEvent(DateTimeOffset At, JobState From, JobState To, string Machine, string Detail, ErrorKind ErrorKind);

/// <summary>Chuyển trạng thái không hợp lệ (ví dụ Running → Merged khi chưa verify / review).</summary>
public sealed class InvalidTransitionException(JobState from, JobState to)
    : InvalidOperationException($"Không được chuyển {from} → {to}")
{
    public JobState From { get; } = from;
    public JobState To { get; } = to;
}

/// <summary>Bảng chuyển trạng thái hợp lệ — QUYẾT ĐỊNH THIẾT KẾ của Claude, agent không đổi.</summary>
public static class JobTransitions
{
    private static readonly JobState[] Stoppable = [JobState.Paused, JobState.Cancelled, JobState.Failed, JobState.Interrupted, JobState.Blocked];

    public static readonly IReadOnlyDictionary<JobState, JobState[]> Allowed = new Dictionary<JobState, JobState[]>
    {
        [JobState.Created] = [JobState.Planned, JobState.Ready, JobState.Cancelled],
        [JobState.Planned] = [JobState.Ready, JobState.Cancelled],
        [JobState.Ready] = [JobState.Running, JobState.Paused, JobState.Cancelled],
        [JobState.Running] = [JobState.Verifying, .. Stoppable],
        [JobState.Verifying] = [JobState.Reviewing, JobState.NeedsFix, JobState.Approval, .. Stoppable], // Approval: dự án tắt review tự động
        [JobState.Reviewing] = [JobState.Approval, JobState.NeedsFix, .. Stoppable],
        [JobState.NeedsFix] = [JobState.Running, JobState.Cancelled, JobState.Blocked],
        [JobState.Approval] = [JobState.Merged, JobState.NeedsFix, JobState.Reviewing, JobState.Cancelled], // Reviewing: diff đổi sau review
        [JobState.Merged] = [JobState.Done],
        [JobState.Paused] = [JobState.Ready, JobState.Cancelled],
        [JobState.Interrupted] = [JobState.Ready, JobState.Running, JobState.Cancelled, JobState.Failed],
        [JobState.Blocked] = [JobState.Ready, JobState.Reviewing, JobState.Cancelled, JobState.Failed],
        [JobState.Failed] = [JobState.Ready, JobState.Cancelled],
        [JobState.Done] = [],
        [JobState.Cancelled] = [],
    };

    public static bool IsAllowed(JobState from, JobState to) => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>Trạng thái có tiến trình agent / reviewer đang chạy (PID chết ⇒ Interrupted khi phục hồi).</summary>
    public static bool IsActive(JobState s) => s is JobState.Running or JobState.Verifying or JobState.Reviewing;
}

/// <summary>
/// Lưu trạng thái job CỤC BỘ từng máy (tiến trình, PID, nhịp sống). Nguồn sự thật dùng chung giữa các máy vẫn là
/// file task + bảng việc trên git. Thiết kế: docs/design/V4-MULTI-AI.md mục B4.
/// </summary>
public sealed class JobStore(string root, string machine, Func<int, bool> isProcessAlive, Func<DateTimeOffset>? clock = null)
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.Now;

    /// <summary>Tạo job ở trạng thái Created (Attempt = 0). Đã có ⇒ trả bản đang có, không ghi đè.</summary>
    public JobRecord Create(string project, string task)
    {
        if (Get(project, task) is { } existing) return existing;
        var now = Now;
        var record = new JobRecord(project, task, JobState.Created, 0, null, null, machine, now, now, ErrorKind.None, null);
        WriteRecordAtomic(project, task, record);
        return record;
    }

    public JobRecord? Get(string project, string task)
    {
        var path = RecordPath(project, task);
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<JobRecord>(File.ReadAllText(path, Utf8NoBom), JsonOptions)
            ?? throw new InvalidDataException($"Dữ liệu job rỗng hoặc hỏng: {path}");
    }

    /// <summary>
    /// Chuyển trạng thái theo JobTransitions; không hợp lệ ⇒ InvalidTransitionException, KHÔNG ghi gì.
    /// Vào Running ⇒ Attempt + 1. Ghi LastError / LastErrorKind (None khi không lỗi). Thêm 1 JobEvent.
    /// Job chưa có ⇒ InvalidOperationException.
    /// </summary>
    public JobRecord Transition(string project, string task, JobState to, string detail,
        ErrorKind errorKind = ErrorKind.None, string? providerId = null, int? pid = null)
    {
        var current = Get(project, task) ?? throw new InvalidOperationException($"Job '{project}/{task}' chưa tồn tại.");
        if (!JobTransitions.IsAllowed(current.State, to))
            throw new InvalidTransitionException(current.State, to);

        var now = Now;
        var updated = current with
        {
            State = to,
            Attempt = to == JobState.Running ? current.Attempt + 1 : current.Attempt,
            ProviderId = providerId ?? current.ProviderId,
            Pid = pid ?? current.Pid,
            Machine = machine,
            UpdatedAt = now,
            LastErrorKind = errorKind,
            LastError = errorKind == ErrorKind.None ? null : detail,
        };

        WriteRecordAtomic(project, task, updated);
        AppendEvent(project, task, new JobEvent(now, current.State, to, machine, detail, errorKind));
        return updated;
    }

    /// <summary>
    /// Giữ khóa chạy cho task. Đã có khóa của PID còn sống ⇒ false.
    /// Khóa của PID đã chết (máy / tiến trình sập) ⇒ coi là khóa mồ côi: nếu job đang ở trạng thái IsActive thì
    /// chuyển Interrupted (detail nêu PID cũ), rồi giữ khóa mới ⇒ true. Tạo file khóa phải nguyên tử (FileMode.CreateNew).
    /// </summary>
    public bool TryAcquireRun(string project, string task, int pid)
    {
        var lockPath = LockPath(project, task);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        if (TryCreateLockFile(lockPath, pid)) return true;

        var existingPid = ReadLockPid(lockPath);
        if (existingPid is int alivePid && isProcessAlive(alivePid)) return false;

        if (Get(project, task) is { } current && JobTransitions.IsActive(current.State))
        {
            var deadPidText = existingPid?.ToString(CultureInfo.InvariantCulture) ?? "không rõ";
            Transition(project, task, JobState.Interrupted, $"Khóa mồ côi từ PID {deadPidText} đã chết", ErrorKind.Infrastructure);
        }

        File.Delete(lockPath);
        return TryCreateLockFile(lockPath, pid);
    }

    /// <summary>Nhả khóa nếu đúng PID đang giữ; PID khác ⇒ không làm gì.</summary>
    public void ReleaseRun(string project, string task, int pid)
    {
        var lockPath = LockPath(project, task);
        if (File.Exists(lockPath) && ReadLockPid(lockPath) == pid)
            File.Delete(lockPath);
    }

    /// <summary>
    /// Phục hồi sau sự cố: mọi job IsActive có Pid mà tiến trình đã chết ⇒ Interrupted (ErrorKind.Infrastructure),
    /// xóa khóa mồ côi. Trả danh sách job đã chuyển. Job đang sống không bị đụng tới.
    /// </summary>
    public IReadOnlyList<JobRecord> RecoverInterrupted()
    {
        if (!Directory.Exists(root)) return [];
        var recovered = new List<JobRecord>();
        foreach (var projectDir in Directory.GetDirectories(root))
        {
            var project = Path.GetFileName(projectDir);
            if (!IsValidName(project)) continue;
            foreach (var file in Directory.GetFiles(projectDir, "*.json"))
            {
                var task = Path.GetFileNameWithoutExtension(file);
                if (!IsValidName(task) || Get(project, task) is not { } job) continue;

                var lockPath = LockPath(project, task);
                var lockPid = File.Exists(lockPath) ? ReadLockPid(lockPath) : null;
                var effectivePid = job.Pid ?? lockPid;

                if (JobTransitions.IsActive(job.State) && effectivePid is int pid && !isProcessAlive(pid))
                {
                    recovered.Add(Transition(project, task, JobState.Interrupted, $"Tiến trình PID {pid} đã dừng bất ngờ", ErrorKind.Infrastructure));
                    if (File.Exists(lockPath)) File.Delete(lockPath);
                }
                else if (lockPid is int lp && !isProcessAlive(lp))
                {
                    File.Delete(lockPath);
                }
            }
        }
        return recovered;
    }

    /// <summary>Lịch sử chuyển trạng thái theo thứ tự ghi. Dòng hỏng trong file ⇒ bỏ qua dòng đó, không ném lỗi.</summary>
    public IReadOnlyList<JobEvent> History(string project, string task)
    {
        var path = EventsPath(project, task);
        if (!File.Exists(path)) return [];
        var events = new List<JobEvent>();
        foreach (var line in File.ReadLines(path, Utf8NoBom))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                if (JsonSerializer.Deserialize<JobEvent>(line, JsonOptions) is { } ev) events.Add(ev);
            }
            catch (JsonException) { }
        }
        return events;
    }

    private string RecordPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.json");
    private string EventsPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.events.jsonl");
    private string LockPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.lock");

    private string ProjectDir(string project, string task)
    {
        if (!IsValidName(project)) throw new ArgumentException($"Tên project không hợp lệ: '{project}'", nameof(project));
        if (!IsValidName(task)) throw new ArgumentException($"Tên task không hợp lệ: '{task}'", nameof(task));
        return Path.Combine(root, project);
    }

    private void WriteRecordAtomic(string project, string task, JobRecord record)
    {
        var path = RecordPath(project, task);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var tmpPath = Path.Combine(dir, $"{task}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(record, JsonOptions), Utf8NoBom);
        File.Move(tmpPath, path, overwrite: true);
    }

    private void AppendEvent(string project, string task, JobEvent ev)
    {
        var path = EventsPath(project, task);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, JsonSerializer.Serialize(ev, JsonOptions) + "\n", Utf8NoBom);
    }

    private static bool TryCreateLockFile(string lockPath, int pid)
    {
        try
        {
            using var fs = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fs, Utf8NoBom);
            writer.Write(pid.ToString(CultureInfo.InvariantCulture));
            return true;
        }
        catch (IOException) { return false; }
    }

    private static int? ReadLockPid(string lockPath)
    {
        try
        {
            return int.TryParse(File.ReadAllText(lockPath, Utf8NoBom).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid) ? pid : null;
        }
        catch (IOException) { return null; }
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrEmpty(name) &&
        !name.Contains("..", StringComparison.Ordinal) &&
        name.All(ch => ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_' or '-');
}
