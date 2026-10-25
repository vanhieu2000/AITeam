namespace AITeam.Hub.Services;

/// <summary>Chín bước theo brief R-011, theo thứ tự hiển thị.</summary>
public enum WorkStep { Received, Plan, PlanApproval, Prep, Code, Verify, Qc, Acceptance, Merge }

/// <summary>Waiting: chưa có bằng chứng; Current: đang chờ làm; Done: có bằng chứng đạt; Failed: cần sửa.</summary>
public enum WorkStepState { Waiting, Current, Done, Failed }

/// <summary>Loại thao tác có trong dòng workflow; bảng cũ không phân loại được dùng Unknown.</summary>
public enum WorkAction { Unknown, Prep, Assign, Verify, Qc, Merge }

/// <summary>Kết quả đã chuẩn hóa; BLOCKED khác lỗi thông thường nhưng đều giữ bước cần sửa.</summary>
public enum WorkOutcome { Unknown, Passed, Failed, Blocked }

/// <summary>Sự kiện riêng của parser, không thay hợp đồng Hub. Time giữ offset; thiếu giờ/người dùng null.
/// Round=0 và TaskId="" chỉ dành cho bảng cũ chưa xác định được. Text giữ nội dung, bỏ marker workflow.</summary>
public sealed record WorkProgressEvent(
    DateTimeOffset? Time, string TaskId, int Round, WorkAction Action,
    WorkOutcome Outcome, string? Actor, string Text);

/// <summary>Một bước hiển thị. Thiếu giờ hoặc người: từng trường tương ứng là "Chưa có dữ liệu".
/// TimeText có giờ dùng "HH:mm dd/MM/yyyy" tại UTC+07:00, không phụ thuộc múi giờ máy.</summary>
public sealed record WorkProgressStep(
    WorkStep Step, WorkStepState State, string TimeText, string ActorText);

/// <summary>Kết quả thuần: đúng chín bước; LatestError là nội dung lỗi đang hiệu lực hoặc null.
/// IsMerged chỉ true khi có Merge đạt hoặc boardStatus chứa ✅; không suy diễn đã nghiệm thu.</summary>
public sealed record WorkProgress(
    WorkStep CurrentStep, IReadOnlyList<WorkProgressStep> Steps, string? LatestError, bool IsMerged);

/// <summary>Parser thuần, không I/O, không chạy lệnh, không trạng thái dùng chung; không đăng ký DI.</summary>
public static class WorkProgressParser
{
    /// <summary>
    /// null/rỗng trả danh sách rỗng. Chấp nhận BOM đầu chuỗi, CRLF/LF/CR.
    /// Chỉ đọc mục heading cấp 2 có "Nhật ký" (kể cả "## 3. Nhật ký (…)"), dừng ở heading cấp 2 kế tiếp.
    /// Bảng cũ | Giờ | Sự kiện | Ghi chú |: bỏ header, separator, ô sự kiện rỗng;
    /// dòng không xác định task/thao tác vẫn giữ Text, Unknown, Round=0, không đoán kết quả từ văn xuôi.
    /// Workflow: "- giờ | task | Prep/Assign/Verify/Qc/Merge | lượt N | kết quả &lt;!-- workflow:… --&gt;".
    /// Task phải khớp ^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9-]*$, lượt là số nguyên dương.
    /// Sai số cột, task, lượt hoặc thao tác: bỏ dòng; sai/thiếu giờ: vẫn giữ sự kiện với Time=null.
    /// Giờ ISO có offset giữ nguyên; "HH:mm dd/MM/yyyy" dùng +07:00; chỉ HH:mm dùng ngày receivedAt
    /// tại +07:00, không có receivedAt thì null. Actor=null nếu nguồn không chỉ rõ người.
    /// ĐẠT/PASS/PASSED (nguyên từ đầu kết quả) → Passed; CẦN SỬA/FAIL/FAILED/LỖI → Failed;
    /// BLOCKED → Blocked; còn lại Unknown. Không coi "KHÔNG ĐẠT" là Passed.
    /// Loại dòng trùng hoàn toàn sau Trim; giữ thứ tự nguồn. Không đọc marker như lệnh hay HTML.
    /// </summary>
    public static IReadOnlyList<WorkProgressEvent> Parse(string? markdown, DateTimeOffset? receivedAt = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <summary>
    /// Trả đúng 9 bước theo enum; mặc định Received=Current, còn lại Waiting. planExists chuyển sang PlanApproval (Received/Plan Done);
    /// planApproved chuyển sang Prep (Received/Plan/PlanApproval Done); chỉ đánh dấu Done những bước có bằng chứng, không điền người/giờ giả.
    /// Lọc đúng TaskId (OrdinalIgnoreCase), bỏ Unknown/round&lt;1; lấy lượt lớn nhất theo SỐ (10 sau 2).
    /// Trong lượt: sự kiện có giờ xếp trước theo Time (so UTC); hòa giờ giữ thứ tự nguồn; thiếu giờ xếp sau theo thứ tự nguồn.
    /// Prep đạt → Prep Done, Code Current; Assign → Code Current (chưa chứng minh Code xong).
    /// Verify đạt → Verify Done, QC Current; QC đạt → QC Done, Acceptance Current.
    /// Lỗi/BLOCKED giữ chính bước Prep/Code/Verify/QC/Merge ở Failed, LatestError=Text;
    /// thành công sau đó cùng bước xóa lỗi đó. Assign tổng kết không che Verify/QC lỗi cùng lượt.
    /// Gate lỗi còn hiệu lực chặn tiến tiếp, trừ Merge đạt mới hơn: Merge Done, CurrentStep=Merge,
    /// IsMerged=true, LatestError=null. boardStatus chứa ✅ cũng chứng minh Merge xong, ưu tiên lỗi cũ.
    /// 🟡/🔵/🔴 không tự chứng minh gate đạt; không có log thì không suy ra Verify/QC/Acceptance Done.
    /// QC đạt chỉ CHỜ nghiệm thu, tuyệt đối không tự đánh dấu Acceptance Done kể cả Merge/✅.
    /// Chuỗi hiển thị thiếu giờ/người là "Chưa có dữ liệu"; giờ rõ đổi +07:00 định dạng HH:mm dd/MM/yyyy.
    /// null events coi rỗng. Không đổi đầu vào, không I/O, không suy luận từ đồng hồ hoặc máy hiện tại.
    /// </summary>
    public static WorkProgress Calculate(
        string taskId, IReadOnlyList<WorkProgressEvent>? events,
        bool planExists = false, bool planApproved = false, string? boardStatus = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}