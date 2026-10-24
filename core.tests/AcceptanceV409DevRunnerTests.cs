using System.Text.Json.Nodes;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V4-09 (Claude viết trước — agent KHÔNG sửa). aiteam run: provider viết code trong worktree.
public sealed class AcceptanceV409DevRunnerTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 24, 10, 0, 0);
    private const string CodexUsageLimit = "You’ve hit your usage limit. Upgrade to Pro (https://chatgpt.com/explore/pro), visit https://chatgpt.com/settings/usage to purchase more credits or try again at Oct 24th, 2026 4:19 PM.";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v409-" + Guid.NewGuid().ToString("N"));
    private readonly string _work, _prompt, _logs;

    public AcceptanceV409DevRunnerTests()
    {
        _work = Path.Combine(_root, "work"); Directory.CreateDirectory(_work);
        _prompt = Path.Combine(_root, "p.md"); File.WriteAllText(_prompt, "làm task");
        _logs = Path.Combine(_root, "logs");
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class Fake(string id, Func<AgentRunRequest, AgentRunResult> run) : IAgentProvider
    {
        public readonly List<AgentRunRequest> Requests = new();
        public string Id => id;
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(id, true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest r, CancellationToken ct) { Requests.Add(r); return Task.FromResult(run(r)); }
    }

    private static AgentRunResult R(RunStatus s, string detail = "", string text = "xong")
        => new("codex", s, text, s == RunStatus.Succeeded ? new TokenUsage(115084, 102400, 1001) : TokenUsage.Unknown,
            TimeSpan.FromSeconds(58.4), "gpt-6.1-sol", "th-1", detail);

    private (DevRunner Runner, Fake Codex, ProviderStateStore State) Build(Func<AgentRunRequest, AgentRunResult> run, bool enabled = true, AgentRole[]? roles = null)
    {
        var cfgs = new[]
        {
            new ProviderConfig { Id = "codex", Kind = ProviderKind.Codex, Exe = "codex.js", Enabled = enabled, Roles = roles ?? new[] { AgentRole.Reviewer, AgentRole.Developer } },
        };
        var settings = new ProviderSettings(cfgs, new Dictionary<AgentRole, IReadOnlyList<string>>());
        var state = new ProviderStateStore(_root);
        var fake = new Fake("codex", run);
        return (new DevRunner(settings, state, _ => fake, () => Now), fake, state);
    }

    private DevRunInput In(string id = "codex", string? prompt = null, string? work = null) =>
        new(id, work ?? _work, prompt ?? _prompt, _logs, TimeSpan.FromMinutes(30), "gpt-6.1-sol");

    [Fact]
    public async Task Success_runs_in_develop_mode_and_clears_rest()
    {
        var (runner, codex, state) = Build(_ => R(RunStatus.Succeeded));
        state.Record(R(RunStatus.QuotaExhausted, "try again in 5 minutes"), Now.AddHours(-2));   // nghỉ cũ đã hết giờ
        var o = await runner.RunAsync(In("CODEX"), CancellationToken.None);

        Assert.Equal((DevRunner.ExitOk, "Succeeded"), (o.ExitCode, o.Status));
        Assert.Equal("Succeeded · codex · 58s · token 115084/1001", o.Summary);
        var req = Assert.Single(codex.Requests);
        Assert.Equal((RunMode.Develop, _work, _prompt, _logs, "gpt-6.1-sol"), (req.Mode, req.WorkDir, req.PromptFile, req.LogDir, req.Model));
        Assert.Equal(TimeSpan.FromMinutes(30), req.Timeout);
        Assert.Null(req.OutputSchemaFile);
        Assert.True(Directory.Exists(_logs));
        Assert.Empty(state.All());
    }

    [Fact]
    public async Task Quota_records_rest_until_reset_time()
    {
        var (runner, _, state) = Build(_ => R(RunStatus.QuotaExhausted, CodexUsageLimit, ""));
        var o = await runner.RunAsync(In(), CancellationToken.None);
        Assert.Equal((DevRunner.ExitQuota, "QuotaExhausted"), (o.ExitCode, o.Status));
        Assert.StartsWith("QuotaExhausted · codex · 58s · token ? — You’ve hit your usage limit.", o.Summary);
        Assert.True(o.Summary.Length <= "QuotaExhausted · codex · 58s · token ? — ".Length + 200);
        Assert.Equal(new DateTime(2026, 10, 24, 16, 19, 0), state.RestOf("codex", Now)!.Until);
    }

    [Fact]
    public async Task Resting_provider_is_not_called()
    {
        var (runner, codex, state) = Build(_ => R(RunStatus.Succeeded));
        state.Record(R(RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        var o = await runner.RunAsync(In(), CancellationToken.None);
        Assert.Equal((DevRunner.ExitResting, "Resting", "NGHỈ: codex nghỉ tới 16:19 24/10 (QuotaExhausted)"), (o.ExitCode, o.Status, o.Summary));
        Assert.Empty(codex.Requests);
        Assert.Null(o.Result);
    }

    [Theory]
    [InlineData(RunStatus.NotLoggedIn, DevRunner.ExitLogin)]
    [InlineData(RunStatus.TimedOut, DevRunner.ExitTimeout)]
    [InlineData(RunStatus.Failed, DevRunner.ExitFailed)]
    [InlineData(RunStatus.ConfigError, DevRunner.ExitFailed)]
    [InlineData(RunStatus.Unavailable, DevRunner.ExitFailed)]
    public async Task Exit_codes_by_status(RunStatus s, int exit)
    {
        var (runner, _, _) = Build(_ => R(s, "chi tiết"));
        var o = await runner.RunAsync(In(), CancellationToken.None);
        Assert.Equal((exit, s.ToString()), (o.ExitCode, o.Status));
        Assert.EndsWith(" — chi tiết", o.Summary);
    }

    [Fact]
    public async Task Bad_inputs_never_call_provider()
    {
        var (runner, codex, _) = Build(_ => R(RunStatus.Succeeded));
        Assert.Equal("LỖI: không có provider ghost", (await runner.RunAsync(In("ghost"), CancellationToken.None)).Summary);
        var miss = await runner.RunAsync(In(prompt: Path.Combine(_root, "khong-co.md")), CancellationToken.None);
        Assert.Equal((DevRunner.ExitConfig, "Config"), (miss.ExitCode, miss.Status));
        Assert.StartsWith("LỖI: không thấy file ", miss.Summary);
        Assert.StartsWith("LỖI: không thấy thư mục ", (await runner.RunAsync(In(work: Path.Combine(_root, "khong-co")), CancellationToken.None)).Summary);
        Assert.Empty(codex.Requests);

        var (off, _, _) = Build(_ => R(RunStatus.Succeeded), enabled: false);
        Assert.Equal("LỖI: provider codex đang tắt", (await off.RunAsync(In(), CancellationToken.None)).Summary);
        var (reviewerOnly, _, _) = Build(_ => R(RunStatus.Succeeded), roles: new[] { AgentRole.Reviewer });
        Assert.Equal("LỖI: provider codex không có vai trò Developer", (await reviewerOnly.RunAsync(In(), CancellationToken.None)).Summary);
    }

    [Fact]
    public async Task Unexpected_exception_is_failed_not_crash()
    {
        var (runner, _, _) = Build(_ => throw new IOException("ổ đĩa đầy"));
        var o = await runner.RunAsync(In(), CancellationToken.None);
        Assert.Equal((DevRunner.ExitFailed, "Failed", "LỖI: IOException: ổ đĩa đầy"), (o.ExitCode, o.Status, o.Summary));
    }

    [Fact]
    public async Task Outcome_json_for_team_ps1()
    {
        var (runner, _, _) = Build(_ => R(RunStatus.Succeeded, "", "Đã sửa 2 file"));
        var j = JsonNode.Parse(DevRunner.OutcomeJson(await runner.RunAsync(In(), CancellationToken.None)))!;
        Assert.Equal((0, "Succeeded", "codex", "gpt-6.1-sol"), ((int)j["exitCode"]!, (string?)j["status"], (string?)j["provider"], (string?)j["model"]));
        Assert.Equal((58, 115084L, 1001L, "th-1"), ((int)j["durationSec"]!, (long)j["inputTokens"]!, (long)j["outputTokens"]!, (string?)j["conversationId"]));
        Assert.Equal(("Đã sửa 2 file", ""), ((string?)j["text"], (string?)j["detail"]));

        var n = JsonNode.Parse(DevRunner.OutcomeJson(new DevRunOutcome(5, "Resting", "NGHỈ: x", null)))!;
        Assert.Equal(((string?)null, 0, -1L, ""), ((string?)n["provider"], (int)n["durationSec"]!, (long)n["inputTokens"]!, (string?)n["text"]));
    }
}
