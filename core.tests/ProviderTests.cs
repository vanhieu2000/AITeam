using AITeam.Core.Process;
using AITeam.Core.Providers;

namespace AITeam.Core.Tests;

public class ProviderTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static ProcessResult Ok(string stdout, string stderr = "") => new(0, stdout, stderr, TimeSpan.FromSeconds(3), false);

    [Fact]
    public void Agy_real_success_log_is_parsed()
    {
        var r = AgyProvider.Parse("agy", Ok(Fixture("agy-success.json")), null);
        Assert.Equal(RunStatus.Succeeded, r.Status);
        Assert.True(r.Tokens.IsKnown);
        Assert.True(r.Tokens.Output > 0);
        Assert.True(r.Duration.TotalMinutes > 60); // T-007 chạy ~73 phút (duration_seconds của agy, không phải thời gian giả của tiến trình)
        Assert.False(string.IsNullOrEmpty(r.ConversationId));
    }

    [Theory]
    [InlineData("{\"status\":\"ERROR\",\"response\":\"\"}", "You have run out of credits", RunStatus.QuotaExhausted)]
    [InlineData("{\"status\":\"ERROR\"}", "RESOURCE_EXHAUSTED: quota", RunStatus.QuotaExhausted)]
    [InlineData("không phải json", "Please log in to continue", RunStatus.NotLoggedIn)]
    [InlineData("", "", RunStatus.Failed)]
    public void Agy_failures_are_classified_and_never_success(string stdout, string stderr, RunStatus expected)
    {
        var r = AgyProvider.Parse("agy", new ProcessResult(1, stdout, stderr, TimeSpan.FromSeconds(1), false), null);
        Assert.Equal(expected, r.Status);
        Assert.False(r.Ok);
    }

    [Fact]
    public void Agy_success_with_big_token_count_is_not_quota()
    {
        // Bài học team.ps1: "429" từng khớp nhầm trong "thinking_tokens":42953.
        var json = "{\"status\":\"SUCCESS\",\"response\":\"xong\",\"usage\":{\"input_tokens\":4290,\"output_tokens\":42953}}";
        Assert.Equal(RunStatus.Succeeded, AgyProvider.Parse("agy", Ok(json), null).Status);
    }

    [Fact]
    public void Agy_timeout_is_timed_out()
    {
        var r = AgyProvider.Parse("agy", new ProcessResult(-1, "", "", TimeSpan.FromMinutes(32), true), null);
        Assert.Equal(RunStatus.TimedOut, r.Status);
    }

    [Fact]
    public void Codex_real_success_events_give_tokens_and_last_message()
    {
        var last = Fixture("codex-review-last.json");
        var r = CodexProvider.Parse("codex", Ok(Fixture("codex-success.jsonl")), last, "gpt-6.1-sol");
        Assert.Equal(RunStatus.Succeeded, r.Status);
        Assert.Equal(last, r.Text);
        Assert.True(r.Tokens.Input > 0 && r.Tokens.CachedInput > 0 && r.Tokens.Output > 0);
        Assert.False(string.IsNullOrEmpty(r.ConversationId));
    }

    [Fact]
    public void Codex_real_model_error_is_config_error_not_success()
    {
        var r = CodexProvider.Parse("codex", new ProcessResult(1, Fixture("codex-model-error.jsonl"), "", TimeSpan.FromSeconds(4), false), "", null);
        Assert.Equal(RunStatus.ConfigError, r.Status);
        Assert.Equal("", r.Text);
        Assert.Contains("not supported", r.Detail);
    }

    [Fact]
    public void Codex_exit_zero_without_turn_completed_is_not_success()
    {
        var r = CodexProvider.Parse("codex", Ok("{\"type\":\"thread.started\",\"thread_id\":\"x\"}\n"), "{\"verdict\":\"PASS\"}", null);
        Assert.NotEqual(RunStatus.Succeeded, r.Status);
        Assert.Equal("", r.Text); // không dùng câu trả lời của lượt chưa hoàn tất
    }

    [Fact]
    public void Codex_read_only_mode_uses_cli_sandbox_and_stdin_prompt()
    {
        var p = new CodexProvider(new ProviderConfig { Id = "codex", Kind = ProviderKind.Codex, Exe = @"C:\x\codex.js", Model = "gpt-6.1-sol" }, new ProcessRunner());
        var args = p.BuildArguments(new AgentRunRequest(@"D:\w", @"D:\p.md", RunMode.ReadOnly, TimeSpan.FromMinutes(5), OutputSchemaFile: @"D:\s.json"), @"D:\last.txt");
        Assert.Equal(@"C:\x\codex.js", args[0]);
        var joined = string.Join(" ", args);
        Assert.Contains("--sandbox read-only", joined);
        Assert.Contains("--output-schema D:\\s.json", joined);
        Assert.Contains("-m gpt-6.1-sol", joined);
        Assert.Equal("-", args[^1]); // lời nhắn qua stdin
        Assert.DoesNotContain("workspace-write", joined);
        Assert.DoesNotContain("danger-full-access", joined);
    }

    [Fact]
    public void Agy_read_only_mode_adds_sandbox_and_time_limit()
    {
        var p = new AgyProvider(new ProviderConfig { Id = "agy", Kind = ProviderKind.Antigravity, Exe = "agy.exe" }, new ProcessRunner());
        var joined = string.Join(" ", p.BuildArguments(new AgentRunRequest(@"D:\w", @"D:\p.md", RunMode.ReadOnly, TimeSpan.FromMinutes(25))));
        Assert.Contains("--sandbox", joined);
        Assert.Contains("--print-timeout 25m", joined);
    }
}
