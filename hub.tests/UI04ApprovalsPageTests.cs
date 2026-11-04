using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Tests;

// R-014 UI-04 (Claude): tab "Chờ anh" chỉ việc nhạy cảm, tab "Claude tự quyết" + mở lại có lý do.
public sealed class UI04ApprovalsPageTests
{
    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private sealed class ListApprovals(IReadOnlyList<Approval> items) : IApprovalService
    {
        public IReadOnlyList<Approval> GetAll() => items;
        public Approval? Get(string id) => items.FirstOrDefault(a => a.Id == id);
        public Approval Decide(string id, bool approve, string? note) => throw new InvalidOperationException("không dùng");
    }

    private sealed class FakeLog : IClaudeDecisionLog
    {
        public List<ClaudeDecision> Items { get; } =
        [
            new("D-20261104010203-ab12", DateTimeOffset.Now, "merge", "Merge W2-02", "AITeam", "W2-02", "R-014", "QC đạt", false, null, null),
        ];
        public IReadOnlyList<ClaudeDecision> List(int max = 200) => Items;
        public ClaudeDecision Reopen(string id, string note)
        {
            if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Cần ghi lý do mở lại.");
            var i = Items.FindIndex(d => d.Id == id);
            if (i < 0) throw new KeyNotFoundException("Không tìm thấy quyết định.");
            return Items[i] = Items[i] with { Reopened = true, ReopenNote = note, ReopenedAt = DateTimeOffset.Now };
        }
    }

    private static Approval A(string id, ApprovalKind kind, ApprovalRisk risk = ApprovalRisk.Medium, int minute = 0) =>
        new(id, kind, "Đơn " + id, "chi tiết", "AITeam", "T-1", null, [], "Claude", new DateTime(2026, 11, 4, 8, minute, 0), ApprovalStatus.Pending, null, null)
        { Risk = risk };

    private static ApprovalsModel Page(IApprovalService svc, IClaudeDecisionLog log) => new(svc, log)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
    };

    [Fact]
    public void Owner_tab_only_has_sensitive_kinds_high_risk_first()
    {
        var svc = new ListApprovals([
            A("A-001", ApprovalKind.Merge),
            A("A-002", ApprovalKind.Security, ApprovalRisk.Medium, 1),
            A("A-003", ApprovalKind.Secret, ApprovalRisk.High, 2),
            A("A-004", ApprovalKind.Improvement),
            A("A-005", ApprovalKind.External, ApprovalRisk.Low, 3),
        ]);
        var p = Page(svc, new FakeLog());
        p.OnGet();
        Assert.Equal(["A-003", "A-002", "A-005"], p.OwnerPending.Select(a => a.Id));
        Assert.Equal(["A-001", "A-004"], p.ClaudePending.Select(a => a.Id));
        Assert.Equal(5, p.Pending.Count);
        Assert.Single(p.Decisions);
        Assert.Equal("owner", p.Tab);
    }

    [Theory]
    [InlineData("claude", "claude")]
    [InlineData("decided", "decided")]
    [InlineData("<script>", "owner")]
    [InlineData(null, "owner")]
    public void Unknown_tab_falls_back_to_owner(string? tab, string expected)
    {
        var p = Page(new ListApprovals([]), new FakeLog());
        p.Tab = tab;
        p.OnGet();
        Assert.Equal(expected, p.Tab);
    }

    [Fact]
    public void Reopen_requires_reason_and_redirects_to_claude_tab()
    {
        var log = new FakeLog();
        var p = Page(new ListApprovals([]), log);

        var bad = Assert.IsType<RedirectToPageResult>(p.OnPostReopen("D-20261104010203-ab12", "  "));
        Assert.Equal("claude", bad.RouteValues!["tab"]);
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
        Assert.False(log.Items[0].Reopened);

        p.ErrorMessage = null;
        Assert.IsType<RedirectToPageResult>(p.OnPostReopen("D-20261104010203-ab12", "Chưa test clone sạch"));
        Assert.Null(p.ErrorMessage);
        Assert.Contains("Đã mở lại", p.Message);
        Assert.True(log.Items[0].Reopened);

        Assert.IsType<RedirectToPageResult>(p.OnPostReopen("D-khong-co", "x"));
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
    }

    [Fact]
    public void Task_link_escapes_and_handles_missing_parts()
    {
        Assert.Null(ApprovalsModel.TaskHref(null, "T-1"));
        Assert.Null(ApprovalsModel.TaskHref("AITeam", " "));
        Assert.Equal("/p/AITeam/t/T-1", ApprovalsModel.TaskHref("AITeam", "T-1"));
        Assert.Equal("/p/a%2Fb/t/..%2Fx", ApprovalsModel.TaskHref("a/b", "../x"));
    }

    [Fact]
    public void Markup_secret_card_points_to_vault_and_never_takes_value()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "hub", "Pages", "Approvals.cshtml"))) dir = dir.Parent;
        var cshtml = File.ReadAllText(Path.Combine(dir!.FullName, "hub", "Pages", "Approvals.cshtml"));
        Assert.Contains("href=\"/Secrets\"", cshtml);
        Assert.Contains("team.ps1 secret-set -Name @a.SecretName", cshtml);
        Assert.DoesNotContain("type=\"password\"", cshtml);       // không bao giờ nhập bí mật vào ô web thường
        Assert.Contains("asp-page-handler=\"Reopen\"", cshtml);
        Assert.Contains("required", cshtml);
    }
}
