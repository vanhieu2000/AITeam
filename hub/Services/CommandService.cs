using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// Điều khiển team từ Hub (R-005 / V5-05). Chủ dự án đã duyệt 25/10: CHỈ các lệnh cố định dưới đây, có xác nhận + nhật ký;
// merge KHÔNG có ở đây (vẫn chỉ qua trang Cần anh duyệt + team.ps1). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C2.

public enum HubCommand { Assign, AssignCodex, Stop, Resume, Verify }

public sealed record CommandRequest(HubCommand Command, string Project, string Task, string? Feedback = null);

/// <summary>Một lệnh đã chạy. ExitCode / EndedAt null ⇒ đang chạy.</summary>
public sealed record CommandJob(string Id, CommandRequest Request, DateTime StartedAt, string LogFile, int? ExitCode, DateTime? EndedAt)
{
    public bool Running => ExitCode == null;
}

/// <summary>Một dòng nhật ký thao tác (.state/audit.jsonl). Result: "started" | "rejected" | "exit {n}".</summary>
public sealed record AuditEntry(DateTime At, string Action, string Project, string Task, string Result, string? Detail);

/// <summary>Khởi chạy tiến trình (tách ra để test không chạy PowerShell thật). onExit(exitCode) gọi khi tiến trình kết thúc.</summary>
public interface IProcessStarter
{
    void Start(string exe, IReadOnlyList<string> args, string workDir, string logFile, Action<int> onExit);
}

public interface ICommandService
{
    /// <summary>null ⇒ hợp lệ; ngược lại câu báo lỗi tiếng Việt (xem CommandService.Validate).</summary>
    string? Validate(CommandRequest req);

    /// <summary>Validate rồi chạy; không hợp lệ ⇒ ghi audit "rejected" + InvalidOperationException(lỗi). Hợp lệ ⇒ audit "started", trả job.</summary>
    CommandJob Start(CommandRequest req);

    /// <summary>Các job từ lúc Hub chạy, mới nhất trước (StartedAt giảm dần, cùng giờ ⇒ thêm sau đứng trước), tối đa 20.</summary>
    IReadOnlyList<CommandJob> Jobs();

    /// <summary>Nhật ký thao tác, mới nhất trước (dòng cuối file đứng đầu), tối đa <paramref name="max"/>; dòng hỏng bỏ qua; chưa có file ⇒ rỗng.</summary>
    IReadOnlyList<AuditEntry> Audit(int max = 50);

    /// <summary>Task GIAO được: mọi dự án ExistsLocally, State Unknown hoặc NeedsFix, FileName khác rỗng. Thứ tự: Project, rồi thứ tự trên bảng việc.</summary>
    IReadOnlyList<TaskCard> Assignable();

    /// <summary>Task GIAO LẠI KÈM GÓP Ý / KIỂM TRA LẠI được: như Assignable nhưng State Review hoặc NeedsFix.</summary>
    IReadOnlyList<TaskCard> Resumable();
}

public sealed class CommandService : ICommandService
{
    public const int MaxFeedback = 2000;
    public const int MaxRunning = 3;
    public static string PowerShellExe => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");

