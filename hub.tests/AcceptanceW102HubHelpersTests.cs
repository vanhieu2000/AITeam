using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AITeam.Hub.Tests;

// Test nghiệm thu W1-02: bất biến khi giao agent; dùng Razor đã biên dịch thật, không đọc dữ liệu máy.
public sealed class AcceptanceW102HubHelpersTests
{
    [Fact]
    public async Task Get_real_page_and_partial_use_safe_paths_and_custom_di()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s => { s.AddSingleton(new Marker()); s.AddSingleton(new HubPaths(env.RootDir, env.RootDir)); });
        Assert.True(IPAddress.IsLoopback(IPAddress.Parse(host.Client.BaseAddress!.Host)));
        Assert.InRange(host.Client.BaseAddress.Port, 1, 65535);
        Assert.Equal(TimeSpan.FromSeconds(15), host.Client.Timeout);
        Assert.Same(env.Paths, host.Services.GetRequiredService<HubPaths>());
        Assert.NotNull(host.Services.GetRequiredService<Marker>());
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<HubPageRenderer>());
        using var page = await host.Client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("<!DOCTYPE html>", await page.Content.ReadAsStringAsync());
        using var partial = await host.Client.GetAsync("/?handler=Jobs");
        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        Assert.DoesNotContain("<!DOCTYPE html>", await partial.Content.ReadAsStringAsync());
        Assert.NotEmpty(await partial.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Missing_token_is_rejected_but_same_client_token_and_cookie_reach_handler()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var fields = new Dictionary<string, string> { ["command"] = "Invalid", ["target"] = "AITeam/T-001" };
        using var missing = await host.Client.PostAsync("/?handler=Command", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var html = await host.Client.GetStringAsync("/");
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        var token = Regex.Match(input, "value=\"([^\"]+)\"");
        Assert.True(token.Success, "Trang thật phải có token antiforgery.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        using var accepted = await host.Client.PostAsync("/?handler=Command", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Contains("id=\"office-control\"", await accepted.Content.ReadAsStringAsync());
        fields["__RequestVerificationToken"] = "token-sai";
        using var invalid = await host.Client.PostAsync("/?handler=Command", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Theory]
    [InlineData("_OfficeControl")]
    [InlineData("/Pages/Shared/_OfficeControl.cshtml")]
    public async Task Real_partial_preserves_viewdata_modelstate_and_encodes_xss(string name)
    {
        using var env = new TestEnvironment();
        const string attack = "<script>alert(1)</script>&\"";
        var commands = DispatchProxy.Create<ICommandService, BrokenCommands>();
        ((BrokenCommands)(object)commands).Message = attack;
        OfficeModel? model = null;
        ViewDataDictionary? original = null;
        var observed = 0;
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton<ICompositeViewEngine>(p => new InspectingEngine(p.GetRequiredService<IRazorViewEngine>(), context =>
            {
                observed++;
                Assert.NotSame(original, context.ViewData);
                Assert.Same(model, context.ViewData.Model);
                Assert.Equal("giá trị giữ lại", context.ViewData["Marker"]);
                Assert.Equal("Lỗi giữ lại", Assert.Single(context.ViewData.ModelState["field"]!.Errors).ErrorMessage);
            }));
        });
        model = new OfficeModel(host.Services.GetRequiredService<IOfficeService>(), commands);
        model.LoadControl();
        original = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = "model cũ" };
        original["Marker"] = "giá trị giữ lại";
        original.ModelState.AddModelError("field", "Lỗi giữ lại");
        var html = await new HubPageRenderer(host.Services).RenderAsync(name, model, original);
        Assert.True(observed > 0, "Phải thực thi view engine thật.");
        Assert.Contains("id=\"office-control\"", html);
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains(HtmlEncoder.Default.Encode(attack), html);
        Assert.DoesNotContain(attack, html);
        Assert.DoesNotContain("<!DOCTYPE html>", html);
        Assert.Equal("model cũ", original.Model);
        Assert.Equal("giá trị giữ lại", original["Marker"]);
    }

    [Fact]
    public async Task Null_viewdata_and_empty_control_lists_render_existing_partial()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var model = new OfficeModel(host.Services.GetRequiredService<IOfficeService>());
        var html = await new HubPageRenderer(host.Services).RenderAsync("_OfficeControl", model);
        Assert.Contains("id=\"office-control\"", html);
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("disabled", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task Blank_view_name_has_exact_error(string? name)
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => new HubPageRenderer(host.Services).RenderAsync(name!));
        Assert.Equal("Cần tên trang hoặc partial.", error.Message);
    }

    [Fact]
    public async Task Missing_view_reports_exact_name_and_host_stays_usable()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var renderer = new HubPageRenderer(host.Services);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync("_W102Missing"));
        Assert.Equal("Không tìm thấy trang hoặc partial: _W102Missing.", error.Message);
        Assert.Contains("<!DOCTYPE html>", await host.Client.GetStringAsync("/"));
    }

    [Fact]
    public async Task Parallel_hosts_get_different_live_ports_and_di_is_isolated()
    {
        using var first = new TestEnvironment();
        using var second = new TestEnvironment();
        // WithAsync bảo đảm host còn lại cũng được dọn nếu một nhánh lỗi.
        await Task.WhenAll(new[] { first, second }.Select(env => HubTestHost.WithAsync(env, async host =>
        {
            Assert.Same(env.Paths, host.Services.GetRequiredService<HubPaths>());
            using var response = await host.Client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var port = host.Client.BaseAddress!.Port;
            lock (portValues) portValues.Add(port);
            await barrier.SignalAndWaitAsync();
        })));
        Assert.Equal(2, portValues.Distinct().Count());
    }
    private readonly List<int> portValues = new();
    private readonly AsyncBarrier barrier = new();

    [Fact]
    public async Task Callback_failure_releases_listener_and_provider_without_deleting_environment()
    {
        using var env = new TestEnvironment();
        Uri? address = null;
        var probe = new LifetimeProbe();
        var expected = new IOException("lỗi callback thử nghiệm");
        var error = await Assert.ThrowsAsync<IOException>(() => HubTestHost.WithAsync(env, host =>
        {
            address = host.Client.BaseAddress;
            host.Services.GetRequiredService<LifetimeProbe>();
            throw expected;
        }, s => s.AddSingleton(_ => probe)));
        Assert.Same(expected, error);
        Assert.True(probe.Disposed);
        Assert.True(Directory.Exists(env.RootDir));
        using var socket = new TcpClient();
        await Assert.ThrowsAnyAsync<SocketException>(() => socket.ConnectAsync(address!.Host, address.Port).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Startup_failure_disposes_created_services_and_preserves_original_exception()
    {
        using var env = new TestEnvironment();
        var probe = new FailingStartup();
        var error = await Assert.ThrowsAsync<IOException>(() => HubTestHost.StartAsync(env,
            s => s.AddSingleton<IHostedService>(_ => probe)));
        Assert.Same(probe.Error, error);
        Assert.True(probe.Disposed);
    }

    [Fact]
    public async Task Dispose_is_repeatable_and_releases_listener_after_success()
    {
        using var env = new TestEnvironment();
        var probe = new LifetimeProbe();
        var host = await HubTestHost.StartAsync(env, s => s.AddSingleton(_ => probe));
        host.Services.GetRequiredService<LifetimeProbe>();
        var address = host.Client.BaseAddress!;
        await host.DisposeAsync();
        await host.DisposeAsync();
        Assert.True(probe.Disposed);
        Assert.True(Directory.Exists(env.RootDir));
        using var socket = new TcpClient();
        await Assert.ThrowsAnyAsync<SocketException>(() => socket.ConnectAsync(address.Host, address.Port).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Render_failure_disposes_its_scope_and_keeps_host_alive()
    {
        using var env = new TestEnvironment();
        var probe = new LifetimeProbe();
        var expected = new IOException("lỗi Razor thử nghiệm");
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddScoped(_ => probe);
            s.AddSingleton<ICompositeViewEngine>(p => new InspectingEngine(p.GetRequiredService<IRazorViewEngine>(), context =>
            {
                context.HttpContext.RequestServices.GetRequiredService<LifetimeProbe>();
                throw expected;
            }));
        });
        var model = new OfficeModel(host.Services.GetRequiredService<IOfficeService>());
        var error = await Assert.ThrowsAsync<IOException>(() => new HubPageRenderer(host.Services).RenderAsync("_OfficeControl", model));
        Assert.Same(expected, error);
        Assert.True(probe.Disposed);
        using var response = await host.Client.GetAsync("/not-a-page-w102");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Null_arguments_fail_before_starting_host()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => HubTestHost.StartAsync(null!));
        using var env = new TestEnvironment();
        await Assert.ThrowsAsync<ArgumentNullException>(() => HubTestHost.WithAsync(env, null!));
        Assert.Throws<ArgumentNullException>(() => new HubPageRenderer(null!));
    }
    private sealed class Marker;
    public class BrokenCommands : DispatchProxy
    {
        public string Message { get; set; } = "";
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new IOException(Message);
    }
    private sealed class LifetimeProbe : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
    private sealed class FailingStartup : IHostedService, IDisposable
    {
        public readonly IOException Error = new("lỗi khởi động thử nghiệm");
        public bool Disposed { get; private set; }
        public Task StartAsync(CancellationToken token) => Task.FromException(Error);
        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
        public void Dispose() => Disposed = true;
    }
    private sealed class AsyncBarrier
    {
        private int count;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task SignalAndWaitAsync()
        {
            if (Interlocked.Increment(ref count) == 2) ready.SetResult();
            return ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }
    private sealed class InspectingEngine(IRazorViewEngine inner, Action<ViewContext> inspect) : ICompositeViewEngine
    {
        public IReadOnlyList<IViewEngine> ViewEngines => new IViewEngine[] { inner };
        public ViewEngineResult FindView(ActionContext context, string name, bool main) => Wrap(inner.FindView(context, name, main));
        public ViewEngineResult GetView(string? path, string name, bool main) => Wrap(inner.GetView(path, name, main));
        private ViewEngineResult Wrap(ViewEngineResult result) => result.Success
            ? ViewEngineResult.Found(result.ViewName, new InspectingView(result.View, inspect)) : result;
    }
    private sealed class InspectingView(IView inner, Action<ViewContext> inspect) : IView
    {
        public string Path => inner.Path;
        public Task RenderAsync(ViewContext context) { inspect(context); return inner.RenderAsync(context); }
    }
}
