using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public interface IApprovalService
{
    /// <summary>Mọi approvals/A-*.json đọc được (file hỏng / thiếu id / id không khớp tên file ⇒ bỏ qua).
    /// Sắp xếp: Pending trước; trong mỗi nhóm RequestedAt mới nhất trước. Thư mục không có ⇒ rỗng.</summary>
    IReadOnlyList<Approval> GetAll();

    /// <summary>Theo id (phải khớp ^A-\d{3}$, sai ⇒ null). Không có / hỏng ⇒ null.</summary>
    Approval? Get(string id);

    /// <summary>
    /// Ghi quyết định. Lỗi:
    /// - id sai mẫu ^A-\d{3}$ ⇒ ArgumentException (chặn "..\x", "A-001.json", "A-1");
    /// - không có file ⇒ KeyNotFoundException;
    /// - Status khác Pending ⇒ InvalidOperationException (không đổi quyết định đã ghi);
    /// - note dài hơn 500 ký tự ⇒ ArgumentException.
    /// Ghi: status = "approved" / "rejected", decidedAt = giờ địa phương "yyyy-MM-ddTHH:mm:ss", note = note.Trim() (rỗng ⇒ null).
    /// Mọi trường khác giữ nguyên (sửa bằng JsonNode, không serialize lại từ record).
    /// Ghi file tạm cùng thư mục rồi File.Move(overwrite: true); UTF-8 không BOM; thụt lề 2 dấu cách.
    /// Trả Approval sau khi ghi.
    /// </summary>
    Approval Decide(string id, bool approve, string? note);
}

/// <summary>Đọc / ghi &lt;AITeamRoot&gt;/approvals/*.json. Enum đọc không phân biệt hoa thường; giá trị lạ ⇒ bỏ qua file.</summary>
public sealed class ApprovalService : IApprovalService
{
    private readonly HubPaths _paths;

    public ApprovalService(HubPaths paths) => _paths = paths;

    public string Folder => Path.Combine(_paths.AITeamRoot, "approvals");

    public IReadOnlyList<Approval> GetAll()
        => throw new NotImplementedException(); // TODO(agent)

    public Approval? Get(string id)
        => throw new NotImplementedException(); // TODO(agent)

    public Approval Decide(string id, bool approve, string? note)
        => throw new NotImplementedException(); // TODO(agent)
}
