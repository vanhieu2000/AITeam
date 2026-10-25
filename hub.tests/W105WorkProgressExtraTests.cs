using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>Test hồi quy bổ sung cho W1-05 theo yêu cầu QC (theo dõi lỗi theo từng bước).</summary>
public sealed class W105WorkProgressExtraTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 22, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Qc_failed_then_verify_failed_then_qc_passed_keeps_verify_failed_and_error()
    {
        var events = new[]
        {
            new WorkProgressEvent(At, "W1-05", 2, WorkAction.Qc, WorkOutcome.Failed, "Codex", "CẦN SỬA: lỗi QC"),
            new WorkProgressEvent(At.AddMinutes(1), "W1-05", 2, WorkAction.Verify, WorkOutcome.Failed, "Codex", "LỖI: cổng KT thất bại"),
            new WorkProgressEvent(At.AddMinutes(2), "W1-05", 2, WorkAction.Qc, WorkOutcome.Passed, "Codex", "ĐẠT")
        };

        var result = WorkProgressParser.Calculate("W1-05", events);

        Assert.Equal(WorkStep.Verify, result.CurrentStep);
        Assert.Equal("LỖI: cổng KT thất bại", result.LatestError);
        Assert.Equal(WorkStepState.Failed, Assert.Single(result.Steps, s => s.Step == WorkStep.Verify).State);
        Assert.NotEqual(WorkStepState.Failed, Assert.Single(result.Steps, s => s.Step == WorkStep.Qc).State);
    }
}
