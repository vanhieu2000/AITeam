# V4-07 · Định tuyến provider: cấm tự duyệt, "nghỉ" khi hết hạn mức, khi nào gọi Codex review

- **Agent:** A
- **Nhánh:** `agent-a/V4-07-provider-routing`
- **Ưu tiên:** cao
- **Ngân sách:** ~230 dòng
- **Yêu cầu:** R-002
- **Độ khó:** M
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — phần lõi để AITeam tự chọn AI phù hợp cho từng việc.
- Reviewer luôn khác AI đã viết code (cấm tự duyệt).
- AI hết hạn mức ⇒ đánh dấu "nghỉ tới giờ X" (đọc đúng giờ trong câu báo lỗi, ví dụ Codex "try again at Oct 24th, 2026 4:19 AM") và tự chuyển sang AI dự phòng.
- Codex hạn mức nhỏ ⇒ chỉ review tự động task khó (L) hoặc task ghi rõ "Review AI: có".

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Routing/RoutingModels.cs` (model đã chốt — KHÔNG đổi)
- `core/AITeam.Core/Routing/Routing.cs` (khung — điền `TODO(agent)`)
- `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Providers/ProviderConfig.cs` (chỉ đọc: `RunStatus`, `AgentRunResult`, `ProviderSettings`)
- `core.tests/AcceptanceV407RoutingTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V4-MULTI-AI.md` mục B2 (vai trò ↔ provider) và C (P0-4, P0-5). Chú thích trong khung là đặc tả; khi khác test thì theo test.

## Ràng buộc
- Dữ liệu không tin cậy: file `.state/providers.json` có thể hỏng ⇒ coi như rỗng, không ném lỗi. Câu báo lỗi provider có thể dài / lạ ⇒ regex không được treo (không dùng lồng lượng từ kiểu `(a+)+`).
- Ghi file: file tạm cùng thư mục rồi `File.Move(overwrite: true)`; UTF-8 không BOM; dùng `System.Text.Json` (enum ghi dạng chữ).
- Giờ: mọi giờ là giờ địa phương truyền vào qua `now` — KHÔNG gọi `DateTime.Now` trong code (để test được).

## Mục tiêu
Điền `QuotaResetParser.Parse`, `ProviderStateStore` (`All`, `RestOf`, `Record`), `ProviderRouter.Pick`, `ReviewPolicy.Decide` theo chú thích khung. Không thêm thư viện.

## Phạm vi (được sửa)
- `core/AITeam.Core/Routing/Routing.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- KHÔNG đổi `core/AITeam.Core/Routing/RoutingModels.cs`, `Providers/`, `Review/`, `Jobs/`, `AITeam.Cli/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV407RoutingTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 39 ca nghiệm thu V4-07).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết (gồm 39 ca V4-07) | `dotnet build AITeam.sln`: 0 Warning(s), 0 Error(s). `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m`: `AITeam.Core.Tests` 98/98 pass (39/39 ca `AcceptanceV407RoutingTests`), `AITeam.Hub.Tests` 270/270 pass. |

- **Đã làm:** Hiện thực `QuotaResetParser.Parse` (4 mẫu thời gian, chọn mẫu đầu tiên > `now`), `ProviderStateStore` (`All`, `RestOf`, `Record` ghi tạm rồi `File.Move` UTF-8 không BOM, đọc hỏng trả rỗng), `ProviderRouter.Pick` (kiểm cấu hình/bật/vai trò/cấm tự duyệt/nghỉ), `ReviewPolicy.Decide` (ưu tiên `Review AI` trước `Độ khó: L`).
- **File đã đổi:** `core/AITeam.Core/Routing/Routing.cs` (giữ nguyên UTF-8 BOM), `tasks/V4-07-provider-routing.md`.
- **Điều chưa chắc:** Không có.

## Review (Claude)

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Minh (A) | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
