using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Yêu cầu" (/Requests) — R-015: danh sách phiếu | chi tiết (tiến độ task, nhật ký, đóng / hủy phiếu).
/// Bố cục: docs/design/R-015-mockup.html màn Yêu cầu. Gửi yêu cầu mới vẫn vào inbox (H3-05).</summary>
[ValidateAntiForgeryToken]
public class RequestsModel : PageModel
{
    public const int MaxText = 4000;
    public const string NewProject = "Dự án mới";

    private readonly IRequestService _requests;
    private readonly IInboxService _inbox;
    private readonly ITeamConfigService _config;
    private readonly IRequestProgressService? _progress;

    public RequestsModel(IRequestService requests, IInboxService inbox, ITeamConfigService config, IRequestProgressService? progress = null)
    {
        _requests = requests;
        _inbox = inbox;
        _config = config;
        _progress = progress;
    }

    public static readonly IReadOnlyList<(string Key, string Label)> Filters = [("open", "Đang làm"), ("done", "Xong"), ("cancel", "Hủy"), ("all", "Tất cả")];

    /// <summary>?f=open|done|cancel|all (mặc định open; giá trị lạ ⇒ open).</summary>
    [BindProperty(SupportsGet = true)] public string? F { get; set; }

    public string Filter => Filters.Any(x => x.Key == F) ? F! : "open";

    /// <summary>Tiến độ từng phiếu theo Id (F1-02). Không có dịch vụ / lỗi ⇒ rỗng.</summary>
    public IReadOnlyDictionary<string, RequestProgress> Progress { get; private set; } = new Dictionary<string, RequestProgress>();

    public IReadOnlyList<RequestThread> Visible { get; private set; } = Array.Empty<RequestThread>();

    public RequestState StateOf(RequestThread t) => Progress.TryGetValue(t.Id, out var p) ? p.State
        : t.Status.Contains('✅') ? RequestState.Done
        : t.Status.Contains('❌') || t.Status.Contains("Hủy", StringComparison.OrdinalIgnoreCase) ? RequestState.Cancelled : RequestState.Open;

    public int CountIn(string filter) => filter switch
    {
        "done" => Threads.Count(t => StateOf(t) == RequestState.Done),
        "cancel" => Threads.Count(t => StateOf(t) == RequestState.Cancelled),
        "all" => Threads.Count,
        _ => Threads.Count(t => StateOf(t) == RequestState.Open),
    };

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
    {
        try
        {
            Projects = _config.GetProjects();
            Threads = _requests.GetAll();
            if (_progress != null)
            {
                try { Progress = _progress.GetAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { Progress = new Dictionary<string, RequestProgress>(); }
            }
            Visible = Filter switch
            {
                "done" => Threads.Where(t => StateOf(t) == RequestState.Done).ToList(),
                "cancel" => Threads.Where(t => StateOf(t) == RequestState.Cancelled).ToList(),
                "all" => Threads,
                _ => Threads.Where(t => StateOf(t) == RequestState.Open).ToList(),
            };
            Selected = (!string.IsNullOrEmpty(Id) && Regex.IsMatch(Id, @"^R-\d{3}$"))
                ? Threads.FirstOrDefault(t => string.Equals(t.Id, Id, StringComparison.OrdinalIgnoreCase)) ?? Visible.FirstOrDefault() ?? Threads.FirstOrDefault()
                : Visible.FirstOrDefault() ?? Threads.FirstOrDefault();
            Waiting = _inbox.GetItems().Where(i => !i.Done)
                .OrderByDescending(i => i.Time).ThenByDescending(i => i.LineNumber).ToList();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

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
    {
        if (string.IsNullOrWhiteSpace(text)) { ErrorMessage = "Nội dung yêu cầu không được để trống."; return RedirectToPage(); }
        if (text.Length > MaxText) { ErrorMessage = "Nội dung quá dài (tối đa 4000 ký tự)."; return RedirectToPage(); }
        var validProject = project == NewProject || _config.GetProjects().Any(p => string.Equals(p.Name, project, StringComparison.Ordinal));
        if (!validProject) { ErrorMessage = "Dự án không hợp lệ."; return RedirectToPage(); }
        try
        {
            _inbox.Add(project, text.Trim());
            Message = "Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        return RedirectToPage();
    }

    /// <summary>R-015: Đánh dấu xong / Hủy / Mở lại phiếu (F1-02 SetState). Luôn PRG về đúng phiếu.</summary>
    public IActionResult OnPostState(string id, string state, string? note)
    {
        var target = state switch { "done" => RequestState.Done, "cancel" => RequestState.Cancelled, "open" => RequestState.Open, _ => (RequestState?)null };
        if (_progress == null || target == null || string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^R-\d{3}$"))
        {
            ErrorMessage = "Yêu cầu đổi trạng thái không hợp lệ.";
            return RedirectToPage();
        }
        var r = _progress.SetState(id, target.Value, note);
        if (r.Ok) Message = target switch { RequestState.Done => $"Đã đánh dấu {id} xong.", RequestState.Cancelled => $"Đã hủy {id}.", _ => $"Đã mở lại {id}." };
        else ErrorMessage = r.Error ?? "Không đổi được trạng thái phiếu.";
        return RedirectToPage(new { id, f = target == RequestState.Open ? "open" : "all" });
    }
}
