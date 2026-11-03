using System.ComponentModel;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
namespace AITeam.Core.Tests;
public sealed class V914RoutingIntegrationTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"v914-d2-"+Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now=new(2026,11,3,10,0,0,TimeSpan.Zero);
    public V914RoutingIntegrationTests(){Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"team.json"),"{}");File.WriteAllText(Path.Combine(root,"team.local.json"),"{\"providers\":{\"codex\":{\"exe\":\"stub-codex\"}}}");}
    public void Dispose()=>Directory.Delete(root,true);
    private sealed class Probe:IProcessRunner
    {
        public List<ProcessSpec> Calls=[];
        public Task<ProcessResult> RunAsync(ProcessSpec spec,CancellationToken ct)
        {Calls.Add(spec);return Task.FromResult(new ProcessResult(0,spec.FileName=="stub-codex"?"Logged in using ChatGPT":"1.19.5","",TimeSpan.Zero,false));}
    }
    private static DeveloperRoutingSnapshot Snapshot(bool privacy=false)=>new(new(null,privacy,"M",100,["hub/Pages/A.cshtml"]),new(),[],false);
    [Fact]public async Task Public_small_task_chooses_deepseek_and_only_probes_local_cli()
    {
        var probe=new Probe();var selected=await DeveloperRouting.SelectAsync(root,Snapshot(),probe,Now,CancellationToken.None);
        Assert.Equal("opencode",selected.ProviderId);Assert.All(probe.Calls,c=>Assert.DoesNotContain("run",c.Arguments));Assert.DoesNotContain(probe.Calls,c=>c.FileName.Contains("agy",StringComparison.OrdinalIgnoreCase));
    }
    [Fact]public async Task Private_repo_and_live_rest_are_enforced_by_dispatch()
    {
        var state=new ProviderStateStore(root);state.Record(new("codex",RunStatus.NotLoggedIn,"",TokenUsage.Unknown,TimeSpan.Zero,null,null,"Chưa đăng nhập"),Now.LocalDateTime);
        var selected=await DeveloperRouting.SelectAsync(root,Snapshot(true),new Probe(),Now,CancellationToken.None);
        Assert.Null(selected.ProviderId);Assert.Equal("Chờ người làm",selected.Status);Assert.NotNull(selected.RetryAt);
    }
    [Fact]public async Task Load_uses_selected_provider_instead_of_legacy_agent_slot()
    {
        Directory.CreateDirectory(Path.Combine(root,"tasks"));
        File.WriteAllText(Path.Combine(root,"tasks/README.md"),"| T-001 | A | branch | 🟡 Đang làm |\n");
        var first=Snapshot(true) with {ProjectPath=root,TaskId="T-001",Task=Snapshot(true).Task with {Difficulty="L"}};
        Assert.Equal("codex",(await DeveloperRouting.SelectAsync(root,first,new Probe(),Now,CancellationToken.None)).ProviderId);
        var next=Snapshot(true) with {ProjectPath=root,TaskId="T-002",AgyInstalled=true,AgyModels=["argon-4"]};
        Assert.Equal("agy",(await DeveloperRouting.SelectAsync(root,next,new Probe(),Now,CancellationToken.None)).ProviderId);
    }
    [Fact]public async Task Pinned_resting_developer_is_not_silently_changed()
    {
        var snapshot=Snapshot() with {Task=Snapshot().Task with {Dev="agy"}};
        var selected=await DeveloperRouting.SelectAsync(root,snapshot,new Probe(),Now,CancellationToken.None);
        Assert.Null(selected.ProviderId);Assert.Contains("Không đổi",selected.Reason);
    }
}
