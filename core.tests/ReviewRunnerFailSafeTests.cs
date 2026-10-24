using AITeam.Core.Providers;
using AITeam.Core.Review;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// Review V4-08 (Codex phát hiện, Claude sửa): lỗi bất ngờ trong lúc review ⇒ BLOCKED có lý do, không làm sập lệnh.
public sealed class ReviewRunnerFailSafeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v408fs-" + Guid.NewGuid().ToString("N"));

    public ReviewRunnerFailSafeTests() => Directory.CreateDirectory(_root);
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class ThrowingProvider : IAgentProvider
    {
        public int Calls;
        public string Id => "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth("codex", true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) { Calls++; throw new InvalidOperationException("codex.js biến mất"); }
    }

    private (ReviewRunner Runner, ThrowingProvider Codex) Build()
    {
        var providers = new[] { new ProviderConfig { Id = "codex", Kind = ProviderKind.Codex, Exe = "codex.js", Roles = new[] { AgentRole.Reviewer } } };
        var settings = new ProviderSettings(providers, new Dictionary<AgentRole, IReadOnlyList<string>> { [AgentRole.Reviewer] = new[] { "codex" } });
        var state = new ProviderStateStore(_root);
        var codex = new ThrowingProvider();
        return (new ReviewRunner(settings, new ProviderRouter(settings, state), state, _ => codex, () => new DateTime(2026, 10, 24, 9, 0, 0)), codex);
    }

    private ReviewRunInput Input(string logDir)
    {
        var task = Path.Combine(_root, "t.md"); File.WriteAllText(task, "# T");
        var diff = Path.Combine(_root, "d.txt"); File.WriteAllText(diff, "+x");
        return new ReviewRunInput(task, diff, null, _root, logDir, "agy", TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Provider_exception_becomes_blocked_once()
    {
        var (runner, codex) = Build();
        var o = await runner.RunAsync(Input(Path.Combine(_root, "logs")), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.Equal("BLOCKED: lỗi khi review — InvalidOperationException: codex.js biến mất", o.Summary);
        Assert.Equal(1, codex.Calls);   // không thử lại tùy tiện
        Assert.Contains("\"BLOCKED\"", ReviewRunner.OutcomeJson(o));
    }

    [Fact]
    public async Task Log_dir_that_is_a_file_becomes_blocked()
    {
        var (runner, codex) = Build();
        var fileAsDir = Path.Combine(_root, "logs-la-file"); File.WriteAllText(fileAsDir, "x");
        var o = await runner.RunAsync(Input(fileAsDir), CancellationToken.None);
        Assert.Equal(ReviewRunner.ExitBlocked, o.ExitCode);
        Assert.StartsWith("BLOCKED: lỗi khi review — IOException", o.Summary);
        Assert.Equal(0, codex.Calls);
    }
}
