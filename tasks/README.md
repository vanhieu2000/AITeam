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
| [V6-02 Chặn giao lại và xin quyết định](V6-02-chan-giao-lai-va-xin-quyet-dinh.md) | C | `agent-c/V6-02-chan-giao-lai` | ✅ Đã merge 03/11 (Claude sửa harness) |
| [W1-07 Trang Việc và thanh bước](W1-07-trang-viec-va-thanh-buoc.md) | B | `agent-b/W1-07-trang-viec-va-thanh-buoc` | ✅ Đã merge (vòng 4) |
| [W1-01 Helper nguồn thật và sandbox PowerShell](W1-01-helper-nguon-that-va-sandbox-powershell.md) | A | `agent-a/W1-01-helper-nguon-that-va-sandbox-powershell` | ✅ Đã merge (vòng 2) |
| [V8-09 · Sửa đọc mã thoát `agy models` + Codex quá tải tự thử lại](V8-09-agy-models-va-codex-qua-tai.md) | A | `agent-a/V8-09-agy-models-va-codex-qua-tai` | ✅ Đã merge (vòng 2) |
| [H7-06 Menu, danh sách dự án và bảng duyệt](H7-06-menu-danh-sach-du-an-va-bang-duyet.md) | A | `agent-a/H7-06-menu-danh-sach-du-an-va-bang-duyet` | ✅ Đã merge 02/11 (OpenCode code, Claude review; fb83f06)
| [V8-10 · `team.ps1 hub -Restart` + giới hạn 45 phút cho `deputy`](V8-10-hub-restart-va-deputy-timeout.md) | B | `agent-a/V8-10-hub-restart-va-deputy-timeout` | ✅ Đã merge (vòng 1) |
| [H7-02 Điều phối plan/prep/QC theo Map](H7-02-dieu-phoi-plan-prep-qc-theo-map.md) | C | `agent-c/H7-02-map-plan-prep-qc` | ✅ Đã merge 03/11 · Windows xanh |
| [H7-04 Thẻ tác nhân và chat giao việc](H7-04-the-tac-nhan-va-chat-giao-viec.md) | B | `agent-b/H7-04-the-tac-nhan-va-chat-giao-viec` | ✅ Đã merge (vòng 1) |
| [W1-03 Sinh mockup và khóa prep trước duyệt](W1-03-sinh-mockup-va-khoa-prep-truoc-duyet.md) | A | `agent-a/W1-03-sinh-mockup-va-khoa-prep-truoc-duyet` | ✅ Đã merge 03/11 (OpenCode gộp main, Claude review) |

