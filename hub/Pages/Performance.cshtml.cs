using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public sealed class PerformanceModel(PerformanceService performance) : PageModel
{
    public PerformanceSnapshot? Data { get; private set; }
    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            Data = performance.Read();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            Error = "Không đọc được dữ liệu hiệu suất.";
        }
    }
}
