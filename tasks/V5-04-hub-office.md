# V5-04 · Hub: trang Văn phòng (bàn làm việc từng thành viên, tự làm mới)

- **Agent:** B
- **Nhánh:** `agent-b/V5-04-hub-office`
- **Ưu tiên:** cao
- **Ngân sách:** ~330 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang Văn phòng:** mỗi thành viên (Claude, Minh, Lan, Codex, Cổng kiểm tra, Phòng nghiên cứu) có 1 "bàn": đang làm gì, bao lâu, rảnh / chờ review / nghỉ tới giờ nào, 3 tin nhắn tiến độ mới nhất.
- Tự làm mới mỗi 10 giây. Nút điều khiển (giao / dừng…) làm ở task sau (V5-05).

## Đọc (CHỈ những file này)
- `hub/Services/OfficeBuilder.cs` (model đã chốt + khung `Build` — điền `TODO(agent)`)
- `hub/Services/OfficeService.cs`, `hub/Pages/Office.cshtml.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Office.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml` (viết theo Bố cục)
- `hub/Models/HubModels.cs`, `hub/Services/RequestService.cs`, `core/AITeam.Core/Messages/MessageStore.cs`, `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API)
- `hub.tests/AcceptanceV504OfficeTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C2; mockup `docs/design/v5-van-phong-mini-mockup.html` (chủ dự án đã duyệt — chỉ để hình dung, dùng class CSS có sẵn của Hub). `IOfficeService` đã đăng ký DI; trang đã có trong test dựng trang thật.

## Bố cục giao diện
`Office.cshtml` (`@page "/Office"`, tiêu đề "Văn phòng"):
1. `Model.Error` ⇒ hộp đỏ như `Inbox.cshtml` dòng 17–21.
2. Hàng đầu: chữ phụ `muted` "Cập nhật HH:mm:ss · tự làm mới 10 giây" + 4 `span.pill` đếm: `run` "{Working} đang làm", `rev` "{Review} chờ review", `ok` "{Idle} rảnh", `err` "{Resting} nghỉ".
3. `<div hx-get="?handler=Desks" hx-trigger="every 10s" hx-swap="outerHTML">` bọc `<partial name="_OfficeDesks" model="Model" />`.

`_OfficeDesks.cshtml` (`@model AITeam.Hub.Pages.OfficeModel`) — vùng có `id="office-desks"` và chính các thuộc tính hx-* ở trên (để lần làm mới sau vẫn chạy):
- Lưới `display:grid; grid-template-columns:repeat(auto-fill,minmax(280px,1fr)); gap:12px`. Mỗi bàn `div.panel` có viền trái 4px theo State: Working `var(--run)`, Review `var(--rev)`, Idle `var(--ok)`, Resting `var(--err)`, Manager `#7c3aed`.
- Đầu bàn: vòng tròn 34px chữ cái đầu của Name (màu theo Key: claude `#7c3aed`, A `#2563eb`, B `#0d9488`, C `#111827`, gate `#64748b`, research `#ea580c`, khác `#9aa1ab`), `<b>Name</b>` + Role (`muted`, 12px), `span.pill` StateText bên phải (Working `run`, Review `rev`, Idle `ok`, Resting `err`, Manager `n`).
- Task (nếu có): khối nền `var(--s2)`: `<b>Id</b> Title`; Project khác rỗng ⇒ Id là liên kết `/p/{Project}/t/{Id}`; ElapsedMin có ⇒ dòng nhỏ "{n} phút".
- Feed: mỗi tin 1 dòng 12px: `<b>HH:mm</b> Text` (Text 1 dòng, `text-overflow:ellipsis`, `title` = đầy đủ).
- Chân bàn (khi Task có Project): liên kết `a.btn.sm` "Mở task" `/p/{Project}/t/{Id}` và "Xem báo cáo" `/p/{Project}/t/{Id}?tab=report`.
- Bề rộng 1000px không tràn ngang. KHÔNG `Html.Raw`. Không thêm mục menu (Claude làm khi merge, kèm chuyển thành trang chủ).

## Mục tiêu
Điền `OfficeBuilder.Build`, `OfficeService` (`Get`, `ParseStaff`), `OfficeModel` theo chú thích khung; viết 2 file Razor theo Bố cục.

