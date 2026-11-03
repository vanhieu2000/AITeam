using AITeam.Hub.Models;
using AITeam.Hub.Services;
namespace AITeam.Hub.DevData;

/// <summary>Đơn minh họa chỉ ở bộ nhớ; không đọc/ghi approvals thật khi HUB_FAKE.</summary>
public sealed class FakeApprovalService : IApprovalService
{
    private readonly object gate = new();
    private readonly List<Approval> approvals =
    [
        new("A-901", ApprovalKind.Merge, "[Dữ liệu mẫu] Duyệt bản sửa giao diện", "Đơn minh họa, bấm duyệt không merge hay chạy lệnh.", "AITeam", "H-mẫu", null, [], "Dữ liệu mẫu", new DateTime(2026,11,3,9,0,0), ApprovalStatus.Pending, null, null),
        new("A-902", ApprovalKind.Decision, "[Dữ liệu mẫu] Chọn cách xử lý task bị chặn", "Đơn minh họa; quyết định chỉ được giữ trong phiên Hub này.", "AITeam", null, null, [], "Dữ liệu mẫu", new DateTime(2026,11,3,9,5,0), ApprovalStatus.Pending, null, null)
    ];
    public IReadOnlyList<Approval> GetAll() { lock (gate) return approvals.OrderBy(a => a.Status == ApprovalStatus.Pending ? 0 : 1).ThenByDescending(a => a.RequestedAt).ToList(); }
    public Approval? Get(string id) { lock (gate) return approvals.FirstOrDefault(a => a.Id == id); }
    public Approval Decide(string id, bool approve, string? note)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^A-\d{3}$")) throw new ArgumentException("Mã đơn duyệt không hợp lệ.");
        if (note?.Length > 500) throw new ArgumentException("Ghi chú tối đa 500 ký tự.");
        lock (gate)
        {
            var index = approvals.FindIndex(a => a.Id == id);
            if (index < 0) throw new KeyNotFoundException("Không tìm thấy đơn duyệt mẫu.");
            if (approvals[index].Status != ApprovalStatus.Pending) throw new InvalidOperationException("Đơn mẫu đã có quyết định.");
            return approvals[index] = approvals[index] with { Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected, DecidedAt = DateTime.Now, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim() };
        }
    }
}
