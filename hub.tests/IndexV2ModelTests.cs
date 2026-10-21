using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class IndexV2ModelTests
{
    [Fact]
    public void IndexModel_loads_v2_stats_and_activities()
    {
        var config = new FakeTeamConfigService();
        var board = new FakeTaskBoardService();
        var run = new FakeAgentRunService();
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        page.OnGet();

        Assert.Equal(3, page.TotalProjects);
        Assert.True(page.TotalAssigned >= 1);
        Assert.True(page.TotalReview >= 1);
        Assert.True(page.TotalNeedsFix >= 0);
        Assert.Equal(1, page.TotalRunningAgents);
        Assert.NotEmpty(page.RecentActivities);
        Assert.True(page.RecentActivities.Count <= 6);
        Assert.True(page.TotalHealthCount > 0);
        Assert.True(page.OkHealthCount > 0);
        Assert.False(page.HasHealthError); // DevData mặc định ok/warning
    }

    [Fact]
    public void IndexModel_works_when_no_projects()
    {
        var config = new EmptyConfigService();
        var board = new EmptyBoardService();
        var run = new EmptyRunService();
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        page.OnGet();

        Assert.Equal(0, page.TotalProjects);
        Assert.Equal(0, page.TotalAssigned);
        Assert.Equal(0, page.TotalReview);
        Assert.Equal(0, page.TotalNeedsFix);
        Assert.Empty(page.Projects);
        Assert.Empty(page.RecentActivities);
    }

    [Fact]
    public void IndexModel_works_when_no_runs()
    {
        var config = new FakeTeamConfigService();
        var board = new FakeTaskBoardService();
        var run = new EmptyRunService();
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        page.OnGet();

        Assert.Empty(page.RecentActivities);
        Assert.NotEmpty(page.Projects);
    }

    [Fact]
    public void IndexModel_works_when_all_runs_are_errors()
    {
        var config = new FakeTeamConfigService();
        var board = new FakeTaskBoardService();
        var run = new ErrorOnlyRunService();
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        page.OnGet();

        Assert.NotEmpty(page.RecentActivities);
        Assert.All(page.RecentActivities, act =>
        {
            Assert.True(act.Verdict == RunVerdict.Error || act.Verdict == RunVerdict.CreditsExhausted);
        });
    }

    [Fact]
    public void IndexModel_supports_long_project_and_task_names()
    {
        var longTaskName = "T-999-this-is-a-very-long-task-name-with-sixty-characters-total";
        var config = new SingleProjectConfigService("ChinaAndroidToolbox");
        var board = new SingleTaskBoardService("ChinaAndroidToolbox", longTaskName);
        var run = new SingleRunService("ChinaAndroidToolbox", longTaskName);
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        page.OnGet();

        var p = Assert.Single(page.Projects);
        Assert.Equal("ChinaAndroidToolbox", p.Info.Name);
        Assert.Contains(longTaskName, p.LatestActivity);

        var act = Assert.Single(page.RecentActivities);
        Assert.Equal(longTaskName, act.Task);
        Assert.Equal("ChinaAndroidToolbox", act.Project);
    }

    [Fact]
    public void IndexModel_HealthPanel_handler_returns_partial()
    {
        var config = new FakeTeamConfigService();
        var board = new FakeTaskBoardService();
        var run = new FakeAgentRunService();
        var health = new FakeHealthService();

        var page = new IndexModel(config, board, run, health);
        var result = page.OnGetHealthPanel();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_HealthPanelPartial", partial.ViewName);
        var model = Assert.IsAssignableFrom<IReadOnlyList<HealthItem>>(partial.Model);
        Assert.NotEmpty(model);
    }

    [Fact]
    public void FormatRelativeTime_formats_relative_and_absolute()
    {
        var now = DateTime.Now;
        Assert.Equal("Vừa xong", IndexModel.FormatRelativeTime(now.AddSeconds(-20)));
        Assert.Equal("15 phút trước", IndexModel.FormatRelativeTime(now.AddMinutes(-15)));
    }

    // --- Helpers / Fakes for testing edge cases ---

    private sealed class EmptyConfigService : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => Array.Empty<ProjectInfo>();
        public ProjectInfo? FindProject(string name) => null;
        public TeamSettings GetSettings() => throw new NotImplementedException();
        public void SaveSettings(TeamSettings settings) { }
        public void SaveProject(ProjectInfo project) { }
    }

    private sealed class EmptyBoardService : ITaskBoardService
    {
        public ProjectBoard GetBoard(string project) => new(
            new ProjectInfo(project, "repo", "path", "work", "stack", "build", "test", new Dictionary<string, string>(), true),
            Array.Empty<TaskCard>(),
            "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private sealed class EmptyRunService : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => Array.Empty<AgentRun>();
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => Array.Empty<AgentRun>();
        public IReadOnlyList<RunningAgent> GetRunning() => Array.Empty<RunningAgent>();
        public bool Stop(int pid) => false;
    }

    private sealed class ErrorOnlyRunService : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => GetRecentRuns();
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => new List<AgentRun>
        {
            new("ChinaAndroidToolbox", "C-001", 1, RunState.Error, "ERROR", "build failed", null, 10, 100, DateTime.Now.AddMinutes(-10), "logs/1.json"),
            new("ChinaAndroidToolbox", "C-002", 1, RunState.Error, "ERROR", "quota rate limit 429", null, 10, 100, DateTime.Now.AddMinutes(-5), "logs/2.json")
        };
        public IReadOnlyList<RunningAgent> GetRunning() => Array.Empty<RunningAgent>();
        public bool Stop(int pid) => false;
    }

    private sealed class SingleProjectConfigService : ITeamConfigService
    {
        private readonly string _name;
        public SingleProjectConfigService(string name) => _name = name;
        public IReadOnlyList<ProjectInfo> GetProjects() => new[]
        {
            new ProjectInfo(_name, "owner/" + _name, "path", "work", "stack", "build", "test", new Dictionary<string, string>(), true)
        };
        public ProjectInfo? FindProject(string name) => GetProjects().FirstOrDefault();
        public TeamSettings GetSettings() => throw new NotImplementedException();
        public void SaveSettings(TeamSettings settings) { }
        public void SaveProject(ProjectInfo project) { }
    }

    private sealed class SingleTaskBoardService : ITaskBoardService
    {
        private readonly string _project;
        private readonly string _taskId;
        public SingleTaskBoardService(string project, string taskId)
        {
            _project = project;
            _taskId = taskId;
        }
        public ProjectBoard GetBoard(string project) => new(
            new ProjectInfo(_project, "owner/" + _project, "path", "work", "stack", "build", "test", new Dictionary<string, string>(), true),
            new[] { new TaskCard(_project, _taskId, "Task Title", "A", "main", TaskState.Assigned, $"{_taskId}.md", 1, null) },
            "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private sealed class SingleRunService : IAgentRunService
    {
        private readonly string _project;
        private readonly string _taskId;
        public SingleRunService(string project, string taskId)
        {
            _project = project;
            _taskId = taskId;
        }
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => new[]
        {
            new AgentRun(_project, _taskId, 1, RunState.Success, "SUCCESS", null, "Done", 10, 100, DateTime.Now.AddMinutes(-5), "logs/1.json")
        };
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => GetRuns(_project);
        public IReadOnlyList<RunningAgent> GetRunning() => Array.Empty<RunningAgent>();
        public bool Stop(int pid) => false;
    }
}
