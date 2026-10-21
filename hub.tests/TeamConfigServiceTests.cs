using System.Text.Json.Nodes;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class TeamConfigServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void GetSettings_without_local_json_reads_team_json()
    {
        // Ghi team.json không có team.local.json
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        var service = new TeamConfigService(_env.Paths);
        var settings = service.GetSettings();

        Assert.Equal(@"D:\Projects", settings.ProjectsRoot);
        Assert.Equal(@"D:\Work", settings.WorkRoot);
        Assert.Equal(@"C:\Tools\agy\bin\agy.exe", settings.AgyExe);
        Assert.Equal("gemini-test", settings.DefaultModel);
        Assert.Equal("test-owner", settings.GitHubOwner);
        Assert.Equal(3, settings.MaxReviewRounds);
        Assert.Equal(2, settings.Developers.Count);
        Assert.Equal(2, settings.AgyAccounts.Count);
    }

    [Fact]
    public void GetSettings_with_local_json_overrides_paths_and_agyExe()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string localJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.local.json");
        File.Copy(localJsonFixture, _env.Paths.TeamLocalJson);

        var service = new TeamConfigService(_env.Paths);
        var settings = service.GetSettings();

        // team.local.json ghi đè projectsRoot, workRoot, agyExe
        Assert.Equal(@"E:\MyProjects", settings.ProjectsRoot);
        Assert.Equal(@"E:\MyWork", settings.WorkRoot);
        Assert.Equal(@"D:\CustomAgy\agy.exe", settings.AgyExe);
        // Các thông tin khác vẫn từ team.json
        Assert.Equal("gemini-test", settings.DefaultModel);
        Assert.Equal("test-owner", settings.GitHubOwner);
    }

    [Fact]
    public void SaveSettings_preserves_unknown_keys_and_creates_bak_and_separates_local_paths()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        var service = new TeamConfigService(_env.Paths);
        var oldSettings = service.GetSettings();

        var newDevs = new List<DeveloperInfo>
        {
            new("A", "Agent A", "Nền tảng"),
            new("B", "Agent B", "Giao diện tiếng Việt")
        };

        var newSettings = new TeamSettings(
            DefaultModel: "claude-3-7-sonnet",
            MaxReviewRounds: 5,
            Developers: newDevs,
            ProjectsRoot: @"Z:\NewProjects",
            WorkRoot: @"Z:\NewWork",
            GitHubOwner: "new-owner",
            AgyAccounts: new[] { "Acc1" },
            AgyExe: @"Z:\bin\agy.exe"
        );

        service.SaveSettings(newSettings);

        // 1. Phải có file .bak
        Assert.True(File.Exists(_env.Paths.TeamJson + ".bak"));

        // 2. team.json phải giữ nguyên các khóa lạ như _ghichu và customField
        string teamJsonText = File.ReadAllText(_env.Paths.TeamJson);
        var teamObj = JsonNode.Parse(teamJsonText) as JsonObject;
        Assert.NotNull(teamObj);
        Assert.Equal("Ghi chú cấu hình mẫu", teamObj["_ghichu"]?.ToString());
        Assert.Equal("giữ nguyên", teamObj["customField"]?.ToString());
        Assert.Equal("new-owner", teamObj["github"]?["owner"]?.ToString());
        Assert.Equal("claude-3-7-sonnet", teamObj["agy"]?["model"]?.ToString());
        Assert.Equal(5, teamObj["review"]?["maxRounds"]?.GetValue<int>());

        // 3. projectsRoot và workRoot riêng của máy KHÔNG được ghi đè vào team.json
        Assert.NotEqual(@"Z:\NewProjects", teamObj["projectsRoot"]?.ToString());
        Assert.NotEqual(@"Z:\NewWork", teamObj["workRoot"]?.ToString());

        // 4. Đường dẫn riêng của máy phải nằm trong team.local.json
        Assert.True(File.Exists(_env.Paths.TeamLocalJson));
        string localJsonText = File.ReadAllText(_env.Paths.TeamLocalJson);
        var localObj = JsonNode.Parse(localJsonText) as JsonObject;
        Assert.NotNull(localObj);
        Assert.Equal(@"Z:\NewProjects", localObj["projectsRoot"]?.ToString());
        Assert.Equal(@"Z:\NewWork", localObj["workRoot"]?.ToString());
        Assert.Equal(@"Z:\bin\agy.exe", localObj["agyExe"]?.ToString());
    }

    [Fact]
    public void ResolveProject_follows_resolution_rules()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string localJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.local.json");
        File.Copy(localJsonFixture, _env.Paths.TeamLocalJson);

        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        // Tạo thư mục thật cho CustomPathProject theo paths trong team.local.json
        string customPath = Path.Combine(_env.RootDir, "SpecialCustom");
        Directory.CreateDirectory(customPath);

        // Sửa lại team.local.json để trỏ CustomPathProject vào customPath
        string localContent = File.ReadAllText(_env.Paths.TeamLocalJson)
            .Replace(@"F:\\Special\\CustomPathProject", customPath.Replace(@"\", @"\\"));
        File.WriteAllText(_env.Paths.TeamLocalJson, localContent);

        var service = new TeamConfigService(_env.Paths);
        var projects = service.GetProjects();

        var customProj = projects.FirstOrDefault(p => p.Name == "CustomPathProject");
        Assert.NotNull(customProj);
        Assert.Equal(customPath, customProj.Path);
        Assert.True(customProj.ExistsLocally);

        var alphaProj = projects.FirstOrDefault(p => p.Name == "ProjectAlpha");
        Assert.NotNull(alphaProj);
        // ProjectAlpha không có path tồn tại trên đĩa nên sẽ fallback theo quy tắc
        Assert.False(alphaProj.ExistsLocally);
    }

    [Fact]
    public void ResolveProject_local_workDirs_overrides_projects_json_workDir()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);
        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        // Máy khác: ProjectAlpha có workDir riêng trong team.local.json
        File.WriteAllText(_env.Paths.TeamLocalJson,
            "{ \"workRoot\": \"E:\\\\MyWork\", \"workDirs\": { \"ProjectAlpha\": \"E:\\\\Agents\\\\Alpha\" } }");

        var projects = new TeamConfigService(_env.Paths).GetProjects();

        var alpha = projects.Single(p => p.Name == "ProjectAlpha");
        Assert.Equal(@"E:\Agents\Alpha", alpha.WorkDir);

        // Dự án không có trong workDirs và không khai báo workDir: workRoot\<tên>
        var custom = projects.Single(p => p.Name == "CustomPathProject");
        Assert.Equal(Path.Combine(@"E:\MyWork", "CustomPathProject"), custom.WorkDir);
    }

    [Fact]
    public void SaveProject_updates_projects_json_and_creates_bak()
    {
        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        var service = new TeamConfigService(_env.Paths);
        var project = new ProjectInfo(
            Name: "ProjectAlpha",
            Repo: "test-owner/ProjectAlpha",
            Path: @"D:\Projects\ProjectAlpha",
            WorkDir: @"D:\Work\ProjectAlpha",
            Stack: ".NET 10 + React",
            Build: "dotnet build -c Release",
            Test: "dotnet test --no-build",
            Areas: new Dictionary<string, string> { ["A"] = "backend", ["B"] = "frontend" },
            ExistsLocally: true
        );

        service.SaveProject(project);

        Assert.True(File.Exists(_env.Paths.ProjectsJson + ".bak"));
        var updated = service.FindProject("ProjectAlpha");
        Assert.NotNull(updated);
        Assert.Equal(".NET 10 + React", updated.Stack);
        Assert.Equal("dotnet build -c Release", updated.Build);
        Assert.Equal("dotnet test --no-build", updated.Test);
        Assert.Equal("backend", updated.Areas["A"]);
    }
}
