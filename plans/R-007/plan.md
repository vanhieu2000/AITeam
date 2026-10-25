# Kế hoạch R-007
## Hiểu yêu cầu
Gom Hub theo quy trình v4 để HieuDV theo dõi yêu cầu, người phụ trách và bước đang chờ.
Giữ dịch vụ, dữ liệu, thao tác hiện có; đổi menu, trang tổng hợp và bổ sung dây chuyền dùng chung.
Địa chỉ cũ chuyển 302, giữ query; POST và handler HTMX tiếp tục hoạt động.
Chỉ nghiên cứu đọc; chưa sửa file, chạy build/test hay kiểm chứng giao diện.
## Research
- `projects.json:24`: A phụ trách Services, Program, hub.tests; B phụ trách Pages, wwwroot, DevData.
- `AGENTS.md:50`: xác nhận HTMX đặt trên form; `:53`: tránh XSS; `:54`: kiểm ảnh 1280×800; `:74`: test dùng HubPaths tạm.
- `docs/design/v6-hub-chuc-nang-mockup.html:164`: bảng ánh xạ; `:170`: duyệt quá vòng; `:172`: sơ đồ v4; `:177`: giữ URL dự án/task.
- `hub/Pages/Office.cshtml:1`: `/` là Văn phòng; `hub/Pages/Index.cshtml:1`: Tổng quan ở `/Overview`.
- `hub/Pages/Requests.cshtml.cs:68`: đã có gửi yêu cầu, giới hạn 4000 ký tự, kiểm dự án; `:50`: chọn phiếu bằng `id`.
- `hub/Pages/Research.cshtml.cs:42`: Research chọn phiếu bằng `r`; `:68`, `:91`, `:112`: hỏi, lưu thủ công, polling.
- `hub/Services/RequestService.cs:116`: phiếu lấy từ requests; plans chưa được dịch vụ này đọc; cần hợp nhất danh sách để hiện brief chưa có phiếu.
- `hub/Services/InsightParsers.cs:44`: đã có ParseRequest; `:94`: ParseReviewRounds chỉ trả số vòng và đạt vòng đầu, chưa đủ xác định vòng cuối.
- `hub/Services/OfficeBuilder.cs:25`: đầu vào hàm thuần; `:38`: thứ tự bàn cũ; `hub/Services/OfficeService.cs:71`: logs nằm dưới WorkDir dự án.
- `team.ps1:602`, `:789`, `:849`: tên log review/plan/prep; `core/AITeam.Core/Providers/DevRunner.cs:103`, `core/AITeam.Core/Review/ReviewRunner.cs:171`: cấu trúc JSON.
- `hub/Pages/Providers.cshtml.cs:59`: đã nạp phân vai/lịch sử; `hub/Pages/Accounts.cshtml.cs:45`: đã có kiểm đăng nhập và đổi tài khoản.
- `hub.tests/AntiforgerySecurityTests.cs:84`: test dựng trang thật; `hub/Program.cs:39`: đăng ký dịch vụ chung.
- CHƯA XÁC MINH: mẫu log plan/prep/review thực tế trong fixtures; ý nghĩa quyết định rỗng/từ chối và nghiệm thu phiếu nhiều task cần chốt trước viết test.
## Rủi ro
- Plan/AI/query gây XSS → Razor encode, plan trong `<pre>`, tab theo danh sách cho phép; không đưa dữ liệu vào script bằng Html.Raw.
- Redirect làm mất handler hoặc POST → chỉ redirect GET trang đầy đủ; giữ endpoint handler và antiforgery, không redirect POST bằng 302.
- Query trùng tab hoặc `r`/`id` → giữ tham số gốc, quy định ưu tiên rõ ràng; URL đích luôn nội bộ, không nhận returnUrl tùy ý.
- Dữ liệu cũ thiếu log bị suy thành hoàn tất → hiển thị “Chưa có dữ liệu”; không tự tạo/ghi bù file trạng thái.
- Task cùng mã khác dự án, đường dẫn giả → ghép khóa dự án/task, kiểm mã và giới hạn đường dẫn trong root cấu hình.
- Log lỗi/cũ bị dùng làm bằng chứng đạt → kiểm cấu trúc, exitCode/verdict; vòng mới nhất ưu tiên, lỗi đọc hiển thị “Không đọc được dữ liệu dây chuyền.”.
- Chia task gây xung đột hoặc vượt ngân sách → Codex chốt khung/API và test trước; tuần tự khi cùng file; ngân sách tính cả mã di chuyển và test bổ sung.
## Task
Mỗi task tối đa 400 dòng, khoảng 15 phút agent; Codex viết khung và test nghiệm thu ở bước sau, agent không sửa test nghiệm thu.
Mọi task phải build 0 warning, test pass; UI kiểm ảnh 1000px và 1280×800. Giữ BOM, không sửa HubModels.cs/Contracts.cs, team.ps1 hay dữ liệu trạng thái.
### H6-01 · Hàm thuần dây chuyền
- **Agent:** A
- **Ngân sách:** ~300 dòng
- **Phạm vi:** `hub/Services/WorkflowBuilder.cs` · `hub.tests/AcceptanceH601Tests.cs`
- **Làm:**
  - Nhận snapshot brief/plan/quyết định/task/log → đúng bảy bước, trạng thái, người phụ trách và bằng chứng; không đọc đĩa.
  - Tách trạng thái từng task và tổng phiếu; phiếu chưa có task không tự hoàn tất Chuẩn bị/Code/QC.
  - NeedsFix quay về Code; review chọn vòng số lớn nhất; không để PASS cũ che vòng mới lỗi; nghiệm thu theo quyết định quản lý đã chốt.
