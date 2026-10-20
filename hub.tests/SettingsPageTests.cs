using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class SettingsPageTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly TeamConfigService _configService;
    private readonly string _dummyAgyExe;

    public SettingsPageTests()
    {
        // Khởi tạo file team.json, team.local.json và projects.json trong thư mục tạm
        string teamFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamFixture, _env.Paths.TeamJson, overwrite: true);

        string localFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.local.json");
        File.Copy(localFixture, _env.Paths.TeamLocalJson, overwrite: true);

        string projectsFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsFixture, _env.Paths.ProjectsJson, overwrite: true);

        _configService = new TeamConfigService(_env.Paths);

        // Tạo một file agy.exe giả lập trong thư mục tạm để kiểm tra IsValidAgyExe
        string agyDir = Path.Combine(_env.RootDir, "Tools", "agy", "bin");
        Directory.CreateDirectory(agyDir);
        _dummyAgyExe = Path.Combine(agyDir, "agy.exe");
        File.WriteAllText(_dummyAgyExe, "dummy-agy");
    }

    public void Dispose() => _env.Dispose();

    [Fact]
    public void OnGet_loads_settings_and_projects()
    {
        var page = new SettingsModel(_configService);
        page.OnGet();

        Assert.NotNull(page.Settings);
        Assert.NotEmpty(page.Projects);
        Assert.Equal("gemini-test", page.Settings.DefaultModel);
    }

    [Fact]
    public void SaveSettings_with_valid_data_succeeds_and_creates_bak()
    {
        var page = new SettingsModel(_configService);

        var devs = new List<SettingsModel.DeveloperFormItem>
        {
            new() { Id = "A", Name = "Agent A", Focus = "Hạ tầng & CSDL" },
            new() { Id = "B", Name = "Agent B", Focus = "Giao diện Razor" }
        };

        var result = page.OnPostSaveSettings(
            defaultModel: "gemini-2.5-pro",
            maxReviewRounds: 5,
            projectsRoot: @"D:\MyProjects",
            workRoot: @"D:\MyWork",
            agyExe: _dummyAgyExe,
            developers: devs
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Đã lưu (có bản .bak)", page.SuccessMessage);
        Assert.Null(page.ErrorMessage);

        // Kiểm tra dữ liệu đã ghi
        var saved = _configService.GetSettings();
        Assert.Equal("gemini-2.5-pro", saved.DefaultModel);
        Assert.Equal(5, saved.MaxReviewRounds);
        Assert.Equal(@"D:\MyProjects", saved.ProjectsRoot);
        Assert.Equal(@"D:\MyWork", saved.WorkRoot);
        Assert.Equal(_dummyAgyExe, saved.AgyExe);
        Assert.Equal("Hạ tầng & CSDL", saved.Developers.First(d => d.Id == "A").Focus);

        // Kiểm tra có file .bak
        Assert.True(File.Exists(_env.Paths.TeamJson + ".bak"));
        Assert.True(File.Exists(_env.Paths.TeamLocalJson + ".bak"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void SaveSettings_with_invalid_MaxReviewRounds_is_rejected_and_does_not_save(int invalidRounds)
    {
        var page = new SettingsModel(_configService);
        string teamJsonBefore = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonBefore = File.ReadAllText(_env.Paths.TeamLocalJson);

        var result = page.OnPostSaveSettings(
            defaultModel: "gemini-test",
            maxReviewRounds: invalidRounds,
            projectsRoot: @"D:\Projects",
            workRoot: @"D:\Work",
            agyExe: _dummyAgyExe,
            developers: null
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("MaxReviewRounds", page.ErrorMessage);
        Assert.Null(page.SuccessMessage);

        // Đảm bảo file không bị thay đổi
        string teamJsonAfter = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonAfter = File.ReadAllText(_env.Paths.TeamLocalJson);
        Assert.Equal(teamJsonBefore, teamJsonAfter);
        Assert.Equal(localJsonBefore, localJsonAfter);
    }

    [Theory]
    [InlineData(@"C:\Tools\not_exist\agy.exe")]
    [InlineData(@"not-a-full-path.exe")]
    [InlineData(@"C:\Tools\agy\bin\other.exe")]
    [InlineData("")]
    [InlineData(null)]
    public void SaveSettings_with_invalid_AgyExe_is_rejected_and_does_not_save(string? invalidExe)
    {
        var page = new SettingsModel(_configService);
        string teamJsonBefore = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonBefore = File.ReadAllText(_env.Paths.TeamLocalJson);

        var result = page.OnPostSaveSettings(
            defaultModel: "gemini-test",
            maxReviewRounds: 3,
            projectsRoot: @"D:\Projects",
            workRoot: @"D:\Work",
            agyExe: invalidExe,
            developers: null
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("AgyExe", page.ErrorMessage);

        // Đảm bảo file không bị thay đổi
        string teamJsonAfter = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonAfter = File.ReadAllText(_env.Paths.TeamLocalJson);
        Assert.Equal(teamJsonBefore, teamJsonAfter);
        Assert.Equal(localJsonBefore, localJsonAfter);
    }

    [Theory]
    [InlineData("model\ninvalid", @"D:\Projects", @"D:\Work")]
    [InlineData("model\rinvalid", @"D:\Projects", @"D:\Work")]
    [InlineData("model", "D:\\Projects\r\nBad", @"D:\Work")]
    [InlineData("model", @"D:\Projects", "D:\\Work\nBad")]
    public void SaveSettings_with_newlines_in_fields_is_rejected_and_does_not_save(
        string model, string projRoot, string workRoot)
    {
        var page = new SettingsModel(_configService);
        string teamJsonBefore = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonBefore = File.ReadAllText(_env.Paths.TeamLocalJson);

        var result = page.OnPostSaveSettings(
            defaultModel: model,
            maxReviewRounds: 3,
            projectsRoot: projRoot,
            workRoot: workRoot,
            agyExe: _dummyAgyExe,
            developers: null
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("xuống dòng", page.ErrorMessage);

        // Đảm bảo file không bị thay đổi
        string teamJsonAfter = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonAfter = File.ReadAllText(_env.Paths.TeamLocalJson);
        Assert.Equal(teamJsonBefore, teamJsonAfter);
        Assert.Equal(localJsonBefore, localJsonAfter);
    }

    [Fact]
    public void SaveSettings_with_newline_in_developer_focus_is_rejected()
    {
        var page = new SettingsModel(_configService);
        string teamJsonBefore = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonBefore = File.ReadAllText(_env.Paths.TeamLocalJson);

        var devs = new List<SettingsModel.DeveloperFormItem>
        {
            new() { Id = "A", Name = "Agent A", Focus = "Dòng 1\nDòng 2" }
        };

        var result = page.OnPostSaveSettings(
            defaultModel: "gemini-test",
            maxReviewRounds: 3,
            projectsRoot: @"D:\Projects",
            workRoot: @"D:\Work",
            agyExe: _dummyAgyExe,
            developers: devs
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("xuống dòng", page.ErrorMessage);

        string teamJsonAfter = File.ReadAllText(_env.Paths.TeamJson);
        string localJsonAfter = File.ReadAllText(_env.Paths.TeamLocalJson);
        Assert.Equal(teamJsonBefore, teamJsonAfter);
        Assert.Equal(localJsonBefore, localJsonAfter);
    }

    [Fact]
    public void SaveProject_with_valid_data_succeeds_and_updates_projects_json()
    {
        var page = new SettingsModel(_configService);

        var result = page.OnPostSaveProject(
            projectName: "ProjectAlpha",
            stack: ".NET 10, HTMX, Razor",
            build: "dotnet build ProjectAlpha.sln -v m",
            test: "dotnet test ProjectAlpha.sln",
            areaA: "src/Services/",
            areaB: "src/Pages/"
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Đã lưu (có bản .bak)", page.SuccessMessage);

        var proj = _configService.FindProject("ProjectAlpha");
        Assert.NotNull(proj);
        Assert.Equal(".NET 10, HTMX, Razor", proj.Stack);
        Assert.Equal("dotnet build ProjectAlpha.sln -v m", proj.Build);
        Assert.Equal("src/Services/", proj.Areas["A"]);
        Assert.Equal("src/Pages/", proj.Areas["B"]);
        Assert.True(File.Exists(_env.Paths.ProjectsJson + ".bak"));
    }

    [Theory]
    [InlineData("stack\nnewline", "dotnet build", "dotnet test")]
    [InlineData("stack", "dotnet build\r\n", "dotnet test")]
    [InlineData("stack", "dotnet build", "dotnet test\n")]
    public void SaveProject_with_newline_is_rejected_and_does_not_save(string stack, string build, string test)
    {
        var page = new SettingsModel(_configService);
        string before = File.ReadAllText(_env.Paths.ProjectsJson);

        var result = page.OnPostSaveProject(
            projectName: "ProjectAlpha",
            stack: stack,
            build: build,
            test: test,
            areaA: "src/",
            areaB: "ui/"
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("xuống dòng", page.ErrorMessage);

        string after = File.ReadAllText(_env.Paths.ProjectsJson);
        Assert.Equal(before, after);
    }

    [Fact]
    public void SaveProject_with_unknown_project_name_is_rejected()
    {
        var page = new SettingsModel(_configService);
        string before = File.ReadAllText(_env.Paths.ProjectsJson);

        var result = page.OnPostSaveProject(
            projectName: "NonExistentProject",
            stack: "stack",
            build: "build",
            test: "test",
            areaA: "a",
            areaB: "b"
        );

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("Không tìm thấy", page.ErrorMessage);

        string after = File.ReadAllText(_env.Paths.ProjectsJson);
        Assert.Equal(before, after);
    }
}
