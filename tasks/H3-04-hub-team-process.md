# H3-04 · Hub v3: trang Đội ngũ + Quy trình & cải tiến

- **Agent:** A
- **Nhánh:** `agent-a/H3-04-hub-team-process`
- **Ưu tiên:** cao
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-003
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang "Đội ngũ":** thẻ điểm từng nhân viên (task, lượt, phút / lượt, đạt cổng kiểm tra lần đầu, đạt vòng 1, lỗi), thẻ đếm lỗi theo nguyên nhân (Claude / nhân viên / công cụ / chủ dự án / hạ tầng), bản đồ nhiệt điểm 7 giai đoạn của từng task.
- **Chức năng mới — trang "Quy trình & cải tiến":** biểu đồ điểm trung bình 7 giai đoạn, danh sách chu kỳ cải tiến (IMP), bảng vấn đề của team (giai đoạn, nguyên nhân, vấn đề, đề xuất) lọc theo nguyên nhân.
- Dữ liệu lấy từ phần H3-01 đã có; trang chỉ đọc.

## Đọc (CHỈ những file này)
- `hub/Pages/Team.cshtml.cs`, `hub/Pages/Process.cshtml.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Team.cshtml`, `hub/Pages/Process.cshtml` (viết theo Bố cục)
- `hub/Models/InsightModels.cs` (model — KHÔNG đổi)
- `hub.tests/AcceptanceH304Tests.cs` (test nghiệm thu — đặc tả chính xác)
- `hub/Pages/Inbox.cshtml` (chỉ để xem class CSS có sẵn: `panel`, `hd`, `bd`, `pill`, `empty`, `muted`, `btn sm`, `btn pri`)

## Bối cảnh
Thiết kế: `docs/design/HUB-V3.md` mục 2; mockup `docs/design/hub-v3-mockup.html` trang Đội ngũ và Quy trình & cải tiến (chỉ để hình dung, KHÔNG chép CSS của mockup).

## Bố cục giao diện
### `Team.cshtml` (`@page "/Team"`, tiêu đề "Đội ngũ")
1. `Model.Error` ⇒ hộp đỏ như `Inbox.cshtml` dòng 17–21.
2. Chữ phụ `muted`: "Chỉ tính task theo quy trình mới (có phiếu R-xxx)."
3. Lưới thẻ `display:grid; grid-template-columns:repeat(auto-fill,minmax(260px,1fr)); gap:12px`:
   - mỗi `StaffScore` 1 `div.panel`: `div.hd` = Tên + `span.pill.rev` "Agent @s.Agent"; `div.bd` 6 dòng (flex 2 đầu, gạch chân nét đứt): Task · Lượt · Phút / lượt (`AvgMinutesPerRun` + "′") · Đạt cổng KT lần đầu (`TeamModel.Pct`) · Đạt vòng 1 (`TeamModel.Pct`) · Lỗi AG (`TeamModel.AgErrorsText`);
   - thẻ cuối "Lỗi theo nguyên nhân": 5 dòng theo `TeamModel.Causes`: `span.pill` mã + `TeamModel.CauseLabel(mã)` + số `CauseCounts[mã]` (căn phải);
   - không có Staff ⇒ `div.empty` "Chưa có task nào theo quy trình mới (có phiếu R-xxx)." (thẻ nguyên nhân vẫn hiện).
4. Panel "Bản đồ nhiệt: điểm 7 giai đoạn theo task" (`overflow-x:auto`): bảng cột Task (liên kết `/p/{Project}/t/{Id}`) · Người · Tiếp nhận · Ý tưởng · Plan · Giao việc · Code · Review/QC · Đánh giá · TB. Ô giai đoạn: `<td class="@TeamModel.HeatCss(score)">` in điểm hoặc "—" (dùng `TeamModel.StageScoreOf`); cột TB: `ProcessScore` hoặc "—". Màu trong `<style>` của trang: `.h5{background:#d7f0de}.h4{background:#ecf7ef}.h3{background:#fff4d6}.h2{background:#fde3d6}.h1{background:#f8cfcf}.h0{color:var(--faint,#9aa1ab)}`, ô căn giữa. Không có hàng ⇒ `div.empty` "Chưa có task nào được đánh giá quy trình.".

### `Process.cshtml` (`@page "/Process"`, tiêu đề "Quy trình & cải tiến")
1. `Model.Error` ⇒ hộp đỏ như trên. Chữ phụ `muted`: "Cứ 3 task là 1 chu kỳ cải tiến (IMP). Chu kỳ sau đo lại xem cải tiến có tác dụng không."
2. Lưới 2 cột `grid-template-columns:minmax(0,1fr) minmax(0,1fr); gap:14px`:
   - Panel "Điểm trung bình từng giai đoạn": SVG `viewBox="0 0 440 200"` `width="100%"`; hàng i (0..6) y = 12 + i·25: nhãn `Stage` tại x=0; thanh `<rect x="120" y height="14" width="@ProcessModel.BarWidth(avg, 260)" class="@ProcessModel.BarCss(avg)">` với `<title>` "n lượt chấm"; số TB (hoặc "—") tại x = 125 + width. Số thập phân in bằng `CultureInfo.InvariantCulture`. Màu: `.b-ok{fill:#22c55e}.b-mid{fill:#f59e0b}.b-bad{fill:#ef4444}.b-none{fill:#e5e7eb}`.
   - Panel "Chu kỳ cải tiến": bảng Id · Tiêu đề · Task (nối ", ") · Trạng thái (in nguyên `Status`). Không có ⇒ `div.empty` "Chưa có chu kỳ cải tiến nào (tạo sau mỗi 3 task).".
