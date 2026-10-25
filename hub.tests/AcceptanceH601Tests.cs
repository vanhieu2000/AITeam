using System.Text.Json;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class AcceptanceH601Tests
{
    private static WorkflowStep Step(WorkflowResult result, WorkflowStage stage) =>
        Assert.Single(result.Steps, s => s.Stage == stage);
    private static WorkflowStep TaskStep(WorkflowResult result, string id, WorkflowStage stage) =>
        Assert.Single(Assert.Single(result.Tasks, t => t.Id == id).Steps, s => s.Stage == stage);
    private static WorkflowResult Build(params WorkflowTaskSnapshot[] tasks) =>
        WorkflowBuilder.Build(new WorkflowSnapshot(Tasks: tasks));
    private static WorkflowTaskSnapshot Task(string id = "H6-01", TaskState state = TaskState.Assigned,
        bool prepared = true, WorkflowReview[]? reviews = null, AgentRun[]? logs = null, string owner = "A") =>
        new(id, owner, state, prepared, reviews, logs);
    private static AgentRun Log(int attempt, RunState state, string task = "H6-01", string path = "logs/run.json") =>
        new("AITeam", task, attempt, state, null, null, null, null, null,
            new DateTime(2026, 10, 25).AddDays(-attempt), path);

    [Fact]
    public void EmptySnapshotHasSevenNamedOwnedPendingSteps()
    {
        var result = WorkflowBuilder.Build(null);
        Assert.Equal(Enum.GetValues<WorkflowStage>(), result.Steps.Select(s => s.Stage));
        Assert.Equal(new[] { "Tiếp nhận", "Ý tưởng", "Plan", "Chuẩn bị", "Code", "QC", "Nghiệm thu" },
            result.Steps.Select(s => s.Name));
        Assert.Equal(new[] { "Claude", "Claude", "Claude", "Codex", "", "Claude", "Claude" },
            result.Steps.Select(s => s.Owner));
        Assert.All(result.Steps, s => { Assert.Equal(WorkflowStatus.Pending, s.Status); Assert.Empty(s.Evidence); });
        Assert.Empty(result.Tasks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void MissingBriefAndPlanDoNotComplete(string? text)
    {
        var result = WorkflowBuilder.Build(new(text, text, Array.Empty<WorkflowDecision>(), Array.Empty<WorkflowTaskSnapshot>()));
        Assert.All(result.Steps, s => Assert.Equal(WorkflowStatus.Pending, s.Status));
    }

    [Fact]
    public void ApprovedIdeaAndPlanDoNotCompleteTaskStagesWithoutTasks()
    {
        var result = WorkflowBuilder.Build(new("brief", "plan", new[] {
            new WorkflowDecision(WorkflowStage.Idea, true, true, "duyệt ý tưởng"),
            new WorkflowDecision(WorkflowStage.Plan, true, true, "duyệt plan") }));
        Assert.All(result.Steps.Take(3), s => Assert.Equal(WorkflowStatus.Done, s.Status));
        Assert.Equal(new[] { "brief" }, Step(result, WorkflowStage.Intake).Evidence);
        Assert.Equal(new[] { "duyệt ý tưởng" }, Step(result, WorkflowStage.Idea).Evidence);
        Assert.Equal(new[] { "duyệt plan", "plan" }, Step(result, WorkflowStage.Plan).Evidence);
        Assert.All(result.Steps.Skip(3), s => Assert.Equal(WorkflowStatus.Pending, s.Status));
    }

    [Theory]
    [InlineData(WorkflowStage.Idea)]
    [InlineData(WorkflowStage.Plan)]
    public void LatestDecisionRejectsEarlierApproval(WorkflowStage stage)
    {
        var result = WorkflowBuilder.Build(new("brief", "plan", new[] {
            new WorkflowDecision(stage, true, true, "cũ"), new WorkflowDecision(stage, false, true, "mới") }));
        Assert.Equal(WorkflowStatus.NeedsFix, Step(result, stage).Status);
        Assert.Contains("mới", Step(result, stage).Evidence);
        Assert.DoesNotContain("cũ", Step(result, stage).Evidence);
    }

    [Fact]
    public void PlanRequiresBothContentAndApproval()
    {
        Assert.Equal(WorkflowStatus.Pending, Step(WorkflowBuilder.Build(new(Plan: "plan")), WorkflowStage.Plan).Status);
        Assert.Equal(WorkflowStatus.Pending, Step(WorkflowBuilder.Build(new(Decisions: new[] {
            new WorkflowDecision(WorkflowStage.Plan, true, true, "duyệt") })), WorkflowStage.Plan).Status);
    }

    [Fact]
    public void DifferentTaskProgressIsPreservedAndAggregated()
    {
        var result = Build(Task("B", TaskState.Review, owner: "B"), Task("A", prepared: false));
        Assert.Equal(new[] { "A", "B" }, result.Tasks.Select(t => t.Id));
        Assert.All(result.Tasks, t => Assert.Equal(new[] { WorkflowStage.Preparation, WorkflowStage.Code, WorkflowStage.Quality },
            t.Steps.Select(s => s.Stage)));
        Assert.Equal(WorkflowStatus.Pending, TaskStep(result, "A", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Done, TaskStep(result, "B", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Running, TaskStep(result, "B", WorkflowStage.Quality).Status);
        Assert.Equal(WorkflowStatus.Running, Step(result, WorkflowStage.Preparation).Status);
        Assert.Equal(WorkflowStatus.Running, Step(result, WorkflowStage.Code).Status);
        Assert.Equal("A, B", Step(result, WorkflowStage.Code).Owner);
        Assert.Equal("B", TaskStep(result, "B", WorkflowStage.Code).Owner);
        Assert.Equal("Codex", TaskStep(result, "B", WorkflowStage.Preparation).Owner);
        Assert.Equal("Claude", TaskStep(result, "B", WorkflowStage.Quality).Owner);
        Assert.Equal(new[] { "B" }, Step(result, WorkflowStage.Preparation).Evidence);
    }

    [Theory]
    [InlineData(RunState.Running, WorkflowStatus.Running)]
    [InlineData(RunState.Success, WorkflowStatus.Done)]
    [InlineData(RunState.Error, WorkflowStatus.NeedsFix)]
    public void LatestNumericAttemptWinsRegardlessOfTimeOrInputOrder(RunState state, WorkflowStatus expected)
    {
        var result = Build(Task(logs: new[] { Log(10, state, path: "vòng 10"), Log(2, RunState.Success),
            Log(100, RunState.Error, task: "khác") }));
        Assert.Equal(expected, Step(result, WorkflowStage.Code).Status);
        Assert.Equal(new[] { "vòng 10" }, Step(result, WorkflowStage.Code).Evidence);
    }

    [Theory]
    [InlineData(true, WorkflowStatus.Done, WorkflowStatus.Done)]
    [InlineData(false, WorkflowStatus.NeedsFix, WorkflowStatus.NeedsFix)]
    public void RoundTenWinsRoundTwo(bool passed, WorkflowStatus code, WorkflowStatus quality)
    {
        var result = Build(Task(state: TaskState.Review, reviews: new[] {
            new WorkflowReview(10, passed, "vòng 10"), new WorkflowReview(2, !passed, "vòng 2") }));
        Assert.Equal(code, Step(result, WorkflowStage.Code).Status);
        Assert.Equal(quality, Step(result, WorkflowStage.Quality).Status);
        Assert.Equal(new[] { "vòng 10" }, Step(result, WorkflowStage.Quality).Evidence);
    }

    [Fact]
    public void NeedsFixReturnsToCodeDespitePassingReviewAndSuccessLog()
    {
        var result = Build(Task(state: TaskState.NeedsFix, reviews: new[] { new WorkflowReview(2, true, "PASS cũ") },
            logs: new[] { Log(2, RunState.Success) }), Task("B", TaskState.Merged));
        Assert.Equal(WorkflowStatus.NeedsFix, TaskStep(result, "H6-01", WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Pending, TaskStep(result, "H6-01", WorkflowStage.Quality).Status);
        Assert.Equal(WorkflowStatus.NeedsFix, Step(result, WorkflowStage.Code).Status);
        Assert.Equal(WorkflowStatus.Running, Step(result, WorkflowStage.Quality).Status);
    }

    [Fact]
    public void LastItemBreaksEqualRoundAndAttemptTies()
    {
        var result = Build(Task(reviews: new[] { new WorkflowReview(2, true, "cũ"), new WorkflowReview(2, false, "mới") },
            logs: new[] { Log(2, RunState.Success, path: "cũ"), Log(2, RunState.Error, path: "mới") }));
        Assert.Equal(WorkflowStatus.NeedsFix, Step(result, WorkflowStage.Code).Status);
        Assert.Equal(new[] { "mới" }, Step(result, WorkflowStage.Code).Evidence);
        Assert.Equal(new[] { "mới" }, Step(result, WorkflowStage.Quality).Evidence);
    }

    [Fact]
    public void LegacyMergedTaskNeedsNoLogsOrReviewsButDoesNotApproveAcceptance()
    {
        var result = Build(Task(state: TaskState.Merged, prepared: false));
        Assert.All(result.Tasks.Single().Steps, s => Assert.Equal(WorkflowStatus.Done, s.Status));
        Assert.All(result.Steps.Skip(3).Take(3), s => Assert.Equal(WorkflowStatus.Done, s.Status));
        Assert.Equal(new[] { "H6-01" }, Step(result, WorkflowStage.Preparation).Evidence);
        Assert.Equal(new[] { "Đã merge: H6-01" }, Step(result, WorkflowStage.Code).Evidence);
        Assert.Equal(new[] { "Đã merge: H6-01" }, Step(result, WorkflowStage.Quality).Evidence);
        Assert.Equal(WorkflowStatus.Pending, Step(result, WorkflowStage.Acceptance).Status);
    }

    [Theory]
    [InlineData(true, true, WorkflowStatus.Done)]
    [InlineData(false, true, WorkflowStatus.NeedsFix)]
    [InlineData(true, false, WorkflowStatus.Pending)]
    [InlineData(false, false, WorkflowStatus.Pending)]
    public void AcceptanceUsesLastManagerDecisionOnly(bool approved, bool final, WorkflowStatus expected)
    {
        var result = WorkflowBuilder.Build(new(Decisions: new[] {
            new WorkflowDecision(WorkflowStage.Acceptance, true, true, "đạt cũ"),
            new WorkflowDecision(WorkflowStage.Acceptance, approved, final, "quyết định mới") }));
        Assert.Equal(expected, Step(result, WorkflowStage.Acceptance).Status);
        Assert.Equal(new[] { "quyết định mới" }, Step(result, WorkflowStage.Acceptance).Evidence);
    }

    [Fact]
    public void UnknownTaskWithoutEvidenceStaysPending()
    {
        var result = Build(Task(state: TaskState.Unknown, prepared: false));
        Assert.All(result.Tasks.Single().Steps, s => Assert.Equal(WorkflowStatus.Pending, s.Status));
    }

    [Fact]
    public void EvidenceIsPlainTextAndNeverOpenedAsPath()
    {
        var text = "../../không-tồn-tại/<script>alert(1)</script>";
        var result = WorkflowBuilder.Build(new(text, Tasks: new[] { Task(logs: new[] { Log(1, RunState.Success, path: text) }) }));
        Assert.Equal(new[] { text }, Step(result, WorkflowStage.Intake).Evidence);
        Assert.Equal(new[] { text }, Step(result, WorkflowStage.Code).Evidence);
    }

    [Fact]
    public void OwnersAndEvidenceAreDistinctSortedAndWhitespaceIsIgnored()
    {
        var result = Build(Task("C", logs: new[] { Log(1, RunState.Success, "C", "z") }),
            Task("B", logs: new[] { Log(1, RunState.Success, "B", "a") }),
            Task("A", logs: new[] { Log(1, RunState.Success, "A", "a") }, owner: " "));
        Assert.Equal("A", Step(result, WorkflowStage.Code).Owner);
        Assert.Equal(new[] { "a", "z" }, Step(result, WorkflowStage.Code).Evidence);
        var blank = Build(Task(logs: new[] { Log(1, RunState.Success, path: " ") }));
        Assert.Empty(Step(blank, WorkflowStage.Code).Evidence);
    }

    [Fact]
    public async Task RepeatedConcurrentCallsAreDeterministicAndDoNotMutateInput()
    {
        var tasks = new[] { Task("B", TaskState.Merged), Task("A", logs: new[] { Log(1, RunState.Error, "A") }) };
        var snapshot = new WorkflowSnapshot("brief", Tasks: tasks);
        var before = JsonSerializer.Serialize(snapshot);
        var expected = JsonSerializer.Serialize(WorkflowBuilder.Build(snapshot));
        var results = await System.Threading.Tasks.Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => System.Threading.Tasks.Task.Run(() => JsonSerializer.Serialize(WorkflowBuilder.Build(snapshot)))));
        Assert.All(results, actual => Assert.Equal(expected, actual));
        Assert.Equal(before, JsonSerializer.Serialize(snapshot));
        Assert.Equal(expected, JsonSerializer.Serialize(WorkflowBuilder.Build(snapshot with { Tasks = tasks.Reverse().ToArray() })));
    }
}
