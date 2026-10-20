using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

public class IndexModel : PageModel
{
    private readonly ITeamConfigService _configService;
    private readonly ITaskBoardService _boardService;
    private readonly IAgentRunService _runService;
    private readonly IHealthService _healthService;

    public IndexModel(
        ITeamConfigService configService,
        ITaskBoardService boardService,
        IAgentRunService runService,
        IHealthService healthService)
    {
        _configService = configService;
        _boardService = boardService;
        _runService = runService;
        _healthService = healthService;
    }

    public record ProjectCardView(
        ProjectInfo Info,
        int AssignedCount,
        int ReviewCount,
        int NeedsFixCount,
        int MergedCount);

    public IReadOnlyList<ProjectCardView> Projects { get; private set; } = Array.Empty<ProjectCardView>();
    public IReadOnlyList<RunningAgent> RunningAgents { get; private set; } = Array.Empty<RunningAgent>();
    public IReadOnlyList<HealthItem> HealthItems { get; private set; } = Array.Empty<HealthItem>();

    public int TotalProjects => Projects.Count;
    public int TotalAssigned => Projects.Sum(p => p.AssignedCount);
    public int TotalReview => Projects.Sum(p => p.ReviewCount);
    public int TotalRunningAgents => RunningAgents.Count;

    public void OnGet()
    {
        LoadData();
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

            projectViews.Add(new ProjectCardView(p, assigned, review, needsFix, merged));
        }

        Projects = projectViews;
        RunningAgents = _runService.GetRunning();
        HealthItems = _healthService.Check();
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
