# F1-01 · Dịch vụ quản lý dự án: thêm / sửa / trạng thái

- **Agent:** C
- **Nhánh:** `agent-c/F1-01-quan-ly-du-an`
- **Ưu tiên:** cao
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-015
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M
- **Review AI:** không (Claude review)

## Tóm tắt
- Chuẩn bị chức năng mới: trang Dự án sẽ thêm / sửa / tạm dừng / lưu trữ được dự án (giao diện Claude làm sau).
- Hiện trang Dự án chỉ liệt kê 3 dòng; sửa thông tin lẫn trong Cài đặt và còn ghi đường dẫn máy vào `projects.json` (dùng chung 2 máy).
- Task này làm phần lõi: đọc / ghi thông tin dự án, kiểm tra dữ liệu nhập, gọi `team.ps1 add-project` an toàn.

## Đọc (CHỈ những file này)
- `hub.tests/AcceptanceF101ProjectAdminTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `hub/Services/ProjectAdminService.cs` (khung do Claude viết)
- `docs/design/R-015-chuc-nang.md` mục F1-01
- `hub/Services/TeamConfigService.cs` (`GetProjects`, `SaveProject`, `WriteWithBackup` — dùng lại cách ghi nguyên tử + .bak)

## Mục tiêu
1. Điền thân `ProjectAdminService` (List, Get, Update, SetStatus, Add) đúng thiết kế mục F1-01.
2. Điền thân `PowerShellProjectScriptRunner.Run`: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <AITeamRoot>\team.ps1` + args qua `ProcessStartInfo.ArgumentList`, stdin rỗng (đóng ngay), đọc stdout + stderr không chặn, quá `timeout` ⇒ kill cả cây, trả mã 124 + output đã có.
3. Đăng ký DI: ĐÃ CÓ trong khung (Claude) — không sửa `HubV3Registration.cs`.

## Phạm vi (được sửa những gì)
- `hub/Services/ProjectAdminService.cs`
- `hub/Services/TeamConfigService.cs` (chỉ nếu cần mở hàm ghi nguyên tử dùng chung; không đổi hành vi cũ)
- `hub.tests/F101ProjectAdminExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-01-quan-ly-du-an.md`

## Không làm
- KHÔNG sửa `hub.tests/AcceptanceF101ProjectAdminTests.cs` và test đang có.
- Không sửa `hub/Pages/**`, `hub/wwwroot/**`, `team.ps1`, `projects.json`, `team.local.json` thật.
- Không thêm chức năng xóa dự án.
- Không sửa `hub/Services/HubV3Registration.cs` (F1-03 đang làm song song).
- Không đổi `ProjectInfo`, `ITeamConfigService`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Dữ liệu nhập từ form web là KHÔNG tin cậy: kiểm tra đúng regex / giới hạn trong thiết kế TRƯỚC khi ghi file hay chạy lệnh; sai ⇒ không ghi gì, không chạy gì.
- Đối số cho `team.ps1` đi qua `ArgumentList` từng phần tử — KHÔNG ghép chuỗi `Arguments`.
- Đường dẫn máy chỉ ghi `team.local.json` → `paths`; giữ mọi khóa khác của 2 file JSON, giữ trường lạ.
- [x] Gọi tiến trình ngoài: giới hạn thời gian (tham số, Hub dùng 10 phút), stdin rỗng, đọc output không chặn.
- [x] Khung có trong commit đầu "Test nghiệm thu F1-01 + khung (Claude)": mọi record / enum / interface / chữ ký trong `ProjectAdminService.cs`. KHÔNG đổi tên / kiểu.
- [x] Đọc / ghi file: chặn reparse point như `TeamConfigService`; ghi cùng khóa `TeamFileLock` với các chỗ ghi `projects.json` / `team.local.json` khác.
- `.cs` giữ UTF-8 có BOM.

