namespace AITeam.Hub.Services;

/// <summary>Một dòng phân vai. Code và Actor phân biệt hoa thường; Note không null, tối đa 500 đơn vị UTF-16, giữ nguyên văn.</summary>
public sealed record WorkflowRole(string Code, string Actor, string Note);

/// <summary>11 dòng theo thứ tự chuẩn; Revision = SHA-256 hex toàn bộ byte file, hoặc "missing" nếu chưa có file.</summary>
public sealed record WorkflowRolesSnapshot(IReadOnlyList<WorkflowRole> Roles, string Revision);

/// <summary>Hợp đồng riêng cho Map; không thay Contracts.cs hoặc HubModels.cs.</summary>
public interface IWorkflowRolesService
{
    /// <summary>
    /// Không ghi file. Thiếu file/workflow/roles trả mặc định v4; null/rỗng/sai kiểu không phải thiếu.
    /// Mã theo thứ tự: receive, idea, research, plan, approve_plan, prep, code, gate, qc, accept, publish.
    /// Tác nhân: claude, codex, minh, lan, gate (Cổng KT), owner (Chủ dự án).
    /// Mặc định claude: receive/idea/approve_plan/accept/publish; codex: research/plan/prep/qc;
    /// minh: code; gate: gate; Note = "". JSON roles là mảng object {code, actor, note} chữ thường.
    /// Sai dữ liệu ném InvalidDataException("Phân vai không hợp lệ."). JSON hỏng, root không là object,
    /// lỗi đọc hoặc reparse point ném IOException("Không đọc được cấu hình phân vai.").
    /// Không đưa dữ liệu gốc vào thông báo. Không theo reparse point của file hoặc thư mục tổ tiên.
    /// </summary>
    WorkflowRolesSnapshot GetRoles();

    /// <summary>
    /// Đủ 11 mã duy nhất và tác nhân hợp lệ; Note không null, Length &lt;= 500. roles null, dòng null,
    /// mã lạ/trùng/thiếu hoặc revision null/rỗng ném InvalidDataException("Phân vai không hợp lệ.").
    /// Chỉ cập nhật workflow.roles; giữ khóa lạ kể cả workflow.*, giữ BOM; file mới không BOM.
    /// Dùng khóa chung theo đường dẫn chuẩn hóa với SaveSettings, đọc lại trong khóa.
    /// Revision khác byte hiện tại ném InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.").
    /// JSON hỏng ném IOException("Không đọc được cấu hình phân vai."). Ghi tạm tên duy nhất cùng thư mục,
    /// flush rồi kiểm tra lại revision và thay nguyên tử (file mới move không ghi đè).
    /// Lỗi ghi/quyền/reparse point ném IOException("Không lưu được cấu hình phân vai."); giữ file cũ, dọn tạm.
    /// Không theo reparse point của file/thư mục tổ tiên. Trả snapshot mới. Không ghi team.local.json.
    /// </summary>
    WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision);
}

/// <summary>Singleton; mọi instance cùng file phối hợp với đường ghi Settings.</summary>
public sealed class WorkflowRolesService(HubPaths paths) : IWorkflowRolesService
{
    /// <inheritdoc />
    public WorkflowRolesSnapshot GetRoles()
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <inheritdoc />
    public WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}