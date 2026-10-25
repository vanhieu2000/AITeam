# H6-07 · Hiệu suất và Nhân viên

- **Agent:** B
- **Nhánh:** `agent-b/H6-07-hieu-suat-va-nhan-vien`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~250 dòng
- **Yêu cầu:** R-007
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có chức năng mới: xem Nhân viên ngay trong màn hình Hiệu suất.
- Đưa Tổng quan sang `/Performance`, đổi nhãn thành Hiệu suất.
- Giữ nguyên số đo, biểu đồ, bộ lọc và handler hiện có.
- Tái dùng phần Team; thông báo rõ khi rỗng hoặc không đọc được dữ liệu.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `hub/Pages/Index.cshtml`
- `hub/Pages/Index.cshtml.cs`
- `hub/Pages/Index.v3.cs`
- `hub/Pages/Shared/_PerformanceStaff.cshtml`
- `hub/Pages/Team.cshtml`
- `hub/Pages/Team.cshtml.cs`
- `hub/Services/TeamInsightsService.cs`
- `hub.tests/AcceptanceH302IndexTests.cs`
- `hub.tests/AcceptanceH607PerformanceTests.cs`

## Bối cảnh
Kế hoạch H6-07 đã duyệt là đặc tả bước này. Index.v3.cs đã có phép tính tổng quan;
TeamModel có phép chiếu dữ liệu và helper. Tái dùng chúng. Không có dịch vụ mới cần DI;
ITeamInsightsService và Razor Pages hiện có được dùng lại. Menu do task khác quản lý.

## Phạm vi (được sửa)
- `hub/Pages/Index.cshtml`
- `hub/Pages/Index.cshtml.cs`
- `hub/Pages/Shared/_PerformanceStaff.cshtml`
- `tasks/H6-07-hieu-suat-va-nhan-vien.md` (chỉ báo cáo)

## Không làm
- Không sửa test nghiệm thu `hub.tests/AcceptanceH607PerformanceTests.cs` hoặc test cũ.
- Không sửa Team.cshtml, TeamModel, Index.v3.cs, dịch vụ, hợp đồng, layout/menu.
- Không xóa trang /Team, không tính lại tỷ lệ/điểm, không chạy agy/codex/git.
- Không cấu hình redirect /Overview ngoài phạm vi; ghi nhận nếu cần cho quản lý.

## Ràng buộc
- Tab chuẩn `overview` (mặc định), `staff`; so khớp chính xác, null/rỗng/lạ về overview.
- Giữ constructor cũ, dịch vụ tùy chọn, giới hạn 20 task, khung giờ và handler hiện có.
- LoadPerformanceStaff chạy sau LoadV3; tab staff nạp TeamModel.OnGet bằng dịch vụ hiện có.
- Dịch vụ null: StaffView vẫn có mô hình rỗng, hiện hai thông báo rỗng của Team.
  Có thể thêm adapter rỗng nội bộ trong Index.cshtml.cs; không đổi hợp đồng/DI.
- Giữ nguyên lỗi “Không đọc được dữ liệu đánh giá: ” + ex.Message; trang vẫn dựng được.
- Nhân viên đủ tên/mã agent, task/lượt/phút, hai tỷ lệ, lỗi AG, 5 nguyên nhân, 7 giai đoạn.
- Link chuyển tab giữ range chuẩn hóa; giữ link range dạng `?range=...` tương thích test cũ.
- Razor mã hóa tên, tiêu đề, lỗi và query; không Html.Raw cho dữ liệu ngoài, không query trong script.
- Không đổi dữ liệu cũ thiếu phiếu/đánh giá; không đọc dữ liệu thật hoặc USERPROFILE trong test.
- .cs/.cshtml lưu UTF-8 có BOM; test nghiệm thu giữ nguyên.

## Giao diện
- /Performance có tiêu đề Hiệu suất, tab Tổng quan / Nhân viên; chỉ hiện nội dung tab đang chọn.
- Chép trình bày Team sang partial, dùng helper TeamModel, giữ nguyên trang Team cũ.
- Khung dữ liệu `min-width:0`, chuỗi dài `overflow-wrap:anywhere`; bảng/biểu đồ `overflow-x:auto`
  cục bộ, không làm trang rộng hơn viewport 1000px.
