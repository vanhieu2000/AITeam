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

    public ProjectModel(ITaskBoardService boardService, ITeamConfigService configService, IAgentRunService? runService = null)
    {
        _boardService = boardService;
        _configService = configService;
        _runService = runService;
    }

    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "board";

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

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            Board = _boardService.GetBoard(Name);
            Runs = _runService != null ? _runService.GetRuns(Name) : Array.Empty<AgentRun>();

            var (preview, remaining) = BoardViews.MergedPreview(Board.Tasks, 7);
            MergedPreviewItems = preview;
            MergedRemaining = remaining;

            AllMergedOrdered = Board.Tasks
                .Where(t => t.State == TaskState.Merged)
                .OrderByDescending(t => ExtractNumber(t.Id))
                .ThenByDescending(t => t.Id)
                .ToList();
        }
        catch
        {
            return NotFound();
        }

        return Page();
    }
}
