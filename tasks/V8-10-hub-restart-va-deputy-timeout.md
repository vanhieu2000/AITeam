# V8-10 · `team.ps1 hub -Restart` + giới hạn 45 phút cho `deputy`

- **Agent:** A
- **Nhánh:** `agent-a/V8-10-hub-restart-va-deputy-timeout`
- **Ưu tiên:** cao
- **Ngân sách:** ~70 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 20 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới cho người dùng Hub; công cụ vận hành.
- (1) `team.ps1 hub` chỉ mở trình duyệt nếu Hub đang chạy ⇒ sau merge không thấy bản mới. Thêm `-Restart`: dừng Hub đang chạy (CHỈ tiến trình có đường dẫn trong `<AITeam>\hub\bin\` hoặc dòng lệnh chứa `<AITeam>\hub\bin\…AITeam.Hub.dll`), build, chạy lại, chờ cổng 5057 mở (tối đa 90 giây).
- (2) `team.ps1 deputy` gọi Codex không giới hạn ⇒ 25–26/10 treo 19 giờ. Thêm giới hạn 45 phút: quá hạn ⇒ dừng cây tiến trình Codex của lượt đó, ghi "Lượt quản lý quá 45 phút — đã dừng" vào QUAN-LY-PHU.md mục nhật ký.

## Đọc (CHỈ những file này)
- `team.ps1` (Invoke-Hub, Invoke-Deputy)
- `core.tests/Support/TeamScript.cs`, `core.tests/Support/TestShell.cs`

## Phạm vi (được sửa những gì)
- `team.ps1`
- `core.tests/AcceptanceV810HubRestartTests.cs`

## Không làm
- Không dừng tiến trình ngoài `<AITeam>\hub\bin` (không đụng Hub chạy từ worktree khác, HDV Tool hay tiến trình người dùng).
- Không đổi cổng 5057.

## Test nghiệm thu
- `core.tests/AcceptanceV810HubRestartTests.cs` (Codex viết cùng lượt, TeamScript + fake): chọn đúng tiến trình theo đường dẫn (bỏ qua Hub ở worktree khác); không có Hub ⇒ chỉ chạy mới; deputy fake chạy quá hạn (giới hạn rút ngắn qua tham số test) ⇒ bị dừng + ghi nhật ký.

## Tiêu chí hoàn thành
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

## Review (Claude)

## Kết quả