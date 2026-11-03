# H8-03 — Review không lặp và phân trang đúng
- Nhánh: `agent-c/H8-03-review-khong-lap`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Review | Task.cshtml chỉ render Markdown một lần, giữ đoạn văn ngoài bullet; HubViews nhận vòng cũ dạng Vòng N hoặc in đậm, không đổi xử lý heading tab của V9-04. |
| Phân trang | RunPage giữ PageSize thực; PaginationSummary tính vị trí từ PageSize và số item thực, không ghi cứng 8. |
| Kiểm thử | 2 test mới đạt; build -warnaserror -nodeReuse:false 0 warning/0 error; toàn bộ Hub 778/792 đạt, 14 lỗi nền, Core 397/690 đạt, 293 lỗi nền. Không sửa test cũ. |
| Giao diện | Ảnh Hub thật HUB_FAKE docs/screenshots/H8-03/task-1280.png; document 1280/1280, dữ liệu tạm, đã xem ảnh. Chưa có ảnh dữ liệu review nhiều vòng thật. |
| Giới hạn | Chờ Windows để kiểm hết test nền. File đổi: Task.cshtml, Agents.cshtml.cs, HubViews và test mới; thay tối thiểu cho đúng yêu cầu. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
