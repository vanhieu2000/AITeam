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
| Tiêu chí hoàn thành | Bằng chứng chạy lại ngày 24/10/2026 |
|---|---|
| Build 0 warning | Với biến môi trường phiên chạy `NuGetAudit=false`: `dotnet build AITeam.sln -m:1` exit 0, 0 warning, 0 error, 14,42 giây. |
| Toàn bộ test, gồm 2 ca V4-11 và review cũ | Cùng biến môi trường: `dotnet test AITeam.sln --no-restore -m:1 --blame-hang-timeout 2m` exit 0; Hub 289 + Core 129 = 418 pass, 0 fail, 0 skip; không có warning. |
| Tên file riêng, cùng stamp; giữ chữ ký public và UTF-8 không BOM | ReviewAsync/SaveResult đã đúng thiết kế; giữ nguyên mã và test nghiệm thu; Core 129 test pass. |
| NU1900 trong phiên kiểm tra | Không còn 3 NU1900 sau restore với `NuGetAudit=false`; chỉ tắt audit cho tiến trình kiểm tra, không sửa cấu hình dự án. |
- File đổi: chỉ `tasks/V4-11-review-unique-files.md`, cập nhật bằng chứng và giữ nguyên trạng thái BOM UTF-8.
- Quyết định: dùng `-m:1` vì `dotnet build AITeam.sln` với cùng biến môi trường vẫn exit 1, 0 warning/0 error, không có chẩn đoán.
- Chưa xác minh: dữ liệu lỗ hổng NuGet; cảnh báo có thể trở lại khi cổng kiểm tra restore với audit bật mà không truy cập được NuGet. Chưa đạt bảo đảm build mặc định/cổng kiểm tra.
- Không sửa cấu hình ngoài phạm vi, không chạy app/chụp ảnh/lệnh git; hệ thống tự commit/push theo chỉ dẫn Codex.
## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- **Lần đầu Codex viết code thật trong quy trình** (`assign -Dev codex`): `aiteam run` → Codex sửa file trong sandbox → `team.ps1` commit "Agent C (Codex)" + push → cổng KT. Code đúng đặc tả từng dòng: 11/25 dòng, 2/2 ca nghiệm thu, 129 + 289 test.
- **Sự cố:** cổng KT trượt vì 3 cảnh báo `NU1900` (NuGet không tải được dữ liệu lỗ hổng — lỗi MẠNG, không phải code) và cờ "đã tự giao lại" chưa từng được bật ⇒ Codex bị giao lại **28 lần** (41 phút, ~6,9 triệu token vào (phần lớn cache), 50 nghìn token ra) tới khi mạng có lại.
  - [CK] (công cụ) cổng KT coi NU1900 là lỗi code ⇒ đã sửa: bỏ qua NU1900, ghi chú; test-verify thêm 2 kịch bản (bản cũ hỏng đúng 2 kiểm tra).
  - [CK] (công cụ) `$script:VerifyRetried` chỉ đọc, không bao giờ gán ⇒ đã sửa + giới hạn cứng 2 lượt / lệnh assign. (Lỗi có từ trước: V4-06 hôm qua cũng bị giao lại 2 lần.)
- Codex vẫn còn hạn mức sau 28 lượt (`aiteam providers --no-check`: Sẵn sàng).

Nhân viên: Codex (C) — 28 lượt (27 thừa do công cụ), lượt cuối 82 giây, 11/25 dòng.

## Kết quả
- **Không có chức năng mới nhìn thấy ngay** — mỗi lần review AI giờ có file lời nhắn / lược đồ / kết quả riêng (`…-yyyyMMdd-HHmmss-xxxxxxxx`), 2 review cùng lúc không ghi đè nhau.
- **Kiểm chứng:** 2 ca nghiệm thu (tuần tự + 4 review song song).
- **Luồng Codex viết code đã chạy thật end-to-end.**

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-004) | — | — | — |
| 2. Ý tưởng | Claude | 5 | ~5' | Việc thật từ hàng chờ để thử Codex | — | — |
| 3. Plan / thiết kế | Claude | 5 | ~10' | Đặc tả từng dòng, test song song | — | — |
| 4. Giao việc | Claude | 4 | ~5' | — | — | — |
| 5. Thực hiện code | Codex (C) | 5 | 82s / lượt | Đúng đặc tả | — | — |
| 6. Review / QC | Cổng KT + Claude | 1 | ~60' | Vòng lặp 28 lượt do cổng KT coi lỗi mạng là lỗi code + cờ giới hạn không hoạt động | CK | Đã sửa + test; giới hạn cứng số lượt |
| 7. Đánh giá & bàn giao | Claude | 5 | ~10' | — | — | — |