- **Test nghiệm thu:** bảy mốc; thiếu/rỗng; quyết định mẫu; nhiều task khác tiến độ; vòng 2/10; đạt rồi cần sửa; task cũ đã merge thiếu log; tính xác định.
- **Phụ thuộc:** —
### H6-02 · Đọc bằng chứng và bàn theo vai trò
- **Agent:** A
- **Ngân sách:** ~370 dòng
- **Phạm vi:** `hub/Services/WorkflowService.cs` · `hub/Services/OfficeBuilder.cs` · `hub/Services/OfficeService.cs` · `hub/Services/HubV3Registration.cs`
- **Làm:**
  - Hợp nhất mã từ requests và plans; đọc brief/plan, liên kết task qua ParseRequest, trạng thái bảng việc và log đúng WorkDir.
  - Chuyển dữ liệu sang H6-01; nguồn thiếu giữ trạng thái thiếu, nguồn hỏng báo “Không đọc được dữ liệu dây chuyền.”; tuyệt đối không ghi.
  - Bàn Claude quản lý/nghiệm thu; Codex kế hoạch/chuẩn bị/QC, việc mới nhất theo thời gian log; Minh/Lan theo staff; Cổng KT; bỏ bàn Research và tránh trùng Codex.
- **Test nghiệm thu:** root/profile tạm; chỉ có brief; nhiều dự án; trùng mã task; traversal; JSON hỏng; prep thất bại; log mới nhất; không thay byte dữ liệu; không đọc hồ sơ thật.
- **Phụ thuộc:** H6-01
### H6-03 · Tương thích địa chỉ cũ
- **Agent:** A
- **Ngân sách:** ~220 dòng
- **Phạm vi:** `hub/Program.cs` · `hub/Services/LegacyHubRedirects.cs` · `hub.tests/AcceptanceH603Tests.cs`
- **Làm:**
  - GET `/Overview` → `/Performance`; `/Team` → `/Performance?tab=staff`; `/Agents` → `/`; `/Inbox` → `/Requests?tab=new`.
  - GET `/Research` → `/Requests?tab=research`; `/Providers` → `/AI?tab=roles`; `/AiAccounts` → `/AI?tab=status`; `/Accounts` → `/AI?tab=antigravity`.
  - Giữ query gốc; tab hợp lệ do người dùng chọn ưu tiên tab mặc định; đích Requests hiểu `r` khi không có `id`; miễn redirect request có handler và mọi POST.
