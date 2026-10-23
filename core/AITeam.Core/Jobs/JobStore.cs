namespace AITeam.Core.Jobs;

/// <summary>
/// Lưu trạng thái job CỤC BỘ từng máy (tiến trình, PID, nhịp sống). Nguồn sự thật dùng chung giữa các máy vẫn là
/// file task + bảng việc trên git. Thiết kế: docs/design/V4-MULTI-AI.md mục B4.
/// KHUNG do Claude viết — agent chỉ điền phần thân (TODO(agent)), giữ nguyên chữ ký public.
///
/// Bố cục trên đĩa (root thường là %LOCALAPPDATA%\AITeam\jobs):
///   {root}\{project}\{task}.json          — JobRecord hiện tại (ghi file tạm rồi đổi tên: không bao giờ để file dở)
///   {root}\{project}\{task}.events.jsonl  — mỗi lần chuyển trạng thái 1 dòng JobEvent, chỉ ghi thêm
///   {root}\{project}\{task}.lock          — khóa chống chạy trùng: nội dung là PID đang giữ
/// Tên project / task chỉ nhận [A-Za-z0-9._-]; tên khác (có .., /, \, :) ⇒ ArgumentException (chặn ghi ra ngoài root).
/// </summary>
public sealed class JobStore(string root, string machine, Func<int, bool> isProcessAlive, Func<DateTimeOffset>? clock = null)
{
    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.Now;

    /// <summary>Tạo job ở trạng thái Created (Attempt = 0). Đã có ⇒ trả bản đang có, không ghi đè.</summary>
    public JobRecord Create(string project, string task)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    public JobRecord? Get(string project, string task)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>
    /// Chuyển trạng thái theo JobTransitions; không hợp lệ ⇒ InvalidTransitionException, KHÔNG ghi gì.
    /// Vào Running ⇒ Attempt + 1. Ghi LastError / LastErrorKind (None khi không lỗi). Thêm 1 JobEvent.
    /// Job chưa có ⇒ InvalidOperationException.
    /// </summary>
    public JobRecord Transition(string project, string task, JobState to, string detail,
        ErrorKind errorKind = ErrorKind.None, string? providerId = null, int? pid = null)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>
    /// Giữ khóa chạy cho task. Đã có khóa của PID còn sống ⇒ false.
    /// Khóa của PID đã chết (máy / tiến trình sập) ⇒ coi là khóa mồ côi: nếu job đang ở trạng thái IsActive thì
    /// chuyển Interrupted (detail nêu PID cũ), rồi giữ khóa mới ⇒ true. Tạo file khóa phải nguyên tử (FileMode.CreateNew).
    /// </summary>
    public bool TryAcquireRun(string project, string task, int pid)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>Nhả khóa nếu đúng PID đang giữ; PID khác ⇒ không làm gì.</summary>
    public void ReleaseRun(string project, string task, int pid)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>
    /// Phục hồi sau sự cố: mọi job IsActive có Pid mà tiến trình đã chết ⇒ Interrupted (ErrorKind.Infrastructure),
    /// xóa khóa mồ côi. Trả danh sách job đã chuyển. Job đang sống không bị đụng tới.
    /// </summary>
    public IReadOnlyList<JobRecord> RecoverInterrupted()
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>Lịch sử chuyển trạng thái theo thứ tự ghi. Dòng hỏng trong file ⇒ bỏ qua dòng đó, không ném lỗi.</summary>
    public IReadOnlyList<JobEvent> History(string project, string task)
    {
        throw new NotImplementedException("TODO(agent)");
    }
}
