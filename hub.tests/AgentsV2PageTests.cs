using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class AgentsV2PageTests
{
    private sealed class FakeRunService : IAgentRunService
    {
        private readonly List<AgentRun> _runs;

        public FakeRunService(List<AgentRun> runs)
        {
            _runs = runs;
        }

        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null)
        {
            var q = _runs.Where(r => string.Equals(r.Project, project, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(taskId))
            {
                q = q.Where(r => string.Equals(r.Task, taskId, StringComparison.OrdinalIgnoreCase));
            }
            return q.ToList();
        }

        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) =>
            _runs.OrderByDescending(r => r.Time).Take(count).ToList();

        public IReadOnlyList<RunningAgent> GetRunning() =>
            new List<RunningAgent>
            {
                new(31640, DateTime.Now.AddMinutes(-34), "ChinaAndroidToolbox", "C-011", "agy.exe")
            };

        public bool Stop(int pid) => pid == 31640;
    }

    private static List<AgentRun> SampleRuns()
    {
        var list = new List<AgentRun>();
        var t0 = new DateTime(2026, 10, 21, 10, 0, 0);
        for (int i = 1; i <= 20; i++)
        {
            string proj = i % 2 == 0 ? "ChinaAndroidToolbox" : "HDVTool";
            var state = i % 3 == 0 ? RunState.Error : RunState.Success;
            string? err = state == RunState.Error ? (i == 6 ? "Your AI credits balance is too low to continue." : "build failed") : null;
            list.Add(new AgentRun(proj, $"T-{i:D3}", 1, state, state == RunState.Success ? "SUCCESS" : "ERROR", err, "Response " + i, 120, 500000 + i * 50000, t0.AddMinutes(i), $"logs/T-{i:D3}-1.json"));
        }
        return list;
    }

    [Fact]
    public void AgentsModel_loads_paged_history_and_pagination_summary()
    {
        var runs = SampleRuns();
        var runService = new FakeRunService(runs);
        var page = new AgentsModel(runService);

        page.OnGet(RunFilter.All, null, 1);

        Assert.Equal(20, page.RunPage.Total);
        Assert.Equal(8, page.RunPage.Items.Count);
        Assert.Equal(3, page.RunPage.PageCount);
        Assert.Equal("1–8 / 20", page.PaginationSummary);

        page.OnGet(RunFilter.All, null, 3);
        Assert.Equal(4, page.RunPage.Items.Count);
        Assert.Equal("17–20 / 20", page.PaginationSummary);
    }

    [Fact]
    public void AgentsModel_filters_by_errors()
    {
        var runs = SampleRuns();
        var runService = new FakeRunService(runs);
        var page = new AgentsModel(runService);

        page.OnGet(RunFilter.Errors, null, 1);

        Assert.All(page.RunPage.Items, r => Assert.NotEqual(RunState.Success, r.State));
        Assert.True(page.RunPage.Total > 0);
    }

    [Fact]
    public void AgentsModel_run_drawer_returns_partial_with_correct_run()
    {
        var runs = SampleRuns();
        var runService = new FakeRunService(runs);
        var page = new AgentsModel(runService);

        var result = page.OnGetRunDrawer("HDVTool", "T-001", 1);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RunDrawerPartial", partial.ViewName);
        var model = Assert.IsType<AgentRun>(partial.Model);
        Assert.Equal("HDVTool", model.Project);
        Assert.Equal("T-001", model.Task);
        Assert.Equal(1, model.Attempt);
    }

    [Fact]
    public void Formatting_helpers_format_expected_strings()
    {
        Assert.Equal("—", AgentsModel.FormatDuration(null));
        Assert.Equal("45s", AgentsModel.FormatDuration(45));
        Assert.Equal("32′", AgentsModel.FormatDuration(1920));

        Assert.Equal("—", AgentsModel.FormatTokens(null));
        Assert.Contains("tr", AgentsModel.FormatTokens(1_270_000));
        Assert.Contains("k", AgentsModel.FormatTokens(85_000));
    }
}
