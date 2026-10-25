# Kế hoạch R-009
## Hiểu yêu cầu
Nâng Hub v6.1 cho HieuDV: Văn phòng tập trung thẻ tác nhân, nghiên cứu và chat giao việc.
Thu gọn menu, thêm danh sách dự án, chuyển Đơn duyệt sang bảng và thêm Map phân vai.
Giữ H6-01, H6-07, H6-08; thay phần R-007 chưa làm; không đổi dữ liệu trạng thái.
Bước này chỉ đọc; chưa chạy build/test vì các lệnh đó tạo file.
## Research
- `projects.json:23–25`: A phụ trách Services, Program, test dịch vụ; B phụ trách Pages, wwwroot, DevData.
- `AGENTS.md`, mục “Lỗi hay gặp”: giữ BOM, đặt `hx-confirm` trên form, không `Html.Raw` dữ liệu ngoài, không test bằng dữ liệu thật.
- `docs/HUB-DESIGN.md:17–20,76–104`: localhost, Razor/HTMX, POST có antiforgery; dịch vụ nhận đường dẫn để test cô lập.
- `hub/Pages/Office.cshtml:1–30`: `/` đang là Office; thẻ và điều khiển nằm trong hai partial riêng.
- `hub/Pages/Shared/_OfficeDesks.cshtml:80–110`: nghiên cứu hiện chỉ là liên kết; Dừng/Kiểm tra lại đã dùng handler Command.
- `hub/Services/OfficeService.cs:43–51`: thành viên đọc từ `staff/*.md`; không được ghi cứng dữ liệu tác nhân.
- `hub/Pages/Requests.cshtml:60–114`, `Requests.cshtml.cs:68–80`: chat đọc nhật ký phiếu; gửi yêu cầu chỉ ghi inbox, chưa gửi bổ sung theo phiếu.
- `hub/Services/RequestService.cs:101–107`: hợp đồng phiếu chỉ đọc; không có API tạo phiếu hoặc nối tin nhắn.
- `hub/Services/ResearchService.cs:23–26,46–47`: đã có ChatGPT tự động và Thủ công; tái sử dụng.
- `hub/Pages/Approvals.cshtml.cs:41–58`: đã có Decide, kiểm tra quyết định và thông báo; chỉ cần đổi trình bày.
- `hub/Pages/Shared/_Layout.cshtml:66–220`: menu hiện còn các mục cần bỏ, danh sách dự án động và ô Antigravity.
- `team.ps1:749–750,793,856`: v4 mặc định Codex plan/prep/QC; plan có `--read-only`.
- `team.ps1:603,765–768`, `core/AITeam.Cli/Program.cs:250–260`: QC qua bộ định tuyến reviewer, chưa nhận người được gán từ Map.
- `hub/Services/TeamConfigService.cs:245–263,367–381`: giữ khóa khác khi lưu Settings, nhưng hàm ghi hiện tại chưa nguyên tử.
- `hub.tests/AcceptanceV507ResearchPageTests.cs:265–293`: đã có mẫu dựng Razor thật, HTTP và HubPaths tạm; không có thư mục `tests/fixtures/`, có `hub.tests/Fixtures/`.
- CHƯA XÁC MINH: quy ước JSON cho `workflow.roles`, xử lý tác nhân không có CLI và quyền đổi người duyệt/publish; cần chốt trước prep.
## Rủi ro
- Map vô tình cấp quyền chạy lệnh/merge → phân vai không thay thế xác thực, phê duyệt và cấm tự duyệt.
- JSON hỏng hoặc lưu đồng thời với Settings làm mất cấu hình → kiểm tra kiểu, không ghi khi đọc lỗi, phát hiện xung đột và thay file nguyên tử.
- Dữ liệu cũ thiếu workflow/roles → dùng mặc định v4, GET không tự ghi/migrate.
- XSS, CSRF, chèn đường dẫn/lệnh qua phiếu hoặc vai trò → mã hóa Razor, antiforgery, danh sách cho phép; không ghép lệnh từ ghi chú.
- Polling thay cả Văn phòng làm mất nội dung đang gõ → chỉ cập nhật vùng trạng thái/kết quả, giữ form và phiếu đang chọn.
- Bảng/thẻ tràn ở 1000px → min-width:0, ngắt chuỗi dài, breakpoint ≤1000px và kiểm tra trình duyệt thật.
- Phạm vi vượt areas → Claude ghi ngoại lệ rõ cho A sửa `team.ps1` và phần CLI cần thiết; không sửa hợp đồng đã khóa.
## Task
Khung và test nghiệm thu do Codex chuẩn bị trước; mỗi task ≤400 dòng triển khai, ≤15 phút agent.
Mỗi task phải build 0 warning, test pass; báo cáo từng tiêu chí → bằng chứng; không sửa test nghiệm thu.
### H7-01 · Dịch vụ Map và lưu nguyên tử
- **Agent:** A
- **Ngân sách:** ~300 dòng
- **Phạm vi:** `hub/Services/WorkflowRolesService.cs` · `hub/Services/TeamConfigService.cs` · `hub/Program.cs`
- **Làm:**
  - Thêm hợp đồng riêng trong Services; 11 mã nghiệp vụ ổn định, 6 tác nhân, ghi chú giới hạn 500 ký tự; không sửa Contracts/HubModels.
  - Thiếu roles → mặc định v4: Claude tiếp nhận/ý tưởng/duyệt kế hoạch/nghiệm thu/publish; Codex research/plan/prep/QC; Minh code; Cổng KT kiểm thử.
  - Chỉ cập nhật `workflow.roles`; giữ khóa lạ và BOM; ghi tạm cùng thư mục rồi thay nguyên tử, phối hợp đường ghi Settings để tránh mất cập nhật.
  - Dữ liệu sai → “Phân vai không hợp lệ.”; JSON hỏng → “Không đọc được cấu hình phân vai.”; xung đột → “Cấu hình đã thay đổi. Vui lòng tải lại.”