3. Panel "Vấn đề của team": `div.hd` = chữ + `span.sp` + dãy liên kết lọc `a.btn.sm`: "Tất cả (@IssueTotal)" `?cause=` và 5 liên kết `?cause=TK` … kèm số `IssueCounts`; liên kết đang chọn thêm `pri`. Bảng: Task (liên kết `/p/{Project}/t/{Task}`) · Giai đoạn · Nguyên nhân (`span.pill` mã, `title` = `TeamModel.CauseLabel`) · Vấn đề · Đề xuất (2 cột cuối cho xuống dòng, `min-width:220px`). Không có ⇒ `div.empty` "Không có vấn đề nào.".
4. Bề rộng 1000px không tràn ngang. KHÔNG dùng `Html.Raw`. Không thêm mục menu (Claude thêm khi merge).

## Mục tiêu
Điền `TeamModel`, `ProcessModel` theo chú thích khung; viết `Team.cshtml`, `Process.cshtml` theo Bố cục.

## Phạm vi (được sửa)
- `hub/Pages/Team.cshtml.cs`
- `hub/Pages/Team.cshtml`
- `hub/Pages/Process.cshtml.cs`
- `hub/Pages/Process.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `hub/Models/*`, dịch vụ, `Program.cs`, `_Layout.cshtml`, `hub.css`, `Index*`. Không thêm thư viện.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH304Tests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 22 ca nghiệm thu H3-04).
- [x] 2 trang đúng mục Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết | `dotnet build AITeam.sln` → 0 Warning(s), 0 Error(s); `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → Passed: 229/229 (`AITeam.Hub.Tests`, gồm 22 ca `AcceptanceH304Tests` + 1 ca `H304TeamProcessTests`), 59/59 (`AITeam.Core.Tests`). |
| 2 trang đúng mục Bố cục | Đã viết `Team.cshtml` (`/Team`) và `Process.cshtml` (`/Process`) đúng bố cục, không dùng `Html.Raw`, pass kiểm tra markup trong `AcceptanceH304Tests.Razor_markup_follows_layout`. |

- **Đã làm:** Hoàn thiện `TeamModel`, `ProcessModel`, giao diện Razor `/Team` và `/Process` theo đặc tả H3-04, thêm unit test `H304TeamProcessTests.cs`.
- **File đã đổi:** `hub/Pages/Team.cshtml.cs`, `hub/Pages/Team.cshtml`, `hub/Pages/Process.cshtml.cs`, `hub/Pages/Process.cshtml`, `hub.tests/H304TeamProcessTests.cs`, `tasks/H3-04-hub-team-process.md`.
- **Điều chưa chắc:** Không có (chưa chạy Hub chụp ảnh trình duyệt theo hướng dẫn giao việc; Claude kiểm tra khi review).

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 22/22 ca nghiệm thu; 59 + 229 test; build 0 warning; ~8 phút, 1 lượt; giữ BOM đủ; 330/320 dòng.
- Code: lọc nguyên nhân chuẩn hóa chữ hoa, đếm đủ 5 khóa kể cả khi lỗi, kẹp độ dài thanh, không ném lỗi ra trang.
- Không có lỗi Blocker / Required. Giao diện xem trực quan sau khi Claude thêm menu (ghi ở Kết quả).

Nhân viên: Minh (A) — 1 lượt, ~8 phút, 330/320 dòng.

## Kết quả
- **Chức năng mới:** trang **Đội ngũ** (thẻ điểm Minh / Lan, đếm lỗi theo nguyên nhân, bản đồ nhiệt 7 giai đoạn) và trang **Quy trình & cải tiến** (biểu đồ điểm TB, chu kỳ IMP, bảng vấn đề lọc theo nguyên nhân).
- **Kiểm chứng:** 22 ca nghiệm thu; chạy Hub thật với dữ liệu R-002 / R-003.
- **Cần cải tiến:** chưa có biểu đồ so sánh giữa các chu kỳ (khi có IMP-002).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-003) | — | — | — |
| 2. Ý tưởng | Claude | 5 | (chung R-003) | — | — | — |
| 3. Plan / thiết kế | Claude | 4 | ~20' | Khung ban đầu dùng `IndexModel.Percent` của task chưa merge — tự phát hiện trước khi giao | TK | Đã thêm vào mẫu task (IMP-001 mục kiểm tra 4) |
| 4. Giao việc | Claude | 5 | ~10' | Bố cục đánh số, 22 ca nghiệm thu | — | — |
| 5. Thực hiện code | Minh (A) | 5 | ~8' (1 lượt) | Đúng đặc tả, giữ BOM | — | — |
| 6. Review / QC | Cổng KT + Claude | 5 | ~5' | Cổng KT không báo nhầm (sau IMP-001) | — | — |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |