using System.Diagnostics;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

// K1-04 (Claude 04/11): nới đứng im 1,5 → 5 giây ở 2 test "có dòng đều" — máy bận thì node khởi động > 1,5 giây ⇒ chập chờn.
// K1-02 (Claude viết trước): OpenCode free hết lượt thì đứng im, không báo lỗi ⇒ ProcessRunner tự dừng khi không ra dòng nào quá IdleTimeout.
public sealed class AcceptanceK102IdleWatchdogTests
{
    private static ProcessSpec Node(string root, string js, TimeSpan timeout, TimeSpan? idle) =>
        new("node", ["-e", js], root, null, timeout) { IdleTimeout = idle };

    [Fact]
    public async Task Silent_process_is_killed_after_idle_timeout_and_marked_stalled()
    {
        using var repo = new TempRepo(false);
        var sw = Stopwatch.StartNew();
        // In 1 dòng rồi im 60 giây (giống OpenCode hết lượt free).
        var r = await new ProcessRunner().RunAsync(
            Node(repo.Root, "console.log('{\"type\":\"step_start\"}');setTimeout(()=>{},60000);", TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(2)),
            CancellationToken.None);
        Assert.True(r.Stalled);
        Assert.False(r.TimedOut);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(20), $"Dừng quá chậm: {sw.Elapsed}");
        Assert.Contains("step_start", r.StdOut);
    }

    [Fact]
    public async Task Process_that_keeps_printing_is_not_stalled_even_if_longer_than_idle_timeout()
    {
        using var repo = new TempRepo(false);
        // In mỗi 400 ms trong ~3,2 giây (dài hơn IdleTimeout 1,5 giây) rồi thoát 0.
        var r = await new ProcessRunner().RunAsync(
            Node(repo.Root, "let n=0;const t=setInterval(()=>{console.log('dong '+n);if(++n>=8){clearInterval(t);}},400);", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            CancellationToken.None);
        Assert.False(r.Stalled);
        Assert.False(r.TimedOut);
        Assert.Equal(0, r.ExitCode);
        Assert.Contains("dong 7", r.StdOut);
    }

    [Fact]
    public async Task Stderr_output_also_counts_as_activity()
    {
        using var repo = new TempRepo(false);
        var r = await new ProcessRunner().RunAsync(
            Node(repo.Root, "let n=0;const t=setInterval(()=>{console.error('e'+n);if(++n>=8){clearInterval(t);}},400);", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            CancellationToken.None);
        Assert.False(r.Stalled);
        Assert.Equal(0, r.ExitCode);
    }

    [Fact]
    public async Task Without_idle_timeout_behaviour_is_unchanged_and_hard_timeout_still_wins()
    {
        using var repo = new TempRepo(false);
        var r = await new ProcessRunner().RunAsync(
            Node(repo.Root, "setTimeout(()=>{},60000);", TimeSpan.FromSeconds(2), null),
            CancellationToken.None);
        Assert.True(r.TimedOut);
        Assert.False(r.Stalled);
        Assert.Null(new ProcessSpec("x", [], ".", null, TimeSpan.FromSeconds(1)).IdleTimeout);
        Assert.False(new ProcessResult(0, "", "", TimeSpan.Zero, false).Stalled);
    }

    [Fact]
    public async Task Caller_cancellation_still_throws_and_is_not_reported_as_stall()
    {
        using var repo = new TempRepo(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ProcessRunner().RunAsync(
            Node(repo.Root, "setTimeout(()=>{},60000);", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10)), cts.Token));
    }

    [Fact]
    public void Stalled_opencode_run_is_timed_out_with_clear_vietnamese_detail()
    {
        var stalled = new ProcessResult(-1, "{\"type\":\"step_start\",\"sessionID\":\"ses_abc\"}\n", "", TimeSpan.FromMinutes(21), false) { Stalled = true };
        var r = OpenCodeProvider.Parse("opencode", stalled, OpenCodeProvider.DefaultModel);
        Assert.Equal(RunStatus.TimedOut, r.Status);
        Assert.False(r.Ok);
        Assert.Contains("đứng im", r.Detail);
        Assert.Contains(((int)OpenCodeProvider.IdleTimeout.TotalMinutes).ToString(), r.Detail);
    }

    [Fact]
    public async Task OpenCode_run_passes_idle_timeout_to_runner()
    {
        using var repo = new TempRepo(false);
        var permissions = RealFile.Copy("templates/opencode-quyen-han.json", repo.Root);
        var prompt = repo.Write("K-1.md", "Task giả.");
        var runner = new CaptureRunner();
        var cfg = new ProviderConfig { Id = "opencode", Exe = "opencode", Model = OpenCodeProvider.DefaultModel, PrivateRepository = false, PermissionsFile = permissions };
        await new OpenCodeProvider(cfg, runner).RunAsync(new(repo.Root, prompt, RunMode.Develop, TimeSpan.FromMinutes(90)) { Title = "K-1" }, CancellationToken.None);
        Assert.NotNull(runner.Spec);
        Assert.Equal(OpenCodeProvider.IdleTimeout, runner.Spec!.IdleTimeout);
        Assert.InRange(OpenCodeProvider.IdleTimeout, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30));
    }

    private sealed class CaptureRunner : IProcessRunner
    {
        public ProcessSpec? Spec { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            Spec = spec;
            return Task.FromResult(new ProcessResult(0, "{\"type\":\"text\",\"part\":{\"text\":\"Xong\"}}", "", TimeSpan.FromMilliseconds(1), false));
        }
    }
}
