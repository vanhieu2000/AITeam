using AITeam.Core.Jobs;

namespace AITeam.Core.Tests;

/// <summary>Test nghiệm thu P0-6 (Claude viết trước, agent KHÔNG sửa). Thiết kế: docs/design/V4-MULTI-AI.md mục B4.</summary>
public sealed class AcceptanceP06JobStoreTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("aiteam-jobs-").FullName;
    private readonly HashSet<int> _alive = [100, 200];
    public void Dispose() => Directory.Delete(_root, true);

    private JobStore Store() => new(_root, "MAY-TEST", pid => _alive.Contains(pid), () => new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.FromHours(7)));

    private static JobStore Advance(JobStore s, params JobState[] path)
    {
        foreach (var st in path) s.Transition("P", "T-1", st, $"tới {st}");
        return s;
    }

    [Fact]
    public void Create_is_idempotent_and_starts_created()
    {
        var s = Store();
        var a = s.Create("P", "T-1");
        Assert.Equal(JobState.Created, a.State);
        Assert.Equal(0, a.Attempt);
        s.Transition("P", "T-1", JobState.Ready, "sẵn sàng");
        Assert.Equal(JobState.Ready, s.Create("P", "T-1").State); // không ghi đè
    }

    [Fact]
    public void Happy_path_to_done_with_history_in_order_and_attempt_counted()
    {
        var s = Store();
        s.Create("P", "T-1");
        Advance(s, JobState.Ready, JobState.Running, JobState.Verifying, JobState.Reviewing, JobState.NeedsFix, JobState.Running,
            JobState.Verifying, JobState.Reviewing, JobState.Approval, JobState.Merged, JobState.Done);
        var j = s.Get("P", "T-1")!;
        Assert.Equal(JobState.Done, j.State);
        Assert.Equal(2, j.Attempt);
        var h = s.History("P", "T-1");
        Assert.Equal(11, h.Count);
        Assert.Equal(JobState.Created, h[0].From);
        Assert.Equal(JobState.Done, h[^1].To);
        Assert.All(h, e => Assert.Equal("MAY-TEST", e.Machine));
    }

    [Theory]
    [InlineData(JobState.Merged)]
    [InlineData(JobState.Approval)]
    [InlineData(JobState.Done)]
    public void Cannot_skip_verify_and_review(JobState target)
    {
        var s = Store();
        s.Create("P", "T-1");
        Advance(s, JobState.Ready, JobState.Running);
        var ex = Assert.Throws<InvalidTransitionException>(() => s.Transition("P", "T-1", target, "nhảy cóc"));
        Assert.Equal(JobState.Running, ex.From);
        Assert.Equal(JobState.Running, s.Get("P", "T-1")!.State); // không ghi gì
        Assert.Equal(2, s.History("P", "T-1").Count);
    }

    [Fact]
    public void Done_and_cancelled_are_final()
    {
        var s = Store();
        s.Create("P", "T-1");
        s.Transition("P", "T-1", JobState.Cancelled, "hủy");
        Assert.Throws<InvalidTransitionException>(() => s.Transition("P", "T-1", JobState.Ready, "mở lại"));
    }

    [Fact]
    public void Transition_unknown_job_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Store().Transition("P", "KHONG-CO", JobState.Ready, "x"));
    }

    [Fact]
    public void Error_kind_and_message_are_recorded()
    {
        var s = Store();
        s.Create("P", "T-1");
        Advance(s, JobState.Ready, JobState.Running);
        var j = s.Transition("P", "T-1", JobState.Failed, "hết credits", ErrorKind.Provider, providerId: "agy");
        Assert.Equal(ErrorKind.Provider, j.LastErrorKind);
        Assert.Equal("hết credits", j.LastError);
        Assert.Equal(ErrorKind.Provider, s.History("P", "T-1")[^1].ErrorKind);
    }

    [Fact]
    public void Lock_prevents_duplicate_run_while_owner_alive()
    {
        var s = Store();
        s.Create("P", "T-1");
        Assert.True(s.TryAcquireRun("P", "T-1", 100));
        Assert.False(s.TryAcquireRun("P", "T-1", 200));
        s.ReleaseRun("P", "T-1", 200); // PID khác: không nhả
        Assert.False(s.TryAcquireRun("P", "T-1", 200));
        s.ReleaseRun("P", "T-1", 100);
        Assert.True(s.TryAcquireRun("P", "T-1", 200));
    }

    [Fact]
    public void Orphan_lock_of_dead_pid_marks_interrupted_and_is_taken_over()
    {
        var s = Store();
        s.Create("P", "T-1");
        Advance(s, JobState.Ready);
        s.Transition("P", "T-1", JobState.Running, "chạy", pid: 999);
        _alive.Add(999);
        Assert.True(s.TryAcquireRun("P", "T-1", 999)); // tiến trình 999 giữ khóa lúc còn sống
        _alive.Remove(999);                            // rồi chết mà không nhả khóa
        Assert.True(s.TryAcquireRun("P", "T-1", 100));
        Assert.Equal(JobState.Interrupted, s.Get("P", "T-1")!.State);
        Assert.Contains("999", s.History("P", "T-1")[^1].Detail);
    }

    [Fact]
    public void Recover_marks_only_dead_active_jobs()
    {
        var s = Store();
        foreach (var (task, pid) in new[] { ("T-1", 100), ("T-2", 555) })
        {
            s.Create("P", task);
            s.Transition("P", task, JobState.Ready, "x");
            s.Transition("P", task, JobState.Running, "chạy", pid: pid);
        }
        s.Create("P", "T-3"); // Created: không đụng
        var moved = s.RecoverInterrupted();
        var one = Assert.Single(moved);
        Assert.Equal("T-2", one.Task);
        Assert.Equal(JobState.Interrupted, s.Get("P", "T-2")!.State);
        Assert.Equal(ErrorKind.Infrastructure, s.Get("P", "T-2")!.LastErrorKind);
        Assert.Equal(JobState.Running, s.Get("P", "T-1")!.State);
        Assert.Equal(JobState.Created, s.Get("P", "T-3")!.State);
    }

    [Theory]
    [InlineData("..", "T-1")]
    [InlineData("P", "..\\x")]
    [InlineData("P", "a/b")]
    [InlineData("C:", "T-1")]
    public void Unsafe_names_are_rejected(string project, string task)
    {
        Assert.Throws<ArgumentException>(() => Store().Create(project, task));
    }

    [Fact]
    public void Corrupt_history_line_is_skipped()
    {
        var s = Store();
        s.Create("P", "T-1");
        s.Transition("P", "T-1", JobState.Ready, "x");
        File.AppendAllText(Path.Combine(_root, "P", "T-1.events.jsonl"), "{hỏng\n");
        s.Transition("P", "T-1", JobState.Running, "y", pid: 100);
        Assert.Equal(2, s.History("P", "T-1").Count);
    }

    [Fact]
    public void Record_file_is_never_left_half_written()
    {
        var s = Store();
        s.Create("P", "T-1");
        s.Transition("P", "T-1", JobState.Ready, "x");
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "P"), "*.tmp"));
        Assert.Equal(JobState.Ready, Store().Get("P", "T-1")!.State); // đọc lại từ đĩa bằng instance mới
    }
}
