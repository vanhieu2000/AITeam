using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Giao việc" (/Requests) — R-003 / H3-05. Bố cục: tasks/H3-05-hub-requests-task-tabs.md.</summary>
[ValidateAntiForgeryToken]
public class RequestsModel : PageModel
{
    public const int MaxText = 4000;
    public const string NewProject = "Dự án mới";

    private readonly IRequestService _requests;
    private readonly IInboxService _inbox;
    private readonly ITeamConfigService _config;

    public RequestsModel(IRequestService requests, IInboxService inbox, ITeamConfigService config)
    {
        _requests = requests;
        _inbox = inbox;
        _config = config;
    }

    /// <summary>?id=R-003 chọn luồng.</summary>
    [BindProperty(SupportsGet = true)] public string? Id { get; set; }

    public IReadOnlyList<RequestThread> Threads { get; private set; } = Array.Empty<RequestThread>();

    /// <summary>Luồng đang xem: theo Id nếu có trong Threads, ngược lại luồng đầu tiên (mới nhất); không có luồng ⇒ null.</summary>
    public RequestThread? Selected { get; private set; }

    /// <summary>Yêu cầu trong inbox.md chưa được Claude nhận (Done == false), mới nhất trước (theo Time, rồi LineNumber giảm dần).</summary>
    public IReadOnlyList<InboxItem> Waiting { get; private set; } = Array.Empty<InboxItem>();

    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = Array.Empty<ProjectInfo>();

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    /// <summary>Nạp Threads, Selected, Waiting, Projects. Lỗi đọc phiếu / inbox ⇒ ErrorMessage (không ném).</summary>
    public void OnGet()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Gửi yêu cầu mới vào inbox (Claude nhận ở đầu phiên làm việc). Kiểm tra:
    /// - text rỗng / toàn khoảng trắng ⇒ ErrorMessage "Nội dung yêu cầu không được để trống.";
    /// - text dài hơn MaxText ⇒ ErrorMessage "Nội dung quá dài (tối đa 4000 ký tự).";
    /// - project phải là tên một dự án trong config.GetProjects() hoặc NewProject; khác ⇒ ErrorMessage "Dự án không hợp lệ.".
    /// (InboxService.Add tự gộp nhiều dòng thành 1 dòng.)
    /// Hợp lệ ⇒ inbox.Add(project, text.Trim()), Message = "Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.".
    /// Luôn RedirectToPage() (PRG), không mang Id.
    /// </summary>
    public IActionResult OnPostSend(string project, string text)
        => throw new NotImplementedException(); // TODO(agent)
}
