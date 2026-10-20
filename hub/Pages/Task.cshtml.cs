using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public class TaskModel : PageModel
{
    private readonly ITaskBoardService _boardService;
    private readonly ITeamConfigService _configService;

    public TaskModel(ITaskBoardService boardService, ITeamConfigService configService)
    {
        _boardService = boardService;
        _configService = configService;
    }

    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public TaskDetail? Detail { get; private set; }
    public ProjectInfo? Project { get; private set; }

    public string BranchGitHubUrl
    {
        get
        {
            if (Project == null || Detail == null || string.IsNullOrEmpty(Detail.Card.Branch))
            {
                return string.Empty;
            }
            return $"https://github.com/{Project.Repo}/tree/{Detail.Card.Branch}";
        }
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Id))
        {
            return RedirectToPage("/Index");
        }

        Project = _configService.FindProject(Name);
        Detail = _boardService.GetTask(Name, Id);

        if (Detail == null)
        {
            return NotFound();
        }

        return Page();
    }
}
