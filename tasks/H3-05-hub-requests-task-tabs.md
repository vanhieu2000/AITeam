# H3-05 · Hub v3: trang Giao việc (hội thoại theo phiếu) + tab Dòng thời gian / Đánh giá quy trình

- **Agent:** A
- **Nhánh:** `agent-a/H3-05-hub-requests-task-tabs`
- **Ưu tiên:** cao
- **Ngân sách:** ~380 dòng
- **Yêu cầu:** R-003
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang "Giao việc":** mỗi phiếu yêu cầu R-xxx là 1 luồng hội thoại: lời anh bên phải, các bước Claude đã làm (nhật ký có giờ) bên trái, kèm danh sách task đã chia. Ô viết yêu cầu mới ở cuối.
- Yêu cầu mới vào hàng chờ; Claude nhận ở đầu phiên làm việc và trả lời trong phiếu (không có AI trả lời tức thì — nói rõ trên trang).
- **Trang chi tiết task thêm 2 tab:** "Dòng thời gian" (code / sửa lại / review / merge, mỗi đoạn bao nhiêu phút) và "Đánh giá quy trình" (điểm 7 giai đoạn, vấn đề, nguyên nhân, đề xuất).

## Đọc (CHỈ những file này)
- `hub/Services/RequestService.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Requests.cshtml.cs` (khung — điền `TODO(agent)`), `hub/Pages/Requests.cshtml` (viết theo Bố cục)
- `hub/Pages/Task.cshtml` (thêm 2 tab), `hub/Pages/Task.cshtml.cs` (Claude đã thêm `Insight` — chỉ đọc)
- `hub/Models/InsightModels.cs`, `hub/Pages/Team.cshtml.cs` (dùng `TeamModel.HeatCss`, `TeamModel.CauseLabel` — chỉ đọc)
- `hub.tests/AcceptanceH305Tests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/HUB-V3.md` mục 2, 4 (giới hạn: không có AI trả lời tức thì); mockup `docs/design/hub-v3-mockup.html` trang Giao việc. Phiếu thật để xem định dạng: `requests/R-003-*.md`.

## Ràng buộc
- Hub chỉ ĐỌC phiếu `requests/`; ghi duy nhất qua `IInboxService.Add` (đã có, tự gộp nhiều dòng).
- Mã phiếu từ URL (`?id=`) chỉ nhận `^R-\d{3}$`; nội dung phiếu / yêu cầu in bằng Razor (tự mã hóa), KHÔNG `Html.Raw`.

## Bố cục giao diện
### `Requests.cshtml` (`@page "/Requests"`, tiêu đề "Giao việc")
1. Hộp `Message` (xanh) / `ErrorMessage` (đỏ) như `Inbox.cshtml` dòng 7–21.
2. Chữ phụ `muted`: "Yêu cầu mới vào hàng chờ; Claude nhận ở đầu phiên làm việc, mở phiếu R-xxx và trả lời trong luồng này. Không có AI trả lời tức thì."
3. Lưới `grid-template-columns: 280px minmax(0,1fr); gap: 14px; align-items: start`:
   - **Trái — panel "Phiếu yêu cầu"** (`div.bd` cuộn dọc, `max-height: 640px`):
     - nếu `Waiting` có mục: tiêu đề nhỏ "Chờ Claude nhận" rồi mỗi mục 1 khối: `span.pill.run` "Chờ nhận" · Project · `Time` "HH:mm dd/MM" và Text (1 dòng, `text-overflow: ellipsis`);
     - mỗi `Threads`: `<a href="?id=@t.Id">` dạng khối (viền `var(--accent)` nếu là `Selected`): `<b>@t.Id</b>` + Title (tối đa 2 dòng), dòng nhỏ `muted`: Status · "@t.Tasks.Count task";
     - không có luồng ⇒ `div.empty` "Chưa có phiếu yêu cầu nào.".
   - **Phải — panel luồng** (khi `Selected` khác null): `div.hd` = "@Selected.Id · @Selected.Title" + `span.sp` + Status. `div.bd`:
     - `div.chat` (cột, `gap: 10px`, `max-height: 520px`, cuộn dọc):
       - `div.msg.me` (căn phải, nền `var(--accent)`, chữ trắng, `white-space: pre-wrap`): `@Selected.Verbatim` (rỗng ⇒ "(phiếu không chép lời nguyên văn)"), dòng nhỏ "Anh · @Selected.ReceivedAt";
       - mỗi `Events`: `div.msg.bot` (căn trái, nền `var(--s2, #f1f3f6)`): dòng nhỏ "<b>Claude</b> · @e.Time", rồi `@e.Text`, `Note` khác rỗng ⇒ `div.muted` `@e.Note`;
       - `Tasks` có mục ⇒ thêm 1 `div.msg.bot` "Chia việc:" + mỗi task 1 chip: Code có, Project có và Code không chứa "/" ⇒ `<a href="/p/@Project/t/@Code">`, ngược lại `span`; chữ "@Code · @Who" (Code rỗng ⇒ Title);
       - `Evaluated` ⇒ `div.msg.bot` "Đã đánh giá xong yêu cầu (mục 4 trong @FileName).";
     - CSS trong `<style>` của trang: `.chat{display:flex;flex-direction:column}`, `.msg{max-width:78%;padding:9px 12px;border-radius:10px;overflow-wrap:anywhere}`, `.msg.me{align-self:flex-end;…}`, `.msg.bot{align-self:flex-start;…}`.
   - **Dưới panel phải** (luôn hiện, kể cả khi không có luồng): `<form method="post" asp-page-handler="Send">` + `@Html.AntiForgeryToken()`: `select.inp name="project"` (mọi `Projects` + `RequestsModel.NewProject`), `textarea.inp name="text" rows="3" maxlength="4000" placeholder="Viết yêu cầu mới…"` (rộng 100%), nút `btn pri` "Gửi" căn phải.
