using AITeam.Core.Messages;
using AITeam.Core.Routing;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public interface IOfficeService
{
    OfficeView Get();
}

/// <summary>Gom dữ liệu thật cho OfficeBuilder (R-005 / V5-04).</summary>
public sealed class OfficeService : IOfficeService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _board;
    private readonly IAgentRunService _runs;
    private readonly IRequestService _requests;

    public OfficeService(HubPaths paths, ITeamConfigService config, ITaskBoardService board, IAgentRunService runs, IRequestService requests)
    {
        _paths = paths;
        _config = config;
        _board = board;
        _runs = runs;
        _requests = requests;
    }

    /// <summary>
    /// staff/*.md ⇒ StaffMember (ParseStaff bên dưới, bỏ null), sắp theo Agent.
    /// Tasks = mọi dự án ExistsLocally: _board.GetBoard(p).Tasks (lỗi 1 dự án ⇒ bỏ qua dự án đó).
    /// Running = _runs.GetRunning(); Rests = new ProviderStateStore(_paths.AITeamRoot).All().
    /// Messages(project, task) = new MessageStore(Path.Combine(&lt;WorkDir của project&gt;, "logs")).Read(task, 3) (project không có / lỗi ⇒ rỗng).
    /// CurrentRequest = phiếu đầu tiên của _requests.GetAll() có Status bắt đầu bằng "🟡" (không có ⇒ null; lỗi ⇒ null).
    /// Now = DateTime.Now. Trả OfficeBuilder.Build(...).
    /// </summary>
    public OfficeView Get()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// staff/A-minh.md ⇒ Agent = phần trước "-" đầu của tên file; Name = "- **Tên:**"; Role = "- **Chuyên môn:**" (cắt còn 40 ký tự, thiếu ⇒ "");
    /// Provider = "codex" nếu dòng "- **Chạy bằng:**" chứa "Codex" (không phân biệt hoa thường), ngược lại "agy". Thiếu Tên ⇒ null.
    /// </summary>
    public static StaffMember? ParseStaff(string fileName, string markdown)
        => throw new NotImplementedException(); // TODO(agent)
}
