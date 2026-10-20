using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public sealed class VaultModel : PageModel
{
    private static readonly Regex ValidNameRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private readonly IVaultService _vaultService;
    private readonly ITeamConfigService _configService;

    public VaultModel(IVaultService vaultService, ITeamConfigService configService)
    {
        _vaultService = vaultService;
        _configService = configService;
    }

    public IReadOnlyList<string> SecretNames { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = Array.Empty<ProjectInfo>();

    [TempData]
    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public IActionResult OnPostAdd(string? secretName, string? secretValue)
    {
        secretName = secretName?.Trim() ?? "";

        if (string.IsNullOrEmpty(secretName) || !ValidNameRegex.IsMatch(secretName))
        {
            ErrorMessage = "Tên bí mật dạng CHU_HOA_GACH_DUOI, ví dụ DB_PASSWORD";
            secretValue = null;
            ModelState.Clear();
            LoadData();
            return Page();
        }

        if (secretValue == null)
        {
            ErrorMessage = "Giá trị rỗng";
            ModelState.Clear();
            LoadData();
            return Page();
        }

        try
        {
            _vaultService.Set(secretName, secretValue);
            SuccessMessage = $"Đã lưu '{secretName}' vào kho bí mật cục bộ.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            secretValue = null;
            ModelState.Clear();
        }

        LoadData();
        return Page();
    }

    public IActionResult OnPostDelete(string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Thiếu tên bí mật cần xóa.";
            LoadData();
            return Page();
        }

        try
        {
            bool removed = _vaultService.Remove(name);
            if (removed)
            {
                SuccessMessage = $"Đã xóa bí mật '{name}'.";
            }
            else
            {
                ErrorMessage = $"Không tìm thấy bí mật '{name}' để xóa.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi xóa bí mật: " + ex.Message;
        }

        LoadData();
        return Page();
    }

    public IActionResult OnPostExportEnv(string? project, List<string>? selectedNames)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            ErrorMessage = "Vui lòng chọn một dự án.";
            LoadData();
            return Page();
        }

        if (selectedNames == null || selectedNames.Count == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất một bí mật để xuất.";
            LoadData();
            return Page();
        }

        try
        {
            _vaultService.ExportEnv(project, selectedNames);
            SuccessMessage = $"Đã ghi {selectedNames.Count} bí mật vào file .env.local của dự án '{project}'.";
        }
        catch (Exception ex)
        {
            // Các lỗi tiếng Việt từ VaultService:
            // - .env.local CHƯA bị .gitignore...
            // - Kho bí mật bị hỏng...
            // - Không tìm thấy thư mục dự án...
            ErrorMessage = ex.Message;
        }

        LoadData();
        return Page();
    }

    private void LoadData()
    {
        try
        {
            SecretNames = _vaultService.ListNames();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            SecretNames = Array.Empty<string>();
        }

        try
        {
            Projects = _configService.GetProjects();
        }
        catch
        {
            Projects = Array.Empty<ProjectInfo>();
        }
    }
}
