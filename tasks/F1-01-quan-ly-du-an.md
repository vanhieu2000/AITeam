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
- `hub/Services/HubV3Registration.cs` (đăng ký DI)

## Mục tiêu
1. Điền thân `ProjectAdminService` (List, Get, Update, SetStatus, Add) đúng thiết kế mục F1-01.
2. Điền thân `PowerShellProjectScriptRunner.Run`: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <AITeamRoot>\team.ps1` + args qua `ProcessStartInfo.ArgumentList`, stdin rỗng (đóng ngay), đọc stdout + stderr không chặn, quá `timeout` ⇒ kill cả cây, trả mã 124 + output đã có.
3. Đăng ký DI: `IProjectScriptRunner` → `PowerShellProjectScriptRunner`, `IProjectAdminService` → `ProjectAdminService` (singleton, `TryAdd`).

## Phạm vi (được sửa những gì)
- `hub/Services/ProjectAdminService.cs`
- `hub/Services/HubV3Registration.cs`
- `hub/Services/TeamConfigService.cs` (chỉ nếu cần mở hàm ghi nguyên tử dùng chung; không đổi hành vi cũ)
- `hub.tests/F101ProjectAdminExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-01-quan-ly-du-an.md`

## Không làm
- KHÔNG sửa `hub.tests/AcceptanceF101ProjectAdminTests.cs` và test đang có.
- Không sửa `hub/Pages/**`, `hub/wwwroot/**`, `team.ps1`, `projects.json`, `team.local.json` thật.
- Không thêm chức năng xóa dự án.
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
- [ ] Build `AITeam.sln` với `-warnaserror` 0 warning
- [ ] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-01 (không sửa)
- [ ] `.cs` giữ BOM UTF-8
- [ ] Commit: `F1-01: dịch vụ quản lý dự án (thêm / sửa / trạng thái)`

## Báo cáo của agent

## Review (Claude)

## Kết quả
