using System.Diagnostics;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

// K1-04 (Claude viết trước): 04/11 12:13 Codex mất kết nối (proxy openai_base_url không chạy, os error 10061) nhưng vẫn in
// "Reconnecting… waiting for network" đều đặn ⇒ bộ canh đứng im K1-02 không bắt được, Codex chạy hết 25 phút giờ cứng.
// Sau sửa: dòng mất kết nối KHÔNG tính là hoạt động; Codex im (không có hoạt động thật) quá IdleTimeout ⇒ dừng, báo rõ.
public sealed class AcceptanceK104CodexNetworkStallTests
{
    // Mẫu THẬT (rút gọn) từ codex exec --json 04/11/2026 khi proxy 127.0.0.1:17841 không chạy.
    private const string RealStdOut = """
        {"type":"thread.started","thread_id":"01a1a565-9f28-7d00-901f-9ba27e978ce8"}
        {"type":"turn.started"}
        {"type":"error","message":"Reconnecting... 2/5 (stream disconnected before completion: No connection could be made because the target machine actively refused it. (os error 10061))"}
        {"type":"item.completed","item":{"id":"item_0","type":"error","message":"Falling back from WebSockets to HTTPS transport. stream disconnected before completion: No connection could be made because the target machine actively refused it. (os error 10061)"}}
        {"type":"error","message":"Reconnecting... waiting for network (Connection failed: error sending request)"}
        """;

    private const string RealStdErr = """
        Reading additional input from stdin...
        2026-11-04T07:11:07.859637Z ERROR codex_models_manager::manager: failed to refresh available models: request timed out
        2026-11-04T07:11:10.439487Z ERROR codex_api::endpoint::responses_websocket: failed to connect to Responses WebSocket stage=Unknown
        """;

    [Theory]
    [InlineData("""{"type":"error","message":"Reconnecting... 2/5 (stream disconnected before completion: No connection could be made because the target machine actively refused it. (os error 10061))"}""")]
    [InlineData("""{"type":"error","message":"Reconnecting... waiting for network (Connection failed: error sending request)"}""")]
    [InlineData("""{"type":"item.completed","item":{"id":"item_0","type":"error","message":"Falling back from WebSockets to HTTPS transport. stream disconnected before completion: x"}}""")]
    [InlineData("2026-11-04T07:11:07.859637Z ERROR codex_models_manager::manager: failed to refresh available models: request timed out")]
    [InlineData("2026-11-04T07:11:10.439487Z ERROR codex_api::endpoint::responses_websocket: failed to connect to Responses WebSocket stage=Unknown")]
    public void Network_retry_lines_are_not_activity(string line) => Assert.False(CodexProvider.IsActivityLine(line));

    [Theory]
    [InlineData("""{"type":"thread.started","thread_id":"01a1"}""")]
    [InlineData("""{"type":"turn.started"}""")]
    [InlineData("""{"type":"item.completed","item":{"id":"item_3","type":"agent_message","text":"Đang sửa team.ps1"}}""")]
    [InlineData("""{"type":"item.started","item":{"id":"item_4","type":"command_execution","command":"dotnet build"}}""")]
    [InlineData("""{"type":"turn.completed","usage":{"input_tokens":1,"output_tokens":1}}""")]
    [InlineData("Reading additional input from stdin...")]
    // Agent nhắc chữ "reconnecting" trong câu trả lời của nó ⇒ vẫn là hoạt động thật (chỉ dòng lỗi / log hệ thống mới bị loại).
    [InlineData("""{"type":"item.completed","item":{"id":"item_5","type":"agent_message","text":"Đã thêm xử lý Reconnecting... waiting for network"}}""")]
    public void Real_work_lines_are_activity(string line) => Assert.True(CodexProvider.IsActivityLine(line));

    private static ProcessSpec Node(string root, string js, TimeSpan idle, Func<string, bool>? isActivity) =>
        new("node", ["-e", js], root, null, TimeSpan.FromSeconds(60)) { IdleTimeout = idle, IsActivity = isActivity };

    // In "Reconnecting" mỗi 300 ms trong 7 giây rồi thoát.
    private const string RetryForSevenSeconds =
        "let n=0;const t=setInterval(()=>{console.log('{\"type\":\"error\",\"message\":\"Reconnecting... waiting for network\"}');if(++n>=23){clearInterval(t);}},300);";

