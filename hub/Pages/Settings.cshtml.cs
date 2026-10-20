using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

public sealed class SettingsModel : PageModel
{
    private readonly ITeamConfigService _configService;

    public SettingsModel(ITeamConfigService configService)
    {
        _configService = configService;
    }

    public TeamSettings Settings { get; private set; } = default!;
    public IReadOnlyList<ProjectInfo> Projects { get; private set; } = Array.Empty<ProjectInfo>();

    [TempData]
    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public IActionResult OnPostSaveSettings(
        string? defaultModel,
        int maxReviewRounds,
        string? projectsRoot,
        string? workRoot,
        string? agyExe,
        List<DeveloperFormItem>? developers)
    {
        defaultModel ??= "";
        projectsRoot ??= "";
        workRoot ??= "";
        agyExe ??= "";

        // Kiểm tra đầu vào:
        // 1. MaxReviewRounds 1-10
        if (maxReviewRounds < 1 || maxReviewRounds > 10)
        {
            ErrorMessage = "Số vòng review tối đa (MaxReviewRounds) phải từ 1 đến 10.";
            LoadData();
            return Page();
        }

        // 2. AgyExe phải là đường dẫn đầy đủ tới agy.exe có thật (dùng HealthService.IsValidAgyExe)
        if (!HealthService.IsValidAgyExe(agyExe))
        {
            ErrorMessage = "Đường dẫn AgyExe phải là đường dẫn đầy đủ tới agy.exe có thật trên máy.";
            LoadData();
            return Page();
        }

        // 3. Chuỗi không chứa ký tự xuống dòng
        if (HasNewline(defaultModel) || HasNewline(projectsRoot) || HasNewline(workRoot) || HasNewline(agyExe))
        {
            ErrorMessage = "Chuỗi cấu hình không được chứa ký tự xuống dòng.";
            LoadData();
            return Page();
        }

        if (developers != null)
        {
            foreach (var dev in developers)
            {
                if (HasNewline(dev.Focus) || HasNewline(dev.Name) || HasNewline(dev.Id))
                {
                    ErrorMessage = "Chuyên môn developer không được chứa ký tự xuống dòng.";
                    LoadData();
                    return Page();
                }
            }
        }

        var current = _configService.GetSettings();
        var devList = new List<DeveloperInfo>();
        if (developers != null && developers.Count > 0)
        {
            foreach (var d in developers)
            {
                devList.Add(new DeveloperInfo(d.Id ?? "", d.Name ?? "", d.Focus ?? ""));
            }
        }
        else
        {
            devList.AddRange(current.Developers);
        }

        var updated = new TeamSettings(
            DefaultModel: defaultModel.Trim(),
            MaxReviewRounds: maxReviewRounds,
            Developers: devList,
            ProjectsRoot: projectsRoot.Trim(),
            WorkRoot: workRoot.Trim(),
            GitHubOwner: current.GitHubOwner,
            AgyAccounts: current.AgyAccounts,
            AgyExe: agyExe.Trim()
        );

        _configService.SaveSettings(updated);
        SuccessMessage = "Đã lưu (có bản .bak)";
        LoadData();
        return Page();
    }

    public IActionResult OnPostSaveProject(
        string projectName,
        string? stack,
        string? build,
        string? test,
        string? areaA,
        string? areaB)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            ErrorMessage = "Tên dự án không được để trống.";
            LoadData();
            return Page();
        }

        stack ??= "";
        build ??= "";
        test ??= "";
        areaA ??= "";
        areaB ??= "";

        // Kiểm tra không chứa ký tự xuống dòng
        if (HasNewline(stack) || HasNewline(build) || HasNewline(test) || HasNewline(areaA) || HasNewline(areaB))
        {
            ErrorMessage = "Chuỗi cấu hình dự án không được chứa ký tự xuống dòng.";
            LoadData();
            return Page();
        }

        var project = _configService.FindProject(projectName);
        if (project == null)
        {
            ErrorMessage = $"Không tìm thấy dự án '{projectName}'.";
            LoadData();
            return Page();
        }

        var areas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in project.Areas)
        {
            areas[kv.Key] = kv.Value;
        }

        if (!string.IsNullOrWhiteSpace(areaA)) areas["A"] = areaA.Trim();
        if (!string.IsNullOrWhiteSpace(areaB)) areas["B"] = areaB.Trim();

        var updated = new ProjectInfo(
            Name: project.Name,
            Repo: project.Repo,
            Path: project.Path,
            WorkDir: project.WorkDir,
            Stack: stack.Trim(),
            Build: build.Trim(),
            Test: test.Trim(),
            Areas: areas,
            ExistsLocally: project.ExistsLocally
        );

        _configService.SaveProject(updated);
        SuccessMessage = "Đã lưu (có bản .bak)";
        LoadData();
        return Page();
    }

    private void LoadData()
    {
        Settings = _configService.GetSettings();
        Projects = _configService.GetProjects();
    }

    private static bool HasNewline(string? s) =>
        s != null && (s.Contains('\r') || s.Contains('\n'));

    public sealed class DeveloperFormItem
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Focus { get; set; }
    }
}
