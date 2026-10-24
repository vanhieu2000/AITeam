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
| Tiêu chí hoàn thành | Bằng chứng kiểm tra lại ngày 24/10/2026 (lượt trả về NU1900) |
|---|---|
| Build mặc định — CHƯA ĐẠT | `dotnet build AITeam.sln`: exit 1, Build FAILED, 0 warning/0 error; không có chẩn đoán nguyên nhân. |
| Build 0 warning — CHƯA ĐẠT | `dotnet build AITeam.sln --no-restore -m:1`: exit 0, 0 error, 3 warning NU1900 tại Hub, Hub.Tests, Core.Tests. |
| Toàn bộ test, gồm 2 ca nghiệm thu V4-11 và review cũ | `dotnet test AITeam.sln --no-restore -m:1 --blame-hang-timeout 2m`: exit 0; Hub 289/289, Core 129/129; tổng 418 pass, 0 fail, 0 skip; vẫn 3 NU1900. |
| Chẩn đoán kết nối NuGet | HTTP_PROXY/HTTPS_PROXY/ALL_PROXY trỏ `127.0.0.1:9`; `curl.exe --noproxy "*" --head --max-time 15 https://api.nuget.org/v3/index.json`: exit 35, TLS SEC_E_NO_CREDENTIALS. |
- Đã đọc đúng 2 file trong mục Đọc: ReviewService đã dùng cùng stamp cho prompt/schema/kết quả và mọi nhánh SaveResult; không sửa mã nguồn hay test nghiệm thu.
- File đổi lượt này: chỉ `tasks/V4-11-review-unique-files.md`, cập nhật bằng chứng và giữ nguyên BOM UTF-8 hiện có.
- Quyết định: không tắt audit/ẩn warning hoặc đổi cấu hình ngoài phạm vi; lỗi truy cập NuGet chưa khắc phục được trong sandbox hiện tại.
- Chưa xác minh: dữ liệu lỗ hổng NuGet và nguyên nhân build mặc định thất bại; cần môi trường NuGet hoạt động để kiểm tra lại tiêu chí 0 warning. Review (Claude) đang trống.
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
