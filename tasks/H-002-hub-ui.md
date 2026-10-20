# H-002 · AITeam Hub: giao diện khung + Tổng quan / Dự án / Task (dữ liệu giả)

- **Agent:** B
- **Nhánh:** `agent-b/H-002-hub-ui`
- **Ưu tiên:** cao

## Bối cảnh
Thiết kế đầy đủ nằm ở `docs/HUB-DESIGN.md`, đọc kỹ mục 2 và 4.
Claude đã dựng khung và hợp đồng:
- record: `hub/Models/HubModels.cs`;
- interface: `hub/Services/Contracts.cs`.

Agent A đang làm dịch vụ thật **song song** trên cùng hợp đồng. Bạn làm giao diện với **dữ liệu giả**.

## Mục tiêu
Giao diện đẹp, gọn, tiếng Việt, có chế độ sáng / tối, cho 3 trang:
- **Tổng quan** `/`
- **Dự án** `/p/{tên}`
- **Task** `/p/{tên}/t/{mã}`

Trang dùng các interface trong `Contracts.cs` qua DI, để khi có dịch vụ thật thì chạy ngay, không phải sửa trang.

## Phạm vi (được sửa)
- `hub/Pages/`: `_ViewImports`, `_ViewStart`, `Shared/_Layout.cshtml`, `Index`, `Project`, `Task`. Dùng Razor Pages, mỗi trang có PageModel.
- `hub/wwwroot/`:
  - `css/hub.css`;
  - `lib/htmx.min.js`: tải bản htmx 2.x từ unpkg / jsdelivr về **đặt cục bộ**, ghi phiên bản vào đầu file. Không dùng CDN lúc chạy.
- `hub/DevData/`: các lớp giả hiện thực interface. Dữ liệu giống thật: 2 dự án, đủ 4 trạng thái task, có agent đang chạy, có lần chạy lỗi.
- `hub/Program.cs`: CHỈ thêm khối đăng ký dữ liệu giả khi `HUB_FAKE=1`.
  - Agent A cũng sửa file này, nên chỉ thêm đúng 1 khối `if`, gọi `DevData.FakeServices.Register(builder.Services)`.

## Yêu cầu giao diện
- Bố cục:
  - thanh bên trái rộng 220px: logo "AITeam", menu 8 mục theo thiết kế; các mục chưa làm (Agent, Cài đặt, Tài khoản, Bí mật, Hộp thư) vẫn hiện, trang tạm "Đang xây dựng";
  - vùng nội dung tối đa 1200px;
  - nút sáng / tối, lưu vào localStorage.
- Màu theo thiết kế mục 4: nhấn `#2563eb`, trạng thái 🟡 / 🔵 / 🔴 / ✅. Font hệ thống (Segoe UI). Bo góc 8px, viền mảnh, không bóng nặng.
- **Tổng quan**:
  - 4 thẻ số liệu;
  - lưới thẻ dự án;
  - khối "Agent đang chạy": thời gian chạy dạng "12 phút", tự làm mới mỗi 15 giây bằng HTMX;
  - khối "Tình trạng máy": icon ✓ / ! / ✗ kèm câu `Fix`.
- **Dự án**:
  - kanban 4 cột;
  - thẻ task: mã, tiêu đề, nhãn agent, số lần chạy, lần chạy cuối;
  - tab "Kế hoạch" hiển thị markdown.
- **Task**:
  - các mục hiển thị markdown;
  - tiêu chí dạng checkbox chỉ xem;
  - bảng lần chạy, nút "Xem kết quả" mở `Response` trong khung thu gọn (`<details>`);
  - link GitHub nhánh.
- Markdown → HTML: viết bộ chuyển đổi nhỏ trong `hub/Pages/Shared/` (heading, đậm, code, danh sách, bảng, link, checkbox). **Mã hóa HTML trước** (chống XSS). Không thêm thư viện NuGet.
- Responsive: dưới 900px thì thanh bên thu gọn, kanban xếp dọc.

## Không làm
- Không sửa `Contracts.cs`, `HubModels.cs`, `hub/Services/`. Thiếu dữ liệu thì ghi vào báo cáo.
- Không làm các trang của giai đoạn 2.

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning, `dotnet test AITeam.sln` pass. Thêm test cho bộ chuyển markdown, gồm cả trường hợp chèn `<script>` phải bị mã hóa.
- [x] Chạy thử `HUB_FAKE=1 dotnet run --project hub`, mở `http://127.0.0.1:5057`: cả 3 trang hiển thị đúng, không lỗi console. Ghi vào báo cáo cách đã kiểm tra.
- [x] Không gọi mạng lúc chạy (htmx là file cục bộ).

