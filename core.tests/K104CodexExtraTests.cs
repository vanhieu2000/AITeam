using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class K104CodexExtraTests
{
    [Theory]
    [InlineData("{", true)]
    [InlineData("{\"type\":null}", true)]
    [InlineData("{\"type\":\"item.started\",\"item\":{\"type\":\"error\"}}", false)]
    [InlineData("{\"type\":\"item.completed\",\"item\":{\"type\":123}}", true)]
    [InlineData("{\"type\":\"item.completed\",\"item\":{\"type\":\"agent_message\",\"text\":\" ERROR codex_x\"}}", true)]
    public void Filter_handles_edge_events(string line, bool expected)
        => Assert.Equal(expected, CodexProvider.IsActivityLine(line));

    [Fact]
    public async Task Throwing_filter_preserves_stderr_and_counts_activity()
    {
        using var repo = new TempRepo(false);
        var spec = new ProcessSpec("node",
            ["-e", "let n=0;const t=setInterval(()=>{console.error('hoat-dong');if(++n>=14)clearInterval(t);},500);"],
            repo.Root, null, TimeSpan.FromSeconds(30),
            IdleTimeout: TimeSpan.FromSeconds(5), IsActivity: _ => throw new InvalidOperationException());
        var result = await new ProcessRunner().RunAsync(spec, CancellationToken.None);
        Assert.False(result.Stalled);
        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(14, result.StdErr.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}