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
| [V5-01 MCP Gateway: aiteam mcp](V5-01-mcp-gateway.md) | A | `agent-a/V5-01-mcp-gateway` | ✅ Đã merge (vòng 1) |
| [V5-04 Hub: trang Văn phòng](V5-04-hub-office.md) | B | `agent-b/V5-04-hub-office` | ✅ Đã merge (vòng 1) |
| [V5-05 Hub: nút điều khiển Văn phòng](V5-05-hub-control.md) | B | `agent-b/V5-05-hub-control` | ✅ Đã merge (vòng 1) |
| [V5-06 Tài khoản AI](V5-06-ai-accounts.md) | A | `agent-a/V5-06-ai-accounts` | ✅ Đã merge (vòng 1) |
| [V5-07a Phòng nghiên cứu (lõi)](V5-07a-research-core.md) | A | `agent-a/V5-07a-research-core` | ✅ Đã merge (vòng 1) |
| [V5-07b Hub: Phòng nghiên cứu](V5-07b-research-page.md) | B | `agent-b/V5-07b-research-page` | ✅ Đã merge (vòng 1) |
| [V6-01 Đếm vòng review và CLI](V6-01-dem-vong-review-va-cli.md) | A | `agent-a/V6-01-dem-vong-review-va-cli` | ✅ Đã merge (vòng 1) |
| [H6-07 Hiệu suất và Nhân viên](H6-07-hieu-suat-va-nhan-vien.md) | B | `agent-b/H6-07-hieu-suat-va-nhan-vien` | ✅ Đã merge (vòng 1) |
| [H6-01 Hàm thuần dây chuyền](H6-01-ham-thuan-day-chuyen.md) | A | `agent-a/H6-01-ham-thuan-day-chuyen` | ✅ Đã merge (vòng 10) |
| [H6-08 AI & tài khoản: khung và Trạng thái](H6-08-ai-tai-khoan-khung-va-trang-thai.md) | B | `agent-b/H6-08-ai-tai-khoan-khung-va-trang-thai` | ✅ Đã merge (vòng 1) |
| [V8-01 Thử lại lỗi máy và ghi nhật ký](V8-01-thu-lai-loi-may-va-ghi-nhat-ky.md) | A | `agent-a/V8-01-thu-lai-loi-may-va-ghi-nhat-ky` | ✅ Đã merge (vòng 1) |
| [H7-01 Dịch vụ Map và lưu nguyên tử](H7-01-dich-vu-map-va-luu-nguyen-tu.md) | B | `agent-a/H7-01-dich-vu-map-va-luu-nguyen-tu` | ✅ Đã merge (vòng 1) |
| [H7-03 Lõi chat Văn phòng và chuyển hướng](H7-03-loi-chat-van-phong-va-chuyen-huong.md) | B | `agent-b/H7-03-loi-chat-van-phong-va-chuyen-huong` | ✅ Đã merge (vòng 1) |
| [S1-01 Skill tiếp nhận đến duyệt kế hoạch](S1-01-skill-tiep-nhan-den-duyet-ke-hoach.md) | B | `agent-b/S1-01-skill-tiep-nhan-den-duyet-ke-hoach` | ✅ Đã merge (vòng 1) |
| [S1-05 Nối skill vào review độc lập](S1-05-noi-skill-vao-review-doc-lap.md) | B | `agent-b/S1-05-noi-skill-vao-review-doc-lap` | ✅ Đã merge (vòng 1) |
| [S1-02 Skill chuẩn bị đến publish](S1-02-skill-chuan-bi-den-publish.md) | B | `agent-b/S1-02-skill-chuan-bi-den-publish` | ✅ Đã merge (vòng 1) |
| [S1-03 Bốn skill code theo stack](S1-03-bon-skill-code-theo-stack.md) | A | `agent-a/S1-03-bon-skill-code-theo-stack` | ✅ Đã merge (vòng 1) |
| [S1-08 Bổ sung quy ước thật vào 4 skill code](S1-08-bo-sung-quy-uoc-that-vao-4-skill-code.md) | B | `agent-b/S1-08-bo-sung-quy-uoc-that-vao-4-skill-code` | ✅ Đã merge (vòng 1) |
| [H7-07 Giao diện Map](H7-07-giao-dien-map.md) | B | `agent-b/H7-07-giao-dien-map` | ✅ Đã merge (vòng 1) |
| [S1-04 Nối skill vào plan, prep và giao code](S1-04-noi-skill-vao-plan-prep-va-giao-code.md) | A | `agent-a/S1-04-noi-skill-vao-plan-prep-va-giao-code` | ✅ Đã merge (vòng 1) |
| [S1-06 Lệnh ghi bài học có commit](S1-06-lenh-ghi-bai-hoc-co-commit.md) | B | `agent-b/S1-06-lenh-ghi-bai-hoc-co-commit` | ✅ Đã merge (vòng 1) |
| [S1-07 Nối nghiệm thu, IMP và hàng chờ Map](S1-07-noi-nghiem-thu-imp-va-hang-cho-map.md) | A | `agent-a/S1-07-noi-nghiem-thu-imp-va-hang-cho-map` | ✅ Đã merge (vòng 1) |
| [V8-02 Điều phối pipeline theo phụ thuộc](V8-02-dieu-phoi-pipeline-theo-phu-thuoc.md) | A | `agent-a/V8-02-dieu-phoi-pipeline-theo-phu-thuoc` | ✅ Đã merge (vòng 1) |
| [W1-05 Parser tiến độ và quy tắc chuyển bước](W1-05-parser-tien-do-va-quy-tac-chuyen-buoc.md) | B | `agent-b/W1-05-parser-tien-do-va-quy-tac-chuyen-buoc` | ✅ Đã merge (vòng 4) |
| [W1-02 Helper dựng trang Hub](W1-02-helper-dung-trang-hub.md) | B | `agent-b/W1-02-helper-dung-trang-hub` | ✅ Đã merge (vòng 3) |
| [V8-03 Gói nghiệm thu và quyết định một lệnh](V8-03-goi-nghiem-thu-va-quyet-dinh-mot-lenh.md) | A | `agent-a/V8-03-goi-nghiem-thu-va-quyet-dinh-mot-lenh` | ✅ Đã merge (vòng 1) |
| [W1-06 Dịch vụ tổng hợp việc theo dự án](W1-06-dich-vu-tong-hop-viec-theo-du-an.md) | B | `agent-b/W1-06-dich-vu-tong-hop-viec-theo-du-an` | ✅ Đã merge (vòng 3) |
| [V6-02 Chặn giao lại và xin quyết định](V6-02-chan-giao-lai-va-xin-quyet-dinh.md) | A | `agent-a/V6-02-chan-giao-lai-va-xin-quyet-dinh` | ⏸ Tạm dừng · quá 3 vòng (còn 2 ca Fresh_ai_log đỏ ở lượt Codex 5) |
| [W1-07 Trang Việc và thanh bước](W1-07-trang-viec-va-thanh-buoc.md) | B | `agent-b/W1-07-trang-viec-va-thanh-buoc` | ✅ Đã merge (vòng 4) |
| [W1-01 Helper nguồn thật và sandbox PowerShell](W1-01-helper-nguon-that-va-sandbox-powershell.md) | A | `agent-a/W1-01-helper-nguon-that-va-sandbox-powershell` | ✅ Đã merge (vòng 2) |
| [V8-09 · Sửa đọc mã thoát `agy models` + Codex quá tải tự thử lại](V8-09-agy-models-va-codex-qua-tai.md) | A | `agent-a/V8-09-agy-models-va-codex-qua-tai` | ✅ Đã merge (vòng 2) |
| [H7-06 Menu, danh sách dự án và bảng duyệt](H7-06-menu-danh-sach-du-an-va-bang-duyet.md) | A | `agent-a/H7-06-menu-danh-sach-du-an-va-bang-duyet` | ⏸ Tạm dừng · lượt 6, cổng KT KHÔNG ĐẠT (24 lỗi Hub), quá 3 lượt |
| [V8-10 · `team.ps1 hub -Restart` + giới hạn 45 phút cho `deputy`](V8-10-hub-restart-va-deputy-timeout.md) | B | `agent-a/V8-10-hub-restart-va-deputy-timeout` | ✅ Đã merge (vòng 1) |
| [H7-02 Điều phối plan/prep/QC theo Map](H7-02-dieu-phoi-plan-prep-qc-theo-map.md) | B | `agent-b/H7-02-dieu-phoi-plan-prep-qc-theo-map` | ⏸ Tạm dừng · quá 3 lượt; còn lỗi harness S1-04 |
| [H7-04 Thẻ tác nhân và chat giao việc](H7-04-the-tac-nhan-va-chat-giao-viec.md) | B | `agent-b/H7-04-the-tac-nhan-va-chat-giao-viec` | ⏸ Tạm dừng · warning test bất biến và hồi quy V5-05/W1-02 |
| [W1-03 Sinh mockup và khóa prep trước duyệt](W1-03-sinh-mockup-va-khoa-prep-truoc-duyet.md) | A | `agent-a/W1-03-sinh-mockup-va-khoa-prep-truoc-duyet` | ⬜ Chưa giao |

Trạng thái: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge

## Kế hoạch / hàng chờ
Theo `docs/HUB-DESIGN.md` mục 7:
- **H-003 (B):** nối trang với dịch vụ thật; trang Agent, Hộp thư. Sau H-001 và H-002.
- **H-004 (A):** trang Cài đặt, Tài khoản, Bí mật; lệnh `team.ps1 hub`. Sau H-001.
- **H-005 (B):** module "AITeam Hub" trong HDV Tool (WebView2), làm ở dự án HDVTool.
