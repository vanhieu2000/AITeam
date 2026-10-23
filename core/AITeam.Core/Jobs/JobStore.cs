using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AITeam.Core.Jobs;

/// <summary>
/// Lưu trạng thái job CỤC BỘ từng máy (tiến trình, PID, nhịp sống). Nguồn sự thật dùng chung giữa các máy vẫn là
/// file task + bảng việc trên git. Thiết kế: docs/design/V4-MULTI-AI.md mục B4.
/// KHUNG do Claude viết — agent chỉ điền phần thân (TODO(agent)), giữ nguyên chữ ký public.
///
/// Bố cục trên đĩa (root thường là %LOCALAPPDATA%\AITeam\jobs):
///   {root}\{project}\{task}.json          — JobRecord hiện tại (ghi file tạm rồi đổi tên: không bao giờ để file dở)
///   {root}\{project}\{task}.events.jsonl  — mỗi lần chuyển trạng thái 1 dòng JobEvent, chỉ ghi thêm
///   {root}\{project}\{task}.lock          — khóa chống chạy trùng: nội dung là PID đang giữ
/// Tên project / task chỉ nhận [A-Za-z0-9._-]; tên khác (có .., /, \, :) ⇒ ArgumentException (chặn ghi ra ngoài root).
/// </summary>
public sealed class JobStore(string root, string machine, Func<int, bool> isProcessAlive, Func<DateTimeOffset>? clock = null)
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.Now;

    /// <summary>Tạo job ở trạng thái Created (Attempt = 0). Đã có ⇒ trả bản đang có, không ghi đè.</summary>
    public JobRecord Create(string project, string task)
    {
        var existing = Get(project, task);
        if (existing is not null) return existing;

        var now = Now;
        var record = new JobRecord(
            Project: project,
            Task: task,
            State: JobState.Created,
            Attempt: 0,
            ProviderId: null,
            Pid: null,
            Machine: machine,
            CreatedAt: now,
            UpdatedAt: now,
            LastErrorKind: ErrorKind.None,
            LastError: null);

        WriteRecordAtomic(project, task, record);
        return record;
    }

    public JobRecord? Get(string project, string task)
    {
        var path = RecordPath(project, task);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path, Utf8NoBom);
        return JsonSerializer.Deserialize<JobRecord>(json, JsonOptions)
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
        var current = Get(project, task)
            ?? throw new InvalidOperationException($"Job '{project}/{task}' chưa tồn tại.");

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

        if (TryCreateLockFile(lockPath, pid))
            return true;

        var existingPid = ReadLockPid(lockPath);
        if (existingPid is int alivePid && isProcessAlive(alivePid))
            return false;

        var current = Get(project, task);
        if (current is not null && JobTransitions.IsActive(current.State))
        {
            var deadPidText = existingPid?.ToString(CultureInfo.InvariantCulture) ?? "không rõ";
            Transition(project, task, JobState.Interrupted,
                $"Khóa mồ côi từ PID {deadPidText} đã chết", ErrorKind.Infrastructure);
        }

        File.Delete(lockPath);
        return TryCreateLockFile(lockPath, pid);
    }

    /// <summary>Nhả khóa nếu đúng PID đang giữ; PID khác ⇒ không làm gì.</summary>
    public void ReleaseRun(string project, string task, int pid)
    {
        var lockPath = LockPath(project, task);
        if (!File.Exists(lockPath)) return;
        if (ReadLockPid(lockPath) == pid)
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
                if (!IsValidName(task)) continue;

                var job = Get(project, task);
                if (job is null) continue;

                var lockPath = LockPath(project, task);
                var lockPid = File.Exists(lockPath) ? ReadLockPid(lockPath) : null;
                var effectivePid = job.Pid ?? lockPid;

                if (JobTransitions.IsActive(job.State) && effectivePid is int pid && !isProcessAlive(pid))
                {
                    var moved = Transition(project, task, JobState.Interrupted,
                        $"Tiến trình PID {pid} đã dừng bất ngờ", ErrorKind.Infrastructure);
                    if (File.Exists(lockPath))
                        File.Delete(lockPath);
                    recovered.Add(moved);
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
                var ev = JsonSerializer.Deserialize<JobEvent>(line, JsonOptions);
                if (ev is not null) events.Add(ev);
            }
            catch (JsonException)
            {
                // Bỏ qua dòng lịch sử bị hỏng theo thiết kế
            }
        }

        return events;
    }

    private string RecordPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.json");
    private string EventsPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.events.jsonl");
    private string LockPath(string project, string task) => Path.Combine(ProjectDir(project, task), $"{task}.lock");

    private string ProjectDir(string project, string task)
    {
        ValidateName(project, nameof(project));
        ValidateName(task, nameof(task));
        return Path.Combine(root, project);
    }

    private void WriteRecordAtomic(string project, string task, JobRecord record)
    {
        var path = RecordPath(project, task);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        var tmpPath = Path.Combine(dir, $"{task}.{Guid.NewGuid():N}.tmp");
        var json = JsonSerializer.Serialize(record, JsonOptions);
        File.WriteAllText(tmpPath, json, Utf8NoBom);
        File.Move(tmpPath, path, overwrite: true);
    }

    private void AppendEvent(string project, string task, JobEvent ev)
    {
        var path = EventsPath(project, task);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(ev, JsonOptions) + "\n";
        File.AppendAllText(path, line, Utf8NoBom);
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
        catch (IOException)
        {
            return false;
        }
    }

    private static int? ReadLockPid(string lockPath)
    {
        try
        {
            var content = File.ReadAllText(lockPath, Utf8NoBom).Trim();
            return int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid) ? pid : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void ValidateName(string name, string paramName)
    {
        if (!IsValidName(name))
            throw new ArgumentException($"Tên project/task không hợp lệ: '{name}'", paramName);
    }

    private static bool IsValidName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Contains("..", StringComparison.Ordinal))
            return false;

        foreach (var ch in name)
        {
            var valid = ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_' or '-';
            if (!valid) return false;
        }

        return true;
    }
}
