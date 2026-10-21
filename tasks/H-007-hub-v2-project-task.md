# H-007 · Hub v2: trang Dự án + trang Task

- **Agent:** A
- **Nhánh:** `agent-a/H-007-hub-v2-project-task`
- **Ưu tiên:** cao
- **Làm sau:** H-006 đã merge (CSS v2, layout, `RunVerdicts`).

## Bối cảnh
- Làm theo `docs/design/HUB-V2.md` **mục 1, 2 (dòng Dự án, Task)** và mockup (màn "Dự án", "Task", ngăn kéo lần chạy).
- Hiện tại: trang Task dài **8,2 màn hình** (in nguyên file task); Dự án 2,4 màn hình (cột "Đã merge" dài theo số task).

## Mục tiêu
1. **Dự án:** breadcrumb + tag công nghệ; nút GitHub / Mở thư mục / + Task (vùng `Actions`). Tab: Bảng việc · Kế hoạch · Lịch sử chạy. Bảng việc: 3 cột Đang làm / Chờ review / Cần sửa (thẻ gọn: mã + agent, tiêu đề, pill trạng thái lần chạy) + cột "Đã merge" dạng danh sách 1 dòng (`BoardViews.MergedPreview`, 7 mục + "+N Xem tất cả" mở danh sách đầy đủ).
2. **Task:** tiêu đề + pill trạng thái + agent; dòng meta (nhánh, thiết kế, ưu tiên). Tab: Tiêu chí (checklist, đếm x/y) · Mô tả (các mục Bối cảnh / Mục tiêu / Phạm vi / Không làm / Ràng buộc…) · Báo cáo agent · Review (từng vòng thu gọn được, nhận xét gắn nhãn BLOCKER / REQUIRED / OPTIONAL — `ReviewParser`) · Lần chạy. Cột phải: bảng lần chạy (pill theo `RunVerdicts`); bấm dòng ⇒ **ngăn kéo phải** (thời điểm, thời lượng, token, lỗi, phản hồi cuối — Markdown an toàn). Tab đổi bằng HTMX hoặc JS thuần, không tải lại cả trang.
3. **Lớp thuần mới** trong `hub/Services/HubViews.cs` (thêm vào file của H-006):
   ```csharp
   public enum ReviewVerdict { Pass, NeedsFix, Other }
   public enum ReviewSeverity { Blocker, Required, Optional, None }
   public sealed record ReviewItem(ReviewSeverity Severity, string Text);
   public sealed record ReviewRound(string Title, ReviewVerdict Verdict, string Markdown, IReadOnlyList<ReviewItem> Items);
   public static class ReviewParser
   {
       // Đầu vào: nội dung mục "Review (Claude)". Mỗi "### <tiêu đề>" là một vòng.
       // Verdict: tiêu đề chứa "ĐẠT" (không kèm "CẦN") ⇒ Pass; chứa "CẦN SỬA" / "CẦN LÀM NỐT" ⇒ NeedsFix; còn lại Other.
       // Items: các dòng danh sách (1. / - ) nằm sau một dòng bắt đầu bằng **Blocker** / **Required** / **Optional**
       // (không phân biệt hoa thường, có thể có chữ sau, ví dụ "**Required** (Claude sửa):") thuộc mức đó;
       // dòng danh sách không thuộc nhóm nào ⇒ None. Không có "###" ⇒ 1 vòng Other chứa toàn bộ.
       public static IReadOnlyList<ReviewRound> Parse(string? markdown);
   }
   public static class BoardViews
   {
       // Task Merged, sắp theo số trong mã giảm dần (C-013 trước C-009), lấy `take`; Remaining = tổng - take (≥ 0).
       public static (IReadOnlyList<TaskCard> Items, int Remaining) MergedPreview(IEnumerable<TaskCard> cards, int take = 7);
   }
   ```

## Phạm vi (được sửa)
- `hub/Pages/Project.cshtml(.cs)`, `hub/Pages/Task.cshtml(.cs)`, `hub/Pages/Shared/_TaskCardPartial.cshtml`, partial mới cho ngăn kéo / tab, `hub/Services/HubViews.cs` (chỉ THÊM lớp), `hub/wwwroot/css/hub.css` (chỉ THÊM lớp nếu thiếu), `hub.tests/`, `docs/screenshots/H-007/`.

