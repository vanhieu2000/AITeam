using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Bảy mốc theo thứ tự quy trình.</summary>
public enum WorkflowStage { Intake = 1, Idea, Plan, Preparation, Code, Quality, Acceptance }
/// <summary>Chưa có bằng chứng; đang làm; hoàn tất; cần sửa.</summary>
public enum WorkflowStatus { Pending, Running, Done, NeedsFix }
/// <summary>Quyết định đã chuẩn hóa, danh sách cũ trước mới sau; phần tử cuối mỗi mốc thắng.</summary>
public sealed record WorkflowDecision(WorkflowStage Stage, bool Approved, bool IsFinal, string Evidence);
/// <summary>Vòng là số nguyên dương; chọn số lớn nhất, cùng vòng chọn phần tử cuối.</summary>
public sealed record WorkflowReview(int Round, bool Passed, string Evidence);
/// <summary>Snapshot task; Prepared nghĩa là đã giao khung; null Reviews/Logs tương đương rỗng.</summary>
public sealed record WorkflowTaskSnapshot(string Id, string Owner, TaskState State, bool Prepared,
    IReadOnlyList<WorkflowReview>? Reviews = null, IReadOnlyList<AgentRun>? Logs = null);
/// <summary>Snapshot trong bộ nhớ; chuỗi null/trắng là thiếu, danh sách null là rỗng.</summary>
public sealed record WorkflowSnapshot(string? Brief = null, string? Plan = null,
    IReadOnlyList<WorkflowDecision>? Decisions = null, IReadOnlyList<WorkflowTaskSnapshot>? Tasks = null);
/// <summary>Mốc có tên tiếng Việt, người phụ trách và bằng chứng văn bản thuần, không phải HTML để thực thi.</summary>
public sealed record WorkflowStep(WorkflowStage Stage, string Name, WorkflowStatus Status,
    string Owner, IReadOnlyList<string> Evidence);
/// <summary>Tiến độ riêng: đúng ba mốc Chuẩn bị, Code, QC theo thứ tự.</summary>
public sealed record WorkflowTaskProgress(string Id, IReadOnlyList<WorkflowStep> Steps);
/// <summary>Tổng phiếu có bảy mốc; Tasks xếp Id theo Ordinal.</summary>
public sealed record WorkflowResult(IReadOnlyList<WorkflowStep> Steps, IReadOnlyList<WorkflowTaskProgress> Tasks);

/// <summary>Hàm static thuần, không phải dịch vụ Hub, không cần DI.</summary>
public static class WorkflowBuilder
{
    /// <summary>
    /// Null snapshot tương đương rỗng; luôn trả Tiếp nhận, Ý tưởng, Plan, Chuẩn bị, Code, QC, Nghiệm thu.
    /// Tiếp nhận Done khi Brief không trắng, bằng chứng Brief nguyên văn. Ý tưởng theo quyết định Idea cuối:
    /// Approved là Done, từ chối NeedsFix, thiếu Pending. Plan từ chối NeedsFix; Done cần Plan không trắng
    /// và quyết định Plan Approved; còn lại Pending. Bằng chứng Plan gồm Plan và quyết định cuối nếu có.
    /// Chuẩn bị task Done nếu Prepared hoặc Merged, còn lại Pending; khi Done bằng chứng là Id.
    /// Code: Merged Done; NeedsFix hoặc review cuối thất bại là NeedsFix; sau đó Review Done;
    /// còn lại lấy log đúng Task có Attempt lớn nhất (cùng số lấy cuối): Running/Success/Error
    /// lần lượt Running/Done/NeedsFix; thiếu log Pending. Không dùng thời gian để chọn vòng.
    /// QC: Merged Done; TaskState.NeedsFix Pending; review cuối Passed Done, thất bại NeedsFix;
    /// thiếu review thì Review Running, còn lại Pending. Review cuối lấy Round số lớn nhất.
    /// Merged cũ thiếu log/review vẫn Done cả ba mốc, Code/QC có bằng chứng "Đã merge: {Id}".
    /// Code có bằng chứng LogPath được chọn và Evidence review được chọn; QC có Evidence review được chọn.
    /// Tổng mỗi mốc task: không task Pending; tất cả Done mới Done; có NeedsFix thì NeedsFix;
    /// còn lại có trạng thái khác Pending thì Running, tất cả Pending thì Pending.
    /// Nghiệm thu chỉ theo quyết định Acceptance cuối: thiếu/chưa IsFinal là Pending;
    /// đã chốt Approved Done, từ chối NeedsFix, không suy từ PASS/merge. Bằng chứng là quyết định cuối.
    /// Owner mốc 1/2/3/6/7 là Claude, mốc 4 Codex, mốc 5 Owner task;
    /// tổng Code nối Owner khác trắng, duy nhất, xếp Ordinal bằng ", ".
    /// Mọi Evidence bỏ chuỗi trắng, bỏ trùng, xếp Ordinal; tổng mốc task hợp bằng chứng mốc tương ứng.
    /// Không sửa đầu vào, không mở đường dẫn, diễn giải HTML, đọc đĩa/mạng/đồng hồ hay chạy tiến trình.
    /// Dữ liệu thiếu nêu trên không gây lỗi nghiệp vụ; không có câu báo lỗi cho người dùng.
    /// Khung chưa triển khai ném NotImplementedException.
    /// </summary>
    public static WorkflowResult Build(WorkflowSnapshot? snapshot)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
