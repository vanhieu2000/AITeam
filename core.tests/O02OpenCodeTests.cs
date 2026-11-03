using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class O02OpenCodeTests
{
    private const string Events = """
        {"type":"text","part":{"text":"Bản nháp"}}
        {"type":"step_finish","part":{"tokens":{"input":10,"output":2,"total":12}}}
        {"type":"text","part":{"text":"Báo cáo cuối"}}
        {"type":"step_finish","part":{"tokens":{"input":20,"output":3,"total":23}}}
        """;

    [Fact]
    public void Parser_uses_last_text_and_sums_all_step_finish_events()
    {
        var result = OpenCodeProvider.Parse("opencode", new(0, Events, "", TimeSpan.Zero, false), OpenCodeProvider.DefaultModel);
        Assert.True(result.Ok); Assert.Equal("Báo cáo cuối", result.Text);
        Assert.Equal(30, result.Tokens.Input); Assert.Equal(5, result.Tokens.Output); Assert.Equal(35, result.Tokens.Total);
    }
    [Fact]
    public void Missing_usage_is_unknown_and_denied_command_is_reported_even_at_exit_zero()
    {
        var result = OpenCodeProvider.Parse("opencode", new(0, "{\"type\":\"text\",\"part\":{\"text\":\"Xong\"}}", "", TimeSpan.Zero, false), OpenCodeProvider.DefaultModel);
        Assert.False(result.Tokens.IsKnown);
        const string denied = "{\"type\":\"tool_use\",\"part\":{\"state\":{\"status\":\"error\",\"input\":{\"command\":\"curl example.com\"},\"error\":\"Permission denied\"}}}";
        result = OpenCodeProvider.Parse("opencode", new(0, Events + "\n" + denied, "", TimeSpan.Zero, false), OpenCodeProvider.DefaultModel);
        Assert.False(result.Ok); Assert.Contains("curl example.com", result.Detail);
    }
    [Fact]
    public async Task Invocation_closes_stdin_copies_permissions_and_keeps_prompt_argument_before_file()
    {
        using var repo = new TempRepo(false);
        var permissions = RealFile.Copy("templates/opencode-quyen-han.json", repo.Root);
        var prompt = repo.Write("T-1.md", "Làm đúng task.");
        var runner = new StubRunner();
        var cfg = new ProviderConfig { Id = "opencode", Exe = "opencode", Model = OpenCodeProvider.DefaultModel, PrivateRepository = false, PermissionsFile = permissions };
        var result = await new OpenCodeProvider(cfg, runner).RunAsync(new(repo.Root, prompt, RunMode.Develop, TimeSpan.FromSeconds(5)) { Title = "T-1" }, CancellationToken.None);
        Assert.True(result.Ok); Assert.NotNull(runner.Spec); Assert.Null(runner.Spec.StdinFile);
        Assert.Equal("Lam dung theo file dinh kem.", runner.Spec.Arguments[1]); Assert.Equal("-f", runner.Spec.Arguments[2]);
        Assert.Contains("T-1", runner.Spec.Arguments); Assert.DoesNotContain("--auto", runner.Spec.Arguments);
        Assert.False(File.Exists(runner.ConfigFile));
    }
    [Theory]
    [InlineData("opencode/big-pickle", false, false)]
    [InlineData("opencode/deepseek-v4.1-flash-free", true, false)]
    public async Task Invalid_model_or_free_private_repo_is_blocked_before_process(string model, bool privateRepo, bool paid)
    {
        var runner = new StubRunner();
        var provider = new OpenCodeProvider(new ProviderConfig { Id = "opencode", Model = model, PrivateRepository = privateRepo, Paid = paid }, runner);
        var result = await provider.RunAsync(new("unused", "unused", RunMode.Develop, TimeSpan.FromSeconds(1)), CancellationToken.None);
        Assert.Equal(RunStatus.ConfigError, result.Status); Assert.Null(runner.Spec);
    }
    [Fact]
    public async Task Fake_cli_process_receives_empty_stdin_and_permission_copy_without_network()
    {
        using var repo = new TempRepo(false);
        var permissions = RealFile.Copy("templates/opencode-quyen-han.json", repo.Root);
        var prompt = repo.Write("O-02.md", "Task giả, không gọi mạng.");
        var script = "const fs=require('fs');let input='';process.stdin.on('data',b=>input+=b);process.stdin.on('end',()=>{"
            + "const a=process.argv.slice(2);if(input!==''||a[0]!=='run'||a[2]!=='-f'||a.includes('--auto')||!fs.existsSync(process.env.OPENCODE_CONFIG))process.exit(9);"
            + "process.stdout.write(" + System.Text.Json.JsonSerializer.Serialize(Events) + ");});";
        var fake = repo.Write("opencode-stub.js", script, false);
        var provider = new OpenCodeProvider(new ProviderConfig { Id = "opencode", Exe = fake, PrivateRepository = false, PermissionsFile = permissions }, new ProcessRunner());
        var result = await provider.RunAsync(new(repo.Root, prompt, RunMode.Develop, TimeSpan.FromSeconds(10)) { Title = "O-02" }, CancellationToken.None);
        Assert.True(result.Ok, result.Detail); Assert.Equal(35, result.Tokens.Total);
    }
    [Fact]
    public void Explicit_disabled_provider_is_not_silently_enabled_by_cli_configuration()
    {
        using var repo = new TempRepo(false);
        repo.Write("team.json", "{}");
        var cfg = OpenCodeProvider.Configuration(repo.Root, true, new ProviderConfig { Id = "opencode", Kind = ProviderKind.OpenCode, Enabled = false, Roles = [] });
        Assert.False(cfg.Enabled); Assert.Empty(cfg.Roles);
    }
    [Fact]
    public void Real_archived_proxy_error_is_failure_with_unknown_tokens()
    {
        var events = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "opencode-proxy-error.jsonl"));
        var result = OpenCodeProvider.Parse("opencode", new(1, events, "", TimeSpan.FromSeconds(2), false), OpenCodeProvider.DefaultModel);
        Assert.False(result.Ok); Assert.False(result.Tokens.IsKnown);
        Assert.Contains("403", result.Detail);
    }
    private sealed class StubRunner : IProcessRunner
    {
        public ProcessSpec? Spec { get; private set; }
        public string? ConfigFile { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            Spec = spec; ConfigFile = spec.Environment!["OPENCODE_CONFIG"];
            Assert.True(File.Exists(ConfigFile)); Assert.Contains("external_directory", File.ReadAllText(ConfigFile));
            return Task.FromResult(new ProcessResult(0, Events, "", TimeSpan.FromMilliseconds(1), false));
        }
    }
}
