# V9-16 — Tự kiểm chất lượng task trước khi giao
- Nhánh: `agent-c/V9-16-kiem-chat-luong-task`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Lint | TaskLinter + CLI lint-task + team.ps1 trước assign; kiểm mục/checkbox/Phạm vi tồn tại hoặc tạo mới, chặn đường dẫn thoát gốc. |
| Test nghiệm thu | Chạy build -warnaserror -nodeReuse:false, rồi filter theo tên file test; chặn test xanh, không tìm thấy test, treo, NullReferenceException/harness. Log lỗi che bí mật. |
| Build/test | Build 0 warning/0 error; 7 test V916 mới đạt. Toàn bộ Core 404/697 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền; /tmp/r012-c5-tests. PS AST 0 lỗi. |
| DỪNG do test cũ | AcceptanceS104SkillPromptsTests.cs:180 chỉ dựng Agent/Nhánh, :168 không nạp Invoke-LintTask, nhưng :51–65 đòi giao prompt thành công. Lint bắt buộc phải chặn task thiếu mục; không thêm nhánh nhận diện test, không sửa harness/assert. Đây là đối chiếu nguồn, chưa chạy lại trên Windows. |
| Giới hạn | Chưa nghiệm thu Windows; tên lớp test khác tên file cần khai báo hợp đồng filter rõ hơn; chưa có fixture dựng Razor lỗi thật ngoài kiểm NullReferenceException. Chưa xác nhận mọi dạng harness lỗi. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chờ giải quyết xung đột nghiệm thu trước triển khai. |
