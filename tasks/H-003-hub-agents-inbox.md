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
- [ ] Build 0 warning; test pass. Test PageModel cho Agents (Stop gọi đúng PID, không có antiforgery thì bị từ chối) và Inbox (Add; nội dung rỗng → báo lỗi, không ghi).
- [ ] Chạy `HUB_FAKE=1` và chạy với dữ liệu thật (`AITEAM_HOME=D:\AITeam`, chỉ XEM, không bấm Dừng với agent thật); các trang hiển thị đúng.
- [ ] Không có chỗ nào render HTML thô từ dữ liệu (`Html.Raw`) ngoài kết quả của `MarkdownRenderer`.

## Báo cáo của agent
<!-- Agent điền -->

## Review (Claude)
<!-- Claude điền -->
