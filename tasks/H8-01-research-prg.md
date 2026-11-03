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
