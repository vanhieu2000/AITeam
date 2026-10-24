# V5-05 · Hub: nút điều khiển trên Văn phòng (giao / dừng / giao lại kèm góp ý / kiểm tra lại)

- **Agent:** B
- **Nhánh:** `agent-b/V5-05-hub-control`
- **Ưu tiên:** cao
- **Ngân sách:** ~400 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — điều khiển team ngay trên Văn phòng:** giao task, giao cho Codex, dừng agent, giao lại kèm góp ý, chạy lại cổng kiểm tra — mỗi nút hỏi xác nhận trước.
- Chỉ 5 lệnh cố định (không có merge — merge vẫn chỉ qua trang "Cần anh duyệt"); mọi thao tác ghi nhật ký `.state/audit.jsonl`.
- Bảng "Lệnh gần đây" hiện lệnh đang chạy / xong / lỗi, xem được output.

## Đọc (CHỈ những file này)
- `hub/Services/CommandService.cs` (khung đã chốt — điền `TODO(agent)`; `NoopProcessStarter` Claude đã viết)
- `hub/Pages/Office.cshtml.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Office.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml` (sửa theo Bố cục)
- `hub/Models/HubModels.cs`, `hub/Services/Contracts.cs`, `hub/Services/OfficeBuilder.cs` (chỉ đọc API)
- `hub.tests/AcceptanceV505CommandTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C2 (chủ dự án duyệt 25/10: "Cho phép điều khiển, có giới hạn"). Đăng ký DI đã xong (`HubV3Registration`, chế độ giả dùng `NoopProcessStarter`). Hub chỉ nghe 127.0.0.1; form POST có antiforgery.

## Bố cục giao diện
**`Office.cshtml`:** sau khối tự làm mới bàn, thêm `@if (Model.ControlsEnabled) { <partial name="_OfficeControl" model="Model" /> }` (khoảng cách trên 16px).

**`_OfficeDesks.cshtml`** (chỉ khi `Model.ControlsEnabled`), ở chân bàn cạnh "Mở task" / "Xem báo cáo":
- Bàn agent (Key `A` / `B` / `C`), State Working, Task có Project: form nhỏ inline với 2 nút `button.btn.sm` `name="command"`: `value="Stop"` chữ "Dừng" (class thêm `danger`) và `value="Verify"` chữ "Kiểm tra lại".
- Bàn `gate` có Task: 1 nút `value="Verify"` "Kiểm tra lại".
- Mỗi form: `<form method="post" asp-page-handler="Command" hx-post="/?handler=Command" hx-target="#office-control" hx-swap="outerHTML" hx-confirm="… {Id}?">` + `<input type="hidden" name="target" value="{Project}/{Id}">`. Câu xác nhận: Dừng ⇒ "Dừng task {Id}? Agent sẽ bị dừng ngay."; Kiểm tra lại ⇒ "Chạy lại cổng kiểm tra cho {Id}?". (Dừng và Kiểm tra lại cần câu khác nhau ⇒ dùng 2 form, hoặc `hx-confirm` đặt trên từng button.)

**`Shared/_OfficeControl.cshtml`** (`@model AITeam.Hub.Pages.OfficeModel`) — `<div id="office-control" class="panel">`, KHÔNG tự làm mới (để không xóa chữ đang gõ):
1. `<h3>Điều khiển</h3>` + dòng `muted` 12px: "Chỉ 5 lệnh cố định · mỗi lệnh hỏi xác nhận và ghi nhật ký · merge chỉ ở trang Cần anh duyệt".
2. `Notice` ⇒ hộp xanh (`background:#ecfdf5;color:#047857;border-color:#10b981`); `CommandError` ⇒ hộp đỏ như `Office.cshtml` dòng 5.
3. Lưới 2 cột `display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:12px`:
   - **Ô "Giao task"**: form (`hx-post` như trên, `hx-target="#office-control"`, `hx-swap="outerHTML"`, `hx-confirm="Giao task đã chọn?"`): `<select class="inp" name="target">` mỗi `Assignable` 1 option value `{Project}/{Id}`, chữ `{Project} · {Id} · {Agent} — {Title}`; 2 nút `name="command"`: `value="Assign"` "Giao task" (`btn pri sm`) và `value="AssignCodex"` "Giao cho Codex" (`btn sm`). Danh sách rỗng ⇒ select `disabled` 1 option "Không có task chưa giao / cần sửa", 2 nút `disabled`.
   - **Ô "Giao lại / kiểm tra lại"**: form như trên (`hx-confirm="Gửi lệnh cho task đã chọn?"`): select `Resumable` (cùng định dạng); `<textarea class="inp" name="feedback" rows="3" maxlength="2000" placeholder="Góp ý cho agent (bắt buộc khi giao lại)">`; 2 nút `value="Resume"` "Giao lại kèm góp ý" (`btn pri sm`), `value="Verify"` "Kiểm tra lại" (`btn sm`). Rỗng ⇒ như ô trên.
