using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public class TaskModel : PageModel
{
    private readonly ITaskBoardService _boardService;
    private readonly ITeamConfigService _configService;
    private readonly IAgentRunService? _runService;

    public TaskModel(ITaskBoardService boardService, ITeamConfigService configService, IAgentRunService? runService = null)
    {
        _boardService = boardService;
        _configService = configService;
        _runService = runService;
    }

    [BindProperty(SupportsGet = true)]
    public string Name { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public TaskDetail? Detail { get; private set; }
    public ProjectInfo? Project { get; private set; }

    public string Priority { get; private set; } = "bình thường";
    public string Design { get; private set; } = string.Empty;
    public bool? LastVerificationPassed { get; private set; }

    public bool IsAgentRunning =>
        Detail?.Card.LastRun?.State == RunState.Running ||
        Detail?.Runs.FirstOrDefault()?.State == RunState.Running;

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

        ExtractMeta();

        return Page();
    }

    private void ExtractMeta()
    {
        if (Detail == null) return;

        // Trích xuất ưu tiên và thiết kế từ các section nếu có
        foreach (var sec in Detail.Sections)
        {
            var lines = sec.Markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("Ưu tiên", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(trimmed, @"Ưu tiên[:\*]*\s*([^\s\*]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        Priority = match.Groups[1].Value.Trim();
                    }
                }

                if (trimmed.Contains("Thiết kế", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(Design))
                {
                    var colonIdx = trimmed.IndexOf(':');
                    if (colonIdx >= 0 && colonIdx < trimmed.Length - 1)
                    {
                        Design = trimmed[(colonIdx + 1)..].Trim();
                    }
                }
            }
        }

        // Kiểm tra lần chạy gần nhất xem có ĐẠT verify không
        var latestRun = Detail.Runs.FirstOrDefault();
        if (latestRun != null && latestRun.State == RunState.Success)
        {
            LastVerificationPassed = true;
        }
    }
}
