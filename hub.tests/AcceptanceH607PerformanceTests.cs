using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// Nghiệm thu H6-07: Antigravity KHÔNG sửa. Chỉ dùng fake, không khởi chạy CLI hoặc đọc dữ liệu người dùng.
public sealed class AcceptanceH607PerformanceTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 10, 0, 0);
    private sealed class Insights(Func<TeamInsights> get) : ITeamInsightsService
    {
        public TeamInsights Get() => get();
        public TaskInsight? GetTask(string project, string taskId) => null;
    }
    private static TeamInsights Data(int count = 2) => new(
        Enumerable.Range(0, count).Select(i => new TaskInsight(
            "DuAn", $"H6-{i:00}", "Công việc " + i, "B", "Nhân viên B", Difficulty.M,
            i == 0 ? null : "R-007", TaskState.Merged, 9 + i, 1, 1, true, true,
            null, Now.AddDays(-i), i == 0 ? Array.Empty<StageScore>() : Enumerable.Range(1, 7).Select(index =>
                System.Text.Json.JsonSerializer.Deserialize<StageScore>(
                    $"{{\"Index\":{index},\"Stage\":\"Giai đoạn\",\"Score\":4}}")!).ToArray(),
            i == 0 ? Array.Empty<ReviewIssue>() : new[] {
                System.Text.Json.JsonSerializer.Deserialize<ReviewIssue>("{\"Cause\":\"AG\",\"Kind\":\"giao diện\"}")! },
            new[] { new TimelineSegment(SegmentKind.Coding, Now.AddDays(-i).AddMinutes(-9), Now.AddDays(-i)) })).ToList(),
        new[] { new StaffScore("B", "Nhân viên B", 1, 3, 12.5, 0.5, 1.0,
            new Dictionary<string, int> { ["giao diện"] = 2 }) },
        Array.Empty<StageAverage>(), Array.Empty<TeamIssue>(), Array.Empty<Improvement>(),
        new OverviewKpis(0.5, 11.0, 2));
    private static TeamInsights Empty() => Data(0) with { Staff = Array.Empty<StaffScore>() };
    private static IndexModel Page(ITeamInsightsService? insights, string? tab = "overview", string range = "fit") =>
        new(new FakeTeamConfigService(), new FakeTaskBoardService(), new FakeAgentRunService(), new FakeHealthService(), insights)
        { Tab = tab!, Range = range, Now = () => Now };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("other")]
    [InlineData("STAFF")]
    [InlineData("<script>alert(1)</script>")]
    public void Unknown_tab_returns_to_overview(string? tab)
    {
        var p = Page(new Insights(() => Data()), tab);
        p.OnGet();
        Assert.Equal("overview", p.Tab);
        Assert.NotNull(p.Chart);
        Assert.Null(p.StaffView);
    }

    [Theory]
    [InlineData("fit")]
    [InlineData("24h")]
    [InlineData("7d")]
    [InlineData("30d")]
    [InlineData("<script>")]
    public void Switching_tabs_preserves_existing_metrics_and_ranges(string range)
    {
        var data = Data(25);
        var service = new Insights(() => data);
        var overview = Page(service, "overview", range);
        var staff = Page(service, "staff", range);
        overview.OnGet(); staff.OnGet();
        Assert.Equal("staff", staff.Tab);
        Assert.Same(data, staff.Insights);
        Assert.Equal(overview.Insights!.Kpis, staff.Insights!.Kpis);
        Assert.Equal(overview.Range, staff.Range);
        Assert.Equal((overview.Chart!.From, overview.Chart.To), (staff.Chart!.From, staff.Chart.To));
        Assert.Equal(overview.TaskRows, staff.TaskRows);
        Assert.Equal(20, staff.TaskRows.Count);
        Assert.Equal(overview.TotalNeedsFix, staff.TotalNeedsFix);
        Assert.Equal(overview.PendingApprovals, staff.PendingApprovals);
    }

    [Fact]
    public void Staff_reuses_complete_team_projection_including_legacy_tasks()
    {
        var data = Data(); var service = new Insights(() => data);
        var expected = new TeamModel(service); expected.OnGet();
        var p = Page(service, "staff"); p.OnGet();
        Assert.NotNull(p.StaffView);
        Assert.Equal(expected.Staff, p.StaffView!.Staff);
        Assert.Equal(expected.HeatRows, p.StaffView.HeatRows);
        Assert.Single(p.StaffView.HeatRows);
        Assert.Equal(7, p.StaffView.HeatRows[0].Stages.Count);
        Assert.Equal(1, p.StaffView.CauseCounts["AG"]);
        Assert.Equal(expected.CauseCounts.OrderBy(x => x.Key), p.StaffView.CauseCounts.OrderBy(x => x.Key));
        Assert.Equal(5, p.StaffView.CauseCounts.Count);
        Assert.Null(p.TaskRows[0].Request);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Empty_or_missing_service_produces_empty_staff(bool missing)
    {
        var p = Page(missing ? null : new Insights(Empty), "staff"); p.OnGet();
        Assert.NotNull(p.StaffView);
        Assert.Empty(p.StaffView!.Staff);
        Assert.Empty(p.StaffView.HeatRows);
        Assert.Null(p.StaffView.Error);
    }

    [Fact]
    public void Service_failure_keeps_exact_error_and_existing_handlers()
    {
        var p = Page(new Insights(() => throw new IOException("đĩa lỗi")), "staff");
        p.OnGet();
        Assert.Equal("Không đọc được dữ liệu đánh giá: đĩa lỗi", p.V3Error);
        Assert.Equal(p.V3Error, p.StaffView!.Error);
        Assert.Empty(p.StaffView.Staff);
        Assert.NotEmpty(p.Projects);
        Assert.IsType<PartialViewResult>(p.OnGetRunningAgents());
        Assert.IsType<PartialViewResult>(p.OnGetHealthPanel());
    }

    private static string Source(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, relative))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, relative));
    }

    // Dựng Razor đã biên dịch của ứng dụng; bỏ ViewStart để không phụ thuộc dữ liệu/layout ngoài phạm vi.
    private static async Task<string> Render(string path, object model, bool decode = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { ApplicationName = typeof(IndexModel).Assembly.GetName().Name, EnvironmentName = "Development" });
        builder.Services.AddRazorPages().AddApplicationPart(typeof(IndexModel).Assembly);
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var action = new ActionContext(context, new RouteData(), new ActionDescriptor());
        var engine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();
        var view = engine.GetView(null, path, false);
        Assert.True(view.Success, string.Join(", ", view.SearchedLocations ?? Array.Empty<string>()));
        var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        var temp = new TempDataDictionary(context, scope.ServiceProvider.GetRequiredService<ITempDataProvider>());
        using var writer = new StringWriter();
        await view.View.RenderAsync(new ViewContext(action, view.View, data, temp, writer, new HtmlHelperOptions()));
        return decode ? System.Net.WebUtility.HtmlDecode(writer.ToString()) : writer.ToString();
    }

    [Fact]
    public async Task Actual_overview_razor_keeps_filters_chart_and_performance_tabs()
    {
        var p = Page(new Insights(() => Data())); p.OnGet();
        var html = await Render("/Pages/Index.cshtml", p);
        Assert.Contains("/Performance", Source("hub/Pages/Index.cshtml"));
        Assert.Contains("Hiệu suất", Source("hub/Pages/Index.cshtml"));
        Assert.Contains("Nhân viên", html);
        Assert.Contains("f-project", html); Assert.Contains("f-state", html);
        Assert.Contains("data-project=", html); Assert.Contains("data-state=", html);
        Assert.Contains("<svg", html);
        Assert.Contains("range=7d", html);
        Assert.Contains("tab=staff", html);
    }

    [Fact]
    public async Task Actual_staff_partial_renders_all_fields_and_empty_messages()
    {
        var p = Page(new Insights(() => Data()), "staff"); p.OnGet();
        var html = await Render("/Pages/Shared/_PerformanceStaff.cshtml", p.StaffView!);
        foreach (var text in new[] { "Nhân viên B", "Agent B", "12.5", "50%", "100%", "giao diện 2",
            "TK", "AG", "CK", "NV", "HT", "7 giai đoạn" }) Assert.Contains(text, html);
        var empty = Page(new Insights(Empty), "staff"); empty.OnGet();
        var blank = await Render("/Pages/Shared/_PerformanceStaff.cshtml", empty.StaffView!);
        Assert.Contains("Chưa có task nào theo quy trình mới", blank);
        Assert.Contains("Chưa có task nào được đánh giá quy trình", blank);
    }

    [Fact]
    public async Task Actual_staff_tab_encodes_untrusted_error_and_hides_overview()
    {
        const string attack = "<script>alert(1)</script>";
        var p = Page(new Insights(() => throw new IOException(attack)), "staff", "7d"); p.OnGet();
        var html = await Render("/Pages/Index.cshtml", p, false);
        Assert.DoesNotContain(attack, html);
        Assert.Contains("&lt;script&gt;", html);
        html = System.Net.WebUtility.HtmlDecode(html);
        Assert.DoesNotContain("id=\"f-project\"", html);
        Assert.Contains("Không đọc được dữ liệu đánh giá:", html);
        Assert.Contains("range=7d", html);
        var source = Source("hub/Pages/Shared/_PerformanceStaff.cshtml");
        Assert.DoesNotContain("Html.Raw", source);
        Assert.DoesNotContain("Html.Raw", Source("hub/Pages/Index.cshtml"));
    }

    [Fact]
    public void Narrow_layout_has_local_scroll_and_long_text_contract()
    {
        var markup = Source("hub/Pages/Index.cshtml") + Source("hub/Pages/Shared/_PerformanceStaff.cshtml");
        // Bỏ comment để placeholder không thể làm ca kiểm tra xanh giả.
        markup = System.Text.RegularExpressions.Regex.Replace(markup, @"@\*[\s\S]*?\*@", "");
        markup = System.Text.RegularExpressions.Regex.Replace(markup, @"\s+", "");
        Assert.Contains("min-width:0", markup);
        Assert.Contains("overflow-wrap:anywhere", markup);
        Assert.Contains("overflow-x:auto", markup);
    }
}