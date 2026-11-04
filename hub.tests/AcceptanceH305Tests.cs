using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-05 (Claude viết trước — agent KHÔNG sửa). Mẫu phiếu chép từ requests/R-002, R-003 thật (rút gọn).
public sealed class AcceptanceH305Tests : IDisposable
{
    private const string R003 = """
        # R-003 · Nâng cấp toàn diện Hub team (v3) + đánh giá quy trình mỗi task

        - **Nhận lúc:** ~00:00 24/10/2026 (phiếu mở 00:11)
        - **Trạng thái:** 🟡 Đang làm
        - **Lời chủ dự án:**
          > mỗi 1 task xong cần đánh giá từ quy tình quản lí gia ovieejc đến
          > tôi muốn nagw cấp toàn diện hub team
        - không phải trích dẫn

        ## 1. Phân tích (Claude)
        - Thiết kế: `docs/design/HUB-V3.md`

        ## 2. Chia việc
        | Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
        |---|---|---|---|---|---|
        | H3-01 dữ liệu insights | AITeam | Minh (A) | ~320 | mặc định | lớp thuần |
        | P0-4/5 routing + cấm tự duyệt | AITeam | (sau) | | | |
        | DI + team.ps1 merge kiểm tra duyệt | AITeam | Claude | ~60 | | nhỏ |
        | | | | | | |

        ## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
        | Giờ | Sự kiện | Ghi chú |
        |---|---|---|
        | 00:11 | Nhận yêu cầu | |
        | 00:12 | Xong thiết kế HUB-V3.md + mockup | chờ chủ dự án duyệt mockup |
        | 00:30 | | dòng trống sự kiện ⇒ bỏ |

        ## 4. Đánh giá sau khi xong (Claude)
        | Chỉ số | Giá trị |
        |---|---|
        | Tổng thời gian (nhận → báo xong) | |
        """;

    private const string R001Done = """
        # R-001 · Hoàn tất kiểm tra bố cục
        - **Trạng thái:** ✅ Xong

        ## 4. Đánh giá sau khi xong (Claude)
        | Chỉ số | Giá trị |
        |---|---|
        | Tổng thời gian (nhận → báo xong) | 9 giờ 10 phút |
        """;

    private readonly TestEnvironment _env = new();
    private string Dir => Path.Combine(_env.AITeamRoot, "requests");

    public AcceptanceH305Tests() => Directory.CreateDirectory(Dir);
    public void Dispose() => _env.Dispose();

    // ───────────── Parser ─────────────

    [Fact]
    public void Parses_real_ticket()
    {
        var r = RequestParser.Parse("R-003-nang-cap-toan-dien-hub.md", R003);
        Assert.NotNull(r);
        Assert.Equal("R-003", r!.Id);
        Assert.Equal("Nâng cấp toàn diện Hub team (v3) + đánh giá quy trình mỗi task", r.Title);
        Assert.Equal("🟡 Đang làm", r.Status);
        Assert.Equal("~00:00 24/10/2026 (phiếu mở 00:11)", r.ReceivedAt);
        Assert.Equal("mỗi 1 task xong cần đánh giá từ quy tình quản lí gia ovieejc đến\ntôi muốn nagw cấp toàn diện hub team", r.Verbatim);
        Assert.Equal("R-003-nang-cap-toan-dien-hub.md", r.FileName);
        Assert.False(r.Evaluated);

        Assert.Equal(new[] { "H3-01", "P0-4/5", "" }, r.Tasks.Select(t => t.Code));
        Assert.Equal(("H3-01 dữ liệu insights", "AITeam", "Minh (A)"), (r.Tasks[0].Title, r.Tasks[0].Project, r.Tasks[0].Who));
        Assert.Equal("DI + team.ps1 merge kiểm tra duyệt", r.Tasks[2].Title);

        Assert.Equal(2, r.Events.Count);
        Assert.Equal(("00:12", "Xong thiết kế HUB-V3.md + mockup", "chờ chủ dự án duyệt mockup"), (r.Events[1].Time, r.Events[1].Text, r.Events[1].Note));
        Assert.Equal("", r.Events[0].Note);
    }

    [Fact]
    public void Evaluated_and_missing_sections()
    {
        var done = RequestParser.Parse("R-001-x.md", R001Done)!;
        Assert.True(done.Evaluated);
        Assert.Equal("Hoàn tất kiểm tra bố cục", done.Title);
        Assert.Equal("", done.Verbatim);
        Assert.Empty(done.Tasks);
        Assert.Empty(done.Events);
        Assert.Equal("", done.ReceivedAt);

        Assert.Null(RequestParser.Parse("ghi-chu.md", R003));
        Assert.Null(RequestParser.Parse("R-1-x.md", R003));
        var empty = RequestParser.Parse("R-009-x.md", "");
        Assert.NotNull(empty);
        Assert.Equal(("R-009", ""), (empty!.Id, empty.Title));
    }

    // ───────────── Dịch vụ ─────────────

