using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Core.Tests.Support;
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
    // -Resume không gọi route-developer: đọc provider đã ghi trong .state/developer-routes.json rồi đi tiếp đường assign cũ.
    [Fact]public async Task Resume_keeps_recorded_developer_and_does_not_call_router()
    {
        using var sandbox=new TempRepo(false);
        var repo=sandbox.Root;
        Directory.CreateDirectory(Path.Combine(repo,"tasks"));
        File.WriteAllText(Path.Combine(repo,"team.json"),"{}");
        var taskFile=Path.Combine(repo,"tasks","T-001-viec.md");
        File.WriteAllText(taskFile,"- **Agent:** A\r\n- **Nhánh:** `agent-a/T-001-viec`\r\n",new UTF8Encoding(true));
        Directory.CreateDirectory(Path.Combine(repo,".state"));
        var key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(repo).ToUpperInvariant())))+":T-001";
        File.WriteAllText(Path.Combine(repo,".state","developer-routes.json"),"{\""+key+"\":\"codex\"}",new UTF8Encoding(false));
        var runtime=Path.Combine(AppContext.BaseDirectory,"S1-06Runtime","team.ps1");
        var script=("""
            $tokens=$null; $errors=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile('__TEAM__',[ref]$tokens,[ref]$errors)
            if ($errors.Count) { throw ($errors | Out-String) }
            foreach ($fn in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) {
                . ([scriptblock]::Create($fn.Extent.Text))
            }
            $Root='__ROOT__'; $Utf8=New-Object Text.UTF8Encoding $false
            $Project='Demo'; $Task='T-001'; $Resume=$true; $Dev=''; $Feedback=''
            $r=[pscustomobject]@{ Path=$Root; Work=(Join-Path $Root 'work'); Logs=(Join-Path $Root 'logs'); Info=@{name='Demo'} }
            $tf=Get-Item '__TASK__'; $text=Get-Content $tf.FullName -Raw -Encoding UTF8
            Write-Output ('DEV=' + (Get-RecordedDeveloper $r $tf $text))
            function Get-Config { [pscustomobject]@{ developers=@(); agy=[pscustomobject]@{ exe=''; model='' }; github=[pscustomobject]@{ owner='giả' } } }
            function Resolve-Project { $r }
            function Find-TaskFile { $tf }
            function Stop-ReviewReassignment { $false }
            function Select-TaskDeveloper { throw 'ROUTE_CALLED' }
            function Assert-NotClaimedElsewhere { throw 'ASSIGN_PATH' }
            Invoke-Assign
            """).Replace("__TEAM__",runtime.Replace("'","''")).Replace("__ROOT__",repo.Replace("'","''")).Replace("__TASK__",taskFile.Replace("'","''"));
        var result=await TestShell.PowerShell(repo,script);
        Assert.Contains("DEV=codex",result.Output);
        Assert.Contains("ASSIGN_PATH",result.Output+result.Error);
        Assert.DoesNotContain("ROUTE_CALLED",result.Output+result.Error);
    }
}
