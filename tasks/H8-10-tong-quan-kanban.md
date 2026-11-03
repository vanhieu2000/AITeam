# H8-10 — Toàn hệ thống và Kanban

- **Yêu cầu:** R-012
- Nhánh: `agent-c/H8-10-tong-quan-kanban`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Mockup | docs/design/H8-10-mockup.html: HTML tĩnh tiếng Việt, ghi rõ số minh họa, chưa nối dữ liệu. |
| Kiểm bố cục | Chromium thật 1000×800: document scrollWidth=clientWidth=1000; mọi nút không tràn. Ảnh docs/screenshots/H8-10/mockup-1000.png. |
| Trạng thái | ĐÃ PUSH MOCKUP, DỪNG CHỜ CHỦ DỰ ÁN DUYỆT theo R-012; chưa sửa UI sản phẩm, không merge main. |
| Giới hạn | Chưa xác minh số liệu thật hoặc luồng backend; nút chỉ minh họa. Không chạy build/test vì chỉ thêm HTML/ảnh/tài liệu. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Phạm vi triển khai đã duyệt
- `hub/Services/WorkOverviewService.cs`, `hub/Services/HubMetricReader.cs`, `hub/Pages/Work.cshtml`, `hub/Pages/Work.cshtml.cs`, `hub/wwwroot/css/work-overview.css` (tạo mới).
- `hub/Services/HubV3Registration.cs`, `hub/Pages/Office.cshtml`, `hub/Pages/Project.cshtml`; test mới `hub.tests/H810OverviewTests.cs`; ảnh `docs/screenshots/H8-10/`.
## Báo cáo của agent — UI đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Duyệt | Chủ dự án đã duyệt mockup trong phiên này; triển khai trên nhánh cũ sau gộp main 9bc9a8e, không merge main. |
| Tổng quan | /Work có mười trạng thái/bộ lọc, Cần anh (bị chặn, tạm dừng >24h khi có giờ, đơn duyệt), chọn dự án và tự cập nhật 30 giây. Liên kết từ Văn phòng và dự án, giữ menu/chat đã nghiệm thu. |
| Kanban | Năm cột đúng yêu cầu; thẻ có người, phút/lượt/cờ. Cột merge chỉ dùng giờ merge thật từ IGitHistory; thiếu giờ không suy diễn thành 7 ngày. |
| Nguồn số liệu | Đọc bảng việc + metrics nullable/đổi cộng dồn theo conversationId thành từng lượt; lỗi nguồn báo rõ. Đọc có giới hạn và chặn traversal/reparse point; không ghi file. |
| Kiểm thử | 3 test mới đạt, gồm 10 trạng thái thật tạm, paused >24h, metrics/cờ, XSS, filter 404, delta và token chưa đo. Build 0 warning/0 error; Hub 806/820 đạt (14 nền), Core 501/829 đạt (328 nền). Chưa chạy Windows. |
| Ảnh thật | docs/screenshots/H8-10/work-1280.png, kanban-1000.png; Chromium HUB_FAKE, document 1280/1280 và 1000/1000; đã xem/sửa xung đột CSS và chụp lại. |
| Giới hạn/đo | Giờ dừng/merge thiếu vẫn chưa biết, không bịa; phút: chưa đo, token: chưa đo cho công việc agent. |