    [Fact]
    public async Task Lines_rejected_by_IsActivity_do_not_reset_idle_clock()
    {
        using var repo = new TempRepo(false);
        var sw = Stopwatch.StartNew();
        var r = await new ProcessRunner().RunAsync(Node(repo.Root, RetryForSevenSeconds.Replace("23", "1000"), TimeSpan.FromSeconds(4), CodexProvider.IsActivityLine), CancellationToken.None);
        Assert.True(r.Stalled, "Chỉ có dòng mất kết nối ⇒ phải bị coi là đứng im");
        Assert.False(r.TimedOut);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), $"Dừng quá chậm: {sw.Elapsed}");
        Assert.Contains("Reconnecting", r.StdOut);
    }

    [Fact]
    public async Task Without_IsActivity_every_line_still_counts()
    {
        using var repo = new TempRepo(false);
        var r = await new ProcessRunner().RunAsync(Node(repo.Root, RetryForSevenSeconds, TimeSpan.FromSeconds(5), null), CancellationToken.None);
        Assert.False(r.Stalled);
        Assert.False(r.TimedOut);
        Assert.Equal(0, r.ExitCode);
    }

    [Fact]
    public void IsActivity_defaults_to_null()
        => Assert.Null(new ProcessSpec("x", [], ".", null, TimeSpan.FromSeconds(1)).IsActivity);

    [Fact]
    public void Stalled_codex_with_network_retries_reports_lost_connection_and_keeps_thread()
    {
        var res = new ProcessResult(-1, RealStdOut, RealStdErr, TimeSpan.FromMinutes(15), false) { Stalled = true };
        var r = CodexProvider.Parse("codex", res, "", "gpt-6.1-sol");
        Assert.Equal(RunStatus.TimedOut, r.Status);
        Assert.Contains("mất kết nối tới máy chủ AI", r.Detail);
        Assert.Contains($"{(int)CodexProvider.IdleTimeout.TotalMinutes} phút", r.Detail);
        Assert.Contains("openai_base_url", r.Detail);
        Assert.Equal("01a1a565-9f28-7d00-901f-9ba27e978ce8", r.ConversationId);
    }

    [Fact]
    public void Stalled_codex_without_network_lines_reports_idle()
    {
        var res = new ProcessResult(-1, """{"type":"thread.started","thread_id":"t1"}""" + "\n", "", TimeSpan.FromMinutes(15), false) { Stalled = true };
        var r = CodexProvider.Parse("codex", res, "", null);
        Assert.Equal(RunStatus.TimedOut, r.Status);
        Assert.Contains("đứng im", r.Detail);
        Assert.DoesNotContain("mất kết nối", r.Detail);
    }

    [Fact]
    public void Hard_timeout_is_unchanged()
    {
        var res = new ProcessResult(-1, RealStdOut, "", TimeSpan.FromMinutes(25), true);
        var r = CodexProvider.Parse("codex", res, "", null);
        Assert.Equal(RunStatus.TimedOut, r.Status);
        Assert.Equal("Quá thời gian", r.Detail);
    }

    [Fact]
    public async Task Codex_run_passes_idle_timeout_and_activity_filter()
    {
        using var repo = new TempRepo(false);
        var prompt = repo.Write("K-4.md", "Task giả.");
        var runner = new CaptureRunner();
        await new CodexProvider(new ProviderConfig { Id = "codex", Exe = "codex.exe" }, runner)
            .RunAsync(new(repo.Root, prompt, RunMode.Develop, TimeSpan.FromMinutes(25)), CancellationToken.None);
        Assert.NotNull(runner.Spec);
        Assert.Equal(CodexProvider.IdleTimeout, runner.Spec!.IdleTimeout);
        Assert.InRange(CodexProvider.IdleTimeout, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20));
        Assert.NotNull(runner.Spec.IsActivity);
        Assert.False(runner.Spec.IsActivity!("""{"type":"error","message":"Reconnecting... waiting for network"}"""));
    }

    [Fact]
    public async Task Codex_health_check_has_no_idle_watchdog()
    {
        var runner = new CaptureRunner();
        await new CodexProvider(new ProviderConfig { Id = "codex", Exe = "codex.exe" }, runner).CheckAsync(CancellationToken.None);
        Assert.Null(runner.Spec!.IdleTimeout);
    }

    // team.ps1 ghi log lượt chạy với error = tóm tắt aiteam run (gồm Detail) ⇒ assign phải báo LỖI MẠNG, không phải hết hạn mức.
    [Fact]
    public async Task Team_ps1_reports_codex_lost_connection_as_network_error()
    {
        using var tmp = new TempRepo(false);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "team.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        static string Q(string v) => "'" + v.Replace("'", "''") + "'";
        var script = $$$"""
            $ErrorActionPreference='Stop'
            . {{{Q(Path.Combine(root!.FullName, "tools/workflow-common.ps1"))}}}
            $tokens=$null; $errors=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile({{{Q(Path.Combine(root.FullName, "team.ps1"))}}},[ref]$tokens,[ref]$errors)
            if ($errors.Count) { throw 'Lỗi cú pháp' }
            $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]}, $true) | ForEach-Object { Invoke-Expression $_.Extent.Text }
            $log = '{"status":"ERROR","provider":"codex","response":"","error":"TimedOut · codex · 900s · token ? — Codex mất kết nối tới máy chủ AI suốt 15 phút (Reconnecting… / waiting for network) — đã dừng. Kiểm tra mạng hoặc openai_base_url trong ~/.codex/config.toml."}'
            if (-not (Test-NetworkError $log '')) { throw 'Codex mất kết nối không được nhận là LỖI MẠNG' }
            if (Test-QuotaExhausted $log '') { throw 'Codex mất kết nối bị báo HẾT HẠN MỨC' }
            $idle = '{"status":"ERROR","provider":"codex","response":"","error":"TimedOut · codex · 900s · token ? — Codex đứng im 15 phút không có hoạt động — đã dừng."}'
            if (Test-NetworkError $idle '') { throw 'Đứng im thường không phải lỗi mạng' }
            'OK-K104'
            """;
        var r = await TestShell.PowerShell(tmp.Root, script, 120);
        Assert.True(r.Code == 0, r.Output + r.Error);
        Assert.Contains("OK-K104", r.Output);
    }

    private sealed class CaptureRunner : IProcessRunner
    {
        public ProcessSpec? Spec { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            Spec = spec;
            return Task.FromResult(new ProcessResult(0, """{"type":"turn.completed","usage":{"input_tokens":1,"output_tokens":1}}""", "Logged in", TimeSpan.FromMilliseconds(1), false));
        }
    }
}
