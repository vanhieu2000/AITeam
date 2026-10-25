using System.Text;
using System.Text.Json;
using AITeam.Core.Tests.Support;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class W106WorkProgressReviewTests
{
    public static IEnumerable<object[]> InvalidConfigurations() =>
        from file in new[] { "projects.json", "team.json", "team.local.json" }
        from kind in new[] { "json", "utf8", "size" }
        select new object[] { file, kind };

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void Real_config_corruption_reports_safe_configuration_error(string file, string kind)
    {
        using var temp = new TempRepo(git: false);
        var paths = Prepare(temp);
        var bytes = kind switch
        {
            "json" => Encoding.UTF8.GetBytes("{hỏng"),
            "utf8" => new byte[] { 0xFF, 0xFE, 0xFF },
            _ => Encoding.UTF8.GetBytes(new string(' ', 1024 * 1024) + (file == "projects.json" ? "[]" : "{}"))
        };
        File.WriteAllBytes(Path.Combine(temp.Root, file), bytes);
        var service = new WorkProgressService(paths, new TeamConfigService(paths), new WorkflowRolesService(paths));
        var error = Assert.Throws<IOException>(() => service.GetProject("Thử"));
        Assert.Equal("Không đọc được tiến độ công việc. Nguồn: cấu hình", error.Message);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(temp.Root, error.ToString());
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(temp.Root, file)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Real_config_accepts_valid_UTF8_with_optional_files(bool optionalFiles)
    {
        using var temp = new TempRepo(git: false);
        var paths = Prepare(temp);
        if (optionalFiles)
        {
            temp.Write("team.json", "{}", bom: false);
            temp.Write("team.local.json", "{}");
        }
        var service = new WorkProgressService(paths, new TeamConfigService(paths), new WorkflowRolesService(paths));
        var snapshot = service.GetProject("Thử");
        Assert.Equal("Thử", snapshot.Project.Name);
        Assert.Empty(snapshot.Requests);
        Assert.Equal("ban đầu", snapshot.Project.Areas["B"]);
    }

    [Fact]
    public void Snapshot_copies_project_and_areas_from_shared_configuration()
    {
        using var temp = new TempRepo(git: false);
        var paths = Prepare(temp);
        var real = new TeamConfigService(paths);
        var project = Assert.Single(real.GetProjects());
        var config = new SharedConfig(real, project);
        var service = new WorkProgressService(paths, config, new WorkflowRolesService(paths));
        var first = service.GetProject("Thử");
        var areas = Assert.IsType<Dictionary<string, string>>(project.Areas);
        areas["B"] = "đã đổi";
        areas["C"] = "mới";
        var second = service.GetProject("Thử");
        Assert.NotSame(project, first.Project);
        Assert.NotSame(project.Areas, first.Project.Areas);
        Assert.NotSame(first.Project.Areas, second.Project.Areas);
        Assert.Equal("ban đầu", first.Project.Areas["B"]);
        Assert.False(first.Project.Areas.ContainsKey("C"));
        Assert.Equal("đã đổi", second.Project.Areas["B"]);
        Assert.Equal("mới", second.Project.Areas["C"]);
    }

    private static HubPaths Prepare(TempRepo temp)
    {
        var repo = Directory.CreateDirectory(Path.Combine(temp.Root, "repo")).FullName;
        temp.Write("projects.json", JsonSerializer.Serialize(new[] { new { name = "Thử", path = repo,
            workDir = repo, areas = new Dictionary<string, string> { ["B"] = "ban đầu" } } }));
        return new HubPaths(temp.Root, Path.Combine(temp.Root, "profile"));
    }

    private sealed class SharedConfig(ITeamConfigService inner, ProjectInfo project) : ITeamConfigService
    {
        public ProjectInfo? FindProject(string name) => name == project.Name ? project : null;
        public IReadOnlyList<ProjectInfo> GetProjects() => new[] { project };
        public TeamSettings GetSettings() => inner.GetSettings();
        public void SaveProject(ProjectInfo value) => throw new InvalidOperationException("Test cấm ghi cấu hình.");
        public void SaveSettings(TeamSettings value) => throw new InvalidOperationException("Test cấm ghi cấu hình.");
    }
}
