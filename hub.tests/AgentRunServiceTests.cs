using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class AgentRunServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void GetRuns_handles_success_error_empty_corrupted_bom_and_ignores_prompt_and_ask()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        // Chuẩn bị thư mục logs cho ProjectAlpha
        string workDir = Path.Combine(_env.RootDir, "work", "ProjectAlpha");
        string logsDir = Path.Combine(workDir, "logs");
        Directory.CreateDirectory(logsDir);

        // Copy các file log từ Fixtures
        string fixturesLogsDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "logs");
        foreach (var file in Directory.GetFiles(fixturesLogsDir))
        {
            File.Copy(file, Path.Combine(logsDir, Path.GetFileName(file)));
        }

        var configService = new TeamConfigService(_env.Paths);
        // Cập nhật workDir của ProjectAlpha trong projects.json để trỏ đúng thư mục tạm
        var proj = configService.FindProject("ProjectAlpha");
        Assert.NotNull(proj);
        var updatedProj = proj with { WorkDir = workDir };
        configService.SaveProject(updatedProj);

        var runService = new AgentRunService(_env.Paths, configService);
        var runs = runService.GetRuns("ProjectAlpha", "T-001");

        // Fixtures logs có:
        // T-001-1.json (SUCCESS)
        // T-001-2.json (ERROR)
        // T-001-3.json (0 bytes -> Running)
        // T-001-4.json (invalid JSON -> Error)
        // T-001-5.json (UTF-8 BOM -> Success)
        // T-001-1.prompt.md (bỏ qua)
        // T-001-1.stderr.txt (bỏ qua)
        // ask-20261020-120000.json (bỏ qua)
        Assert.Equal(5, runs.Count);

        var run1 = runs.First(r => r.Attempt == 1);
        Assert.Equal(RunState.Success, run1.State);
        Assert.Equal("SUCCESS", run1.Status);
        Assert.Equal(12.5, run1.DurationSeconds);
        Assert.Equal(1500, run1.TotalTokens);
        Assert.Equal("Thành công", run1.Response);

        var run2 = runs.First(r => r.Attempt == 2);
        Assert.Equal(RunState.Error, run2.State);
        Assert.Equal("ERROR", run2.Status);
        Assert.Equal("Đã có lỗi xảy ra", run2.Error);

        var run3 = runs.First(r => r.Attempt == 3);
        Assert.Equal(RunState.Running, run3.State);
        Assert.Null(run3.Status);

        var run4 = runs.First(r => r.Attempt == 4);
        Assert.Equal(RunState.Error, run4.State);
        Assert.Equal("ERROR", run4.Status);
        Assert.Contains("Log hỏng", run4.Error);

        var run5 = runs.First(r => r.Attempt == 5);
        Assert.Equal(RunState.Success, run5.State);
        Assert.Equal("SUCCESS", run5.Status);
        Assert.Equal("Tiếng Việt có BOM", run5.Response);
    }

    [Fact]
    public void Stop_returns_false_for_non_existent_or_non_agy_process()
    {
        var configService = new TeamConfigService(_env.Paths);
        var runService = new AgentRunService(_env.Paths, configService);

        // PID tiến trình không tồn tại
        Assert.False(runService.Stop(999999));

        // Tiến trình hiện tại (dotnet test / test runner) không phải agy.exe
        int currentPid = Environment.ProcessId;
        Assert.False(runService.Stop(currentPid));
    }
}
