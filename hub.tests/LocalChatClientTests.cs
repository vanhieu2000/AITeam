using AITeam.Core.Process;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class LocalChatClientTests
{
    [Fact]
    public void Codex_arguments_use_read_only_stdin_and_configured_model_without_shell()
    {
        var args = CodexChatClient.Arguments(new ChatSettings { Model = "gpt-example" }, "/tmp/chat space", "/tmp/answer.txt");
        Assert.Contains("read-only", args);
        Assert.Contains("features.shell_tool=false", args);
        Assert.Contains("features.unified_exec=false", args);
        Assert.Contains("windows.sandbox=\"unelevated\"", args);
        Assert.Contains("gpt-example", args);
        Assert.Contains("/tmp/chat space", args);
        Assert.Equal("-", args[^1]);
        Assert.DoesNotContain("--dangerously-bypass-approvals-and-sandbox", args);
    }

    [Fact]
    public void Usage_is_known_only_when_provider_reports_it()
    {
        var result = new ProcessResult(0, "{\"type\":\"turn.completed\",\"usage\":{\"input_tokens\":120,\"cached_input_tokens\":30,\"output_tokens\":12}}", "", TimeSpan.Zero, false);
        var answer = CodexChatClient.Parse(result, "Xin chào");
        Assert.Null(answer.Error);
        Assert.Equal(132, answer.Tokens);
        answer = CodexChatClient.Parse(result with { StdOut = "{\"type\":\"turn.completed\"}" }, "Được rồi");
        Assert.Null(answer.Tokens);
    }

    [Fact]
    public void Cli_errors_do_not_expose_credentials_or_claim_success()
    {
        var raw = "sensitive account information";
        var result = new ProcessResult(1, "{\"type\":\"turn.failed\"}", "401 authentication " + raw, TimeSpan.Zero, false);
        var answer = CodexChatClient.Parse(result, "Không được coi là thành công");
        Assert.Contains("đăng nhập", answer.Error);
        Assert.DoesNotContain(raw, answer.Error);
        Assert.Empty(answer.Text);
        answer = CodexChatClient.Parse(result with { StdErr = "429 quota" }, "");
        Assert.Contains("hạn mức", answer.Error);
    }

    [Fact]
    public async Task Real_client_passes_memory_via_stdin_and_removes_temporary_files()
    {
        var runner = new FakeRunner();
        var client = new CodexChatClient(runner);
        var context = new[] { new ChatMessage(1, "user", "Nhớ tên dự án AITeam", "complete", "", "", null, null) };
        var answer = await client.ReplyAsync(new ChatSettings(), context, CancellationToken.None);
        Assert.Equal("Tôi nhớ AITeam.", answer.Text);
        Assert.Equal(15, answer.Tokens);
        Assert.Contains("AITeam", runner.Prompt);
        Assert.False(Directory.Exists(runner.Directory));
    }

    private sealed class FakeRunner : IProcessRunner
    {
        public string Prompt { get; private set; } = "";
        public string Directory { get; private set; } = "";
        public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            Directory = spec.WorkingDirectory;
            Assert.NotNull(spec.StdinFile);
            Prompt = await File.ReadAllTextAsync(spec.StdinFile, ct);
            var args = spec.Arguments.ToList();
            await File.WriteAllTextAsync(args[args.IndexOf("-o") + 1], "Tôi nhớ AITeam.", ct);
            return new(0, "{\"type\":\"turn.completed\",\"usage\":{\"input_tokens\":10,\"output_tokens\":5}}", "", TimeSpan.Zero, false);
        }
    }
}
