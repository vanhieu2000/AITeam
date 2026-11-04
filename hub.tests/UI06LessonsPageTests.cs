using AITeam.Core.Lessons;
using AITeam.Core.Workflow;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// R-014 UI-06 (Claude): xem lại từng bước + mẹo & bài học (nháp → luật chuẩn).
public sealed class UI06LessonsPageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-ui06-" + Guid.NewGuid().ToString("N"));
    private readonly LessonService _lessons;
    private readonly WorkflowConfigService _workflow;

    public UI06LessonsPageTests()
    {
        Directory.CreateDirectory(_root);
        _lessons = new LessonService(_root);
        _workflow = new WorkflowConfigService(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private static T WithTemp<T>(T page) where T : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        page.TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData());
        return page;
    }

    private sealed class FakeReports : IReportService
    {
        public ReportData Query(ReportFilter filter, DateTimeOffset? now = null) => throw new NotSupportedException();
        public string ExportCsv(ReportFilter filter, string table) => throw new NotSupportedException();
        public RequestTimeline? Timeline(string requestId) => requestId != "R-014" ? null : new RequestTimeline("R-014", "Văn phòng <AI>",
        [
            new(1, "Tiếp nhận", "Claude", null, DateTimeOffset.Now.AddHours(-5), null, null, null, 0, 0, [], null, []),
            new(2, "Giao V10-02 / code", "Antigravity", "gemini-3.8", DateTimeOffset.Now.AddHours(-4), DateTimeOffset.Now.AddHours(-3),
                42, 120_000, 2, 1, ["[TK] (test) thiếu ca ESC"], 75, ["V10-02"]),
        ], 5, ["Một cảnh báo"]);
    }

    [Fact]
    public void Review_rejects_bad_id_and_reports_missing_request()
    {
        var p = WithTemp(new RequestReviewModel(new FakeReports(), _lessons));
        Assert.IsType<NotFoundResult>(p.OnGet("../R-014"));
        Assert.IsType<PageResult>(p.OnGet("R-999"));
        Assert.Contains("Không tìm thấy", p.Error);
        Assert.IsType<PageResult>(p.OnGet("R-014"));
        Assert.Null(p.Error);
        Assert.Equal(2, p.Timeline!.Steps.Count);
    }

    [Fact]
    public void Review_step_note_becomes_draft_with_safe_source()
    {
        var p = WithTemp(new RequestReviewModel(new FakeReports(), _lessons));
        Assert.IsType<RedirectToPageResult>(p.OnPostLesson("R-014", "meo", "Viết test ESC so ordinal", "xUnit so theo văn hóa bỏ qua ESC",
            "Giao V10-02 / code", "V10-02", "Antigravity"));
        Assert.Null(p.ErrorMessage);
        var l = Assert.Single(_lessons.Lessons());
        Assert.Equal("R-014", l.RequestId);
        Assert.Equal("V10-02", l.TaskId);
        Assert.Equal("Giao V10-02  code", l.Step);       // '/' bị bỏ
        Assert.Equal("Antigravity", l.Ai);
        Assert.Equal(LessonStatus.Draft, l.Status);

        Assert.IsType<RedirectToPageResult>(p.OnPostLesson("R-014", "xoa", "x", "", null, "../x", null));
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
        Assert.Single(_lessons.Lessons());
        Assert.IsType<NotFoundResult>(p.OnPostLesson("R-1", "meo", "x", "", null, null, null));
    }

    [Theory]
    [InlineData("../../etc", ".etc")]
    [InlineData("a\\b/c", "abc")]
    [InlineData("   ", null)]
    [InlineData("//", null)]
    public void Safe_label_strips_path_chars(string input, string? expected) =>
        Assert.Equal(expected, RequestReviewModel.SafeLabel(input));

    [Fact]
    public void Lessons_page_promotes_drops_and_retires()
    {
        var a = _lessons.AddDraft("Giữ BOM", "Agent hay làm mất BOM.", "meo", "R-014", null, "code_loi", "opencode");
        var b = _lessons.AddDraft("Bỏ cái này", "", "cai-tien", null, null, null, null);

        var p = WithTemp(new LessonsModel(_lessons, _workflow));
        p.OnGet();
        Assert.Equal(2, p.Items.Count);
        Assert.Equal(2, p.Counts["draft"]);
        Assert.NotEmpty(p.TaskTypes);
        Assert.NotEmpty(p.Steps);

        Assert.IsType<RedirectToPageResult>(p.OnPostPromote(a.Id, "Giữ BOM UTF-8", "Mọi file .cs giữ BOM.", ["tinh-nang-loi"], ["code_loi"], "opencode, agy"));
        Assert.Null(p.ErrorMessage);
        var rule = Assert.Single(_lessons.Rules());
        Assert.Equal(["opencode", "agy"], rule.Ais);
        Assert.Equal(["tinh-nang-loi"], rule.TaskTypes);

        Assert.IsType<RedirectToPageResult>(p.OnPostPromote(a.Id, "lại", "x", null, null, null));
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));     // đã nâng rồi

        p.ErrorMessage = null;
        p.OnPostDrop(b.Id);
        Assert.Null(p.ErrorMessage);
        p.OnPostRetire(rule.Id);
        Assert.Equal(RuleStatus.Retired, _lessons.Rules().Single().Status);

        p.OnPostDrop("../LS-001");
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));

        var q = WithTemp(new LessonsModel(_lessons, _workflow)) ;
        q.View = "rules";
        q.OnGet();
        Assert.Single(q.RuleItems);
        Assert.Equal(0, q.Counts["rules"]);
    }

    [Fact]
    public void Lessons_filters_and_bad_view_fallback()
    {
        _lessons.AddDraft("Một", "a", "meo", "R-001", null, null, "codex");
        _lessons.AddDraft("Hai", "b", "cai-tien", "R-002", null, null, null);
        var p = WithTemp(new LessonsModel(_lessons, _workflow));
        p.View = "<x>"; p.Kind = "cai-tien";
        p.OnGet();
        Assert.Equal("draft", p.View);
        Assert.Equal("Hai", Assert.Single(p.Items).Title);

        p = WithTemp(new LessonsModel(_lessons, _workflow));
        p.Ai = "codex";
        p.OnGet();
        Assert.Equal("Một", Assert.Single(p.Items).Title);
        Assert.Equal("/Reports/Lessons?ai=codex", p.Link());
    }

    [Fact]
    public async Task Pages_render_through_real_host()
    {
        _lessons.AddDraft("<script>alert(1)</script>", "x", "meo", "R-014", null, null, null);
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton<IReportService>(new FakeReports());
            s.AddSingleton<ILessonService>(_lessons);
        });
        var raw = await host.Client.GetStringAsync("/r/R-014/review");
        Assert.Contains("&lt;AI&gt;", raw);
        Assert.DoesNotContain("<AI>", raw);
        var review = System.Net.WebUtility.HtmlDecode(raw);
        Assert.Contains("V10-02", review);
        Assert.Contains("Ghi mẹo", review);
        Assert.Contains("Đề xuất cải tiến", review);
        Assert.Contains("handler=Lesson", review);
        Assert.Contains("__RequestVerificationToken", review);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await host.Client.GetAsync("/r/R-1x/review")).StatusCode);

        var lessons = await host.Client.GetStringAsync("/Reports/Lessons");
        Assert.DoesNotContain("<script>alert(1)</script>", lessons);
        Assert.Contains("Nâng thành luật chuẩn", System.Net.WebUtility.HtmlDecode(lessons));
    }
}