    [Fact]
    public void Service_lists_newest_first_and_guards_ids()
    {
        File.WriteAllText(Path.Combine(Dir, "R-001-x.md"), R001Done);
        File.WriteAllText(Path.Combine(Dir, "R-003-y.md"), R003);
        File.WriteAllText(Path.Combine(Dir, "R-002-z.md"), "# R-002 · Hai");
        File.WriteAllText(Path.Combine(Dir, "README.md"), "# không phải phiếu");
        var svc = new RequestService(_env.Paths);

        Assert.Equal(new[] { "R-003", "R-002", "R-001" }, svc.GetAll().Select(r => r.Id));
        Assert.Equal("Hai", svc.Get("R-002")!.Title);
        Assert.Null(svc.Get("R-004"));
        Assert.Null(svc.Get("..\\R-001"));
        Assert.Null(svc.Get("R-001-x"));
        Assert.Null(svc.Get(""));
    }

    [Fact]
    public void Service_missing_folder_is_empty()
    {
        Directory.Delete(Dir);
        Assert.Empty(new RequestService(_env.Paths).GetAll());
    }

    // ───────────── Trang ─────────────

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private (RequestsModel Page, FakeInboxService Inbox) Page(string? id = null)
    {
        File.WriteAllText(Path.Combine(Dir, "R-001-x.md"), R001Done);
        File.WriteAllText(Path.Combine(Dir, "R-003-y.md"), R003);
        var inbox = new FakeInboxService();
        var p = new RequestsModel(new RequestService(_env.Paths), inbox, new FakeTeamConfigService())
        {
            Id = id,
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
        };
        return (p, inbox);
    }

    [Fact]
    public void Page_has_antiforgery_attribute()
        => Assert.True(typeof(RequestsModel).IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true));

    [Theory]
    [InlineData(null, "R-003")]
    [InlineData("R-001", "R-001")]
    [InlineData("R-777", "R-003")]
    [InlineData("<script>", "R-003")]
    public void OnGet_selects_thread(string? id, string expected)
    {
        var (p, _) = Page(id);
        p.OnGet();
        Assert.Equal(2, p.Threads.Count);
        Assert.Equal(expected, p.Selected!.Id);
        Assert.NotEmpty(p.Projects);
        Assert.All(p.Waiting, w => Assert.False(w.Done));
        Assert.NotEmpty(p.Waiting);
    }

    [Fact]
    public void Post_valid_request_goes_to_inbox()
    {
        var (p, inbox) = Page();
        var project = new FakeTeamConfigService().GetProjects()[0].Name;
        var before = inbox.GetItems().Count;
        Assert.IsType<RedirectToPageResult>(p.OnPostSend(project, "  Thêm biểu đồ token  "));
        Assert.Equal(before + 1, inbox.GetItems().Count);
        Assert.Equal("Thêm biểu đồ token", inbox.GetItems()[^1].Text);
        Assert.StartsWith("Đã gửi.", p.Message);

        Assert.IsType<RedirectToPageResult>(p.OnPostSend(RequestsModel.NewProject, "Dự án mới hoàn toàn"));
        Assert.Equal(before + 2, inbox.GetItems().Count);
    }

    [Theory]
    [InlineData("   ", "Nội dung yêu cầu không được để trống.")]
    [InlineData("TOO_LONG", "Nội dung quá dài (tối đa 4000 ký tự).")]
    public void Post_rejects_bad_text(string text, string error)
    {
        var (p, inbox) = Page();
        var before = inbox.GetItems().Count;
        if (text == "TOO_LONG") text = new string('x', RequestsModel.MaxText + 1);
        Assert.IsType<RedirectToPageResult>(p.OnPostSend(new FakeTeamConfigService().GetProjects()[0].Name, text));
        Assert.Equal(error, p.ErrorMessage);
        Assert.Equal(before, inbox.GetItems().Count);
    }

    [Fact]
    public void Post_rejects_unknown_project()
    {
        var (p, inbox) = Page();
        var before = inbox.GetItems().Count;
        p.OnPostSend("Dự án lạ: chèn", "abc");
        Assert.Equal("Dự án không hợp lệ.", p.ErrorMessage);
        Assert.Equal(before, inbox.GetItems().Count);
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
        var req = Source(Path.Combine("hub", "Pages", "Requests.cshtml"));
        Assert.Contains("@page \"/Requests\"", req);
        Assert.Contains("asp-page-handler=\"Send\"", req);
        Assert.Contains("@Html.AntiForgeryToken()", req);
        Assert.Contains("maxlength=\"4000\"", req);
        Assert.Contains("?id=", req);
        Assert.Contains("msg me", req);
        Assert.Contains("msg bot", req);
        Assert.Contains("white-space: pre-wrap", req);
        Assert.Contains("Chưa có phiếu yêu cầu nào", req);
        Assert.DoesNotContain("Html.Raw", req);

        var task = Source(Path.Combine("hub", "Pages", "Task.cshtml"));
        Assert.Contains("switchTaskTab('timeline')", task);
        Assert.Contains("switchTaskTab('process')", task);
        Assert.Contains("Model.Insight", task);
        Assert.Contains("UiFormat.HeatCss(", task);
        Assert.Contains("Chưa có số đo thời gian cho task này", task);
        Assert.Contains("Chưa đánh giá quy trình", task);
        // 2 chỗ Html.Raw cũ đã an toàn (JSON lần chạy; tham số drawer chỉ là số / "first"). Không được thêm chỗ mới.
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(task, "Html\\.Raw").Count);
    }
}
