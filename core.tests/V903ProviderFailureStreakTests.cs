using AITeam.Core.Providers;
using AITeam.Core.Routing;
namespace AITeam.Core.Tests;
public sealed class V903ProviderFailureStreakTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "v903-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTime Now = new(2026,11,3,10,0,0);
    public V903ProviderFailureStreakTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root,true);
    private static AgentRunResult Run(RunStatus status, string id = "codex") => new(id,status,"",TokenUsage.Unknown,TimeSpan.Zero,null,null,"Lỗi tiến trình giả");
    [Fact] public void Third_failure_rests_provider_across_store_instances_and_router_excludes_it()
    {
        for(var i=0;i<2;i++) { new ProviderStateStore(root).Record(Run(RunStatus.Failed), Now.AddMinutes(i)); Assert.Null(new ProviderStateStore(root).RestOf("codex", Now.AddMinutes(i))); }
        new ProviderStateStore(root).Record(Run(RunStatus.Failed,"CODEX"),Now.AddMinutes(2));
        var rest = new ProviderStateStore(root).RestOf("codex", Now.AddMinutes(2));
        Assert.NotNull(rest); Assert.Contains("3 lỗi liên tiếp",rest.Detail); Assert.Equal(Now.AddMinutes(17),rest.Until);
        var route = new ProviderRouter(ProviderSettings.Default("unused"), new ProviderStateStore(root)).Pick(AgentRole.Developer, null, Now.AddMinutes(2));
        Assert.Equal("agy", route.ProviderId);
    }
    [Fact] public void Success_or_manual_clear_resets_streak()
    {
        var store = new ProviderStateStore(root);
        store.Record(Run(RunStatus.Failed),Now); store.Record(Run(RunStatus.Failed),Now);
        store.Record(Run(RunStatus.Succeeded),Now);
        store.Record(Run(RunStatus.Failed),Now); Assert.Null(store.RestOf("codex",Now));
        store.Clear("codex"); store.Record(Run(RunStatus.Failed),Now); store.Record(Run(RunStatus.Failed),Now);
        Assert.Null(store.RestOf("codex",Now));
    }
    private sealed class FailingProvider : IAgentProvider
    {
        public int Calls { get; private set; }
        public string Id => "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(Id, true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) { Calls++; return Task.FromResult(Run(RunStatus.Failed)); }
    }
    [Fact] public async Task Developer_gets_warning_then_next_run_is_blocked_without_calling_provider()
    {
        var settings = ProviderSettings.Default("unused"); var provider = new FailingProvider();
        var runner = new DevRunner(settings, new ProviderStateStore(root), _ => provider, () => Now);
        var prompt = Path.Combine(root,"prompt.md"); File.WriteAllText(prompt,"Không gọi AI thật");
        var input = new DevRunInput("codex",root,prompt,Path.Combine(root,"logs"),TimeSpan.FromMinutes(1));
        await runner.RunAsync(input,CancellationToken.None); await runner.RunAsync(input,CancellationToken.None);
        var third = await runner.RunAsync(input,CancellationToken.None);
        Assert.Contains("CẢNH BÁO",third.Summary);
        Assert.Equal(DevRunner.ExitResting,(await runner.RunAsync(input,CancellationToken.None)).ExitCode);
        Assert.Equal(3,provider.Calls);
    }
    [Fact] public void Corrupt_counter_is_preserved_and_reported()
    {
        Directory.CreateDirectory(Path.Combine(root,".state")); var file=Path.Combine(root,".state/provider-failures.json"); File.WriteAllText(file,"{ hỏng");
        Assert.Throws<System.Text.Json.JsonException>(()=>new ProviderStateStore(root).Record(Run(RunStatus.Failed),Now));
        Assert.Equal("{ hỏng",File.ReadAllText(file));
    }
}
