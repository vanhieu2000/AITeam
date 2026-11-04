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

}
