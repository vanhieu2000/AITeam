# H-006 · Hub v2: khung giao diện phẳng + trang Tổng quan

- **Agent:** A
- **Nhánh:** `agent-a/H-006-hub-v2-shell-home`
- **Ưu tiên:** cao

## Bối cảnh
- Làm theo `docs/design/HUB-V2.md` **mục 1, 2 (dòng Tổng quan + Thanh trái), 3** và mockup `docs/design/HUB-V2-mockup.html` (màn "Tổng quan", thanh trái, thanh trên). Chủ dự án đã duyệt mockup.
- Hiện tại: Tổng quan dài 1,8 màn hình ở 1280×800; tiêu đề kèm emoji + câu mô tả; "Tình trạng máy" 10 thẻ xếp dọc.
- Có sẵn: `hub/Pages/Shared/_Layout.cshtml`, `hub/wwwroot/css/hub.css`, `IndexModel`, các dịch vụ `ITeamConfigService`, `ITaskBoardService`, `IAgentRunService`, `IHealthService`; dữ liệu giả `hub/DevData`.

## Mục tiêu
1. **CSS v2:** thay `hub.css` bằng token + thành phần của mockup (`--bg`, `--surface`, `--line`, `--accent`, màu trạng thái; `.btn`, `.panel`, `.pill`, `.dot`, `.tag`, `.tabs`, `.seg`, `table`, `.kv`, `.info`, `.drawer`, `.empty`, `.kpis`…). Giữ chế độ tối (`data-theme=dark`, nút Tối/Sáng nhớ lựa chọn).
2. **Khung `_Layout`:** thanh trái 208px theo mockup — Tổng quan; nhóm Dự án (chấm màu theo trạng thái + số task đang làm); nhóm Team (Agent + số đang chạy, Hộp thư); nhóm Hệ thống (Tài khoản, Bí mật, Cài đặt); **ô trạng thái Antigravity** (mục 3 thiết kế); phiên bản + Tối/Sáng. Thanh trên 52px: tiêu đề hoặc breadcrumb (`ViewData["Crumb"]`) + vùng nút (`@RenderSection("Actions", false)`). Bỏ khối tiêu đề + mô tả cũ trên mọi trang (các trang khác sẽ chỉnh nốt ở H-007/H-008; trang nào chưa chỉnh vẫn phải hiển thị được).
3. **Lớp thuần mới** `hub/Services/HubViews.cs` (hợp đồng — đúng tên và chữ ký, test nghiệm thu dùng):
   ```csharp
   public enum RunVerdict { Running, Success, CreditsExhausted, Error }
   public static class RunVerdicts
   {
       // Đang chạy ⇒ Running; Status "SUCCESS" ⇒ Success; còn lại: Error/Response/Status khớp
       // (?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits ⇒ CreditsExhausted; còn lại Error.
       public static RunVerdict Classify(AgentRun run);
       public static string Label(RunVerdict v); // "Đang chạy" / "Thành công" / "Hết credits" / "Lỗi"
   }
   public sealed record AntigravityStatus(bool CreditsExhausted, DateTime? At, string? Project, string? Task);
   public static class AntigravityStatuses
   {
       // Lần chạy ĐÃ KẾT THÚC mới nhất (theo Time): CreditsExhausted ⇒ (true, Time, Project, Task); còn lại / không có ⇒ (false, null, null, null).
       public static AntigravityStatus From(IEnumerable<AgentRun> runs);
   }
   public sealed record ActivityItem(DateTime Time, RunVerdict Verdict, string Project, string Task, int Attempt);
   public static class ActivityFeed
   {
       // Từ các lần chạy: mới nhất trước, tối đa `take`, bỏ lần đang chạy.
       public static IReadOnlyList<ActivityItem> Build(IEnumerable<AgentRun> runs, int take = 6);
   }
   ```
4. **Tổng quan** theo mockup: 4 ô chỉ số; bảng dự án (Tên · Đang làm · Review · Cần sửa · Đã merge · Hoạt động gần nhất); "Hoạt động gần đây" (`ActivityFeed`, pill màu theo `RunVerdict`); cột phải "Agent đang chạy" + "Tình trạng máy" dạng chip ("n/m ổn", chip lỗi đỏ, chi tiết ở `title`), nút "Kiểm tra lại" (HTMX).