- **Test nghiệm thu:** từng ánh xạ trả 302/Location đúng; query lặp, Unicode, mã hóa; không vòng lặp/open redirect; handler polling và POST cũ còn chạy; URL dự án/task giữ nguyên.
- **Phụ thuộc:** — 
### H6-04 · Yêu cầu và các tab chính
- **Agent:** B
- **Ngân sách:** ~340 dòng
- **Phạm vi:** `hub/Pages/Requests.cshtml` · `hub/Pages/Requests.cshtml.cs` · `hub/Pages/Shared/_Workflow.cshtml`
- **Làm:**
  - Đổi tên Yêu cầu, hiện cả brief chưa có phiếu; thanh bảy bước dùng partial chung; chọn `id`, tương thích `r`, tab lạ về Brief & kế hoạch.
  - Tạo năm tab Brief & kế hoạch, Task, Research, Nhật ký, Chat; brief/plan hiển thị chữ được encode, thiếu plan báo “Chưa có kế hoạch.”.
  - Giữ form “Gửi yêu cầu mới”, Chat và dữ liệu nhật ký hiện có; không thêm AI tức thì; giữ lỗi “Nội dung yêu cầu không được để trống.” và giới hạn 4000 ký tự.
- **Test nghiệm thu:** mọi tab dựng thật; chỉ có brief; không phiếu; task liên kết đúng dự án; XSS plan/query; gửi rỗng/4000/4001 ký tự/dự án lạ; POST thiếu token bị chặn.
- **Phụ thuộc:** H6-02
### H6-05 · Research trong phiếu
- **Agent:** B
- **Ngân sách:** ~280 dòng
- **Phạm vi:** `hub/Pages/Requests.Research.cs` · `hub/Pages/Requests.cshtml.cs` · `hub/Pages/Shared/_RequestResearch.cshtml` · `hub/Pages/Shared/_ResearchResults.cshtml`
- **Làm:**
  - Nối tab Research H6-04 với IResearchService; tái dùng form/kết quả hiện có, ràng buộc phiếu đang chọn.
  - Giữ hỏi tự động, copy/dán thủ công, lưu nguồn và polling; endpoint cũ tiếp tục trả partial đúng kiểu model.
  - Giữ lỗi “Phiếu không hợp lệ.”, “Cần nội dung câu hỏi.”, “Chọn ít nhất 1 AI.”; không gọi connector khi chỉ dựng trang.
- **Test nghiệm thu:** hai phiếu không lẫn dữ liệu; hỏi/lưu/polling bằng fake; phiếu giả/traversal; AI hoặc nguồn độc hại; antiforgery; chế độ thủ công và không có tài khoản.
- **Phụ thuộc:** H6-04
### H6-06 · Văn phòng, Task và menu v6
- **Agent:** B
- **Ngân sách:** ~320 dòng
- **Phạm vi:** `hub/Pages/Office.cshtml*` · `hub/Pages/Shared/_OfficeDesks.cshtml` · `hub/Pages/Shared/_Layout.cshtml` · `hub/Pages/Task.cshtml*` · `hub/Pages/Process.cshtml` · `hub/wwwroot/css/hub.css`
- **Làm:**
  - Hiện bàn H6-02 và “Dây chuyền đang chạy”; Office/Task dùng `_Workflow`; task không gắn phiếu báo “Task chưa gắn yêu cầu.”.
  - Menu đúng chín mục/nhóm theo brief, giữ dự án động; đổi liên kết nội bộ sang URL mới và trạng thái menu đang chọn; Process thêm sơ đồ v4.
  - Giữ điều khiển/polling Văn phòng, hx-confirm trên form; dùng CSS Hub, cho thanh bước/tab xuống dòng ở 1000px.
- **Test nghiệm thu:** Office/Task/Process dựng thật; tên staff thay đổi; không bàn Research/trùng Codex; đếm menu; đủ bảy bước/người làm; thao tác Dừng hỏi xác nhận; ảnh 1000px và 1280×800 không tràn ngang.
- **Phụ thuộc:** H6-04
### H6-07 · Hiệu suất và Nhân viên
- **Agent:** B
- **Ngân sách:** ~250 dòng
- **Phạm vi:** `hub/Pages/Index.cshtml` · `hub/Pages/Index.cshtml.cs` · `hub/Pages/Shared/_PerformanceStaff.cshtml`
- **Làm:**
  - Đưa trang Tổng quan sang `/Performance`, đổi nhãn Hiệu suất; giữ số đo, biểu đồ, bộ lọc và handler hiện có.
  - Thêm tab Nhân viên bằng phần hiển thị Team hiện có và ITeamInsightsService; không viết lại phép tính hay xóa trang/dịch vụ cũ.
  - Tab lạ về tổng quan; giữ lỗi “Không đọc được dữ liệu đánh giá: …”, dữ liệu rỗng hiển thị rõ.
