using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// Review H3-02 (Claude): số đo các lượt cùng phiên agy là CỘNG DỒN ⇒ Hub từng hiện V4-06 51′ thay vì 23′.
public sealed class InsightDeltaTests
{
    private static RunRecord R(string at, int run, int dur, long tok, string? conv, string task = "V4-06")
        => InsightParsers.ParseRun($$"""{"at":"{{at}}","project":"AITeam","task":"{{task}}","run":"{{run}}","durationSec":{{dur}},"totalTokens":{{tok}}{{(conv == null ? "" : $",\"conversationId\":\"{conv}\"")}}}""")!;

    [Fact]
    public void Real_V4_06_cumulative_runs_become_per_run()
    {
        const string conv = "ead182f4-a20b-457a-9ef3-0d6ddd2ecb87";
        var runs = new[]
        {
            R("2026-10-23T23:56:27", 3, 1380, 189519, conv),
            R("2026-10-23T23:43:15", 1, 588, 93186, conv),
            R("2026-10-23T23:51:17", 2, 1070, 157778, conv),
        };
        Assert.Equal(conv, runs[0].ConversationId);

        var d = InsightParsers.ToDeltas(runs);
        Assert.Equal(new[] { 1, 2, 3 }, d.Select(r => r.Run));
        Assert.Equal(new[] { 588, 482, 310 }, d.Select(r => r.DurationSec));
        Assert.Equal(new[] { 93186L, 64592L, 31741L }, d.Select(r => r.TotalTokens));
        Assert.Equal(23, (int)Math.Round(d.Sum(r => r.DurationSec) / 60.0)); // không phải 51′

        // Timeline không còn đoạn "Sửa lại" chồng lên đoạn trước.
        var tl = InsightParsers.BuildTimeline(d, null);
        for (int i = 1; i < tl.Count; i++) Assert.True(tl[i].Start >= tl[i - 1].End.AddSeconds(-60), $"đoạn {i} chồng đoạn trước");
    }

    [Fact]
    public void Runs_without_conversation_or_from_other_conversations_stay_separate()
    {
        var d = InsightParsers.ToDeltas(new[]
        {
            R("2026-10-21T10:00:00", 1, 600, 1000, null, "T-007"),
            R("2026-10-21T11:00:00", 2, 900, 2000, null, "T-007"),
            R("2026-10-21T12:00:00", 1, 300, 500, "a", "C-011"),
            R("2026-10-21T13:00:00", 1, 200, 400, "b", "C-011"),
        });
        Assert.Equal(new[] { 600, 900, 300, 200 }, d.Select(r => r.DurationSec));
    }
}
