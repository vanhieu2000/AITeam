using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-02 phần trang Tổng quan (Claude viết trước — agent KHÔNG sửa).
public sealed class AcceptanceH302IndexTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 1, 0, 0);

    private static TaskInsight Task(string id, DateTime end, Difficulty d = Difficulty.M) => new(
        "AITeam", id, "Việc " + id, "A", "Minh", d, "R-003", TaskState.Merged, 9, 1, 1, true, true, 4.6, end,
        Array.Empty<StageScore>(), Array.Empty<ReviewIssue>(),
        new[] { new TimelineSegment(SegmentKind.Coding, end.AddMinutes(-9), end), new TimelineSegment(SegmentKind.Merge, end, end) });

    private static TeamInsights Data(int n = 2) => new(
        Enumerable.Range(0, n).Select(i => Task($"H3-{i:00}", Now.AddMinutes(-10 - i))).ToList(),
        Array.Empty<StaffScore>(), Array.Empty<StageAverage>(), Array.Empty<TeamIssue>(), Array.Empty<Improvement>(),
        new OverviewKpis(0.5, 11.0, 2));

    private sealed class FakeInsights(Func<TeamInsights> get) : ITeamInsightsService
    {
        public TeamInsights Get() => get();
        public TaskInsight? GetTask(string project, string taskId) => null;
    }

    private sealed class FakeApprovals(int pending) : IApprovalService
    {
        public IReadOnlyList<Approval> GetAll() => Enumerable.Range(1, pending + 1).Select(i => new Approval(
            $"A-{i:000}", ApprovalKind.Decision, "t", "", "AITeam", null, null, Array.Empty<string>(), "Claude", Now,
            i <= pending ? ApprovalStatus.Pending : ApprovalStatus.Approved, null, null)).ToList();
        public Approval? Get(string id) => null;
        public Approval Decide(string id, bool approve, string? note) => throw new NotSupportedException();
    }

    private static IndexModel Page(ITeamInsightsService? insights, IApprovalService? approvals, string range = "fit") =>
        new(new FakeTeamConfigService(), new FakeTaskBoardService(), new FakeAgentRunService(), new FakeHealthService(), insights, approvals)
        { Range = range, Now = () => Now };

    [Fact]
    public void Default_range_fits_recent_tasks_and_counts_pending_approvals()
    {
        var data = Data();
        var p = Page(new FakeInsights(() => data), new FakeApprovals(2));
        p.OnGet();
        Assert.Same(data, p.Insights);
        Assert.NotNull(p.Chart);
        var (from, to) = GanttLayout.FitWindow(data.Tasks, Now);
        Assert.Equal((from, to), (p.Chart!.From, p.Chart.To));
        Assert.Equal(2, p.Chart.Rows.Count);
        Assert.Equal(2, p.PendingApprovals);
        Assert.Equal(2, p.TaskRows.Count);
        Assert.Null(p.V3Error);
        Assert.NotEmpty(p.Projects); // phần cũ vẫn nạp
    }

    [Theory]
    [InlineData("24h", 24.0)]
    [InlineData("7d", 24.0 * 7)]
    [InlineData("30d", 24.0 * 30)]
    public void Fixed_ranges_end_now(string range, double hours)
    {
        var p = Page(new FakeInsights(() => Data()), null, range);
        p.OnGet();
        Assert.Equal(Now, p.Chart!.To);
        Assert.Equal(Now.AddHours(-hours), p.Chart.From);
        Assert.Equal(0, p.PendingApprovals);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1y")]
    [InlineData("<script>")]
    public void Unknown_range_falls_back_to_fit(string range)
    {
        var p = Page(new FakeInsights(() => Data()), null, range);
        p.OnGet();
        Assert.Equal("fit", p.Range);
        Assert.NotNull(p.Chart);
    }

    [Fact]
    public void Task_rows_are_capped_at_20()
    {
        var p = Page(new FakeInsights(() => Data(25)), null);
        p.OnGet();
        Assert.Equal(20, p.TaskRows.Count);
        Assert.Equal("H3-00", p.TaskRows[0].Id);
    }

    [Fact]
    public void Old_constructor_without_v3_services_still_works()
    {
        var p = new IndexModel(new FakeTeamConfigService(), new FakeTaskBoardService(), new FakeAgentRunService(), new FakeHealthService());
        p.OnGet();
        Assert.Null(p.Insights);
        Assert.Null(p.Chart);
        Assert.Empty(p.TaskRows);
        Assert.Null(p.V3Error);
    }

    [Fact]
    public void Insights_failure_is_reported_not_thrown()
    {
        var p = Page(new FakeInsights(() => throw new IOException("đĩa lỗi")), new FakeApprovals(1));
        p.OnGet();
        Assert.Null(p.Insights);
        Assert.Null(p.Chart);
        Assert.Equal("Không đọc được dữ liệu đánh giá: đĩa lỗi", p.V3Error);
        Assert.Equal(1, p.PendingApprovals);
        Assert.NotEmpty(p.Projects);
    }

    [Fact]
    public void Pills_and_percent()
    {
        Assert.Equal(("pill ok", "S"), IndexModel.DifficultyPill(Difficulty.S));
        Assert.Equal(("pill run", "M"), IndexModel.DifficultyPill(Difficulty.M));
        Assert.Equal(("pill err", "L"), IndexModel.DifficultyPill(Difficulty.L));
        Assert.Equal(("pill n", "—"), IndexModel.DifficultyPill(Difficulty.Unknown));
        Assert.Equal(("pill run", "Đang làm"), IndexModel.StatePill(TaskState.Assigned));
        Assert.Equal(("pill rev", "Chờ review"), IndexModel.StatePill(TaskState.Review));
        Assert.Equal(("pill fix", "Cần sửa"), IndexModel.StatePill(TaskState.NeedsFix));
        Assert.Equal(("pill ok", "Đã merge"), IndexModel.StatePill(TaskState.Merged));
        Assert.Equal(("pill n", "?"), IndexModel.StatePill(TaskState.Unknown));
        Assert.Equal("50%", IndexModel.Percent(0.5));
        Assert.Equal("67%", IndexModel.Percent(0.666));
        Assert.Equal("—", IndexModel.Percent(null));
    }

    private static string Source(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, relative))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, relative));
    }

    [Fact]
    public void Razor_markup_follows_layout()
    {
        var index = Source(Path.Combine("hub", "Pages", "Index.cshtml"));
        Assert.Contains("_GanttPartial", index);
        foreach (var r in new[] { "?range=fit", "?range=24h", "?range=7d", "?range=30d" }) Assert.Contains(r, index);
        Assert.Contains("href=\"/Approvals\"", index);
        Assert.Contains("Điểm quy trình", index);
        Assert.Contains("data-project=", index);
        Assert.Contains("data-state=", index);
        Assert.Contains("V3Error", index);
        Assert.DoesNotContain("Html.Raw", index);

        var gantt = Source(Path.Combine("hub", "Pages", "Shared", "_GanttPartial.cshtml"));
        Assert.Contains("@model AITeam.Hub.Services.GanttChart", gantt);
        Assert.Contains("viewBox", gantt);
        Assert.Contains("<title>", gantt);
        Assert.Contains("/t/", gantt);           // nhãn hàng dẫn tới trang task
        Assert.Contains("Chưa có số đo thời gian trong khoảng này", gantt);
        Assert.DoesNotContain("Html.Raw", gantt);
    }
}
