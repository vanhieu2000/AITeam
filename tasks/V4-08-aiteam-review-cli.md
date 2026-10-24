# V4-08 · Lệnh `aiteam review`: chọn reviewer, thử reviewer kế khi hết hạn mức, mã thoát cho team.ps1

- **Agent:** B
- **Nhánh:** `agent-b/V4-08-aiteam-review-cli`
- **Ưu tiên:** cao
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-002
- **Độ khó:** M
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — lệnh để `team.ps1 assign` tự gọi AI review sau khi cổng kiểm tra đạt.
- Reviewer luôn khác AI đã code; reviewer hết hạn mức ⇒ đánh dấu nghỉ và thử reviewer kế (tối đa 2).
- Kết quả rõ ràng: PASS / CẦN SỬA (kèm danh sách lỗi để giao lại) / BLOCKED (kèm lý do) — lỗi bất kỳ không bao giờ thành PASS.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Review/ReviewRunner.cs` (khung — điền `TODO(agent)`)
- `core/AITeam.Cli/Program.cs` (thêm lệnh `review` theo mục Mục tiêu 2)
- `core/AITeam.Core/Review/ReviewService.cs`, `core/AITeam.Core/Review/ReviewModels.cs`, `core/AITeam.Core/Routing/RoutingModels.cs`, `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API)
- `core.tests/AcceptanceV408ReviewRunnerTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V4-MULTI-AI.md` mục B4, B5, C (P0-3, P0-7). V4-07 (định tuyến) đã merge.

## Ràng buộc
- Fail-safe: lỗi / quá giờ / không đọc được ⇒ BLOCKED, KHÔNG BAO GIỜ PASS. Không tự gọi reviewer trùng developer.
- Giờ: chỉ dùng `now()` truyền vào (test được). Không gọi provider khi đầu vào thiếu.
- CLI: chỉ đọc file người gọi đưa; không chạy git, không đọc file ngoài các tham số.

## Mục tiêu
1. Điền `ReviewRunner.RunAsync`, `Summarize`, `Feedback`, `OutcomeJson` theo chú thích khung (lớp bọc provider ghi `ProviderStateStore` là lớp `private` trong cùng file).
2. `Program.cs` thêm lệnh:
   `aiteam review --task-file <f> --diff-file <f> [--verify-file <f>] --workdir <d> --logdir <d> --dev <id> [--timeout-min 15] [--out <f>] [--auto]`
   - thiếu `--task-file` / `--diff-file` / `--workdir` / `--logdir` / `--dev` ⇒ in lỗi, trả 2;
   - `--auto` ⇒ đọc file task, `ReviewPolicy.Decide`; AutoReview = false ⇒ in "SKIP: {Reason}", ghi `--out` (nếu có) JSON `{"exitCode":4,"verdict":"SKIPPED","summary":"SKIP: …","notes":[],"feedback":"","provider":null,"diffHash":null}`, trả 4;
   - ngược lại: `ProviderSettings.Load(root)`, `ProviderStateStore(root)`, `ProviderRouter`, factory = `settings.Create(c, new ProcessRunner())`, now = `DateTime.Now`; chạy `ReviewRunner`; in `Summary`, mỗi Note "  - {note}", rồi `Feedback` nếu có; ghi `--out` bằng `OutcomeJson` (UTF-8 không BOM); trả `ExitCode`.
   - Thêm dòng hướng dẫn lệnh `review` vào `Help()`.

## Phạm vi (được sửa)
- `core/AITeam.Core/Review/ReviewRunner.cs`
- `core/AITeam.Cli/Program.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `ReviewService.cs`, `ReviewModels.cs`, `Routing/`, `Providers/`, `Jobs/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV408ReviewRunnerTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 9 ca nghiệm thu V4-08).
- [x] `aiteam review` không đủ tham số trả 2; `--auto` với task độ khó M trả 4 và in "SKIP: …" (agent thử bằng file tạm, KHÔNG gọi Codex thật).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln` pass hết | `dotnet build AITeam.sln` → 0 Warning, 0 Error; `dotnet test AITeam.sln --blame-hang-timeout 2m` → Core.Tests 108/108 pass (9/9 ca nghiệm thu V4-08), Hub.Tests 270/270 pass |
| `aiteam review` thiếu tham số trả 2; `--auto` với task độ khó M trả 4, in `SKIP: …` | Kiểm thử trong `ReviewCliTests.Missing_args_returns_2_and_auto_medium_task_returns_4_with_skip_json`: gọi `aiteam.dll review` trên file tạm trả 2 và 4 (`SKIP: Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)` + `--out` JSON `SKIPPED`) |
|---|---|
| | |

- **Đã làm:** Hoàn thiện `ReviewRunner` (`RunAsync`, `Summarize`, `Feedback`, `OutcomeJson`, lớp bọc `StateRecordingProvider` ghi `ProviderStateStore`), thêm lệnh `aiteam review` vào `Program.cs` (cập nhật `Help()`, kiểm tra `--auto` qua `ReviewPolicy.Decide`, chạy `ReviewRunner`, xuất JSON UTF-8 không BOM) và thêm test CLI `ReviewCliTests.cs`.
- **File đã đổi:** `core/AITeam.Core/Review/ReviewRunner.cs`, `core/AITeam.Cli/Program.cs`, `core.tests/ReviewCliTests.cs`, `tasks/V4-08-aiteam-review-cli.md`.
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
| 5. Thực hiện code | Lan (B) | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
