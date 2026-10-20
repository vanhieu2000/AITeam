using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class TaskBoardServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void GetBoard_parses_4_states_links_plan_markdown_and_runs()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        // Tạo thư mục dự án cho ProjectAlpha
        string projectPath = Path.Combine(_env.RootDir, "ProjectAlpha");
        string tasksDir = Path.Combine(projectPath, "tasks");
        Directory.CreateDirectory(tasksDir);

        // Copy tasks/README.md
        string readmeFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tasks", "README.md");
        File.Copy(readmeFixture, Path.Combine(tasksDir, "README.md"));

        var configService = new TeamConfigService(_env.Paths);
        var proj = configService.FindProject("ProjectAlpha");
        Assert.NotNull(proj);
        configService.SaveProject(proj with { Path = projectPath });

        var runService = new AgentRunService(_env.Paths, configService);
        var boardService = new TaskBoardService(_env.Paths, configService, runService);

        var board = boardService.GetBoard("ProjectAlpha");

        Assert.Equal("ProjectAlpha", board.Project.Name);
        Assert.Equal(4, board.Tasks.Count);

        // Kiểm tra 4 task và 4 trạng thái
        var t1 = board.Tasks.First(t => t.Id == "T-001");
        Assert.Equal("Tính năng một", t1.Title);
        Assert.Equal("A", t1.Agent);
        Assert.Equal("agent-a/T-001", t1.Branch);
        Assert.Equal(TaskState.Assigned, t1.State);
        Assert.Equal("T-001-sample.md", t1.FileName);

        var t2 = board.Tasks.First(t => t.Id == "T-002");
        Assert.Equal(TaskState.Review, t2.State);

        var t3 = board.Tasks.First(t => t.Id == "T-003");
        Assert.Equal(TaskState.NeedsFix, t3.State);

        var t4 = board.Tasks.First(t => t.Id == "T-004");
        Assert.Equal(TaskState.Merged, t4.State);

        // Kiểm tra PlanMarkdown
        Assert.StartsWith("## Kế hoạch / hàng chờ", board.PlanMarkdown);
        Assert.Contains("Task T-005", board.PlanMarkdown);
    }

    [Fact]
    public void GetTask_parses_sections_criteria_checkboxes_and_handles_missing_file()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        string projectPath = Path.Combine(_env.RootDir, "ProjectAlpha");
        string tasksDir = Path.Combine(projectPath, "tasks");
        Directory.CreateDirectory(tasksDir);

        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "tasks", "README.md"), Path.Combine(tasksDir, "README.md"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "tasks", "T-001-sample.md"), Path.Combine(tasksDir, "T-001-sample.md"));

        var configService = new TeamConfigService(_env.Paths);
        var proj = configService.FindProject("ProjectAlpha");
        Assert.NotNull(proj);
        configService.SaveProject(proj with { Path = projectPath });

        var runService = new AgentRunService(_env.Paths, configService);
        var boardService = new TaskBoardService(_env.Paths, configService, runService);

        // 1. Task có file đầy đủ
        var detail1 = boardService.GetTask("ProjectAlpha", "T-001");
        Assert.NotNull(detail1);
        Assert.Equal("T-001", detail1.Card.Id);
        Assert.True(detail1.Sections.Count > 0);
        Assert.Contains(detail1.Sections, s => s.Title == "Bối cảnh");
        Assert.Contains(detail1.Sections, s => s.Title == "Tiêu chí hoàn thành");

        // Kiểm tra criteria
        Assert.Equal(3, detail1.Criteria.Count);
        Assert.False(detail1.Criteria[0].Done);
        Assert.Equal("Chưa hoàn thành mục 1", detail1.Criteria[0].Text);
        Assert.True(detail1.Criteria[1].Done);
        Assert.Equal("Đã hoàn thành mục 2", detail1.Criteria[1].Text);
        Assert.False(detail1.Criteria[2].Done);

        // 2. Task có trong bảng nhưng thiếu file task (T-002)
        var detail2 = boardService.GetTask("ProjectAlpha", "T-002");
        Assert.NotNull(detail2);
        Assert.Equal("T-002", detail2.Card.Id);
        Assert.Empty(detail2.Sections);
        Assert.Empty(detail2.Criteria);

        // 3. Task không tồn tại cả trong bảng lẫn trên đĩa
        var detailUnknown = boardService.GetTask("ProjectAlpha", "T-999");
        Assert.Null(detailUnknown);
    }
}
