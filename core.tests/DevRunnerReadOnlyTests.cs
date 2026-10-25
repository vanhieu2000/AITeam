using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// R-006 (Claude): bước lập kế hoạch chạy Codex CHỈ ĐỌC qua `aiteam run --read-only`.
public sealed class DevRunnerReadOnlyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-ro-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class Fake : IAgentProvider
    {
        public readonly List<AgentRunRequest> Requests = new();
        public string Id => "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<AgentRunResult> RunAsync(AgentRunRequest r, CancellationToken ct)
        {
            Requests.Add(r);
            return Task.FromResult(new AgentRunResult("codex", RunStatus.Succeeded, "kế hoạch", TokenUsage.Unknown, TimeSpan.FromSeconds(3), null, null, ""));
        }
    }

    [Theory]
    [InlineData(RunMode.ReadOnly)]
    [InlineData(RunMode.Develop)]
    public async Task Mode_is_passed_to_provider(RunMode mode)
    {
        var work = Directory.CreateDirectory(Path.Combine(_root, "w")).FullName;
        var prompt = Path.Combine(_root, "p.md");
        File.WriteAllText(prompt, "x");
        var fake = new Fake();
        var settings = new ProviderSettings(
            new[] { new ProviderConfig { Id = "codex", Kind = ProviderKind.Codex, Exe = "codex.js", Roles = new[] { AgentRole.Developer } } },
            new Dictionary<AgentRole, IReadOnlyList<string>>());
        var runner = new DevRunner(settings, new ProviderStateStore(_root), _ => fake, () => DateTime.Now);

        var o = await runner.RunAsync(new DevRunInput("codex", work, prompt, Path.Combine(_root, "logs"), TimeSpan.FromMinutes(5), null, mode), CancellationToken.None);

        Assert.Equal(DevRunner.ExitOk, o.ExitCode);
        Assert.Equal(mode, Assert.Single(fake.Requests).Mode);
    }

    [Fact]
    public void Default_mode_is_develop() => Assert.Equal(RunMode.Develop, new DevRunInput("codex", "w", "p", "l", TimeSpan.FromMinutes(1)).Mode);
}
