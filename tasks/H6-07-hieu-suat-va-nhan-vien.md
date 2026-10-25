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
Antigravity điền bảng từng tiêu chí → bằng chứng (lệnh, số pass/warning, ảnh),
file thay đổi và lý do, quyết định tự đưa ra, điều chưa xác minh.

### Chuẩn bị bước 4 (Codex)
| Tiêu chí | Bằng chứng |
|---|---|
| Khung biên dịch | `dotnet build AITeam.sln -m:1 -v:normal`: thành công, 0 lỗi, 8 warning NU1900 do không truy cập api.nuget.org; đã dùng tổng cộng 2 lượt build |
| Nghiệm thu đỏ | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceH607PerformanceTests --logger "console;verbosity=quiet"`: 18 thất bại, 0 pass, 0 skip; chưa triển khai thân hàm |
| BOM | Kiểm tra byte đầu 4 file C#/Razor: EF BB BF |
| Phạm vi | Sửa Index.cshtml/Index.cshtml.cs để đặt khung; tạo _PerformanceStaff.cshtml, AcceptanceH607PerformanceTests.cs; điền task |
Quyết định: dùng TeamModel làm mô hình partial để tái dùng phép chiếu và helper; không thêm dịch vụ/DI.
Dữ liệu test là đối tượng giả lập theo hợp đồng đang dùng, không phải bản sao dữ liệu thật.
Chưa xác minh: dựng trang sau triển khai, layout/ảnh và phép đo 1000px; chưa chạy toàn bộ test solution.
Không chạy git theo chỉ thị bước 4; hệ thống tự commit. Báo cáo này không thay báo cáo triển khai của Antigravity.

## Review (Claude)
Chờ review.