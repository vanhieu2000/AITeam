# W1-07 · Trang Việc và thanh bước

- **Agent:** B
- **Nhánh:** `agent-b/W1-07-trang-viec-va-thanh-buoc`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~330 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->


## Tóm tắt
- Có chức năng mới: xem yêu cầu và công việc của dự án trong tab “Việc”.
- Chọn yêu cầu rồi chọn task để biết đang ở bước nào trong chín bước.
- Hiện giờ, người phụ trách và lỗi gần nhất theo dữ liệu có sẵn.
- Dùng menu dự án chung, giữ liên kết Đơn duyệt; trang này chỉ xem.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-011/brief.md`
- `docs/design/R-011-mockup.html`
- `hub/Pages/Project.cshtml.cs`
- `hub/Pages/Project.cshtml`
- `hub/Pages/Shared/_Layout.cshtml`
- `hub/wwwroot/css/hub.css`
- `hub/Services/WorkProgressService.cs`
- `hub/Services/WorkProgressParser.cs`
- `hub/Models/HubModels.cs`
- `hub/Services/Contracts.cs`
- `hub.tests/AcceptanceW107WorkPageTests.cs`
- `hub.tests/Support/HubTestHost.cs`
- `tests/fixtures/W1-07/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md`

## Bối cảnh
Phụ thuộc W1-02 (HubTestHost) và W1-06 (IWorkProgressService) đã có trong worktree.
Tận dụng route `/p/{name}` và menu dự án hiện có, thay nội dung bảng việc bằng tab Việc;
không thêm trang danh sách dự án hay bảng Kanban thứ hai. Mockup chỉ là bố cục:
không sao chép dữ liệu, JavaScript innerHTML, bộ lọc hoặc đồng hồ giả trong mockup.

## Mục tiêu
Hoàn thiện OnGet trong khung ProjectModel, giữ các chữ ký/property đã chuẩn bị.
Inject IWorkProgressService qua constructor hiện có; dịch vụ thuộc W1-06, không tạo dịch vụ mới.
Giữ chức năng tab Kế hoạch và Lịch sử chạy cùng các property cũ để tương thích.

## Phạm vi (được sửa)
- `hub/Pages/Project.cshtml.cs`
- `hub/Pages/Project.cshtml`
- `hub/Pages/Shared/_Layout.cshtml`
- `hub/wwwroot/css/hub.css`
- `tasks/W1-07-trang-viec-va-thanh-buoc.md` (chỉ báo cáo và tiêu chí hoàn thành)

## Không làm
- Không sửa test nghiệm thu `hub.tests/AcceptanceW107WorkPageTests.cs` và fixture `tests/fixtures/W1-07/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md`.
- Không sửa hợp đồng, parser, dịch vụ W1-06, DI toàn ứng dụng hoặc handler duyệt.
- Không đọc file/parse nhật ký trong PageModel hay Razor, không ghi dữ liệu/chạy tiến trình.
- Không thêm POST, nút chạy lệnh, tạo task, bộ lọc trạng thái, danh sách dự án trùng R-009.
- Không đổi team.ps1, tools, cấu hình, định dạng file nguồn.
- Không chạy ứng dụng, chụp ảnh hoặc đo trình duyệt trong bước lập trình này.

## Ràng buộc
- OnGet: Name rỗng chuyển /Index; dự án không cấu hình trả 404.
- Lấy snapshot mới bằng GetProject(Name cấu hình) đúng một lần. Không GetRequest rồi đọc lại nguồn.
- RequestId và TaskId chỉ là khóa tra snapshot, so OrdinalIgnoreCase; không biến thành đường dẫn/URL.
- RequestId rỗng chọn yêu cầu đầu theo thứ tự snapshot. TaskId rỗng xem tiến độ yêu cầu.
  TaskId có giá trị phải thuộc yêu cầu đã chọn; mã không tồn tại/sai quan hệ/sai định dạng trả 404,
  không chuyển hướng ra ngoài. Task không có phiếu có thể hiển thị ở nhóm riêng từ UnlinkedTasks,
  nhưng không bịa quan hệ hoặc cho TaskId của nhóm này qua một RequestId khác.
- SelectedProgress là đúng Progress của yêu cầu/task đã chọn; không tính lại bước từ thứ tự hoặc board.
- Dự án rỗng: HTTP 200, “Chưa có việc.”, không tạo thanh bước giả.
- Thiếu thời gian/người/nhật ký dùng “Chưa có dữ liệu”; không dùng DateTime.Now hoặc Agent B cố định.
- IOException: HTTP 200 với WorkError đúng “Không đọc được tiến độ công việc.”; không hiện exception,
  đường dẫn/nội dung riêng tư/InnerException. InvalidDataException trả 404 (bắt trước IOException).
- Tab thiếu, board hoặc work là Việc; tab plan/runs giữ chức năng hiện có; giá trị tab lạ về Việc.
- Razor encode mọi tiêu đề/người/lỗi; không Html.Raw dữ liệu vào script, không nối HTML/URL từ dữ liệu.
  Link chọn việc dùng TagHelper hoặc query được encode. URL tùy ý trong log chỉ hiện văn bản.
- Giữ link `/Approvals` và toàn bộ handler duyệt; menu “Việc” dẫn dự án đang chọn hoặc dự án đầu
  cấu hình; nếu chưa có dự án thì dẫn menu dự án chung hiện có, không bịa tên AITeam.
- Không thêm service nên không có đăng ký DI mới. Constructor có tham số tùy chọn cuối để giữ tương thích
  các test dựng PageModel cũ; HTTP thật phải resolve dịch vụ W1-06, không dùng dữ liệu giả fallback.
- File .cs/.cshtml lưu UTF-8 BOM. Không đổi file ngoài phạm vi; không sửa test để qua.
- Không có hợp đồng CLI/tiến trình ngoài hay ghi file mới trong tính năng này.

## Dữ liệu mẫu thật
- Fixture chép nguyên byte từ `requests/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md`:
  giữ tên, BOM và xuống dòng; có nhật ký bảng cũ xen dòng workflow, nhiều lượt Verify/Qc/Assign/Merge.
- Đã mở projects.json thật, xác nhận gốc là mảng; trang không tự đọc/ghi file này.
- Test dùng snapshot hợp đồng W1-06; Progress dựng bằng parser hiện có trên bản sao phiếu thật.
  Không dùng đường dẫn D: trong cấu hình thật để đọc repo hay workdir.
- Dữ liệu máy thật R-009/V8-02 và log WorkDir chưa xác minh trong bước chuẩn bị, thuộc nghiệm thu bước 7.

## Giao diện
Làm theo `docs/design/R-011-mockup.html`: danh sách yêu cầu trái 38%, chi tiết phải 62%;
tiêu đề dài phải xuống dòng; chín bước, trạng thái hiện tại đậm, lỗi gần nhất, giờ/người.
Chỉ lấy dữ liệu có thật từ Progress/Steps và log snapshot, không sao chép số liệu mockup.
Trạng thái hiện tại dùng `aria-current="step"`; các bước đã merge vẫn hiển thị đủ.
Link Đơn duyệt luôn tới route có sẵn, không chuyển handler sang trang Việc.

## Bố cục giao diện
- Gốc `data-work-page`, danh sách `data-work-request="<Id>"`.
- Khối chi tiết `data-selected-request="<Id>"`; chỉ khi chọn task có `data-selected-task="<Id>"`.
- Chín mục li trong ol `class="work-steps"`, mỗi mục `data-work-step="0"` tới `"8"`,
  theo thứ tự: Tiếp nhận, Kế hoạch, Duyệt kế hoạch, Chuẩn bị, Code, Cổng KT, QC, Nghiệm thu, Merge.
- Grid dùng minmax(0,...) và min-width:0; chuỗi 140 ký tự kể cả không có khoảng trắng dùng
  overflow-wrap:anywhere. Không khóa height/overflow:hidden làm mất nội dung như Kanban cũ.
- Thanh bước tự xuống hàng trong media query tối đa 1000px (hoặc ngưỡng 1100px theo mockup),
  tối đa 5 cột; bước hiện tại font-weight ít nhất 600. CSS phải khớp thẻ/class thực tế.
- Tránh form POST/hx-post và các nút hành động cũ “+ Task”, “Mở thư mục” trên trang Việc.
  Menu dự án chung dùng lại nguyên nguồn ConfigService, không tạo select/danh sách dự án mới trong thân trang.
- Claude kiểm giao diện thật ở bước 7; lập trình viên chỉ kiểm bằng test HTTP/Razor và markup.

## Test nghiệm thu
`hub.tests/AcceptanceW107WorkPageTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới.
- [ ] `dotnet test AITeam.sln`: mọi test pass, test nghiệm thu giữ nguyên.
- [x] Chọn đúng dự án/yêu cầu/task, không trộn dữ liệu, mã giả trả 404.
- [x] Trang mặc định là Việc; đầy đủ chín bước, hiện trạng thái/giờ/người/lỗi đúng snapshot.
- [x] Rỗng, thiếu task/giờ/nhật ký và lỗi nguồn có thông báo quy định; không lộ dữ liệu riêng tư.
- [x] Razor encode XSS, trang chỉ GET, liên kết duyệt giữ route cũ.
- [x] Markup/CSS xuống dòng cho chuỗi dài và thanh bước ở 1000px theo đặc tả.
- [x] Giữ kế hoạch/lịch sử chạy, không thêm danh sách dự án/bảng việc trùng.

