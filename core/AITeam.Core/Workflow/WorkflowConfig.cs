namespace AITeam.Core.Workflow;

/// <summary>Một bước trong quy trình của một loại việc. Code duy nhất trong loại việc; Fallback khác Owner và có thể null.</summary>
public sealed record WorkflowStep(
    string Code,
    string Name,
    string Owner,
    string? Fallback,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Rules,
    int? MaxMinutes,
    int? MaxRounds,
    bool OwnerApproval);

/// <summary>Một loại việc với danh sách bước theo thứ tự. CodePrefixes dùng suy loại việc từ mã task.</summary>
public sealed record TaskType(
    string Id,
    string Name,
    IReadOnlyList<string> CodePrefixes,
    IReadOnlyList<WorkflowStep> Steps);

/// <summary>Toàn bộ cấu hình quy trình. Revision = SHA-256 hex nội dung file; rỗng khi dùng cấu hình mặc định.</summary>
public sealed record WorkflowConfig(int Version, IReadOnlyList<TaskType> TaskTypes, string Revision);

/// <summary>Kết quả kiểm tra cấu hình; lỗi bằng tiếng Việt, ghi rõ loại việc / bước.</summary>
public sealed record WorkflowValidation(bool Ok, IReadOnlyList<string> Errors);

/// <summary>Hợp đồng đọc / ghi / kiểm tra config/workflow.json (R-014).</summary>
public interface IWorkflowConfigService
{
    /// <summary>Không có file ⇒ cấu hình mặc định (6 loại), không ghi file. File có mà hỏng ⇒ InvalidDataException.</summary>
    WorkflowConfig Load();

    /// <summary>Trả toàn bộ lỗi tìm thấy; Ok = không có lỗi.</summary>
    WorkflowValidation Validate(WorkflowConfig config);

    /// <summary>Validate lỗi ⇒ ArgumentException; Revision lệch ⇒ InvalidOperationException. Ghi tạm rồi thay nguyên tử.</summary>
    WorkflowConfig Save(WorkflowConfig config, string expectedRevision);

    /// <summary>
    /// Dòng "- **Loại việc:** &lt;Tên hoặc id&gt;" trong file task; không có ⇒ theo CodePrefixes của mã task;
    /// không khớp ⇒ "chua-phan-loai" (Steps lấy của "tinh-nang-loi").
    /// </summary>
    TaskType Resolve(string taskId, string? taskText);

    /// <summary>Tên thư mục trong skills/, sắp xếp ordinal.</summary>
    IReadOnlyList<string> KnownSkills();
}