## Không làm
- Không đổi dịch vụ dữ liệu, định dạng file task. Không đổi khung layout (H-006).

## Ràng buộc
- Nội dung file task / log là dữ liệu không tin cậy: chỉ hiển thị qua `MarkdownRenderer` an toàn hiện có hoặc Razor encode. Có test: phản hồi agent chứa `<script>` / `javascript:` không thành mã chạy được.
- Task không có mục "Review (Claude)" / không có lần chạy / không có tiêu chí ⇒ tab tương ứng hiện trạng thái rỗng, không lỗi.

## Giao diện
- Mockup màn "Dự án", "Task". 1280×800: hai trang **không cuộn trang** với dữ liệu thật của `ChinaAndroidToolbox` (14 task) và task `T-006` của HDVTool (3 vòng review); nội dung tab dài tự cuộn trong khung của tab.
- Ảnh nộp `docs/screenshots/H-007/`: `project.png`, `task-criteria.png`, `task-review.png`, `task-drawer.png` (ngăn kéo mở), `task-empty.png`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH007Tests.cs` — có sẵn trên nhánh (commit đầu tiên).

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln` pass hết, gồm `AcceptanceH007Tests` (không sửa) + test của agent
- [ ] Hai trang đúng mockup, không cuộn trang ở 1280×800; 5 ảnh đã nộp
- [ ] Giữ BOM UTF-8

## Báo cáo của agent
(Agent dừng 2 lần vì hết credits Antigravity sau khi xong code 3/4 bước; theo yêu cầu chủ dự án, Claude hoàn tất phần ảnh + review.)
- Đã làm: `ReviewParser`, `BoardViews` (HubViews.cs); trang Dự án (tab Bảng việc / Kế hoạch / Lịch sử chạy, cột Đã merge 7 mục + "Xem tất cả"); trang Task (5 tab, mở tab qua `?tab=`, cột lần chạy, ngăn kéo chi tiết qua `?drawer=`); `H007ExtraTests.cs`.
- Ảnh: `docs/screenshots/H-007/` (Claude chụp bằng Edge headless 1280×800).

## Review (Claude)
### Vòng 1 — ĐẠT
Đã kiểm tra: cổng kiểm tra build 0 warning, 148/148 test, test nghiệm thu nguyên vẹn, quét bí mật sạch (cảnh báo "ngoài phạm vi" là báo nhầm do mẫu `Project.cshtml(.cs)`); đọc diff; chạy Hub từ nhánh ở 1280×800 với dữ liệu thật: trang Dự án ChinaAndroidToolbox và Task T-006 không cuộn trang, nội dung tab tự cuộn; thử chèn mã qua `?drawer=` không chạy.

**Blocker** (Claude sửa):
1. `Task.cshtml` — giá trị `?drawer=` chèn thẳng vào script bằng `Html.Raw` ⇒ XSS phản xạ (`?drawer=1';…;//`). Sửa: chỉ nhận số lần chạy hoặc "first".

**Required** (Claude sửa):
2. `Task.cshtml` — nhãn tab hiện nguyên mã `<span class="c">4/4</span>` (chuỗi bị encode). Sửa bằng markup Razor.
3. `Task.cshtml` — "Cổng kiểm tra: ĐẠT" thực ra suy từ lần chạy cuối thành công ⇒ báo sai. Đổi thành "Lần chạy cuối: thành công".
4. `hub.css` — ngăn kéo có thanh cuộn ngang (lưới `.kv` / `.bd` không co). Sửa `minmax(0, 1fr)` + ngắt dòng dài.
5. Agent để lại thay đổi chưa commit (mở tab qua `?tab=`) và script chụp ngoài phạm vi: Claude commit phần đầu, bỏ script.

**Optional** (hàng chờ): vòng review có danh sách nhận xét hiển thị cả danh sách gắn nhãn lẫn Markdown gốc (trùng nội dung); review cũ dạng "**Vòng 1 — ĐẠT**" (không có ###) gộp thành một khối "Đánh giá".

Claude sửa: 4 lỗi trên (1 bảo mật, 3 giao diện).
