namespace AITeam.Core.Jobs;

/// <summary>Trạng thái 1 task (thiết kế: docs/design/V4-MULTI-AI.md mục B4).</summary>
public enum JobState
{
    Created, Planned, Ready, Running, Verifying, Reviewing, Approval, Merged, Done,
    NeedsFix, Paused, Cancelled, Failed, Interrupted, Blocked,
}

/// <summary>Phân loại lỗi để không retry mù: hạ tầng ≠ provider ≠ build ≠ code ≠ thiết kế ≠ nghiệp vụ.</summary>
public enum ErrorKind { None, Infrastructure, Provider, Build, Code, Design, Business }

public sealed record JobRecord(
    string Project,
    string Task,
    JobState State,
    int Attempt,
    string? ProviderId,
    int? Pid,
    string Machine,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ErrorKind LastErrorKind,
    string? LastError);

public sealed record JobEvent(DateTimeOffset At, JobState From, JobState To, string Machine, string Detail, ErrorKind ErrorKind);

/// <summary>Chuyển trạng thái không hợp lệ (ví dụ Running → Merged khi chưa verify / review).</summary>
public sealed class InvalidTransitionException(JobState from, JobState to)
    : InvalidOperationException($"Không được chuyển {from} → {to}")
{
    public JobState From { get; } = from;
    public JobState To { get; } = to;
}

/// <summary>Bảng chuyển trạng thái hợp lệ — QUYẾT ĐỊNH THIẾT KẾ của Claude, agent không đổi.</summary>
public static class JobTransitions
{
    private static readonly JobState[] Stoppable = [JobState.Paused, JobState.Cancelled, JobState.Failed, JobState.Interrupted, JobState.Blocked];

    public static readonly IReadOnlyDictionary<JobState, JobState[]> Allowed = new Dictionary<JobState, JobState[]>
    {
        [JobState.Created] = [JobState.Planned, JobState.Ready, JobState.Cancelled],
        [JobState.Planned] = [JobState.Ready, JobState.Cancelled],
        [JobState.Ready] = [JobState.Running, JobState.Paused, JobState.Cancelled],
        [JobState.Running] = [JobState.Verifying, .. Stoppable],
        [JobState.Verifying] = [JobState.Reviewing, JobState.NeedsFix, JobState.Approval, .. Stoppable], // Approval: dự án tắt review tự động
        [JobState.Reviewing] = [JobState.Approval, JobState.NeedsFix, .. Stoppable],
        [JobState.NeedsFix] = [JobState.Running, JobState.Cancelled, JobState.Blocked],
        [JobState.Approval] = [JobState.Merged, JobState.NeedsFix, JobState.Reviewing, JobState.Cancelled], // Reviewing: diff đổi sau review
        [JobState.Merged] = [JobState.Done],
        [JobState.Paused] = [JobState.Ready, JobState.Cancelled],
        [JobState.Interrupted] = [JobState.Ready, JobState.Running, JobState.Cancelled, JobState.Failed],
        [JobState.Blocked] = [JobState.Ready, JobState.Reviewing, JobState.Cancelled, JobState.Failed],
        [JobState.Failed] = [JobState.Ready, JobState.Cancelled],
        [JobState.Done] = [],
        [JobState.Cancelled] = [],
    };

    public static bool IsAllowed(JobState from, JobState to) => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>Trạng thái có tiến trình agent / reviewer đang chạy (PID chết ⇒ Interrupted khi phục hồi).</summary>
    public static bool IsActive(JobState s) => s is JobState.Running or JobState.Verifying or JobState.Reviewing;
}
