using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// R-004 (Claude): xóa trạng thái nghỉ từ Hub (nút "Hết nghỉ").
public sealed class ProviderStateClearTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-clear-" + Guid.NewGuid().ToString("N"));
    public ProviderStateClearTests() => Directory.CreateDirectory(_root);
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    [Fact]
    public void Clear_removes_only_that_provider()
    {
        var now = new DateTime(2026, 10, 24, 10, 0, 0);
        var s = new ProviderStateStore(_root);
        s.Record(new AgentRunResult("codex", RunStatus.QuotaExhausted, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "try again in 2 hours"), now);
        s.Record(new AgentRunResult("agy", RunStatus.NotLoggedIn, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "Please log in"), now);
        Assert.True(s.Clear("CODEX"));
        Assert.Null(s.RestOf("codex", now));
        Assert.NotNull(s.RestOf("agy", now));
        Assert.False(s.Clear("codex"));
        Assert.False(new ProviderStateStore(Path.Combine(_root, "khong-co")).Clear("agy"));
    }
}