- **Test nghiệm thu:** số liệu trước/sau bằng nhau; bộ lọc/query; tab nhân viên đủ dữ liệu; rỗng/lỗi dịch vụ; dựng thật; biểu đồ/bảng không làm tràn trang 1000px.
- **Phụ thuộc:** —
### H6-08 · AI & tài khoản: khung và Trạng thái
- **Agent:** B
- **Ngân sách:** ~230 dòng
- **Phạm vi:** `hub/Pages/AI.cshtml` · `hub/Pages/AI.cshtml.cs` · `hub/Pages/Shared/_AiStatus.cshtml`
- **Làm:**
  - Tạo `/AI` với bốn tab theo mockup; tab mặc định Trạng thái, tab lạ quay về mặc định.
  - Tái dùng IAiAccountsService và `_AiAccountRow`; giữ kiểm tra, alias, mode, ngắt kết nối cùng các handler cũ.
  - Form chỉ tới endpoint hiện có, sau thao tác quay về tab tương ứng qua redirect trang cũ; không đưa bí mật vào HTML.
- **Test nghiệm thu:** dựng thật mọi tab; trạng thái chưa kết nối/thủ công/tắt; kiểm tra bằng fake; lỗi lưu; antiforgery; alias chứa HTML; URL cũ tương thích.
- **Phụ thuộc:** —
### H6-09 · AI: Phân vai, Antigravity và Lịch sử nghỉ
- **Agent:** B
- **Ngân sách:** ~370 dòng
- **Phạm vi:** `hub/Pages/AI.cshtml*` · `hub/Pages/Shared/_AiRoles.cshtml` · `hub/Pages/Shared/_AiAntigravity.cshtml` · `hub/Pages/Shared/_AiRestHistory.cshtml`
- **Làm:**
  - Hoàn thiện ba tab bằng phần hiển thị Providers/Accounts và logic sẵn có; giữ routing, lịch sử nghỉ, kiểm đăng nhập/đổi tài khoản và thông tin tài khoản cũ.
  - Dùng handler cũ, không đổi ghi cấu hình; lỗi giữ “Provider không hợp lệ.” và “Không đọc được cấu hình provider: …”.
  - Bổ sung URL/tab mới vào test dựng trang do Codex chuẩn bị; kiểm lại toàn bộ luồng redirect và các thao tác từ trang tổng hợp.
- **Test nghiệm thu:** routing/lịch sử đúng dữ liệu; nghỉ/hết nghỉ; cấu hình hỏng; POST thiếu token bị chặn; đổi tài khoản bằng fake, không tác động máy thật; ảnh 1000px/1280×800; toàn bộ test cũ còn nguyên.
- **Phụ thuộc:** H6-08
## Quyết định của quản lý (Claude, 25/10 11:05)
- ĐÃ DUYỆT 9 task. Bảng suy trạng thái (chốt cho H6-01):
  - Bước 3 Duyệt = đã xong khi plan.md có mục "## Quyết định của quản lý" chứa chữ "ĐÃ DUYỆT"; có mục nhưng không có chữ đó (hoặc có "TỪ CHỐI") ⇒ đang chờ.
  - Bước 6 QC = xong khi review json mới nhất của task có verdict PASS; CHANGES_REQUESTED ⇒ quay về bước 5 Code.
  - Bước 7 Nghiệm thu = "### Vòng N — ĐẠT" trong "## Review (Claude)" (Claude) hoặc task ✅ đã merge.
  - Phiếu xong khi MỌI task của phiếu ✅ đã merge; task cũ đã merge thiếu log ⇒ coi các bước 4–7 là xong (dữ liệu cũ, quy trình v3).
- Thứ tự giao (Codex hạn mức nhỏ ⇒ chuẩn bị từng task, không dồn): A: H6-01 → H6-02 → H6-03; B: H6-07, H6-08 → H6-09, rồi H6-04 → H6-05 → H6-06 (sau H6-02).

## Câu hỏi cho quản lý
- Chốt bảng suy trạng thái trước H6-01: mục “Quyết định của quản lý” rỗng/từ chối có tính đã duyệt không; “Vòng N — ĐẠT” hoàn tất QC hay Nghiệm thu; phiếu nhiều task có cần tất cả đạt/merge mới hoàn tất?
