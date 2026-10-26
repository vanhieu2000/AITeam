namespace AITeam.Core.Review;

/// <summary>Quyết định phân vai; ProviderId null với vai thủ công hoặc QC cũ; IsExplicit chỉ đúng khi có roles hợp lệ.</summary>
public sealed record WorkflowAssignment(string Role, string Actor, string? ProviderId, bool IsExplicit, bool ReadOnly, bool Manual);

/// <summary>Đọc Map dùng chung cho CLI và điều phối; không ghi cấu hình và không gọi tiến trình.</summary>
public static class WorkflowRoleDispatch
{
    /// <summary>
    /// Chỉ nhận role plan/prep/qc (Ordinal), ngoài ra ArgumentException("Vai trò không được hỗ trợ.").
    /// Đọc teamFile UTF-8 có/không BOM; không theo reparse point của file hoặc tổ tiên.
    /// Thiếu file/workflow/roles: plan/prep = codex, QC ProviderId=null để dùng router cũ; IsExplicit=false.
    /// Có roles: kiểm schema H7-01 đủ 11 mã duy nhất, actor hợp lệ, note chuỗi tối đa 500 UTF-16.
    /// Ánh xạ cố định codex→codex, minh/lan→agy; claude/gate/owner cần thủ công, không tự chạy Claude CLI.
    /// ReadOnly=true với plan/qc, false với prep. IsExplicit=true khi có roles hợp lệ.
    /// Schema sai ném InvalidDataException("Phân vai không hợp lệ."); JSON hỏng/root không object/lỗi đọc/
    /// reparse point ném IOException("Không đọc được cấu hình phân vai."). Không phản chiếu giá trị ngoài vào lỗi.
    /// </summary>
    public static WorkflowAssignment Resolve(string teamFile, string role)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
