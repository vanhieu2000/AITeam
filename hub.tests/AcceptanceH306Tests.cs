using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-06 (Claude viết trước — agent KHÔNG sửa). Trang Provider.
public sealed class AcceptanceH306Tests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 24, 10, 0, 0);
    private const string CodexUsageLimit = "You’ve hit your usage limit. … try again at Oct 24th, 2026 4:19 PM.";
    private readonly TestEnvironment _env = new();

    public AcceptanceH306Tests() => File.WriteAllText(_env.Paths.TeamJson, """{ "agy": { "exe": "C:\\Tools\\agy\\bin\\agy.exe" } }""");
    public void Dispose() => _env.Dispose();

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private sealed class FakeProvider(string id, Func<ProviderHealth> health) : IAgentProvider
    {
        public string Id => id;
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(health());
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new NotSupportedException();
    }

    private ProvidersModel Page(Func<ProviderConfig, IAgentProvider>? factory = null) => new(_env.Paths, factory)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
        Now = () => Now,
    };

    private ProviderStateStore State => new(_env.AITeamRoot);
    private static AgentRunResult Run(string id, RunStatus s, string detail) => new(id, s, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, detail);

    [Fact]
    public void Lists_default_providers_routing_and_rest_state()
    {
        State.Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now.AddMinutes(-5));
        var p = Page();
        p.OnGet();

        Assert.Null(p.Error);
        Assert.Equal(new[] { "agy", "codex" }, p.Rows.Select(r => r.Id));
        var agy = p.Rows[0];
        Assert.Equal(("Antigravity", true, "Developer", "mặc định", 2), (agy.Kind, agy.Enabled, agy.Roles, agy.Model, agy.MaxConcurrent));
        Assert.Null(agy.RestUntil);
        var codex = p.Rows[1];
        Assert.Equal(("Codex", "Reviewer, Developer", "gpt-6.1-sol"), (codex.Kind, codex.Roles, codex.Model));
        Assert.Equal(new DateTime(2026, 10, 24, 16, 19, 0), codex.RestUntil);
        Assert.Equal("QuotaExhausted", codex.RestReason);
        Assert.StartsWith("You’ve hit your usage limit", codex.RestDetail);

        Assert.Equal(new[] { ("Developer", "agy → codex"), ("Reviewer", "codex") }, p.Routing);
        Assert.Single(p.History);
    }

    [Fact]
    public void Expired_rest_is_history_not_current()
    {
        State.Record(Run("codex", RunStatus.QuotaExhausted, "try again in 5 minutes"), Now.AddHours(-3));
        State.Record(Run("agy", RunStatus.NotLoggedIn, "Please log in"), Now.AddHours(-1));
        var p = Page();
        p.OnGet();
        Assert.Null(p.Rows.Single(r => r.Id == "codex").RestUntil);
        Assert.Equal("NotLoggedIn", p.Rows.Single(r => r.Id == "agy").RestReason);
        Assert.Equal(new[] { "agy", "codex" }, p.History.Select(h => h.ProviderId));   // mới nhất trước
    }

    [Fact]
    public void Broken_config_reports_error()
    {
        File.WriteAllText(_env.Paths.TeamJson, """{ "routing": { "Reviewer": ["ghost"] } }""");
        var p = Page();
        p.OnGet();
        Assert.StartsWith("Không đọc được cấu hình provider: ", p.Error);
        Assert.Empty(p.Rows);
        Assert.Empty(p.Routing);
    }

    [Fact]
    public void Clear_rest()
    {
        State.Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        var p = Page();
        Assert.IsType<RedirectToPageResult>(p.OnPostClear("CODEX"));
        Assert.Equal("Đã xóa trạng thái nghỉ của CODEX.", p.Message);
        Assert.Null(State.RestOf("codex", Now));

        p.OnPostClear("codex");
        Assert.Equal("codex không ở trạng thái nghỉ.", p.Message);

        p.Message = null;
        Assert.IsType<RedirectToPageResult>(p.OnPostClear("../team"));
        Assert.Equal("Provider không hợp lệ.", p.ErrorMessage);
        Assert.Null(p.Message);
    }

    [Fact]
    public void Page_has_antiforgery_attribute()
        => Assert.True(typeof(ProvidersModel).IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true));

    [Theory]
    [InlineData(true, true, "OK")]
    [InlineData(true, false, "CHƯA ĐĂNG NHẬP")]
    [InlineData(true, null, "KHÔNG RÕ")]
    [InlineData(false, null, "CHƯA CÀI")]
    public void Tag_of_health(bool installed, bool? loggedIn, string tag)
        => Assert.Equal(tag, ProvidersModel.TagOf(new ProviderHealth("x", installed, loggedIn, "")));

    [Fact]
    public async Task Health_check_returns_partial_with_first_detail_line()
    {
        var p = Page(c => new FakeProvider(c.Id, () => new ProviderHealth(c.Id, true, false, "Please log in\nchi tiết thêm")));
        var r = Assert.IsType<PartialViewResult>(await p.OnGetHealthAsync("codex"));
        Assert.Equal("_ProviderHealthPartial", r.ViewName);
        Assert.Equal(new ProviderHealthView("codex", "CHƯA ĐĂNG NHẬP", "Please log in"), r.Model);

        var bad = Assert.IsType<PartialViewResult>(await p.OnGetHealthAsync("ghost"));
        Assert.Equal(new ProviderHealthView("ghost", "LỖI", "Provider không hợp lệ."), bad.Model);

        var boom = Page(c => new FakeProvider(c.Id, () => throw new InvalidOperationException("node không chạy")));
        var e = Assert.IsType<PartialViewResult>(await boom.OnGetHealthAsync("agy"));
        Assert.Equal(new ProviderHealthView("agy", "LỖI", "node không chạy"), e.Model);
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
        var page = Source(Path.Combine("hub", "Pages", "Providers.cshtml"));
        Assert.Contains("@page \"/Providers\"", page);
        Assert.Contains("hx-get=\"?handler=Health&id=", page);
        Assert.Contains("asp-page-handler=\"Clear\"", page);
        Assert.Contains("@Html.AntiForgeryToken()", page);
        Assert.Contains("Nghỉ tới", page);
        Assert.Contains("Chưa có lần nghỉ nào", page);
        Assert.DoesNotContain("Html.Raw", page);
        var partial = Source(Path.Combine("hub", "Pages", "Shared", "_ProviderHealthPartial.cshtml"));
        Assert.Contains("@model AITeam.Hub.Pages.ProviderHealthView", partial);
        Assert.DoesNotContain("Html.Raw", partial);
    }
}
