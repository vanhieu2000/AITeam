namespace AITeam.Hub.Models;

// HỢP ĐỒNG "Cần anh duyệt" (R-003 / H3-03) — Claude thiết kế, agent KHÔNG đổi. Thiết kế: docs/design/HUB-V3.md mục 3.
// File: <AITeam>/approvals/A-001.json do team.ps1 (Claude) tạo. Hub CHỈ ghi 3 trường status / decidedAt / note,
// giữ nguyên mọi trường khác (kể cả trường lạ). Hub KHÔNG tự chạy merge hay lệnh hệ thống.
//
// Mẫu JSON (tên trường camelCase, enum chữ thường):
// { "id": "A-001", "kind": "merge", "title": "Merge H3-01 …", "detail": "markdown",
//   "project": "AITeam", "task": "H3-01", "commit": "<sha nhánh lúc xin duyệt>", "links": ["tasks/H3-01-….md"],
//   "requestedBy": "Claude", "requestedAt": "2026-10-24T01:30:00", "status": "pending", "decidedAt": null, "note": null }

// V10-03 (R-014): chủ dự án chỉ duyệt việc nhạy cảm; merge / QC / cải tiến do Claude tự quyết + ghi nhật ký.
// JSON kind: merge | decision | improvement | secret | security | sensitive-data | external.
public enum ApprovalKind { Merge, Decision, Improvement, Secret, Security, SensitiveData, External }

public enum ApprovalStatus { Pending, Approved, Rejected }

// Mức rủi ro (JSON: low | medium | high). Thiếu / giá trị lạ ⇒ Medium để đọc được file cũ.
public enum ApprovalRisk { Low, Medium, High }

public static class ApprovalPolicy
{
    /// <summary>Chủ dự án phải duyệt: bí mật, bảo mật, dữ liệu nhạy cảm, hành động ra ngoài, quyết định sản phẩm.</summary>
    public static bool RequiresOwner(ApprovalKind kind) => kind is
        ApprovalKind.Secret or ApprovalKind.Security or ApprovalKind.SensitiveData or ApprovalKind.External or ApprovalKind.Decision;
}

public sealed record Approval(
    string Id,                 // "A-001"
    ApprovalKind Kind,
    string Title,
    string Detail,             // markdown
    string Project,
    string? Task,
    string? Commit,            // merge: commit nhánh lúc xin duyệt; team.ps1 merge từ chối nếu nhánh đã đổi
    IReadOnlyList<string> Links,
    string RequestedBy,
    DateTime RequestedAt,
    ApprovalStatus Status,
    DateTime? DecidedAt,
    string? Note)
{
    public ApprovalRisk Risk { get; init; } = ApprovalRisk.Medium;

    /// <summary>Chỉ TÊN khóa trong két (^[A-Z][A-Z0-9_]{1,63}$); sai mẫu ⇒ null. KHÔNG BAO GIỜ chứa giá trị khóa.</summary>
    public string? SecretName { get; init; }

    /// <summary>File nhạy cảm do cổng kiểm tra phát hiện trong diff (rỗng với file cũ).</summary>
    public IReadOnlyList<string> Files { get; init; } = [];
}

// ── Nhật ký "Claude tự quyết" (approvals/claude-decisions.jsonl, chỉ thêm dòng) ──

public sealed record ClaudeDecision(
    string Id, DateTimeOffset At, string Kind, string Title,
    string? Project, string? Task, string? Request, string? Detail,
    bool Reopened, string? ReopenNote, DateTimeOffset? ReopenedAt);

public interface IClaudeDecisionLog
{
    /// <summary>Gộp dòng quyết định + dòng mở lại; mới nhất trước. Dòng hỏng bị bỏ qua.</summary>
    IReadOnlyList<ClaudeDecision> List(int max = 200);

    /// <summary>id sai mẫu ^D-\d{14}-[a-z0-9]{4}$ ⇒ ArgumentException; note rỗng ⇒ ArgumentException;
    /// không có ⇒ KeyNotFoundException. Ghi thêm dòng reopen (append-only), trả quyết định đã mở lại.</summary>
    ClaudeDecision Reopen(string id, string note);
}
