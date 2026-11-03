# H8-04 — Hiệu suất không cắt chữ cột phải
- Nhánh: `agent-c/H8-04-hieu-suat-man-hinh-hep`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| CSS | Index.cshtml cho cột phải xuống dưới ở ≤1100 px, tiêu đề tự tăng chiều cao, hàng khóa/giá trị xuống dòng và thẻ KPI co theo chiều rộng. Bỏ ellipsis hoạt động. |
| Ảnh thật | docs/screenshots/H8-04/performance-1000.png, right-column-1000.png, performance-1280.png; Chromium HUB_FAKE/gốc tạm; document 1000/1000 và 1280/1280, đã xem ảnh cột phải. |
| Build/test | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. Test Hub 776/790 đạt (14 lỗi nền), Core 397/690 đạt (293 lỗi nền). Không đổi test/assert cũ. |
| Giới hạn | Chỉ sửa CSS nhỏ; bảng nhiều cột vẫn có cuộn nội bộ. Chưa nghiệm thu toàn bộ trên Windows. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