- Reviewer chụp 1000×800 và 1280×800, hai tab với tên dài, trạng thái rỗng/lỗi, lưu
  `docs/screenshots/H6-07/`; đo document.scrollWidth <= innerWidth.
  Test CSS chỉ kiểm tra hợp đồng, không thay thế đo trình duyệt.

## Test nghiệm thu
`hub.tests/AcceptanceH607PerformanceTests.cs`

## Tiêu chí hoàn thành
- [ ] /Performance dựng Razor thật, tiêu đề/tab đúng; tab lạ về overview.
- [ ] Số đo/khung giờ bằng trước khi đổi, query/bộ lọc/handler cũ còn hoạt động.
- [ ] Nhân viên tương đương TeamModel, giữ dữ liệu cũ thiếu đánh giá/phiếu.
- [ ] Null/rỗng/lỗi rõ ràng; HTML trong dữ liệu ngoài được mã hóa.
- [ ] Không tràn trang 1000px; có ảnh và phép đo khi review.
- [ ] `dotnet build AITeam.sln` 0 lỗi, không warning mới khi hoàn thiện.
- [ ] `dotnet test AITeam.sln` pass, không sửa/Skip nghiệm thu.

## Báo cáo của agent
| Tiêu chí | Bằng chứng thực tế |
|---|---|
| /Performance, tiêu đề, tab, tab lạ | Test Razor thật và chuẩn hóa tab: H607 18/18 pass trong lệnh test riêng bên dưới. |
| Số đo, khung giờ, bộ lọc, handler cũ | H607 + H302: 30/30 pass, 0 skip; dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceH607PerformanceTests&#124;FullyQualifiedName~AcceptanceH302IndexTests" --blame-hang-timeout 2m. |
| Nhân viên tương đương Team, dữ liệu cũ | Test phép chiếu TeamModel, task thiếu phiếu/đánh giá pass; không sửa dịch vụ, hợp đồng hay Team. |
| Null/rỗng/lỗi, mã hóa HTML | Test dịch vụ null/rỗng/lỗi và Razor mã hóa lỗi pass; giữ nguyên thông báo lỗi và hai thông báo rỗng. |
| Không tràn 1000px, ảnh review | Test hợp đồng CSS pass: min-width:0, overflow-wrap:anywhere, overflow-x:auto; CHƯA đo trình duyệt/chụp ảnh, Claude thực hiện theo phân công. |
| Build | dotnet build AITeam.sln --no-restore -m:1: thành công, 0 lỗi, 0 warning. |
| Test toàn solution | dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m: 624 pass, 1 fail, 0 skip (Hub 403/404; Core 221/221); không sửa test nghiệm thu. |
File đổi: Index.cshtml — route/tab, giữ tổng quan, CSS; Index.cshtml.cs — nạp TeamModel và adapter rỗng; _PerformanceStaff.cshtml — tái dùng trình bày Team; task này — báo cáo.
Quyết định: chỉ chuỗi chính xác staff chọn Nhân viên; giữ range đã chuẩn hóa; khởi tạo PageContext từ ViewContext khi dựng Razor trực tiếp thiếu context.
Môi trường: build đầu thất bại không chẩn đoán, lượt --no-restore báo thiếu assets; dotnet restore AITeam.sln -m:1 --ignore-failed-sources -p:NuGetAudit=false thành công rồi build lại xanh.
Chưa đạt: AntiforgerySecurityTests.Pages_render_successfully_with_fake_services("/Overview") nhận 404 thay vì 200; task cấm cấu hình redirect /Overview và sửa test cũ, cần Claude quyết định tương thích route.
Kiểm tra byte: 3 file code và 2 file test chỉ đọc đều EF-BB-BF; không chạy app/chụp ảnh/git; hệ thống tự commit/push; phiên này không có công cụ aiteam_post_message.
## Review (Claude)
Chờ review.