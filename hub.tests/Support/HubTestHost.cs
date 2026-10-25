using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;
using AITeam.Hub.DevData;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
namespace AITeam.Hub.Tests.Support;

/// <summary>Host Razor thật cho test; không sở hữu TestEnvironment của bên gọi.</summary>
public sealed class HubTestHost : IAsyncDisposable
{
    /// <summary>Client giữ cookie, không tự chuyển hướng, timeout 15 giây; BaseAddress là loopback cổng đã bind.</summary>
    public HttpClient Client { get; }
    /// <summary>Provider chứa environment.Paths, fake an toàn và HubPageRenderer đăng ký scoped.</summary>
    public IServiceProvider Services => _app.Services;
    /// <summary>
    /// Dựng Razor từ assembly Hub như AntiforgerySecurityTests; FakeServices.Register trước configureServices.
    /// Không chạy Program Hub/đọc cấu hình thật; content root=environment.RootDir; không lưu DataProtection vào profile thật.
    /// Khóa HubPaths=environment.Paths sau callback; chỉ Listen IPAddress.Loopback cổng 0, không dò cổng trước.
    /// Giữ antiforgery. Lỗi khởi động/callback: dispose mọi tài nguyên đã tạo rồi ném lại lỗi gốc.
    /// Null environment: ArgumentNullException. Không dispose environment.
    /// </summary>
    public static async Task<HubTestHost> StartAsync(TestEnvironment environment, Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var hubAssembly = typeof(OfficeModel).Assembly;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = hubAssembly.GetName().Name, ContentRootPath = environment.RootDir });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(environment.Paths);
        builder.Services.AddRazorPages().AddApplicationPart(hubAssembly);
        builder.Services.AddAntiforgery();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddScoped<HubPageRenderer>();
        FakeServices.Register(builder.Services);
        WebApplication? app = null;
        HttpClient? client = null;
        HttpClientHandler? handler = null;
        try
        {
            configureServices?.Invoke(builder.Services);
            builder.Services.RemoveAll<HubPaths>();
            builder.Services.AddSingleton(environment.Paths);
            builder.Services.TryAddScoped<HubPageRenderer>();
            app = builder.Build();
            app.UseAntiforgery();
            app.MapRazorPages();
            await app.StartAsync();
            var address = app.Urls.FirstOrDefault() ?? app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer(), AllowAutoRedirect = false };
            client = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            return new HubTestHost(app, client);
        }
        catch
        {
            await SafeDisposeAsync(client);
            await SafeDisposeAsync(handler);
            if (app != null)
            {
                try { await app.StopAsync(); } catch { }
                await SafeDisposeAsync(app.Services, builder.Services);
                try { await app.DisposeAsync(); } catch { }
            }
            else await SafeDisposeAsync(null, builder.Services);
            throw;
        }
    }
    /// <summary>StartAsync rồi await action; luôn dispose host, ném lại lỗi gốc. Null action: ArgumentNullException trước khi tạo host.</summary>
    public static async Task WithAsync(TestEnvironment environment, Func<HubTestHost, Task> action, Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(action);
        var host = await StartAsync(environment, configureServices);
        try { await action(host); }
        finally { await host.DisposeAsync(); }
    }
    /// <summary>Dừng listener, dispose client/provider cả khi StopAsync lỗi; gọi lặp an toàn, không xóa environment.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await SafeDisposeAsync(Client);
        try { await _app.StopAsync(); } catch { }
        await SafeDisposeAsync(_app.Services);
        try { await _app.DisposeAsync(); } catch { }
    }
    internal static async ValueTask SafeDisposeAsync(object? provider, IEnumerable<ServiceDescriptor>? descriptors = null)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var root = provider?.GetType().GetProperty("Root", flags)?.GetValue(provider) ?? provider;
        if (root?.GetType().GetProperty("Disposables", flags)?.GetValue(root) is System.Collections.IList list)
        {
            var items = list.Cast<object?>().Reverse().ToArray();
            if (!list.IsReadOnly) list.Clear();
            foreach (var item in items) await OneAsync(item, seen);
        }
        if (descriptors != null) foreach (var d in descriptors.Reverse()) await OneAsync(d.ImplementationInstance, seen);
        await OneAsync(provider, seen);
        static async ValueTask OneAsync(object? obj, HashSet<object> seen)
        {
            if (obj == null || !seen.Add(obj)) return;
            try { if (obj is IAsyncDisposable ad) await ad.DisposeAsync(); else if (obj is IDisposable disp) disp.Dispose(); } catch { }
        }
    }
    private readonly WebApplication _app;
    private int _disposed;
    private HubTestHost(WebApplication app, HttpClient client) { _app = app; Client = client; }
}