4. `<h4>Lệnh gần đây</h4>` + `<div id="office-jobs" hx-get="/?handler=Jobs" hx-trigger="every 10s" hx-swap="innerHTML"><partial name="_OfficeJobs" model="Model" /></div>`.

**`Shared/_OfficeJobs.cshtml`** (`@model AITeam.Hub.Pages.OfficeModel`): `Jobs` rỗng ⇒ `muted` "Chưa có lệnh nào từ lúc mở Hub." Ngược lại `table.hub-table` cột: Giờ (`HH:mm:ss`), Lệnh (`CommandService.Label`), Task (`{Project} · {Task}`), Trạng thái (`Running` ⇒ `pill run` "đang chạy"; ExitCode 0 ⇒ `pill ok` "xong"; khác ⇒ `pill err` "lỗi {n}"), liên kết "Xem log" `/?handler=JobLog&id={Id}` `target="_blank"`.

Chung: bề rộng 1000px không tràn ngang; chữ dài (Title) cắt bằng CSS, không cắt chuỗi; KHÔNG `Html.Raw`; không thêm mục menu.

## Mục tiêu
Điền `CommandService` (Label, Validate, BuildArguments, CleanFeedback, Start, Jobs, Audit, Assignable, Resumable), `RealProcessStarter`, các handler `OfficeModel` theo chú thích khung; viết 2 file Razor mới + sửa 2 file Razor theo Bố cục.

## Phạm vi (được sửa)
- `hub/Services/CommandService.cs`
- `hub/Pages/Office.cshtml.cs`
- `hub/Pages/Office.cshtml`
- `hub/Pages/Shared/_OfficeDesks.cshtml`
- `hub/Pages/Shared/_OfficeControl.cshtml`
- `hub/Pages/Shared/_OfficeJobs.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi record / enum / interface trong `CommandService.cs`, không đổi `NoopProcessStarter`.
- Không sửa `core/`, `Program.cs`, `HubV3Registration.cs`, `FakeServices.cs`, `_Layout.cshtml`, `hub.css`, `team.ps1`.
- Không thêm lệnh nào ngoài 5 lệnh; không gọi qua `cmd /c` hay chuỗi lệnh ghép — chỉ `ProcessStartInfo.ArgumentList`.

## Ràng buộc
- **Bảo mật:** dữ liệu form là KHÔNG tin cậy: project phải có trong cấu hình, task qua regex + phải có trên bảng việc, command chỉ đúng tên enum (không nhận số). Log job chỉ mở theo Id có trong danh sách, không ghép đường dẫn từ dữ liệu người dùng.
- Test KHÔNG được chạy `powershell.exe` / `team.ps1` thật (dùng IProcessStarter giả; test thật chỉ dùng `cmd.exe` như test nghiệm thu).
- File `.cs` / `.cshtml` giữ UTF-8 có BOM.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceV505CommandTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm mọi ca nghiệm thu V5-05 và V5-04).
- [x] Đúng Bố cục (Claude chạy Hub chế độ giả kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build + Test toàn dự án | `dotnet build AITeam.sln` → 0 Warning(s), 0 Error(s); `dotnet test AITeam.sln --blame-hang-timeout 2m` → Passed 509/509 (Hub.Tests: 354, Core.Tests: 155, trong đó `AcceptanceV505CommandTests`: 51/51). |
| Đúng Bố cục Razor | `AcceptanceV505CommandTests.Razor_markup_follows_layout` & `Office_page_renders_control_panel_in_fake_mode` pass; không dùng `Html.Raw`. |

- **Đã làm:** Hiện thực `CommandService`, `RealProcessStarter`, các handler điều khiển trên `OfficeModel`, cập nhật `Office.cshtml` / `_OfficeDesks.cshtml`, tạo `_OfficeControl.cshtml` / `_OfficeJobs.cshtml` và thêm `CommandServiceTests.cs`.
- **File đã đổi:** `hub/Services/CommandService.cs`, `hub/Pages/Office.cshtml.cs`, `hub/Pages/Office.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml`, `hub/Pages/Shared/_OfficeControl.cshtml`, `hub/Pages/Shared/_OfficeJobs.cshtml`, `hub.tests/CommandServiceTests.cs`, `tasks/V5-05-hub-control.md`.
- **Điều chưa chắc:** Chưa chạy Hub thật để chụp ảnh (theo hướng dẫn Claude sẽ chạy kiểm tra khi review).

## Review (Claude)

## Kết quả
