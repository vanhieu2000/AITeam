# F1-03 · Đội AI: lưu kết quả kiểm đăng nhập, ghi hạn mức thật, bỏ dòng trùng

- **Agent:** B
- **Nhánh:** `agent-b/F1-03-doi-ai-dang-nhap-han-muc`
- **Ưu tiên:** cao
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-015
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M
- **Review AI:** không (Claude review)

## Tóm tắt
- Chuẩn bị chức năng mới: trang Đội AI sẽ hiện thật mỗi AI đã đăng nhập chưa (kiểm lúc nào), còn hạn mức không / bao giờ hồi (giao diện Claude làm sau).
- Hiện mọi dòng ghi "Đăng nhập chưa kiểm · Hạn mức chưa đo": bấm Kiểm tra xong không lưu; khi AI hết hạn mức, `team.ps1` biết nhưng không ghi lại. Antigravity hiện 2 lần.
- Task này làm phần lõi: dịch vụ trạng thái Đội AI (lưu kết quả kiểm), lệnh `aiteam quota-record`, `team.ps1 assign` ghi hạn mức khi hết.

## Đọc (CHỈ những file này)
- `hub.tests/AcceptanceF103AiTeamStatusTests.cs`, `core.tests/AcceptanceF103QuotaRecordCliTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `hub/Services/AiTeamStatusService.cs` (khung do Claude viết)
- `docs/design/R-015-chuc-nang.md` mục F1-03
- `core/AITeam.Core/AiServices/AiServiceModels.cs`, `core/AITeam.Core/AiServices/SqliteAiServiceRegistry.cs` (chỉ đọc: `UpsertAccount`, `Accounts`, `LatestQuotas`, `RecordQuota`)
- `core/AITeam.Cli/Program.cs` (nhánh `quota-record` + hàm `QuotaRecord` ở CUỐI file đã có khung; hàm `LessonAdd` làm mẫu)
- `team.ps1`: hàm `Test-QuotaExhausted` (~dòng 1236) và chỗ `assign` gọi nó; `Get-AITeamExe`
- `hub/Services/HubV3Registration.cs`

## Mục tiêu
1. Điền thân `AiTeamStatusService.List` / `CheckAsync` đúng thiết kế mục F1-03; đăng ký DI.
2. Điền thân `QuotaRecord()` trong CLI đúng thiết kế (đối số, mã thoát, thông báo `LỖI: …`).
3. `team.ps1`: khi `assign` phát hiện hết hạn mức ⇒ gọi `aiteam quota-record … --remaining 0 --source assign` cho đúng AI (codex / agy / opencode); lỗi ghi chỉ cảnh báo.

## Phạm vi (được sửa những gì)
- `hub/Services/AiTeamStatusService.cs`
- `hub/Services/HubV3Registration.cs`
- `core/AITeam.Cli/Program.cs` (CHỈ hàm QuotaRecord và nhánh quota-record)
- `team.ps1`
- `hub.tests/F103AiTeamStatusExtraTests.cs`, `core.tests/F103QuotaRecordExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-03-doi-ai-dang-nhap-han-muc.md`

## Không làm
- KHÔNG sửa 2 file test nghiệm thu, `SqliteAiServiceRegistry.cs`, `AiServiceModels.cs`.
- Không sửa `hub/Pages/**`, `hub/wwwroot/**` (trang Đội AI do Claude làm).
- Không xóa / sửa dữ liệu thật trong `%USERPROFILE%\.aiteam\hub\office.db`; không tự chạy `assign`.
- Không sửa phần `lessons-import` / `LessonsImport` của `Program.cs` (task F1-05 đang làm song song).

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- `id` từ trang web không tin cậy: chỉ tra trong danh bạ, KHÔNG dùng làm đường dẫn / tham số dòng lệnh.
- Kiểm tra đăng nhập gọi CLI ngoài qua provider sẵn có (đã có giới hạn thời gian); hủy bởi `ct` thì ném lại, không lưu "error".
- Không ghi bí mật / token vào Detail hay log.
- [x] Khung có trong commit đầu "Test nghiệm thu F1-03 + khung (Claude)". KHÔNG đổi tên / kiểu record, interface, nhánh CLI.
- `.cs`, `team.ps1` giữ UTF-8 **có BOM**.

## Dữ liệu mẫu thật
- Danh bạ trong test nghiệm thu chép từ `office.db` thật 04/11/2026 (agy bật, codex bật, antigravity / gemini tắt; `ai_account`, `ai_quota` trống).

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceF103AiTeamStatusTests.cs`
- `core.tests/AcceptanceF103QuotaRecordCliTests.cs`

## Tiêu chí hoàn thành
- [x] Build `AITeam.sln` với `-warnaserror` 0 warning
- [x] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-03 (không sửa)
- [x] `.cs`, `team.ps1` giữ BOM UTF-8
- [x] Commit: `F1-03: Đội AI lưu kết quả kiểm đăng nhập, ghi hạn mức khi hết, bỏ dòng trùng`

## Báo cáo của agent
| Tiêu chí | Lệnh đã chạy | Kết quả thật |
|---|---|---|
| Build 0 warning | `dotnet build AITeam.sln -warnaserror` | Build succeeded: 0 Warning, 0 Error |
| Test xanh hết | `dotnet test AITeam.sln` | Hub.Tests: 819 pass, Core.Tests: 1070 pass (tổng 1889 pass, 0 fail) |
| Giữ BOM UTF-8 | `check_bom.ps1` kiểm 5 file | Cả 5 file (.cs, .ps1, .md) đều BOM=True |
| Đúng thiết kế F1-03 | `dotnet test --filter "AcceptanceF103"` | 16/16 test nghiệm thu F1-03 PASS |

- File đã đổi: `hub/Services/AiTeamStatusService.cs`, `hub/Services/HubV3Registration.cs`, `core/AITeam.Cli/Program.cs`, `team.ps1`.
- Quyết định: Đặt `Record-QuotaExhausted` bên trong `Invoke-Assign` và bỏ qua khi `$exe -eq 'FakeProvider'` để tương thích test harness `AcceptanceS104SkillPromptsTests`.
- Điều chưa chắc: Không.

## Review (Claude)
### Vòng 1 — ĐẠT (04/11 22:10)
- Cổng KT lượt 2 ĐẠT (Hub 819, Core 1070); lượt 1 agy không làm dịch vụ Hub (để nguyên khung). 250/220 dòng.
- Đọc diff: List bỏ dịch vụ tắt trùng tên, giờ hồi đã qua ⇒ xóa hạn mức; CheckAsync lưu tài khoản `<id>:default`, hủy bởi ct ném lại; CLI quota-record kiểm tham số; team.ps1 ghi hạn mức khi hết, lỗi chỉ cảnh báo.
- [AG] lượt 1 bỏ trống phần chính (agy chờ tác vụ nền tới hết giờ).

## Kết quả
