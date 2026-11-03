using System.Net;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Core.Research;
using AITeam.Hub.DevData;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU V5-07b (Claude viết trước — agent KHÔNG sửa). Phòng nghiên cứu trên Hub.
public sealed class AcceptanceV507ResearchPageTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private DateTime _now = new(2026, 10, 25, 3, 0, 0);
    public void Dispose() => _env.Dispose();

    private static readonly RequestThread R005 = new("R-005", "Văn phòng mini", "🟡 Đang làm", "01:05", "nâng hub thành văn phòng mini", Array.Empty<RequestTaskRef>(), Array.Empty<RequestEvent>(), false, "R-005.md");
    private static readonly RequestThread R004 = new("R-004", "Codex dự phòng", "✅ Xong", "09:00", "tải codex cli", Array.Empty<RequestTaskRef>(), Array.Empty<RequestEvent>(), true, "R-004.md");

    private sealed class Requests : IRequestService
    {
        public IReadOnlyList<RequestThread> GetAll() => new[] { R005, R004 };
        public RequestThread? Get(string id) => GetAll().FirstOrDefault(r => r.Id == id);
    }

    private sealed class Accounts(Dictionary<string, AiMode> modes) : IAiAccountsService
    {
        public IReadOnlyList<AiAccountView> List() => AiCatalog.All.Select(s =>
        {
            var m = modes.TryGetValue(s.Id, out var x) ? x : s.DefaultMode;
            return new AiAccountView(s, "", m, m == AiMode.Automatic ? AiStatus.Connected : AiStatus.ManualOnly, m.ToString(), "", null);
        }).ToList();
        public Task<AiAccountView> CheckAsync(string? id, CancellationToken ct) => throw new NotSupportedException();
        public AiAccountView Save(string? id, string? alias, string? mode) => throw new NotSupportedException();
    }

    private sealed class Conn(string ai, Func<string, ResearchAnswer> answer) : IResearchConnector
    {
        public readonly List<string> Prompts = new();
        public string Ai => ai;
        public string Via => ai + " CLI";
        public Task<ResearchAnswer> AskAsync(string prompt, CancellationToken ct) { Prompts.Add(prompt); return Task.FromResult(answer(prompt)); }
    }

    private readonly List<Func<Task>> _pending = new();
    private readonly Conn _chatgpt = new("chatgpt", _ => new ResearchAnswer("chatgpt", true, "ChatGPT trả lời", "", RunStatus.Succeeded, TimeSpan.FromSeconds(9)));
    // Chủ dự án quyết 25/10 (sau V5-07b): chỉ ChatGPT tự động + 1 lựa chọn "Thủ công" chung (AI bất kỳ). Claude sửa test theo.
    private ResearchService Service(Dictionary<string, AiMode>? modes = null, IResearchConnector? chatgpt = null) => new(
        _env.Paths, new Requests(),
        new Accounts(modes ?? new() { ["chatgpt"] = AiMode.Automatic }),
        ai => ai == "chatgpt" ? chatgpt ?? _chatgpt : null,
        () => _now,
        f => _pending.Add(f));

    private async Task RunPending() { foreach (var f in _pending.ToList()) await f(); _pending.Clear(); }

    [Fact]
    public void Options_chatgpt_and_one_manual_choice()
    {
        var o = Service().Options();
        Assert.Equal(new[] { "chatgpt", "manual" }, o.Select(x => x.Ai));
        Assert.Equal(new[] { true, false }, o.Select(x => x.CanAsk));
        Assert.Equal(("ChatGPT", "Thủ công (AI bất kỳ)", ""), (o[0].Name, o[1].Name, o[1].OfficialUrl));
        Assert.Equal(AiMode.Manual, o[1].Mode);
        Assert.False(Service(new() { ["chatgpt"] = AiMode.Manual }).Options()[0].CanAsk);
    }

    [Fact]
    public async Task Ask_starts_automatic_and_offers_manual()
    {
        var s = Service();
        var r = s.Ask("R-005", "  So sánh UX?  ", new[] { "MANUAL", "chatgpt", "gemini", "evil", "chatgpt" }, includeRequestText: false);

        Assert.Equal("Q20261025-030000", r.QuestionId);
        Assert.Contains("Phiếu: R-005 — Văn phòng mini", r.Prompt);
        Assert.DoesNotContain("nâng hub", r.Prompt);   // không kèm nội dung phiếu
        Assert.Equal(new[] { ("chatgpt", "started"), ("manual", "manual") }, r.Outcomes.Select(x => (x.Ai, x.Kind)));   // gemini không còn là lựa chọn ⇒ bỏ qua
        Assert.Equal(1, s.RunningCount());
        Assert.All(s.Jobs("R-005"), j => Assert.True(j.Running));

        await RunPending();
        Assert.Equal(0, s.RunningCount());
        Assert.Equal(r.Prompt, Assert.Single(_chatgpt.Prompts));
        var job = Assert.Single(s.Jobs("R-005"));
        Assert.NotNull(job.FilePath);
        Assert.Null(job.Error);

        var e = Assert.Single(s.Entries("R-005"));
        Assert.Equal(("chatgpt", "chatgpt CLI", "So sánh UX?", "ChatGPT trả lời"), (e.Ai, e.Via, e.Question, e.Answer));
        Assert.Empty(s.Jobs("R-004"));
    }

    [Fact]
    public async Task Failed_answer_becomes_job_error_and_disabled_is_skipped()
    {
        var bad = new Conn("chatgpt", _ => new ResearchAnswer("chatgpt", false, "", "Codex chưa đăng nhập (chạy `codex login`)", RunStatus.NotLoggedIn, TimeSpan.FromSeconds(2)));
        var s = Service(chatgpt: bad);
        s.Ask("R-005", "Hỏi?", new[] { "chatgpt" }, false);
        await RunPending();
        Assert.Equal("Codex chưa đăng nhập (chạy `codex login`)", Assert.Single(s.Jobs("R-005")).Error);
        Assert.Empty(s.Entries("R-005"));

        var off = Service(new() { ["chatgpt"] = AiMode.Disabled }).Ask("R-005", "Hỏi?", new[] { "chatgpt" }, false);
        Assert.Equal(("skipped", "Đang tắt — bật ở trang Tài khoản AI"), (off.Outcomes[0].Kind, off.Outcomes[0].Text));
    }

    [Fact]
    public async Task Ask_includes_request_text_only_when_asked()
    {
        var r = Service().Ask("R-005", "Hỏi?", new[] { "manual" }, includeRequestText: true);
        Assert.Contains("<<<DỮ LIỆU\nnâng hub thành văn phòng mini\nDỮ LIỆU>>>", r.Prompt);
        Assert.Equal(("manual", "manual", "Copy lời nhắn, dán câu trả lời"), (r.Outcomes[0].Ai, r.Outcomes[0].Kind, r.Outcomes[0].Text));
        await RunPending();
        Assert.Empty(_chatgpt.Prompts);
    }

    [Fact]
    public async Task One_question_per_ai_at_a_time_and_min_interval()
    {
        var s = Service();
        s.Ask("R-005", "Câu 1", new[] { "chatgpt" }, false);
        var busy = s.Ask("R-005", "Câu 2", new[] { "chatgpt" }, false);
        Assert.Equal(("skipped", "Đang hỏi câu trước"), (busy.Outcomes[0].Kind, busy.Outcomes[0].Text));

        await RunPending();
        _now = _now.AddSeconds(14.2);
        var wait = s.Ask("R-005", "Câu 3", new[] { "chatgpt" }, false);
        Assert.Equal(("skipped", "Đợi 46 giây nữa"), (wait.Outcomes[0].Kind, wait.Outcomes[0].Text));

        _now = _now.AddSeconds(46);
        Assert.Equal("started", s.Ask("R-005", "Câu 4", new[] { "chatgpt" }, false).Outcomes[0].Kind);
    }

    [Theory]
    [InlineData("R-999", "Hỏi?", "chatgpt", "Phiếu không hợp lệ.")]
    [InlineData("../R-005", "Hỏi?", "chatgpt", "Phiếu không hợp lệ.")]
    [InlineData("R-005", "Hỏi?", "evil", "Chọn ít nhất 1 AI.")]
    [InlineData("R-005", "   ", "chatgpt", "Cần nội dung câu hỏi.")]
    public void Ask_rejects(string req, string q, string ai, string error)
    {
        var s = Service();
        Assert.Equal(error, Assert.Throws<ArgumentException>(() => s.Ask(req, q, new[] { ai }, false)).Message);
        Assert.Empty(_pending);
        Assert.Equal(0, s.RunningCount());
    }

    [Fact]
    public void Ask_with_no_ai_list() => Assert.Equal("Chọn ít nhất 1 AI.", Assert.Throws<ArgumentException>(() => Service().Ask("R-005", "Hỏi?", null, false)).Message);

    [Fact]
    public void Connector_exception_becomes_job_error()
    {
        var s = new ResearchService(_env.Paths, new Requests(), new Accounts(new() { ["chatgpt"] = AiMode.Automatic }),
            _ => new Conn("chatgpt", _ => throw new IOException("mạng")), () => _now, f => f().GetAwaiter().GetResult());
        s.Ask("R-005", "Hỏi?", new[] { "chatgpt" }, false);
        Assert.Equal("Lỗi: mạng", Assert.Single(s.Jobs("R-005")).Error);
    }

    [Fact]
    public void Save_manual_answer()
    {
        var s = Service();
        var e = s.SaveManual("R-005", "Q20261025-030000", "Manual", "Hỏi?", "  AI nào đó trả lời  ", "https://x.example");
        Assert.Equal(("manual", "Thủ công (dán từ web)", "AI nào đó trả lời", "https://x.example"), (e.Ai, e.Via, e.Answer, e.Sources));
        Assert.Single(s.Entries("R-005"));
        Assert.StartsWith("# Nghiên cứu R-005 · Thủ công (AI bất kỳ)\n", File.ReadAllText(e.FilePath).Replace("\r\n", "\n"));

        Assert.Equal("Phiếu không hợp lệ.", Assert.Throws<ArgumentException>(() => s.SaveManual("R-999", "Q20261025-030000", "manual", "q", "a", null)).Message);
        Assert.Equal("AI không hợp lệ.", Assert.Throws<ArgumentException>(() => s.SaveManual("R-005", "Q20261025-030000", "deepseek", "q", "a", null)).Message);
        Assert.Equal("Cần nội dung câu trả lời.", Assert.Throws<ArgumentException>(() => s.SaveManual("R-005", "Q20261025-030000", "manual", "q", " ", null)).Message);
        Assert.Equal("Câu trả lời quá dài (tối đa 20000 ký tự).", Assert.Throws<ArgumentException>(() => s.SaveManual("R-005", "Q20261025-030000", "manual", "q", new string('a', 20001), null)).Message);
        Assert.Equal("Cần nội dung câu hỏi.", Assert.Throws<ArgumentException>(() => s.SaveManual("R-005", "Q20261025-030000", "manual", "", "a", null)).Message);
        Assert.Throws<ArgumentException>(() => s.SaveManual("R-005", "..\\x", "manual", "q", "a", null));
        Assert.Empty(s.Entries("../.."));
    }

    // ───────────── Trang ─────────────

    [Fact]
    public void Page_get_picks_request()
    {
        var p = new ResearchModel(Service(), new Requests());
        p.OnGet("R-004");
        Assert.Equal("R-004", p.Current!.Id);
        Assert.Equal(2, p.Options.Count);
        var q = new ResearchModel(Service(), new Requests());
        q.OnGet("../x");
        Assert.Equal("R-005", q.Current!.Id);   // r lạ ⇒ phiếu đầu tiên
        Assert.Null(q.Error);
    }

    [Fact]
    public void Page_ask_and_errors()
    {
        var p = new ResearchModel(Service(), new Requests());
        // H8-01 (R-012): thành công ⇒ Post-Redirect-Get để F5 không gửi lại câu hỏi.
        var ok = Assert.IsType<RedirectToPageResult>(p.OnPostAsk("R-005", "Hỏi?", new[] { "chatgpt", "manual" }, false));
        Assert.Equal("R-005", ok.RouteValues!["r"]);
        Assert.Equal("Q20261025-030000", ok.RouteValues!["q"]);
        Assert.Equal("Q20261025-030000", p.Asked!.QuestionId);

        // Lỗi ⇒ vẫn trả trang để hiện thông báo, không redirect.
        var bad = new ResearchModel(Service(), new Requests());
        Assert.IsType<PageResult>(bad.OnPostAsk("R-005", "Hỏi?", null, false));
        Assert.Equal("Chọn ít nhất 1 AI.", bad.Error);
        Assert.Null(bad.Asked);
    }

    [Fact]
    public void Page_save_manual()
    {
        var p = new ResearchModel(Service(), new Requests());
        p.OnPostSaveManual("R-005", "Q20261025-030000", "manual", "Hỏi?", "Trả lời", null);
        Assert.Equal("Đã lưu câu trả lời Thủ công (AI bất kỳ).", p.Notice);
        Assert.Single(p.Entries);
        var bad = new ResearchModel(Service(), new Requests());
        bad.OnPostSaveManual("R-005", "Q20261025-030000", "manual", "Hỏi?", "", null);
        Assert.Equal("Cần nội dung câu trả lời.", bad.Error);
    }

    [Fact]
    public void Results_partial()
    {
        var p = new ResearchModel(Service(), new Requests());
        var r = Assert.IsType<PartialViewResult>(p.OnGetResults("R-005"));
        Assert.Equal("_ResearchResults", r.ViewName);
        Assert.Same(p, r.Model);
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
        // H7-05 (R-012): form tách vào partial _ResearchPanel dùng chung cho /Research và Văn phòng.
        var shell = Source(Path.Combine("hub", "Pages", "Research.cshtml"));
        Assert.Contains("@page \"/Research\"", shell);
        Assert.Contains("<partial name=\"_ResearchPanel\"", shell);
        var panel = Source(Path.Combine("hub", "Pages", "Shared", "_ResearchPanel.cshtml"));
        var page = shell + panel;
        Assert.Contains("asp-page-handler=\"@askHandler\"", panel);
        Assert.Contains("asp-page-handler=\"@saveHandler\"", panel);
        Assert.Contains("\"ResearchAsk\" : \"Ask\"", panel);
        Assert.Contains("\"ResearchSaveManual\" : \"SaveManual\"", panel);
        Assert.Contains("maxlength=\"2000\"", page);
        Assert.Contains("name=\"includeRequestText\"", page);
        Assert.Contains("navigator.clipboard", page);
        Assert.Contains("_ResearchResults", page);
        var results = Source(Path.Combine("hub", "Pages", "Shared", "_ResearchResults.cshtml"));
        Assert.Contains("every 5s", results);
        Assert.Contains("white-space:pre-wrap", results.Replace(" ", ""));
        Assert.Contains("KHÔNG phải lệnh", results);
        foreach (var src in new[] { page, results }) Assert.DoesNotContain("Html.Raw", src);
    }

    // ───────────── Dựng thật ─────────────

    private async Task WithHub(Func<HttpClient, Task> act)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(ResearchModel).Assembly.GetName().Name });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddSingleton<IResearchService>(Service());
        builder.Services.AddRazorPages().AddApplicationPart(typeof(ResearchModel).Assembly);
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
        var html = await c.GetStringAsync("/Research");
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("ChatGPT", html);
        var r = await c.PostAsync("/Research?handler=Ask", new FormUrlEncodedContent(new Dictionary<string, string> { ["r"] = "R-005", ["question"] = "x", ["ai"] = "chatgpt" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Empty(_pending);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Research?handler=Results&r=R-005")).StatusCode);
    });
}
