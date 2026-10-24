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
| Build 0 warning — CHƯA ĐẠT | `dotnet build AITeam.sln -m:1 --no-restore`: exit 0, 0 lỗi, 3 warning NU1900 tại Hub, Hub.Tests, Core.Tests. |
| Toàn bộ test pass | `dotnet test AITeam.sln --no-restore --no-build -m:1 --blame-hang-timeout 2m`: exit 0; Hub 289/289, Core 129/129; tổng 418 pass, 0 fail, 0 skip. |
| Gồm nghiệm thu V4-11 và review cũ | Chạy toàn bộ hai assembly bằng lệnh trên, bao gồm 2 ca V4-11; giữ nguyên file test nghiệm thu. |
- Đã kiểm tra: ReviewAsync dùng stamp giờ + 8 ký tự hex cho prompt/schema/kết quả, mọi nhánh SaveResult dùng chung stamp; không cần sửa thêm logic.
- Xác minh lỗi mạng: `dotnet restore AITeam.sln --force --disable-parallel -m:1` exit 0 nhưng vẫn 3 warning NU1900: không tải được `https://api.nuget.org/v3/index.json`; buộc restore lại không khắc phục được.
- File đổi lượt này: chỉ file task này để cập nhật kết quả kiểm tra và nguyên nhân bị chặn; giữ nguyên BOM UTF-8.
- Quyết định: giữ phạm vi task, không sửa cấu hình ngoài phạm vi hoặc tắt/ẩn cảnh báo kiểm tra lỗ hổng để làm cổng kiểm tra xanh; dùng `-m:1` và timeout test 2m theo chỉ dẫn vận hành.
- Chưa xác minh: build 0 warning và dữ liệu lỗ hổng NuGet; cần môi trường truy cập được NuGet để chạy lại cổng kiểm tra. Không chạy app/chụp ảnh theo phân công.
- Không chạy lệnh git theo chỉ dẫn Codex dự phòng; hệ thống thực hiện commit/push.

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
