using System.Text.Json;
using AITeam.Core.Tests.Support;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class W106WorkProgressSourceSafetyTests
{
    [Theory]
    [InlineData("projects.json")]
    [InlineData("team.json")]
    [InlineData("team.local.json")]
    public async Task Linked_configuration_is_rejected_before_real_config_service_reads(string name)
    {
        using var temp = new TempRepo(git: false);
        var paths = new HubPaths(temp.Root, Path.Combine(temp.Root, "profile"));
        var target = Path.GetFullPath(temp.Write("outside/source.json", "{}"));
        var link = Path.Combine(temp.Root, name);
        // Cấu hình dự án rỗng làm FindProject trả null nếu đọc trước khi kiểm liên kết.
        temp.Write("projects.json", "[]");
        File.Delete(link);
        var result = await TestShell.Run(temp.Root, "cmd.exe", 30, "/c", "mklink", link, target);
        Assert.True(result.Code == 0, result.Error);
        try
        {
            var service = new WorkProgressService(paths, new TeamConfigService(paths), new WorkflowRolesService(paths));
            var error = Assert.Throws<IOException>(() => service.GetProject("Kiểm thử"));
            Assert.Equal("Không đọc được tiến độ công việc. Nguồn: cấu hình", error.Message);
            Assert.Null(error.InnerException);
            Assert.DoesNotContain(temp.Root, error.ToString());
        }
        finally { File.Delete(link); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_plans_is_valid_but_plans_file_is_a_source_error(bool plansIsFile)
    {
        using var temp = new TempRepo(git: false);
        var paths = new HubPaths(temp.Root, Path.Combine(temp.Root, "profile"));
        temp.Write("projects.json", JsonSerializer.Serialize(new[] { new
        {
            name = "Kiểm thử", path = temp.Root, workDir = temp.Root
        } }));
        if (plansIsFile) temp.Write("plans", "Dữ liệu sai loại.");
        var service = new WorkProgressService(paths, new TeamConfigService(paths), new WorkflowRolesService(paths));
        if (plansIsFile)
        {
            var error = Assert.Throws<IOException>(() => service.GetProject("Kiểm thử"));
            Assert.Equal("Không đọc được tiến độ công việc. Nguồn: plans", error.Message);
            Assert.Null(error.InnerException);
            Assert.DoesNotContain(temp.Root, error.ToString());
        }
        else
        {
            var snapshot = service.GetProject("Kiểm thử");
            Assert.Equal(temp.Root, snapshot.Project.Path);
            Assert.Empty(snapshot.Requests);
            Assert.Empty(snapshot.UnlinkedTasks);
            Assert.False(Directory.Exists(Path.Combine(temp.Root, "plans")));
        }
    }
}
