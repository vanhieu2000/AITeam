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
        if (string.IsNullOrWhiteSpace(viewName)) throw new ArgumentException("Cần tên trang hoặc partial.");
        var scope = _services.CreateAsyncScope();
        StringWriter? writer = null;
        try
        {
            var sp = scope.ServiceProvider;
            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test"));
            var routeData = new RouteData();
            routeData.Values["page"] = "/Office";
            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var engine = sp.GetRequiredService<ICompositeViewEngine>();
            var result = engine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
            if (!result.Success) result = engine.FindView(actionContext, viewName, isMainPage: false);
            if (!result.Success || result.View == null) throw new InvalidOperationException($"Không tìm thấy trang hoặc partial: {viewName}.");
            var metadataProvider = sp.GetService<IModelMetadataProvider>() ?? new EmptyModelMetadataProvider();
            var modelState = viewData != null ? new ModelStateDictionary(viewData.ModelState) : new ModelStateDictionary();
            var vd = new ViewDataDictionary(metadataProvider, modelState);
            if (viewData != null) foreach (var item in viewData) vd[item.Key] = item.Value;
            vd.Model = model;
            var tempData = new TempDataDictionary(httpContext, sp.GetRequiredService<ITempDataProvider>());
            writer = new StringWriter();
            var viewContext = new ViewContext(actionContext, result.View, vd, tempData, writer, new HtmlHelperOptions());
            await result.View.RenderAsync(viewContext);
            return writer.ToString();
        }
        finally
        {
            await HubTestHost.SafeDisposeAsync(writer);
            await HubTestHost.SafeDisposeAsync(scope.ServiceProvider);
            try { await scope.DisposeAsync(); } catch { }
        }
    }
}

public sealed class HubHelpersCleanupRegressionTests
{
    [Fact]
    public async Task Cleanup_continues_after_dispose_error_supports_async_and_preserves_original_exception()
    {
        using var env = new TestEnvironment();
        var (cbAsync, cbSync, cbExpected) = (new AsyncProbe(), new SyncProbe(), new IOException("lỗi callback"));
        var cbEx = await Assert.ThrowsAsync<IOException>(() => HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton(cbAsync); s.AddSingleton<IDisposable>(new ThrowingProbe()); s.AddSingleton(cbSync); throw cbExpected;
        }));
        Assert.Same(cbExpected, cbEx);
        Assert.True(cbAsync.Disposed && cbSync.Disposed);

        var (stAsync, stSync, stExpected) = (new AsyncProbe(), new SyncProbe(), new IOException("lỗi startup"));
        var stEx = await Assert.ThrowsAsync<IOException>(() => HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton(_ => stAsync); s.AddSingleton(_ => new ThrowingProbe()); s.AddSingleton(_ => stSync);
            s.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => { sp.GetRequiredService<AsyncProbe>(); sp.GetRequiredService<ThrowingProbe>(); sp.GetRequiredService<SyncProbe>(); throw stExpected; });
        }));
        Assert.Same(stExpected, stEx);
        Assert.True(stAsync.Disposed && stSync.Disposed);

        var (rdAsync, rdSync, rdExpected) = (new AsyncProbe(), new SyncProbe(), new IOException("lỗi render"));
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddScoped(_ => rdAsync); s.AddScoped(_ => new ThrowingProbe()); s.AddScoped(_ => rdSync);
            s.AddScoped<ICompositeViewEngine>(sp => { sp.GetRequiredService<AsyncProbe>(); sp.GetRequiredService<ThrowingProbe>(); sp.GetRequiredService<SyncProbe>(); throw rdExpected; });
        });
        var rdEx = await Assert.ThrowsAsync<IOException>(() => new HubPageRenderer(host.Services).RenderAsync("_OfficeControl"));
        Assert.Same(rdExpected, rdEx);
        Assert.True(rdAsync.Disposed && rdSync.Disposed);
    }
    private sealed class AsyncProbe : IAsyncDisposable { public bool Disposed; public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; } }
    private sealed class SyncProbe : IDisposable { public bool Disposed; public void Dispose() => Disposed = true; }
    private sealed class ThrowingProbe : IDisposable, IAsyncDisposable { public void Dispose() => throw new InvalidOperationException("lỗi dispose"); public ValueTask DisposeAsync() => ValueTask.FromException(new InvalidOperationException("lỗi dispose async")); }
}