using Microsoft.Extensions.DependencyInjection;
namespace AITeam.Hub.Tests.Support;

/// <summary>Host Razor thật cho test; không sở hữu TestEnvironment của bên gọi.</summary>
public sealed class HubTestHost : IAsyncDisposable
{
    /// <summary>Client giữ cookie, không tự chuyển hướng, timeout 15 giây; BaseAddress là loopback cổng đã bind.</summary>
    public HttpClient Client { get { throw new NotImplementedException(); // TODO(agent)
    } }
    /// <summary>Provider chứa environment.Paths, fake an toàn và HubPageRenderer đăng ký scoped.</summary>
    public IServiceProvider Services { get { throw new NotImplementedException(); // TODO(agent)
    } }
    /// <summary>
    /// Dựng Razor từ assembly Hub như AntiforgerySecurityTests; FakeServices.Register trước configureServices.
    /// Không chạy Program Hub/đọc cấu hình thật; content root=environment.RootDir; không lưu DataProtection vào profile thật.
    /// Khóa HubPaths=environment.Paths sau callback; chỉ Listen IPAddress.Loopback cổng 0, không dò cổng trước.
    /// Giữ antiforgery. Lỗi khởi động/callback: dispose mọi tài nguyên đã tạo rồi ném lại lỗi gốc.
    /// Null environment: ArgumentNullException. Không dispose environment.
    /// </summary>
    public static Task<HubTestHost> StartAsync(TestEnvironment environment, Action<IServiceCollection>? configureServices = null)
    { throw new NotImplementedException(); // TODO(agent)
    }
    /// <summary>StartAsync rồi await action; luôn dispose host, ném lại lỗi gốc. Null action: ArgumentNullException trước khi tạo host.</summary>
    public static Task WithAsync(TestEnvironment environment, Func<HubTestHost, Task> action, Action<IServiceCollection>? configureServices = null)
    { throw new NotImplementedException(); // TODO(agent)
    }
    /// <summary>Dừng listener, dispose client/provider cả khi StopAsync lỗi; gọi lặp an toàn, không xóa environment.</summary>
    public ValueTask DisposeAsync()
    { throw new NotImplementedException(); // TODO(agent)
    }
}