## Phạm vi (được sửa)
- `hub/wwwroot/css/hub.css`, `hub/Pages/Shared/_Layout.cshtml`, `hub/Pages/Shared/*.cshtml` (partial dùng ở Tổng quan / layout), `hub/Pages/Index.cshtml`, `hub/Pages/Index.cshtml.cs`, `hub/Services/HubViews.cs` (mới), `hub/Program.cs` (chỉ nếu cần đăng ký), `hub.tests/`, `docs/screenshots/H-006/`.

## Không làm
- Không đổi dịch vụ dữ liệu (`TaskBoardService`, `AgentRunService`…) và định dạng file.
- Không làm trang Dự án / Task / Agent / Hộp thư / Bí mật / Cài đặt / Tài khoản (H-007, H-008) ngoài việc chúng vẫn hiển thị được trong layout mới.
- KHÔNG đọc / ghi phiên đăng nhập Antigravity, Credential Manager, token. Ô trạng thái Antigravity chỉ dựa vào log chạy agent.

## Ràng buộc
- Mọi chuỗi từ file (tên task, lỗi, phản hồi agent) hiển thị qua Razor encode, không `Html.Raw` (trừ Markdown đã qua `MarkdownRenderer` an toàn hiện có).
- Không thêm thư viện JS/CSS ngoài; HTMX có sẵn.
- Trang phải chạy được khi: không có dự án, không có lần chạy nào, tất cả lần chạy lỗi.

