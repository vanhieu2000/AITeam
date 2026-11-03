# V9-15 — Hàng đợi tự chạy và khóa file
- Nhánh: `agent-c/V9-15-hang-doi-khoa-file`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Trạng thái | Dừng triển khai hàng đợi do thiếu hợp đồng nền trên origin/main 759ddaf. |
| Phụ thuộc | R-012 C4 yêu cầu định tuyến C3, hạn mức B2 và tạm dừng B4. Các nhánh V9-14/V9-11/V9-13 mới push, chưa được merge; main chưa có DeveloperSelector/QuotaObservation/RunDiagnostics. |
| Rủi ro cụ thể | Auto chạy song song khi thiếu khóa/quota/pause thống nhất có thể giao lại task đã tạm dừng hoặc tiếp tục dùng provider hết hạn mức. Không viết vòng lặp bỏ qua các điều kiện bắt buộc. |
| Chưa làm | Không thêm auto/locks.json tạm và không tự merge/cherry-pick nhánh khác. Cần tích hợp nền trước để kiểm 2 task cùng file, phụ thuộc đã merge và đổi người khi hết hạn mức. |
| Kiểm tra | Đọc mã main và yêu cầu C4; thay đổi chỉ tài liệu, không chạy lại build/test, không sửa assert cũ. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Phụ thuộc | Đã fetch main 9bc9a8e; DeveloperSelector/RunDiagnostics đã có, nhưng QuotaObservation.cs của V9-11 chưa tồn tại trên main. |
| Trạng thái | Chờ đúng mục 3 GIAO-CODEX-DOT-2.md: chỉ làm hàng đợi sau khi V9-11 vào main. V9-11 hiện còn test V911 cũ xung đột đã báo, không tự merge nhánh đó. |
| Kiểm tra | Chỉ cập nhật nhánh và báo cáo, chưa sửa queue/lock; không chạy build/test không liên quan. Chưa chạy Windows. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
