# H6-08 · AI & tài khoản: khung và Trạng thái

- **Agent:** B
- **Nhánh:** `agent-b/H6-08-ai-tai-khoan-khung-va-trang-thai`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~230 dòng
- **Yêu cầu:** R-007
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có: thêm trang AI & tài khoản tại /AI, mặc định mở Trạng thái.
- Gom điều hướng thành bốn tab; tab lạ trở về Trạng thái.
- Hiển thị tài khoản từ dịch vụ hiện có, dùng lại hàng và thao tác cũ.
- Giữ đường dẫn cũ, kiểm tra theo yêu cầu, không đưa bí mật vào HTML.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `tasks/H6-08-ai-tai-khoan-khung-va-trang-thai.md`
- `hub/Pages/AI.cshtml`
- `hub/Pages/AI.cshtml.cs`
- `hub/Pages/Shared/_AiStatus.cshtml`
- `hub/Pages/AiAccounts.cshtml`
- `hub/Pages/AiAccounts.cshtml.cs`
- `hub/Pages/Shared/_AiAccountRow.cshtml`
- `hub/Services/Contracts.cs`
- `hub.tests/AcceptanceH608AiTests.cs`

## Bối cảnh
Theo kế hoạch H6-08 đã duyệt. Dùng IAiAccountsService đã đăng ký cho trang cũ; Razor Pages tự kích hoạt AIModel qua DI, không thêm dịch vụ.
Kế hoạch chưa nêu đường dẫn mockup. Giả định khung: status (Trạng thái), accounts (Tài khoản), providers (Nhà cung cấp), vault (Kho bí mật). Claude đối chiếu tên/bố cục với mockup ở bước 7; không triển khai nội dung ba tab còn lại.

## Phạm vi (được sửa)
- `hub/Pages/AI.cshtml`
- `hub/Pages/AI.cshtml.cs`
- `hub/Pages/Shared/_AiStatus.cshtml`

## Không làm
- Không sửa hợp đồng, dịch vụ, dữ liệu lưu, trang cũ, layout chung.
- Không thêm handler Save/Disable/Check; không chạy agy/codex hay đọc kho bí mật.
- Không triển khai nội dung quản trị ba tab còn lại; chỉ khung và liên kết trang cũ.
- Không sửa `hub.tests/AcceptanceH608AiTests.cs` (nghiệm thu chuẩn bị sẵn, ngoài ngân sách sản phẩm).

## Ràng buộc
- Ngân sách sản phẩm ~230 dòng. Thay TODO bằng triển khai, không giữ NotImplementedException.
- Giữ chữ ký AIModel(IAiAccountsService accounts), Tab, Rows, Error, OnGet(string? tab = null). Chỉ nhận bốn khóa tab chính xác, phân biệt hoa/thường; null/rỗng/khoảng trắng/tab lạ về status.
- GET /AI chỉ gọi List một lần ở status; tab khác không gọi dịch vụ. Không CheckAsync khi tải trang. Lỗi List: Rows rỗng, Error chính xác "Không đọc được danh sách tài khoản AI."; không đưa ex.Message ra HTML vì có thể chứa bí mật.
- Trạng thái: bảng sáu cột Dịch vụ/Kết nối qua/Chế độ/Trạng thái/Kiểm tra lúc/Thao tác; từng hàng dùng _AiAccountRow; không ghi cứng dịch vụ. Hiện câu "Không lưu mật khẩu, cookie hay khóa API." và Error đã mã hóa.
- Bốn liên kết /AI?tab=... trong vùng aria-label="Các tab AI"; đúng một aria-current="page". Tiêu đề "AI & tài khoản".
- Tab accounts/providers/vault có liên kết tương ứng /AiAccounts, /Providers, /Vault. Chỉ khung, không nhân bản nội dung cũ.
- Giữ form _AiAccountRow: POST /AiAccounts?handler=Save, POST /AiAccounts?handler=Disable; GET /AiAccounts?handler=Check&id=... trả hàng HTMX. Antiforgery bắt buộc, alias mã hóa bằng Razor, không Html.Raw hay script nhận query/form.
- Sau POST giữ RedirectToPage() của /AiAccounts, trang cũ tương ứng tab Tài khoản. Không thêm returnUrl hay sửa handler ngoài phạm vi. Chưa có redirect tự động về /AI: yêu cầu này cần Claude xác nhận vì thay trang cũ vượt phạm vi đã duyệt.
- Giữ /AiAccounts và handler cũ tương thích dữ liệu cũ, alias, mode; lỗi Save và Ngắt giữ nguyên.
- .cs/.cshtml UTF-8 có BOM. Test dùng TestEnvironment và HubPaths tạm, probe giả, không truy cập hồ sơ thật hay chạy CLI.

## Test nghiệm thu
`hub.tests/AcceptanceH608AiTests.cs`

## Tiêu chí hoàn thành
- [ ] Dựng thật /AI và mọi tab qua Razor Pages; default/fallback/đánh dấu tab đúng.
- [ ] Dữ liệu động đủ chưa kết nối/thủ công/tắt; lỗi đọc an toàn; không probe khi GET.
- [ ] Dùng lại hàng; alias HTML được mã hóa; form có antiforgery và chỉ tới endpoint cũ.
- [ ] Kiểm tra bằng probe giả; lỗi lưu, ngắt giữ alias; URL và redirect cũ tương thích.
- [ ] `dotnet build AITeam.sln` thành công, không warning mới sau triển khai.
- [ ] `dotnet test AITeam.sln` pass, không sửa test nghiệm thu.

## Báo cáo của agent
Agent triển khai điền bảng từng tiêu chí → bằng chứng (lệnh và kết quả thật, số test pass, số warning), file đổi và lý do, quyết định tự đưa ra, điều chưa xác minh.
Chuẩn bị Codex: thân OnGet để TODO; test được phép đỏ. Chưa đối chiếu mockup và chưa xác minh redirect về tab do giới hạn phạm vi. Kết quả kiểm tra chuẩn bị xem thông báo bàn giao.


### Kiểm tra lúc chuẩn bị (Codex)
| Tiêu chí chuẩn bị | Bằng chứng thật |
|---|---|
| Khung biên dịch | `dotnet build AITeam.sln` lần 1: exit 1 nhưng 0 lỗi/0 warning, không có chẩn đoán. Lần 2 `dotnet build AITeam.sln -m:1 -v:normal`: thành công, 0 lỗi, 1 warning CS9113 được cho phép, 23,22 giây. Không chạy build lần 3. |
| Nghiệm thu đỏ trước triển khai | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceH608AiTests --logger "console;verbosity=quiet"`: tổng 28, pass 0, fail 28, skip 0. Chưa chạy toàn bộ solution test. |
| Mã hóa | Kiểm tra byte đầu cả bốn file .cs/.cshtml: EF BB BF, đủ BOM UTF-8. |
| Phạm vi | Tạo AI.cshtml (khung Razor), AI.cshtml.cs (hợp đồng PageModel, thân OnGet TODO), _AiStatus.cshtml (khung partial), AcceptanceH608AiTests.cs (28 ca); điền file task này. Không thay endpoint cũ hay hợp đồng. |
| Điều chưa xác minh | Tên/bố cục bốn tab theo mockup chưa được cung cấp; redirect tự động về tab /AI cần thay trang cũ ngoài phạm vi. Đã ghi giả định và giới hạn bên trên. Chưa kiểm tra giao diện thật. |
## Review (Claude)
<!-- Claude điền sau nghiệm thu. -->