4. Bề rộng 1000px không tràn ngang. KHÔNG `Html.Raw`. Không thêm mục menu (Claude thêm khi merge).

### `Task.cshtml` — thêm 2 tab (giữ nguyên 3 tab cũ, cột "Lần chạy" bên phải, 2 chỗ `Html.Raw` cũ)
1. Dòng 14: `activeTab` nhận thêm `"timeline"`, `"process"` (từ `?tab=`).
2. Sau tab "Báo cáo & review": `<span class="tab" id="tabTimelineBtn" onclick="switchTaskTab('timeline')">Dòng thời gian</span>` và `<span class="tab" id="tabProcessBtn" onclick="switchTaskTab('process')">Đánh giá quy trình</span>` (thêm `span.c` điểm TB nếu có `ProcessScore`).
3. `div#tabTimelineContent` (cùng kiểu với `tabDescContent`): `Model.Insight` null hoặc `Timeline` rỗng ⇒ `div.empty` "Chưa có số đo thời gian cho task này (chỉ có từ 21/10/2026)."; ngược lại dòng tóm tắt "Phút agent: … · Lượt: … · Vòng review: … · Độ khó: …" rồi bảng: Đoạn (`pill run` "Code" / `pill fix` "Sửa lại" / `pill rev` "Review" / `pill ok` "Merge") · Bắt đầu "HH:mm dd/MM" · Kết thúc · Phút (Merge ⇒ "—").
4. `div#tabProcessContent`: `Insight` null hoặc `Stages` rỗng ⇒ `div.empty` "Chưa đánh giá quy trình cho task này."; ngược lại bảng Giai đoạn · Ai · Điểm (`<td class="@TeamModel.HeatCss(s.Score)">`, null ⇒ "—") · Thời gian · Vấn đề · Nguyên nhân (`span.pill` + `title=TeamModel.CauseLabel`) · Đề xuất; dưới bảng "Điểm TB: … / 5". Thêm CSS `.h5`…`.h0` giống `Team.cshtml` vào `<style>` của trang.
5. `switchTaskTab`: ẩn / hiện thêm 2 khối mới, bật class `on` đúng nút.

## Mục tiêu
Điền `RequestParser`, `RequestService`, `RequestsModel` theo chú thích khung; viết `Requests.cshtml`; thêm 2 tab vào `Task.cshtml` theo Bố cục.

