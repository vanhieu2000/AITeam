using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace AITeam.Hub.Tests;
public sealed class V905BuildCleanupTests
{
    private sealed class Probe : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }
    [Fact] public async Task Singleton_created_before_build_throws_is_disposed_and_original_error_is_preserved()
    {
        using var environment = new TestEnvironment();
        var factoryProbe = new Probe(); var instanceProbe = new Probe();
        var expected = new IOException("Lỗi logger trong Build");
        var error = await Assert.ThrowsAsync<IOException>(() => HubTestHost.StartAsync(environment, services =>
        {
            services.AddSingleton(_ => factoryProbe);
            services.AddSingleton(instanceProbe);
            // Resolve cả hai đăng ký cùng kiểu để factory được tạo trước lỗi Build.
            services.AddSingleton<ILoggerFactory>(provider =>
            {
                provider.GetServices<Probe>().ToArray();
                throw expected;
            });
        }));
        Assert.Same(expected, error);
        Assert.Contains("Build", error.StackTrace);
        Assert.Equal(1, factoryProbe.DisposeCount); Assert.Equal(1, instanceProbe.DisposeCount);
    }
}
