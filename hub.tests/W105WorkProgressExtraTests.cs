using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class W105WorkProgressExtraTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 22, 0, 0, TimeSpan.FromHours(7));
    private static WorkProgressEvent Event(WorkAction action, WorkOutcome outcome = WorkOutcome.Passed,
        int minute = 0, int round = 1) => new(At.AddMinutes(minute), "W1-05", round,
            action, outcome, null, $"{action}: {outcome} ({minute})");

    [Theory]
    [InlineData(false, WorkStep.Verify)]
    [InlineData(true, WorkStep.Acceptance)]
    public void Qc_success_survives_verify_failure_and_later_verify_success(bool verifyPassed, WorkStep expected)
    {
        var verifyFailure = Event(WorkAction.Verify, WorkOutcome.Failed, minute: 2);
        var events = new List<WorkProgressEvent>
        {
            Event(WorkAction.Prep), Event(WorkAction.Assign),
            Event(WorkAction.Qc, WorkOutcome.Failed, minute: 1), verifyFailure,
            Event(WorkAction.Qc, minute: 3)
        };
        if (verifyPassed) events.Add(Event(WorkAction.Verify, minute: 4));

        var result = WorkProgressParser.Calculate("W1-05", events, planApproved: true);

        Assert.Equal(expected, result.CurrentStep);
        Assert.Equal(verifyPassed ? null : verifyFailure.Text, result.LatestError);
        Assert.Equal(WorkStepState.Done, result.Steps.Single(s => s.Step == WorkStep.Qc).State);
        Assert.Equal(verifyPassed ? WorkStepState.Done : WorkStepState.Failed,
            result.Steps.Single(s => s.Step == WorkStep.Verify).State);
    }

    [Theory]
    [InlineData(1, 2, WorkStepState.Done)]
    [InlineData(0, 10, WorkStepState.Failed)]
    public void Latest_result_is_selected_per_step_by_time_then_numeric_round(
        int passedMinute, int failedRound, WorkStepState expected)
    {
        var failed = Event(WorkAction.Qc, WorkOutcome.Failed, round: failedRound);
        var passed = Event(WorkAction.Qc, minute: passedMinute, round: 2);
        var result = WorkProgressParser.Calculate("w1-05", new[]
        {
            passed, failed, Event(WorkAction.Prep, round: 99)
        });

        Assert.Equal(expected, result.Steps.Single(s => s.Step == WorkStep.Qc).State);
        Assert.Equal(WorkStepState.Done, result.Steps.Single(s => s.Step == WorkStep.Prep).State);
        Assert.Equal(WorkStep.Received, result.CurrentStep);
        Assert.Null(result.LatestError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Merge_evidence_completes_all_steps_and_clears_errors(bool board)
    {
        var events = new List<WorkProgressEvent> { Event(WorkAction.Qc, WorkOutcome.Failed) };
        if (!board) events.Add(Event(WorkAction.Merge, minute: 1));
        var result = WorkProgressParser.Calculate("W1-05", events, boardStatus: board ? "✅" : null);

        Assert.True(result.IsMerged);
        Assert.Equal(WorkStep.Merge, result.CurrentStep);
        Assert.Null(result.LatestError);
        Assert.Equal(9, result.Steps.Count);
        Assert.All(result.Steps, step => Assert.Equal(WorkStepState.Done, step.State));
    }

    [Fact]
    public void Xong_is_a_passed_workflow_result()
    {
        var parsed = WorkProgressParser.Parse("## Nhật ký\n- 22:00 | W1-05 | Assign | lượt 1 | XONG");
        Assert.Equal(WorkOutcome.Passed, Assert.Single(parsed).Outcome);
        var result = WorkProgressParser.Calculate("W1-05", parsed);
        Assert.Equal(WorkStepState.Done, result.Steps.Single(s => s.Step == WorkStep.Code).State);
    }
}
