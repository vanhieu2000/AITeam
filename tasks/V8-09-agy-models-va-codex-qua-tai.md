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

| Tiêu chí | Lệnh / bằng chứng thật |
|---|---|
| Build 0 warning: ĐẠT | `dotnet build AITeam.sln --no-restore -m:1 --nologo`: 0 lỗi, 0 warning (restore tuần tự đã thành công; build mặc định ban đầu thất bại). |
| agy mã 0/1; Codex quá tải, fallback, quota, giữ phần dở: ĐẠT | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceV809RetryTests" --blame-hang-timeout 2m`: 7 pass, 0 fail, 0 skip. |
| Test toàn solution: CHƯA ĐẠT | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 712 pass/9 fail; Core chưa tổng kết, đã dừng khi vượt 3 phút. |
| Cổng verify: CHƯA ĐẠT | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: kiểm tra tĩnh ĐẠT; kịch bản 1 lỗi Git CreateFileMapping/Win32 error 5 trong sandbox. |
- File đổi: `team.ps1` giữ Handle, dùng chung retry prep/dev/QC; `team.json` thêm mảng fallback rỗng; `core.tests/AcceptanceV809RetryTests.cs` thêm 7 ca; phiếu này ghi báo cáo.
- Giữ BOM UTF-8 của `team.ps1`; test mới có BOM; `team.json` và phiếu giữ mã hóa ban đầu.
- Quyết định: nhận diện quá tải từ stdout/stderr và JSON kết quả; quota ưu tiên chặn retry; không xóa/reset worktree, chỉ thay file kết quả lượt chạy.
- Cần Claude review: S104 báo không tìm thấy `Invoke-CodexRetry` (nghi harness chưa nạp phụ thuộc mới); giữ nguyên test cũ. Hub lỗi File.Replace bị từ chối quyền; Core còn lỗi Git/timeout và S106 assertion.
- Chưa xác minh agent thật và hiệu lực `--model` qua CLI thật theo giới hạn không chạy app; toàn solution/cổng verify cần chạy lại ngoài sandbox.
- Không chạy lệnh git theo chỉ dẫn Codex dự phòng; hệ thống tự commit/push, chưa có commit được kiểm.

## Review (Claude)

## Kết quả