    private static readonly Regex TaskIdRegex = new(@"^[A-Za-z0-9][A-Za-z0-9-]{0,63}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _board;
    private readonly IProcessStarter _starter;
    private readonly Func<DateTime> _now;
    private readonly object _lock = new();
    private readonly List<CommandJob> _jobs = new();

    public CommandService(HubPaths paths, ITeamConfigService config, ITaskBoardService board, IProcessStarter starter, Func<DateTime>? now = null)
    {
        _paths = paths;
        _config = config;
        _board = board;
        _starter = starter;
        _now = now ?? (() => DateTime.Now);
    }

    public string AuditFile => Path.Combine(_paths.AITeamRoot, ".state", "audit.jsonl");
    public string JobsDir => Path.Combine(_paths.AITeamRoot, ".state", "hub-jobs");

    /// <summary>Chữ trên nút / trong danh sách: Assign "Giao task", AssignCodex "Giao cho Codex", Stop "Dừng", Resume "Giao lại kèm góp ý", Verify "Kiểm tra lại".</summary>
    public static string Label(HubCommand c) => c switch
    {
        HubCommand.Assign => "Giao task",
        HubCommand.AssignCodex => "Giao cho Codex",
        HubCommand.Stop => "Dừng",
        HubCommand.Resume => "Giao lại kèm góp ý",
        HubCommand.Verify => "Kiểm tra lại",
        _ => c.ToString(),
    };

    /// <summary>
    /// Thứ tự kiểm tra, câu lỗi CHÍNH XÁC:
    /// 1. Project không có trong config.GetProjects() (so tên không phân biệt hoa thường) ⇒ "Dự án không hợp lệ."
    /// 2. Task không khớp ^[A-Za-z0-9][A-Za-z0-9-]{0,63}$ ⇒ "Mã task không hợp lệ."
    /// 3. Không có trên bảng việc của dự án (board.GetBoard(tên dự án đúng hoa thường).Tasks, Id khớp không phân biệt hoa thường) ⇒ "Không thấy task {Task} trên bảng việc."
    /// 4. Đã có job ĐANG CHẠY cho cùng Project + Task (không phân biệt hoa thường) ⇒ "Đang có lệnh chạy cho {Task}."
    ///    Đã có MaxRunning job đang chạy (mọi task) ⇒ "Đang chạy tối đa 3 lệnh, đợi xong rồi thử lại."
    /// 5. Theo lệnh và State của task:
    ///    - Assign / AssignCodex: State Unknown hoặc NeedsFix; khác ⇒ "Chỉ giao được task chưa giao hoặc cần sửa."
    ///    - Stop: State Assigned; khác ⇒ "Task {Task} không đang chạy."
    ///    - Resume: State Review hoặc NeedsFix; khác ⇒ "Chỉ giao lại được task chờ review hoặc cần sửa.";
    ///      Feedback dài hơn MaxFeedback (độ dài gốc) ⇒ "Góp ý quá dài (tối đa 2000 ký tự)."; CleanFeedback(Feedback) rỗng ⇒ "Cần nội dung góp ý."
    ///    - Verify: State Assigned, Review hoặc NeedsFix; khác ⇒ "Task {Task} chưa có bản làm việc để kiểm tra."
    /// {Task} là chuỗi người dùng gửi (đã qua bước 2 nên an toàn). Hợp lệ ⇒ null. Lỗi đọc cấu hình / bảng việc ⇒ để ngoại lệ bay ra.
    /// </summary>
    public string? Validate(CommandRequest req)
    {
        lock (_lock) return ValidateCore(req);
    }

    private string? ValidateCore(CommandRequest req)
    {
        var proj = _config.GetProjects().FirstOrDefault(p => string.Equals(p.Name, req.Project, StringComparison.OrdinalIgnoreCase));
        if (proj == null) return "Dự án không hợp lệ.";
        if (string.IsNullOrEmpty(req.Task) || !TaskIdRegex.IsMatch(req.Task)) return "Mã task không hợp lệ.";

        var card = _board.GetBoard(proj.Name).Tasks.FirstOrDefault(t => string.Equals(t.Id, req.Task, StringComparison.OrdinalIgnoreCase));
        if (card == null) return $"Không thấy task {req.Task} trên bảng việc.";

        if (_jobs.Any(j => j.Running && string.Equals(j.Request.Project, req.Project, StringComparison.OrdinalIgnoreCase) && string.Equals(j.Request.Task, req.Task, StringComparison.OrdinalIgnoreCase)))
            return $"Đang có lệnh chạy cho {card.Id}.";
        if (_jobs.Count(j => j.Running) >= MaxRunning)
            return "Đang chạy tối đa 3 lệnh, đợi xong rồi thử lại.";

        return req.Command switch
        {
            HubCommand.Assign or HubCommand.AssignCodex => card.State is TaskState.Unknown or TaskState.NeedsFix ? null : "Chỉ giao được task chưa giao hoặc cần sửa.",
            HubCommand.Stop => card.State == TaskState.Assigned ? null : $"Task {card.Id} không đang chạy.",
            HubCommand.Resume when card.State is not (TaskState.Review or TaskState.NeedsFix) => "Chỉ giao lại được task chờ review hoặc cần sửa.",
            HubCommand.Resume when req.Feedback != null && req.Feedback.Length > MaxFeedback => "Góp ý quá dài (tối đa 2000 ký tự).",
            HubCommand.Resume when string.IsNullOrEmpty(CleanFeedback(req.Feedback)) => "Cần nội dung góp ý.",
            HubCommand.Resume => null,
            HubCommand.Verify => card.State is TaskState.Assigned or TaskState.Review or TaskState.NeedsFix ? null : $"Task {card.Id} chưa có bản làm việc để kiểm tra.",
            _ => "Lệnh không hợp lệ.",
        };
    }

    /// <summary>
    /// Tham số cho powershell.exe (KHÔNG qua shell, dùng ArgumentList), đúng thứ tự:
    /// ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", teamPs1, &lt;lệnh&gt;…]
    ///   Assign      ⇒ "assign", "-Project", P, "-Task", T
    ///   AssignCodex ⇒ "assign", "-Project", P, "-Task", T, "-Dev", "codex"
    ///   Stop        ⇒ "stop", "-Task", T
    ///   Resume      ⇒ "assign", "-Project", P, "-Task", T, "-Resume", "-Feedback", CleanFeedback(Feedback)
    ///   Verify      ⇒ "verify", "-Project", P, "-Task", T
    /// </summary>
    public static IReadOnlyList<string> BuildArguments(CommandRequest req, string teamPs1)
    {
        string[] head = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", teamPs1 };
        string[] tail = req.Command switch
        {
            HubCommand.Assign => new[] { "assign", "-Project", req.Project, "-Task", req.Task },
            HubCommand.AssignCodex => new[] { "assign", "-Project", req.Project, "-Task", req.Task, "-Dev", "codex" },
            HubCommand.Stop => new[] { "stop", "-Task", req.Task },
            HubCommand.Resume => new[] { "assign", "-Project", req.Project, "-Task", req.Task, "-Resume", "-Feedback", CleanFeedback(req.Feedback) },
            HubCommand.Verify => new[] { "verify", "-Project", req.Project, "-Task", req.Task },
            _ => throw new ArgumentOutOfRangeException(nameof(req)),
        };
        return head.Concat(tail).ToList();
    }

    /// <summary>
    /// Làm sạch góp ý trước khi truyền cho PowerShell 5.1 (tham số có " / xuống dòng từng bị sai): xuống dòng (\r\n, \n, \r) ⇒ " / ",
    /// bỏ mọi ký tự điều khiển khác (char.IsControl), dấu " ⇒ ', dấu ` (backtick) ⇒ ', rồi Trim. null ⇒ "".
    /// </summary>
    public static string CleanFeedback(string? feedback)
    {
        if (string.IsNullOrEmpty(feedback)) return "";
        var filtered = new string(feedback.Where(ch => ch is '\r' or '\n' || !char.IsControl(ch)).ToArray()).Trim();
        if (filtered.Length == 0) return "";
        return filtered.Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " / ").Replace('"', '\'').Replace('`', '\'').Trim();
    }

