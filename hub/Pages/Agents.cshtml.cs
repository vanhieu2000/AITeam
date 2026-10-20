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
    private readonly IAntiforgery? _antiforgery;

    public AgentsModel(IAgentRunService runService, IAntiforgery? antiforgery = null)
    {
        _runService = runService;
        _antiforgery = antiforgery;
    }

    public IReadOnlyList<RunningAgent> RunningAgents { get; private set; } = Array.Empty<RunningAgent>();
    public IReadOnlyList<AgentRun> RecentRuns { get; private set; } = Array.Empty<AgentRun>();

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        RunningAgents = _runService.GetRunning();
        RecentRuns = _runService.GetRecentRuns(20);
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
}