- **Test nghiệm thu:** thiếu/null/rỗng/sai kiểu; đủ 11 vai; tác nhân lạ/trùng/thiếu dòng; ghi chú biên; giữ khóa khác/BOM; JSON hỏng, lỗi ghi và xung đột không mất file.
- **Phụ thuộc:** —
### H7-02 · Điều phối plan/prep/QC theo Map
- **Agent:** A
- **Ngân sách:** ~350 dòng
- **Phạm vi:** `team.ps1` · `core/AITeam.Cli/Program.cs` · `core/AITeam.Core/Review/`
- **Làm:**
  - Đọc đúng ba vai plan/prep/qc theo schema H7-01; thiếu mục → giữ hành vi cũ; không ghi lại team.json.
  - Chọn tác nhân theo quyết định quản lý; chuyển danh tính thành provider bằng ánh xạ cố định, giữ plan chỉ đọc và hợp đồng kết quả hiện tại.
  - QC áp dụng cả lệnh riêng và luồng sau verify; giữ chặn tự duyệt, không tự đổi người được gán bằng fallback.
  - Vai không chạy tự động được → dừng rõ “Tác nhân được gán cần thực hiện thủ công.”; cấu hình sai → lỗi, mã thoát khác 0.
- **Test nghiệm thu:** mặc định cũ; đổi từng vai; không tác động bước khác; provider thiếu/tắt; chống tự duyệt; chuỗi độc hại; stub tiến trình chứng minh đúng người được gọi và cờ chỉ đọc.
- **Phụ thuộc:** H7-01
### H7-03 · Lõi chat Văn phòng và chuyển hướng
- **Agent:** A
- **Ngân sách:** ~220 dòng
- **Phạm vi:** `hub/Services/OfficeChatService.cs` · `hub/Program.cs`
- **Làm:**
  - Dùng IRequestService đọc phiếu, IInboxService gửi yêu cầu; tạo mới đi qua inbox hiện có, không tự tạo/sửa file phiếu.
  - Gửi bổ sung phiếu tồn tại → inbox chứa mã phiếu đã xác minh theo quy ước quản lý chốt; giữ giới hạn nội dung 4000 ký tự và dự án hợp lệ.
  - Giữ lỗi “Nội dung yêu cầu không được để trống.”, “Nội dung quá dài (tối đa 4000 ký tự).”, “Dự án không hợp lệ.”; phiếu sai → “Phiếu không hợp lệ.”
  - Chuyển GET `/Agents`, `/Inbox` sang `/` bằng 302 tại Program; không chạy handler cũ, không thay dịch vụ.
