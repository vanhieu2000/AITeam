using System.Text.Json.Nodes;
using AITeam.Core.Providers;
using AITeam.Core.Review;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V4-08 (Claude viết trước — agent KHÔNG sửa). aiteam review: chọn reviewer, thử reviewer kế khi hết hạn mức, mã thoát.
public sealed class AcceptanceV408ReviewRunnerTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 23, 23, 40, 0);
    private const string CodexUsageLimit = "You’ve hit your usage limit. Upgrade to Pro (https://chatgpt.com/explore/pro), visit https://chatgpt.com/settings/usage to purchase more credits or try again at Oct 24th, 2026 4:19 AM.";
    private const string PassJson = """{"verdict":"PASS","findings":[{"severity":"optional","file":"a.cs","line":3,"evidence":"tên biến","fix":"đổi tên"}],"unverified":[]}""";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v408-" + Guid.NewGuid().ToString("N"));
    private readonly string _task, _diff, _verify, _logs;

    public AcceptanceV408ReviewRunnerTests()
    {
        Directory.CreateDirectory(_root);
        _task = Path.Combine(_root, "T-01.md"); File.WriteAllText(_task, "# T-01 · Thử\n\n## Tiêu chí hoàn thành\n- [ ] chặn .pem\n");
        _diff = Path.Combine(_root, "diff.txt"); File.WriteAllText(_diff, "diff --git a/x b/x\n+dòng mới\n");
        _verify = Path.Combine(_root, "verify.md"); File.WriteAllText(_verify, "Cổng kiểm tra: ĐẠT");
        _logs = Path.Combine(_root, "logs");
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class FakeProvider(string id, ProviderKind kind, Func<AgentRunResult> run) : IAgentProvider
    {
        public int Calls;
        public string Id => id;
        public ProviderKind Kind => kind;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(id, true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) { Calls++; return Task.FromResult(run()); }
    }

    private static AgentRunResult Ok(string id, string text) => new(id, RunStatus.Succeeded, text, new TokenUsage(100, 0, 10), TimeSpan.FromSeconds(39), "m", "c", "");
    private static AgentRunResult Quota(string id) => new(id, RunStatus.QuotaExhausted, "", TokenUsage.Unknown, TimeSpan.FromSeconds(2), null, null, CodexUsageLimit);

    private (ReviewRunner Runner, Dictionary<string, FakeProvider> Fakes, ProviderStateStore State) Build(
        string[] reviewers, Dictionary<string, Func<AgentRunResult>> behaviour)
    {
        var providers = new List<ProviderConfig>
        {
            new() { Id = "agy", Kind = ProviderKind.Antigravity, Exe = "agy.exe", Roles = new[] { AgentRole.Developer } },
            new() { Id = "codex", Kind = ProviderKind.Codex, Exe = "codex.js", Roles = new[] { AgentRole.Reviewer, AgentRole.Developer } },
            new() { Id = "claude", Kind = ProviderKind.ClaudeCli, Exe = "claude.exe", Roles = new[] { AgentRole.Reviewer } },
        };
        var settings = new ProviderSettings(providers, new Dictionary<AgentRole, IReadOnlyList<string>> { [AgentRole.Reviewer] = reviewers });
        var state = new ProviderStateStore(_root);
        var fakes = providers.ToDictionary(p => p.Id, p => new FakeProvider(p.Id, p.Kind, behaviour.TryGetValue(p.Id, out var b) ? b : () => Ok(p.Id, PassJson)));
        var runner = new ReviewRunner(settings, new ProviderRouter(settings, state), state, c => fakes[c.Id], () => Now);
        return (runner, fakes, state);
    }

    private ReviewRunInput Input(string dev = "agy", string? diff = null, string? verify = "") =>
        new(_task, diff ?? _diff, verify == "" ? _verify : verify, _root, _logs, dev, TimeSpan.FromMinutes(5));

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task Real_changes_requested_output_gives_exit_1_and_feedback()
    {
        var (runner, fakes, _) = Build(new[] { "codex" }, new() { ["codex"] = () => Ok("codex", Fixture("codex-review-last.json")) });
        var o = await runner.RunAsync(Input(), CancellationToken.None);

        Assert.Equal(ReviewRunner.ExitChanges, o.ExitCode);
        Assert.Equal("CHANGES_REQUESTED · codex · 2 phát hiện (0 blocker, 2 required, 0 optional) · 39s", o.Summary);
        Assert.Equal(1, fakes["codex"].Calls);
        var fb = ReviewRunner.Feedback(o.Result!).Split('\n');
        Assert.Equal("Reviewer codex yêu cầu sửa:", fb[0]);
        Assert.Equal(3, fb.Length);
        Assert.StartsWith("- [required] tools/secret-scan.ps1:17 — Ngoại lệ templates/", fb[1]);
        Assert.Contains(" ⇒ Không áp dụng ngoại lệ templates/", fb[1]);
        Assert.NotEmpty(Directory.GetFiles(_logs, "review-*.json"));   // ReviewService đã lưu kết quả
    }

    [Fact]
    public async Task Pass_gives_exit_0_and_empty_feedback()
    {
        var (runner, _, _) = Build(new[] { "codex" }, new());
        var o = await runner.RunAsync(Input(), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitPass, o.ExitCode);
        Assert.Equal("PASS · codex · 1 phát hiện (0 blocker, 0 required, 1 optional) · 39s", o.Summary);
        Assert.Equal("", ReviewRunner.Feedback(o.Result!));
        Assert.Empty(o.Notes);
    }

    [Fact]
    public async Task Quota_on_first_reviewer_marks_rest_and_tries_next()
    {
        var (runner, fakes, state) = Build(new[] { "codex", "claude" }, new() { ["codex"] = () => Quota("codex") });
        var o = await runner.RunAsync(Input(), CancellationToken.None);

        Assert.Equal(ReviewRunner.ExitPass, o.ExitCode);
        Assert.Equal("claude", o.Result!.ProviderId);
        Assert.Equal((1, 1), (fakes["codex"].Calls, fakes["claude"].Calls));
        Assert.Equal(new DateTime(2026, 10, 24, 4, 19, 0), state.RestOf("codex", Now)!.Until);
        Assert.Contains(o.Notes, n => n.StartsWith("codex: Provider lỗi (QuotaExhausted)") && n.EndsWith("⇒ thử reviewer khác"));
        Assert.Contains("codex: nghỉ tới 04:19 24/10 (QuotaExhausted)", o.Notes);   // lý do router bỏ qua ở lần 2
    }

    [Fact]
    public async Task Only_reviewer_resting_gives_blocked_without_calling_it()
    {
        var (runner, fakes, state) = Build(new[] { "codex" }, new());
        state.Record(Quota("codex"), Now);
        var o = await runner.RunAsync(Input(), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.Equal("BLOCKED: không có reviewer khả dụng — codex: nghỉ tới 04:19 24/10 (QuotaExhausted)", o.Summary);
        Assert.Equal(0, fakes["codex"].Calls);
        Assert.Null(o.Result);
    }

    [Fact]
    public async Task Self_review_is_never_called()
    {
        var (runner, fakes, _) = Build(new[] { "codex" }, new());
        var o = await runner.RunAsync(Input(dev: "Codex"), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.Equal("BLOCKED: không có reviewer khả dụng — codex: là developer của task — cấm tự duyệt", o.Summary);
        Assert.Equal(0, fakes["codex"].Calls);
    }

    [Fact]
    public async Task Quota_on_every_reviewer_stops_after_max_attempts()
    {
        var (runner, fakes, _) = Build(new[] { "codex", "claude" }, new() { ["codex"] = () => Quota("codex"), ["claude"] = () => Quota("claude") });
        var o = await runner.RunAsync(Input(), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.StartsWith("BLOCKED · claude: Provider lỗi (QuotaExhausted)", o.Summary);
        Assert.Equal((1, 1), (fakes["codex"].Calls, fakes["claude"].Calls));
    }

    [Fact]
    public async Task Timeout_is_blocked_without_retry()
    {
        var timedOut = new AgentRunResult("codex", RunStatus.TimedOut, "", TokenUsage.Unknown, TimeSpan.FromMinutes(5), null, null, "quá 5 phút");
        var (runner, fakes, state) = Build(new[] { "codex", "claude" }, new() { ["codex"] = () => timedOut });
        var o = await runner.RunAsync(Input(), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.Equal(0, fakes["claude"].Calls);          // lỗi không phải hạn mức ⇒ không đổi reviewer
        Assert.Null(state.RestOf("codex", Now));
    }

    [Fact]
    public async Task Bad_inputs()
    {
        var (runner, fakes, _) = Build(new[] { "codex" }, new());
        var missing = await runner.RunAsync(Input(diff: Path.Combine(_root, "khong-co.txt")), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitConfig, missing.ExitCode);
        Assert.StartsWith("LỖI: không thấy file ", missing.Summary);

        var empty = Path.Combine(_root, "empty.txt"); File.WriteAllText(empty, "  \n");
        var e = await runner.RunAsync(Input(diff: empty), CancellationToken.None);
        Assert.Equal((ReviewRunner.ExitBlocked, "BLOCKED: diff rỗng — không có gì để review"), (e.ExitCode, e.Summary));

        var noVerify = await runner.RunAsync(Input(verify: Path.Combine(_root, "khong-co.md")), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitPass, noVerify.ExitCode);
        Assert.Equal(1, fakes["codex"].Calls);
    }

    [Fact]
    public async Task Outcome_json_for_team_ps1()
    {
        var (runner, _, _) = Build(new[] { "codex" }, new() { ["codex"] = () => Ok("codex", Fixture("codex-review-last.json")) });
        var o = await runner.RunAsync(Input(), CancellationToken.None);
        var j = JsonNode.Parse(ReviewRunner.OutcomeJson(o))!;
        Assert.Equal(1, (int)j["exitCode"]!);
        Assert.Equal("CHANGES_REQUESTED", (string?)j["verdict"]);
        Assert.Equal("codex", (string?)j["provider"]);
        Assert.Equal(o.Result!.DiffHash, (string?)j["diffHash"]);
        Assert.StartsWith("Reviewer codex yêu cầu sửa:", (string?)j["feedback"]);
        Assert.Equal(o.Summary, (string?)j["summary"]);
        Assert.NotNull(j["notes"] as JsonArray);

        var blocked = JsonNode.Parse(ReviewRunner.OutcomeJson(new ReviewRunOutcome(3, "BLOCKED: x", new[] { "a" }, null)))!;
        Assert.Equal(("BLOCKED", (string?)null, ""), ((string?)blocked["verdict"], (string?)blocked["provider"], (string?)blocked["feedback"]));
    }
}
