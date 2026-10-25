using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Log chỉ đọc, Name là tên file an toàn; Content nguyên văn, không suy diễn kết quả gate.</summary>
public sealed record WorkProgressLog(string Name, string Content);

/// <summary>Task nối bằng metadata Yêu cầu; Agent từ metadata Agent; Progress theo W1-05.</summary>
public sealed record WorkTaskProgress(string Id, string Title, string Agent, WorkProgress Progress,
    IReadOnlyList<WorkProgressLog> Logs);

/// <summary>Yêu cầu luôn hiện kể cả chưa có task. StepCounts đủ chín khóa, đếm task theo CurrentStep.
/// Progress lấy task chưa merge đầu tiên theo Id, hoặc task đầu nếu tất cả merge;
/// khi chưa có task tính bằng plan và nhật ký yêu cầu.</summary>
public sealed record WorkRequestProgress(string Id, string Title, WorkProgress Progress,
    IReadOnlyList<WorkTaskProgress> Tasks, IReadOnlyDictionary<WorkStep, int> StepCounts);

/// <summary>Snapshot mới mỗi lần đọc; Requests, Tasks và UnlinkedTasks xếp Id OrdinalIgnoreCase.</summary>
public sealed record WorkProjectProgress(ProjectInfo Project, IReadOnlyList<WorkRequestProgress> Requests,
    IReadOnlyList<WorkTaskProgress> UnlinkedTasks);

/// <summary>Hợp đồng riêng của trang Việc, không thay Contracts.cs hoặc HubModels.cs.</summary>
public interface IWorkProgressService
{
    /// <summary>
    /// Lấy dự án bằng ITeamConfigService.FindProject; không dùng tên dự án làm đường dẫn.
    /// Đọc requests/R-xxx-*.md, plans/R-xxx/plan.md, tasks/*.md tại Project.Path;
    /// logs/&lt;task&gt;-verify-N.md và logs/&lt;task&gt;.review-N.json chỉ tại Project.WorkDir.
    /// Không đòi .git; thiếu thư mục con trả rỗng, thiếu repo là lỗi nguồn.
    /// Nối bằng đúng metadata '- **Yêu cầu:** R-xxx'; thiếu liên kết hoặc phiếu không tồn tại vào UnlinkedTasks.
    /// Tiêu đề lấy heading đầu, bỏ Id và dấu ·. Mã lấy tên file hợp lệ, không lấy đường dẫn từ văn xuôi.
    /// PlanApproved khi có 'ĐÃ DUYỆT', loại dòng chứa 'CHƯA ĐÃ DUYỆT'; plan mất là chưa có bằng chứng.
    /// Dùng WorkProgressParser.Parse/Calculate và boardStatus đúng dòng task ở tasks/README.md.
    /// ReceivedAt từ Nhận lúc (UTC+07); thiếu giờ giữ 'Chưa có dữ liệu'.
    /// Actor lấy IWorkflowRolesService.GetRoles: receive/plan/approve_plan/prep/code/gate/qc/accept/publish.
    /// Code ưu tiên DeveloperInfo.Name có Id khớp Agent; còn lại tra Developers rồi tên chuẩn:
    /// claude=Claude, codex=Codex, minh=Minh, lan=Lan, gate=Cổng KT, owner=Chủ dự án.
    /// Chỉ gán ActorText cho bước Done/Failed/Current; không bịa giờ hoặc gán người cho Waiting.
    /// Log thiếu trả Logs rỗng; log hiện có phải UTF-8 hợp lệ, review là JSON object.
    /// Logs xếp Name OrdinalIgnoreCase; giữ nguyên Content, không đoán schema/kết quả log ngoài W1-05.
    /// null/rỗng/tên dự án không cấu hình: InvalidDataException('Việc không hợp lệ.').
    /// Lỗi nguồn (UTF-8/JSON hỏng, lỗi đọc, mã/link sai, trùng Id, reparse point, vượt giới hạn)
    /// ném IOException('Không đọc được tiến độ công việc. Nguồn: &lt;tên an toàn&gt;').
    /// Tên an toàn chỉ tên file hoặc requests/plans/tasks/logs/cấu hình; không lộ đường dẫn,
    /// nội dung hoặc InnerException chứa bí mật. Chặn ../đường dẫn tuyệt đối trong metadata.
    /// Từ chối reparse point ở file, thư mục nguồn và mọi tổ tiên đến gốc.
    /// Giới hạn mỗi file 2 MiB, mỗi thư mục nguồn 2000 file; không chạy tiến trình, ghi/migrate/tạo thư mục.
    /// Nhiều lời gọi đồng thời phải độc lập, không lẫn dữ liệu dự án, không cache cũ.
    /// </summary>
    WorkProjectProgress GetProject(string project);

    /// <summary>Đọc lại theo GetProject. requestId khớp ^R-\d+$, không phân biệt hoa thường.
    /// Mã sai hoặc không có phiếu: InvalidDataException('Việc không hợp lệ.'); không đọc đường dẫn người dùng.</summary>
    WorkRequestProgress GetRequest(string project, string requestId);
}

/// <summary>Dịch vụ chỉ đọc, nhận HubPaths để kiểm thử trong thư mục tạm.</summary>
public sealed class WorkProgressService(HubPaths paths, ITeamConfigService config, IWorkflowRolesService roles) : IWorkProgressService
{
    /// <inheritdoc />
    public WorkProjectProgress GetProject(string project)
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <inheritdoc />
    public WorkRequestProgress GetRequest(string project, string requestId)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
