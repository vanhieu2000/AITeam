# H-003 · Hub: menu dự án động, trang Agent, Hộp thư

- **Agent:** B
- **Nhánh:** `agent-b/H-003-hub-agents-inbox`
- **Ưu tiên:** cao

## Bối cảnh
- Thiết kế: `docs/HUB-DESIGN.md` mục 4.4, 4.8 và mục 5 (bảo mật).
- Đã có:
  - H-001: dịch vụ thật, đăng ký DI khi không có `HUB_FAKE`;
  - H-002: giao diện khung, Tổng quan / Dự án / Task.
- Các trang Agent, Hộp thư đang là "Đang xây dựng".

## Mục tiêu
1. **Thanh bên**: danh sách dự án lấy từ `ITeamConfigService.GetProjects()`, không ghi cứng. Hiện đang thiếu ChinaAndroidToolbox.
2. **Trang Agent `/agents`**:
   - bảng `IAgentRunService.GetRunning()`: dự án, task (link tới trang task), PID, bắt đầu, đã chạy bao lâu;
   - tự làm mới mỗi 15 giây bằng HTMX;
   - nút **Dừng**: POST có antiforgery, hộp xác nhận "Dừng agent đang làm <task>?", gọi `Stop(pid)`, báo kết quả;
   - bảng "20 lần chạy gần nhất" (`GetRecentRuns(20)`): dự án, task, lần, trạng thái, thời lượng, token, thời điểm; bấm vào mở `<details>` xem Response / Error (hiển thị markdown an toàn bằng `MarkdownRenderer.Render`).
3. **Trang Hộp thư `/inbox`**:
   - form: chọn dự án (danh sách dự án + "Dự án mới"), ô nội dung nhiều dòng, nút Gửi. POST có antiforgery → `IInboxService.Add`;
   - danh sách: "Chưa xử lý" (dòng `[ ]`) trước, "Đã nhận" (`[x]`) sau; nội dung qua `RenderInlineSafe`.

## Phạm vi (được sửa)
- `hub/Pages/Agents.*`, `hub/Pages/Inbox.*`, `hub/Pages/Shared/_Layout.cshtml`, partial mới trong `hub/Pages/Shared/`, `hub/wwwroot/css/hub.css`, `hub/DevData/FakeServices.cs` (cập nhật dữ liệu giả nếu cần), `hub.tests/`.

## Không làm
- Không sửa `hub/Services/`, `Contracts.cs`, `HubModels.cs`.
- Không làm trang Cài đặt / Tài khoản / Bí mật (task H-004).

## Tiêu chí hoàn thành
- [x] Build 0 warning; test pass. Test PageModel cho Agents (Stop gọi đúng PID, không có antiforgery thì bị từ chối) và Inbox (Add; nội dung rỗng → báo lỗi, không ghi).
- [x] Chạy `HUB_FAKE=1` và chạy với dữ liệu thật (`AITEAM_HOME=D:\AITeam`, chỉ XEM, không bấm Dừng với agent thật); các trang hiển thị đúng.
- [x] Không có chỗ nào render HTML thô từ dữ liệu (`Html.Raw`) ngoài kết quả của `MarkdownRenderer`.

## Báo cáo của agent
### 1. Đã làm gì
- **Thanh bên (Sidebar)**: Cập nhật `_Layout.cshtml` lấy danh sách dự án động từ `ITeamConfigService.GetProjects()`, tự động đánh dấu mục đang xem; hiển thị đầy đủ các dự án bao gồm `ChinaAndroidToolbox`. Gỡ nhãn "GĐ 2" ở menu Agent và Hộp thư.
- **Trang Agent `/agents`**:
  - Xây dựng PageModel `AgentsModel` (`[ValidateAntiForgeryToken]`) và partial `_AgentsRunningTablePartial.cshtml`.
  - Bảng tiến trình đang chạy (`IAgentRunService.GetRunning()`): hiển thị Dự án, Task (link tới trang task), PID, Thời điểm bắt đầu, Thời gian đã chạy.
  - Tự làm mới bảng tiến trình mỗi 15 giây thông qua HTMX (`hx-get="/Agents?handler=RunningTable"`).
  - Nút **Dừng**: Form POST có Antiforgery token, hộp thoại xác nhận `confirm('Dừng agent đang làm <task>?')`, gọi `_runService.Stop(pid)` và thông báo kết quả thành công/thất bại.
  - Bảng "20 lần chạy gần nhất" (`_runService.GetRecentRuns(20)`): hiển thị Dự án, Task (link), Lần chạy, Trạng thái (Thành công / Lỗi / Đang chạy), Thời lượng, Token, Thời điểm. Cho phép mở `<details>` xem phản hồi Response hoặc thông báo Lỗi Error thông qua bộ render an toàn `MarkdownRenderer.Render`.
- **Trang Hộp thư `/inbox`**:
  - Xây dựng PageModel `InboxModel` (`[ValidateAntiForgeryToken]`).
  - Form gửi yêu cầu: chọn dự án (danh sách dự án từ `GetProjects()` + tùy chọn "Dự án mới"), ô nhập nội dung nhiều dòng, nút Gửi. POST có Antiforgery token → gọi `IInboxService.Add`. Khi nội dung rỗng hoặc chỉ có khoảng trắng: báo lỗi xác thực qua `ModelState`, không gọi `Add`.
  - Danh sách yêu cầu: chia tách và hiển thị nhóm "Chưa xử lý" (các dòng `[ ]`) trước, nhóm "Đã nhận việc" (các dòng `[x]`) sau. Nội dung hiển thị an toàn chống XSS qua `MarkdownRenderer.RenderInlineSafe`.
