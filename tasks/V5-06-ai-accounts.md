# V5-06 · Tài khoản AI: danh mục, chế độ, trạng thái chuẩn + trang Hub

- **Agent:** A
- **Nhánh:** `agent-a/V5-06-ai-accounts`
- **Ưu tiên:** cao
- **Ngân sách:** ~420 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang "Tài khoản AI":** 1 bảng cho Antigravity, Codex, ChatGPT, Gemini, Claude, DeepSeek, Grok: kết nối qua đâu, chế độ (Tắt / Thủ công / Tự động), trạng thái (Đã kết nối, Cần đăng nhập, Đang nghỉ…), nút Kiểm tra / Mở trang chính thức / Ngắt.
- Chỉ lưu tên gợi nhớ + chế độ. Không lưu mật khẩu, cookie, khóa API; không đọc nội dung tệp đăng nhập.
- Là nền cho Phòng nghiên cứu (V5-07): biết AI nào đang dùng được tự động.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Accounts/AiAccounts.cs` (khung đã chốt — điền `TODO(agent)`; `AiCatalog` Claude đã viết)
- `hub/Services/AiAccountsService.cs`, `hub/Pages/AiAccounts.cshtml.cs` (khung — điền `TODO(agent)`)
- `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Providers/ProviderConfig.cs`, `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API: ProviderHealth, ProviderSettings, ProviderStateStore)
- `hub/Pages/Providers.cshtml` (mẫu bảng + nút Kiểm tra HTMX), `hub/Pages/Shared/_ProviderHealthPartial.cshtml`
- `core.tests/AcceptanceV506AccountsTests.cs`, `hub.tests/AcceptanceV506AiAccountsTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C3 và G2. DI đã đăng ký (`HubV3Registration`). Mục menu: Claude thêm khi merge.

## Bố cục giao diện
**`AiAccounts.cshtml`** (`@page "/AiAccounts"`, tiêu đề "Tài khoản AI"):
1. `Message` ⇒ hộp xanh, `ErrorMessage` / `Error` ⇒ hộp đỏ (như `Providers.cshtml`).
2. Hộp ghi chú `muted`: "Chỉ lưu tên gợi nhớ và chế độ ở %LOCALAPPDATA%\AITeam\config\accounts.json. Không lưu mật khẩu, cookie hay khóa API. Đăng nhập luôn làm trong CLI / trang chính thức của từng hãng."
3. `table.hub-table` (bọc `div` `overflow-x:auto`), thead: Dịch vụ · Kết nối qua · Chế độ · Trạng thái · Kiểm tra lúc · Thao tác. tbody: `@foreach` `<partial name="_AiAccountRow" model="row" />`.

**`Shared/_AiAccountRow.cshtml`** (`@model AITeam.Core.Accounts.AiAccountView`) — 1 `<tr id="acc-{Service.Id}">`:
- Dịch vụ: `<b>Name</b>` + dòng `muted` 12px Role; Alias khác rỗng ⇒ thêm "· {Alias}".
- Kết nối qua: Service.Via.
- Chế độ: form `<form method="post" asp-page-handler="Save" class="row" style="gap:6px">` hidden `id`; `<input class="inp" name="alias" maxlength="40" placeholder="Tên gợi nhớ" value="@Model.Alias">`; `<select class="inp" name="mode">` 3 option (value tên enum, chữ `AiAccountsModel.ModeText`, chọn sẵn Mode; option Automatic `disabled` khi `!Service.AutomaticAvailable`); nút `btn sm` "Lưu".
- Trạng thái: `span.pill {PillClass(Status)}` StatusText; Detail khác rỗng ⇒ dòng `muted` 12px (cắt bằng CSS, `title` = đầy đủ).
- Kiểm tra lúc: `CheckedAt?.ToString("HH:mm:ss")` hoặc "—".
- Thao tác: `button.btn.sm` "Kiểm tra" `hx-get="/AiAccounts?handler=Check&id={Id}" hx-target="#acc-{Id}" hx-swap="outerHTML"`; `a.btn.sm` "Trang chính thức" `href=OfficialUrl target="_blank" rel="noopener noreferrer"`; `<details>` "Đăng nhập" ⇒ chữ Service.LoginHint (Antigravity: thêm liên kết `/Accounts`); Mode khác Disabled ⇒ form `asp-page-handler="Disable"` (hidden id) nút `btn sm danger` "Ngắt" với `onclick="return confirm('Ngắt {Name}? Không xóa gì.')"`.
- Không `type="password"`, không `Html.Raw`. Bề rộng 1000px: bảng cuộn ngang trong khung, trang không tràn.

## Mục tiêu
Điền mọi `TODO(agent)` trong 3 file khung theo chú thích; viết 2 file Razor theo Bố cục.

## Phạm vi (được sửa)
- `core/AITeam.Core/Accounts/AiAccounts.cs`
- `hub/Services/AiAccountsService.cs`
- `hub/Pages/AiAccounts.cshtml.cs`
- `hub/Pages/AiAccounts.cshtml`
- `hub/Pages/Shared/_AiAccountRow.cshtml`
- `core.tests/`, `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi record / enum / chữ ký public và `AiCatalog` trong khung; không sửa `HubV3Registration.cs`, `Program.cs`, `_Layout.cshtml`, `hub.css`, `Providers*`, `Accounts*`, `team.ps1`.
- Không chạy lệnh đăng nhập, không mở trình duyệt, không gọi mạng từ code.

## Ràng buộc
- **Bảo mật:** accounts.json CHỈ 3 trường id / alias / mode. Tệp đăng nhập của CLI (vd. `.gemini\oauth_creds.json`) chỉ `File.Exists`, KHÔNG đọc nội dung. Dữ liệu form không tin cậy: id qua `AiCatalog.Find`, mode chỉ đúng tên enum, alias qua `CleanAlias`.
- Test không chạy agy / codex / gemini thật (dùng probe giả).
- File `.cs` / `.cshtml` giữ UTF-8 có BOM.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV506AccountsTests.cs`
- `hub.tests/AcceptanceV506AiAccountsTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm mọi ca nghiệm thu V5-06).
- [x] Đúng Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Test pass hết (gồm nghiệm thu V5-06) | `dotnet test AITeam.sln --blame-hang-timeout 2m` → Core: 183 passed (28 test V5-06), Hub: 312 passed (10 test V5-06), 0 failed |
| Đúng Bố cục & bảo mật | `AcceptanceV506AiAccountsTests.Razor_markup_follows_layout` & `Page_renders_and_post_needs_antiforgery` pass; không `Html.Raw`, không `type="password"` |

- **Đã làm:** Hoàn thiện `AiAccountStore` (Load/Save/CleanAlias), `AiStatusResolver.Resolve`, `CliProbes` (FindOnPath/Gemini/Claude), `AiAccountsService` (List/CheckAsync/Save/DefaultProbe), `AiAccountsModel` và 2 view `AiAccounts.cshtml`, `Shared/_AiAccountRow.cshtml`.
- **File đã đổi:** `core/AITeam.Core/Accounts/AiAccounts.cs`, `hub/Services/AiAccountsService.cs`, `hub/Pages/AiAccounts.cshtml.cs`, `hub/Pages/AiAccounts.cshtml`, `hub/Pages/Shared/_AiAccountRow.cshtml`, `tasks/V5-06-ai-accounts.md`.
- **Điều chưa chắc:** Không có (nhánh khởi tạo ở `b8a0094` trước commit tạo file task `339c15b` trên `main`, nên đã lấy `tasks/V5-06-ai-accounts.md` từ `339c15b` để điền báo cáo).

## Review (Claude)