## Phạm vi (được sửa)
- `hub/Services/OfficeBuilder.cs`
- `hub/Services/OfficeService.cs`
- `hub/Pages/Office.cshtml.cs`
- `hub/Pages/Office.cshtml`
- `hub/Pages/Shared/_OfficeDesks.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi model trong `OfficeBuilder.cs` (record / enum), `core/`, `Program.cs`, `HubV3Registration.cs`, `_Layout.cshtml`, `hub.css`, `Index*`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceV504OfficeTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 11 ca nghiệm thu V5-04 + trang `/Office` dựng thật).
- [x] Đúng Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết | `dotnet build AITeam.sln` ⇒ 0 Warning(s), 0 Error(s); `dotnet test AITeam.sln --blame-hang-timeout 3m` ⇒ Passed 138/138 (`AITeam.Core.Tests`) + 302/302 (`AITeam.Hub.Tests`, gồm 11 ca `AcceptanceV504OfficeTests` + `OfficeServiceTests` + test dựng trang `/Office`). |
| Đúng Bố cục `/Office` & `_OfficeDesks` | Đã cài đặt đúng bố cục `Office.cshtml` và `_OfficeDesks.cshtml` (hộp lỗi, hàng tổng hợp + 4 pill, HTMX `every 10s`, lưới bàn `minmax(280px,1fr)`, viền màu theo `DeskState`, vòng chữ cái đầu 34px, khối Task + liên kết, Feed 3 tin, nút Mở task / Xem báo cáo, không `Html.Raw`). |

- **Đã làm:** Hiện thực `OfficeBuilder.Build`, `OfficeService.Get` & `OfficeService.ParseStaff`, `OfficeModel.OnGet` & `OnGetDesks`, giao diện `Office.cshtml` & `_OfficeDesks.cshtml`, và thêm unit test `OfficeServiceTests`.
- **File đã đổi:** `hub/Services/OfficeBuilder.cs`, `hub/Services/OfficeService.cs`, `hub/Pages/Office.cshtml.cs`, `hub/Pages/Office.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml`, `hub.tests/OfficeServiceTests.cs`, `tasks/V5-04-hub-office.md`.
- **Điều chưa chắc:** Không có.

## Review (Claude)
### Vòng 1 — ĐẠT (25/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 11/11 ca nghiệm thu + `/Office` dựng thật; 138 + 302 test; 354/330 dòng; giữ BOM đủ (Lưu ý riêng có tác dụng).
- Xem Hub thật (cổng 5058, dữ liệu thật): 6 bàn, đếm trạng thái, tự làm mới 10 giây; lưới co giãn (khung hẹp ⇒ 1 cột).
- [TK] (giao diện) đặc tả bàn Claude lấy giờ hiện tại + ghép giờ sự kiện vào chữ ⇒ hiện 2 giờ. Claude sửa: giờ của chính sự kiện, chữ không ghép giờ; sửa test nghiệm thu theo.
- [CK] (công cụ) `assign` không còn ghi "🔵 Chờ review" sau cổng KT khi review AI bỏ qua ⇒ Văn phòng hiện Lan "đang làm" dù đã xong. Claude sửa team.ps1 (82ee7f1).

Nhân viên: Lan (B) — 1 lượt, ~13 phút, 354/330 dòng.

## Kết quả
- **Chức năng mới — trang Văn phòng:** bàn làm việc của Claude, Minh, Lan, Codex, Cổng kiểm tra, Phòng nghiên cứu: đang làm gì, bao lâu, rảnh / chờ review / nghỉ tới giờ nào, 3 tin tiến độ mới nhất; tự làm mới 10 giây.
- **Kiểm chứng:** 11 ca nghiệm thu; chạy Hub thật.
- **Cần cải tiến:** nút điều khiển (V5-05); tin tiến độ của agent cần `assign` nối MCP (V5-02).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-005) | — | — | — |
| 2. Ý tưởng | Claude | 5 | (chung R-005) | Mockup đã duyệt | — | — |
| 3. Plan / thiết kế | Claude | 4 | ~25' | Giờ tin bàn Claude đặc tả sai | TK | Đặc tả hiển thị: thử với dữ liệu thật (phiếu R-005) trước khi giao |
| 4. Giao việc | Claude | 5 | ~10' | Hàm thuần + 11 ca nghiệm thu, song song V5-01 | — | — |
| 5. Thực hiện code | Lan (B) | 5 | ~13' (1 lượt) | Đúng đặc tả, giữ BOM | — | — |
| 6. Review / QC | Cổng KT + Claude | 4 | ~15' | Chạy Hub thật bắt 2 lỗi (TK + CK) | CK | Đã sửa trạng thái bảng việc |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |