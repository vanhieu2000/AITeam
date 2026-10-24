using System.Collections.Concurrent;
using AITeam.Core.Providers;
using AITeam.Core.Review;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V4-11 (Claude viết trước — agent KHÔNG sửa). ReviewService: file tạm / kết quả có tên riêng mỗi lần review.
public sealed class AcceptanceV411ReviewFilesTests : IDisposable
{
    private const string PassJson = """{"verdict":"PASS","findings":[],"unverified":[]}""";
    private readonly string _log = Path.Combine(Path.GetTempPath(), "aiteam-v411-" + Guid.NewGuid().ToString("N"));

    public void Dispose() { try { Directory.Delete(_log, true); } catch { } }

    /// <summary>Đọc lời nhắn ĐÚNG lúc được gọi (trước khi lần review khác kịp ghi đè).</summary>
    private sealed class Spy(int delayMs) : IAgentProvider
    {
        public readonly ConcurrentBag<(string Prompt, string Schema, string PromptText)> Calls = new();
        public string Id => "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth("codex", true, true, ""));
        public async Task<AgentRunResult> RunAsync(AgentRunRequest r, CancellationToken ct)
        {
            var text = await File.ReadAllTextAsync(r.PromptFile, ct);
            await Task.Delay(delayMs, ct);
            Calls.Add((r.PromptFile, r.OutputSchemaFile!, text));
            return new AgentRunResult("codex", RunStatus.Succeeded, PassJson, new TokenUsage(1, 0, 1), TimeSpan.FromSeconds(1), "m", "c", "");
        }
    }

    private static ReviewPack Pack(ReviewService s, string marker) =>
        s.BuildPack(new ReviewPackInput($"# {marker}\n- tiêu chí của {marker}", $"diff --git a/{marker} b/{marker}\n+{marker}\n", null, null));

    [Fact]
    public async Task Two_reviews_in_same_folder_and_second_keep_separate_files()
    {
        var s = new ReviewService();
        var spy = new Spy(0);
        await s.ReviewAsync(Pack(s, "TASK-ONE"), spy, "agy", _log, _log, TimeSpan.FromMinutes(1), CancellationToken.None);
        await s.ReviewAsync(Pack(s, "TASK-TWO"), spy, "agy", _log, _log, TimeSpan.FromMinutes(1), CancellationToken.None);

        var calls = spy.Calls.ToList();
        Assert.Equal(2, calls.Count);
        Assert.Equal(2, calls.Select(c => c.Prompt).Distinct().Count());
        Assert.Equal(2, calls.Select(c => c.Schema).Distinct().Count());
        Assert.All(calls, c => Assert.StartsWith(_log, c.Prompt));
        Assert.All(calls, c => Assert.Matches(@"prompt-review-\d{8}-\d{6}-[0-9a-f]{8}\.md$", Path.GetFileName(c.Prompt)));
        Assert.All(calls, c => Assert.Matches(@"schema-review-\d{8}-\d{6}-[0-9a-f]{8}\.json$", Path.GetFileName(c.Schema)));

        // File lời nhắn của lần 1 còn nguyên nội dung lần 1 (không bị lần 2 ghi đè).
        var one = calls.Single(c => c.PromptText.Contains("TASK-ONE"));
        Assert.Contains("TASK-ONE", File.ReadAllText(one.Prompt));
        Assert.DoesNotContain("TASK-TWO", File.ReadAllText(one.Prompt));

        // 2 kết quả, không ghi đè nhau dù cùng giây.
        var results = Directory.GetFiles(_log, "review-*.json");
        Assert.Equal(2, results.Length);
        Assert.All(results, f => Assert.Matches(@"^review-\d{8}-\d{6}-[0-9a-f]{8}\.json$", Path.GetFileName(f)));
    }

    [Fact]
    public async Task Parallel_reviews_each_see_their_own_prompt()
    {
        var s = new ReviewService();
        var spy = new Spy(150);
        var tasks = Enumerable.Range(1, 4).Select(i =>
            s.ReviewAsync(Pack(s, $"PAR-{i}"), spy, "agy", _log, _log, TimeSpan.FromMinutes(1), CancellationToken.None)).ToList();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(ReviewVerdict.Pass, r.Verdict));
        Assert.Equal(4, spy.Calls.Select(c => c.Prompt).Distinct().Count());
        foreach (var c in spy.Calls)
        {
            var marker = System.Text.RegularExpressions.Regex.Match(c.PromptText, @"PAR-\d").Value;
            Assert.Contains(marker, File.ReadAllText(c.Prompt));   // file của mình vẫn là của mình
        }
        Assert.Equal(4, Directory.GetFiles(_log, "review-*.json").Length);
    }
}
