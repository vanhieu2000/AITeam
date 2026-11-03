using System.Collections;
using System.Reflection;
using AITeam.Hub.DevData;
using AITeam.Hub.Services;
namespace AITeam.Hub.Tests;
public sealed class H805RetentionTests
{
    [Fact] public void Old_completed_jobs_are_released_from_memory_and_running_jobs_are_kept()
    {
        using var env=new TestEnvironment();var starter=new Starter();var time=new DateTime(2026,11,3);
        var service=new CommandService(env.Paths,new FakeTeamConfigService(),new FakeTaskBoardService(),starter,()=>time);
        var first=service.Start(new CommandRequest(HubCommand.Verify,"AITeam","H-001"));
        starter.Complete=true;
        for(var i=0;i<205;i++){ time=time.AddSeconds(1);service.Start(new CommandRequest(HubCommand.Verify,"AITeam","H-002")); }
        var stored=(IList)typeof(CommandService).GetField("_jobs",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(service)!;
        Assert.Equal(200,stored.Count);Assert.Equal(200,service.Jobs().Count);Assert.Contains(service.Jobs(),j=>j.Id==first.Id&&j.Running);
        starter.FirstExit!(0);Assert.False(Assert.Single(service.Jobs(),j=>j.Id==first.Id).Running);
    }
    private sealed class Starter:IProcessStarter
    {
        public bool Complete;public Action<int>? FirstExit;
        public void Start(string exe,IReadOnlyList<string> args,string workDir,string logFile,Action<int> onExit){if(Complete)onExit(0);else FirstExit=onExit;}
    }
}
