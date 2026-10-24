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
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln` mã thoát 1, không nêu lỗi; sau restore, `dotnet build AITeam.sln --no-restore -m:1` thành công: 0 lỗi, 3 warning NU1900 vì không truy cập được api.nuget.org; chưa đạt 0 warning. |
| Toàn bộ test pass | `dotnet test AITeam.sln --no-restore --no-build -m:1 --blame-hang-timeout 2m`: Hub 289/289, Core 129/129; tổng 418 pass, 0 fail, 0 skip. |
| Hai ca V4-11 và test review cũ | Lệnh toàn bộ ở trên bao gồm test review cũ; `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceV411ReviewFilesTests" --blame-hang-timeout 2m`: 2/2 pass. |
- **Đã làm:** Tạo stamp giờ + 8 ký tự hex mỗi ReviewAsync, dùng chung cho prompt/schema/kết quả và mọi nhánh fail-safe; giữ chữ ký public, UTF-8 không BOM và test nghiệm thu nguyên vẹn.
- **File đã đổi:** `core/AITeam.Core/Review/ReviewService.cs` (tên file riêng, tham số private SaveResult và chú thích); file task này (báo cáo); giữ nguyên trạng thái BOM của từng file.
- **Quyết định:** Dùng `-m:1` sau khi MSBuild mặc định thất bại không nêu lỗi; restore bằng `dotnet restore AITeam.sln --disable-parallel -m:1 --verbosity normal` thành công, 3 warning NU1900; dùng timeout 2m theo chỉ dẫn mới nhất.
- **Điều chưa chắc:** Chưa xác minh được tiêu chí build 0 warning do mạng NuGet; không chạy app/chụp ảnh theo phân công. Không chạy git; hệ thống tự commit/push theo chỉ dẫn Codex.

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
