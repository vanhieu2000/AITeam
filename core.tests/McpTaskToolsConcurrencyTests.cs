using AITeam.Core.Mcp;
using AITeam.Core.Messages;

namespace AITeam.Core.Tests;

public sealed class McpTaskToolsConcurrencyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v501-conc-" + Guid.NewGuid().ToString("N"));

    public McpTaskToolsConcurrencyTests() => Directory.CreateDirectory(_root);

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    [Fact]
    public async Task Post_message_concurrent_at_59_does_not_exceed_60()
    {
        var store = new MessageStore(_root);
        var tools = new McpTaskTools(
            new McpSession("AITeam", "V5-1", "developer", "Minh (A)", Path.Combine(_root, "task.md"), _root),
            store,
            () => new DateTime(2026, 10, 25, 10, 0, 0));

        for (var i = 0; i < McpTaskTools.MaxMessagesPerSession - 1; i++)
            Assert.Equal($"Đã ghi tin nhắn ({i + 1}/60).", tools.PostMessage($"tin {i}"));

        using var barrier = new Barrier(8);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            barrier.SignalAndWait();
            return tools.PostMessage($"đồng thời {i}");
        })));

        Assert.Single(results, r => r == "Đã ghi tin nhắn (60/60).");
        Assert.Equal(7, results.Count(r => r == "LỖI: quá 60 tin nhắn trong lượt này"));
        Assert.Equal(60, store.Read("V5-1", 500).Count);
    }
}
