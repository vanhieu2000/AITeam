using AITeam.Core.Messages;

namespace AITeam.Core.Tests;

// R-005 (Claude): kho tin nhắn theo task — MCP ghi, Hub đọc.
public sealed class MessageStoreTests : IDisposable
{
    private static readonly DateTime T0 = new(2026, 10, 25, 10, 0, 0);
    private readonly string _logs = Path.Combine(Path.GetTempPath(), "aiteam-msg-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_logs, true); } catch { } }

    [Fact]
    public void Append_and_read_back_in_order()
    {
        var s = new MessageStore(_logs);
        s.Append("V5-01", "Minh (A)", "progress", "Bắt đầu task", T0);
        s.Append("V5-01", "Minh (A)", "done", "Xong, 18/18 test", T0.AddMinutes(9));
        var m = s.Read("V5-01");
        Assert.Equal(new[] { "Bắt đầu task", "Xong, 18/18 test" }, m.Select(x => x.Text));
        Assert.Equal(("Minh (A)", "done", T0.AddMinutes(9)), (m[1].From, m[1].Kind, m[1].At));
        var bytes = File.ReadAllBytes(s.PathFor("V5-01"));
        Assert.False(bytes[0] == 0xEF, "không BOM");
    }

    [Theory]
    [InlineData("../x")]
    [InlineData("a/b")]
    [InlineData("C:x")]
    [InlineData("")]
    [InlineData("V5 01")]
    public void Bad_task_ids_rejected(string task)
    {
        var s = new MessageStore(_logs);
        Assert.Throws<ArgumentException>(() => s.Append(task, "x", "progress", "hi", T0));
        Assert.Empty(s.Read(task));
    }

    [Fact]
    public void Validates_kind_text_and_sanitizes()
    {
        var s = new MessageStore(_logs);
        Assert.Throws<ArgumentException>(() => s.Append("T-1", "x", "rm -rf", "hi", T0));
        Assert.Throws<ArgumentException>(() => s.Append("T-1", "x", "progress", "   ", T0));
        var m = s.Append("T-1", new string('a', 100), "progress", "dòng 1\u0007\u001b[31m\ndòng 2\t" + new string('x', 3000), T0);
        Assert.Equal(MessageStore.MaxFrom, m.From.Length);
        Assert.DoesNotContain('\u0007', m.Text);
        Assert.DoesNotContain('\u001b', m.Text);
        Assert.Contains("\ndòng 2", m.Text);
        Assert.Equal(MessageStore.MaxText + 1, m.Text.Length);
        Assert.EndsWith("…", m.Text);
    }

    [Fact]
    public void Read_skips_broken_lines_and_limits_to_latest()
    {
        var s = new MessageStore(_logs);
        for (var i = 1; i <= 5; i++) s.Append("T-2", "x", "progress", $"tin {i}", T0.AddMinutes(i));
        File.AppendAllText(s.PathFor("T-2"), "{ hỏng\n\n");
        Assert.Equal(new[] { "tin 4", "tin 5" }, s.Read("T-2", 3).Select(x => x.Text));   // 3 dòng cuối: tin 4, tin 5, dòng hỏng
        Assert.Equal(5, s.Read("T-2").Count);
        Assert.Empty(s.Read("KHONG-CO"));
    }

    [Fact]
    public void Concurrent_appends_from_many_threads_are_all_kept()
    {
        var s = new MessageStore(_logs);
        Parallel.For(0, 50, i => s.Append("T-3", "x", "progress", $"tin {i}", T0));
        Assert.Equal(50, s.Read("T-3", 500).Count);
    }
}
