using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AITeam.Hub.Pages;
public sealed class WorkModel(WorkOverviewService overview):PageModel
{
    public OfficeWorkSnapshot? Data{get;private set;}
    public string? Project{get;private set;} public OfficeTaskStatus? FilterState{get;private set;}
    public IReadOnlyList<OfficeWorkCard> Visible{get;private set;}=[];
    public IReadOnlyList<OfficeWorkCard> NeedsOwner{get;private set;}=[];
    public string? Error{get;private set;}
    public DateTime Now=>DateTime.Now;
    public IActionResult OnGet(string? project,string? state)
    {
        Project=project;
        if(state!=null){if(!Enum.TryParse<OfficeTaskStatus>(state,out var parsed)||!Enum.IsDefined(parsed))return NotFound();FilterState=parsed;}
        try
        {
            Data=overview.Read();if(project!=null&&!Data.Projects.Any(p=>p.Name.Equals(project,StringComparison.OrdinalIgnoreCase)))return NotFound();
            var scope=Data.Tasks.Where(t=>project==null||t.Task.Project.Equals(project,StringComparison.OrdinalIgnoreCase)).ToList();
            Visible=scope.Where(t=>FilterState==null||t.Status==FilterState).ToList();
            NeedsOwner=scope.Where(t=>t.Status==OfficeTaskStatus.Blocked||t.Status==OfficeTaskStatus.Paused&&t.UpdatedAt<DateTimeOffset.Now.AddHours(-24)).OrderBy(t=>t.UpdatedAt).ToList();
        }catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException){Error="Không đọc được tổng quan công việc.";}
        return Page();
    }
    public IReadOnlyList<OfficeWorkCard> Column(int column)=>Visible.Where(t=>column switch
    {0=>t.Status==OfficeTaskStatus.Pending,1=>t.Status is OfficeTaskStatus.Running or OfficeTaskStatus.Gate,2=>t.Status==OfficeTaskStatus.Review,
        3=>t.Status is OfficeTaskStatus.NeedsFix or OfficeTaskStatus.Paused,4=>t.Status==OfficeTaskStatus.Merged&&t.MergedAt>=Now.AddDays(-7),_=>false}).ToList();
}
