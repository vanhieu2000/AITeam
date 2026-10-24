# Bảng công việc · AITeam (AITeam Hub)

Quy trình:
1. Claude thiết kế (`docs/HUB-DESIGN.md`) và viết task.
2. Agent làm trên nhánh riêng và push.
3. Claude review và test.
4. Đạt thì merge vào `main`, chưa đạt thì trả về sửa.

Hướng dẫn cho agent: [AGENTS.md](../AGENTS.md).

| Task | Agent | Nhánh | Trạng thái |
|---|---|---|---|
| [H-001 Hub: các dịch vụ dữ liệu](H-001-hub-services.md) | A | `agent-a/H-001-hub-services` | ✅ Đã merge (vòng 2) |
| [H-002 Hub: giao diện khung + 3 trang](H-002-hub-ui.md) | B | `agent-b/H-002-hub-ui` | ✅ Đã merge (vòng 1) |
| [H-003 Hub: menu dự án động, Agent, Hộp thư](H-003-hub-agents-inbox.md) | B | `agent-b/H-003-hub-agents-inbox` | ✅ Đã merge (vòng 1) |
| [H-004 Hub: Cài đặt, Tài khoản, Bí mật, team.ps1 hub](H-004-hub-settings-accounts-secrets.md) | A | `agent-a/H-004-hub-settings` | ✅ Đã merge (vòng 1) |
| [H-006 Hub v2: khung phẳng + Tổng quan](H-006-hub-v2-shell-home.md) | A | `agent-a/H-006-hub-v2-shell-home` | ✅ Đã merge (vòng 1) |
| [H-007 Hub v2: Dự án + Task](H-007-hub-v2-project-task.md) | A | `agent-a/H-007-hub-v2-project-task` | ✅ Đã merge (vòng 1, Claude hoàn tất) |
| [H-008 Hub v2: Agent, Hộp thư, Tài khoản, Bí mật, Cài đặt](H-008-hub-v2-agents-inbox-secrets-settings.md) | A | `agent-a/H-008-hub-v2-other-pages` | ✅ Đã merge (vòng 1, Claude hoàn tất) |
| [V4-03 Codex review chỉ đọc](V4-03-codex-review.md) | A | `agent-a/V4-03-codex-review` | ✅ Đã merge (vòng 1) |
| [V4-06 Trạng thái job + phục hồi](V4-06-job-state.md) | B | `agent-b/V4-06-job-state` | ✅ Đã merge (vòng 1) |
| [H3-01 Hub v3: dịch vụ dữ liệu đánh giá team](H3-01-hub-insights-data.md) | A | `agent-a/H3-01-hub-insights-data` | ✅ Đã merge (vòng 1) |
| [H3-03 Hub v3: trang Cần anh duyệt](H3-03-hub-approvals.md) | B | `agent-b/H3-03-hub-approvals` | ✅ Đã merge (vòng 1) |
| [H3-02 Hub v3: trang Tổng quan (chỉ số, biểu đồ thời gian, nhiệm vụ)](H3-02-hub-overview-v3.md) | B | `agent-b/H3-02-hub-overview-v3` | ✅ Đã merge (vòng 1) |
| [H3-04 Hub v3: trang Đội ngũ + Quy trình & cải tiến](H3-04-hub-team-process.md) | A | `agent-a/H3-04-hub-team-process` | ✅ Đã merge (vòng 1) |
| [H3-05 Hub v3: trang Giao việc + tab Dòng thời gian / Đánh giá quy trình](H3-05-hub-requests-task-tabs.md) | A | `agent-a/H3-05-hub-requests-task-tabs` | ✅ Đã merge (vòng 1) |
| [V4-07 Định tuyến provider (cấm tự duyệt, nghỉ khi hết hạn mức)](V4-07-provider-routing.md) | A | `agent-a/V4-07-provider-routing` | ✅ Đã merge (vòng 1) |
| [V4-08 Lệnh aiteam review (chọn reviewer, thử reviewer kế, mã thoát)](V4-08-aiteam-review-cli.md) | B | `agent-b/V4-08-aiteam-review-cli` | ✅ Đã merge (vòng 1) |
| [V4-09 Lệnh aiteam run (Codex viết code dự phòng)](V4-09-aiteam-run-dev.md) | A | `agent-a/V4-09-aiteam-run-dev` | ✅ Đã merge (vòng 1) |
| [H3-06 Hub: trang Provider](H3-06-hub-providers.md) | B | `agent-b/H3-06-hub-providers` | ✅ Đã merge (vòng 1) |
| [V4-10 aiteam providers hiện trạng thái nghỉ (Codex làm)](V4-10-providers-rest-status.md) | C | `agent-c/V4-10-providers-rest-status` | ✅ Đã merge (vòng 1) |
| [V4-11 ReviewService: tên file riêng mỗi lần review (Codex làm)](V4-11-review-unique-files.md) | C | `agent-c/V4-11-review-unique-files` | ✅ Đã merge (vòng 1) |
| [V5-01 MCP Gateway: aiteam mcp](V5-01-mcp-gateway.md) | A | `agent-a/V5-01-mcp-gateway` | 🟡 Đang chạy · máy DESKTOP-PP14EFS · 01:36 25/10/2026 |
| [V5-04 Hub: trang Văn phòng](V5-04-hub-office.md) | B | `agent-b/V5-04-hub-office` | 🟡 Đang chạy · máy DESKTOP-PP14EFS · 01:32 25/10/2026 |

Trạng thái: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge

## Kế hoạch / hàng chờ
Theo `docs/HUB-DESIGN.md` mục 7:
- **H-003 (B):** nối trang với dịch vụ thật; trang Agent, Hộp thư. Sau H-001 và H-002.
- **H-004 (A):** trang Cài đặt, Tài khoản, Bí mật; lệnh `team.ps1 hub`. Sau H-001.
- **H-005 (B):** module "AITeam Hub" trong HDV Tool (WebView2), làm ở dự án HDVTool.
