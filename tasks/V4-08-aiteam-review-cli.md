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
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 9 ca nghiệm thu V4-08).
- [ ] `aiteam review` không đủ tham số trả 2; `--auto` với task độ khó M trả 4 và in "SKIP: …" (agent thử bằng file tạm, KHÔNG gọi Codex thật).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| | |

- **Đã làm:**
- **File đã đổi:**
- **Điều chưa chắc:**

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
