# V8-09 · Sửa đọc mã thoát `agy models` + Codex quá tải tự thử lại

- **Agent:** A
- **Nhánh:** `agent-a/V8-09-agy-models-va-codex-qua-tai`
- **Ưu tiên:** cao
- **Ngân sách:** ~80 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 20 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới.
- (1) V8-06 in "Giữ model cũ: agy models trả mã ." — `Start-Process -PassThru` trong PS 5.1 trả ExitCode rỗng nếu không giữ `$proc.Handle` trước khi chờ ⇒ luôn giữ model cũ (H7-03 chạy flash-medium).
- (2) Codex báo "Selected model is at capacity" (26/10: W1-01 dev, H7-02 prep) ⇒ hiện báo lỗi luôn, phải giao tay lại.

## Đọc (CHỈ những file này)
- `team.ps1` (Get-AgyModels; nơi gọi `aiteam run --provider codex` cho prep / dev / QC)
- `core.tests/Support/TestShell.cs`, `core.tests/Support/TeamScript.cs`

## Mục tiêu
1. Get-AgyModels: lấy `$null = $proc.Handle` ngay sau Start-Process (trước WaitForExit) để ExitCode có giá trị; mã ≠ 0 mới coi là lỗi.
2. Mọi lượt Codex (prep, dev, QC): kết quả chứa `at capacity` hoặc `overloaded` hoặc mã 429 không phải quota ⇒ chờ 60 giây, thử lại 1 lần; vẫn lỗi ⇒ thử 1 lần với model đầu tiên trong `team.json` → `codex.modelFallback` (mảng, mặc định rỗng = bỏ bước này); ghi 1 dòng "Codex quá tải ⇒ thử lại …".
3. Giữ phần dở: không xóa / reset worktree giữa các lần thử.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `team.json`
- `core.tests/AcceptanceV809RetryTests.cs`

## Test nghiệm thu
- `core.tests/AcceptanceV809RetryTests.cs` (Codex viết cùng lượt, dùng TeamScript + fake): agy models fake mã 0 ⇒ đọc danh sách; mã 1 ⇒ giữ model cũ; codex fake trả "at capacity" lần 1, OK lần 2 ⇒ thành công + 1 dòng thông báo; 2 lần quá tải + modelFallback ⇒ lần 3 dùng model dự phòng; quota thật (usage limit) ⇒ KHÔNG thử lại.

## Tiêu chí hoàn thành
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

## Review (Claude)

## Kết quả