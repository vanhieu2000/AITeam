using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>
/// Test nghiệm thu H-008 (Claude viết trước khi giao — agent KHÔNG sửa file này).
/// Hợp đồng: tasks/H-008-hub-v2-agents-inbox-secrets-settings.md mục "Mục tiêu" 6.
/// </summary>
public sealed class AcceptanceH008Tests
{
    private static readonly DateTime T0 = new(2026, 10, 21, 9, 0, 0);

    private static AgentRun Run(int i, RunState state, string? status, string? error = null, string project = "HDVTool") =>
        new(project, "T-" + i.ToString("000"), 1, state, status, error, null, 60, 1000, T0.AddMinutes(i), "logs/x.json");

    /// <summary>27 lần chạy giống lịch sử thật 21/10: 15 thành công, 9 hết credits, 3 lỗi khác; + 1 đang chạy.</summary>
    private static List<AgentRun> History()
    {
        var list = new List<AgentRun>();
        for (int i = 1; i <= 27; i++)
        {
            string project = i % 2 == 0 ? "ChinaAndroidToolbox" : "HDVTool";
            if (i <= 15) list.Add(Run(i, RunState.Success, "SUCCESS", project: project));
            else if (i <= 24) list.Add(Run(i, RunState.Error, "ERROR", "Your AI credits balance is too low to continue.", project));
            else list.Add(Run(i, RunState.Error, "ERROR", "build failed", project));
        }
        list.Add(Run(99, RunState.Running, null, project: "ChinaAndroidToolbox"));
        return list;
    }

    [Fact]
    public void All_excludes_running_sorts_newest_first_and_pages_by_8()
    {
        var p1 = RunHistory.Page(History(), RunFilter.All, null, 1);
        Assert.Equal(27, p1.Total);
        Assert.Equal(4, p1.PageCount);
        Assert.Equal(8, p1.Items.Count);
        Assert.Equal(T0.AddMinutes(27), p1.Items[0].Time);
        Assert.DoesNotContain(p1.Items, r => r.State == RunState.Running);

        var p4 = RunHistory.Page(History(), RunFilter.All, null, 4);
        Assert.Equal(3, p4.Items.Count);
        Assert.Equal(T0.AddMinutes(1), p4.Items[^1].Time);
    }

    [Fact]
    public void Errors_include_credit_exhaustion_and_other_errors()
    {
        var p = RunHistory.Page(History(), RunFilter.Errors, null, 1, pageSize: 50);
        Assert.Equal(12, p.Total);
        Assert.All(p.Items, r => Assert.NotEqual(RunState.Success, r.State));
    }

    [Fact]
    public void Success_filter_and_project_filter_combine()
    {
        var p = RunHistory.Page(History(), RunFilter.Success, "HDVTool", 1, pageSize: 50);
        Assert.Equal(8, p.Total);   // i lẻ trong 1..15
        Assert.All(p.Items, r => Assert.Equal("HDVTool", r.Project));
        Assert.Equal(RunHistory.Page(History(), RunFilter.Success, "", 1, 50).Total,
                     RunHistory.Page(History(), RunFilter.Success, null, 1, 50).Total);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(99, 4)]
    public void Out_of_range_page_is_clamped(int requested, int expected)
        => Assert.Equal(expected, RunHistory.Page(History(), RunFilter.All, null, requested).Page);

    [Fact]
    public void Empty_history_gives_one_empty_page()
    {
        var p = RunHistory.Page(Array.Empty<AgentRun>(), RunFilter.All, null, 1);
        Assert.Empty(p.Items);
        Assert.Equal(0, p.Total);
        Assert.Equal(1, p.PageCount);
        Assert.Equal(1, p.Page);
    }
}