    /// <summary>
    /// Validate lỗi ⇒ ghi audit (Action = tên enum, Result "rejected", Detail = câu lỗi) rồi ném InvalidOperationException(câu lỗi).
    /// Hợp lệ (toàn bộ Start trong 1 lock, để 2 lần bấm cùng lúc không qua được bước 4):
    ///   Id = "{_now():yyyyMMdd-HHmmss}-{Task}" (trùng Id đã có ⇒ thêm "-2", "-3"…); LogFile = JobsDir/{Id}.log (tạo thư mục);
    ///   thêm job vào danh sách; ghi audit "started" (Detail = CleanFeedback nếu Resume, ngược lại null);
    ///   _starter.Start(PowerShellExe, BuildArguments(req, Path.Combine(AITeamRoot, "team.ps1")), AITeamRoot, LogFile, onExit).
    ///   onExit(n): thay job bằng bản có ExitCode = n, EndedAt = _now() (khóa lock), ghi audit "exit {n}".
    ///   _starter.Start ném lỗi ⇒ job kết thúc ExitCode -1, audit "exit -1" (Detail = ex.Message), rồi ném lại.
    /// Audit: mỗi dòng 1 JSON (JsonSerializerDefaults.Web ⇒ camelCase), UTF-8 KHÔNG BOM, ghi nối có khóa.
    /// </summary>
    public CommandJob Start(CommandRequest req)
    {
        lock (_lock)
        {
            var err = ValidateCore(req);
            if (err != null)
            {
                WriteAudit(new AuditEntry(_now(), req.Command.ToString(), req.Project, req.Task, "rejected", err));
                throw new InvalidOperationException(err);
            }

            var now = _now();
            var baseId = $"{now:yyyyMMdd-HHmmss}-{req.Task}";
            var id = baseId;
            for (var s = 2; _jobs.Any(j => string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase)); s++)
                id = $"{baseId}-{s}";

            Directory.CreateDirectory(JobsDir);
            var logFile = Path.Combine(JobsDir, id + ".log");
            _jobs.Add(new CommandJob(id, req, now, logFile, null, null));
            WriteAudit(new AuditEntry(now, req.Command.ToString(), req.Project, req.Task, "started", req.Command == HubCommand.Resume ? CleanFeedback(req.Feedback) : null));

            var finished = false;
            void Finish(int code, string? exitDetail)
            {
                lock (_lock)
                {
                    if (finished) return;
                    finished = true;
                    var endedAt = _now();
                    var idx = _jobs.FindIndex(j => j.Id == id);
                    if (idx >= 0) _jobs[idx] = _jobs[idx] with { ExitCode = code, EndedAt = endedAt };
                    WriteAudit(new AuditEntry(endedAt, req.Command.ToString(), req.Project, req.Task, $"exit {code}", exitDetail));
                }
            }

            try
            {
                _starter.Start(PowerShellExe, BuildArguments(req, Path.Combine(_paths.AITeamRoot, "team.ps1")), _paths.AITeamRoot, logFile, code => Finish(code, null));
            }
            catch (Exception ex)
            {
                Finish(-1, ex.Message);
                throw;
            }
            return _jobs.First(j => j.Id == id);
        }
    }

    private void WriteAudit(AuditEntry entry)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AuditFile)!);
        File.AppendAllText(AuditFile, JsonSerializer.Serialize(entry, AuditJsonOptions) + "\n", Utf8NoBom);
    }

    public IReadOnlyList<CommandJob> Jobs()
    {
        lock (_lock) return _jobs.AsEnumerable().Reverse().OrderByDescending(j => j.StartedAt).Take(20).ToList();
    }

    public IReadOnlyList<AuditEntry> Audit(int max = 50)
    {
        if (max <= 0) return Array.Empty<AuditEntry>();
        lock (_lock)
        {
            if (!File.Exists(AuditFile)) return Array.Empty<AuditEntry>();
            var lines = File.ReadAllLines(AuditFile, Utf8NoBom);
            var result = new List<AuditEntry>();
            for (var i = lines.Length - 1; i >= 0 && result.Count < max; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                try
                {
                    if (JsonSerializer.Deserialize<AuditEntry>(lines[i], AuditJsonOptions) is { } e) result.Add(e);
                }
                catch (JsonException) { }
            }
            return result;
        }
    }

    public IReadOnlyList<TaskCard> Assignable()
        => FilterTasks(s => s is TaskState.Unknown or TaskState.NeedsFix);

    public IReadOnlyList<TaskCard> Resumable()
        => FilterTasks(s => s is TaskState.Review or TaskState.NeedsFix);

    private IReadOnlyList<TaskCard> FilterTasks(Func<TaskState, bool> predicate) =>
        _config.GetProjects()
            .Where(p => p.ExistsLocally)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .SelectMany(p => _board.GetBoard(p.Name).Tasks)
            .Where(t => !string.IsNullOrWhiteSpace(t.FileName) && predicate(t.State))
            .ToList();
}

