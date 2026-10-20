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

Trạng thái: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge

## Kế hoạch / hàng chờ
Theo `docs/HUB-DESIGN.md` mục 7:
- **H-003 (B):** nối trang với dịch vụ thật; trang Agent, Hộp thư. Sau H-001 và H-002.
- **H-004 (A):** trang Cài đặt, Tài khoản, Bí mật; lệnh `team.ps1 hub`. Sau H-001.
- **H-005 (B):** module "AITeam Hub" trong HDV Tool (WebView2), làm ở dự án HDVTool.
