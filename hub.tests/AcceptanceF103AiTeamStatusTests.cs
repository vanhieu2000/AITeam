using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Providers;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Test nghiệm thu F1-03 (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-03.
/// Trang Tài khoản AI hiện "Đăng nhập chưa kiểm · Hạn mức chưa đo" ở mọi dòng (kết quả Kiểm tra không được lưu)
/// và Antigravity 2 lần (agy bật + antigravity tắt trong office.db thật 04/11/2026).</summary>
public sealed class AcceptanceF103AiTeamStatusTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 21, 0, 0, TimeSpan.Zero);
    private readonly SqliteAiServiceRegistry _registry = new(":memory:", Path.GetTempPath());
    private readonly FakeFactory _factory = new();

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeProvider(string id, Func<ProviderHealth> health) : IAgentProvider
    {
        public string Id => id;
        public ProviderKind Kind => default;
        public bool HasReadOnlySandbox => false;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(health());
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeFactory : IAiProviderFactory
    {
        public Dictionary<string, Func<ProviderHealth>> Health { get; } = new();
        public List<string> Created { get; } = new();
        public IAgentProvider Create(AiService service)
        {
            Created.Add(service.Id);
            return new FakeProvider(service.Id, Health[service.Id]);
        }
        public IReadOnlyList<AiService> ForRole(AiRole role) => [];
    }

    public AcceptanceF103AiTeamStatusTests()
    {
        // Danh bạ THẬT (office.db 04/11/2026): agy bật, codex bật, antigravity / gemini / grok tắt.
        Add("agy", "Antigravity", AiAdapter.CliJson, "C:\\Tools\\agy\\bin\\agy.exe", true, [AiRole.Code, AiRole.Research]);
        Add("codex", "Codex", AiAdapter.CliJson, "codex", true, [AiRole.Code, AiRole.Qc]);
        Add("antigravity", "Antigravity", AiAdapter.CliText, "agy", false, [AiRole.Chat]);
        Add("gemini", "Gemini", AiAdapter.CliText, "agy", false, [AiRole.Chat]);
    }

    public void Dispose() { }

    private void Add(string id, string name, AiAdapter adapter, string command, bool enabled, IReadOnlyList<AiRole> roles)
        => _registry.Upsert(new AiService(id, name, adapter, command, [], null, ["m1"], roles, 1, false, null, null, enabled, Now, Now));

    private AiTeamStatusService Service() => new(_registry, _factory, new FixedTime(Now));

    [Fact]
    public void List_hides_disabled_duplicate_names_and_puts_enabled_first()
    {
        var list = Service().List();
        Assert.Equal(new[] { "agy", "codex", "gemini" }, list.Select(m => m.Id));
        Assert.True(list[0].Enabled);
        Assert.False(list[2].Enabled);
        Assert.Equal("unknown", list[0].LoginState);
        Assert.Null(list[0].CheckedAt);
        Assert.Null(list[0].RemainingPct);
    }

    [Theory]
    [InlineData(true, true, "ok")]
    [InlineData(true, false, "logged-out")]
    [InlineData(true, null, "unknown")]
    [InlineData(false, null, "not-installed")]
    public async Task Check_persists_login_state_and_time(bool installed, bool? loggedIn, string expected)
    {
        _factory.Health["codex"] = () => new ProviderHealth("codex", installed, loggedIn, "chi tiết");

        var s = await Service().CheckAsync("codex", CancellationToken.None);

        Assert.NotNull(s);
        Assert.Equal(expected, s!.LoginState);
        Assert.Equal(Now, s.CheckedAt);
        Assert.Equal("chi tiết", s.Detail);
        // Lưu bền: dịch vụ MỚI (đọc lại từ registry) vẫn thấy kết quả
        var again = Service().List().Single(m => m.Id == "codex");
        Assert.Equal(expected, again.LoginState);
        Assert.Equal(Now, again.CheckedAt);
        var acc = Assert.Single(_registry.Accounts("codex"));
        Assert.Equal("codex:default", acc.Id);
    }

    [Fact]
    public async Task Check_error_is_saved_as_error_with_message()
    {
        _factory.Health["agy"] = () => throw new InvalidOperationException("agy treo");
        var s = await Service().CheckAsync("agy", CancellationToken.None);
        Assert.Equal("error", s!.LoginState);
        Assert.Contains("agy treo", s.Detail);
        Assert.Equal("error", Service().List()[0].LoginState);
    }

    [Fact]
    public async Task Check_unknown_or_disabled_service_does_not_run_provider()
    {
        Assert.Null(await Service().CheckAsync("khong-co", CancellationToken.None));
        Assert.Null(await Service().CheckAsync("../x", CancellationToken.None));
        Assert.Null(await Service().CheckAsync("antigravity", CancellationToken.None));
        Assert.Empty(_factory.Created);
    }

    [Fact]
    public void Latest_quota_is_shown_and_past_rest_time_is_cleared()
    {
        _registry.RecordQuota(new AiQuota("agy", null, 0, Now.AddHours(3), "assign", Now.AddMinutes(-5)));
        _registry.RecordQuota(new AiQuota("codex", null, 0, Now.AddHours(-1), "assign", Now.AddHours(-6)));

        var list = Service().List();
        var agy = list.Single(m => m.Id == "agy");
        Assert.Equal(0, agy.RemainingPct);
        Assert.Equal(Now.AddHours(3), agy.RestUntil);
        Assert.Equal("assign", agy.QuotaSource);

        // Giờ hồi đã qua ⇒ coi như đã hồi: không hiện "hết hạn mức" nữa
        var codex = list.Single(m => m.Id == "codex");
        Assert.Null(codex.RestUntil);
        Assert.Null(codex.RemainingPct);
    }

    [Fact]
    public void Registered_in_di_and_assign_records_quota()
    {
        var root = FindRoot();
        Assert.Contains("AiTeamStatusService", File.ReadAllText(Path.Combine(root, "hub", "Services", "HubV3Registration.cs")));
        Assert.Contains("quota-record", File.ReadAllText(Path.Combine(root, "team.ps1")));
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("AITeam.sln");
    }
}
