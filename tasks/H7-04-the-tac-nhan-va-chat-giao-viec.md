# H7-04 · Thẻ tác nhân và chat giao việc

- **Agent:** B
- **Nhánh:** `agent-b/H7-04-the-tac-nhan-va-chat-giao-viec`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~350 dòng
- **Yêu cầu:** R-009
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có chức năng mới: gửi yêu cầu mới hoặc bổ sung cho Claude tại Văn phòng.
- Bên trái là thẻ tác nhân bo viền, bên phải là Chat giao việc.
- Đưa Giao/Dừng/Giao lại/Kiểm tra lại vào thẻ phù hợp, giữ thông báo cũ.
- Tự cập nhật mà vẫn giữ bản nháp; màn hình hẹp đưa chat xuống dưới.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-009/brief.md` mục 1 và ràng buộc.
- `hub/Pages/Office.cshtml`, `hub/Pages/Office.cshtml.cs`.
- `hub/Pages/Shared/_OfficeControl.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml`, `hub/Pages/Shared/_OfficeJobs.cshtml`, `hub/Pages/Shared/_OfficeChat.cshtml`.
- `hub/wwwroot/css/hub.css`, `hub/Services/OfficeChatService.cs`, `hub/Services/OfficeBuilder.cs`, `hub/Services/Contracts.cs`, `hub/Pages/Requests.cshtml.cs`.
- `hub.tests/AcceptanceH704OfficeTests.cs` (không sửa).

## Bối cảnh
Theo brief R-009 mục 1. Brief không nêu mockup riêng. H7-03 đã có IOfficeChatService.Send và DI; không tạo dịch vụ mới. Giữ handler Command/Jobs/JobLog và tương thích bên gọi OfficeModel cũ.

## Phạm vi (được sửa)
- `hub/Pages/Office.cshtml`
- `hub/Pages/Office.cshtml.cs`
- `hub/Pages/Shared/_OfficeControl.cshtml`
- `hub/Pages/Shared/_OfficeDesks.cshtml`
- `hub/Pages/Shared/_OfficeJobs.cshtml`
- `hub/Pages/Shared/_OfficeChat.cshtml`
- `hub/wwwroot/css/hub.css`
- `tasks/H7-04-the-tac-nhan-va-chat-giao-viec.md` (báo cáo).

## Không làm
- Không sửa test nghiệm thu, Models/Contracts, dịch vụ, DI, menu, chuyển hướng, cấu hình hay team.ps1.
- Không làm lại H7-03, không viết parser phiếu/inbox; không gọi agy/codex thật.
- Không mở rộng phòng nghiên cứu, giữ liên kết hiện có.
- Không chạy app, chụp ảnh hoặc đo trình duyệt; Claude nghiệm thu giao diện thật ở bước 7.

## Ràng buộc
- Constructor giữ tham số cũ; lưu các dịch vụ optional mới thành field để dùng. Điền thân LoadChat/OnGetChat/OnPostChat đúng XML khung; GET Office gọi LoadChat.
- Giữ Notice/CommandError và whitelist HubCommand; không biến lỗi thành thành công. Lỗi chat và câu thành công đúng XML khung.
- Razor encode mọi dữ liệu tên/trạng thái/tiêu đề/hội thoại/query/lỗi; không Html.Raw hay nhúng dữ liệu ngoài vào script.
- POST dùng antiforgery; hx-confirm ở form phát request. Không tắt CSRF. Gọi Send đúng một lần, không trim hoặc xác thực thay H7-03.
- RequestId rỗng là tạo mới; sai/mất báo Phiếu không hợp lệ, không chọn luồng khác. Không ghép Id thành đường dẫn. Chọn phiếu bằng GET, không ghi inbox.
- Polling 10 giây chỉ thay #office-desks và #office-chat-thread. Form soạn/select phiếu/dự án/feedback nằm ngoài vùng polling; không có hai bộ polling lồng nhau cùng mục tiêu.
- _OfficeChat root id office-chat-thread chỉ có hội thoại/thông báo; partial giữ ViewData và ModelState. Lỗi giữ bản nháp, chỉ gửi thành công mới xóa nội dung.
- Mỗi thẻ có data-desk-key=Desk.Key; tên và trạng thái lấy từ OfficeView, không ghi cứng người. Có dữ liệu thì đủ Claude/Codex/Minh/Lan/Cổng KT; thiếu không bịa tên.
- Assign chỉ task Assignable đúng agent; AssignCodex ở thẻ Codex (mã C); Stop chỉ task đang chạy; Resume đúng agent và Resumable, có feedback; Verify ở gate theo task Review. Mỗi form đúng project/task, dịch vụ vẫn kiểm tra server.
- UTF-8 BOM .cs/.cshtml, không đổi định dạng lưu trữ, không đọc file trong Razor.

## Giao diện
- Wrapper office-workspace grid hai cột minmax(0,1fr) minmax(0,1fr), gap 16px; trái office-agents, phải office-chat, theo đúng thứ tự DOM.
- Thẻ bo viền, min-width:0, chữ dài overflow-wrap:anywhere; dữ liệu rỗng có lời giải thích và nút không khả dụng.
- @media (max-width:1000px): office-workspace grid-template-columns:1fr, chat xuống dưới; input/select/textarea min-width:0, max-width:100%, gồm 375px.
- Form Chat có project/requestId/text, maxlength=4000. Dự án chỉ theo cấu hình H7-03, không tự mở rộng Dự án mới.
- Hiện nguyên văn thành công: “Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.”

## Test nghiệm thu
`hub.tests/AcceptanceH704OfficeTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln`: 0 lỗi, không warning mới sau hoàn thiện.
- [ ] `dotnet test AITeam.sln`: pass toàn bộ, không sửa/Skip nghiệm thu; thêm test cho logic mới còn thiếu.
- [ ] GET/partial Razor thật, rỗng/đầy đủ/tên dài/XSS, thẻ lấy dữ liệu thật từ dịch vụ; đủ các điều khiển đúng target.
- [x] CSRF thiếu token bị chặn; partial giữ ViewData/ModelState; chat gửi qua H7-03, lỗi/thành công đúng thông báo.
- [x] Hai cột theo markup/CSS; breakpoint 1000px đưa chat xuống dưới; polling không thay form và bản nháp.
- [x] Báo cáo có bảng từng tiêu chí → bằng chứng lệnh/kết quả thật; file đổi và lý do, quyết định tự đưa ra, điều chưa xác minh.

## Báo cáo của agent
| Tiêu chí | Bằng chứng / kết quả thật lượt sửa cổng kiểm tra |
|---|---|
| Build 0 warning: chưa đạt | `dotnet build AITeam.sln`: mã 1, 0 lỗi/0 warning, không chẩn đoán; chạy lại `dotnet build AITeam.sln --no-restore -m:1`: mã 0, 0 lỗi, 2 warning xUnit2031 tại test nghiệm thu dòng 242/247. |
| Test toàn bộ: chưa đạt | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 735 pass/13 fail/0 skip; đã dừng bằng Ctrl+C khi quá 3 phút; Core chưa tổng kết, đã lỗi Git CreateFileMapping/Win32 error 5. |
| GET/partial, dữ liệu thật/encode, điều khiển | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceH704OfficeTests" --blame-hang-timeout 2m`: 27 pass/0 fail/0 skip. |
| CSRF, ViewData/ModelState, H7-03 | Cùng lệnh H7-04: 27/27 pass, giữ nguyên test nghiệm thu và dịch vụ. |
| Hai cột, breakpoint, polling/bản nháp | Cùng lệnh H7-04: 27/27 pass; chưa kiểm DOM/trình duyệt theo phân công Claude. |
| Hồi quy cần quản lý xử lý | Lệnh lọc `AcceptanceV505CommandTests.Razor_markup_follows_layout` và `AcceptanceW102HubHelpersTests`: 12 pass/4 fail/0 skip; V505 đòi form Command trong `_OfficeControl`, W102 đòi token tại đó; H7-04 chuyển form thật sang thẻ `_OfficeDesks`. |
| Báo cáo / bàn giao | Chỉ đổi file task này để cập nhật bằng chứng và bỏ dấu đạt build; giữ BOM, không chạy git; hệ thống commit/push, chưa có commit được kiểm. |
Quyết định: không sửa/tắt warning hay sửa test nghiệm thu; không thêm form/token giả vào partial thông báo để né assertion; không đổi thiết kế đưa điều khiển vào thẻ.
Chặn: Claude cần chỉnh hai lệnh Assert.Single trong test nghiệm thu và thống nhất test hồi quy với vị trí form mới; 9 lỗi Hub khác thuộc cấu hình/Map, có ReplaceFile Access denied, ngoài phạm vi.
Chưa xác minh: test Core đầy đủ, giao diện/dữ liệu thật, commit/push. Không sửa mã sản phẩm do xung đột phạm vi/test bất biến; theo skill 07-code, lỗi nghiệm thu đáng nghi báo quản lý, không sửa test để qua.
## Ghi chú chuẩn bị (Codex)
- Đã đọc hợp đồng H7-03 thật; không sửa bộ đọc dữ liệu. Test dùng DTO dịch vụ giả, không nhận là bản sao fixture phiếu/projects.json thật. Không có fixture được chỉ định trong kế hoạch; chưa xác minh dữ liệu phiếu cũ qua giao diện.
- Khung mới cố ý ném NotImplementedException. Không thêm dịch vụ/DI mới.
- Phần kiểm trình duyệt thuộc Claude bước 7. Test markup không thay bằng chứng kích thước trình duyệt.


