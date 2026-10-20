using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

[ValidateAntiForgeryToken]
public class InboxModel : PageModel
{
    private readonly IInboxService _inboxService;
    private readonly ITeamConfigService _configService;
    private readonly IAntiforgery? _antiforgery;

    public InboxModel(IInboxService inboxService, ITeamConfigService configService, IAntiforgery? antiforgery = null)
    {
        _inboxService = inboxService;
        _configService = configService;
        _antiforgery = antiforgery;
    }

    [BindProperty]
    public string Project { get; set; } = "";

    [BindProperty]
    public string Text { get; set; } = "";

    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = Array.Empty<ProjectInfo>();
    public IReadOnlyList<InboxItem> PendingItems { get; private set; } = Array.Empty<InboxItem>();
    public IReadOnlyList<InboxItem> DoneItems { get; private set; } = Array.Empty<InboxItem>();

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_antiforgery != null && HttpContext != null)
        {
            var isValid = await _antiforgery.IsRequestValidAsync(HttpContext);
            if (!isValid)
            {
                return BadRequest("Antiforgery token không hợp lệ.");
            }
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            ModelState.AddModelError(nameof(Text), "Nội dung yêu cầu không được để trống.");
            ErrorMessage = "Vui lòng nhập nội dung yêu cầu.";
            LoadData();
            return Page();
        }

        string targetProject = string.IsNullOrWhiteSpace(Project) ? "Dự án mới" : Project.Trim();
        _inboxService.Add(targetProject, Text.Trim());

        Message = "Đã gửi yêu cầu vào hộp thư thành công.";
        return RedirectToPage();
    }

    private void LoadData()
    {
        Projects = _configService.GetProjects();
        var allItems = _inboxService.GetItems();
        PendingItems = allItems.Where(i => !i.Done)
                               .OrderByDescending(i => i.Time)
                               .ThenByDescending(i => i.LineNumber)
                               .ToList();
        DoneItems = allItems.Where(i => i.Done)
                            .OrderByDescending(i => i.Time)
                            .ThenByDescending(i => i.LineNumber)
                            .ToList();
    }
}
