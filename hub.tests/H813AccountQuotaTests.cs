using System.Net;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

/// <summary>Nghiệm thu H8-13: thẻ tài khoản AI có hạn mức, giờ hồi, nguồn số liệu và liên kết https.</summary>
public sealed class H813AccountQuotaTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly DateTime _now = new(2026, 11, 3, 10, 0, 0);

    public void Dispose() => _env.Dispose();

    private sealed class Runs : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => [];
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => [];
        public IReadOnlyList<RunningAgent> GetRunning() =>
            [new RunningAgent(100, new DateTime(2026, 11, 3, 9, 0, 0), "AITeam", "H8-13", "agy.exe --prompt H8-13.prompt.md")];
        public bool Stop(int pid) => false;
    }

    private AiAccountsService Service(Func<string, CancellationToken, Task<ProviderHealth>>? probe = null)
        => new(_env.Paths, new Runs(), new AiAccountStore(Path.Combine(_env.RootDir, "accounts.json")),
            probe, () => _now);

    [Fact]
    public void Cards_cover_four_accounts_and_unknown_is_not_zero()
    {
        var cards = Service().ListCards();

        Assert.Equal(new[] { "codex", "opencode", "antigravity", "claude" }, cards.Select(c => c.Service.Id));
        Assert.Equal("Chỉ DeepSeek", cards.Single(c => c.Service.Id == "opencode").AllowedModels);
        Assert.All(cards, card =>
        {
            Assert.Equal("không rõ", card.QuotaSource);
            Assert.Null(card.UsedPercent);
            Assert.Null(card.RemainingPercent);
            Assert.Null(card.TokensToday);
            Assert.Null(card.TokensLast7Days);
        });
    }

    [Fact]
    public void Quota_source_is_estimate_or_provider_and_low_quota_warns()
    {
        var state = new ProviderStateStore(_env.AITeamRoot);
        state.RecordQuota("opencode", new QuotaObservation(85m, 60, _now.AddHours(2), "estimate", null), _now);
        state.RecordQuota("codex", new QuotaObservation(10m, 60, _now.AddHours(2), "provider", null), _now);

        var cards = Service().ListCards();
        var opencode = cards.Single(c => c.Service.Id == "opencode");
        Assert.Equal("ước tính", opencode.QuotaSource);
        Assert.Equal(15m, opencode.RemainingPercent!.Value);
        Assert.True(opencode.WarnLow);

        var codex = cards.Single(c => c.Service.Id == "codex");
        Assert.Equal("provider", codex.QuotaSource);
        Assert.Equal(90m, codex.RemainingPercent!.Value);
        Assert.False(codex.WarnLow);
    }

    [Fact]
    public void Resting_account_shows_reason_and_reset_time()
    {
        new ProviderStateStore(_env.AITeamRoot).Record(
            new AgentRunResult("codex", RunStatus.QuotaExhausted, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "try again at 4:19 PM"), _now);

        var codex = Service().ListCards().Single(c => c.Service.Id == "codex");
        Assert.True(codex.Resting);
        Assert.Contains("nghỉ", codex.RestText, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(codex.ResetsAt);
    }

    [Fact]
    public void Links_skip_javascript_and_http()
    {
        File.WriteAllText(_env.Paths.TeamJson, """
            {
              "accounts": [
                {
                  "id": "codex",
                  "links": [
                    { "label": "Hạn mức", "url": "javascript:alert(1)" },
                    { "label": "Thanh toán", "url": "http://example.com/billing" },
                    { "label": "Tài liệu", "url": "https://example.com/docs" }
                  ]
                }
              ]
            }
            """);

        var codex = Service().ListCards().Single(c => c.Service.Id == "codex");
        var link = Assert.Single(codex.Links);
        Assert.Equal("https://example.com/docs", link.Url);
        Assert.Equal("Tài liệu", link.Label);
    }

    [Fact]
    public void Tokens_are_null_when_not_measured_and_sum_when_metrics_exist()
    {
        var before = Service().ListCards().Single(c => c.Service.Id == "codex");
        Assert.Null(before.TokensToday);
        Assert.Null(before.TokensLast7Days);

        var runs = Path.Combine(_env.AITeamRoot, "metrics", "runs");
        Directory.CreateDirectory(runs);
        File.WriteAllText(Path.Combine(runs, "a.json"),
            """{"project":"AITeam","task":"H8-13","run":1,"at":"2026-11-03T09:00:00+07:00","provider":"codex","totalTokens":1200,"tokensKnown":true}""");
        File.WriteAllText(Path.Combine(runs, "b.json"),
            """{"project":"AITeam","task":"H8-13","run":2,"at":"2026-10-29T09:00:00+07:00","provider":"codex","totalTokens":800,"tokensKnown":true}""");

        var cards = Service().ListCards();
        var codex = cards.Single(c => c.Service.Id == "codex");
        Assert.Equal(1200L, codex.TokensToday!.Value);
        Assert.Equal(2000L, codex.TokensLast7Days!.Value);
        Assert.Null(cards.Single(c => c.Service.Id == "opencode").TokensToday);
    }

    [Fact]
    public Task Page_returns_200_with_four_cards_and_hides_secret() => RenderPage(async html =>
    {
        Assert.Contains("Tài khoản AI và hạn mức", html);
        Assert.Contains("card-codex", html);
        Assert.Contains("card-opencode", html);
        Assert.Contains("card-antigravity", html);
        Assert.Contains("card-claude", html);
        Assert.Contains("Nguồn: không rõ", html);
        Assert.Contains("chưa đo", html);
        Assert.Contains("dưới 20%", html);
    });

    private async Task RenderPage(Func<string, Task> assertions)
    {
        var secret = string.Concat("sk-", new string('a', 40));
        new ProviderStateStore(_env.AITeamRoot).RecordQuota(
            "opencode", new QuotaObservation(85m, 60, _now.AddHours(2), "estimate", null), _now);
        var service = Service((id, ct) => Task.FromResult(new ProviderHealth(id, true, true, "phiên bản 1.2.3 " + secret)));
        await service.CheckAsync("codex", CancellationToken.None);

        await HubTestHost.WithAsync(_env, async host =>
        {
            var response = await host.Client.GetAsync("/AiAccounts");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            await assertions(html);
            Assert.DoesNotContain(secret, html);
            Assert.DoesNotContain("sk-", html);
        }, services => services.AddSingleton<IAiAccountsService>(service));
    }
}