- **Test nghiệm thu:** gửi mới/bổ sung; phiếu mất/giả mạo/traversal; nội dung 0/4000/4001; dự án lạ; lỗi inbox; 302 Location đúng, không ghi trạng thái khi GET.
- **Phụ thuộc:** —
### H7-04 · Thẻ tác nhân và chat giao việc
- **Agent:** B
- **Ngân sách:** ~350 dòng
- **Phạm vi:** `hub/Pages/Office.cshtml*` · `hub/Pages/Shared/_Office*.cshtml` · `hub/wwwroot/css/hub.css`
- **Làm:**
  - Dựng hai cột: trái thẻ Claude/Codex/Minh/Lan/Cổng KT, phải Chat giao việc; lấy tên/trạng thái từ dữ liệu hiện có, thẻ bo viền.
  - Đưa Giao/Dừng/Giao lại/Kiểm tra lại vào thẻ phù hợp; tái sử dụng CommandService và giữ thông báo handler hiện tại.
  - Nối H7-03: chọn phiếu, xem hội thoại, tạo yêu cầu hoặc gửi bổ sung; thông báo thành công nêu rõ Claude nhận khi bắt đầu phiên.
  - Form POST có antiforgery; hx-confirm trên form; polling không xóa bản nháp; ≤1000px chat xuống dưới.
- **Test nghiệm thu:** dựng GET/partial thật; dữ liệu rỗng và đầy đủ; đủ thẻ; từng nút đúng task/lệnh; CSRF bị chặn; XSS được mã hóa; polling giữ bản nháp; ảnh 1280×800, 1000px và 375px.
- **Phụ thuộc:** H7-03
### H7-05 · Nhúng Phòng nghiên cứu
- **Agent:** B
- **Ngân sách:** ~250 dòng
- **Phạm vi:** `hub/Pages/Office.cshtml*` · `hub/Pages/Research.cshtml*` · `hub/Pages/Shared/_Research*.cshtml`
- **Làm:**
  - Tách phần nghiên cứu dùng chung, nhúng dưới khu thẻ; dùng lại IResearchService và hai lựa chọn ChatGPT/Thủ công.
  - Giữ hỏi tự động, copy lời nhắn, lưu câu trả lời/nguồn thủ công và polling kết quả; POST về đúng trang, không lồng form.
  - Phiếu chọn trong Văn phòng truyền đúng sang nghiên cứu; chưa có phiếu → vô hiệu hóa gửi, hiện “Chưa có phiếu để nghiên cứu.”
