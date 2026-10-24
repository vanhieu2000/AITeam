# V4-09 · Lệnh `aiteam run`: chạy Codex (hoặc provider khác) viết code trong worktree

- **Agent:** A
- **Nhánh:** `agent-a/V4-09-aiteam-run-dev`
- **Ưu tiên:** cao
- **Ngân sách:** ~170 dòng
- **Yêu cầu:** R-004
- **Độ khó:** M
- **Review AI:** có
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — lệnh để `team.ps1 assign` giao task cho Codex khi Antigravity hết hạn mức (hoặc khi chủ dự án chọn `-Dev codex`).
- Codex hết hạn mức ⇒ đánh dấu nghỉ đúng giờ; đang nghỉ thì không gọi.
- Kết quả rõ ràng bằng mã thoát + file JSON để `team.ps1` tự commit, push và kiểm tra tiếp.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Providers/DevRunner.cs` (khung — điền `TODO(agent)`)
- `core/AITeam.Cli/Program.cs` (thêm lệnh `run` theo Mục tiêu 2; xem lệnh `review` làm mẫu)
- `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API: `AgentRunRequest`, `ProviderStateStore`)
- `core.tests/AcceptanceV409DevRunnerTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V4-MULTI-AI.md` mục B2 (Developer: agy → codex). Thử thật 24/10: Codex `--sandbox workspace-write` sửa file + build + test được, nhưng KHÔNG `git add/commit` được (sandbox chặn ghi `.git` của worktree) ⇒ team.ps1 commit thay.

## Ràng buộc
- Không gọi provider khi đầu vào sai hoặc provider đang nghỉ. Giờ chỉ qua `now()` truyền vào.
- Lỗi bất ngờ ⇒ kết quả `Failed` có lý do, không làm sập lệnh (bài học V4-08).
- CLI: `Arg` đã từ chối giá trị bắt đầu "--" — giữ nguyên.

## Mục tiêu
1. Điền `DevRunner.RunAsync` và `OutcomeJson` theo chú thích khung.
2. `Program.cs` thêm lệnh:
   `aiteam run --provider <id> --workdir <d> --prompt-file <f> --logdir <d> [--timeout-min 30] [--model <m>] [--out <f>]`
   - thiếu `--provider` / `--workdir` / `--prompt-file` / `--logdir` ⇒ in lỗi ra stderr, trả 2;
   - `ProviderSettings.Load(root)`, `ProviderStateStore(root)`, factory = `settings.Create(c, new ProcessRunner())`, now = `DateTime.Now`;
   - in `Summary`; ghi `--out` (UTF-8 không BOM) bằng `OutcomeJson`; trả `ExitCode`.
   - Thêm dòng hướng dẫn vào `Help()`.

## Phạm vi (được sửa)
- `core/AITeam.Core/Providers/DevRunner.cs`
- `core/AITeam.Cli/Program.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `CodexProvider.cs`, `AgyProvider.cs`, `ProviderModels.cs`, `ProviderConfig.cs`, `Routing/`, `Review/`, `hub/`, `team.ps1`. KHÔNG gọi Codex / agy thật trong test.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV409DevRunnerTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 11 ca nghiệm thu V4-09).
- [x] `aiteam run` thiếu tham số trả 2 (thử bằng dòng lệnh, KHÔNG gọi provider thật).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 11 ca nghiệm thu V4-09) | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)`; `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → `Passed! 124 (Core.Tests) + 276 (Hub.Tests) = 400 passed, 0 failed` (11/11 `AcceptanceV409DevRunnerTests` + 2/2 `DevRunnerCliTests`). |
| `aiteam run` thiếu tham số trả 2 (thử bằng dòng lệnh, KHÔNG gọi provider thật) | `dotnet core/AITeam.Cli/bin/Debug/net10.0-windows/aiteam.dll run --provider codex` → stderr `LỖI: thiếu tham số bắt buộc (--provider, --workdir, --prompt-file, --logdir).`, `ExitCode=2`. |

- **Đã làm:** Hiện thực `DevRunner.RunAsync` (kiểm tra cấu hình/vai trò/file/workDir/trạng thái nghỉ, chạy provider ở `RunMode.Develop`, ghi `ProviderStateStore.Record`, tạo `Summary` và bắt ngoại lệ bất ngờ trả `Failed`) cùng `DevRunner.OutcomeJson`; thêm lệnh `aiteam run` và dòng `Help()` trong `Program.cs`; thêm test CLI `DevRunnerCliTests.cs`.
- **File đã đổi:** `core/AITeam.Core/Providers/DevRunner.cs`, `core/AITeam.Cli/Program.cs`, `core.tests/DevRunnerCliTests.cs`, `tasks/V4-09-aiteam-run-dev.md`.
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
| 6. Review / QC | Cổng KT + Codex + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
