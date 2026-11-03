using System.Net;
using System.Text;
using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Process;
using AITeam.Core.Providers;

namespace AITeam.Core.Tests;

// Test nghiệm thu V10-02 (Claude): chạy AI theo danh bạ — factory + 3 adapter chung. KHÔNG sửa file này.
public sealed class V1002AiAdapterTests : IDisposable
{
    private const string FakeKey = "fake-test-value-0001";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aiteam-v1002-" + Guid.NewGuid().ToString("N"));

    public V1002AiAdapterTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static AiService Svc(string id, AiAdapter adapter, string? command = "mycli", IReadOnlyList<string>? args = null,
        string? baseUrl = null, AiRole[]? roles = null, bool enabled = true, string? keyName = null) =>
        new(id, "Dịch vụ " + id, adapter, command, args ?? [], baseUrl, ["model-x"], roles ?? [AiRole.Chat],
            1, false, keyName, null, enabled, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class Runner(Func<ProcessSpec, ProcessResult> run) : IProcessRunner
    {
        public List<ProcessSpec> Specs { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            Specs.Add(spec);
            return Task.FromResult(run(spec));
        }
    }

    private sealed class Secrets : ISecretLookup
    {
        public string? Get(string name) => name == "FAKE_KEY_NAME" ? FakeKey : null;
    }

    private sealed class Http(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct));
            return reply(request);
        }
    }

    private static ProcessResult Ok(string stdout) => new(0, stdout, "", TimeSpan.FromMilliseconds(5), false);

    private AgentRunRequest Request(RunMode mode = RunMode.ReadOnly)
    {
        var prompt = Path.Combine(_dir, "prompt.md");
        File.WriteAllText(prompt, "Câu hỏi thử", new UTF8Encoding(true));
        return new AgentRunRequest(_dir, prompt, mode, TimeSpan.FromSeconds(30));
    }

    private SqliteAiServiceRegistry Registry() =>
        new(Path.Combine(_dir, "hub", "office.db"), Path.Combine(_dir, "root"));

    private AiProviderFactory Factory(IProcessRunner? runner = null, HttpMessageHandler? http = null) =>
        new(Registry(), runner ?? new Runner(_ => Ok("")), new Secrets(), http);

    [Fact]
    public void Factory_maps_known_ids_to_dedicated_providers_and_others_by_adapter()
    {
        var factory = Factory();
        Assert.IsType<CodexProvider>(factory.Create(Svc("codex", AiAdapter.CliJson, "codex")));
        Assert.IsType<OpenCodeProvider>(factory.Create(Svc("opencode", AiAdapter.CliJson, "opencode")));
        Assert.IsType<AgyProvider>(factory.Create(Svc("antigravity", AiAdapter.CliJson, "agy")));
        Assert.IsType<AgyProvider>(factory.Create(Svc("agy", AiAdapter.CliJson, "agy")));

        var text = factory.Create(Svc("mycli", AiAdapter.CliText));
        Assert.IsType<CliTextProvider>(text);
        Assert.Equal("mycli", text.Id);
        Assert.Equal(ProviderKind.Generic, text.Kind);
        Assert.IsType<CliJsonProvider>(factory.Create(Svc("jsoncli", AiAdapter.CliJson)));
        Assert.IsType<OpenAiHttpProvider>(factory.Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "https://api.example.test/v1")));
    }

    [Fact]
    public void Factory_rejects_disabled_service_and_filters_by_role()
    {
        var factory = Factory();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.Create(Svc("tat", AiAdapter.CliText, enabled: false)));
        Assert.Contains("đang tắt", ex.Message);

        var registry = Registry();
        registry.Upsert(Svc("chat-a", AiAdapter.CliText, roles: [AiRole.Chat]));
        registry.Upsert(Svc("code-b", AiAdapter.CliText, roles: [AiRole.Code]));
        registry.Upsert(Svc("chat-off", AiAdapter.CliText, roles: [AiRole.Chat], enabled: false));
        var chat = new AiProviderFactory(registry, new Runner(_ => Ok("")), new Secrets()).ForRole(AiRole.Chat).Select(s => s.Id).ToList();
        Assert.Contains("chat-a", chat);
        Assert.DoesNotContain("code-b", chat);
        Assert.DoesNotContain("chat-off", chat);
    }

    [Fact]
    public async Task CliText_substitutes_placeholders_inside_each_argument_without_shell()
    {
        var runner = new Runner(_ => Ok("Trả lời thử"));
        var provider = Factory(runner).Create(Svc("mycli", AiAdapter.CliText, args: ["run", "{prompt_file}", "--m", "{model}", "--dir={workdir}"]));
        var request = Request();
        var result = await provider.RunAsync(request, CancellationToken.None);

        Assert.True(result.Ok, result.Detail);
        Assert.Equal("Trả lời thử", result.Text.Trim());
        var spec = Assert.Single(runner.Specs);
        Assert.Equal("mycli", spec.FileName);
        Assert.Equal(["run", request.PromptFile, "--m", "model-x", "--dir=" + _dir], spec.Arguments);
        Assert.DoesNotContain(spec.FileName, new[] { "cmd", "cmd.exe", "powershell", "powershell.exe", "sh", "bash" });
    }

    [Fact]
    public async Task CliJson_joins_text_lines_reads_tokens_and_skips_garbage()
    {
        var stdout = "{\"type\":\"text\",\"text\":\"Xin \"}\nkhông phải json\n{\"type\":\"text\",\"part\":{\"text\":\"chào\"}}\n{\"type\":\"usage\",\"tokens\":{\"input\":12,\"output\":7}}\n";
        var provider = Factory(new Runner(_ => Ok(stdout))).Create(Svc("jsoncli", AiAdapter.CliJson, args: ["{prompt_file}"]));
        var result = await provider.RunAsync(Request(), CancellationToken.None);

        Assert.True(result.Ok, result.Detail);
        Assert.Equal("Xin chào", result.Text);
        Assert.Equal(12, result.Tokens.Input);
        Assert.Equal(7, result.Tokens.Output);
    }

    [Fact]
    public async Task Cli_free_usage_limit_in_stderr_is_quota_exhausted()
    {
        var runner = new Runner(_ => new ProcessResult(1, "", "AI_APICallError: Free usage limit reached.", TimeSpan.FromSeconds(1), false));
        var result = await Factory(runner).Create(Svc("mycli", AiAdapter.CliText)).RunAsync(Request(), CancellationToken.None);
        Assert.Equal(RunStatus.QuotaExhausted, result.Status);
    }

    [Fact]
    public async Task Cli_command_with_shell_characters_is_config_error_and_never_runs()
    {
        var runner = new Runner(_ => Ok("không được chạy"));
        var result = await Factory(runner).Create(Svc("bad", AiAdapter.CliText, "codex & del x")).RunAsync(Request(), CancellationToken.None);
        Assert.Equal(RunStatus.ConfigError, result.Status);
        Assert.Empty(runner.Specs);
    }

    [Fact]
    public async Task Http_success_sends_bearer_and_redacts_key_echoed_by_server()
    {
        var http = new Http(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Khóa của anh là " + FakeKey + "\"}}],\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4}}", Encoding.UTF8, "application/json"),
        });
        var provider = Factory(http: http).Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "https://api.example.test/v1", keyName: "FAKE_KEY_NAME"));
        var result = await provider.RunAsync(Request(), CancellationToken.None);

        Assert.True(result.Ok, result.Detail);
        var req = Assert.Single(http.Requests);
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("https://api.example.test/v1/chat/completions", req.RequestUri!.ToString());
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
        Assert.Equal(FakeKey, req.Headers.Authorization.Parameter);
        Assert.Contains("Câu hỏi thử", http.Bodies[0]);
        Assert.Contains("model-x", http.Bodies[0]);
        Assert.DoesNotContain(FakeKey, result.Text);
        Assert.DoesNotContain(FakeKey, result.Detail);
        Assert.Contains("***", result.Text);
        Assert.Equal(9, result.Tokens.Input);
        Assert.Equal(4, result.Tokens.Output);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, RunStatus.QuotaExhausted)]
    [InlineData(HttpStatusCode.Unauthorized, RunStatus.NotLoggedIn)]
    [InlineData(HttpStatusCode.Forbidden, RunStatus.NotLoggedIn)]
    public async Task Http_status_codes_are_classified(HttpStatusCode code, RunStatus expected)
    {
        var http = new Http(_ => new HttpResponseMessage(code) { Content = new StringContent("{\"error\":\"x " + FakeKey + "\"}") });
        var result = await Factory(http: http).Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "https://api.example.test/v1", keyName: "FAKE_KEY_NAME"))
            .RunAsync(Request(), CancellationToken.None);
        Assert.Equal(expected, result.Status);
        Assert.DoesNotContain(FakeKey, result.Detail);
        Assert.DoesNotContain(FakeKey, result.Text);
    }

    [Fact]
    public async Task Http_does_not_follow_redirect_to_other_host()
    {
        var http = new Http(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.Redirect);
            r.Headers.Location = new Uri("https://evil.example.test/steal");
            return r;
        });
        var result = await Factory(http: http).Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "https://api.example.test/v1", keyName: "FAKE_KEY_NAME"))
            .RunAsync(Request(), CancellationToken.None);
        Assert.False(result.Ok);
        Assert.Single(http.Requests);
        Assert.DoesNotContain(http.Requests, r => r.RequestUri!.Host.Contains("evil", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Http_rejects_plain_http_to_remote_host_and_develop_mode()
    {
        var http = new Http(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var remote = await Factory(http: http).Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "http://evil.example.test/v1"))
            .RunAsync(Request(), CancellationToken.None);
        Assert.Equal(RunStatus.ConfigError, remote.Status);

        var develop = await Factory(http: http).Create(Svc("api", AiAdapter.OpenAiCompatibleHttp, null, baseUrl: "https://api.example.test/v1"))
            .RunAsync(Request(RunMode.Develop), CancellationToken.None);
        Assert.Equal(RunStatus.ConfigError, develop.Status);
        Assert.Empty(http.Requests);
    }
}
