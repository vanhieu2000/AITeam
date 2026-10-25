using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
namespace AITeam.Hub.Tests.Support;

/// <summary>Thực thi Razor đã biên dịch; trả HTML nguyên bản, không decode/chuẩn hóa markup.</summary>
public sealed class HubPageRenderer
{
    private readonly IServiceProvider _services;
    /// <summary>Nhận provider HubTestHost; không tạo cấu hình/HubPaths khác. Null services: ArgumentNullException.</summary>
    public HubPageRenderer(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;

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
    public async Task<string> RenderAsync(string viewName, object? model = null, ViewDataDictionary? viewData = null)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("Cần tên trang hoặc partial.");

        await using var scope = _services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test"));
        var routeData = new RouteData();
        routeData.Values["page"] = "/Office";
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

        var engine = sp.GetRequiredService<ICompositeViewEngine>();
        var result = engine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
        if (!result.Success)
            result = engine.FindView(actionContext, viewName, isMainPage: false);
        if (!result.Success || result.View == null)
            throw new InvalidOperationException($"Không tìm thấy trang hoặc partial: {viewName}.");

        var metadataProvider = sp.GetService<IModelMetadataProvider>() ?? new EmptyModelMetadataProvider();
        var modelState = viewData != null ? new ModelStateDictionary(viewData.ModelState) : new ModelStateDictionary();
        var vd = new ViewDataDictionary(metadataProvider, modelState);
        if (viewData != null)
        {
            foreach (var item in viewData)
                vd[item.Key] = item.Value;
        }
        vd.Model = model;

        var tempData = new TempDataDictionary(httpContext, sp.GetRequiredService<ITempDataProvider>());
        await using var writer = new StringWriter();
        var viewContext = new ViewContext(actionContext, result.View, vd, tempData, writer, new HtmlHelperOptions());
        await result.View.RenderAsync(viewContext);
        return writer.ToString();

    }
}
