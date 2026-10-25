BƯỚC 4 của quy trình v4 (chia việc): bạn (Codex) chuẩn bị 1 task cho lập trình viên Antigravity. Bạn viết ĐẶC TẢ + KHUNG CODE + TEST NGHIỆM THU — KHÔNG viết phần thân hàm (để `throw new NotImplementedException(); // TODO(agent)`).

Làm trong thư mục hiện tại (worktree của nhánh task). KHÔNG chạy lệnh git (hệ thống tự commit).
TIẾT KIỆM TOKEN (bắt buộc): CHỈ đọc `AGENTS.md`, `templates/task.md`, `plans/{REQUEST}/brief.md` (+ mockup nó nêu, nếu task có giao diện), các file trong "Phạm vi" của kế hoạch và tối đa 5 file mã liên quan trực tiếp (giao diện / lớp được gọi). KHÔNG quét cả repo, không đọc file task / phiếu cũ, không đọc thư mục bin/obj/logs. Chạy build tối đa 2 lần.

1. Điền file `{TASKFILE}` (đã có khung từ templates/task.md, giữ nguyên các dòng Agent / Nhánh / Yêu cầu / Review AI ở đầu):
   - "## Tóm tắt" (3–5 gạch lời thường, có chức năng mới cho người dùng không), "## Đọc (CHỈ những file này)", "## Phạm vi (được sửa)" (đường dẫn trong backtick), "## Không làm", "## Ràng buộc", "## Tiêu chí hoàn thành";
   - "## Test nghiệm thu": CHỈ đường dẫn file test trong backtick (vd. `core.tests/AcceptanceXxxTests.cs`).
   - Dòng "- **Ngân sách:** ~N dòng" theo kế hoạch.
   - Task khó (sửa `team.ps1` / khóa / chạy song song / giao dịch git / Độ khó L) ⇒ thêm dòng "- **Model:** `claude-opus-5-5-medium`" (V8-01: flash-medium bỏ sót lỗi `reset --hard` 2 lượt). Mã model phải có trong `agy models` (25/10: mã cũ claude-opus-4-6-thinking bị gỡ ⇒ agent không chạy). Task giao diện / tài liệu nhỏ ⇒ để trống (mặc định).
2. Khung code: interface / record / lớp / chữ ký public đúng như kế hoạch; chú thích `///` mô tả CHÍNH XÁC hành vi + câu báo lỗi; đăng ký DI nếu là dịch vụ Hub.
3. **Dữ liệu THẬT, không đoán định dạng** (lỗi lặp 2 lần 25/10: `projects.json` là MẢNG ở gốc; phiếu là `requests/R-xxx-<slug>.md` với tiêu đề `## 3. Nhật ký (…)`, xuống dòng CRLF): mở file thật trong repo trước khi đặc tả; test dựng từ BẢN SAO nội dung file thật (giữ BOM / CRLF / tên file), không tự chế cấu trúc.
   Test nghiệm thu (xUnit): đủ ca biên / lỗi / bảo mật / dữ liệu cũ trong kế hoạch; bỏ code mới đi thì test phải đỏ; không `return` sớm; chuỗi giống bí mật ghép lúc chạy (`string.Concat("sk-", …)`); không chạy tiến trình thật của agy / codex. Test gọi `powershell.exe` đọc stdout: đặt `StandardOutputEncoding` UTF-8 (C#) + `[Console]::OutputEncoding` UTF-8 (script) — S1-04 thiếu ⇒ agent lách bằng mã nhận diện harness. Gọi PowerShell bằng `-File` script tạm, KHÔNG `-EncodedCommand` (stderr thành CLIXML). Repo git tạm: bỏ cờ ReadOnly mọi file trước `Directory.Delete`. Commit biến thể có thể trùng fixture ⇒ `--allow-empty` (S1-06: 51 test đỏ chỉ vì harness).
   - Khung của lệnh CLI / tiến trình con (vd. `aiteam <lệnh>`): KHÔNG để `NotImplementedException` văng khỏi `Main` — bắt lại, in `LỖI: chưa làm` ra stderr, trả mã 1. (Lỗi chưa bắt trong tiến trình con ⇒ Windows hiện hộp "dotnet.exe - Application Error" trên máy chủ dự án — 25/10.)
4. Chạy `{BUILD}` — phải 0 lỗi (test nghiệm thu được phép ĐỎ khi chạy vì thân hàm chưa có; cảnh báo CS9113 tham số chưa dùng trong khung được chấp nhận).
5. KHÔNG đưa vào task tiêu chí "chụp ảnh / chạy app / đo trình duyệt": lập trình viên không chạy app; Claude kiểm giao diện thật ở bước 7 (nghiệm thu). Giao diện kiểm bằng test dựng trang + test markup.
6. File `.cs` / `.cshtml` / `.ps1` lưu UTF-8 CÓ BOM. Đọc `AGENTS.md` mục "Lỗi hay gặp".
6. Cuối cùng trả lời ≤ 10 dòng: file đã tạo, số ca test, điều chưa chắc.

=== KẾ HOẠCH ĐÃ DUYỆT CỦA TASK {TASK} ===
