namespace AITeam.Hub.Models;

// HỢP ĐỒNG "Cần anh duyệt" (R-003 / H3-03) — Claude thiết kế, agent KHÔNG đổi. Thiết kế: docs/design/HUB-V3.md mục 3.
// File: <AITeam>/approvals/A-001.json do team.ps1 (Claude) tạo. Hub CHỈ ghi 3 trường status / decidedAt / note,
// giữ nguyên mọi trường khác (kể cả trường lạ). Hub KHÔNG tự chạy merge hay lệnh hệ thống.
//
// Mẫu JSON (tên trường camelCase, enum chữ thường):
// { "id": "A-001", "kind": "merge", "title": "Merge H3-01 …", "detail": "markdown",
//   "project": "AITeam", "task": "H3-01", "commit": "<sha nhánh lúc xin duyệt>", "links": ["tasks/H3-01-….md"],
//   "requestedBy": "Claude", "requestedAt": "2026-10-24T01:30:00", "status": "pending", "decidedAt": null, "note": null }

public enum ApprovalKind { Merge, Decision, Improvement }

public enum ApprovalStatus { Pending, Approved, Rejected }

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
    string? Note);
