using System.Text.RegularExpressions;
using AITeam.Core.Metrics;
using AITeam.Core.Research;
using AITeam.Core.Tasks;
using AITeam.Hub.Models;
namespace AITeam.Hub.Services;
public sealed record RunDetailView(string Project,string Task,int Number,HubMetric? Metric,AgentRun? Agent,string LogTail,string Report,IReadOnlyList<string> Warnings);
public sealed record RequestTaskView(TaskCard Card,TaskDetail? Detail,IReadOnlyList<StructuredFinding> Findings);
public sealed record RequestDetailView(RequestThread Request,IReadOnlyList<RequestTaskView> Tasks,IReadOnlyList<HubMetric> Runs,int CurrentStep,IReadOnlyList<bool> Evidence,IReadOnlyList<string> Warnings);
public sealed class WorkDetailService(HubPaths paths,ITeamConfigService config,ITaskBoardService boards,IAgentRunService agentRuns)
{
    public RunDetailView? Run(string? project,string task,int n)
    {
        if(!Regex.IsMatch(task,@"\A[A-Za-z][A-Za-z0-9]*-\d+(?:-[A-Za-z0-9]+)*\z")||n<1)return null;
        var code=HubMetricReader.TaskCode(task);var metrics=HubMetricReader.Read(paths);var warnings=metrics.Warnings.ToList();
        var options=new List<(ProjectInfo Project,List<HubMetric> Metrics,AgentRun? Log)>();
        foreach(var p in config.GetProjects().Where(p=>project==null||p.Name.Equals(project,StringComparison.OrdinalIgnoreCase)))
        {
            var found=metrics.Runs.Where(r=>r.Project.Equals(p.Name,StringComparison.OrdinalIgnoreCase)&&r.Task==code&&r.Run==n).OrderByDescending(r=>r.At).ToList();
            var log=agentRuns.GetRuns(p.Name,task).FirstOrDefault(r=>r.Attempt==n);
            if(found.Count>0||log!=null)options.Add((p,found,log));
        }
        if(options.Count==0)return null;
        if(options.Count>1)throw new InvalidDataException("Mã lượt trùng giữa các dự án; hãy chọn dự án từ trang task.");
        var selected=options[0];if(selected.Metrics.Count>1)warnings.Add("Có nhiều bản ghi cùng lượt; đang hiển thị bản mới nhất.");
        var tail="";
        if(selected.Log!=null&&Environment.GetEnvironmentVariable("HUB_FAKE")!="1")
        {
            try
            {
                if(File.Exists(selected.Log.LogPath))
                {
                    var relative=Path.GetRelativePath(selected.Project.WorkDir,selected.Log.LogPath);
                    var text=HubMetricReader.ReadText(selected.Project.WorkDir,relative);
                    tail=SecretRedactor.Redact(string.Join('\n',(text??"").Replace("\r\n","\n").Split('\n').TakeLast(50))).Text;
                }
            }catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException){warnings.Add("Không đọc được log an toàn trên máy này (giới hạn 2 MiB, UTF-8).");}
        }
        return new(selected.Project.Name,code,n,selected.Metrics.FirstOrDefault(),selected.Log,tail,SecretRedactor.Redact(selected.Log?.Response??"").Text,warnings);
    }
    public RequestDetailView? Request(string id)
    {
        if(!Regex.IsMatch(id,@"\AR-\d{3}\z"))return null;
        var folder=Path.Combine(paths.AITeamRoot,"requests");
        if(!Directory.Exists(folder))return null;
        for(string? p=folder;p!=null;p=Directory.GetParent(p)?.FullName)
            if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Không đọc phiếu qua symlink/reparse point.");
        var candidates=Directory.EnumerateFiles(folder,id+"*.md").Take(2).ToArray();
        if(candidates.Length>1)throw new InvalidDataException("Có nhiều phiếu cùng mã yêu cầu.");
        if(candidates.Length==0)return null;
        var request=RequestParser.Parse(Path.GetFileName(candidates[0]),HubMetricReader.ReadText(paths.AITeamRoot,"requests/"+Path.GetFileName(candidates[0]))!);if(request==null)return null;
        var tasks=new List<RequestTaskView>();var warnings=new List<string>();var metrics=HubMetricReader.Read(paths);warnings.AddRange(metrics.Warnings);
        foreach(var project in config.GetProjects())
        {
            try
            {
                foreach(var card in boards.GetBoard(project.Name).Tasks)
                {
                    var linked=request.Tasks.Any(t=>HubMetricReader.TaskCode(t.Code)==HubMetricReader.TaskCode(card.Id)&&t.Project.Equals(project.Name,StringComparison.OrdinalIgnoreCase));
                    if(!linked&&!string.IsNullOrWhiteSpace(card.FileName))
                    {
                        var source=HubMetricReader.ReadText(project.Path,"tasks/"+card.FileName);
                        linked=source!=null&&TaskFile.Parse(card.FileName,source).Request==id;
                    }
                    if(!linked)continue;
                    var detail=boards.GetTask(project.Name,card.Id);
                    var review=detail?.Sections.FirstOrDefault(s=>s.Title.StartsWith("Review",StringComparison.OrdinalIgnoreCase));
                    tasks.Add(new(card,detail,FindingParser.Parse(review?.Markdown)));
                }
            }catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException){warnings.Add("Thiếu dữ liệu task của "+project.Name+".");}
        }
        if(request.Tasks.Any(t=>!tasks.Any(v=>HubMetricReader.TaskCode(v.Card.Id)==HubMetricReader.TaskCode(t.Code))))warnings.Add("Một số task trong phiếu chưa có dữ liệu trên máy này.");
        var runs=metrics.Runs.Where(r=>tasks.Any(t=>t.Card.Project.Equals(r.Project,StringComparison.OrdinalIgnoreCase)&&HubMetricReader.TaskCode(t.Card.Id)==r.Task)).ToList();
        var allMerged=tasks.Count>0&&tasks.All(t=>t.Card.State==TaskState.Merged);
        var evaluated=tasks.Count>0&&tasks.All(t=>t.Detail?.Sections.Any(s=>s.Title.StartsWith("Đánh giá",StringComparison.OrdinalIgnoreCase)&&InsightParsers.ParseProcessReview("## Đánh giá quy trình\n"+s.Markdown).Any(v=>v.Score.HasValue))==true);
        var current=request.Evaluated?10:allMerged?(evaluated?10:9):tasks.Count>0?tasks.Where(t=>t.Card.State!=TaskState.Merged).Select(t=>t.Card.State switch{TaskState.Assigned or TaskState.NeedsFix=>5,TaskState.Review=>7,_=>4}).Min():3;
        var latest=runs.GroupBy(r=>(r.Project,r.Task)).Select(g=>g.OrderByDescending(r=>r.At).First()).ToList();
        var evidence=new[]{true,request.Events.Any(e=>Regex.IsMatch(e.Text,@"(?i)đã thiết kế|đã duyệt mockup")),
            HubMetricReader.ReadText(paths.AITeamRoot,"plans/"+id+"/plan.md") is {Length:>0},tasks.Count>0,runs.Count>0,
            tasks.Count>0&&latest.Count==tasks.Count&&latest.All(r=>r.VerifyOk==true),
            tasks.Count>0&&tasks.All(t=>ReviewParser.Parse(t.Detail?.Sections.FirstOrDefault(s=>s.Title.StartsWith("Review",StringComparison.OrdinalIgnoreCase))?.Markdown).LastOrDefault()?.Verdict==ReviewVerdict.Pass),
            allMerged,evaluated,request.Evaluated};
        return new(request,tasks,runs,current,evidence,warnings.Distinct().ToList());
    }
}
