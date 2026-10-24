using System.Diagnostics;
using System.Text.Json;
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

    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _board;
    private readonly IProcessStarter _starter;
    private readonly Func<DateTime> _now;

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
    public static string Label(HubCommand c)
        => throw new NotImplementedException(); // TODO(agent)

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
        => throw new NotImplementedException(); // TODO(agent)

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
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Làm sạch góp ý trước khi truyền cho PowerShell 5.1 (tham số có " / xuống dòng từng bị sai): xuống dòng (\r\n, \n, \r) ⇒ " / ",
    /// bỏ mọi ký tự điều khiển khác (char.IsControl), dấu " ⇒ ', dấu ` (backtick) ⇒ ', rồi Trim. null ⇒ "".
    /// </summary>
    public static string CleanFeedback(string? feedback)
        => throw new NotImplementedException(); // TODO(agent)

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
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<CommandJob> Jobs()
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<AuditEntry> Audit(int max = 50)
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<TaskCard> Assignable()
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<TaskCard> Resumable()
        => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>
/// Chạy thật: Process (UseShellExecute false, CreateNoWindow, ArgumentList từng phần tử, WorkingDirectory),
/// stdout + stderr ghi nối vào logFile (UTF-8, mỗi dòng 1 lần ghi có khóa), stdin chuyển hướng rồi đóng ngay (team.ps1 / agy treo nếu stdin mở);
/// EnableRaisingEvents + Exited ⇒ đợi đọc hết output (WaitForExit()) rồi onExit(ExitCode). Không đợi tiến trình xong.
/// </summary>
public sealed class RealProcessStarter : IProcessStarter
{
    public void Start(string exe, IReadOnlyList<string> args, string workDir, string logFile, Action<int> onExit)
        => throw new NotImplementedException(); // TODO(agent)
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
