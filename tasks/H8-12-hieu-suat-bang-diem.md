# H8-12 — Hiệu suất và bảng điểm AI
- Nhánh: `agent-c/H8-12-hieu-suat-bang-diem`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Mockup | docs/design/H8-12-mockup.html: HTML tĩnh tiếng Việt, ghi rõ số minh họa, chưa nối dữ liệu. |
| Kiểm bố cục | Chromium thật 1000×800: document scrollWidth=clientWidth=1000; mọi nút không tràn. Ảnh docs/screenshots/H8-12/mockup-1000.png. |
| Trạng thái | ĐÃ PUSH MOCKUP, DỪNG CHỜ CHỦ DỰ ÁN DUYỆT theo R-012; chưa sửa UI sản phẩm, không merge main. |
| Giới hạn | Chưa xác minh số liệu thật hoặc luồng backend; nút chỉ minh họa. Không chạy build/test vì chỉ thêm HTML/ảnh/tài liệu. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — OpenCode
| Tiêu chí | Bằng chứng |
|---|---|
| 7 chỉ số tuần này/tuần trước | `PerformanceService.Read` dựng M1–M7 từ `HubMetricReader` + mục Review; M1 để "chưa đo" vì chưa có nguồn phút chủ dự án. |
| Lỗi theo mã + bảng điểm | `FindingParser` (mã lạ ⇒ "khác"), điểm theo provider/model, phút TB S/M/L, token TB, AG theo nhóm, hạn mức; < 3 task ⇒ "Chưa đủ dữ liệu". |
| Giao diện 1000 px | `Performance.cshtml` + `performance.css`: thẻ grid, thanh cột chồng, bảng cuộn trong khung; thêm liên kết từ Văn phòng. |
| File đã đổi | `hub/Services/PerformanceService.cs`, `hub/Pages/Performance.cshtml(.cs)`, `hub/wwwroot/css/performance.css`, `hub/Services/HubV3Registration.cs`, `hub/Pages/Office.cshtml`, `hub.tests/H812PerformanceTests.cs`. |
| Build | `dotnet build AITeam.sln -warnaserror -nodeReuse:false`: 0 warning, 0 error. |
| Test | `H812PerformanceTests`: 2/2 pass; toàn bộ Hub: 824/824 pass; Core: 907/907 pass (chạy riêng `-m:1`). |
| Chưa xác minh | Chưa mở trình duyệt thật 1000×800; `/Performance?tab=staff` cũ giờ do trang mới xử lý, `/Overview` giữ nguyên. |