/// <summary>
/// Chạy thật: Process (UseShellExecute false, CreateNoWindow, ArgumentList từng phần tử, WorkingDirectory),
/// stdout + stderr ghi nối vào logFile (UTF-8, mỗi dòng 1 lần ghi có khóa), stdin chuyển hướng rồi đóng ngay (team.ps1 / agy treo nếu stdin mở);
/// EnableRaisingEvents + Exited ⇒ đợi đọc hết output (WaitForExit()) rồi onExit(ExitCode). Không đợi tiến trình xong.
/// </summary>
public sealed class RealProcessStarter : IProcessStarter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public void Start(string exe, IReadOnlyList<string> args, string workDir, string logFile, Action<int> onExit)
    {
        if (Path.GetDirectoryName(logFile) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var logLock = new object();
        var started = new ManualResetEventSlim(false);
        void Append(string? s) { if (s != null) lock (logLock) File.AppendAllText(logFile, s + Environment.NewLine, Utf8NoBom); }

        proc.OutputDataReceived += (_, e) => Append(e.Data);
        proc.ErrorDataReceived += (_, e) => Append(e.Data);
        proc.Exited += (_, _) => Task.Run(() =>
        {
            try { started.Wait(); proc.WaitForExit(); var c = proc.ExitCode; proc.Dispose(); started.Dispose(); onExit(c); }
            catch { try { proc.Dispose(); } catch { } try { started.Dispose(); } catch { } onExit(-1); }
        });

        try { proc.Start(); proc.StandardInput.Close(); proc.BeginOutputReadLine(); proc.BeginErrorReadLine(); started.Set(); }
        catch { started.Set(); proc.Dispose(); started.Dispose(); throw; }
    }
}

/// <summary>Chế độ dữ liệu giả / rỗng (HUB_FAKE, HUB_EMPTY): không chạy gì, chỉ ghi 1 dòng vào log rồi báo xong mã 0. (Claude viết.)</summary>
public sealed class NoopProcessStarter : IProcessStarter
{
    public void Start(string exe, IReadOnlyList<string> args, string workDir, string logFile, Action<int> onExit)
    {
        File.AppendAllText(logFile, "(chế độ dữ liệu giả — không chạy) " + string.Join(' ', args.Skip(5)) + Environment.NewLine);
        onExit(0);
    }
}
