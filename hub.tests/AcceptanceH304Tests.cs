using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-04 (Claude viết trước — agent KHÔNG sửa). Trang Đội ngũ + Quy trình & cải tiến.
public sealed class AcceptanceH304Tests
{
    private static StageScore S(int i, int? score, string cause = "", string issue = "", string proposal = "")
        => new(i, InsightParsers.StageNames[i - 1], "Claude", score, "", issue, cause, proposal);

    private static TaskInsight T(string id, string? request, IReadOnlyList<StageScore> stages, params ReviewIssue[] issues) => new(
        "AITeam", id, "Việc " + id, "B", "Lan", Difficulty.M, request, TaskState.Merged, 13, 3, 1, true, false, 4.1, null,
        stages, issues, Array.Empty<TimelineSegment>());

    private static ReviewIssue I(string id, string cause, string kind = "phạm vi") => new("AITeam", id, cause, kind, "x");

    private static readonly TaskInsight V406 = T("V4-06", "R-002",
        new[] { S(1, 5), S(2, 5), S(3, 5), S(4, 2, "TK", "Phạm vi quên file khung", "Phạm vi tự thêm file khung"), S(5, 5), S(6, 2, "CK", "Cổng KT báo nhầm", "Đã sửa"), S(7, 5) },
        I("V4-06", "TK"), I("V4-06", "CK"));

    private static readonly TaskInsight V403 = T("V4-03", "R-002",
        new[] { S(1, 5), S(3, 4, "TK", "Thiếu BOM"), S(6, null, "HT", "Codex hết hạn mức") },
        I("V4-03", "AG", "bảo mật"), I("V4-03", "XX"));

    private static readonly TaskInsight T007 = T("T-007", null, Array.Empty<StageScore>(), I("T-007", "AG", "giao diện"));

    private static TeamInsights Data(params TaskInsight[] tasks) => new(
        tasks,
        new[] { new StaffScore("B", "Lan", 1, 3, 4.3, 0.0, 1.0, new Dictionary<string, int>()) },
        Enumerable.Range(1, 7).Select(i => new StageAverage(i, InsightParsers.StageNames[i - 1], i == 4 ? 3.0 : (double?)null, i == 4 ? 2 : 0)).ToList(),
        tasks.SelectMany(t => t.Stages.Where(s => s.Cause != "").Select(s => new TeamIssue(t.Project, t.Id, s.Index, s.Stage, s.Cause, s.Issue, s.Proposal))).ToList(),
        new[] { new Improvement("IMP-001", "IMP-001 · Chu kỳ 1", "🟡 Đề xuất", new[] { "AITeam/V4-03" }, "IMP-001-a.md", "") },
        new OverviewKpis(1.0, 11.0, 2));

    private sealed class Fake(Func<TeamInsights> get) : ITeamInsightsService
    {
        public TeamInsights Get() => get();
        public TaskInsight? GetTask(string project, string taskId) => null;
    }

    // ───────────── Đội ngũ ─────────────

    [Fact]
    public void Team_page_loads_staff_heat_rows_and_cause_counts()
    {
        var p = new TeamModel(new Fake(() => Data(V406, T007, V403)));
        p.OnGet();
        Assert.Equal("Lan", Assert.Single(p.Staff).Name);
        Assert.Equal(new[] { "V4-06", "V4-03" }, p.HeatRows.Select(t => t.Id)); // T-007 không có bảng đánh giá
        Assert.Equal(TeamModel.Causes, p.CauseCounts.Keys.ToArray());
        Assert.Equal(1, p.CauseCounts["TK"]);
        Assert.Equal(1, p.CauseCounts["CK"]);
        Assert.Equal(1, p.CauseCounts["AG"]);   // lỗi AG của T-007 (không có phiếu R-xxx) không tính
        Assert.Equal(0, p.CauseCounts["NV"]);
        Assert.Equal(0, p.CauseCounts["HT"]);   // HT chỉ có trong bảng đánh giá, không phải lỗi review
        Assert.Null(p.Error);
    }

    [Fact]
    public void Team_heat_rows_capped_at_15()
    {
        var many = Enumerable.Range(1, 20).Select(i => T($"X-{i:00}", "R-003", new[] { S(1, 5) })).ToArray();
        var p = new TeamModel(new Fake(() => Data(many)));
        p.OnGet();
        Assert.Equal(15, p.HeatRows.Count);
        Assert.Equal("X-01", p.HeatRows[0].Id);
    }

    [Fact]
    public void Team_error_is_reported()
    {
        var p = new TeamModel(new Fake(() => throw new IOException("hỏng")));
        p.OnGet();
        Assert.Equal("Không đọc được dữ liệu đánh giá: hỏng", p.Error);
        Assert.Empty(p.Staff);
        Assert.Empty(p.HeatRows);
        Assert.All(TeamModel.Causes, c => Assert.Equal(0, p.CauseCounts[c]));
    }

    [Theory]
    [InlineData(5, "h5")]
    [InlineData(4, "h4")]
    [InlineData(3, "h3")]
    [InlineData(2, "h2")]
    [InlineData(1, "h1")]
    [InlineData(null, "h0")]
    [InlineData(0, "h0")]
    [InlineData(6, "h0")]
    public void Heat_css(int? score, string css) => Assert.Equal(css, TeamModel.HeatCss(score));