## Dữ liệu mẫu thật
- Hằng `ProjectsJson` trong test nghiệm thu: rút gọn từ `projects.json` thật ngày 04/11/2026 (thụt lề kiểu `ConvertTo-Json` của PowerShell).

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceF101ProjectAdminTests.cs`

## Tiêu chí hoàn thành
- [x] Build `AITeam.sln` với `-warnaserror` 0 warning
- [ ] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-01 (không sửa)
- [x] `.cs` giữ BOM UTF-8
- [ ] Commit: `F1-01: dịch vụ quản lý dự án (thêm / sửa / trạng thái)`

## Báo cáo của agent
| Tiêu chí | Bằng chứng thật (lượt sửa cổng kiểm tra) |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln -warnaserror -m:1 -p:UseSharedCompilation=false -v:minimal`: thành công, 0 warning, 0 lỗi (1m50s). |
| Nghiệm thu F1-01 | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~F101ProjectAdmin" --blame-hang-timeout 2m`: 37 pass, 0 fail, 0 skip; không sửa test nghiệm thu. |
| Test toàn solution chưa đạt | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 839 pass/8 fail/0 skip; dừng bằng Ctrl+C khoảng 3 phút, Core chưa có tổng kết, mã thoát 1. |
| Lỗi ngoài F1-01 | Settings/WorkflowRoles/Lessons: `UnauthorizedAccessException` tại `File.Replace`; test Git Core: signal pipe/CreateFileMapping, Win32 error 5. Không mở rộng sửa. |
| BOM UTF-8 | PowerShell đọc 3 byte đầu: ProjectAdminService.cs, TeamConfigService.cs, F101ProjectAdminExtraTests.cs đều EF BB BF. |
| Ngoại lệ phạm vi cần Claude quyết | Giữ `hub/Services/HubV3Registration.cs`: hai đăng ký `IProjectAdminService`/`IProjectScriptRunner` cần để DI dùng dịch vụ thật và test `Services_registered_in_di_and_real_runner_uses_argument_list` pass; xóa sẽ trái nghiệm thu. |
| Giới hạn kiểm chứng phạm vi | Không chạy git nên chưa đối chiếu được bản gốc để hoàn tác chính xác; lượt này không sửa HubV3Registration.cs, đề nghị Claude xét ngoại lệ như yêu cầu cổng cho phép. |
| Commit/push | Không chạy git theo chỉ dẫn Codex; hệ thống tự commit/push, chưa có commit được kiểm chứng. |
File đổi lượt này: chỉ tasks/F1-01-quan-ly-du-an.md, bổ sung lý do ngoại lệ và kết quả chạy lại; giữ nguyên logic và test.
Phần triển khai trước: ProjectAdminService.cs (dịch vụ/runner), TeamConfigService.cs (mở hàm ghi nguyên tử), F101ProjectAdminExtraTests.cs (4 test bổ sung).
Chưa xác minh: tổng kết Core, toàn solution xanh, chênh lệch DI với bản gốc và quyết định ngoại lệ của Claude; không chạy app/chụp ảnh.
## Review (Claude)
### Vòng 1 — ĐẠT (04/11 21:45)
- Cổng KT lượt 1: build + test xanh (Hub 847, Core 1063), 273/320 dòng. Dòng "ngoài phạm vi HubV3Registration.cs" là báo nhầm: 2 dòng DI do Claude thêm vào khung.
- Đọc diff: kiểm tra đầu vào trước khi ghi / chạy, ArgumentList, đường dẫn máy chỉ vào team.local.json, ghi nguyên tử + .bak + giữ BOM, chặn reparse point.
- [CK] cổng KT tính commit khung "(Claude)" sau commit đầu là sửa ngoài phạm vi.
- **Optional:** `Add` giữ khóa cấu hình suốt lúc chạy team.ps1 (tối đa 10 phút) ⇒ trang Dự án có thể chờ; nên nhả khóa trước khi chạy script.

## Kết quả