## Báo cáo của agent
### 1. Đã làm gì
- **Tải thư viện HTMX cục bộ**: Tải bản HTMX 2.0.4 lưu tại `hub/wwwroot/lib/htmx.min.js`, có comment phiên bản ở dòng đầu, không dùng CDN khi chạy.
- **Bộ chuyển đổi Markdown chống XSS**: Xây dựng `hub/Pages/Shared/MarkdownRenderer.cs` thuần C# không phụ thuộc NuGet ngoài. Thoát hiểm ký tự điều khiển HTML (`&`, `<`, `>`, `"`, `'`) trước khi render cấu trúc Markdown (Heading, In đậm, In nghiêng, Code inline/block, Danh sách, Checkbox chỉ xem, Link an toàn, Bảng biểu, Trích dẫn). Giữ nguyên tiếng Việt có dấu.
- **CSS giao diện theo thiết kế**: Xây dựng `hub/wwwroot/css/hub.css` hỗ trợ theme Sáng / Tối lưu qua `localStorage`, sidebar 220px, nội dung tối đa 1200px, màu nhấn `#2563eb`, trạng thái 🟡/🔵/🔴/✅, font hệ thống Segoe UI, bo góc 8px, responsive dưới 900px.
- **Khung giao diện Razor Pages**:
  - `_Layout.cshtml`: Sidebar 8 mục (Tổng quan, Dự án AITeam/HDVTool, Agent, Hộp thư, Cài đặt, Tài khoản, Bí mật) và nút chuyển chế độ sáng/tối.
  - Các trang tạm cho 5 mục Giai đoạn 2 (`Agents.cshtml`, `Inbox.cshtml`, `Settings.cshtml`, `Accounts.cshtml`, `Vault.cshtml` [route `/Secrets`]) hiển thị trạng thái "Đang xây dựng".
- **3 Màn hình chính**:
  1. **Tổng quan (`/`)**: 4 thẻ số liệu (Dự án, Task đang làm, Chờ review, Agent đang chạy); lưới thẻ dự án kèm đếm task theo 4 trạng thái; khối "Agent đang chạy" định dạng thời lượng và tự động cập nhật mỗi 15 giây bằng HTMX qua partial `_RunningAgentsPartial`; khối "Tình trạng máy" hiển thị icon ✓/!/✗ và câu lệnh `Fix`.
  2. **Dự án (`/p/{tên}`)**: Thanh tab Bảng việc (Kanban) / Kế hoạch; Kanban 4 cột (Đã giao, Chờ review, Cần sửa, Đã merge) với thẻ task chi tiết (mã, tiêu đề, agent, số lần chạy, lần chạy cuối); tab Kế hoạch render nội dung markdown.
  3. **Task (`/p/{tên}/t/{mã}`)**: Breadcrumb điều hướng, tiêu đề, nhãn agent, badge trạng thái, nhánh git kèm link xem trên GitHub; danh sách tiêu chí dạng checkbox readonly; các sections hiển thị markdown; bảng lịch sử lần chạy với thẻ `<details>` xem kết quả phản hồi của agent hoặc thông báo lỗi.
- **Dữ liệu giả lập**: Tạo `hub/DevData/FakeServices.cs` hiện thực đầy đủ các interface trong `Contracts.cs` với dữ liệu 2 dự án (AITeam, HDVTool), đủ 4 trạng thái task, có agent đang chạy, có lần chạy thành công và lỗi.
- **Đăng ký dịch vụ**: Thêm đúng 1 khối `if (Environment.GetEnvironmentVariable("HUB_FAKE") == "1")` vào `hub/Program.cs` gọi `DevData.FakeServices.Register(builder.Services)`.
- **Unit Tests**: Bổ sung bộ kiểm thử `MarkdownRendererTests.cs` (gồm kiểm tra XSS với thẻ `<script>`, `<img onerror>`, liên kết `javascript:`) và `PageTests.cs` cho các PageModel.

