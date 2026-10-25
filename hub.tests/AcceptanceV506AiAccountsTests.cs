using System.Net;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU V5-06 (Claude viết trước — agent KHÔNG sửa). Trang Tài khoản AI.
public sealed class AcceptanceV506AiAccountsTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly DateTime _now = new(2026, 10, 25, 15, 0, 0);
    public void Dispose() => _env.Dispose();

    private string AccountsFile => Path.Combine(_env.RootDir, "LocalAppData", "AITeam", "config", "accounts.json");

    private sealed class Runs(int running) : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => Array.Empty<AgentRun>();
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => Array.Empty<AgentRun>();
        public IReadOnlyList<RunningAgent> GetRunning() => Enumerable.Range(0, running).Select(i => new RunningAgent(100 + i, DateTime.Now, "AITeam", "T-" + i, "agy")).ToList();
        public bool Stop(int pid) => false;
    }

    private readonly List<string> _probed = new();

    private AiAccountsService Service(int running = 0, Func<string, CancellationToken, Task<ProviderHealth>>? probe = null) => new(
        _env.Paths, new Runs(running), new AiAccountStore(AccountsFile),
        probe ?? ((id, ct) => { lock (_probed) _probed.Add(id); return Task.FromResult(new ProviderHealth(id, true, id != "gemini", id == "gemini" ? "Chưa đăng nhập" : "Logged in")); }),
        () => _now);

    [Fact]
    public void List_does_not_probe_and_uses_defaults()
    {
        var rows = Service().List();
        Assert.Equal(AiCatalog.All.Select(s => s.Id), rows.Select(r => r.Service.Id));
        Assert.Empty(_probed);
        var byId = rows.ToDictionary(r => r.Service.Id);
        Assert.Equal((AiStatus.Disconnected, "Chưa kiểm tra"), (byId["antigravity"].Status, byId["antigravity"].StatusText));
        Assert.Equal(AiStatus.ManualOnly, byId["gemini"].Status);
        Assert.Equal(AiStatus.Disconnected, byId["deepseek"].Status);
    }

    [Fact]
    public async Task Check_caches_result_and_marks_busy_and_rest()
    {
        var s = Service(running: 1);
        var codex = await s.CheckAsync("CODEX", CancellationToken.None);
        Assert.Equal((AiStatus.Connected, _now), (codex.Status, codex.CheckedAt));
        Assert.Equal(new[] { "codex" }, _probed);
        Assert.Equal(AiStatus.Connected, s.List().Single(r => r.Service.Id == "codex").Status);   // đệm

        var agy = await s.CheckAsync("antigravity", CancellationToken.None);
        Assert.Equal(AiStatus.Busy, agy.Status);   // đang có agent chạy

        new AITeam.Core.Routing.ProviderStateStore(_env.AITeamRoot).Record(
            new AgentRunResult("codex", RunStatus.QuotaExhausted, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "try again at 4:19 PM"), _now);
        var rested = s.List().Single(r => r.Service.Id == "chatgpt");
        Assert.Equal(AiStatus.ManualOnly, rested.Status);   // chatgpt mặc định thủ công
        s.Save("chatgpt", "", "Automatic");
        Assert.Equal(AiStatus.RateLimited, s.List().Single(r => r.Service.Id == "chatgpt").Status);   // dùng chung trạng thái nghỉ của codex
    }

    [Fact]
    public async Task Check_errors_and_timeout()
    {
        var s = Service(probe: (id, ct) => throw new InvalidOperationException("hỏng"));
        var v = await s.CheckAsync("codex", CancellationToken.None);
        Assert.Equal((AiStatus.Error, "hỏng"), (v.Status, v.Detail));

        var slow = Service(probe: async (id, ct) => { await Task.Delay(Timeout.Infinite, ct); return null!; });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var t = await slow.CheckAsync("codex", cts.Token);   // huỷ từ ngoài cũng thành dòng Error, không ném
        Assert.Equal(AiStatus.Error, t.Status);

        await Assert.ThrowsAsync<ArgumentException>(() => Service().CheckAsync("../x", CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => Service().CheckAsync(null, CancellationToken.None));
    }

    [Fact]
    public void Save_validates_mode()
    {
        var s = Service();
        var v = s.Save("gemini", "Google", "automatic");
        Assert.Equal(("Google", AiMode.Automatic, AiStatus.Disconnected), (v.Alias, v.Mode, v.Status));
        foreach (var bad in new[] { null, "", "1", "Auto", "Manual,Automatic" })
            Assert.Equal("Chế độ không hợp lệ.", Assert.Throws<ArgumentException>(() => s.Save("gemini", "x", bad)).Message);
        Assert.Throws<InvalidOperationException>(() => s.Save("grok", "x", "Automatic"));
        Assert.Throws<ArgumentException>(() => s.Save("nope", "x", "Manual"));
    }

    [Fact]
    public async Task Default_probe_for_browser_services_runs_nothing()
    {
        var probe = AiAccountsService.DefaultProbe(_env.Paths);
        Assert.Equal(new ProviderHealth("deepseek", false, null, "Chưa có connector tự động"), await probe("deepseek", CancellationToken.None));
        var cl = await probe("claude", CancellationToken.None);   // gemini nay qua agy (chạy thật) ⇒ thử claude: chỉ xem PATH
        Assert.Equal("claude", cl.ProviderId);
    }

    // ───────────── Trang ─────────────

    private AiAccountsModel Page(AiAccountsService s) => new(s)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new NoTempData()),
    };

    private sealed class NoTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    [Fact]
    public void Page_get_save_disable()
    {
        var s = Service();
        var p = Page(s);
        p.OnGet();
        Assert.Equal(7, p.Rows.Count);
        Assert.Null(p.Error);

        Assert.IsType<RedirectToPageResult>(p.OnPostSave("gemini", "Google", "Automatic"));
        Assert.Equal("Đã lưu Gemini: Tự động.", p.Message);

        var q = Page(s);
        q.OnPostSave("deepseek", "x", "Automatic");
        Assert.Equal("DeepSeek chưa hỗ trợ chế độ tự động.", q.ErrorMessage);
        var r = Page(s);
        r.OnPostSave("gemini", "x", "9");
        Assert.Equal("Chế độ không hợp lệ.", r.ErrorMessage);

        var d = Page(s);
        Assert.IsType<RedirectToPageResult>(d.OnPostDisable("gemini"));
        Assert.Equal("Đã ngắt Gemini (không xóa gì).", d.Message);
        var g = s.List().Single(x => x.Service.Id == "gemini");
        Assert.Equal(("Google", AiMode.Disabled), (g.Alias, g.Mode));   // giữ tên gợi nhớ

        var e = Page(s);
        e.OnPostDisable("../x");
        Assert.Equal("Dịch vụ không hợp lệ.", e.ErrorMessage);
    }

    [Fact]
    public async Task Check_handler_returns_row_partial_or_404()
    {
        var p = Page(Service());
        var r = Assert.IsType<PartialViewResult>(await p.OnGetCheckAsync("codex"));
        Assert.Equal("_AiAccountRow", r.ViewName);
        Assert.Equal(AiStatus.Connected, Assert.IsType<AiAccountView>(r.Model).Status);
        Assert.IsType<NotFoundResult>(await p.OnGetCheckAsync("..\\x"));
    }

    [Fact]
    public void Texts()
    {
        Assert.Equal(new[] { "Tắt", "Thủ công", "Tự động" }, Enum.GetValues<AiMode>().Select(AiAccountsModel.ModeText));
        Assert.Equal(new[] { "n", "rev", "ok", "n", "run", "rev", "n", "err" }, Enum.GetValues<AiStatus>().Select(AiAccountsModel.PillClass));
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
        var page = Source(Path.Combine("hub", "Pages", "AiAccounts.cshtml"));
        Assert.Contains("@page \"/AiAccounts\"", page);
        Assert.Contains("_AiAccountRow", page);
        Assert.Contains("Không lưu mật khẩu", page);
        var row = Source(Path.Combine("hub", "Pages", "Shared", "_AiAccountRow.cshtml"));
        Assert.Contains("@model AITeam.Core.Accounts.AiAccountView", row);
        Assert.Contains("handler=Check", row);
        Assert.Contains("asp-page-handler=\"Save\"", row);
        Assert.Contains("asp-page-handler=\"Disable\"", row);
        Assert.Contains("rel=\"noopener noreferrer\"", row);
        Assert.Contains("maxlength=\"40\"", row);
        Assert.DoesNotContain("type=\"password\"", page + row);
        foreach (var src in new[] { page, row }) Assert.DoesNotContain("Html.Raw", src);
    }

    // ───────────── Dựng thật ─────────────

    private async Task WithHub(Func<HttpClient, Task> act)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(AiAccountsModel).Assembly.GetName().Name });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddSingleton<IAiAccountsService>(Service());   // đăng ký trước ⇒ AddHubV3 (TryAdd) không ghi đè; không đụng file thật của máy
        builder.Services.AddRazorPages().AddApplicationPart(typeof(AiAccountsModel).Assembly);
        builder.Services.AddAntiforgery();
        FakeServices.Register(builder.Services);
        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            await act(client);
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public Task Page_renders_and_post_needs_antiforgery() => WithHub(async c =>
    {
        var html = await c.GetStringAsync("/AiAccounts");
        Assert.Contains("DeepSeek", html);
        Assert.Contains("__RequestVerificationToken", html);
        var r = await c.PostAsync("/AiAccounts?handler=Save", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "gemini", ["mode"] = "Manual" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False(File.Exists(AccountsFile));
        var check = await c.GetAsync("/AiAccounts?handler=Check&id=codex");
        Assert.Equal(HttpStatusCode.OK, check.StatusCode);
    });
}
