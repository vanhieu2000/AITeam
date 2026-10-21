using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>
/// Test nghiệm thu H-006 (Claude viết trước khi giao — agent KHÔNG sửa file này).
/// Hợp đồng: tasks/H-006-hub-v2-shell-home.md mục "Mục tiêu" 3.
/// Mẫu lỗi lấy từ log thật 21/10/2026 (C-011-1 … T-006-4).
/// </summary>
public sealed class AcceptanceH006Tests
{
    private static readonly DateTime T0 = new(2026, 10, 21, 12, 0, 0);

    private static AgentRun Run(string task, int attempt, RunState state, string? status, string? error = null,
        string? response = null, int minutes = 0, string project = "ChinaAndroidToolbox") =>
        new(project, task, attempt, state, status, error, response, 60, 1000, T0.AddMinutes(minutes), $"logs/{task}-{attempt}.json");

    // ───── RunVerdicts ─────

    [Fact]
    public void Classify_running_and_success()
    {
        Assert.Equal(RunVerdict.Running, RunVerdicts.Classify(Run("C-011", 5, RunState.Running, null)));
        Assert.Equal(RunVerdict.Success, RunVerdicts.Classify(Run("C-010", 4, RunState.Success, "SUCCESS")));
    }

    [Theory]
    [InlineData("Your AI credits balance is too low to continue.")]                       // log thật 21/10
    [InlineData("RESOURCE_EXHAUSTED (code 429): Resource has been exhausted (e.g. check quota).")]
    [InlineData("rate limit exceeded")]
    [InlineData("HTTP 429 Too Many Requests")]
    [InlineData("You are out of credits")]
    public void Classify_credit_and_quota_errors_as_CreditsExhausted(string error)
        => Assert.Equal(RunVerdict.CreditsExhausted, RunVerdicts.Classify(Run("C-013", 1, RunState.Error, "ERROR", error)));

    [Fact]
    public void Classify_success_mentioning_quota_or_429_digits_stays_Success()
    {
        // Log thành công có "thinking_tokens":42953 (chứa 429) và câu trả lời nhắc chữ quota — không được coi là hết credits.
        var run = Run("C-010", 1, RunState.Success, "SUCCESS", null, "Đã kiểm tra quota; thinking_tokens 42953");
        Assert.Equal(RunVerdict.Success, RunVerdicts.Classify(run));
    }

    [Fact]
    public void Classify_other_errors_and_number_containing_429_as_Error()
    {
        Assert.Equal(RunVerdict.Error, RunVerdicts.Classify(Run("T-001", 1, RunState.Error, "ERROR", "build failed after 14290 ms")));
        Assert.Equal(RunVerdict.Error, RunVerdicts.Classify(Run("T-001", 2, RunState.Error, null, null)));
    }

    [Fact]
    public void Label_is_vietnamese()
    {
        Assert.Equal("Đang chạy", RunVerdicts.Label(RunVerdict.Running));
        Assert.Equal("Thành công", RunVerdicts.Label(RunVerdict.Success));
        Assert.Equal("Hết credits", RunVerdicts.Label(RunVerdict.CreditsExhausted));
        Assert.Equal("Lỗi", RunVerdicts.Label(RunVerdict.Error));
    }

    // ───── AntigravityStatuses ─────

    [Fact]
    public void Status_uses_latest_finished_run_and_ignores_running_one()
    {
        var runs = new[]
        {
            Run("T-006", 4, RunState.Error, "ERROR", "Your AI credits balance is too low to continue.", minutes: 30, project: "HDVTool"),
            Run("C-011", 5, RunState.Running, null, minutes: 40),            // đang chạy, mới hơn ⇒ bỏ qua
            Run("C-010", 4, RunState.Success, "SUCCESS", minutes: 10),
        };
        var s = AntigravityStatuses.From(runs);
        Assert.True(s.CreditsExhausted);
        Assert.Equal(T0.AddMinutes(30), s.At);
        Assert.Equal("HDVTool", s.Project);
        Assert.Equal("T-006", s.Task);
    }

    [Fact]
    public void Status_ok_when_latest_finished_run_succeeded_after_an_exhausted_one()
    {
        var runs = new[]
        {
            Run("C-013", 1, RunState.Error, "ERROR", "Your AI credits balance is too low to continue.", minutes: 5),
            Run("C-013", 2, RunState.Success, "SUCCESS", minutes: 50),
        };
        Assert.Equal(new AntigravityStatus(false, null, null, null), AntigravityStatuses.From(runs));
    }

    [Fact]
    public void Status_ok_when_no_runs()
        => Assert.Equal(new AntigravityStatus(false, null, null, null), AntigravityStatuses.From(Array.Empty<AgentRun>()));

    // ───── ActivityFeed ─────

    [Fact]
    public void Feed_newest_first_skips_running_and_limits_count()
    {
        var runs = Enumerable.Range(1, 9)
            .Select(i => Run("C-0" + (10 + i), 1, i % 3 == 0 ? RunState.Error : RunState.Success, i % 3 == 0 ? "ERROR" : "SUCCESS",
                i % 3 == 0 ? "Your AI credits balance is too low to continue." : null, minutes: i))
            .Append(Run("C-099", 1, RunState.Running, null, minutes: 100))
            .ToList();

        var feed = ActivityFeed.Build(runs);

        Assert.Equal(6, feed.Count);
        Assert.DoesNotContain(feed, f => f.Task == "C-099");
        Assert.Equal(T0.AddMinutes(9), feed[0].Time);
        Assert.True(feed.Zip(feed.Skip(1)).All(p => p.First.Time >= p.Second.Time));
        Assert.Equal(RunVerdict.CreditsExhausted, feed[0].Verdict);        // i = 9
        Assert.Equal(RunVerdict.Success, feed[1].Verdict);                 // i = 8
        Assert.Equal("ChinaAndroidToolbox", feed[0].Project);
    }

    [Fact]
    public void Feed_take_parameter_and_empty_input()
    {
        var runs = new[] { Run("A", 1, RunState.Success, "SUCCESS", minutes: 1), Run("B", 1, RunState.Success, "SUCCESS", minutes: 2) };
        Assert.Single(ActivityFeed.Build(runs, take: 1));
        Assert.Empty(ActivityFeed.Build(Array.Empty<AgentRun>()));
    }
}
