using System.Text;
using System.Text.Json.Nodes;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-03 (Claude viết trước — agent KHÔNG sửa). Thiết kế: docs/design/HUB-V3.md mục 3, khung ApprovalService.
public sealed class AcceptanceH303Tests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly ApprovalService _svc;
    private string Dir => Path.Combine(_env.AITeamRoot, "approvals");

    public AcceptanceH303Tests()
    {
        _svc = new ApprovalService(_env.Paths);
        Directory.CreateDirectory(Dir);
    }

    public void Dispose() => _env.Dispose();

    private void Put(string id, string kind = "merge", string status = "pending", string at = "2026-10-24T01:30:00", string? extra = null, string? fileName = null)
    {
        var json = $$"""
            {
              "id": "{{id}}", "kind": "{{kind}}", "title": "Merge {{id}} <b>đậm</b>", "detail": "- Cổng KT **ĐẠT**",
              "project": "AITeam", "task": "H3-01", "commit": "4f2a9c1d", "links": ["tasks/H3-01-hub-insights-data.md"],
              "requestedBy": "Claude", "requestedAt": "{{at}}", "status": "{{status}}", "decidedAt": null, "note": null{{extra}}
            }
            """;
        File.WriteAllText(Path.Combine(Dir, (fileName ?? id) + ".json"), json, new UTF8Encoding(false));
    }

    // ───────────── Dịch vụ ─────────────

    [Fact]
    public void Reads_real_format()
    {
        Put("A-001");
        var a = _svc.Get("A-001");
        Assert.NotNull(a);
        Assert.Equal((ApprovalKind.Merge, ApprovalStatus.Pending), (a!.Kind, a.Status));
        Assert.Equal(("AITeam", "H3-01", "4f2a9c1d", "Claude"), (a.Project, a.Task, a.Commit, a.RequestedBy));
        Assert.Equal(new DateTime(2026, 10, 24, 1, 30, 0), a.RequestedAt);
        Assert.Equal(new[] { "tasks/H3-01-hub-insights-data.md" }, a.Links);
        Assert.Null(a.DecidedAt);
        Assert.Null(a.Note);
    }

    [Fact]
    public void GetAll_orders_pending_first_newest_first_and_skips_bad_files()
    {
        Put("A-001", status: "approved", at: "2026-10-24T03:00:00");
        Put("A-002", at: "2026-10-24T01:00:00");
        Put("A-003", kind: "decision", at: "2026-10-24T02:00:00");
        Put("A-004", kind: "improvement", status: "rejected", at: "2026-10-24T00:10:00");
        File.WriteAllText(Path.Combine(Dir, "A-005.json"), "{ hỏng");
        Put("A-006", kind: "la-lung");
        Put("A-007", fileName: "A-008");             // id không khớp tên file
        File.WriteAllText(Path.Combine(Dir, "README.md"), "x");

        Assert.Equal(new[] { "A-003", "A-002", "A-001", "A-004" }, _svc.GetAll().Select(a => a.Id));
    }

    [Fact]
    public void Missing_folder_gives_empty()
    {
        Directory.Delete(Dir);
        Assert.Empty(_svc.GetAll());
        Assert.Null(_svc.Get("A-001"));
    }

    [Fact]
    public void Decide_writes_only_three_fields_and_keeps_unknown_fields()
    {
        Put("A-001", extra: ", \"verify\": { \"ok\": true, \"tests\": 230 }");
        var before = DateTime.Now.AddSeconds(-2);
        var a = _svc.Decide("A-001", approve: true, note: "  ok, merge đi  ");

        Assert.Equal(ApprovalStatus.Approved, a.Status);
        Assert.Equal("ok, merge đi", a.Note);
        Assert.True(a.DecidedAt >= before && a.DecidedAt <= DateTime.Now.AddSeconds(2));

        var bytes = File.ReadAllBytes(Path.Combine(Dir, "A-001.json"));
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "không được có BOM");
        var node = JsonNode.Parse(bytes)!;
        Assert.Equal("approved", (string?)node["status"]);
        Assert.Equal("ok, merge đi", (string?)node["note"]);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$", (string?)node["decidedAt"]);
        Assert.Equal(230, (int?)node["verify"]!["tests"]);           // trường lạ giữ nguyên
        Assert.Equal("4f2a9c1d", (string?)node["commit"]);
        Assert.Equal("Merge A-001 <b>đậm</b>", (string?)node["title"]);
        Assert.DoesNotContain(Directory.GetFiles(Dir), f => !f.EndsWith(".json")); // không sót file tạm
    }

    [Fact]
    public void Reject_with_empty_note_stores_null()
    {
        Put("A-002");
        var a = _svc.Decide("A-002", approve: false, note: "   ");
        Assert.Equal(ApprovalStatus.Rejected, a.Status);
        Assert.Null(a.Note);
        Assert.Equal("rejected", (string?)JsonNode.Parse(File.ReadAllText(Path.Combine(Dir, "A-002.json")))!["status"]);
    }

    [Theory]
    [InlineData("..\\A-001")]
    [InlineData("../A-001")]
    [InlineData("A-001.json")]
    [InlineData("A-1")]
    [InlineData("C:\\x\\A-001")]
    [InlineData("")]
    public void Decide_rejects_bad_ids(string id)
    {
        Put("A-001");
        Assert.Throws<ArgumentException>(() => _svc.Decide(id, true, null));
        Assert.Null(_svc.Get(id));
    }

    [Fact]
    public void Decide_errors_missing_already_decided_long_note()
    {
        Assert.Throws<KeyNotFoundException>(() => _svc.Decide("A-009", true, null));

        Put("A-001", status: "approved");
        var original = File.ReadAllText(Path.Combine(Dir, "A-001.json"));
        Assert.Throws<InvalidOperationException>(() => _svc.Decide("A-001", false, "đổi ý"));
        Assert.Equal(original, File.ReadAllText(Path.Combine(Dir, "A-001.json")));

        Put("A-002");
        Assert.Throws<ArgumentException>(() => _svc.Decide("A-002", true, new string('x', 501)));
        Assert.Equal(ApprovalStatus.Pending, _svc.Get("A-002")!.Status);
    }

    // ───────────── Trang ─────────────

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private ApprovalsModel Page() => new(_svc)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
    };

    [Fact]
    public void Page_has_antiforgery_attribute()
        => Assert.True(typeof(ApprovalsModel).IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true));

    [Fact]
    public void OnGet_splits_pending_and_decided()
    {
        Put("A-001", status: "approved");
        Put("A-002");
        Put("A-003", kind: "decision");
        var p = Page();
        p.OnGet();
        Assert.Equal(2, p.Pending.Count);
        Assert.All(p.Pending, a => Assert.Equal(ApprovalStatus.Pending, a.Status));
        Assert.Equal("A-001", Assert.Single(p.Decided).Id);
    }

    [Fact]
    public void Post_approve_and_reject()
    {
        Put("A-001");
        Put("A-002");
        var p = Page();
        Assert.IsType<RedirectToPageResult>(p.OnPostDecide("A-001", "approve", "được"));
        Assert.Equal("Đã duyệt A-001: Merge A-001 <b>đậm</b>", p.Message);
        Assert.Equal(ApprovalStatus.Approved, _svc.Get("A-001")!.Status);

        Assert.IsType<RedirectToPageResult>(p.OnPostDecide("A-002", "reject", null));
        Assert.StartsWith("Đã từ chối A-002", p.Message);
        Assert.Equal(ApprovalStatus.Rejected, _svc.Get("A-002")!.Status);
    }

    [Fact]
    public void Post_errors_become_message_not_exception()
    {
        Put("A-001", status: "approved");
        Put("A-002");
        var p = Page();

        Assert.IsType<RedirectToPageResult>(p.OnPostDecide("../A-001", "approve", null));
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));

        p.ErrorMessage = null;
        Assert.IsType<RedirectToPageResult>(p.OnPostDecide("A-001", "reject", null));   // đã quyết
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));

        p.ErrorMessage = null;
        Assert.IsType<RedirectToPageResult>(p.OnPostDecide("A-002", "xoa-het", null)); // decision lạ
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
        Assert.Equal(ApprovalStatus.Pending, _svc.Get("A-002")!.Status);
    }

    [Fact]
    public void Razor_markup_follows_layout_and_is_xss_safe()
    {
        // Đọc file nguồn .cshtml (test chạy trong hub.tests/bin/Debug/net10.0-windows/).
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "hub", "Pages", "Approvals.cshtml"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var cshtml = File.ReadAllText(Path.Combine(dir!.FullName, "hub", "Pages", "Approvals.cshtml"));

        Assert.Contains("asp-page-handler=\"Decide\"", cshtml);
        Assert.Contains("name=\"decision\" value=\"approve\"", cshtml);
        Assert.Contains("name=\"decision\" value=\"reject\"", cshtml);
        Assert.Contains("name=\"note\"", cshtml);
        Assert.Contains("maxlength=\"500\"", cshtml);
        Assert.Contains("MarkdownRenderer.Render(", cshtml);    // Detail là markdown đã lọc an toàn
        Assert.DoesNotContain("Html.Raw", cshtml);              // Title / Note không bao giờ in thô
        Assert.Contains("Không có việc nào chờ anh duyệt", cshtml);
    }
}
