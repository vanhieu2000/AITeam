using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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
    {
        var state = new ProviderStateStore(_paths.AITeamRoot);
        var now = Now();
        History = state.All().OrderByDescending(x => x.At).Take(10).ToList();

        try
        {
            var settings = ProviderSettings.Load(_paths.AITeamRoot);
            Rows = settings.Providers.Select(c =>
            {
                var rest = state.RestOf(c.Id, now);
                return new ProviderRow(
                    c.Id,
                    c.Kind.ToString(),
                    c.Enabled,
                    string.Join(", ", c.Roles),
                    string.IsNullOrWhiteSpace(c.Model) ? "mặc định" : c.Model,
                    c.MaxConcurrent,
                    rest?.Until,
                    rest?.Reason.ToString(),
                    rest?.Detail);
            }).ToList();

            Routing = settings.Routing
                .OrderBy(kv => kv.Key.ToString(), StringComparer.Ordinal)
                .Select(kv => (kv.Key.ToString(), string.Join(" → ", kv.Value)))
                .ToList();
        }
        catch (Exception ex)
        {
            Error = "Không đọc được cấu hình provider: " + ex.Message;
            Rows = Array.Empty<ProviderRow>();
            Routing = Array.Empty<(string, string)>();
        }
    }

    /// <summary>
    /// "Hết nghỉ": id phải là provider có trong cấu hình (không phân biệt hoa thường), ngược lại ErrorMessage "Provider không hợp lệ." (không đụng file).
    /// Clear(id) true ⇒ Message "Đã xóa trạng thái nghỉ của {id}."; false ⇒ Message "{id} không ở trạng thái nghỉ.". Lỗi đọc cấu hình ⇒ ErrorMessage. Luôn RedirectToPage().
    /// </summary>
    public IActionResult OnPostClear(string id)
    {
        try
        {
            var settings = ProviderSettings.Load(_paths.AITeamRoot);
            if (string.IsNullOrWhiteSpace(id) || !settings.Providers.Any(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Provider không hợp lệ.";
                return RedirectToPage();
            }

            var cleared = new ProviderStateStore(_paths.AITeamRoot).Clear(id);
            Message = cleared ? $"Đã xóa trạng thái nghỉ của {id}." : $"{id} không ở trạng thái nghỉ.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không đọc được cấu hình provider: " + ex.Message;
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Nút "Kiểm tra" (HTMX GET ?handler=Health&amp;id=…): id không có trong cấu hình ⇒ view (id, "LỖI", "Provider không hợp lệ.").
    /// Ngược lại CheckAsync với giới hạn 20 giây (CancellationTokenSource); quá giờ / ném lỗi ⇒ (id, "LỖI", message).
    /// Trả PartialViewResult { ViewName = "_ProviderHealthPartial", ViewData = new ViewDataDictionary&lt;ProviderHealthView&gt;(new EmptyModelMetadataProvider(), ModelState) { Model = view } }
    /// (dựng tay như IndexModel.OnGetRunningAgents — KHÔNG dùng Partial() vì cần PageContext) với Tag = TagOf(health), Detail = dòng đầu của health.Detail.
    /// </summary>
    public async Task<IActionResult> OnGetHealthAsync(string id)
    {
        id ??= "";
        ProviderHealthView view;
        try
        {
            var settings = ProviderSettings.Load(_paths.AITeamRoot);
            var cfg = settings.Providers.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (cfg == null)
            {
                view = new ProviderHealthView(id, "LỖI", "Provider không hợp lệ.");
            }
            else
            {
                var provider = _factory != null ? _factory(cfg) : settings.Create(cfg, new ProcessRunner());
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var health = await provider.CheckAsync(cts.Token);
                var firstLine = (health.Detail ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                view = new ProviderHealthView(id, TagOf(health), firstLine);
            }
        }
        catch (Exception ex)
        {
            view = new ProviderHealthView(id, "LỖI", ex.Message);
        }

        return new PartialViewResult
        {
            ViewName = "_ProviderHealthPartial",
            ViewData = new ViewDataDictionary<ProviderHealthView>(new EmptyModelMetadataProvider(), ModelState) { Model = view }
        };
    }

    /// <summary>Giống `aiteam providers`: !Installed ⇒ "CHƯA CÀI"; LoggedIn true ⇒ "OK"; false ⇒ "CHƯA ĐĂNG NHẬP"; null ⇒ "KHÔNG RÕ".</summary>
    public static string TagOf(ProviderHealth h)
        => !h.Installed ? "CHƯA CÀI" : h.LoggedIn switch { true => "OK", false => "CHƯA ĐĂNG NHẬP", null => "KHÔNG RÕ" };
}
