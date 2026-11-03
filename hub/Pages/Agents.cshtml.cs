using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

[ValidateAntiForgeryToken]
public class AgentsModel : PageModel
{
    private readonly IAgentRunService _runService;
    private readonly ITeamConfigService? _configService;
    private readonly IAntiforgery? _antiforgery;

    public AgentsModel(IAgentRunService runService, IAntiforgery? antiforgery = null)
        : this(runService, null, antiforgery)
    {
    }

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public AgentsModel(IAgentRunService runService, ITeamConfigService? configService, IAntiforgery? antiforgery = null)
    {
        _runService = runService;
        _configService = configService;
        _antiforgery = antiforgery;
    }

    public IReadOnlyList<RunningAgent> RunningAgents { get; private set; } = Array.Empty<RunningAgent>();
    public IReadOnlyList<AgentRun> RecentRuns { get; private set; } = Array.Empty<AgentRun>();
    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = Array.Empty<ProjectInfo>();
    public RunPage RunPage { get; private set; } = new(Array.Empty<AgentRun>(), 0, 1, 1);
    public RunFilter Filter { get; private set; } = RunFilter.All;
    public string? SelectedProject { get; private set; }

    public string PaginationSummary
    {
        get
        {
            if (RunPage.Total == 0) return "0 / 0";
            int from = (RunPage.Page - 1) * RunPage.PageSize + 1;
            int to = Math.Min(from + RunPage.Items.Count - 1, RunPage.Total);
            return $"{from}–{to} / {RunPage.Total}";
        }
    }

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet(RunFilter filter = RunFilter.All, string? project = null, int page = 1)
    {
        Filter = filter;
        SelectedProject = string.IsNullOrWhiteSpace(project) ? null : project.Trim();
        LoadData(page);
    }

    public IActionResult OnGetRunningTable()
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
            ViewName = "_AgentsRunningTablePartial",
            ViewData = viewData
        };
    }

    public IActionResult OnGetRunDrawer(string project, string task, int attempt)
    {
        var runs = _runService.GetRuns(project, task);
        var run = runs.FirstOrDefault(r => r.Attempt == attempt) ?? runs.FirstOrDefault();
        var viewData = new ViewDataDictionary<AgentRun?>(new EmptyModelMetadataProvider(), ModelState)
        {
            Model = run
        };
        return new PartialViewResult
        {
            ViewName = "_RunDrawerPartial",
            ViewData = viewData
        };
    }

    public async Task<IActionResult> OnPostStopAsync(int pid)
    {
        if (_antiforgery != null && HttpContext != null)
        {
            var isValid = await _antiforgery.IsRequestValidAsync(HttpContext);
            if (!isValid)
            {
                return BadRequest("Antiforgery token không hợp lệ.");
            }
        }

        bool stopped = _runService.Stop(pid);
        if (stopped)
        {
            Message = $"Đã dừng tiến trình agent (PID: {pid}) thành công.";
        }
        else
        {
            ErrorMessage = $"Không thể dừng tiến trình PID: {pid}. Tiến trình có thể đã kết thúc hoặc không phải agy.exe.";
        }

        return RedirectToPage();
    }

    private void LoadData(int page)
    {
        RunningAgents = _runService.GetRunning();
        RecentRuns = _runService.GetRecentRuns(20);

        Projects = _configService?.GetProjects() ?? Array.Empty<ProjectInfo>();

        var allRuns = new List<AgentRun>();
        if (!string.IsNullOrWhiteSpace(SelectedProject))
        {
            allRuns.AddRange(_runService.GetRuns(SelectedProject));
        }
        else if (Projects.Count > 0)
        {
            foreach (var p in Projects)
            {
                allRuns.AddRange(_runService.GetRuns(p.Name));
            }
        }
        else
        {
            allRuns.AddRange(_runService.GetRecentRuns(100));
        }

        RunPage = RunHistory.Page(allRuns, Filter, SelectedProject, page, pageSize: 8);
    }

    public static string FormatTokens(long? tokens)
    {
        if (!tokens.HasValue) return "—";
        if (tokens.Value >= 1_000_000)
        {
            double millions = tokens.Value / 1_000_000.0;
            return millions.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " tr";
        }
        if (tokens.Value >= 1_000)
        {
            return (tokens.Value / 1_000.0).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " k";
        }
        return tokens.Value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
    }

    public static string FormatDuration(double? seconds)
    {
        if (!seconds.HasValue) return "—";
        int totalSec = (int)Math.Round(seconds.Value);
        int minutes = totalSec / 60;
        if (minutes > 0)
        {
            return $"{minutes}′";
        }
        return $"{totalSec}s";
    }
}