- **CSS (`hub.css`)**: Bổ sung kiểu dáng cho nút `.btn-danger`, `.btn-sm`, form controls, hộp thông báo `.alert`, thẻ chi tiết lần chạy `.run-details`, danh sách mục hộp thư `.inbox-item`.
- **Dữ liệu giả lập (`FakeServices.cs`)**: Cập nhật `FakeTeamConfigService` bổ sung dự án `ChinaAndroidToolbox`, thêm tracking `LastStoppedPid` cho `FakeAgentRunService`.
- **Kiểm thử (`hub.tests`)**:
  - Tạo `AgentsInboxPageTests.cs` kiểm tra: AgentsModel nạp dữ liệu, partial view, nút Stop gọi đúng PID, từ chối khi không có / sai antiforgery token; InboxModel phân nhóm [ ] / [x], từ chối nội dung rỗng (không ghi, báo lỗi), lưu đúng item hợp lệ, từ chối khi không có antiforgery token; kiểm tra menu cấu hình trả về ChinaAndroidToolbox.
  - Cập nhật `PageTests.cs` tương ứng với 3 dự án trong `FakeTeamConfigService`.

### 2. File nào đã đổi
- `hub/Pages/Shared/_Layout.cshtml`: Hiển thị danh sách dự án động từ `ConfigService.GetProjects()`.
- `hub/Pages/Shared/_AgentsRunningTablePartial.cshtml`: Partial bảng tiến trình agent đang chạy tự làm mới mỗi 15s.
- `hub/Pages/Agents.cshtml` & `hub/Pages/Agents.cshtml.cs`: Màn hình và PageModel quản lý tiến trình Agent.
- `hub/Pages/Inbox.cshtml` & `hub/Pages/Inbox.cshtml.cs`: Màn hình và PageModel Hộp thư yêu cầu.
- `hub/wwwroot/css/hub.css`: Style cho bảng, nút nguy hiểm, form, alert, run details và inbox items.
- `hub/DevData/FakeServices.cs`: Thêm dự án ChinaAndroidToolbox và LastStoppedPid.
- `hub.tests/AgentsInboxPageTests.cs`: 12 test cases mới cho Agents, Inbox và Antiforgery.
- `hub.tests/PageTests.cs`: Cập nhật assertion 3 dự án.
- `tasks/H-003-hub-agents-inbox.md`: Đánh dấu hoàn thành tiêu chí và ghi báo cáo.

### 3. Đã kiểm tra thế nào
- **Biên dịch**: `dotnet build AITeam.sln` -> **0 Warning, 0 Error**.
- **Kiểm thử**: `dotnet test AITeam.sln` -> **Passed 60/60 tests (0 Failed, 0 Skipped)**.
```
Passed!  - Failed:     0, Passed:    60, Skipped:     0, Total:    60, Duration: 1 s - AITeam.Hub.Tests.dll (net10.0)
```
- **Kiểm tra an toàn HTML / XSS**: Không có bất kỳ chỗ nào sử dụng `Html.Raw` trên toàn bộ codebase Razor Pages ngoài `MarkdownRenderer`.
- **Kiểm tra chạy với `HUB_FAKE=1`**:
  - Mở các trang `/`, `/Agents`, `/Agents?handler=RunningTable`, `/Inbox`, `/p/ChinaAndroidToolbox` đều trả về HTTP 200 OK.
  - Xác nhận menu thanh bên có đầy đủ 3 dự án (AITeam, HDVTool, ChinaAndroidToolbox).
  - Xác nhận trang `/Agents` hiển thị tiến trình đang chạy và 4 lần chạy giả định với `<details>`.
  - Xác nhận trang `/Inbox` hiển thị form và phân loại rõ ràng 2 nhóm Chưa xử lý / Đã nhận việc.
- **Kiểm tra chạy với dữ liệu thật (`AITEAM_HOME=D:\AITeam`)**:
  - Quét đúng 2 tiến trình `agy.exe` đang chạy thật trên máy từ WMI (PID 22104, 18168), chỉ XEM, không bấm Dừng.
  - Quét và hiển thị chính xác 20 lần chạy gần nhất từ logs thật của ChinaAndroidToolbox (C-001, C-002) và AITeam (H-001) với phản hồi markdown được format sạch đẹp.
  - Toàn bộ các trang trả về HTTP 200 OK.

### 4. Còn gì chưa chắc
- Không có vấn đề tồn đọng; toàn bộ mục tiêu và tiêu chí hoàn thành đều đã được kiểm tra thực tế và xác minh tự động bằng unit test.

## Review (Claude)
**Vòng 1 — ĐẠT** (20/10/2026)

Đã kiểm tra:
- Diff đúng phạm vi (Pages, partial, CSS, DevData, test). Không sửa Services / hợp đồng.
- Build 0 warning; test 47 → 60. Không có `Html.Raw`.
- Nút Dừng:
  - POST có antiforgery (Razor Pages tự kiểm + kiểm thêm thủ công);
  - dịch vụ chỉ dừng tiến trình `agy.exe` (H-001).
- Chạy `HUB_FAKE=1`, mở trình duyệt:
  - trang Agent (bảng đang chạy, tự làm mới, 20 lần chạy gần nhất có xem phản hồi) đúng thiết kế;
  - trang Hộp thư (form gửi, danh sách Chưa xử lý / Đã nhận) đúng thiết kế;
  - thanh bên lấy dự án động, có ChinaAndroidToolbox.