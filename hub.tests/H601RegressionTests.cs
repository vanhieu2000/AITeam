using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class H601RegressionTests
{
    private static WorkflowStep Step(WorkflowResult result, WorkflowStage stage) =>
        Assert.Single(result.Steps, s => s.Stage == stage);

    private static WorkflowStep TaskStep(WorkflowResult result, string id, WorkflowStage stage) =>
        Assert.Single(Assert.Single(result.Tasks, t => t.Id == id).Steps, s => s.Stage == stage);

    private static AgentRun Log(int attempt, RunState state, string task = "H6-01", string path = "logs/run.json") =>
        new("AITeam", task, attempt, state, null, null, null, null, null,
            new DateTime(2026, 10, 25).AddDays(-attempt), path);

    [Fact]
    public void AssignedTaskWithPassingReviewAndErrorLogEvaluatesCodeToNeedsFix()
    {
        // Task Assigned cÃ³ review vÃ²ng 2 PASS nhÆ°ng cÃ³ log attempt 10 Error:
        // PASS cÅ© khÃ´ng Ä‘Æ°á»£c che lá»—i vÃ²ng má»›i, Code pháº£i lÃ  NeedsFix theo XML.
        var task = new WorkflowTaskSnapshot(
            Id: "H6-01",
            Owner: "A",
            State: TaskState.Assigned,
            Prepared: true,
            Reviews: new[] { new WorkflowReview(2, true, "PASS cÅ©") },
            Logs: new[] { Log(10, RunState.Error, path: "vÃ²ng 10 error") }
        );

        var result = WorkflowBuilder.Build(new WorkflowSnapshot(Tasks: new[] { task }));

        Assert.Equal(WorkflowStatus.NeedsFix, TaskStep(result, "H6-01", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.NeedsFix, Step(result, WorkflowStage.Code).Status);
        Assert.Contains("vÃ²ng 10 error", Step(result, WorkflowStage.Code).Evidence);
    }

    [Fact]
    public void AssignedTaskWithPassingReviewAndRunningLogEvaluatesCodeToRunning()
    {
        // Task Assigned cÃ³ review vÃ²ng 2 PASS nhÆ°ng cÃ³ log attempt 10 Running:
        // PASS cÅ© khÃ´ng Ä‘Æ°á»£c che log Ä‘ang cháº¡y, Code pháº£i lÃ  Running theo XML.
        var task = new WorkflowTaskSnapshot(
            Id: "H6-01",
            Owner: "A",
            State: TaskState.Assigned,
            Prepared: true,
            Reviews: new[] { new WorkflowReview(2, true, "PASS cÅ©") },
            Logs: new[] { Log(10, RunState.Running, path: "vÃ²ng 10 running") }
        );

        var result = WorkflowBuilder.Build(new WorkflowSnapshot(Tasks: new[] { task }));

        Assert.Equal(WorkflowStatus.Running, TaskStep(result, "H6-01", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Running, Step(result, WorkflowStage.Code).Status);
    }

    [Fact]
    public void AssignedTaskWithPassingReviewAndNoLogStaysPending()
    {
        // Task Assigned cÃ³ review vÃ²ng 2 PASS nhÆ°ng thiáº¿u log:
        // ChÆ°a vÃ o Review state vÃ  khÃ´ng cÃ³ log thÃ¬ Code váº«n lÃ  Pending.
        var task = new WorkflowTaskSnapshot(
            Id: "H6-01",
            Owner: "A",
            State: TaskState.Assigned,
            Prepared: true,
            Reviews: new[] { new WorkflowReview(2, true, "PASS cÅ©") },
            Logs: null
        );

        var result = WorkflowBuilder.Build(new WorkflowSnapshot(Tasks: new[] { task }));

        Assert.Equal(WorkflowStatus.Pending, TaskStep(result, "H6-01", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Pending, Step(result, WorkflowStage.Code).Status);
    }
}