- **Test nghiệm thu:** dựng trang thật; tự động thành công/lỗi, thủ công lưu/đọc lại; thiếu phiếu; CSRF/XSS; đổi phiếu không lẫn kết quả; trang Research cũ vẫn hoạt động.
- **Phụ thuộc:** H7-04
### H7-06 · Menu, danh sách dự án và bảng duyệt
- **Agent:** B
- **Ngân sách:** ~330 dòng
- **Phạm vi:** `hub/Pages/Shared/_Layout.cshtml` · `hub/Pages/Projects.cshtml*` · `hub/Pages/Approvals.cshtml*` · `hub/wwwroot/css/hub.css`
- **Làm:**
  - Menu chỉ còn Văn phòng, Đơn duyệt, Dự án; Đánh giá gồm Hiệu suất và Quy trình & cải tiến; Hệ thống gồm Cài đặt, Tài khoản `/AI`, Map.
  - Xóa ô Antigravity và truy vấn chỉ phục vụ ô/menu cũ; địa chỉ chức năng cũ ngoài hai redirect vẫn mở.
  - `/Projects` liệt kê mọi dự án từ cấu hình, liên kết trang chi tiết; thêm dự án không cần sửa menu; rỗng → “Chưa có dự án.”
  - Đơn duyệt dùng bảng đủ sáu cột brief, nút Duyệt/Từ chối trong dòng chờ; giữ note, trạng thái và handler Decide hiện có.
- **Test nghiệm thu:** dựng trang thật với 0/1/nhiều dự án; menu chính xác và active; route cũ; đủ cột/trạng thái; quyết định lặp/sai; POST thiếu token; XSS; không tràn ở 1000px.
- **Phụ thuộc:** H7-03
### H7-07 · Giao diện Map
- **Agent:** B
- **Ngân sách:** ~170 dòng
- **Phạm vi:** `hub/Pages/Map.cshtml*` · `hub/wwwroot/css/hub.css`
- **Làm:**
  - `/Map` hiển thị đủ 11 nghiệp vụ đúng brief, mỗi dòng chọn một trong sáu tác nhân và ghi chú quyền hạn.
  - Nạp mặc định/dữ liệu đã lưu từ H7-01; lưu bằng POST antiforgery; thành công → “Đã lưu phân vai.”
  - Hiện lỗi dịch vụ, giữ nội dung nhập khi lưu thất bại; giải thích gán vai không tự cấp quyền thực thi/duyệt.
- **Test nghiệm thu:** GET Razor thật; đủ dòng/lựa chọn; mặc định và cấu hình cũ; lưu/tải lại; dữ liệu giả mạo/CSRF/XSS; lỗi ghi giữ form; ảnh 1280×800 và 1000px.
- **Phụ thuộc:** H7-01
## Câu hỏi cho quản lý
- Chốt plan/prep/QC khi chọn Claude, Minh, Lan, Cổng KT hoặc Chủ dự án: thực hiện thủ công hay phải bổ sung connector? Đề xuất lưu đủ sáu lựa chọn, chỉ tự chạy khi có hỗ trợ và không tự fallback.
- Chấp thuận chat tạo mới/bổ sung qua inbox, Claude tạo/cập nhật phiếu ở phiên kế tiếp; bổ sung dùng tiền tố `Bổ sung phiếu R-xxx:`? Hiện chưa có dịch vụ chat hai chiều ghi trực tiếp.

## Quyết định của quản lý (Claude, 25/10 12:30)
- ĐÃ DUYỆT 7 task. R-007: H6-02…06, H6-09 BỎ (thay bằng R-009).
- Map lưu đủ 6 lựa chọn. team.ps1 CHỈ tự chạy khi tác nhân được gán có hỗ trợ (plan / prep / QC: Codex; code: Minh / Lan qua agy); gán cho Claude / Chủ dự án / tác nhân chưa hỗ trợ ⇒ in "Bước <tên> gán cho <ai> — làm thủ công" rồi dừng bước đó, KHÔNG tự chuyển người khác.
- Chat Văn phòng: tạo mới / bổ sung ghi vào inbox.md (bổ sung dùng tiền tố "Bổ sung phiếu R-xxx: "); Claude xử lý ở phiên kế tiếp. Hiện danh sách phiếu + nhật ký phiếu (chỉ đọc) trong khung chat.
- Thứ tự: A: H7-01 + H7-03 song song → H7-02 (sau V8-01, cùng sửa team.ps1). B: H7-07 (sau H7-01), H7-04 + H7-06 (sau H7-03) → H7-05.