## Phạm vi (được sửa)
- `hub/Services/RequestService.cs`
- `hub/Pages/Requests.cshtml.cs`
- `hub/Pages/Requests.cshtml`
- `hub/Pages/Task.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `hub/Models/*`, `Task.cshtml.cs`, `Team.cshtml.cs`, dịch vụ khác, `Program.cs`, `_Layout.cshtml`, `hub.css`. Không sửa / xóa file trong `requests/`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH305Tests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 14 ca nghiệm thu H3-05).
- [x] 2 trang đúng mục Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s), 0 Error(s)`. `dotnet test AITeam.sln --blame-hang-timeout 3m` → `Passed! Core.Tests: 59/59, Hub.Tests: 243/243` (14/14 ca `AcceptanceH305Tests` pass). |
| 2 trang đúng mục Bố cục | Kiểm qua `AcceptanceH305Tests.Razor_markup_follows_layout` pass; chưa chạy Hub trực tiếp theo quy định agent. |

- **Đã làm:** Hoàn thiện `RequestParser.Parse`, `RequestService.GetAll/Get`, `RequestsModel.OnGet/OnPostSend`; viết bố cục `Requests.cshtml`; thêm 2 tab `Dòng thời gian` và `Đánh giá quy trình` vào `Task.cshtml`.
- **File đã đổi:** `hub/Services/RequestService.cs`, `hub/Pages/Requests.cshtml.cs`, `hub/Pages/Requests.cshtml`, `hub/Pages/Task.cshtml`, `tasks/H3-05-hub-requests-task-tabs.md`.
- **Điều chưa chắc:** Không có; giao diện thực tế chờ Claude chạy Hub kiểm tra khi review.

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 14/14 ca nghiệm thu; build 0 warning; ~11 phút, 1 lượt; 348/380 dòng.
- Code: đọc phiếu chịu được dữ liệu thiếu, chặn mã phiếu lạ, kiểm dự án / độ dài khi gửi, không `Html.Raw` mới.
- Xem Hub thật (cổng 5058, dữ liệu thật): trang Giao việc hiện 3 phiếu R-001…R-003 với nhật ký; tab Dòng thời gian, Đánh giá quy trình của V4-06 đúng.
- [AG] (BOM) mất BOM UTF-8 ở 3 file khung — lần 2 (H3-01) ⇒ thêm vào Lưu ý riêng của Minh. Cổng KT mới (IMP-001) tự trả lại, không tốn lượt.
- [TK] (chất lượng) khung của Claude quên đăng ký `IRequestService` ⇒ trang /Requests trắng (lỗi 500); test nghiệm thu chỉ thử model, không dựng trang thật.
- Claude sửa (~10 dòng, 10 phút): đăng ký `IRequestService`; thêm 5 trang v3 (/, /Approvals, /Team, /Process, /Requests) vào test dựng trang thật `Pages_render_successfully_with_fake_services`; chữ lời chủ dự án bị khoảng trống thừa (`pre-wrap` cả khối) ⇒ chỉ áp cho phần chữ. 59 + 248 test pass.

Nhân viên: Minh (A) — 1 lượt, ~11 phút, 348/380 dòng.

## Kết quả
- **Chức năng mới — trang Giao việc:** mỗi phiếu R-xxx là 1 luồng hội thoại (lời anh bên phải, các bước Claude làm có giờ bên trái, danh sách task đã chia); ô gửi yêu cầu mới (vào hàng chờ, Claude nhận đầu phiên).
- **Trang chi tiết task thêm 2 tab:** Dòng thời gian (code / sửa lại / review / merge + số phút) và Đánh giá quy trình (7 giai đoạn, tô màu điểm).
- **Kiểm chứng:** 14 ca nghiệm thu + 5 trang dựng thật trong test; chạy Hub với dữ liệu thật.
- **Cần cải tiến:** chưa có AI trả lời tức thì trong luồng (giới hạn đã nói trong thiết kế).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-003) | — | — | — |
| 2. Ý tưởng | Claude | 5 | (chung R-003) | Dùng phiếu R-xxx sẵn có làm luồng chat, không cần lưu trữ mới | — | — |
| 3. Plan / thiết kế | Claude | 4 | ~20' | Khung quên đăng ký dịch vụ mới | TK | Trang mới ⇒ luôn thêm vào test dựng trang thật (đã làm) |
| 4. Giao việc | Claude | 5 | ~10' | Đổi sang Minh để chạy song song với Lan | — | — |
| 5. Thực hiện code | Minh (A) | 4 | ~11' (1 lượt) | Đúng đặc tả; mất BOM lần 2 | AG | Lưu ý riêng (đã thêm) |
| 6. Review / QC | Cổng KT + Claude | 4 | ~15' | Cổng KT tự trả BOM (IMP-001 có tác dụng); lỗi trắng trang chỉ lộ khi chạy thật | TK | Như mục 3 |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |