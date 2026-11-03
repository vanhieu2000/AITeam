# V9-02 — Đồng bộ test pipeline khi máy bận
- Nhánh: `agent-c/V9-02-dong-bo-test-pipeline`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Harness | V8-02 nhánh Together chờ cả hai child đã ghi start bằng điều kiện SpinUntil có timeout; đồng hồ logic chia sẻ qua mutex thay UtcNow.Ticks để so thứ tự không phụ thuộc độ phân giải Windows. |
| Assert | Script đối chiếu với origin/main xác nhận mọi dòng Assert./Need/Expect-Error giữ nguyên; không đổi mã sản phẩm. |
| Cú pháp/build | PS AST fixture mới 0 lỗi; build -warnaserror -nodeReuse:false 0 warning/0 error. |
| Test | Core 397/690 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền. Các ca PowerShell chưa chạy được; chưa có bằng chứng hết đỏ ngẫu nhiên trên máy Windows bận. |
| Giới hạn | V8-03 không có Sleep cố định trong file hiện tại nên không sửa đoán. Nhánh mô phỏng công việc tranh chấp khóa vẫn giữ Delay để không làm yếu test khóa; phần bỏ toàn bộ delay chưa hoàn tất. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chờ kiểm Windows, chưa tuyên bố hoàn tất V9-02. |
