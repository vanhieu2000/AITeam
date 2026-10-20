using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public class ProjectModel : PageModel
{
    private readonly ITaskBoardService _boardService;
    private readonly ITeamConfigService _configService;

    public ProjectModel(ITaskBoardService boardService, ITeamConfigService configService)
    {
        _boardService = boardService;
        _configService = configService;
    }

    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "kanban";

    public ProjectBoard? Board { get; private set; }

    public IReadOnlyList<TaskCard> AssignedTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Assigned).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> ReviewTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Review).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> NeedsFixTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.NeedsFix).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IReadOnlyList<TaskCard> MergedTasks =>
        Board?.Tasks.Where(t => t.State == TaskState.Merged).ToList() ?? (IReadOnlyList<TaskCard>)Array.Empty<TaskCard>();

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            Board = _boardService.GetBoard(Name);
        }
        catch
        {
            return NotFound();
        }

        return Page();
    }
}
