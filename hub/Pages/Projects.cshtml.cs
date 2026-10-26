using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Danh sách mọi dự án từ cấu hình tại /Projects; không đọc bảng việc hoặc tiến trình.</summary>
public sealed class ProjectsModel(ITeamConfigService config) : PageModel
{
    /// <summary>Giữ nguyên mọi dự án và thứ tự GetProjects(), kể cả dự án chưa có trên máy.</summary>
    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = [];

    /// <summary>
    /// Nạp Projects bằng GetProjects() đúng một lần. Không ghi cấu hình, không lọc ExistsLocally.
    /// Không nuốt lỗi dịch vụ thành danh sách rỗng; truyền nguyên ngoại lệ cho cơ chế lỗi chung.
    /// Trang rỗng hiển thị chính xác “Chưa có dự án.”; tên dự án được Razor encode,
    /// liên kết /p/{name} phải mã hóa tên thành một đoạn URL.
    /// </summary>
    public void OnGet()
    {
        Projects = config.GetProjects();
    }
}
