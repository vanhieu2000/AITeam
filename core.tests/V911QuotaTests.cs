using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class V911QuotaTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0);

    [Fact]
    public void Unknown_does_not_claim_zero_remaining_or_a_reset_time()
    {
        var q = QuotaObservation.Unknown;
        Assert.Equal("unknown", q.Source);
        Assert.Null(q.UsedPercent); Assert.Null(q.ResetsAt);
        Assert.Null(QuotaEstimator.Estimate(12, null, 300, Now.AddHours(1), 4, Now).UsedPercent);
    }

    [Fact]
    public void Estimate_has_explicit_source_and_forecast_from_last_24_hours()
    {
        var q = QuotaEstimator.Estimate(12, 6, 300, Now.AddHours(4), 48, Now);
        Assert.Equal("estimate", q.Source);
        Assert.Equal(50m, q.UsedPercent);
        Assert.Equal(Now.AddHours(3), q.ExhaustsAt);
        Assert.Equal(Now.AddHours(4), q.ResetsAt);
        Assert.Null(QuotaEstimator.Estimate(12, 6, 300, Now.AddHours(3), 0, Now).ExhaustsAt);
        Assert.Null(QuotaEstimator.Estimate(12, 6, 300, Now.AddHours(3), 24, Now).ExhaustsAt);
    }

    [Fact]
    public void Provider_observation_is_persisted_and_exhaustion_rests_until_reset()
    {
        using var repo = new TempRepo(false);
        var store = new ProviderStateStore(repo.Root);
        var reset = Now.AddHours(2);
        store.RecordQuota("codex", new(100, 300, reset, "provider", null), Now);
        var rest = Assert.IsType<ProviderRest>(store.RestOf("codex", Now));
        Assert.Equal(reset, rest.Until);
        Assert.Equal(reset, rest.ResetsAt);
        Assert.Equal("provider", rest.Source);
        Assert.Equal(100m, rest.UsedPercent);
        Assert.Null(store.RestOf("codex", reset));
    }

    [Fact]
    public void Fake_source_or_invalid_percentage_is_not_saved()
    {
        using var repo = new TempRepo(false);
        var store = new ProviderStateStore(repo.Root);
        Assert.Throws<ArgumentException>(() => store.RecordQuota("codex", new(101, 300, Now.AddHours(1), "provider", null), Now));
        Assert.Throws<ArgumentException>(() => store.RecordQuota("codex", new(50, 300, Now.AddHours(1), "invented", null), Now));
        Assert.Empty(store.All());
    }

    [Fact]
    public void Multiple_configured_windows_are_loaded_without_guessing_unknown_provider()
    {
        using var repo = new TempRepo(false);
        repo.Write("team.json", "{\"quotas\":{\"opencode\":[{\"limitUsd\":12,\"windowMinutes\":300},{\"limitUsd\":30,\"windowMinutes\":10080}]}}");
        var windows = QuotaPolicy.Load(repo.Root, "opencode");
        Assert.Equal(2, windows.Count); Assert.Equal(12m, windows[0].LimitUsd);
        Assert.Empty(QuotaPolicy.Load(repo.Root, "codex"));
    }

    [Fact]
    public void Corrupt_state_is_never_overwritten_by_new_quota_observation()
    {
        using var repo = new TempRepo(false);
        var store = new ProviderStateStore(repo.Root);
        repo.Write(".state/providers.json", "broken-json");
        Assert.Throws<System.Text.Json.JsonException>(() => store.RecordQuota("codex", QuotaObservation.Unknown, Now));
        Assert.Equal("broken-json", File.ReadAllText(store.FilePath));
    }
}
