using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class PageTests
{
    private readonly FakeTeamConfigService _config = new();
    private readonly FakeTaskBoardService _board = new();
    private readonly FakeAgentRunService _run = new();
    private readonly FakeHealthService _health = new();

    [Fact]
    public void Index_page_loads_all_data()
    {
        var page = new IndexModel(_config, _board, _run, _health);
        page.OnGet();

        Assert.Equal(2, page.TotalProjects);
        Assert.True(page.TotalAssigned >= 1);
        Assert.True(page.TotalReview >= 1);
        Assert.Equal(1, page.TotalRunningAgents);
        Assert.NotEmpty(page.HealthItems);
    }

    [Fact]
    public void Index_running_agents_handler_returns_partial()
    {
        var page = new IndexModel(_config, _board, _run, _health);
        var result = page.OnGetRunningAgents();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RunningAgentsPartial", partial.ViewName);
        var model = Assert.IsAssignableFrom<IReadOnlyList<RunningAgent>>(partial.Model);
        Assert.Single(model);
    }

    [Fact]
    public void Project_page_loads_board_and_groups_tasks()
    {
        var page = new ProjectModel(_board, _config)
        {
            Name = "AITeam"
        };

        var result = page.OnGet();
        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.Board);
        Assert.NotEmpty(page.AssignedTasks);
        Assert.NotEmpty(page.ReviewTasks);
        Assert.NotEmpty(page.NeedsFixTasks);
        Assert.NotEmpty(page.MergedTasks);
    }

    [Fact]
    public void Task_page_loads_task_details_and_runs()
    {
        var page = new TaskModel(_board, _config)
        {
            Name = "AITeam",
            Id = "H-002"
        };

        var result = page.OnGet();
        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.Detail);
        Assert.NotNull(page.Project);
        Assert.Equal("H-002", page.Detail.Card.Id);
        Assert.NotEmpty(page.Detail.Sections);
        Assert.NotEmpty(page.Detail.Criteria);
        Assert.NotEmpty(page.Detail.Runs);
        Assert.Contains("agent-b/H-002-hub-ui", page.BranchGitHubUrl);
    }

    [Fact]
    public void FormatElapsed_formats_seconds_minutes_and_hours()
    {
        Assert.Equal("45 giây", IndexModel.FormatElapsed(TimeSpan.FromSeconds(45)));
        Assert.Equal("12 phút", IndexModel.FormatElapsed(TimeSpan.FromMinutes(12)));
        Assert.Equal("2 giờ 15 phút", IndexModel.FormatElapsed(TimeSpan.FromMinutes(135)));
    }
}
