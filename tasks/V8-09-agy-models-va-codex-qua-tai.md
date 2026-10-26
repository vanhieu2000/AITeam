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

| Tiêu chí | Lệnh / bằng chứng thật lượt sửa QC |
|---|---|
| Build 0 warning: ĐẠT | `dotnet build AITeam.sln --no-restore -m:1 --nologo`: 0 lỗi, 0 warning; 17,11 giây. |
| agy mã 0/1; Codex retry/fallback/quota, giữ phần dở: ĐẠT | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceV809RetryTests --blame-hang-timeout 2m`: 7 pass, 0 fail, 0 skip. |
| Lỗi QC S104: CHƯA ĐẠT | Cùng lệnh test riêng với filter `FullyQualifiedName~AcceptanceS104SkillPromptsTests.ProviderGiaNhanPromptThat_MoiNhanh`: 2 pass, 3 fail; không tìm thấy `Invoke-CodexRetry`. |
| Test toàn solution: CHƯA ĐẠT | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 712 pass/9 fail (File.Replace bị từ chối quyền); Core chưa tổng kết, đã có lỗi S104 và Git; dừng tiến trình test sau 175 giây bằng Stop-Process (taskkill bị sandbox từ chối). |
| Cổng verify: CHƯA ĐẠT | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: 2 kiểm tra tĩnh đạt; kịch bản 1 lỗi Git CreateFileMapping, Win32 error 5 (sandbox). |
- Chỉ đổi phiếu này để cập nhật bằng chứng; giữ nguyên mã sản phẩm, test nghiệm thu và mã hóa/BOM ban đầu.
- Chặn: TeamScript chỉ nạp hàm được chọn, không tự nạp phụ thuộc; bộ test S104 chưa nạp `Invoke-CodexRetry`. Cần Claude sửa danh sách phụ thuộc của test; không đổi test hoặc viết mã nhận ra test để né lỗi (AGENTS.md, dòng cuối).
- Quyết định: dừng sửa mã theo quy tắc “Test sai thì ghi vào báo cáo và dừng”; không mở rộng phạm vi sang harness/test cũ. Chưa xác minh hết lỗi V802/V803 do giới hạn thời gian và sandbox Git.
- Không chạy lệnh git trực tiếp; hệ thống tự commit/push. Chưa có commit được kiểm; không chạy app/chụp ảnh theo phân công.
## Review (Claude)

## Kết quả