    [Fact]
    public void Text_helpers()
    {
        Assert.Equal("giao diện 2 · test 1", TeamModel.AgErrorsText(new Dictionary<string, int> { ["test"] = 1, ["giao diện"] = 2 }));
        Assert.Equal("bảo mật 1 · test 1", TeamModel.AgErrorsText(new Dictionary<string, int> { ["test"] = 1, ["bảo mật"] = 1 }));
        Assert.Equal("khác 1", TeamModel.AgErrorsText(new Dictionary<string, int> { [""] = 1 }));
        Assert.Equal("—", TeamModel.AgErrorsText(new Dictionary<string, int>()));
        Assert.Equal("Task / thiết kế (Claude)", TeamModel.CauseLabel("TK"));
        Assert.Equal("Nhân viên (agent)", TeamModel.CauseLabel("AG"));
        Assert.Equal("Công cụ / cổng kiểm tra", TeamModel.CauseLabel("CK"));
        Assert.Equal("Nghiệp vụ / chủ dự án", TeamModel.CauseLabel("NV"));
        Assert.Equal("Hạ tầng / provider", TeamModel.CauseLabel("HT"));
        Assert.Equal("ZZ", TeamModel.CauseLabel("ZZ"));
        Assert.Equal(("50%", "67%", "—"), (TeamModel.Pct(0.5), TeamModel.Pct(0.666), TeamModel.Pct(null)));
        Assert.Equal(2, TeamModel.StageScoreOf(V406, 4));
        Assert.Null(TeamModel.StageScoreOf(V403, 2));    // không có dòng
        Assert.Null(TeamModel.StageScoreOf(V403, 6));    // có dòng nhưng không điểm
    }

    // ───────────── Quy trình & cải tiến ─────────────

    [Fact]
    public void Process_page_loads_everything_unfiltered()
    {
        var p = new ProcessModel(new Fake(() => Data(V406, V403)));
        p.OnGet();
        Assert.Equal(7, p.StageAverages.Count);
        Assert.Equal(4, p.Issues.Count);
        Assert.Equal(4, p.IssueTotal);
        Assert.Equal(TeamModel.Causes, p.IssueCounts.Keys.ToArray());
        Assert.Equal((2, 1, 1, 0), (p.IssueCounts["TK"], p.IssueCounts["CK"], p.IssueCounts["HT"], p.IssueCounts["AG"]));
        Assert.Equal("IMP-001", Assert.Single(p.Improvements).Id);
        Assert.Null(p.Error);
    }

    [Theory]
    [InlineData("TK", 2)]
    [InlineData("tk", 2)]
    [InlineData("HT", 1)]
    [InlineData("NV", 0)]
    public void Process_issues_filter_by_cause(string cause, int count)
    {
        var p = new ProcessModel(new Fake(() => Data(V406, V403))) { Cause = cause };
        p.OnGet();
        Assert.Equal(count, p.Issues.Count);
        Assert.Equal(4, p.IssueTotal);
        Assert.Equal(cause.ToUpperInvariant(), p.Cause);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("<b>")]
    public void Process_unknown_cause_shows_all(string cause)
    {
        var p = new ProcessModel(new Fake(() => Data(V406, V403))) { Cause = cause };
        p.OnGet();
        Assert.Equal("", p.Cause);
        Assert.Equal(4, p.Issues.Count);
    }

    [Fact]
    public void Process_error_is_reported()
    {
        var p = new ProcessModel(new Fake(() => throw new IOException("hỏng")));
        p.OnGet();
        Assert.Equal("Không đọc được dữ liệu đánh giá: hỏng", p.Error);
        Assert.Empty(p.Issues);
        Assert.All(TeamModel.Causes, c => Assert.Equal(0, p.IssueCounts[c]));
    }

    [Fact]
    public void Bar_helpers()
    {
        Assert.Equal(300.0, ProcessModel.BarWidth(5.0));
        Assert.Equal(180.0, ProcessModel.BarWidth(3.0));
        Assert.Equal(246.0, ProcessModel.BarWidth(4.1));
        Assert.Equal(100.0, ProcessModel.BarWidth(2.5, 200));
        Assert.Equal(0.0, ProcessModel.BarWidth(null));
        Assert.Equal(300.0, ProcessModel.BarWidth(7.0));
        Assert.Equal("b-ok", ProcessModel.BarCss(4.5));
        Assert.Equal("b-mid", ProcessModel.BarCss(4.4));
        Assert.Equal("b-mid", ProcessModel.BarCss(3.5));
        Assert.Equal("b-bad", ProcessModel.BarCss(3.0));
        Assert.Equal("b-none", ProcessModel.BarCss(null));
    }

    // ───────────── Markup ─────────────

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
        var team = Source(Path.Combine("hub", "Pages", "Team.cshtml"));
        Assert.Contains("@page \"/Team\"", team);
        Assert.Contains("TeamModel.HeatCss(", team);
        Assert.Contains("TeamModel.CauseLabel(", team);
        Assert.Contains("/t/", team);
        Assert.Contains("Chưa có task nào theo quy trình mới", team);
        Assert.DoesNotContain("Html.Raw", team);

        var process = Source(Path.Combine("hub", "Pages", "Process.cshtml"));
        Assert.Contains("@page \"/Process\"", process);
        Assert.Contains("ProcessModel.BarWidth(", process);
        Assert.Contains("?cause=", process);
        Assert.Contains("CultureInfo.InvariantCulture", process);
        Assert.Contains("Chưa có chu kỳ cải tiến nào", process);
        Assert.Contains("Không có vấn đề nào", process);
        Assert.DoesNotContain("Html.Raw", process);
    }
}