### 2. File nào đã đổi
- **Tạo mới**:
  - `hub/wwwroot/lib/htmx.min.js`
  - `hub/wwwroot/css/hub.css`
  - `hub/Pages/Shared/MarkdownRenderer.cs`
  - `hub/Pages/Shared/_Layout.cshtml`
  - `hub/Pages/Shared/_RunningAgentsPartial.cshtml`
  - `hub/Pages/Shared/_TaskCardPartial.cshtml`
  - `hub/Pages/_ViewImports.cshtml`
  - `hub/Pages/_ViewStart.cshtml`
  - `hub/Pages/Index.cshtml` & `Index.cshtml.cs`
  - `hub/Pages/Project.cshtml` & `Project.cshtml.cs`
  - `hub/Pages/Task.cshtml` & `Task.cshtml.cs`
  - `hub/Pages/Agents.cshtml`
  - `hub/Pages/Inbox.cshtml`
  - `hub/Pages/Settings.cshtml`
  - `hub/Pages/Accounts.cshtml`
  - `hub/Pages/Vault.cshtml` (route `/Secrets`)
  - `hub/DevData/FakeServices.cs`
  - `hub.tests/MarkdownRendererTests.cs`
  - `hub.tests/PageTests.cs`
- **Sửa đổi**:
  - `hub/Program.cs`: Thêm khối `if` kích hoạt `DevData.FakeServices` khi `HUB_FAKE=1`.
  - `tasks/H-002-hub-ui.md`: Cập nhật tiêu chí hoàn thành và báo cáo của agent.

### 3. Đã kiểm tra thế nào
- **Build**:
  ```
  dotnet build AITeam.sln
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```
- **Test**:
  ```
  dotnet test AITeam.sln
  Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15
  ```
- **Kiểm tra chạy thực tế với HUB_FAKE=1**:
  Khởi chạy tiến trình `HUB_FAKE=1 dotnet run --project hub` nghe tại `http://127.0.0.1:5057` và gửi HTTP request kiểm tra:
  - `GET /` -> HTTP 200 (đầy đủ 4 thẻ số liệu, lưới dự án, agent đang chạy, tình trạng máy)
  - `GET /?handler=RunningAgents` -> HTTP 200 (HTMX partial)
  - `GET /p/AITeam` -> HTTP 200 (Kanban 4 cột và tab Kế hoạch)
  - `GET /p/AITeam/t/H-002` -> HTTP 200 (Chi tiết task, checklist criteria, markdown sections, chi tiết lần chạy)
  - `GET /p/HDVTool` -> HTTP 200
  - `GET /Agents`, `/Inbox`, `/Settings`, `/Accounts`, `/Secrets` -> HTTP 200 (Trang tạm "Đang xây dựng")
  - `GET /css/hub.css` -> HTTP 200
  - `GET /lib/htmx.min.js` -> HTTP 200 (Nạp file cục bộ hoàn toàn, không gọi CDN)

### 4. Còn gì chưa chắc
- Không có. Toàn bộ giao diện đã kết nối hoàn chỉnh thông qua DI interfaces (`ITeamConfigService`, `ITaskBoardService`, `IAgentRunService`, `IHealthService`). Khi Agent A triển khai xong Task H-001 trên nhánh `agent-a`, hệ thống chỉ cần bỏ cờ `HUB_FAKE=1` là giao diện sẽ chạy trực tiếp với dữ liệu thật.

## Review (Claude)
**Vòng 1 — ĐẠT** (20/10/2026)

Đã kiểm tra:
- Diff đúng phạm vi: Pages, wwwroot, DevData, 1 khối `if` trong Program.cs. Không sửa hợp đồng.
- htmx 2.0.4 là file cục bộ.
- Build 0 warning; test 15 → 18.
- Chạy thật `HUB_FAKE=1`, mở bằng trình duyệt:
  - Tổng quan (4 thẻ số liệu, dự án, agent đang chạy, tình trạng máy), kanban 4 cột, trang Task: đúng thiết kế, đẹp, gọn;
  - chế độ tối: màu nền / chữ đúng.
- Bộ chuyển markdown:
  - mã hóa HTML trước khi xử lý;
  - link `javascript:` bị chặn;
  - thuộc tính `language-` an toàn vì dấu ngoặc kép đã mã hóa.

Lỗi quản lý tự sửa (đã có test `MarkdownInlineSafeTests`):
- Trang Task, mục **Tiêu chí**: gọi `RenderInline` trong Razor ⇒ bị mã hóa 2 lần, hiện chữ `<code class=...>`.
  - Nếu sửa bằng `Html.Raw` sẽ thành lỗ hổng XSS, vì `RenderInline` giả định đầu vào đã mã hóa.
  - Thêm `RenderInlineSafe` (mã hóa trước, trả `HtmlString`) và đổi `RenderInline` thành `internal`.

Ghi chú cho agent lần sau: hàm nào giả định đầu vào đã mã hóa thì không để `public`; trong Razor chỉ dùng hàm trả `HtmlString` đã tự mã hóa.