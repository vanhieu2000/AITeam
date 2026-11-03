using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AITeam.Hub.Pages;
public sealed class RequestModel(WorkDetailService details):PageModel
{
    public RequestDetailView? Data{get;private set;} public string? Error{get;private set;}
    public IActionResult OnGet(string? id)
    {
        try{Data=details.Request(id??"");return Data==null?NotFound():Page();}
        catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException){Error="Không đọc được chi tiết yêu cầu.";return Page();}
    }
}
