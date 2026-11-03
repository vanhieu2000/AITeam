# H8-05 — Log tiếng Việt và giữ 200 job gần nhất
- Nhánh: `agent-c/H8-05-log-viet-va-gioi-han-job`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| DỪNG xung đột | AcceptanceV505CommandTests.Jobs_keeps_at_most_twenty (:253–264) tạo 25 job và Assert.Equal(20, s.Jobs().Count). Yêu cầu giữ danh sách 200 gần nhất phải trả 25 tại đây, trái assert cũ. |
| Hiện trạng | CommandService.Jobs trả Take(20), danh sách _jobs trong bộ nhớ chưa giới hạn; RealProcessStarter đọc UTF-8 trong khi PowerShell 5.1 cần thống nhất OutputEncoding. |
| Chưa sửa | Không đổi assert, không tạo hai chế độ nhận diện test để giữ 20. Dừng mã này theo chỉ dẫn; cần chốt hợp đồng 20 hiển thị/200 lưu hoặc 200 hiển thị. |
| Kiểm tra | Đối chiếu nguồn/test; chỉ báo cáo, không chạy build/test hay tiến trình PowerShell thật. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