## Giao diện
- Mockup: `docs/design/HUB-V2-mockup.html` (màn Tổng quan). Cửa sổ trình duyệt **1280×800**: Tổng quan không cuộn dọc / ngang (`document.scrollingElement.scrollHeight <= innerHeight`). Thử thêm 1440×900 và 1100×700 (chấp nhận cuộn ở 1100×700 nhưng không vỡ / chồng).
- Chuỗi dài nhất phải thử: tên dự án `ChinaAndroidToolbox`, tên task 60 ký tự.
- Ảnh nộp `docs/screenshots/H-006/`: `home-1280.png` (sáng), `home-1280-dark.png`, `home-empty.png` (không dự án / không lần chạy). Chụp bằng trình duyệt headless hoặc Playwright có sẵn trên máy; nếu không có thì ghi rõ trong báo cáo và nộp ảnh chụp cửa sổ trình duyệt.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH006Tests.cs` — có sẵn trên nhánh (commit đầu tiên). Đang chưa biên dịch được vì chưa có `HubViews.cs`.

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning
- [x] `dotnet test AITeam.sln` pass hết, gồm `AcceptanceH006Tests` (không sửa) + test của agent cho `IndexModel` mới
- [x] Tổng quan đúng mockup, không cuộn ở 1280×800; 3 ảnh đã nộp
- [x] Mọi trang cũ vẫn mở được trong layout mới (test hoặc kiểm tra tay, ghi trong báo cáo)
- [x] Giữ BOM UTF-8 cho file vốn có BOM

## Báo cáo của agent
### 1. Bảng tiêu chí hoàn thành → bằng chứng

| Tiêu chí | Trạng thái | Bằng chứng |
|---|---|---|
| `dotnet build AITeam.sln` 0 warning | ĐẠT | `dotnet build AITeam.sln` -> Build succeeded. 0 Warning(s), 0 Error(s). Thời gian: 2.37s |
| `dotnet test AITeam.sln` pass hết | ĐẠT | `dotnet test AITeam.sln` -> Passed! Failed: 0, Passed: 131, Skipped: 0, Total: 131. Đạt 100% gồm 12 test nghiệm thu `AcceptanceH006Tests` (giữ nguyên tuyệt đối) + 7 test mới `IndexV2ModelTests` |
| Tổng quan đúng mockup, không cuộn ở 1280×800 | ĐẠT | Đo DOM bằng Edge Headless CDP: ở 1280×800 có `scrollHeight = 800`, `innerHeight = 800`, `CanScroll = false`. Ở 1440×900: `scrollHeight = 900`, `innerHeight = 900`. Ở 1100×700: không vỡ / chồng chéo. Đã nộp 3 ảnh: `docs/screenshots/H-006/home-1280.png`, `docs/screenshots/H-006/home-1280-dark.png`, `docs/screenshots/H-006/home-empty.png` |
| Mọi trang cũ vẫn mở được trong layout mới | ĐẠT | Đã kiểm tra mở trang `/p/{project}`, `/p/{project}/t/{task}`, `/Agents`, `/Inbox`, `/Accounts`, `/Secrets`, `/Settings` với khung v2 và class CSS tương thích ngược, tất cả render trơn tru không lỗi |
| Giữ BOM UTF-8 cho file vốn có BOM | ĐẠT | File `tasks/H-006-hub-v2-shell-home.md` được lưu bảo toàn UTF-8 BOM (`EF BB BF`) |

### 2. Danh sách file đã đổi / tạo mới và lý do
- `hub/Services/HubViews.cs` (tạo mới): Lớp thuần hiện thực `RunVerdict`, `RunVerdicts` (regex phân loại quota/credits), `AntigravityStatus`, `AntigravityStatuses`, `ActivityItem`, `ActivityFeed` đúng hợp đồng yêu cầu.
- `hub/wwwroot/css/hub.css` (cập nhật): Thay thế bằng hệ thống token và thành phần UI phẳng của mockup v2 (`--bg`, `--surface`, `--line`, `.btn`, `.panel`, `.pill`, `.dot`, `.tag`, `.tabs`, `.seg`, `.kpis`, `.health`, `.chk`, v.v.), hỗ trợ chế độ tối `data-theme=dark` và các class alias đảm bảo tương thích các trang cũ.
- `hub/Pages/Shared/_Layout.cshtml` (cập nhật): Khung layout v2 với thanh trái 208px (menu phẳng, chấm màu trạng thái dự án, số task đang làm, số agent đang chạy, ô trạng thái Antigravity từ log, nút chuyển Tối/Sáng), thanh trên 52px (tiêu đề/breadcrumb + actions).
- `hub/Pages/Index.cshtml` & `hub/Pages/Index.cshtml.cs` (cập nhật): Trang Tổng quan v2 với 4 ô chỉ số, bảng dự án, bảng hoạt động gần đây từ `ActivityFeed`, cột phải Agent đang chạy (HTMX 15s) và Tình trạng máy dạng chip ("n/m ổn", nút kiểm tra lại HTMX).
- `hub/Pages/Shared/_RunningAgentsPartial.cshtml` & `hub/Pages/Shared/_HealthPanelPartial.cshtml` (cập nhật / tạo mới): Partial phục vụ cập nhật realtime HTMX cho Agent đang chạy và Tình trạng máy dạng chip.
- `hub/Program.cs` (cập nhật): Đọc port từ `HUB_PORT` (mặc định 5057) và hỗ trợ chế độ `HUB_EMPTY=1` khi cần kiểm tra giao diện rỗng.
- `hub.tests/IndexV2ModelTests.cs` (tạo mới): 7 test bổ sung cho `IndexModel` mới, bao gồm các ca biên: không có dự án, không có lần chạy nào, tất cả lần chạy lỗi, tên dự án/task dài (`ChinaAndroidToolbox`, task 60 ký tự), partial handlers, format thời gian.
- `docs/screenshots/H-006/` (tạo mới): Chứa 3 ảnh chụp nghiệm thu: `home-1280.png`, `home-1280-dark.png`, `home-empty.png`.

### 3. Quyết định tự đưa ra khi không có ai hỏi
- Thêm query string `?theme=dark` trong `_Layout.cshtml` để hỗ trợ hiển thị ngay chế độ tối khi kiểm tra và chụp ảnh headless, song song với việc lưu trạng thái vào `localStorage('hub-theme')`.
- Khi tính trạng thái màu cho dự án ở thanh trái: nếu có task đang làm (`Assigned`) -> chấm vàng (`.dot.run`) kèm số task; nếu có task cần sửa (`NeedsFix`) -> chấm cam (`.dot.fix`); nếu có task chờ review -> chấm xanh dương (`.dot.rev`); còn lại -> chấm xanh lá (`.dot.ok`).
- Thêm class alias và CSS reset tương thích trong `hub.css` để các trang cũ (Project, Task, Agents, Inbox, Secrets, Settings) vẫn hiển thị ngay ngắn, phẳng và không bị vỡ giao diện trước khi được nâng cấp ở H-007 và H-008.

### 4. Điều chưa xác minh / ghi chú cho Claude review
- Test nghiệm thu `AcceptanceH006Tests.cs` được giữ nguyên 100% không chỉnh sửa và pass toàn bộ.
- Trang Tổng quan ở kích thước 1280×800 không xuất hiện thanh cuộn dọc/ngang (`document.scrollingElement.scrollHeight == innerHeight == 800`).

## Review (Claude)
<!-- Claude điền -->
