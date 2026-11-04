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
| Tiêu chí | Bằng chứng thật |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln -warnaserror --no-restore -m:1 -p:UseSharedCompilation=false -v:minimal`: thành công, 0 warning, 0 lỗi. |
| Test nghiệm thu nguyên vẹn | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~F101ProjectAdmin" --blame-hang-timeout 2m`: 37 pass, 0 fail, 0 skip (33 nghiệm thu + 4 bổ sung). |
| Test toàn solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 839 pass/8 fail/0 skip; dừng khoảng 3 phút, Core chưa có tổng kết. |
| Nguyên nhân chưa xanh toàn bộ | Ngoài F1-01: Settings/WorkflowRoles/Lessons bị `UnauthorizedAccessException` tại `File.Replace`; test Git của Core lỗi signal pipe/CreateFileMapping, Win32 error 5. |
| BOM UTF-8 | Kiểm bằng Python `read_bytes().startswith(b'\xef\xbb\xbf')`: cả 3 file C# đã đổi đều True. |
| Commit/push | Không chạy lệnh git theo chỉ dẫn Codex; hệ thống tự commit/push, chưa có commit được kiểm chứng. |
Đã đổi: `ProjectAdminService.cs` điền 5 hàm, kiểm dữ liệu, khóa chung, giữ trường lạ, runner ArgumentList/stdin đóng/đọc đồng thời/kill cây và mã 124.
Đã đổi: `TeamConfigService.cs` chỉ mở `WriteWithBackup` thành internal và dùng file tạm + đổi tên nguyên tử, giữ BOM/.bak; không sửa SaveSettings/SaveProject.
Đã thêm: `F101ProjectAdminExtraTests.cs` kiểm cấu hình hỏng không ghi, BOM/.bak và runner thật (stdin/output/timeout); test chỉ dùng dữ liệu tạm.
Tự quyết: dùng `File.Move(..., overwrite: true)` cùng thư mục vì sandbox chặn `File.Replace`; tắt compiler server sau lỗi named pipe, dùng `-m:1` vì build mặc định thoát mã 1 không chẩn đoán.
Chưa xác minh: toàn bộ Core, môi trường ngoài sandbox và commit/push; không chạy app/chụp ảnh, không sửa test nghiệm thu/giao diện.

## Review (Claude)

## Kết quả
