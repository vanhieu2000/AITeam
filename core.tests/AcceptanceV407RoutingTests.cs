using System.Text;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V4-07 (Claude viết trước — agent KHÔNG sửa). Định tuyến provider, cấm tự duyệt, "nghỉ" khi hết hạn mức.
public sealed class AcceptanceV407RoutingTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 23, 23, 40, 0);

    // Câu thật Codex trả ngày 23/10/2026 khi tài khoản ChatGPT hết hạn mức.
    private const string CodexUsageLimit = "You’ve hit your usage limit. Upgrade to Pro (https://chatgpt.com/explore/pro), visit https://chatgpt.com/settings/usage to purchase more credits or try again at Oct 24th, 2026 4:19 AM.";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v407-" + Guid.NewGuid().ToString("N"));

    public AcceptanceV407RoutingTests() => Directory.CreateDirectory(_root);
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    // ───────────── QuotaResetParser ─────────────

    [Fact]
    public void Real_codex_usage_limit_gives_reset_time()
        => Assert.Equal(new DateTime(2026, 10, 24, 4, 19, 0), QuotaResetParser.Parse(CodexUsageLimit, Now));

    [Theory]
    [InlineData("try again at October 25, 2026 1:05 PM", 2026, 10, 25, 13, 5)]
    [InlineData("Try again at Oct 24 2026 12:00 AM", 2026, 10, 24, 0, 0)]
    [InlineData("try again at Nov 1st, 2026 12:30 PM", 2026, 11, 1, 12, 30)]
    public void Absolute_dates(string text, int y, int mo, int d, int h, int mi)
        => Assert.Equal(new DateTime(y, mo, d, h, mi, 0), QuotaResetParser.Parse(text, Now));

    [Fact]
    public void Time_without_date_is_today_or_tomorrow()
    {
        Assert.Equal(new DateTime(2026, 10, 23, 23, 55, 0), QuotaResetParser.Parse("try again at 11:55 PM", Now));
        Assert.Equal(new DateTime(2026, 10, 24, 4, 19, 0), QuotaResetParser.Parse("please try again at 4:19 AM.", Now));
    }

    [Theory]
    [InlineData("Rate limited. Try again in 2 hours 5 minutes.", 125 * 60)]
    [InlineData("try again in 37 minutes", 37 * 60)]
    [InlineData("quota resets in 1h 30m", 90 * 60)]
    [InlineData("retry in 45 seconds", 45)]
    [InlineData("try again in 1 hour and 10 mins", 70 * 60)]
    [InlineData("HTTP 429, Retry-After: 120", 120)]
    [InlineData("please retry after 30 seconds", 30)]
    public void Relative_spans(string text, int seconds)
        => Assert.Equal(Now.AddSeconds(seconds), QuotaResetParser.Parse(text, Now));

    [Theory]
    [InlineData("Your AI credits balance is too low to continue.")]   // agy thật: không có giờ
    [InlineData("")]
    [InlineData(null)]
    [InlineData("try again at Oct 20th, 2026 4:19 AM")]               // đã qua ⇒ vô nghĩa
    [InlineData("thinking_tokens: 42953")]
    public void No_usable_time_gives_null(string? text) => Assert.Null(QuotaResetParser.Parse(text, Now));

    [Fact]
    public void First_pattern_in_text_wins()
        => Assert.Equal(Now.AddMinutes(10), QuotaResetParser.Parse("try again in 10 minutes (or try again at Oct 25th, 2026 4:19 AM)", Now));

    // ───────────── ProviderStateStore ─────────────

    private static AgentRunResult Run(string id, RunStatus s, string detail = "", string text = "")
        => new(id, s, text, TokenUsage.Unknown, TimeSpan.FromSeconds(3), null, null, detail);

    [Fact]
    public void Quota_marks_rest_until_reset_then_success_clears()
    {
        var store = new ProviderStateStore(_root);
        Assert.Empty(store.All());   // chưa có file

        store.Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        var rest = store.RestOf("CODEX", Now);
        Assert.NotNull(rest);
        Assert.Equal((new DateTime(2026, 10, 24, 4, 19, 0), RunStatus.QuotaExhausted, Now), (rest!.Until, rest.Reason, rest.At));
        Assert.True(rest.Detail.Length <= ProviderStateStore.MaxDetail);
        Assert.Null(store.RestOf("codex", new DateTime(2026, 10, 24, 4, 19, 0)));   // tới giờ ⇒ hết nghỉ

        // Bản mới đọc lại từ đĩa
        Assert.NotNull(new ProviderStateStore(_root).RestOf("codex", Now));
        var bytes = File.ReadAllBytes(Path.Combine(_root, ".state", "providers.json"));
        Assert.False(bytes[0] == 0xEF, "không được có BOM");
        Assert.Contains("QuotaExhausted", Encoding.UTF8.GetString(bytes));

        store.Record(Run("codex", RunStatus.Succeeded), Now.AddHours(6));
        Assert.Null(store.RestOf("codex", Now));
        Assert.Empty(store.All());
    }

    [Fact]
    public void Quota_without_time_rests_default_and_login_rests_a_day()
    {
        var store = new ProviderStateStore(_root);
        store.Record(Run("agy", RunStatus.QuotaExhausted, "Your AI credits balance is too low to continue."), Now);
        Assert.Equal(Now + ProviderStateStore.DefaultQuotaRest, store.RestOf("agy", Now)!.Until);

        store.Record(Run("codex", RunStatus.NotLoggedIn, "Please log in"), Now);
        Assert.Equal(Now + ProviderStateStore.LoginRest, store.RestOf("codex", Now)!.Until);
        Assert.Equal(2, store.All().Count);

        // Ghi đè, không nhân đôi
        store.Record(Run("agy", RunStatus.QuotaExhausted, "try again in 5 minutes"), Now);
        Assert.Equal(2, store.All().Count);
        Assert.Equal(Now.AddMinutes(5), store.RestOf("agy", Now)!.Until);
    }

    [Theory]
    [InlineData(RunStatus.Failed)]
    [InlineData(RunStatus.TimedOut)]
    [InlineData(RunStatus.ConfigError)]
    [InlineData(RunStatus.Unavailable)]
    public void Other_failures_do_not_change_state(RunStatus s)
    {
        var store = new ProviderStateStore(_root);
        store.Record(Run("codex", s, "try again in 5 minutes"), Now);
        Assert.Empty(store.All());
        Assert.False(File.Exists(store.FilePath));
    }

    [Fact]
    public void Broken_state_file_is_treated_as_empty_and_repaired()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".state"));
        File.WriteAllText(Path.Combine(_root, ".state", "providers.json"), "{ hỏng");
        var store = new ProviderStateStore(_root);
        Assert.Empty(store.All());
        Assert.Null(store.RestOf("codex", Now));
        store.Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        Assert.Single(new ProviderStateStore(_root).All());
        Assert.DoesNotContain(Directory.GetFiles(Path.Combine(_root, ".state")), f => !f.EndsWith("providers.json"));
    }

    // ───────────── ProviderRouter ─────────────

    private static ProviderConfig P(string id, ProviderKind k, bool enabled = true, params AgentRole[] roles)
        => new() { Id = id, Kind = k, Exe = id + ".exe", Enabled = enabled, Roles = roles };

    private ProviderRouter Router(params (AgentRole Role, string[] Ids)[] routing)
    {
        var providers = new[]
        {
            P("agy", ProviderKind.Antigravity, true, AgentRole.Developer),
            P("codex", ProviderKind.Codex, true, AgentRole.Reviewer, AgentRole.Developer),
            P("claude", ProviderKind.ClaudeCli, true, AgentRole.Reviewer),
            P("off", ProviderKind.Codex, false, AgentRole.Reviewer),
        };
        var map = routing.ToDictionary(r => r.Role, r => (IReadOnlyList<string>)r.Ids);
        return new ProviderRouter(new ProviderSettings(providers, map), new ProviderStateStore(_root));
    }

    [Fact]
    public void Developer_agy_gets_codex_reviewer()
    {
        var d = Router((AgentRole.Reviewer, new[] { "codex", "claude" })).Pick(AgentRole.Reviewer, "agy", Now);
        Assert.True(d.Ok);
        Assert.Equal("codex", d.ProviderId);
        Assert.Empty(d.Skipped);
    }

    [Fact]
    public void Self_review_is_banned_and_next_reviewer_chosen()
    {
        var d = Router((AgentRole.Reviewer, new[] { "codex", "claude" })).Pick(AgentRole.Reviewer, "CODEX", Now);
        Assert.Equal("claude", d.ProviderId);
        Assert.Equal(new[] { "codex: là developer của task — cấm tự duyệt" }, d.Skipped);
    }

    [Fact]
    public void Resting_provider_is_skipped_with_reason()
    {
        new ProviderStateStore(_root).Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        var d = Router((AgentRole.Reviewer, new[] { "codex", "claude" })).Pick(AgentRole.Reviewer, "agy", Now);
        Assert.Equal("claude", d.ProviderId);
        Assert.Equal(new[] { "codex: nghỉ tới 04:19 24/10 (QuotaExhausted)" }, d.Skipped);

        var later = Router((AgentRole.Reviewer, new[] { "codex", "claude" })).Pick(AgentRole.Reviewer, "agy", new DateTime(2026, 10, 24, 5, 0, 0));
        Assert.Equal("codex", later.ProviderId);   // hết giờ nghỉ ⇒ dùng lại
    }

    [Fact]
    public void No_reviewer_available_gives_all_reasons()
    {
        new ProviderStateStore(_root).Record(Run("codex", RunStatus.QuotaExhausted, CodexUsageLimit), Now);
        var d = Router((AgentRole.Reviewer, new[] { "off", "agy", "codex", "ghost" })).Pick(AgentRole.Reviewer, "agy", Now);
        Assert.False(d.Ok);
        Assert.Null(d.ProviderId);
        Assert.Equal(new[]
        {
            "off: đang tắt",
            "agy: không có vai trò Reviewer",
            "codex: nghỉ tới 04:19 24/10 (QuotaExhausted)",
            "ghost: không có cấu hình",
        }, d.Skipped);
    }

    [Fact]
    public void Developer_fallback_and_exclusion_wording()
    {
        var r = Router((AgentRole.Developer, new[] { "agy", "codex" }));
        Assert.Equal("agy", r.Pick(AgentRole.Developer, null, Now).ProviderId);
        var d = r.Pick(AgentRole.Developer, "agy", Now);
        Assert.Equal("codex", d.ProviderId);
        Assert.Equal(new[] { "agy: bị loại trừ" }, d.Skipped);

        var none = r.Pick(AgentRole.Planner, null, Now);
        Assert.False(none.Ok);
        Assert.Equal(new[] { "không có provider nào cho vai trò Planner" }, none.Skipped);
    }

    // ───────────── ReviewPolicy ─────────────

    [Theory]
    [InlineData("- **Độ khó:** L", true, "Độ khó L")]
    [InlineData("- **Độ khó:** l (lớn)", true, "Độ khó L")]
    [InlineData("- **Độ khó:** M", false, "Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)")]
    [InlineData("không có dòng nào", false, "Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)")]
    [InlineData("- **Độ khó:** S\n- **Review AI:** có", true, "Task yêu cầu Review AI")]
    [InlineData("- **Review AI:** Co", true, "Task yêu cầu Review AI")]
    [InlineData("- **Độ khó:** L\r\n- **Review AI:** không", false, "Task tắt Review AI")]
    [InlineData("- **Review AI:** khong", false, "Task tắt Review AI")]
    [InlineData("- **Độ khó:** L\n- **Review AI:** tùy", true, "Độ khó L")]
    public void Review_policy(string text, bool auto, string reason)
        => Assert.Equal(new ReviewDecision(auto, reason), ReviewPolicy.Decide(text));
}
