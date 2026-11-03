# H7-05 · Nhúng Phòng nghiên cứu trong Văn phòng

- **Agent:** C
- **Nhánh:** `agent-c/H7-05-nhung-phong-nghien-cuu`
- **Yêu cầu:** R-012 (tiếp nối R-009)
- **Ngân sách:** ~250 dòng triển khai, test/tài liệu riêng
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M

## Tóm tắt
- Dùng chung phần nghiên cứu giữa Văn phòng và Research; giữ hỏi tự động, thủ công, copy lời nhắn, lưu kết quả và polling.
- Chọn phiếu trong Chat giao việc cũng chọn đúng phiếu nghiên cứu; không có phiếu thì khóa gửi.

## Đọc
`plans/R-009/plan.md`, `docs/MO-TA-HE-THONG.md`, `hub/Pages/Office.cshtml*`, `hub/Pages/Research.cshtml*`, `hub/Services/ResearchService.cs`, `hub.tests/AcceptanceV507ResearchPageTests.cs`.

## Phạm vi
- `hub/Pages/Office.cshtml`, `hub/Pages/Office.cshtml.cs`, `hub/Pages/Research.cshtml`, `hub/Pages/Research.cshtml.cs`.
- Tạo mới `hub/Pages/Shared/_OfficeResearch.cshtml`, `hub/Pages/Shared/_ResearchPanel.cshtml`, `hub.tests/H705EmbeddedResearchTests.cs`.
- File task này và `tasks/README.md`.

## Không làm
- Không sửa assert/test cũ, không thay dịch vụ Research hoặc dữ liệu thật, không mang H8-14/V9-04 sang nhánh, không merge main.

## Tiêu chí
- [x] Tách partial dùng chung, form POST về đúng trang, không lồng form.
- [x] Phiếu chọn truyền đúng; chưa chọn/mã sai không gọi AI; đổi phiếu không lẫn kết quả.
- [x] Test tự động/lỗi, thủ công lưu/đọc lại, CSRF/XSS; Research cũ chạy cùng partial.
- [ ] Tất cả nghiệm thu cũ đạt: đang xung đột test đọc trực tiếp source.

## Test nghiệm thu
Giữ nguyên `hub.tests/AcceptanceV507ResearchPageTests.cs`; bổ sung `hub.tests/H705EmbeddedResearchTests.cs` dùng HubTestHost, dữ liệu tạm và dịch vụ giả, không gọi AI thật.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Nhúng/chọn phiếu | 3 test H705 mới đều đạt; HTTP thật, hỏi/lưu cùng dịch vụ và phiếu, không ghi file thật. |
| An toàn/hồi quy route | Test mới kiểm thiếu token 400, ID sai không gọi AI, HTML được encode, /Research vẫn đọc kết quả. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Xung đột nghiệm thu | AcceptanceV507ResearchPageTests.Razor_markup_follows_layout yêu cầu asp-page-handler="Ask" trực tiếp trong Research.cshtml; H7-05 chuyển form vào partial dùng chung và chọn handler theo trang. |
| Toàn bộ | Hub 778/793 đạt, 15 lỗi (14 Windows + 1 test source xung đột); Core 397/690 đạt, 293 lỗi nền Windows/4 trích dẫn tài liệu lệch. 0 skip. |
- DỪNG H7-05 theo yêu cầu chủ dự án khi gặp test cũ xung đột; không sửa assert, không chèn comment/markup giả để qua test, chưa nghiệm thu giao diện cuối.
- File sửa/tạo đúng Phạm vi; giữ BOM/CRLF. Quyết định dùng lại ResearchModel/IResearchService, partial poll không thay khung soạn Chat giao việc.
- Phút: chưa đo. Token: chưa đo. Cần quản lý chốt test source so với yêu cầu tách partial trước khi tiếp tục; không merge main.
