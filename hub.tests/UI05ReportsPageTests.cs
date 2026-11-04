using System.Text.RegularExpressions;
using AITeam.Hub.Pages;
using AITeam.Hub.Pages.Shared;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// R-014 UI-05 (Claude): màn Báo cáo — bộ lọc trên URL, sắp xếp bảng điểm, biểu đồ SVG an toàn, CSV.
public sealed class UI05ReportsPageTests
{
    private sealed class FakeReports : IReportService
    {
        public List<ReportFilter> Calls { get; } = [];
        public ReportFilter Last => Calls[^1];

        public ReportData Query(ReportFilter filter, DateTimeOffset? now = null)
        {
            Calls.Add(filter);
            return new ReportData(filter,
                [new("done", "Task xong", 5, 3, "task"), new("first_pass_pct", "Đạt vòng 1", 60, null, "%"),
                 new("ai_minutes", "Phút AI", null, null, "phút"), new("errors_tk", "Lỗi TK", 2, 4, "lỗi")],
                [new("T44", new DateOnly(2026, 10, 26), 3, 66.7, 1, 2, 0), new("T45", new DateOnly(2026, 11, 2), 2, null, 0, 1, 1)],
                [new("AITeam", 120.5), new("<script>x</script>", 10)],
                [new("S", 2), new("M", 3), new("L", 0), new("chưa rõ", 1)],
                [new("Codex", 4), new("OpenCode", 2)],
                [new("codex", "Codex", 80, 4, 75, 100, 50, 1, 22.5), new("opencode", "OpenCode", 55, 2, null, 50, 100, 0, null)],
                [new("TK", "test", 2, ["V10-02", "W2-02"]), new("AG", "giao diện", 3, ["H9-01"])],
                ["AITeam", "HDVTool"], ["Codex", "OpenCode"], ["Cảnh báo mẫu"]);
        }

        public RequestTimeline? Timeline(string requestId) => null;

        public string ExportCsv(ReportFilter filter, string table) =>
            table is "scores" or "weeks" or "errors" or "tasks" ? "﻿a,b\n" : throw new ArgumentException("Bảng cần xuất không hợp lệ: " + table);
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 0, 0, TimeSpan.FromHours(7));

    private static ReportsModel Page(FakeReports svc) => new(svc) { Clock = () => Now };

    [Fact]
    public void Default_is_30_days_overview_sorted_by_score()
    {
        var svc = new FakeReports();
        var p = Page(svc);
        p.OnGet();
        Assert.Equal(new DateOnly(2026, 10, 6), svc.Last.From);
        Assert.Equal(new DateOnly(2026, 11, 4), svc.Last.To);
        Assert.Equal("overview", p.Tab);
        Assert.Equal(["codex", "opencode"], p.SortedScores.Select(s => s.Person));
        Assert.Equal("/Reports?range=30", p.Url());
    }

    [Fact]
    public void Bad_query_values_fall_back_safely()
    {
        var svc = new FakeReports();
        var p = Page(svc);
        p.Range = "9999"; p.Tab = "<x>"; p.Sort = "drop"; p.Dir = "x"; p.Difficulty = "XL";
        p.Project = "a\u0000b"; p.Person = new string('p', 200);
        p.OnGet();
        Assert.Equal("30", p.Range);
        Assert.Equal("overview", p.Tab);
        Assert.Equal("score", p.Sort);
        Assert.Equal("desc", p.Dir);
        Assert.Null(svc.Last.Difficulty);
        Assert.Null(svc.Last.Project);
        Assert.Null(svc.Last.Person);
    }

    [Fact]
    public void Custom_range_swaps_and_caps_one_year()
    {
        var svc = new FakeReports();
        var p = Page(svc);
        p.Range = "custom"; p.From = new DateOnly(2026, 11, 1); p.To = new DateOnly(2026, 10, 1);
        p.OnGet();
        Assert.Equal(new DateOnly(2026, 10, 1), svc.Last.From);
        Assert.Equal(new DateOnly(2026, 11, 1), svc.Last.To);

        p = Page(svc);
        p.Range = "custom"; p.From = new DateOnly(2020, 1, 1); p.To = new DateOnly(2026, 1, 1);
        p.OnGet();
        Assert.Equal(366, svc.Last.To.DayNumber - svc.Last.From.DayNumber);

        p = Page(svc);
        p.Range = "custom"; p.From = null;   // thiếu ngày ⇒ về 30 ngày
        p.OnGet();
        Assert.Equal("30", p.Range);
    }

    [Fact]
    public void Person_filter_still_lists_all_people_options()
    {
        var svc = new FakeReports();
        var p = Page(svc);
        p.Person = "codex";
        p.OnGet();
        Assert.Equal("codex", svc.Calls[0].Person);
        Assert.Null(svc.Calls[1].Person);
        Assert.Equal(["codex", "opencode"], p.PeopleOptions.Select(o => o.Key));
    }

