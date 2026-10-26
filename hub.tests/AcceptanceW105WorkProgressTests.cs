using System.Text;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>Nghiệm thu bất biến W1-05; dữ liệu gốc là bản chép nguyên byte, biến thể được ghi rõ.</summary>
public sealed class AcceptanceW105WorkProgressTests
{
    private const string TaskId = "W1-05";
    private const string RequestName = "R-011-bo-testqc-chuan-codex-tra-ke-hoachmockup.md";
    private static readonly DateTimeOffset At = new(2026, 10, 25, 22, 0, 0, TimeSpan.FromHours(7));
    private static string Fixture(string name) =>
        Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "WorkProgress", name)));

    // Chỉ chèn vào bản sao phiếu thật; không dựng lại cấu trúc phiếu.
    private static string Journal(params string[] lines)
    {
        var original = Fixture(RequestName);
        Assert.Contains("## 3. Nhật ký (", original);
        Assert.Contains("\r\n", original);
        return original.Replace("| | Nhận yêu cầu | |", string.Join("\r\n", lines), StringComparison.Ordinal);
    }

    // Biến thể định dạng workflow từ brief R-011, chưa có dòng thực thi trong phiếu R-011 gốc.
    private static string Line(string action = "Verify", string result = "ĐẠT", int round = 2,
        string time = "22:00 25/10/2026", string task = TaskId) =>
        $"- {time} | {task} | {action} | lượt {round} | {result} <!-- workflow:{task}:{action}:{round} -->";

    private static WorkProgressEvent Event(WorkAction action, WorkOutcome outcome = WorkOutcome.Passed,
        int round = 2, int minute = 0, string task = TaskId, string? actor = "Codex") =>
        new(At.AddMinutes(minute), task, round, action, outcome, actor, $"{action}: {outcome}");

    private static WorkProgressStep Step(WorkProgress progress, WorkStep step) =>
        Assert.Single(progress.Steps, s => s.Step == step);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n")]
    public void Empty_input_has_no_events(string? input) =>
        Assert.Empty(WorkProgressParser.Parse(input));

    [Fact]
    public void Real_numbered_legacy_table_keeps_unknown_event_without_fabricating_time()
    {
        var events = WorkProgressParser.Parse(Fixture(RequestName));
        var item = Assert.Single(events);
        Assert.Contains("Nhận yêu cầu", item.Text);
        Assert.Null(item.Time);
        Assert.Null(item.Actor);
        Assert.Equal("", item.TaskId);
        Assert.Equal(0, item.Round);
        Assert.Equal(WorkAction.Unknown, item.Action);
        Assert.Equal(WorkOutcome.Unknown, item.Outcome);
    }

    [Theory]
    [InlineData("\r\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r", true)]
    public void Workflow_handles_line_endings_bom_and_numbered_heading(string newline, bool bom)
    {
        var input = Journal(Line()).TrimStart('\uFEFF').Replace("\r\n", newline);
        var item = Assert.Single(WorkProgressParser.Parse((bom ? "\uFEFF" : "") + input));
        Assert.Equal(TaskId, item.TaskId);
        Assert.Equal(2, item.Round);
        Assert.Equal(WorkAction.Verify, item.Action);
        Assert.Equal(WorkOutcome.Passed, item.Outcome);
        Assert.Equal(At, item.Time);
        Assert.Null(item.Actor);
        Assert.DoesNotContain("workflow:", item.Text);
    }

    [Fact]
    public void Workflow_only_reads_journal_and_removes_exact_duplicate()
    {
        var row = Line();
        var input = row + "\r\n" + Journal(row, row, Line("Qc")) + "\r\n" + Line("Merge");
        var items = WorkProgressParser.Parse(input);
        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { WorkAction.Verify, WorkAction.Qc }, items.Select(e => e.Action));
    }

    [Theory]
    [InlineData("- thiếu cột")]
    [InlineData("- 22:00 | W1-05 | Verify | lượt x | ĐẠT")]
    [InlineData("- 22:00 | W1-05 | Verify | lượt 0 | ĐẠT")]
    [InlineData("- 22:00 | W1-05 | Verify | lượt 999999999999 | ĐẠT")]
    [InlineData("- 22:00 | ../W1-05 | Verify | lượt 2 | ĐẠT")]
    [InlineData("- 22:00 | W1-05 | Execute | lượt 2 | ĐẠT")]
    public void Malformed_workflow_is_ignored(string row) =>
        Assert.Empty(WorkProgressParser.Parse(Journal(row)));

    [Theory]
    [InlineData("Prep", WorkAction.Prep)]
    [InlineData("Assign", WorkAction.Assign)]
    [InlineData("Verify", WorkAction.Verify)]
    [InlineData("Qc", WorkAction.Qc)]
    [InlineData("Merge", WorkAction.Merge)]
    public void Recognizes_all_workflow_actions(string action, WorkAction expected) =>
        Assert.Equal(expected, Assert.Single(WorkProgressParser.Parse(Journal(Line(action)))).Action);

    [Theory]
    [InlineData("ĐẠT", WorkOutcome.Passed)]
    [InlineData("PASS: xong", WorkOutcome.Passed)]
    [InlineData("PASSED", WorkOutcome.Passed)]
    [InlineData("CẦN SỬA: lỗi build", WorkOutcome.Failed)]
    [InlineData("FAIL: lỗi", WorkOutcome.Failed)]
    [InlineData("FAILED", WorkOutcome.Failed)]
    [InlineData("LỖI: dừng", WorkOutcome.Failed)]
    [InlineData("BLOCKED: hết hạn mức", WorkOutcome.Blocked)]
    [InlineData("KHÔNG ĐẠT", WorkOutcome.Failed)] // Quản lý sửa 26/10: team.ps1 ghi "KHÔNG ĐẠT" khi cổng KT trượt ⇒ là lỗi (dữ liệu thật requests/R-011)
    [InlineData("chưa rõ", WorkOutcome.Unknown)]
    public void Results_use_explicit_prefix_not_substring(string result, WorkOutcome expected) =>
        Assert.Equal(expected, Assert.Single(WorkProgressParser.Parse(Journal(Line(result: result)))).Outcome);

    [Theory]
    [InlineData("không phải giờ")]
    [InlineData("")]
    [InlineData("22:00")]
    public void Missing_or_bad_time_remains_unknown(string time)
    {
        var item = Assert.Single(WorkProgressParser.Parse(Journal(Line(time: time))));
        Assert.Null(item.Time);
        Assert.Equal(WorkAction.Verify, item.Action);
    }

    [Fact]
    public void Hour_only_uses_received_date_in_vietnam()
    {
        var received = new DateTimeOffset(2026, 10, 25, 18, 0, 0, TimeSpan.Zero);
        var item = Assert.Single(WorkProgressParser.Parse(Journal(Line(time: "22:00")), received));
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 22, 0, 0, TimeSpan.FromHours(7)), item.Time);
    }

    [Fact]
    public void Iso_offset_is_preserved()
    {
        var item = Assert.Single(WorkProgressParser.Parse(Journal(Line(time: "2026-10-25T15:00:00Z"))));
        Assert.Equal(TimeSpan.Zero, item.Time!.Value.Offset);
        Assert.Equal(At.ToUniversalTime(), item.Time);
    }

    [Theory]
    [InlineData(false, false, WorkStep.Received)]
    [InlineData(true, false, WorkStep.PlanApproval)]
    [InlineData(true, true, WorkStep.Prep)]
    public void Initial_progress_has_nine_steps_and_no_invented_metadata(bool plan, bool approved, WorkStep current)
    {
        var result = WorkProgressParser.Calculate(TaskId, null, plan, approved);
        Assert.Equal(Enum.GetValues<WorkStep>(), result.Steps.Select(s => s.Step));
        Assert.Equal(current, result.CurrentStep);
        Assert.Equal(WorkStepState.Current, Step(result, current).State);
        Assert.False(result.IsMerged);
        Assert.Null(result.LatestError);
        Assert.Equal(plan || approved ? WorkStepState.Done : WorkStepState.Current, Step(result, WorkStep.Received).State);
        Assert.Equal(plan || approved ? WorkStepState.Done : WorkStepState.Waiting, Step(result, WorkStep.Plan).State);
        Assert.Equal(approved ? WorkStepState.Done : plan ? WorkStepState.Current : WorkStepState.Waiting,
            Step(result, WorkStep.PlanApproval).State);
        Assert.All(result.Steps, s =>
        {
            Assert.Equal("Chưa có dữ liệu", s.TimeText);
            Assert.Equal("Chưa có dữ liệu", s.ActorText);
        });
    }

    [Theory]
    [InlineData(WorkAction.Prep, WorkStep.Prep, WorkStep.Code)]
    [InlineData(WorkAction.Verify, WorkStep.Verify, WorkStep.Qc)]
    [InlineData(WorkAction.Qc, WorkStep.Qc, WorkStep.Acceptance)]
    public void Passed_gate_completes_only_evidenced_step(WorkAction action, WorkStep done, WorkStep current)
    {
        var result = WorkProgressParser.Calculate(TaskId, new[] { Event(action) });
        Assert.Equal(current, result.CurrentStep);
        Assert.Equal(WorkStepState.Done, Step(result, done).State);
        Assert.Equal(WorkStepState.Current, Step(result, current).State);
        Assert.Equal("22:00 25/10/2026", Step(result, done).TimeText);
        Assert.Equal("Codex", Step(result, done).ActorText);
        Assert.NotEqual(WorkStepState.Done, Step(result, WorkStep.Acceptance).State);
        Assert.False(result.IsMerged);
    }

    [Theory]
    [InlineData(WorkAction.Prep, WorkStep.Prep)]
    [InlineData(WorkAction.Assign, WorkStep.Code)]
    [InlineData(WorkAction.Verify, WorkStep.Verify)]
    [InlineData(WorkAction.Qc, WorkStep.Qc)]
    [InlineData(WorkAction.Merge, WorkStep.Merge)]
    public void Failure_keeps_step_and_error(WorkAction action, WorkStep step)
    {
        var failed = Event(action, WorkOutcome.Failed);
        var result = WorkProgressParser.Calculate(TaskId, new[] { failed });
        Assert.Equal(step, result.CurrentStep);
        Assert.Equal(WorkStepState.Failed, Step(result, step).State);
        Assert.Equal(failed.Text, result.LatestError);
        Assert.False(result.IsMerged);
    }

    [Theory]
    [InlineData(WorkAction.Verify, WorkOutcome.Failed, WorkStep.Verify)]
    [InlineData(WorkAction.Qc, WorkOutcome.Failed, WorkStep.Qc)]
    [InlineData(WorkAction.Qc, WorkOutcome.Blocked, WorkStep.Qc)]
    public void Assign_summary_cannot_hide_gate_failure(WorkAction action, WorkOutcome outcome, WorkStep step)
    {
        var failed = Event(action, outcome);
        var result = WorkProgressParser.Calculate(TaskId, new[] { failed, Event(WorkAction.Assign, minute: 1) });
        Assert.Equal(step, result.CurrentStep);
        Assert.Equal(WorkStepState.Failed, Step(result, step).State);
        Assert.Equal(failed.Text, result.LatestError);
    }

    [Fact]
    public void Success_after_failure_clears_current_error()
    {
        var result = WorkProgressParser.Calculate(TaskId, new[]
        {
            Event(WorkAction.Verify, WorkOutcome.Failed), Event(WorkAction.Verify, minute: 1)
        });
        Assert.Equal(WorkStep.Qc, result.CurrentStep);
        Assert.Equal(WorkStepState.Done, Step(result, WorkStep.Verify).State);
        Assert.Null(result.LatestError);
    }

    [Fact]
    public void Numeric_round_and_task_filter_prevent_old_or_other_failure_winning()
    {
        var result = WorkProgressParser.Calculate("w1-05", new[]
        {
            Event(WorkAction.Prep, round: 10), Event(WorkAction.Verify, WorkOutcome.Failed, round: 2),
            Event(WorkAction.Qc, WorkOutcome.Blocked, round: 99, task: "W1-06")
        });
        Assert.Equal(WorkStep.Code, result.CurrentStep);
        Assert.Null(result.LatestError);
        Assert.NotEqual(WorkStepState.Done, Step(result, WorkStep.Qc).State);
    }

    [Fact]
    public void Timestamp_order_uses_instant_not_offset_or_source_order()
    {
        var earlier = Event(WorkAction.Verify, WorkOutcome.Failed) with { Time = At.ToUniversalTime() };
        var later = Event(WorkAction.Verify, minute: 1);
        var result = WorkProgressParser.Calculate(TaskId, new[] { later, earlier });
        Assert.Equal(WorkStep.Qc, result.CurrentStep);
        Assert.Null(result.LatestError);
    }

    [Fact]
    public void Missing_metadata_uses_placeholder_and_source_order()
    {
        var item = Event(WorkAction.Verify) with { Time = null, Actor = null };
        var result = WorkProgressParser.Calculate(TaskId, new[] { item });
        Assert.Equal(WorkStep.Qc, result.CurrentStep);
        Assert.Equal("Chưa có dữ liệu", Step(result, WorkStep.Verify).TimeText);
        Assert.Equal("Chưa có dữ liệu", Step(result, WorkStep.Verify).ActorText);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Merge_evidence_overrides_old_error_but_never_proves_acceptance(bool board)
    {
        var events = new List<WorkProgressEvent> { Event(WorkAction.Qc, WorkOutcome.Failed) };
        if (!board) events.Add(Event(WorkAction.Merge, minute: 1));
        var result = WorkProgressParser.Calculate(TaskId, events, boardStatus: board ? "✅ Đã merge (vòng 2)" : null);
        Assert.True(result.IsMerged);
        Assert.Equal(WorkStep.Merge, result.CurrentStep);
        Assert.Equal(WorkStepState.Done, Step(result, WorkStep.Merge).State);
        Assert.NotEqual(WorkStepState.Done, Step(result, WorkStep.Acceptance).State);
        Assert.Null(result.LatestError);
    }

    [Fact]
    public void Real_board_status_is_consumable_without_inferring_gate_results()
    {
        var row = Fixture("README.md").Split('\n').Single(s => s.StartsWith("| [H6-01 ", StringComparison.Ordinal));
        var status = row.Split('|')[4].Trim();
        var result = WorkProgressParser.Calculate("H6-01", Array.Empty<WorkProgressEvent>(), boardStatus: status);
        Assert.True(result.IsMerged);
        Assert.Equal(WorkStepState.Done, Step(result, WorkStep.Merge).State);
        Assert.NotEqual(WorkStepState.Done, Step(result, WorkStep.Verify).State);
        Assert.NotEqual(WorkStepState.Done, Step(result, WorkStep.Acceptance).State);
    }

    [Theory]
    [InlineData("🟡 Đang chạy")]
    [InlineData("🔵 Chờ review")]
    [InlineData("🔴 Cần sửa")]
    public void Board_without_merge_does_not_prove_any_gate(string status)
    {
        var result = WorkProgressParser.Calculate(TaskId, null, boardStatus: status);
        Assert.False(result.IsMerged);
        Assert.Equal(WorkStepState.Waiting, Step(result, WorkStep.Verify).State);
        Assert.Equal(WorkStepState.Waiting, Step(result, WorkStep.Qc).State);
        Assert.Equal(WorkStepState.Waiting, Step(result, WorkStep.Acceptance).State);
    }

    [Fact]
    public void Assign_success_is_waiting_for_code_not_code_completed()
    {
        var result = WorkProgressParser.Calculate(TaskId, new[] { Event(WorkAction.Assign) });
        Assert.Equal(WorkStep.Code, result.CurrentStep);
        Assert.Equal(WorkStepState.Current, Step(result, WorkStep.Code).State);
        Assert.Equal(WorkStepState.Waiting, Step(result, WorkStep.Verify).State);
    }

    [Fact]
    public void Later_qc_success_does_not_bypass_unresolved_verify_failure()
    {
        var failed = Event(WorkAction.Verify, WorkOutcome.Failed);
        var result = WorkProgressParser.Calculate(TaskId, new[] { failed, Event(WorkAction.Qc, minute: 1) });
        Assert.Equal(WorkStep.Verify, result.CurrentStep);
        Assert.Equal(WorkStepState.Failed, Step(result, WorkStep.Verify).State);
        Assert.Equal(failed.Text, result.LatestError);
    }

    [Fact]
    public void New_round_merge_clears_previous_round_failure()
    {
        var result = WorkProgressParser.Calculate(TaskId, new[]
        {
            Event(WorkAction.Qc, WorkOutcome.Blocked, round: 2),
            Event(WorkAction.Merge, round: 10, minute: 1)
        });
        Assert.True(result.IsMerged);
        Assert.Equal(WorkStep.Merge, result.CurrentStep);
        Assert.Null(result.LatestError);
        Assert.Equal(WorkStepState.Waiting, Step(result, WorkStep.Acceptance).State);
    }

    [Fact]
    public void Unknown_outcome_cannot_advance_gate_and_equal_times_keep_source_order()
    {
        var result = WorkProgressParser.Calculate(TaskId, new[]
        {
            Event(WorkAction.Verify, WorkOutcome.Failed), Event(WorkAction.Verify, WorkOutcome.Unknown)
        });
        Assert.Equal(WorkStep.Verify, result.CurrentStep);
        Assert.NotNull(result.LatestError);
        var resolved = WorkProgressParser.Calculate(TaskId, new[]
        {
            Event(WorkAction.Verify, WorkOutcome.Failed), Event(WorkAction.Verify)
        });
        Assert.Equal(WorkStep.Qc, resolved.CurrentStep);
        Assert.Null(resolved.LatestError);
    }
    [Fact]
    public void Untrusted_text_remains_data_and_input_is_not_mutated()
    {
        const string text = "BLOCKED: <script>alert(1)</script> ../outside $(Get-Process)";
        var parsed = Assert.Single(WorkProgressParser.Parse(Journal(Line("Qc", text))));
        Assert.Equal(WorkOutcome.Blocked, parsed.Outcome);
        Assert.Contains("<script>alert(1)</script>", parsed.Text);
        var input = new[] { parsed };
        var copy = input.ToArray();
        var first = WorkProgressParser.Calculate(TaskId, input);
        var second = WorkProgressParser.Calculate(TaskId, input);
        Assert.Equal(copy, input);
        Assert.Equal(first.CurrentStep, second.CurrentStep);
        Assert.Equal(first.Steps.ToArray(), second.Steps.ToArray());
        Assert.Equal(parsed.Text, first.LatestError);
    }
}