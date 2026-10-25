using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace AITeam.Hub.Tests.Support;

/// <summary>Thực thi Razor đã biên dịch; trả HTML nguyên bản, không decode/chuẩn hóa markup.</summary>
public sealed class HubPageRenderer
{
    /// <summary>Nhận provider HubTestHost; không tạo cấu hình/HubPaths khác. Null services: ArgumentNullException.</summary>
    public HubPageRenderer(IServiceProvider services)
    { throw new NotImplementedException(); // TODO(agent)
    }
    /// <summary>
    /// Mỗi lần tạo scope, HttpContext/ActionContext riêng; tìm qua ICompositeViewEngine, isMainPage=false.
    /// Nhận tên _OfficeControl hoặc /Pages/Shared/_OfficeControl.cshtml, dùng Razor trong assembly Hub, không đọc repo.
    /// Sao chép ViewData sang dictionary mới, giữ entry và ModelState; Model bằng model truyền vào (kể cả null).
    /// Null viewData hợp lệ. ViewContext có TempData và RouteData.Values["page"]="/Office" cho Form TagHelper.
    /// Trả HTML nguyên bản (giữ encode/token). Luôn dispose scope/writer; ném lại lỗi gốc khi view lỗi.
    /// Tên null/rỗng/trắng: ArgumentException("Cần tên trang hoặc partial.").
    /// Không thấy view: InvalidOperationException("Không tìm thấy trang hoặc partial: {viewName}.").
    /// </summary>
    public Task<string> RenderAsync(string viewName, object? model = null, ViewDataDictionary? viewData = null)
    { throw new NotImplementedException(); // TODO(agent)
    }
}
