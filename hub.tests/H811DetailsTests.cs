using System.Text.Json;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
namespace AITeam.Hub.Tests;
public sealed class H811DetailsTests
{
    private sealed class Config(ProjectInfo p):ITeamConfigService
    {public IReadOnlyList<ProjectInfo> GetProjects()=>[p];public ProjectInfo? FindProject(string name)=>name==p.Name?p:null;public TeamSettings GetSettings()=>new("",3,[],"","","",[],"");public void SaveSettings(TeamSettings s)=>throw new NotSupportedException();public void SaveProject(ProjectInfo p)=>throw new NotSupportedException();}
    private sealed class Board(ProjectInfo p,TaskDetail detail):ITaskBoardService
    {public ProjectBoard GetBoard(string name)=>new(p,[detail.Card],"");public TaskDetail? GetTask(string project,string id)=>id==detail.Card.Id?detail:null;}
    private sealed class Runs(AgentRun run):IAgentRunService
    {public IReadOnlyList<AgentRun> GetRuns(string project,string? taskId=null)=>project==run.Project&&(taskId==null||taskId==run.Task)?[run]:[];public IReadOnlyList<AgentRun> GetRecentRuns(int count=20)=>[run];public IReadOnlyList<RunningAgent> GetRunning()=>[];public bool Stop(int pid)=>false;}
    [Fact]public async Task Request_task_and_run_links_show_real_totals_ten_steps_and_encoded_tail()
    {
        using var env=new TestEnvironment();var projectRoot=Path.Combine(env.RootDir,"P");var work=Path.Combine(env.RootDir,"work");Directory.CreateDirectory(Path.Combine(projectRoot,"tasks"));Directory.CreateDirectory(Path.Combine(work,"logs"));Directory.CreateDirectory(Path.Combine(env.AITeamRoot,"requests"));Directory.CreateDirectory(Path.Combine(env.AITeamRoot,"metrics/runs"));
        File.WriteAllText(Path.Combine(projectRoot,"tasks/T-001.md"),"# T-001 · Việc\n- **Yêu cầu:** R-001\n");
        File.WriteAllText(Path.Combine(env.AITeamRoot,"requests/R-001-demo.md"),"# R-001 · Yêu cầu thật tạm\n- **Trạng thái:** Đang làm\n- **Lời chủ dự án:**\n> Nội dung <script>bad</script>\n\n## 2. Chia việc\n| Task | Dự án | Ai |\n|---|---|---|\n| T-001 | P | C |\n## 3. Nhật ký\n| Giờ | Sự kiện | Ghi chú |\n|---|---|---|\n| 09:00 | Tiếp nhận | Đã nhận |\n");
        File.WriteAllText(Path.Combine(env.AITeamRoot,"metrics/runs/run.json"),JsonSerializer.Serialize(new{project="P",task="T-001",run=2,at=DateTimeOffset.Now.ToString("O"),durationSec=120,totalTokens=123,provider="codex",model="model-test",verifyOk=false,flags=new[]{"TN"},linesAdded=5,linesRemoved=2}));
        var log=Path.Combine(work,"logs/T-001-2.json");File.WriteAllText(log,string.Join('\n',Enumerable.Range(1,70).Select(i=>i==70?"<script>tail</script>":"dòng "+i)));
        var run=new AgentRun("P","T-001",2,RunState.Success,"SUCCESS",null,"Báo cáo **đã làm** <script>report</script>",120,123,DateTime.Now,log);
        var project=new ProjectInfo("P","owner/P",projectRoot,work,"","","",new Dictionary<string,string>(),true);
        var card=new TaskCard("P","T-001","Việc","C","branch",TaskState.Review,"T-001.md",1,run);
        var detail=new TaskDetail(card,[new("Review (Claude)","### Vòng 1 — CẦN SỬA\n- [TN] (test) Harness cần sửa")],[],[run]);
        await using var host=await HubTestHost.StartAsync(env,s=>{s.AddSingleton<ITeamConfigService>(new Config(project));s.AddSingleton<ITaskBoardService>(new Board(project,detail));s.AddSingleton<IAgentRunService>(new Runs(run));});
        var service=host.Services.GetRequiredService<WorkDetailService>();var request=service.Request("R-001")!;Assert.Equal(10,request.Evidence.Count);Assert.Equal(7,request.CurrentStep);Assert.Single(request.Tasks);Assert.Equal("TN",Assert.Single(request.Tasks[0].Findings).Cause);
        var data=service.Run("P","T-001",2)!;Assert.Equal(123,data.Metric!.Tokens);Assert.Equal(50,data.LogTail.Split('\n').Length);Assert.DoesNotContain("dòng 20\n",data.LogTail);
        var html=await host.Client.GetStringAsync("/Run?project=P&task=T-001&n=2");Assert.DoesNotContain("<script>tail</script>",html);Assert.DoesNotContain("<script>report</script>",html);Assert.Contains("123 token",html);Assert.Contains("Không đạt",html);
        html=await host.Client.GetStringAsync("/Request?id=R-001");Assert.DoesNotContain("<script>bad</script>",html);Assert.Contains("/p/P/t/T-001",html);
        Assert.Equal(System.Net.HttpStatusCode.NotFound,(await host.Client.GetAsync("/Run?task=..%2Fescape&n=2")).StatusCode);
    }
}
