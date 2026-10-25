BƯỚC 4 của quy trình v4 (chia việc): bạn (Codex) chuẩn bị 1 task cho lập trình viên Antigravity. Bạn viết ĐẶC TẢ + KHUNG CODE + TEST NGHIỆM THU — KHÔNG viết phần thân hàm (để `throw new NotImplementedException(); // TODO(agent)`).

Làm trong thư mục hiện tại (worktree của nhánh task). KHÔNG chạy lệnh git (hệ thống tự commit).

1. Điền file `{TASKFILE}` (đã có khung từ templates/task.md, giữ nguyên các dòng Agent / Nhánh / Yêu cầu / Review AI ở đầu):
   - "## Tóm tắt" (3–5 gạch lời thường, có chức năng mới cho người dùng không), "## Đọc (CHỈ những file này)", "## Phạm vi (được sửa)" (đường dẫn trong backtick), "## Không làm", "## Ràng buộc", "## Tiêu chí hoàn thành";
   - "## Test nghiệm thu": CHỈ đường dẫn file test trong backtick (vd. `core.tests/AcceptanceXxxTests.cs`).
   - Dòng "- **Ngân sách:** ~N dòng" theo kế hoạch.
2. Khung code: interface / record / lớp / chữ ký public đúng như kế hoạch; chú thích `///` mô tả CHÍNH XÁC hành vi + câu báo lỗi; đăng ký DI nếu là dịch vụ Hub.
3. Test nghiệm thu (xUnit): đủ ca biên / lỗi / bảo mật / dữ liệu cũ trong kế hoạch; bỏ code mới đi thì test phải đỏ; không `return` sớm; chuỗi giống bí mật ghép lúc chạy (`string.Concat("sk-", …)`); không chạy tiến trình thật của agy / codex.
4. Chạy `{BUILD}` — phải 0 lỗi (test nghiệm thu được phép ĐỎ khi chạy vì thân hàm chưa có; cảnh báo CS9113 tham số chưa dùng trong khung được chấp nhận).
5. File `.cs` / `.cshtml` / `.ps1` lưu UTF-8 CÓ BOM. Đọc `AGENTS.md` mục "Lỗi hay gặp".
6. Cuối cùng trả lời ≤ 10 dòng: file đã tạo, số ca test, điều chưa chắc.

=== KẾ HOẠCH ĐÃ DUYỆT CỦA TASK {TASK} ===
