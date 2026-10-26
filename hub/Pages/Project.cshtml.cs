using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public class ProjectModel : PageModel
{
    private readonly ITaskBoardService _boardService;
    private readonly ITeamConfigService _configService;
    private readonly IAgentRunService? _runService;

    private readonly IWorkProgressService? _workService;

    public ProjectModel(ITaskBoardService boardService, ITeamConfigService configService, IAgentRunService? runService = null, IWorkProgressService? workService = null)
    {
        _boardService = boardService;
        _configService = configService;
        _runService = runService;
        _workService = workService;
    }

    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "board";

    /// <summary>Mã yêu cầu trong snapshot; rỗng chọn yêu cầu đầu; sai hoặc khác dự án trả 404.</summary>
    [BindProperty(SupportsGet = true)]
    public string? RequestId { get; set; }

    /// <summary>Mã task thuộc yêu cầu đã chọn; rỗng xem yêu cầu; sai quan hệ trả 404.</summary>
    [BindProperty(SupportsGet = true)]
    public string? TaskId { get; set; }

    public WorkProjectProgress? Work { get; private set; }
    public WorkRequestProgress? SelectedRequest { get; private set; }
    public WorkTaskProgress? SelectedTask { get; private set; }
    public WorkProgress? SelectedProgress { get; private set; }
    public string? WorkError { get; private set; }

    public ProjectBoard? Board { get; private set; }
    public IReadOnlyList<AgentRun> Runs { get; private set; } = Array.Empty<AgentRun>();

    public IReadOnlyList<TaskCard> AssignedTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Assigned).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> ReviewTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Review).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> NeedsFixTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.NeedsFix).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> MergedTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Merged).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> MergedPreviewItems { get; private set; } = Array.Empty<TaskCard>();
    public int MergedRemaining { get; private set; }
    public IReadOnlyList<TaskCard> AllMergedOrdered { get; private set; } = Array.Empty<TaskCard>();

    private static int ExtractNumber(string id)
    {
        var match = Regex.Match(id, @"\d+");
        return match.Success && int.TryParse(match.Value, out var n) ? n : -1;
    }

    /// <summary>
    /// GET /p/{name}: giữ bảng, kế hoạch và lịch sử; tab mặc định/board/work hiển thị Việc.
    /// Name rỗng chuyển /Index; dự án không cấu hình hoặc mã chọn không thuộc snapshot trả 404.
    /// Gọi IWorkProgressService.GetProject đúng Name cấu hình, một lần; không đọc file/chạy lệnh.
    /// Chọn Id không phân biệt hoa thường; RequestId rỗng chọn yêu cầu đầu trong snapshot;
    /// TaskId rỗng chọn tiến độ yêu cầu, có TaskId phải thuộc SelectedRequest.
    /// Dự án rỗng vẫn Page, SelectedProgress=null; giao diện ghi 'Chưa có việc.'.
    /// IOException từ dịch vụ: Page với WorkError='Không đọc được tiến độ công việc.',
    /// không lộ exception/đường dẫn/nội dung; InvalidDataException trả 404.
    /// SelectedProgress giữ nguyên tiến độ từ W1-06, không suy diễn trạng thái/giờ/người.
    /// Không có POST hoặc nút chạy lệnh; link /Approvals giữ handler duyệt hiện có.
    /// </summary>
    public IActionResult OnGet()
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