## Báo cáo của agent
| Tiêu chí | Bằng chứng lượt sửa 26/10 |
|---|---|
| Build | `dotnet build AITeam.sln --no-restore -m:1 --disable-build-servers`: exit 0, 0 lỗi, 0 warning. |
| Test toàn solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 660 pass/9 fail/0 skip; Core ít nhất 6 fail, chưa tổng kết; dừng trước 3 phút, exit 1. Chưa đạt. |
| Chọn đúng dự án/yêu cầu/task, 404 | [A] `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceW105WorkProgressTests\|FullyQualifiedName~AcceptanceW107WorkPageTests" --blame-hang-timeout 2m`: 79 pass/0 fail/0 skip, gồm 16 ca W1-07. |
| Mặc định Việc/chín bước/giờ/người/lỗi | [A]: Default/Selection/Latest_failure pass; giữ nguyên snapshot. |
| Rỗng/thiếu dữ liệu/lỗi nguồn | [A]: Empty/Request_without_tasks/Source_error pass, không lộ lỗi riêng tư. |
| XSS/chỉ GET/duyệt/CSS/giữ tab cũ | [A]: Long_untrusted_title/Work_css/Default pass; giữ nguyên PageModel/Razor/layout/CSS trong lượt này; không chạy app/chụp ảnh. |
| Hồi quy W1-05 | [A]: Results_use_explicit_prefix_not_substring pass, KHÔNG ĐẠT=Unknown; hoàn tác nhận KHÔNG ĐẠT/XONG trong parser. |
| Bộ đếm V8-02 | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceV802PipelineTests.Bo_dem_rieng_khong_reset_khong_fallback" --blame-hang-timeout 2m`: 9 pass/0 fail/0 skip, exit 0, 2 phút 25 giây. |
| File đổi/phạm vi cần Claude quyết | Xóa file thêm WorkProgressOutcomeRegressionTests.cs; sửa WorkProgressParser.cs để hoàn tác hai tiền tố trên; giữ xử lý giờ legacy/chặn lỗi bước cũ từ lượt trước vì nghiệm thu W1-07 cần hiện 17:03/CẦN SỬA; xin Claude quyết phần parser ngoài phạm vi. Task: cập nhật báo cáo. |
| BOM/test nghiệm thu | BOM parser, Project.cshtml.cs, Project.cshtml, _Layout.cshtml đều True; không sửa test nghiệm thu/fixture. |
| Chưa xác minh/bàn giao | Toàn solution còn lỗi File.Replace Access denied (Hub), Git CreateFileMapping Win32 error 5 và test mutex (Core); không sửa ngoài phạm vi để né sandbox. Không gọi git; hệ thống tự commit/push, chưa có commit được kiểm. |
## Ghi nhận chuẩn bị (Codex)
- Khung OnGet dùng NotImplementedException; chưa triển khai thân hàm hoặc markup/CSS.
- Test nghiệm thu gọi HTTP/Razor thật qua HubTestHost; nguồn và cấu hình giả không ghi dữ liệu.
- Đã chạy đúng hai lần build, không chạy git:
  1. dotnet build AITeam.sln: exit 1, in Build FAILED nhưng 0 warning/0 error, không có chẩn đoán.
  2. dotnet build AITeam.sln -m:1 --disable-build-servers -v:normal:
     Build succeeded, 0 Error(s), 1 Warning(s), 38,36 giây.
     Warning xUnit2013 tại assertion đếm aria-current đã sửa thành Assert.Single sau build;
     chưa build lại để giữ giới hạn hai lần.
- dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter
  FullyQualifiedName~AcceptanceW107WorkPageTests --blame-hang-timeout 2m:
  14 fail, 0 pass, 0 skip, 3 giây; HTTP 500 khi OnGet còn throw NotImplementedException.
- Nguồn hiện có 16 ca (10 Fact + 2 dòng Theory chọn task + 6 dòng Theory mã giả:
  thực tế 8 Fact + 2 + 6 = 16). Hai Fact bổ sung về lỗi Qc cũ và CSS cùng bản sửa warning
  chưa được biên dịch/chạy lại; không coi là đã kiểm xanh. Chưa chạy toàn bộ solution test.
- BOM: Project.cshtml.cs=True, AcceptanceW107WorkPageTests.cs=True.
  Fixture so Base64 toàn bộ byte với phiếu nguồn: True, 6938 byte; giữ BOM/xuống dòng/tên file.
- File thay đổi: Project.cshtml.cs (khung DI/property/OnGet); file task (đặc tả);
  AcceptanceW107WorkPageTests.cs (test bất biến); fixture R-010 (bản sao byte thật).
  Không sửa markup/CSS sản phẩm trong bước chuẩn bị.
- Điều chưa chắc: helper/parser thuộc các task phụ thuộc; chưa kiểm ảnh hoặc dữ liệu WorkDir thật.
  Không sửa phạm vi các phụ thuộc để che lỗi.

## Review (Claude)
<!-- Claude điền kết quả review. -->

## Kết quả
<!-- Claude điền khi nghiệm thu đạt. -->

## Đánh giá quy trình
<!-- Claude điền sau khi hoàn tất. -->