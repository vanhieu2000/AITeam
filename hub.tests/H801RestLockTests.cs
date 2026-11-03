using System.Net;
using System.Text.RegularExpressions;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Core.Research;
using AITeam.Core.Routing;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// H8-01 (R-012) phần 2: khóa AI đang nghỉ trong Phòng nghiên cứu.
public sealed class H801RestLockTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0);
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private sealed class Requests : IRequestService
    {
        private static readonly RequestThread R001 = new("R-001", "Phiếu nghiên cứu", "🟡 Đang làm", "10:00", "hỏi AI", [], [], false, "");
        public IReadOnlyList<RequestThread> GetAll() => [R001];
        public RequestThread? Get(string id) => GetAll().FirstOrDefault(r => r.Id == id);
    }

    private sealed class Accounts : IAiAccountsService
    {
        public IReadOnlyList<AiAccountView> List() => AiCatalog.All.Select(s =>
        {
            var mode = s.Id == "chatgpt" ? AiMode.Automatic : s.DefaultMode;
            var status = mode == AiMode.Automatic ? AiStatus.Connected : AiStatus.ManualOnly;
            return new AiAccountView(s, "", mode, status, mode.ToString(), "", null);
        }).ToList();
        public Task<AiAccountView> CheckAsync(string? id, CancellationToken ct) => throw new NotSupportedException();
        public AiAccountView Save(string? id, string? alias, string? mode) => throw new NotSupportedException();
    }

    private sealed class Conn : IResearchConnector
    {
        public int Calls { get; private set; }
        public string Ai => "chatgpt";
        public string Via => "codex CLI (tài khoản ChatGPT)";
        public Task<ResearchAnswer> AskAsync(string prompt, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new ResearchAnswer("chatgpt", true, "Trả lời", "", RunStatus.Succeeded, TimeSpan.FromSeconds(1)));
        }
    }

    private ResearchService Service(DateTime now, Conn conn) => new(
        _env.Paths, new Requests(), new Accounts(),
        ai => ai == "chatgpt" ? conn : null,
        () => now,
        f => f().GetAwaiter().GetResult());

    private Task<HubTestHost> Host(DateTime now, Conn conn) => HubTestHost.StartAsync(_env, s =>
    {
        s.AddSingleton<IRequestService>(new Requests());
        s.AddSingleton<IAiAccountsService>(new Accounts());
        s.AddSingleton<IResearchService>(Service(now, conn));
    });

    private void RestUntil(DateTime until) => new ProviderStateStore(_env.AITeamRoot)
        .RecordQuota("codex", new QuotaObservation(100m, 60, until, "provider", null), Now);

    private static string AntiforgeryToken(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    [Fact]
    public async Task Resting_provider_is_disabled_and_shows_reason_and_resume_time()
    {
        var until = Now.AddHours(1);
        RestUntil(until);
        var conn = new Conn();
        await using var host = await Host(Now, conn);

        var html = await host.Client.GetStringAsync("/Research?r=R-001");
        var text = WebUtility.HtmlDecode(html);

        Assert.Matches("value=\"chatgpt\"[^>]*disabled", html);
        Assert.Contains("hết hạn mức · hồi lúc 11:00 03/11", text);

        var manualTag = Regex.Match(html, "<input[^>]*value=\"manual\"[^>]*>").Value;
        Assert.NotEmpty(manualTag);
        Assert.DoesNotContain("disabled", manualTag);
        Assert.Equal(0, conn.Calls);
    }

    [Fact]
    public async Task Post_to_resting_ai_does_not_call_it_and_returns_page_with_error()
    {
        RestUntil(Now.AddHours(1));
        var conn = new Conn();
        await using var host = await Host(Now, conn);

        var html = await host.Client.GetStringAsync("/Research?r=R-001");
        host.Client.DefaultRequestHeaders.Add("RequestVerificationToken", AntiforgeryToken(html));
        var response = await host.Client.PostAsync("/Research?handler=Ask", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["r"] = "R-001",
            ["question"] = "Hỏi khi đang nghỉ",
            ["ai"] = "chatgpt",
        }));
        var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ChatGPT đang nghỉ đến 11:00 03/11", body);
        Assert.Equal(0, conn.Calls);
        Assert.Empty(host.Services.GetRequiredService<IResearchService>().Jobs("R-001"));
    }

    [Fact]
    public void Expired_rest_opens_ai_again()
    {
        var until = Now.AddHours(1);
        RestUntil(until);

        var option = Service(until.AddMinutes(1), new Conn()).Options().Single(o => o.Ai == "chatgpt");

        Assert.True(option.CanAsk);
        Assert.False(option.Resting);
        Assert.Null(option.RestUntil);
        Assert.False(option.QuotaUnknown);
    }

    [Fact]
    public async Task Broken_state_does_not_lock_or_crash_the_page()
    {
        var dir = Path.Combine(_env.AITeamRoot, ".state");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "providers.json"), "{ file hỏng");
        var conn = new Conn();
        await using var host = await Host(Now, conn);

        var response = await host.Client.GetAsync("/Research?r=R-001");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("chưa rõ hạn mức", html);
        var chatGptTag = Regex.Match(html, "<input[^>]*value=\"chatgpt\"[^>]*>").Value;
        Assert.NotEmpty(chatGptTag);
        Assert.DoesNotContain("disabled", chatGptTag);
        Assert.Equal(0, conn.Calls);
    }
}
