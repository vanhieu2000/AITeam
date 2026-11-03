using System.Text.Json;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
namespace AITeam.Hub.Tests;
public sealed class H810OverviewTests
{
    private sealed class Config(ProjectInfo project):ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects()=>[project];public ProjectInfo? FindProject(string name)=>name==project.Name?project:null;
        public TeamSettings GetSettings()=>new("",3,[],"","","",[],"");public void SaveSettings(TeamSettings s)=>throw new NotSupportedException();public void SaveProject(ProjectInfo p)=>throw new NotSupportedException();
    }
    private sealed class Board(ProjectInfo project,IReadOnlyList<TaskCard> tasks):ITaskBoardService
    {public ProjectBoard GetBoard(string name)=>new(project,tasks,"");public TaskDetail? GetTask(string project,string id)=>null;}
    private sealed class History:IGitHistory {public DateTime? MergedAt(string repo,string task)=>DateTime.Now.AddDays(-1);}
    [Fact]public async Task Ten_board_states_metrics_owner_attention_and_filtered_kanban_are_real_data()
    {
        using var env=new TestEnvironment();var path=Path.Combine(env.RootDir,"P");Directory.CreateDirectory(Path.Combine(path,"tasks"));
        var project=new ProjectInfo("P","owner/P",path,path,"","","",new Dictionary<string,string>(),true);
        var states=Enum.GetValues<OfficeTaskStatus>();var tasks=states.Select((s,i)=>new TaskCard("P",$"T-{i+1:D3}","Tiêu đề <script>bad</script>","C","branch",TaskState.Unknown,"task.md",0,null)).ToList();
        File.WriteAllText(Path.Combine(path,"tasks/README.md"),string.Join('\n',states.Select((s,i)=>$"| T-{i+1:D3} | C | branch | {WorkOverviewService.Label(s)} |")));
        var metrics=Path.Combine(env.AITeamRoot,"metrics/runs");Directory.CreateDirectory(metrics);
        File.WriteAllText(Path.Combine(metrics,"review.json"),JsonSerializer.Serialize(new{project="P",task="T-005",run="1",at=DateTimeOffset.Now.AddHours(-1).ToString("O"),durationSec=90,totalTokens=0,tokensKnown=false,flags=new[]{"TO"}}));
        File.WriteAllText(Path.Combine(metrics,"paused.json"),JsonSerializer.Serialize(new{project="P",task="T-008",run=1,at=DateTimeOffset.Now.AddHours(-30).ToString("O")}));
        await using var host=await HubTestHost.StartAsync(env,s=>{s.AddSingleton<ITeamConfigService>(new Config(project));s.AddSingleton<ITaskBoardService>(new Board(project,tasks));s.AddSingleton<IGitHistory>(new History());});
        var snapshot=host.Services.GetRequiredService<WorkOverviewService>().Read();Assert.Equal(10,snapshot.Tasks.Count);Assert.All(states,s=>Assert.Single(snapshot.Tasks,t=>t.Status==s));
        var review=Assert.Single(snapshot.Tasks,t=>t.Status==OfficeTaskStatus.Review);Assert.Equal(1.5,review.Minutes);Assert.Contains("TO",review.Flags);
        Assert.Null(Assert.Single(HubMetricReader.Read(env.Paths).Runs,r=>r.Task=="T-005").Tokens);
        var page=new AITeam.Hub.Pages.WorkModel(host.Services.GetRequiredService<WorkOverviewService>());page.OnGet("P",null);Assert.Equal(2,page.NeedsOwner.Count);Assert.Single(page.Column(4));
        var html=await host.Client.GetStringAsync("/Work?project=P");Assert.DoesNotContain("<script>bad</script>",html);Assert.Equal(5,System.Text.RegularExpressions.Regex.Matches(html,"class=\"work-column\"").Count);
        Assert.Equal(System.Net.HttpStatusCode.NotFound,(await host.Client.GetAsync("/Work?state=999")).StatusCode);
    }
    [Fact]public void Continued_runs_use_deltas_without_turning_missing_measurements_into_zero()
    {
        using var env=new TestEnvironment();var folder=Path.Combine(env.AITeamRoot,"metrics/runs");Directory.CreateDirectory(folder);
        for(var i=1;i<=2;i++)File.WriteAllText(Path.Combine(folder,i+".json"),JsonSerializer.Serialize(new{project="P",task="T-001",run=i,at=DateTimeOffset.Now.AddMinutes(i).ToString("O"),durationSec=i*60,totalTokens=i*100,conversationId="same"}));
        var runs=HubMetricReader.Read(env.Paths).Runs;Assert.Equal(new long?[]{100,100},runs.Select(r=>r.Tokens));Assert.Equal(new double?[]{60,60},runs.Select(r=>r.Seconds));
    }
    [Fact]public void Reader_rejects_path_escape_and_keeps_unknown_tokens_unknown()
    {
        using var env=new TestEnvironment();Assert.Throws<InvalidDataException>(()=>HubMetricReader.ReadText(env.AITeamRoot,"../outside"));
        Assert.Empty(HubMetricReader.Read(env.Paths).Runs);
    }
}
