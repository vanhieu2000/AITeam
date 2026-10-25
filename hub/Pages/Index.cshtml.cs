using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

public partial class IndexModel : PageModel
{
    private readonly ITeamConfigService _configService;
    private readonly ITaskBoardService _boardService;
    private readonly IAgentRunService _runService;
    private readonly IHealthService _healthService;
    private readonly ITeamInsightsService? _insights;
    private readonly IApprovalService? _approvals;

    public IndexModel(
        ITeamConfigService configService,
        ITaskBoardService boardService,
        IAgentRunService runService,
        IHealthService healthService,
        ITeamInsightsService? insights = null,
        IApprovalService? approvals = null)
    {
        _configService = configService;
        _boardService = boardService;
        _runService = runService;
        _healthService = healthService;
        _insights = insights;
        _approvals = approvals;
    }

    public record ProjectCardView(
        ProjectInfo Info,
        int AssignedCount,
        int ReviewCount,
        int NeedsFixCount,
        int MergedCount,
        string LatestActivity);

    public IReadOnlyList<ProjectCardView> Projects { get; private set; } = Array.Empty<ProjectCardView>();
    public IReadOnlyList<RunningAgent> RunningAgents { get; private set; } = Array.Empty<RunningAgent>();
    public IReadOnlyList<HealthItem> HealthItems { get; private set; } = Array.Empty<HealthItem>();
    public IReadOnlyList<ActivityItem> RecentActivities { get; private set; } = Array.Empty<ActivityItem>();

    public int TotalProjects => Projects.Count;
    public int TotalAssigned => Projects.Sum(p => p.AssignedCount);
    public int TotalReview => Projects.Sum(p => p.ReviewCount);
    public int TotalNeedsFix => Projects.Sum(p => p.NeedsFixCount);
    public int TotalRunningAgents => RunningAgents.Count;

    public int OkHealthCount => HealthItems.Count(h => h.Level == HealthLevel.Ok);
    public int TotalHealthCount => HealthItems.Count;
    public bool HasHealthError => HealthItems.Any(h => h.Level == HealthLevel.Error);

    /// <summary>Chỉ "staff" chọn Nhân viên; null, rỗng hoặc tab lạ về "overview".</summary>
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "overview";

    /// <summary>Mô hình Team hiện có, nạp khi chọn staff, kể cả trường hợp không có dịch vụ.</summary>
    public TeamModel? StaffView { get; private set; }

    /// <summary>
    /// Chuẩn hóa Tab; nạp StaffView bằng TeamModel và ITeamInsightsService hiện có, không tính lại số liệu.
    /// Dịch vụ null: dùng dữ liệu rỗng, không lỗi. Dịch vụ lỗi: hiển thị
    /// "Không đọc được dữ liệu đánh giá: " + ex.Message, không ném lỗi khỏi trang.
    /// Không đổi Range, Insights, Chart, TaskRows hoặc các handler hiện có.
    /// </summary>
    private void LoadPerformanceStaff()
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    public void OnGet()
    {
        LoadData();
        LoadV3();
        LoadPerformanceStaff();
    }

    public IActionResult OnGetRunningAgents()
    {
        RunningAgents = _runService.GetRunning();
        var viewData = new ViewDataDictionary<IReadOnlyList<RunningAgent>>(
            new EmptyModelMetadataProvider(),
            ModelState)
        {
            Model = RunningAgents
        };

        return new PartialViewResult
        {
            ViewName = "_RunningAgentsPartial",
            ViewData = viewData
        };
    }

    public IActionResult OnGetHealthPanel()
    {
        HealthItems = _healthService.Check();
        var viewData = new ViewDataDictionary<IReadOnlyList<HealthItem>>(
            new EmptyModelMetadataProvider(),
            ModelState)
        {
            Model = HealthItems
        };

        return new PartialViewResult
        {
            ViewName = "_HealthPanelPartial",
            ViewData = viewData
        };
    }

    private void LoadData()
    {
        var rawProjects = _configService.GetProjects();
        var projectViews = new List<ProjectCardView>();

        foreach (var p in rawProjects)
        {
            var board = _boardService.GetBoard(p.Name);
            var assigned = board.Tasks.Count(t => t.State == TaskState.Assigned);
            var review = board.Tasks.Count(t => t.State == TaskState.Review);
            var needsFix = board.Tasks.Count(t => t.State == TaskState.NeedsFix);
            var merged = board.Tasks.Count(t => t.State == TaskState.Merged);

            var projectRuns = _runService.GetRuns(p.Name);
            var latestRun = projectRuns.OrderByDescending(r => r.Time).FirstOrDefault();
            string latestActivity;
            if (latestRun != null)
            {
                latestActivity = $"{latestRun.Task} · {FormatRelativeTime(latestRun.Time)}";
            }
            else
            {
                var latestTask = board.Tasks.FirstOrDefault();
                if (latestTask != null)
                {
                    latestActivity = $"{latestTask.Id} · {latestTask.State}";
                }
                else
                {
                    latestActivity = "—";
                }
            }

            projectViews.Add(new ProjectCardView(p, assigned, review, needsFix, merged, latestActivity));
        }

        Projects = projectViews;
        RunningAgents = _runService.GetRunning();
        HealthItems = _healthService.Check();

        var recentRuns = _runService.GetRecentRuns(30);
        RecentActivities = ActivityFeed.Build(recentRuns, take: 6);
    }

    public static string FormatRelativeTime(DateTime time)
    {
        var diff = DateTime.Now - time;
        if (diff.TotalMinutes < 1)
        {
            return "Vừa xong";
        }
        if (diff.TotalHours < 1)
        {
            return $"{(int)diff.TotalMinutes} phút trước";
        }
        if (time.Date == DateTime.Today)
        {
            return time.ToString("HH:mm");
        }
        return time.ToString("dd/MM HH:mm");
    }

    public static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1)
        {
            return $"{Math.Max(1, (int)elapsed.TotalSeconds)} giây";
        }
        if (elapsed.TotalHours < 1)
        {
            return $"{(int)elapsed.TotalMinutes} phút";
        }
        return $"{(int)elapsed.TotalHours} giờ {(int)elapsed.Minutes} phút";
    }
}