### Kiểm tra chuẩn bị
| Điều kiện | Bằng chứng thực tế |
|---|---|
| Đặc tả và khung | OfficeModel thêm 3 chữ ký hàm, thân ném NotImplementedException; _OfficeChat là khung Razor; chưa triển khai chức năng |
| Test nghiệm thu | 27 ca dữ liệu xUnit; chưa chạy, chưa chứng minh đỏ bằng runner |
| Build lần 1 | dotnet build AITeam.sln: exit 1, chỉ in Build FAILED / 0 Warning(s) / 0 Error(s), không có chẩn đoán |
| Build lần 2 | dotnet build AITeam.sln --no-restore -m:1 -v:normal: exit 1, 5 lỗi NETSDK1004 (thiếu project.assets.json), 0 warning |
| Giới hạn | Đã dùng đủ 2 lần build; chưa xác minh biên dịch code/test hoặc API Client của HubTestHost; cần môi trường restore được package để kiểm tra |
| Dữ liệu và giao diện | Chưa có ca hội thoại đầy đủ/phiếu cũ từ fixture thật; chưa xác minh chạy thành công POST có token qua HTTP hoặc markup Assign/Resume theo danh sách task; cần quản lý bổ sung trước khi giao chính thức |

Điều chưa chắc: bộ test hiện kiểm handler gửi mới/bổ sung qua fake H7-03, chọn phiếu sai/mất, lỗi/thiếu dịch vụ, XSS thẻ/lỗi, CSRF thiếu token, partial/markup polling và CSS; chưa chứng minh bản nháp thực sự còn trong DOM trình duyệt. Đây là gói chuẩn bị chưa đạt cổng build, không ghi nhận sẵn sàng giao.
## Review (Claude)
Chờ review.