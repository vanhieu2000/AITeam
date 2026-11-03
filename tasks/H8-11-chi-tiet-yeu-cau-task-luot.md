# H8-11 — Chi tiết yêu cầu, task và lượt

- **Yêu cầu:** R-012
- Nhánh: `agent-c/H8-11-chi-tiet-yeu-cau-task-luot`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Mockup | docs/design/H8-11-mockup.html: HTML tĩnh tiếng Việt, ghi rõ số minh họa, chưa nối dữ liệu. |
| Kiểm bố cục | Chromium thật 1000×800: document scrollWidth=clientWidth=1000; mọi nút không tràn. Ảnh docs/screenshots/H8-11/mockup-1000.png. |
| Trạng thái | ĐÃ PUSH MOCKUP, DỪNG CHỜ CHỦ DỰ ÁN DUYỆT theo R-012; chưa sửa UI sản phẩm, không merge main. |
| Giới hạn | Chưa xác minh số liệu thật hoặc luồng backend; nút chỉ minh họa. Không chạy build/test vì chỉ thêm HTML/ảnh/tài liệu. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Phạm vi triển khai đã duyệt
- Tạo `hub/Services/WorkDetailService.cs`, `hub/Pages/Request.cshtml`, `hub/Pages/Request.cshtml.cs`, `hub/Pages/Run.cshtml`, `hub/Pages/Run.cshtml.cs`, `hub/wwwroot/css/work-details.css`, `hub.tests/H811DetailsTests.cs`.
- `hub/Services/HubV3Registration.cs`, `hub/Pages/Task.cshtml`, `hub/Pages/Office.cshtml`, `hub/Pages/Requests.cshtml`; tái dùng nguyên khung `HubMetricReader.cs` từ H8-10 và `core/AITeam.Core/Metrics/FindingParser.cs` từ V9-12 chưa vào main.
## Báo cáo của agent — UI đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Duyệt/nhánh | Chủ dự án đã duyệt; cập nhật nhánh cũ với main 9bc9a8e. Không sửa assert cũ/không merge main. |
| Yêu cầu | /Request?id=R-xxx: lời nguyên văn, 10 bước/bước hiện tại theo task, bằng chứng chưa có giữ chưa xác nhận; tổng phút/token đã đo, lượt thiếu đo, task, mã lỗi và nhật ký. |
| Task/lượt | Giữ Gantt/review/kết quả/đánh giá cũ; thêm liên kết chi tiết từng lượt và mã lỗi nhóm. Office/Requests nối chi tiết yêu cầu, Task nối /Run?project=…&task=…&n=…; từ /Work qua task tới log 2 lần bấm. |
| Log/an toàn | Lượt có provider/model/phút/token/+−/cờ/cổng KT/báo cáo; 50 dòng cuối log UTF-8 ≤2MiB, che bí mật và Razor encode; thiếu log trên máy không giả dữ liệu. Whitelist query, chặn traversal/reparse; trùng lượt cảnh báo. |
| Test/build | H811DetailsTests HTTP/dữ liệu tạm đạt: 10 bước, TN, token, 50 dòng, XSS và query sai 404. Build 0 warning/0 error; Hub 804/818 đạt (14 nền), Core 501/829 đạt (328 nền); chưa chạy Windows. |
| Ảnh thật | docs/screenshots/H8-11/request-1280.png và run-1000.png; Chromium nguồn minh họa tạm, document 1280/1280 và 1000/1000; đã xem ảnh. |
| Giới hạn/đo | Bước theo trạng thái task, các thời điểm lịch sử thiếu không suy diễn; log quá lớn hiện thông báo. Phút: chưa đo; token: chưa đo cho agent. |
