using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class W105RuleRegressionTests
{
    [Theory]
    [InlineData(false, WorkStep.Verify)]
    [InlineData(true, WorkStep.Acceptance)]
    public void Current_step_is_first_unresolved_step_after_qc_recovers(bool verifyRecovers, WorkStep expected)
    {
        var at = new DateTimeOffset(2026, 10, 25, 22, 0, 0, TimeSpan.FromHours(7));
        var events = new List<WorkProgressEvent>
        {
            new(at, "W1-05", 2, WorkAction.Prep, WorkOutcome.Passed, null, "ĐẠT chuẩn bị"),
            new(at.AddMinutes(1), "W1-05", 2, WorkAction.Qc, WorkOutcome.Failed, null, "LỖI QC"),
            new(at.AddMinutes(2), "W1-05", 2, WorkAction.Verify, WorkOutcome.Failed, null, "LỖI Verify"),
            new(at.AddMinutes(3), "W1-05", 2, WorkAction.Qc, WorkOutcome.Passed, null, "ĐẠT QC")
        };
        if (verifyRecovers)
            events.Add(new(at.AddMinutes(4), "W1-05", 2, WorkAction.Verify, WorkOutcome.Passed, null, "ĐẠT Verify"));

        var result = WorkProgressParser.Calculate("W1-05", events, planExists: true, planApproved: true);

        Assert.Equal(expected, result.CurrentStep);
        Assert.Equal(verifyRecovers ? null : "LỖI Verify", result.LatestError);
        Assert.Equal(WorkStepState.Done, Assert.Single(result.Steps, s => s.Step == WorkStep.Code).State);
        Assert.Equal(WorkStepState.Done, Assert.Single(result.Steps, s => s.Step == WorkStep.Qc).State);
        Assert.Equal(verifyRecovers ? WorkStepState.Done : WorkStepState.Failed,
            Assert.Single(result.Steps, s => s.Step == WorkStep.Verify).State);
        Assert.Equal(verifyRecovers ? WorkStepState.Current : WorkStepState.Waiting,
            Assert.Single(result.Steps, s => s.Step == WorkStep.Acceptance).State);
        Assert.False(result.IsMerged);
    }
}
