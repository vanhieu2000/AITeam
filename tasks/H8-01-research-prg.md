# H8-01 — Phòng nghiên cứu tránh gửi lại khi F5
- Nhánh: `agent-c/H8-01-research-prg`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| DỪNG xung đột | AcceptanceV507ResearchPageTests.cs:202 Assert.IsType<PageResult>(OnPostAsk(...)); R-012 yêu cầu POST-Redirect-GET, thành công phải redirect. Hai hợp đồng trả về xung đột trực tiếp. |
| Giữ nguyên | Chưa đổi handler thành redirect, không sửa assert hay giả PageResult. Cần quản lý cập nhật hợp đồng nghiệm thu trước. |
| Phần quota | Cần khóa provider nghỉ và hiện giờ hồi, nhưng không triển khai dở mã đang bị chặn bởi xung đột nghiệm thu; dữ liệu quota B2 chưa trên main. |
| Kiểm tra | Đối chiếu nguồn handler và assert; chỉ báo cáo, không chạy lại build/test. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Cập nhật | Pull d32ffae của Claude và gộp main 9bc9a8e vào đúng nhánh cũ; test V507 mới đòi redirect r/q. |
| DỪNG xung đột bổ sung | H705EmbeddedResearchTests.cs:35 vẫn Assert.Equal(HttpStatusCode.OK, asked.StatusCode) sau POST thành công ResearchAsk. Chỉ dẫn đợt 2 bắt OnPostResearchAsk redirect về /?r=…; HTTP redirect phải 302, không thể đồng thời trả 200. |
| Giữ nguyên | Không đổi assert/harness, không dùng HTMX header hoặc nhận diện client test để trả kết quả khác nhau; chưa sửa handlers nhằm tránh triển khai nửa hợp đồng. Đây là đối chiếu nguồn, chưa chạy test mới vì mã dừng. |
| Quota | V9-11 chưa vào main và còn xung đột V911QuotaTests; khóa provider nghỉ/giờ hồi tiếp tục chờ theo chỉ dẫn đợt 2. |
| Bước cần quản lý | Cập nhật hợp đồng HTTP thành công của H705 theo PRG, đồng thời giữ kiểm XSS/manual save sau GET redirect. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chưa chạy Windows. |