| [V9-10 Token mỗi lượt](V9-10-token-moi-luot.md) | C | `agent-c/V9-10-token-moi-luot` | ✅ Đã merge 03/11 · còn mẫu OpenCode thành công |
| [V9-13 Tự gắn cờ lượt](V9-13-tu-gan-co-moi-luot.md) | C | `agent-c/V9-13-tu-gan-co-luot` | ✅ Đã merge 03/11 (gồm V9-03) · còn log thật H7-06 |
| [H8-14 ChatGPT tại Hub, model và ghi nhớ SQLite](H8-14-chatgpt-local-va-ghi-nho-sqlite.md) | C | `agent-c/H8-14-chatgpt-local` | ✅ Đã merge (vòng 2) · gọi ChatGPT thật OK trên Windows |
| [H7-05 Nhúng Phòng nghiên cứu](H7-05-nhung-phong-nghien-cuu.md) | C | `agent-c/H7-05-nhung-phong-nghien-cuu` | ✅ Đã merge 03/11 (Claude cập nhật test V5-07) |
| [O-02 OpenCode DeepSeek](O-02-opencode-deepseek.md) | C | `agent-c/O-02-opencode-deepseek` | ✅ Đã merge 03/11 · chưa chạy model thật (proxy 403) |
| [V9-06 Codex ổn định](V9-06-codex-on-dinh.md) | C | `agent-c/V9-06-codex-on-dinh` | ✅ Đã merge 03/11 |
| [V9-14 Định tuyến theo luật](V9-14-dinh-tuyen-theo-luat.md) | C | `agent-c/V9-14-dinh-tuyen-theo-luat` | ✅ Đã merge 03/11 (phần 2: OpenCode sửa Resume giữ người làm) |
| [H8-03 Review không lặp](H8-03-review-khong-lap.md) | C | `agent-c/H8-03-review-khong-lap` | ✅ Đã merge 03/11 |
| [H8-04 Hiệu suất màn hình hẹp](H8-04-hieu-suat-man-hinh-hep.md) | C | `agent-c/H8-04-hieu-suat-man-hinh-hep` | ✅ Đã merge 03/11 |
| [H8-06 Đơn duyệt mẫu](H8-06-don-duyet-mau.md) | C | `agent-c/H8-06-don-duyet-mau` | ✅ Đã merge 03/11 |
| [V9-01 Xóa cache model](V9-01-xoa-cache-model.md) | C | `agent-c/V9-01-xoa-cache-model` | ✅ Đã merge 03/11 |
| [V9-02 Đồng bộ test pipeline](V9-02-dong-bo-test-pipeline.md) | C | `agent-c/V9-02-dong-bo-test-pipeline` | ✅ Đã merge 03/11 |
| [V9-05 Dispose khi Build lỗi](V9-05-dispose-khi-build-loi.md) | C | `agent-c/V9-05-dispose-khi-build-loi` | ✅ Đã merge 03/11 |
| [V9-07 Cổng kiểm tra harness/khóa build](V9-07-cong-kiem-tra-build-harness.md) | C | `agent-c/V9-07-cong-kiem-tra-build-harness` | ✅ Đã merge 03/11 |
| [V9-11 Hạn mức và giờ hồi](V9-11-han-muc-va-gio-hoi.md) | C | `agent-c/V9-11-han-muc-va-gio-hoi` | ✅ Đã merge 03/11 đợt 2 (sao lưu state hỏng) |
| [V9-12 Lỗi có cấu trúc](V9-12-loi-co-cau-truc.md) | C | `agent-c/V9-12-loi-co-cau-truc` | ✅ Đã merge 03/11 đợt 2 |
| [H8-05 Log tiếng Việt và giới hạn job](H8-05-log-viet-va-gioi-han-job.md) | C | `agent-c/H8-05-log-viet-va-gioi-han-job` | ✅ Đã merge 03/11 đợt 2 |
| [V9-16 Kiểm chất lượng task](V9-16-kiem-chat-luong-task.md) | C | `agent-c/V9-16-kiem-chat-luong-task` | ✅ Đã merge 03/11 đợt 2 |
| [H8-10 Tổng quan và Kanban](H8-10-tong-quan-kanban.md) | C | `agent-c/H8-10-tong-quan-kanban` | ✅ Đã merge 03/11 (mockup đã duyệt) |
| [H8-11 Chi tiết yêu cầu, task, lượt](H8-11-chi-tiet-yeu-cau-task-luot.md) | C | `agent-c/H8-11-chi-tiet-yeu-cau-task-luot` | ✅ Đã merge 03/11 (mockup đã duyệt) |
| [H8-01 Phòng nghiên cứu PRG](H8-01-research-prg.md) | C | `agent-c/H8-01-research-prg` | ✅ Đã merge 03/11 (OpenCode: PRG + khóa AI đang nghỉ) |
| [H8-12 Hiệu suất và bảng điểm](H8-12-hieu-suat-bang-diem.md) | C | `agent-c/H8-12-hieu-suat-bang-diem` | ✅ Đã merge 03/11 (OpenCode, vòng 2) · H8-15 đã sửa giao diện |
| [H8-13 Tài khoản và hạn mức](H8-13-tai-khoan-han-muc.md) | C | `agent-c/H8-13-tai-khoan-han-muc` | ✅ Đã merge 03/11 (OpenCode) · H8-15 đã sửa giao diện |
| [V9-15 Hàng đợi và khóa file](V9-15-hang-doi-khoa-file.md) | C | `agent-c/V9-15-hang-doi-khoa-file` | ✅ Đã merge 03/11 (OpenCode vòng 2: khóa + hàng đợi một nguồn C#) |
| [W1-04 Cổng kiểm tra QC](W1-04-cong-kiem-tra-qc.md) | C | `agent-c/W1-04-cong-kiem-tra-qc` | ✅ Đã merge 03/11 (OpenCode, Claude review) |
| [H8-15 Sửa nhỏ Hiệu suất và Tài khoản AI](H8-15-sua-nho-hieu-suat-tai-khoan.md) | O | `agent-c/H8-15-sua-nho-hieu-suat-tai-khoan` | ✅ Đã merge 03/11 (OpenCode) |
| [V9-17 Ghi số đo lượt chạy ngoài assign](V9-17-ghi-luot-chay-ngoai-assign.md) | O | `agent-o/V9-17-ghi-luot-chay-ngoai-assign` | ✅ Đã merge 03/11 (OpenCode vòng 2) |
| [V10-01 Danh bạ AI SQLite](V10-01-danh-ba-ai-sqlite.md) | O | `agent-c/V10-01-danh-ba-ai-sqlite` | ✅ Đã merge 03/11 (OpenCode, thay Codex hết hạn mức) |
| [V10-03 Duyệt chỉ việc nhạy cảm](V10-03-duyet-nhay-cam.md) | A | `agent-c/V10-03-duyet-nhay-cam` | 🟡 Đang chạy · máy HIEUDV · 00:29 04/11/2026 |
| [W2-01 Cấu hình loại việc](W2-01-cau-hinh-loai-viec.md) | O | `agent-o/W2-01-cau-hinh-loai-viec` | ✅ Đã merge 03/11 (OpenCode) |
| [V10-04 Dịch vụ báo cáo](V10-04-dich-vu-bao-cao.md) | O | `agent-o/V10-04-dich-vu-bao-cao` | ✅ Đã merge 04/11 (OpenCode) |
| [V10-02 Chạy AI theo danh bạ](V10-02-adapter-ai-tu-danh-ba.md) | B | `agent-o/V10-02-adapter-ai-tu-danh-ba` | 🟡 Đang chạy · máy HIEUDV · 00:49 04/11/2026 |
| [W2-02 Luật chuẩn + bài học](W2-02-luat-chuan-va-bai-hoc.md) | B | `agent-o/W2-02-luat-chuan-va-bai-hoc` | ⏳ Chờ AI rảnh |
| [V10-05 Trợ lý chat](V10-05-tro-ly-chat.md) | O | `agent-o/V10-05-tro-ly-chat` | ⏳ Chờ V10-02 |

Trạng thái: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge

## Kế hoạch / hàng chờ
Theo `docs/HUB-DESIGN.md` mục 7:
- **H-003 (B):** nối trang với dịch vụ thật; trang Agent, Hộp thư. Sau H-001 và H-002.
- **H-004 (A):** trang Cài đặt, Tài khoản, Bí mật; lệnh `team.ps1 hub`. Sau H-001.
- **H-005 (B):** module "AITeam Hub" trong HDV Tool (WebView2), làm ở dự án HDVTool.

| O-02 | OpenCode/DeepSeek | agent-c/O-02-opencode-deepseek | Chờ kiểm mạng/Windows; 8 test mới đạt |
