using AITeam.Core.Accounts;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Tài khoản AI" (/AiAccounts) — R-005 / V5-06.</summary>
public class AiAccountsModel : PageModel
{
    private readonly IAiAccountsService _accounts;

    public AiAccountsModel(IAiAccountsService accounts) => _accounts = accounts;

    public IReadOnlyList<AiAccountView> Rows { get; private set; } = Array.Empty<AiAccountView>();

    /// <summary>Lỗi đọc ⇒ "Không đọc được danh sách tài khoản AI: " + ex.Message; Rows rỗng.</summary>
    public string? Error { get; private set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Nút "Kiểm tra" (HTMX GET ?handler=Check&amp;id=…): Id lạ (ArgumentException) ⇒ NotFound().
    /// Ngược lại PartialViewResult "_AiAccountRow" với model AiAccountView (dựng tay ViewDataDictionary&lt;AiAccountView&gt;(new EmptyModelMetadataProvider(), ModelState) { Model = row }).
    /// </summary>
    public Task<IActionResult> OnGetCheckAsync(string? id)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// POST ?handler=Save (form có antiforgery): _accounts.Save(id, alias, mode) ⇒ Message "Đã lưu {Name}: {Mode}."
    /// (Mode chữ: Disabled "Tắt", Manual "Thủ công", Automatic "Tự động" — xem ModeText); ArgumentException / InvalidOperationException ⇒ ErrorMessage = ex.Message.
    /// Luôn RedirectToPage().
    /// </summary>
    public IActionResult OnPostSave(string? id, string? alias, string? mode)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Nút "Ngắt": giữ alias hiện tại (từ List()), đổi mode Disabled ⇒ Message "Đã ngắt {Name} (không xóa gì).". Lỗi như OnPostSave. RedirectToPage().</summary>
    public IActionResult OnPostDisable(string? id)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Disabled "Tắt", Manual "Thủ công", Automatic "Tự động".</summary>
    public static string ModeText(AiMode m)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Lớp pill: Connected "ok"; ManualOnly "n"; Busy "run"; RateLimited, LoginRequired "rev"; Error "err"; Unsupported, Disconnected "n".</summary>
    public static string PillClass(AiStatus s)
        => throw new NotImplementedException(); // TODO(agent)
}
