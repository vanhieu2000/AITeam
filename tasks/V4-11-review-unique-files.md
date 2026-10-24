# V4-11 · ReviewService: file tạm và kết quả có tên riêng mỗi lần review

- **Agent:** C
- **Nhánh:** `agent-c/V4-11-review-unique-files`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~25 dòng
- **Yêu cầu:** R-004
- **Độ khó:** S
- **Thời gian tối đa:** 15 phút
- **Chạy bằng:** Codex (`assign -Dev codex`)

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — sửa lỗi tiềm ẩn: 2 lần review AI cùng thư mục log (hoặc cùng giây) đang ghi đè file lời nhắn / lược đồ / kết quả của nhau.
- Mỗi lần review dùng tên file riêng (giờ + mã ngẫu nhiên), giữ lại để tra cứu.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Review/ReviewService.cs` (hàm `ReviewAsync`, `SaveResult`)
- `core.tests/AcceptanceV411ReviewFilesTests.cs` (test nghiệm thu — đặc tả chính xác)

## Mục tiêu
Trong `ReviewService`:
1. Đầu `ReviewAsync`: `var stamp = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..24];` (8 ký tự hex sau gạch: `yyyyMMdd-HHmmss-xxxxxxxx`).
2. `promptFile` = `prompt-review-{stamp}.md`, `schemaFile` = `schema-review-{stamp}.json` (thay cho tên cố định).
3. `SaveResult(logDir, result)` ⇒ thêm tham số `stamp`, ghi `review-{stamp}.json` (thay `review-{yyyyMMdd-HHmmss}.json`); mọi chỗ gọi truyền cùng `stamp`.
4. Giữ nguyên: UTF-8 KHÔNG BOM cho prompt / schema, mọi luật fail-safe, chữ ký public.

## Phạm vi (được sửa)
- `core/AITeam.Core/Review/ReviewService.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi chữ ký public, `ReviewRunner.cs`, `Providers/`, `Routing/`, `AITeam.Cli/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV411ReviewFilesTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 2 ca nghiệm thu V4-11 và các test review cũ).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng chạy lại ngày 24/10/2026 (lượt sửa NU1900) |
|---|---|
| Build 0 warning — CHƯA ĐẠT | `dotnet build AITeam.sln -m:1`: exit 0, 0 error, 6 warning NU1900 (3 project, lặp restore/build), 14,07 giây. |
| Toàn bộ test, gồm 2 ca nghiệm thu V4-11 và review cũ — ĐẠT | `dotnet test AITeam.sln --no-restore -m:1 --blame-hang-timeout 2m`: exit 0; Hub 289/289, Core 129/129; tổng 418 pass, 0 fail, 0 skip; 3 NU1900. |
| Xác minh kết nối NuGet — BỊ CHẶN | `curl.exe --noproxy '*' --head --connect-timeout 5 --max-time 15 https://api.nuget.org/v3/index.json`: exit 35, Schannel SEC_E_NO_CREDENTIALS; build vẫn không tải được chỉ mục NuGet. |
- Đã đọc AGENTS.md và đúng hai file mục Đọc; Review (Claude) trống; mã hiện có dùng cùng stamp riêng cho prompt/schema/kết quả, giữ prompt/schema UTF-8 không BOM.
- File đổi lượt này: chỉ `tasks/V4-11-review-unique-files.md` để cập nhật bằng chứng; giữ nguyên trạng thái BOM UTF-8, không sửa mã nguồn hoặc test nghiệm thu.
- Quyết định: không tắt audit hoặc ẩn NU1900; không sửa cấu hình ngoài phạm vi, không thay đổi cấu hình hệ thống. Dùng `-m:1` như lượt trước.
- Điểm chặn còn lại: cần môi trường kết nối NuGet hoạt động để kiểm tra lỗ hổng; chưa đạt build 0 warning, không thể sửa lỗi kết nối bằng mã ReviewService trong phạm vi được giao.
- Không chạy app/chụp ảnh, không chạy lệnh git; hệ thống tự commit/push theo chỉ dẫn Codex dự phòng.
## Review (Claude)

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Codex | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
