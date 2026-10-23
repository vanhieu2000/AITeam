# H3-02 · Hub v3: trang Tổng quan (chỉ số, biểu đồ thời gian, danh sách nhiệm vụ)

- **Agent:** B
- **Nhánh:** `agent-b/H3-02-hub-overview-v3`
- **Ưu tiên:** cao
- **Ngân sách:** ~330 dòng
- **Yêu cầu:** R-003
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới:** trang Tổng quan có 5 chỉ số mới (đang chạy, chờ anh duyệt, đạt vòng 1, phút agent trung bình, cần sửa).
- **Biểu đồ thời gian:** mỗi task 1 hàng, thấy rõ đoạn code / sửa lại / review / merge; chọn khung "Gần nhất · 24 giờ · 7 ngày · 30 ngày".
- **Danh sách nhiệm vụ:** độ khó S/M/L, trạng thái, phút agent, số vòng, điểm quy trình; lọc theo dự án và trạng thái.
- Phần cũ (bảng dự án, agent đang chạy, sức khỏe) giữ nguyên bên dưới.

## Đọc (CHỈ những file này)
- `hub/Services/GanttLayout.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Index.v3.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Index.cshtml` (thêm phần mới theo Bố cục), `hub/Pages/Shared/_GanttPartial.cshtml` (viết mới)
- `hub/Models/InsightModels.cs` (model dữ liệu — KHÔNG đổi)
- `hub.tests/AcceptanceH302GanttTests.cs`, `hub.tests/AcceptanceH302IndexTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/HUB-V3.md` mục 2; mockup `docs/design/hub-v3-mockup.html` trang Tổng quan (chỉ để hình dung, KHÔNG chép CSS của mockup). `IndexModel` đã là `partial` và nhận `ITeamInsightsService?`, `IApprovalService?` (Claude làm sẵn); `OnGet` đã gọi `LoadV3()`.

## Bố cục giao diện (`Index.cshtml`, theo thứ tự từ trên xuống)
1. Nếu `Model.V3Error` khác rỗng: hộp đỏ giống thông báo lỗi ở `Inbox.cshtml` dòng 17–21, nội dung `@Model.V3Error`.
2. **Hàng chỉ số** — thay khối `div.kpis` hiện có bằng 5 ô `div.kpi` (giữ class `kpis`):
   - `kpi run`: `@Model.RunningAgents.Count` — "Đang chạy";
   - `kpi fix`, bọc trong `<a href="/Approvals">`: `@Model.PendingApprovals` — "Chờ anh duyệt";
   - `kpi`: `@IndexModel.Percent(Model.Insights?.Kpis.Round1Rate)` — "Đạt vòng 1 (@(Model.Insights?.Kpis.TasksCounted ?? 0) task gần nhất)";
   - `kpi`: `AvgAgentMinutes` dạng "11′" (null ⇒ "—") — "Phút agent TB / task";
   - `kpi fix`: `@Model.TotalNeedsFix` — "Cần sửa".
3. **Panel "Biểu đồ thời gian"** (`div.panel`, rộng hết hàng): `div.hd` có chữ "Biểu đồ thời gian", `span.sp`, rồi 4 liên kết `a.btn.sm` — "Gần nhất" `?range=fit`, "24 giờ" `?range=24h`, "7 ngày" `?range=7d`, "30 ngày" `?range=30d`; liên kết đang chọn thêm class `pri`. `div.bd` (cho `overflow-x:auto`): chú thích màu (Code · Sửa lại · Review · Merge) rồi `<partial name="_GanttPartial" model="Model.Chart" />` (Chart null ⇒ không render partial).
   `_GanttPartial.cshtml` (`@model AITeam.Hub.Services.GanttChart`):
   - không có hàng ⇒ `div.empty` "Chưa có số đo thời gian trong khoảng này.";
   - `<svg viewBox="0 0 @Model.Width @Model.Height" width="100%" style="min-width:640px" font-size="12">`;
   - mỗi mốc: đường dọc từ y=Top tới Height − AxisHeight (màu `var(--line, #e3e6eb)`) và chữ nhãn ở y = Height − 6, `text-anchor="middle"`;
   - mỗi hàng: `<a href="/p/@row.Project/t/@row.Id"><text x="6" y="@(row.Y + 12)">@row.Label</text></a>` + độ khó (chữ nhỏ màu nhạt) ngay sau;
   - mỗi thanh: `<rect x y="@row.Y" width height="16" rx="3" class="g-@bar.Css"><title>@bar.Tooltip</title></rect>`;
   - màu trong `<style>` của partial: `.g-coding{fill:#f59e0b}.g-fix{fill:#ef4444}.g-review{fill:#a78bfa}.g-merge{fill:#22c55e}`;
   - số thập phân in bằng `ToString(CultureInfo.InvariantCulture)` (máy tiếng Việt in dấu phẩy ⇒ SVG hỏng).
