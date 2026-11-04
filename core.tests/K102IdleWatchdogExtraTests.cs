using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class K102IdleWatchdogExtraTests
{
    [Fact]
    public async Task Hard_timeout_wins_when_idle_deadline_is_the_same()
    {
        using var repo = new TempRepo(false);
        var timeout = TimeSpan.FromSeconds(1);
        var result = await new ProcessRunner().RunAsync(
            new("node", ["-e", "setTimeout(()=>{},60000);"], repo.Root, null, timeout, IdleTimeout: timeout),
            CancellationToken.None);
        Assert.True(result.TimedOut);
        Assert.False(result.Stalled);
    }

    [Fact]
    public void Stalled_result_keeps_session_and_exact_detail_even_with_invalid_output()
    {
        var result = OpenCodeProvider.Parse("opencode",
            new(-1, "{\"type\":\"step_start\",\"sessionID\":\"ses_resume\"}\ninvalid\n", "",
                TimeSpan.FromMinutes(21), false, true), OpenCodeProvider.DefaultModel);
        Assert.Equal(RunStatus.TimedOut, result.Status);
        Assert.Equal("OpenCode đứng im 20 phút không in gì (treo hoặc hết lượt miễn phí) — đã dừng.", result.Detail);
        Assert.Contains("ses_resume", result.ToString());
    }
}