    [Theory]
    [InlineData("pass", "asc", "opencode")]    // chưa đo (null) coi như thấp nhất
    [InlineData("pass", "desc", "codex")]
    [InlineData("tasks", "asc", "opencode")]
    [InlineData("name", "desc", "opencode")]
    [InlineData("ontime", "desc", "opencode")]
    public void Sort_scores(string sort, string dir, string first)
    {
        var p = Page(new FakeReports());
        p.Sort = sort; p.Dir = dir;
        p.OnGet();
        Assert.Equal(first, p.SortedScores[0].Person);
    }

    [Fact]
    public void Url_keeps_filters_and_escapes()
    {
        var p = Page(new FakeReports());
        p.Project = "A&B"; p.Tab = "people"; p.Sort = "tasks";
        p.OnGet();
        Assert.Equal("/Reports?range=30&project=A%26B&tab=people&sort=tasks", p.Url());
        Assert.Equal("/Reports?range=30&project=A%26B&handler=Csv&table=scores", p.CsvUrl("scores"));
    }

    [Fact]
    public void Delta_text_never_relies_on_color_only()
    {
        Assert.Equal(("▲ 2 task so với kỳ trước", "up"), ReportsModel.Delta(new("done", "Task xong", 5, 3, "task")));
        Assert.Equal(("▼ 2 lỗi so với kỳ trước", "up"), ReportsModel.Delta(new("errors_tk", "Lỗi TK", 2, 4, "lỗi")));
        Assert.Equal("chưa có kỳ trước", ReportsModel.Delta(new("x", "x", 1, null, "%")).Text);
        Assert.Equal("chưa đo", ReportsModel.Num(null));
        Assert.Equal("60%", ReportsModel.Num(60, "%"));
    }

    [Fact]
    public void Csv_handler_returns_file_or_400()
    {
        var file = Assert.IsType<FileContentResult>(Page(new FakeReports()).OnGetCsv("weeks"));
        Assert.StartsWith("text/csv", file.ContentType);
        Assert.Equal("bao-cao-weeks-20261006-20261104.csv", file.FileDownloadName);
        Assert.IsType<BadRequestObjectResult>(Page(new FakeReports()).OnGetCsv("../secret"));
    }

    private static string Render(Microsoft.AspNetCore.Html.IHtmlContent c)
    {
        using var w = new StringWriter();
        c.WriteTo(w, System.Text.Encodings.Web.HtmlEncoder.Default);
        return w.ToString();
    }

    [Fact]
    public void Svg_charts_encode_labels_and_handle_empty()
    {
        var html = Render(SvgCharts.HBars("t<b>", [new SvgCharts.Bar("<script>alert(1)</script>", 3, "/x?a=\"y")], "phút"));
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<b>", html);
        Assert.DoesNotContain("a=\"y", html);
        Assert.Contains("role=\"img\"", html);
        Assert.Contains("Chưa có số liệu", Render(SvgCharts.HBars("t", [], "x")));
        Assert.Contains("Chưa có số liệu", Render(SvgCharts.Columns("t", [], [], "x")));
        Assert.Contains("Chưa có số liệu", Render(SvgCharts.StackedShare("t", [("S", 0, "--c1")])));

        var cols = Render(SvgCharts.Columns("t", ["T1", "T2"], [new("A", "--c1", [1, 2]), new("B", "--c2", [0, 3])], "lỗi", [50, null], "đạt"));
        Assert.Equal(3, Regex.Matches(cols, "<rect ").Count);   // ô bằng 0 không vẽ
        Assert.Single(Regex.Matches(cols, "<circle "));          // điểm chưa đo không vẽ

        Assert.Equal(5, SvgCharts.NiceMax(3.2));
        Assert.Equal(1, SvgCharts.NiceMax(0));
        Assert.Equal(200, SvgCharts.NiceMax(120.5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?tab=people&sort=pass&dir=asc")]
    [InlineData("?tab=projects")]
    [InlineData("?tab=errors")]
    public async Task Page_renders_every_tab_with_fake_data(string query)
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s => s.AddSingleton<IReportService>(new FakeReports()));
        var html = await host.Client.GetStringAsync("/Reports" + query);
        Assert.Contains("Báo cáo", html);
        Assert.DoesNotContain("<script>x</script>", html);
        if (query == "") Assert.True(Regex.Matches(html, "<svg class=\"chart").Count >= 5);
        if (query.Contains("people")) Assert.Contains("aria-sort=\"ascending\"", html);
        if (query.Contains("errors")) Assert.Contains("V10-02", html);
    }
}
