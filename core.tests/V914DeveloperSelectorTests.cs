using AITeam.Core.Routing;
namespace AITeam.Core.Tests;
public sealed class V914DeveloperSelectorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-03T00:00:00Z");
    private static DeveloperTask Task(string difficulty = "S", string? dev = null, bool privacy = false) => new(dev, privacy, difficulty, 100, ["hub/site.css"]);
    private static DeveloperCandidate Codex(bool available = true) => new("codex", available, true, "gpt", 1);
    private static DeveloperCandidate OpenCode() => new("opencode", true, false, "deepseek", 0);
    [Fact] public void Pinned_resting_developer_is_not_replaced()
    {
        var result = DeveloperSelector.Select(Task(dev: "codex"), [Codex(false) with { RestUntil = Now.AddHours(1) }, OpenCode()], Now);
        Assert.Null(result.ProviderId); Assert.Equal(Now.AddHours(1), result.RetryAt); Assert.Contains("Không đổi", result.Reason);
    }
    [Fact] public void Private_repo_filters_free_provider() => Assert.Equal("codex", DeveloperSelector.Select(Task(privacy: true), [Codex(), OpenCode()], Now).ProviderId);
    [Theory] [InlineData("S", "opencode")] [InlineData("M", "opencode")] [InlineData("L", "codex")]
    public void Difficulty_prefers_the_specified_provider(string difficulty, string provider) => Assert.Equal(provider, DeveloperSelector.Select(Task(difficulty), [Codex(), OpenCode()], Now).ProviderId);
    [Fact] public void Powershell_or_big_change_prefers_codex()
    {
        Assert.Equal("codex", DeveloperSelector.Select(Task() with { Scope = ["team.ps1"] }, [Codex(), OpenCode()], Now).ProviderId);
        Assert.Equal("codex", DeveloperSelector.Select(Task() with { PlannedLines = 301 }, [Codex(), OpenCode()], Now).ProviderId);
    }
    [Fact] public void Fallback_balances_load_and_excludes_missing_argon()
    {
        var agy = new DeveloperCandidate("agy", true, true, "argon-4", 0);
        Assert.Equal("agy", DeveloperSelector.Select(Task(), [Codex(), agy], Now).ProviderId);
        Assert.Equal("codex", DeveloperSelector.Select(Task(), [Codex(), agy with { Model = "other" }], Now).ProviderId);
    }
    [Fact] public void No_candidate_or_scope_lock_waits()
    {
        Assert.Equal("Chờ người làm", DeveloperSelector.Select(Task(), [], Now).Status);
        Assert.Null(DeveloperSelector.Select(Task() with { ScopeLocked = true }, [Codex()], Now).ProviderId);
        Assert.Null(DeveloperSelector.Select(Task(), [OpenCode() with { Model = "other" }], Now).ProviderId);
    }
    [Fact] public void Earliest_reset_is_reported_when_all_rest()
    {
        var result = DeveloperSelector.Select(Task(), [Codex(false) with { RestUntil = Now.AddHours(2) }, OpenCode() with { Available = false, RestUntil = Now.AddHours(1) }], Now);
        Assert.Equal(Now.AddHours(1), result.RetryAt);
    }
}
