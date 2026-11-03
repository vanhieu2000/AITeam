using System.Globalization;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
namespace AITeam.Hub.Services;
public enum OfficeTaskStatus { Planned,Pending,Running,Gate,Review,NeedsFix,Blocked,Paused,Merged,Cancelled }
public sealed record OfficeWorkCard(TaskCard Task,OfficeTaskStatus Status,double? Minutes,int Runs,IReadOnlyList<string> Flags,DateTimeOffset? UpdatedAt,DateTime? MergedAt);
public sealed record OfficeWorkSnapshot(IReadOnlyList<OfficeWorkCard> Tasks,IReadOnlyList<ProjectInfo> Projects,IReadOnlyList<Approval> Approvals,IReadOnlyList<string> Warnings);
public sealed class WorkOverviewService(HubPaths paths,ITeamConfigService config,ITaskBoardService boards,IApprovalService approvals,IGitHistory git)
{
    public static string Label(OfficeTaskStatus status)=>status switch
    {OfficeTaskStatus.Planned=>"Kế hoạch",OfficeTaskStatus.Pending=>"Chờ làm",OfficeTaskStatus.Running=>"Đang làm",OfficeTaskStatus.Gate=>"Cổng KT",OfficeTaskStatus.Review=>"Chờ review",OfficeTaskStatus.NeedsFix=>"Cần sửa",OfficeTaskStatus.Blocked=>"Bị chặn",OfficeTaskStatus.Paused=>"Tạm dừng",OfficeTaskStatus.Merged=>"Đã merge",_=>"Hủy"};
    public static OfficeTaskStatus ParseStatus(string raw,TaskState fallback)
    {
        if(raw.Contains("Hủy",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Cancelled;
        if(raw.Contains("Tạm dừng",StringComparison.OrdinalIgnoreCase)||raw.Contains("⏸",StringComparison.Ordinal))return OfficeTaskStatus.Paused;
        if(raw.Contains("Bị chặn",StringComparison.OrdinalIgnoreCase)||raw.Contains("⛔",StringComparison.Ordinal))return OfficeTaskStatus.Blocked;
        if(raw.Contains("Cổng KT",StringComparison.OrdinalIgnoreCase)&&!raw.Contains("ĐẠT",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Gate;
        if(raw.Contains("Chờ review",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Review;
        if(raw.Contains("Cần sửa",StringComparison.OrdinalIgnoreCase)||raw.Contains("🔴",StringComparison.Ordinal))return OfficeTaskStatus.NeedsFix;
        if(raw.Contains("Đã merge",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Merged;
        if(raw.Contains("Kế hoạch",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Planned;
        if(raw.Contains("Chờ làm",StringComparison.OrdinalIgnoreCase)||raw.Contains("Chờ người",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Pending;
        if(raw.Contains("Đang làm",StringComparison.OrdinalIgnoreCase)||raw.Contains("Đã giao",StringComparison.OrdinalIgnoreCase))return OfficeTaskStatus.Running;
        return fallback switch{TaskState.Assigned=>OfficeTaskStatus.Running,TaskState.Review=>OfficeTaskStatus.Review,TaskState.NeedsFix=>OfficeTaskStatus.NeedsFix,TaskState.Merged=>OfficeTaskStatus.Merged,_=>OfficeTaskStatus.Pending};
    }
    public OfficeWorkSnapshot Read()
    {
        var metrics=HubMetricReader.Read(paths);var result=new List<OfficeWorkCard>();var warnings=metrics.Warnings.ToList();var projects=config.GetProjects();
        foreach(var project in projects)
        {
            try
            {
                var board=boards.GetBoard(project.Name);var statuses=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                var raw=HubMetricReader.ReadText(project.Path,"tasks/README.md");
                if(raw!=null)foreach(var line in raw.Split('\n').Where(l=>l.StartsWith('|')))
                {var cells=line.Split('|');if(cells.Length<5)continue;var id=Regex.Match(cells[1],@"[A-Za-z][A-Za-z0-9]*-\d+").Value;if(id.Length>0)statuses[id]=cells[^2].Trim();}
                foreach(var task in board.Tasks)
                {
                    var code=HubMetricReader.TaskCode(task.Id);var runs=metrics.Runs.Where(r=>r.Project.Equals(project.Name,StringComparison.OrdinalIgnoreCase)&&r.Task==code).ToList();
                    var status=ParseStatus(statuses.GetValueOrDefault(task.Id)??"",task.State);
                    var seconds=runs.Where(r=>r.Seconds.HasValue).Sum(r=>r.Seconds!.Value);
                    DateTimeOffset? updated=runs.Count>0?runs[^1].At:task.LastRun==null?null:new DateTimeOffset(task.LastRun.Time);
                    result.Add(new(task,status,runs.Any(r=>r.Seconds.HasValue)?seconds/60:task.LastRun?.DurationSeconds/60,Math.Max(task.Runs,runs.Count),runs.SelectMany(r=>r.Flags).Distinct().ToList(),updated,status==OfficeTaskStatus.Merged?git.MergedAt(project.Path,task.Id):null));
                }
            }
            catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException){warnings.Add("Không đọc được bảng việc của "+project.Name+".");}
        }
        return new(result,projects,approvals.GetAll().Where(a=>a.Status==ApprovalStatus.Pending).ToList(),warnings.Distinct().ToList());
    }
}
