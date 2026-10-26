using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class WorkProgressOutcomeRegressionTests
{
    [Fact]
    public void Real_round_seven_verify_failure_keeps_gate_current_and_error_visible()
    {
        // Bản sao dòng thật từ requests/R-011-bo-testqc-chuan-codex-tra-ke-hoachmockup.md.
        const string journal = """
            ## Nhật ký
            - 2026-10-26 11:51:19 +07:00 | W1-07 | Verify | lượt 3 | ĐẠT <!-- workflow:W1-07:Verify:3 -->
            - 2026-10-26 12:50:11 +07:00 | W1-07 | Verify | lượt 7 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:7 -->
            """;

        var events = WorkProgressParser.Parse(journal);
        var progress = WorkProgressParser.Calculate("W1-07", events, true, true);

        Assert.Equal(WorkOutcome.Failed, events[1].Outcome);
        Assert.Equal(WorkStep.Verify, progress.CurrentStep);
        Assert.Equal("KHÔNG ĐẠT", progress.LatestError);
        Assert.Equal(WorkStepState.Failed, progress.Steps.Single(s => s.Step == WorkStep.Verify).State);
        Assert.Equal("12:50 26/10/2026", progress.Steps.Single(s => s.Step == WorkStep.Verify).TimeText);
        Assert.Equal(events[1], progress.RecentEvents[0]);
    }

    [Theory]
    [InlineData("KHÔNG ĐẠT: cần sửa", WorkOutcome.Failed)]
    [InlineData("cần sửa", WorkOutcome.Failed)]
    [InlineData("BLOCKED: chưa đủ dữ liệu", WorkOutcome.Blocked)]
    [InlineData("LỖI: chưa đạt", WorkOutcome.Failed)]
    [InlineData("ĐẠT", WorkOutcome.Passed)]
    [InlineData("PASS: hoàn thành", WorkOutcome.Passed)]
    [InlineData("PASSED", WorkOutcome.Passed)]
    [InlineData("XONG", WorkOutcome.Passed)]
    [InlineData("ĐẠTING", WorkOutcome.Unknown)]
    [InlineData("PASSAGE", WorkOutcome.Unknown)]
    [InlineData("XONGVIỆC", WorkOutcome.Unknown)]
    [InlineData("KHÔNG ĐẠTING", WorkOutcome.Unknown)]
    public void Outcome_uses_whole_prefix_words(string result, WorkOutcome expected)
    {
        var events = WorkProgressParser.Parse($"## Nhật ký\n- 12:50 | W1-07 | Verify | lượt 7 | {result}");

        Assert.Equal(expected, Assert.Single(events).Outcome);
    }
}
