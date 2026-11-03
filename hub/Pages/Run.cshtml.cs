using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AITeam.Hub.Pages;
public sealed class RunModel(WorkDetailService details):PageModel
{
    public RunDetailView? Data{get;private set;} public string? Error{get;private set;}
    public IActionResult OnGet(string? project,string? task,int n)
    {
        try{Data=details.Run(project,task??"",n);return Data==null?NotFound():Page();}
        catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException){Error=ex is InvalidDataException?ex.Message:"Không đọc được chi tiết lượt.";return Page();}
    }
}
