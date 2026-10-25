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

    private readonly IResearchService? _research;

    public OfficeService(HubPaths paths, ITeamConfigService config, ITaskBoardService board, IAgentRunService runs, IRequestService requests, IResearchService? research = null)
    {
        _paths = paths;
        _config = config;
        _board = board;
        _runs = runs;
        _requests = requests;
        _research = research;
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
    {
        var staffDir = Path.Combine(_paths.AITeamRoot, "staff");
        var staff = Directory.Exists(staffDir)
            ? Directory.EnumerateFiles(staffDir, "*.md")
                .Select(f => ParseStaff(Path.GetFileName(f), File.ReadAllText(f)))
                .Where(s => s != null)
                .Select(s => s!)
                .OrderBy(s => s.Agent, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<StaffMember>();

        var projects = _config.GetProjects();
        var tasks = new List<TaskCard>();
        foreach (var p in projects.Where(p => p.ExistsLocally))
        {
            try { tasks.AddRange(_board.GetBoard(p.Name).Tasks); }
            catch { }
        }

        var running = _runs.GetRunning();
        var rests = new ProviderStateStore(_paths.AITeamRoot).All();

        IReadOnlyList<TaskMessage> GetMessages(string project, string task)
        {
            try
            {
                var proj = projects.FirstOrDefault(p => string.Equals(p.Name, project, StringComparison.OrdinalIgnoreCase))
                           ?? _config.FindProject(project);
                if (proj == null || string.IsNullOrEmpty(proj.WorkDir)) return Array.Empty<TaskMessage>();
                return new MessageStore(Path.Combine(proj.WorkDir, "logs")).Read(task, 3);
            }
            catch
            {
                return Array.Empty<TaskMessage>();
            }
        }

        RequestThread? currentReq = null;
        try
        {
            currentReq = _requests.GetAll().FirstOrDefault(r => r.Status != null && r.Status.StartsWith("🟡", StringComparison.Ordinal));
        }
        catch { }

        int researchRunning = 0;
        try { researchRunning = _research?.RunningCount() ?? 0; } catch { }
        return OfficeBuilder.Build(new OfficeInput(DateTime.Now, staff, tasks, running, rests, GetMessages, currentReq, researchRunning));
    }


    /// <summary>
    /// staff/A-minh.md ⇒ Agent = phần trước "-" đầu của tên file; Name = "- **Tên:**"; Role = "- **Chuyên môn:**" (cắt còn 40 ký tự, thiếu ⇒ "");
    /// Provider = "codex" nếu dòng "- **Chạy bằng:**" chứa "Codex" (không phân biệt hoa thường), ngược lại "agy". Thiếu Tên ⇒ null.
    /// </summary>
    public static StaffMember? ParseStaff(string fileName, string markdown)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName ?? "");
        var dash = baseName.IndexOf('-');
        var agent = (dash > 0 ? baseName[..dash] : baseName).Trim();
        if (string.IsNullOrEmpty(agent)) return null;

        string name = "", role = "", provider = "agy";
        foreach (var raw in (markdown ?? "").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
        {
            var line = raw.Trim();
            if (line.StartsWith("- **Tên:**", StringComparison.OrdinalIgnoreCase))
                name = line["- **Tên:**".Length..].Trim();
            else if (line.StartsWith("- **Chuyên môn:**", StringComparison.OrdinalIgnoreCase))
                role = line["- **Chuyên môn:**".Length..].Trim();
            else if (line.StartsWith("- **Chạy bằng:**", StringComparison.OrdinalIgnoreCase))
            {
                var runBy = line["- **Chạy bằng:**".Length..].Trim();
                provider = runBy.Contains("Codex", StringComparison.OrdinalIgnoreCase) ? "codex" : "agy";
            }
        }

        if (string.IsNullOrEmpty(name)) return null;
        if (role.Length > 40) role = role[..40];
        return new StaffMember(agent, name, role, provider);
    }

}