4. **Panel "Danh sách nhiệm vụ"**: `div.hd` có chữ, `span.sp`, 2 `select.inp` lọc dự án / trạng thái (JS nhỏ trong trang ẩn / hiện `tr` theo `data-project`, `data-state`). Bảng cột: Mã (liên kết `/p/{Project}/t/{Id}`) · Nhiệm vụ · Dự án · Người (`StaffName`) · Độ khó (`DifficultyPill`) · Trạng thái (`StatePill`) · Phút agent (Runs = 0 ⇒ "—") · Vòng (0 ⇒ "—") · Điểm quy trình ("4.1 / 5", null ⇒ "—") · Yêu cầu (null ⇒ "—"). Dòng dưới bảng (class `muted`): "Hiện tối đa 20 task gần nhất." Không có task ⇒ `div.empty` "Chưa có nhiệm vụ.".
5. Phần cũ (`div.grid` bảng dự án + hoạt động + agent đang chạy + sức khỏe) giữ nguyên, ở dưới.
6. Bề rộng 1000px không tràn ngang (bảng / SVG cuộn trong panel). KHÔNG dùng `Html.Raw`.

## Mục tiêu
Điền `GanttLayout.FitWindow` / `Build` (hàm thuần, theo chú thích khung), `IndexModel.LoadV3` + 3 hàm tĩnh trong `Index.v3.cs`, viết `_GanttPartial.cshtml` và thêm các khối mới vào `Index.cshtml` theo Bố cục.

## Phạm vi (được sửa)
- `hub/Services/GanttLayout.cs`
- `hub/Pages/Index.v3.cs`
- `hub/Pages/Index.cshtml`
- `hub/Pages/Shared/_GanttPartial.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `hub/Models/*`, `Index.cshtml.cs`, `Program.cs`, `_Layout.cshtml`, `hub.css`, dịch vụ khác. Không thêm thư viện JS / biểu đồ.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH302GanttTests.cs`
- `hub.tests/AcceptanceH302IndexTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 19 ca nghiệm thu H3-02 và các test cũ của trang Tổng quan).
- [x] `Index.cshtml`, `_GanttPartial.cshtml` đúng mục Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết | `dotnet build AITeam.sln` (0 Warning, 0 Error); `dotnet test AITeam.sln --blame-hang-timeout 3m` (Passed: 59 Core.Tests + 225 Hub.Tests = 284/284, gồm 19 ca nghiệm thu H3-02) |
| Bố cục `Index.cshtml` & `_GanttPartial.cshtml` | Đúng mục Bố cục (hộp lỗi `V3Error`, 5 KPI, panel Biểu đồ thời gian + SVG partial, panel Danh sách nhiệm vụ + bộ lọc JS, giữ nguyên phần cũ; pass `Razor_markup_follows_layout`) |

- **Đã làm:** Hiện thực `GanttLayout.FitWindow`/`Build`, `IndexModel.LoadV3` và các hàm pill/percent trong `Index.v3.cs`, tạo `_GanttPartial.cshtml` vẽ SVG biểu đồ thời gian (dùng `CultureInfo.InvariantCulture`) và cập nhật `Index.cshtml` thêm 5 KPI, biểu đồ thời gian, bảng nhiệm vụ kèm lọc dự án/trạng thái.
- **File đã đổi:** `hub/Services/GanttLayout.cs`, `hub/Pages/Index.v3.cs`, `hub/Pages/Index.cshtml`, `hub/Pages/Shared/_GanttPartial.cshtml`, `tasks/H3-02-hub-overview-v3.md`.
- **Điều chưa chắc:** Không có.

## Review (Claude)

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Lan (B) | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
