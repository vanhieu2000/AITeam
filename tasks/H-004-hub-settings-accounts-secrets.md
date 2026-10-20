# H-004 · Hub: Cài đặt, Tài khoản, Bí mật + lệnh `team.ps1 hub`

- **Agent:** A
- **Nhánh:** `agent-a/H-004-hub-settings`
- **Ưu tiên:** trung bình. Giao **sau** H-003 vì cả hai cùng sửa `_Layout` / CSS.

## Bối cảnh
- Thiết kế: `docs/HUB-DESIGN.md` mục 4.5, 4.6, 4.7 và mục 5.
- Dịch vụ đã có (H-001): `ITeamConfigService.SaveSettings / SaveProject`, `IHealthService` (`CheckAgyLoginAsync`, `OpenSwitchAccountWindow`, `InstallClaudeIntegration`), `IVaultService`.

## Mục tiêu
1. **`/settings`**: form sửa
   - `DefaultModel`, `MaxReviewRounds`, chuyên môn của từng developer;
   - `ProjectsRoot`, `WorkRoot`, `AgyExe` (ghi vào `team.local.json`);
   - mỗi dự án: `build`, `test`, `stack`, `areas`.
   - Kiểm tra đầu vào: `MaxReviewRounds` 1–10; `AgyExe` phải là đường dẫn đầy đủ tới `agy.exe` có thật (dùng `HealthService.IsValidAgyExe`); chuỗi không chứa ký tự xuống dòng. Lưu xong báo "Đã lưu (có bản .bak)".
2. **`/accounts`**:
   - GitHub: kết quả từ `IHealthService.Check()`;
   - Antigravity: nút "Kiểm tra đăng nhập" (HTMX, có trạng thái chờ ≤ 2 phút) và nút "Đổi tài khoản", kèm hướng dẫn `/logout` → đăng nhập → `/quit`; danh sách nhãn tài khoản;
   - Claude: trạng thái skill / CLAUDE.md + nút "Cài / sửa".
3. **`/secrets`**:
   - danh sách TÊN; form thêm (tên + `type=password autocomplete=off`); xóa có xác nhận;
   - form "Xuất .env.local": chọn dự án + tick tên;
   - lỗi hiện bằng tiếng Việt (kho hỏng, chưa gitignore, tên sai).
   - **Không có đường nào trả giá trị bí mật về trình duyệt; không ghi log giá trị.**
4. **`team.ps1 hub`**: build Hub nếu chưa có bản build hoặc mã nguồn mới hơn, chạy ở nền (cửa sổ ẩn), đợi cổng 5057 mở, rồi mở trình duyệt. Chạy lại khi Hub đang chạy thì chỉ mở trình duyệt.

## Phạm vi (được sửa)
- `hub/Pages/Settings.*`, `hub/Pages/Accounts.*`, `hub/Pages/Vault.*` (route `/secrets`), `hub/wwwroot/css/hub.css` (chỉ thêm), `hub.tests/`, `team.ps1` (chỉ thêm lệnh `hub`, lưu UTF-8 có BOM), `README.md` (mục lệnh).

## Không làm
- Không sửa `hub/Services/` trừ khi thiếu thật sự (ghi lý do), không sửa `Contracts.cs` / `HubModels.cs`.

## Tiêu chí hoàn thành
- [x] Build 0 warning; test pass. Test PageModel:
  - Settings: dữ liệu sai bị từ chối, không ghi file;
  - Secrets: thêm / xóa / xuất gọi đúng dịch vụ; response và HTML không chứa giá trị bí mật (test bằng chuỗi giá trị đặc biệt);
  - mọi POST thiếu antiforgery bị từ chối.
- [x] Chạy `HUB_FAKE=1`: 3 trang hiển thị đúng. KHÔNG bấm lưu / bí mật trên dữ liệu thật khi test.
- [x] `team.ps1 hub` chạy được; ghi cách đã thử vào báo cáo.

## Báo cáo của agent
### 1. Đã làm gì
- **Trang Cài đặt (`/settings`)**: Xây dựng `SettingsModel` và Razor view `Settings.cshtml`. Hỗ trợ cấu hình `team.json` / `team.local.json` (`DefaultModel`, `MaxReviewRounds`, `ProjectsRoot`, `WorkRoot`, `AgyExe`, chuyên môn developer) và `projects.json` (`Stack`, `Build`, `Test`, `Areas`). Kiểm tra đầu vào nghiêm ngặt: `MaxReviewRounds` trong khoảng 1–10; `AgyExe` phải trỏ tới file `agy.exe` có thật (dùng `HealthService.IsValidAgyExe`); kiểm tra toàn bộ chuỗi không chứa ký tự xuống dòng (`\r`, `\n`). Sau khi lưu thông báo "Đã lưu (có bản .bak)".
- **Trang Tài khoản (`/accounts`)**: Xây dựng `AccountsModel` và view `Accounts.cshtml`, kèm partial view `_AgyLoginResult.cshtml`. Hiển thị trạng thái GitHub CLI và owner từ `team.json`; trạng thái Antigravity CLI, danh sách nhãn tài khoản, nút "Kiểm tra đăng nhập" bằng HTMX (có spinner chờ, trả kết quả partial), nút "Đổi tài khoản" (mở cửa sổ kèm hướng dẫn `/logout` → đăng nhập → `/quit`); khối Claude tích hợp kèm nút "Cài / sửa" (`InstallClaudeIntegration`).
- **Trang Bí mật (`/secrets`)**: Xây dựng `VaultModel` và view `Vault.cshtml`. Chỉ hiển thị tên bí mật trong kho; form thêm với ô mật khẩu `type=password autocomplete=off`; nút xóa có popup xác nhận; form xuất `.env.local` cho từng dự án với thông báo lỗi tiếng Việt (kho hỏng, chưa gitignore, tên sai). Đảm bảo giá trị bí mật không bao giờ lọt vào HTML, response, log, ViewData hay ModelState.
- **Lệnh `team.ps1 hub`**: Bổ sung hàm `Test-PortOpen` và `Invoke-Hub` vào `team.ps1`. Tự động kiểm tra cổng 5057; nếu chưa chạy thì so sánh thời gian file nguồn với file dll để build nếu cần, khởi chạy tiến trình ở nền (cửa sổ ẩn), đợi cổng 5057 mở và mở trình duyệt. Nếu Hub đang chạy thì chỉ mở trình duyệt. Lưu file dưới định dạng UTF-8 có BOM.
- **Tài liệu `README.md`**: Cập nhật mục 3 "Vận hành hằng ngày" bổ sung dòng lệnh `.\team.ps1 hub`.
- **Giao diện `hub.css`**: Thêm các class giao diện form, alert, thẻ card, bảng bí mật, badge trạng thái và hiệu ứng chờ HTMX indicator vào cuối file `hub/wwwroot/css/hub.css` (chỉ thêm).
- **Kiểm thử `hub.tests/`**: Bổ sung 4 file test mới: `SettingsPageTests.cs`, `AccountsPageTests.cs`, `SecretsPageTests.cs`, `AntiforgerySecurityTests.cs`. Kiểm tra đầy đủ từ chối dữ liệu sai, không rò rỉ bí mật, từ chối POST thiếu antiforgery token trên máy chủ thực tế và render trang với `HUB_FAKE=1`.

### 2. File nào đã đổi
- `hub/Pages/Settings.cshtml` (sửa giao diện hoàn chỉnh)
- `hub/Pages/Settings.cshtml.cs` (tạo mới PageModel)
- `hub/Pages/Accounts.cshtml` (sửa giao diện hoàn chỉnh)
- `hub/Pages/Accounts.cshtml.cs` (tạo mới PageModel)
- `hub/Pages/Shared/_AgyLoginResult.cshtml` (tạo mới partial view cho HTMX)
- `hub/Pages/Vault.cshtml` (sửa giao diện hoàn chỉnh)
- `hub/Pages/Vault.cshtml.cs` (tạo mới PageModel)
- `hub/wwwroot/css/hub.css` (chỉ thêm CSS ở cuối file)
- `team.ps1` (chỉ thêm lệnh `hub`, lưu UTF-8 có BOM)
- `README.md` (thêm lệnh vào bảng mục 3)
- `hub.tests/SettingsPageTests.cs` (tạo mới unit test)
- `hub.tests/AccountsPageTests.cs` (tạo mới unit test)
- `hub.tests/SecretsPageTests.cs` (tạo mới unit test)
- `hub.tests/AntiforgerySecurityTests.cs` (tạo mới integration test)
- `tasks/H-004-hub-settings-accounts-secrets.md` (cập nhật báo cáo và checklist)

### 3. Đã kiểm tra thế nào
- **Build**:
```
dotnet build AITeam.sln
AITeam.Hub -> D:\AITeam-work\AITeam\a-h-004\hub\bin\Debug\net10.0-windows\AITeam.Hub.dll
AITeam.Hub.Tests -> D:\AITeam-work\AITeam\a-h-004\hub.tests\bin\Debug\net10.0-windows\AITeam.Hub.Tests.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
- **Test**:
```
dotnet test AITeam.sln
Passed!  - Failed:     0, Passed:    96, Skipped:     0, Total:    96, Duration: 1 s - AITeam.Hub.Tests.dll (net10.0)
```
- **Chạy thử `HUB_FAKE=1`**:
Khởi động Hub với `HUB_FAKE=1`, kiểm tra các route `/Settings`, `/Accounts`, `/Secrets` đều trả về HTTP 200 OK với giao diện đầy đủ.
- **Chạy thử `team.ps1 hub`**:
  - Lần 1: Chạy `.\team.ps1 hub` khi cổng 5057 chưa mở -> Hub khởi chạy ngầm trong cửa sổ ẩn, cổng 5057 mở và trình duyệt được gọi mở trang `http://127.0.0.1:5057`.
  - Lần 2: Chạy `.\team.ps1 hub` khi Hub đang chạy -> Báo "Hub đang chạy tại http://127.0.0.1:5057 — đang mở trình duyệt." và chỉ mở trình duyệt, không khởi tạo thêm tiến trình.

### 4. Còn gì chưa chắc
- Không có. Toàn bộ tính năng và tiêu chí kiểm thử đã hoàn thành đầy đủ, chính xác.

## Review (Claude)
<!-- Claude điền -->
