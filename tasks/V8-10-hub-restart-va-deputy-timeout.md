# V8-10 · `team.ps1 hub -Restart` + giới hạn 45 phút cho `deputy`

- **Agent:** B
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

| Tiêu chí | Lệnh và bằng chứng thật |
|---|---|
| Restart đúng Hub, bỏ worktree khác; chưa có Hub thì chạy mới | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceV810HubRestartTests" --blame-hang-timeout 2m`: 3 pass, 0 fail, 0 skip (2 ca Hub, 1 ca deputy). |
| Deputy quá hạn: dừng cây, nhật ký, bỏ khóa | Test trên pass; giới hạn mặc định 2700 giây, test truyền 1 giây; kiểm PID + `/T /F`, hai tiến trình giả đã dừng, nhật ký giữ BOM/nội dung cũ. |
| Build 0 warning | `dotnet build AITeam.sln -m:1 --nologo -v minimal`: thành công, 0 lỗi nhưng 8 warning NU1900 vì không truy cập api.nuget.org; CHƯA ĐẠT 0 warning. |
| Test toàn solution | `dotnet test AITeam.sln --no-restore -m:1 --blame-hang-timeout 2m --nologo -v minimal`: Hub 712 pass/9 fail/0 skip; Core chưa có tổng kết, đã dừng khi quá 3 phút; lỗi File.Replace bị từ chối, Git CreateFileMapping error 5 và test hook ngoài phạm vi. |
| Cổng kiểm tra | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: exit 1, kiểm tra tĩnh ĐẠT, kịch bản Git clone bị sandbox chặn CreateFileMapping error 5. |
| BOM/cú pháp | Kiểm byte: team.ps1 và file test có BOM UTF-8; PowerShell Parser: 0 lỗi. |
- File đổi: `team.ps1` (restart, chờ dừng tối đa 10 giây/chờ cổng 90 giây, timeout deputy); `core.tests/AcceptanceV810HubRestartTests.cs` (3 ca mới); phiếu này (báo cáo).
- Tự quyết: cổng còn bận sau dừng thì báo lỗi; fake taskkill kiểm đúng lệnh rồi dừng tiến trình giả, vì taskkill thật bị sandbox từ chối.
- Chưa xác minh: taskkill /T thật ngoài sandbox, Hub/Codex thật; không chạy app/chụp ảnh theo phân công; không sửa lỗi ngoài phạm vi.
- Không chạy git trực tiếp; hệ thống chịu trách nhiệm commit/push, chưa có commit được kiểm.
## Review (Claude)

## Kết quả