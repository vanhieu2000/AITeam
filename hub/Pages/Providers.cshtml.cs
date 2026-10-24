using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Một dòng bảng Provider.</summary>
public sealed record ProviderRow(
    string Id,
    string Kind,              // "Antigravity" / "Codex" / "ClaudeCli"
    bool Enabled,
    string Roles,             // "Reviewer, Developer" (thứ tự trong cấu hình, nối ", ")
    string Model,             // null / rỗng ⇒ "mặc định"
    int MaxConcurrent,
    DateTime? RestUntil,      // chỉ khi đang nghỉ (Until > now)
    string? RestReason,       // "QuotaExhausted" / "NotLoggedIn"
    string? RestDetail);

/// <summary>Kết quả nút "Kiểm tra" (HTMX). Tag: "OK" · "CHƯA CÀI" · "CHƯA ĐĂNG NHẬP" · "KHÔNG RÕ" · "LỖI".</summary>
public sealed record ProviderHealthView(string Id, string Tag, string Detail);

/// <summary>Trang "Provider" (/Providers) — R-004 / H3-06. Bố cục: tasks/H3-06-hub-providers.md.</summary>
[ValidateAntiForgeryToken]
public class ProvidersModel : PageModel
{
    private readonly HubPaths _paths;
    private readonly Func<ProviderConfig, IAgentProvider>? _factory;

    /// <summary><paramref name="factory"/> chỉ để test; null ⇒ settings.Create(c, new ProcessRunner()).</summary>
    public ProvidersModel(HubPaths paths, Func<ProviderConfig, IAgentProvider>? factory = null)
    {
        _paths = paths;
        _factory = factory;
    }

    public IReadOnlyList<ProviderRow> Rows { get; private set; } = Array.Empty<ProviderRow>();

    /// <summary>Mỗi vai trò có trong Routing: ("Developer", "agy → codex"), sắp theo tên vai trò.</summary>
    public IReadOnlyList<(string Role, string Chain)> Routing { get; private set; } = Array.Empty<(string, string)>();

    /// <summary>Mọi bản ghi trong .state/providers.json (kể cả đã hết hạn), At mới nhất trước, tối đa 10.</summary>
    public IReadOnlyList<ProviderRest> History { get; private set; } = Array.Empty<ProviderRest>();

    /// <summary>Không đọc được cấu hình (team.json hỏng / routing trỏ provider không có) ⇒ "Không đọc được cấu hình provider: " + ex.Message.</summary>
    public string? Error { get; private set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    /// <summary>Giờ hiện tại — test thay được.</summary>
    internal Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <summary>ProviderSettings.Load(_paths.AITeamRoot) + new ProviderStateStore(root). Lỗi cấu hình ⇒ Error, các danh sách rỗng (History vẫn đọc).</summary>
    public void OnGet()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// "Hết nghỉ": id phải là provider có trong cấu hình (không phân biệt hoa thường), ngược lại ErrorMessage "Provider không hợp lệ." (không đụng file).
    /// Clear(id) true ⇒ Message "Đã xóa trạng thái nghỉ của {id}."; false ⇒ Message "{id} không ở trạng thái nghỉ.". Lỗi đọc cấu hình ⇒ ErrorMessage. Luôn RedirectToPage().
    /// </summary>
    public IActionResult OnPostClear(string id)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Nút "Kiểm tra" (HTMX GET ?handler=Health&amp;id=…): id không có trong cấu hình ⇒ view (id, "LỖI", "Provider không hợp lệ.").
    /// Ngược lại CheckAsync với giới hạn 20 giây (CancellationTokenSource); quá giờ / ném lỗi ⇒ (id, "LỖI", message).
    /// Trả PartialViewResult { ViewName = "_ProviderHealthPartial", ViewData = new ViewDataDictionary&lt;ProviderHealthView&gt;(new EmptyModelMetadataProvider(), ModelState) { Model = view } }
    /// (dựng tay như IndexModel.OnGetRunningAgents — KHÔNG dùng Partial() vì cần PageContext) với Tag = TagOf(health), Detail = dòng đầu của health.Detail.
    /// </summary>
    public Task<IActionResult> OnGetHealthAsync(string id)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Giống `aiteam providers`: !Installed ⇒ "CHƯA CÀI"; LoggedIn true ⇒ "OK"; false ⇒ "CHƯA ĐĂNG NHẬP"; null ⇒ "KHÔNG RÕ".</summary>
    public static string TagOf(ProviderHealth h)
        => throw new NotImplementedException(); // TODO(agent)
}
