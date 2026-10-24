using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Văn phòng" (/Office; Claude chuyển thành trang chủ khi merge) — R-005 / V5-04.</summary>
public class OfficeModel : PageModel
{
    private readonly IOfficeService _office;

    public OfficeModel(IOfficeService office) => _office = office;

    public OfficeView? View { get; private set; }

    /// <summary>Lỗi khi đọc dữ liệu ⇒ "Không đọc được dữ liệu văn phòng: " + ex.Message; View null.</summary>
    public string? Error { get; private set; }

    /// <summary>Nạp View (bắt lỗi ⇒ Error).</summary>
    public void OnGet()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// HTMX tự làm mới (GET ?handler=Desks mỗi 10 giây): nạp như OnGet rồi trả PartialViewResult "_OfficeDesks"
    /// với model OfficeModel (chính trang này) — dựng tay ViewDataDictionary&lt;OfficeModel&gt;(new EmptyModelMetadataProvider(), ModelState) { Model = this }.
    /// </summary>
    public IActionResult OnGetDesks()
        => throw new NotImplementedException(); // TODO(agent)
}
