# W1-02 · Helper dựng trang Hub

- **Agent:** B
- **Nhánh:** `agent-b/W1-02-helper-dung-trang-hub`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~190 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
- **Model:** `gemini-4-argon-high`

## Tóm tắt
- Không thêm chức năng mới cho người dùng; đây là bộ hỗ trợ kiểm thử Hub dùng lại.
- Gom cách dựng HTTP thật và render Razor để task sau không viết lại host.
- Giữ cookie, token chống giả mạo và HTML encode để test phát hiện lỗi bảo mật.
- Mỗi test dùng dữ liệu tạm riêng; tự dọn host cả khi có lỗi và chạy nhiều host đồng thời.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-011/brief.md` — mục tiêu 1, phần helper Hub; không làm phần trang Việc.
- `hub.tests/Support/HubTestHost.cs`
- `hub.tests/Support/HubPageRenderer.cs`
- `hub.tests/AcceptanceW102HubHelpersTests.cs`
- `hub.tests/TestEnvironment.cs`
- `hub.tests/AntiforgerySecurityTests.cs` — mẫu dựng HTTP/Razor/fake hiện có.
- `hub.tests/AcceptanceV505CommandTests.cs` — WithHub và ca POST thật.
- `hub.tests/AITeam.Hub.Tests.csproj`
- `hub/Pages/Office.cshtml.cs`
- `hub/Pages/Shared/_OfficeControl.cshtml`

## Bối cảnh
Theo kế hoạch W1-02 đã duyệt trong R-011: các test đang lặp WebApplication/Kestrel và mới kiểm một phần markup nguồn. Cần hai helper chung, không thay test cũ. Khung XML là hợp đồng bắt buộc; không cần dịch vụ sản phẩm hoặc đăng ký DI ở Program Hub.

## Mục tiêu
- Hoàn thiện `HubTestHost.StartAsync`, `WithAsync`, `DisposeAsync`, thuộc tính `Client`/`Services`.
- Hoàn thiện `HubPageRenderer` và `RenderAsync`; đăng ký renderer scoped trong DI host test.
- Giữ chữ ký public; cho phép thêm thành viên private trong hai file được sửa.

## Phạm vi (được sửa)
- `hub.tests/Support/HubTestHost.cs`
- `hub.tests/Support/HubPageRenderer.cs`
- `tasks/W1-02-helper-dung-trang-hub.md` — chỉ điền Báo cáo của agent.

## Không làm
- Không sửa test nghiệm thu mới/cũ, project, Program Hub, model/service sản phẩm hoặc TestEnvironment.
- Không làm trang Việc, đổi thiết kế giao diện, định dạng dữ liệu, team.ps1, skill hoặc công cụ.
- Không chạy agy/codex/tiến trình thật trong test; không đọc profile/repo thật, không chạy entry point sản phẩm.
- Không chụp ảnh, chạy app thủ công hoặc đo trình duyệt. Bước 7 do Claude nghiệm thu giao diện khi cần.

## Ràng buộc
- Dùng Razor đã biên dịch của assembly Hub, FakeServices.Register và environment.Paths; callback DI chạy sau fake, HubPaths khóa lại sau callback. Content root là thư mục tạm, tắt nguồn cấu hình máy thật và tránh lưu khóa DataProtection vào profile.
- Chỉ bind IPAddress.Loopback cổng 0, lấy địa chỉ sau khi server bind; không dùng cổng cố định, không tìm cổng rảnh rồi bind lại, không chia sẻ host/client tĩnh.
- HttpClient có cookie, không tự redirect, timeout 15 giây; giữ nguyên chống giả mạo. POST thiếu/sai token trả 400, có token và cookie đến handler (200).
- Renderer tạo scope/HttpContext riêng mỗi lần, dùng ICompositeViewEngine, giữ TempData, ModelState và các entry ViewData; sao chép trước khi thay Model. Không decode hoặc chỉnh HTML đầu ra.
- Chấp nhận tên partial và đường dẫn Razor tuyệt đối trong hệ thống view (không phải đọc file tùy ý). Lỗi tên/mất view đúng câu trong XML; lỗi từ DI/view/callback giữ nguyên.
- Dọn host/client/provider khi khởi động lỗi, callback lỗi hoặc kết thúc; dọn scope/writer khi render lỗi. Dispose host lặp an toàn, không xóa TestEnvironment của bên gọi.
- Mọi `.cs` lưu UTF-8 có BOM. Ngân sách ~190 dòng cho hai helper, không tính đặc tả và test nghiệm thu đã khóa.
- Không cần dữ liệu projects.json/phiếu/log: helper không phân tích các định dạng đó. Dữ liệu thật ở đây là Razor đã biên dịch của Hub, đã đối chiếu `_OfficeControl.cshtml` và mẫu WithHub; không tự chế trang/định dạng fixture thay thế.
- Hợp đồng ngoài: không CLI/tiến trình con; không thêm package. Các phụ thuộc đã hiện hữu, cả hai file khung nằm trong phạm vi. Không cần sửa DI sản phẩm; DI test đăng ký trong StartAsync.

## Test nghiệm thu
`hub.tests/AcceptanceW102HubHelpersTests.cs`

## Tiêu chí hoàn thành
- [ ] GET trang và partial thật, DI tùy biến, HubPaths đúng môi trường tạm và renderer có trong DI.
- [ ] POST thiếu/sai token bị chặn; token lấy từ GET cùng cookie được chấp nhận.
- [ ] Render partial bằng tên/đường dẫn, model và ViewData/ModelState giữ đủ, không sửa đầu vào; HTML chứa token và XSS vẫn encode.
- [ ] ViewData null/danh sách rỗng render được; tên null/rỗng/trắng và view thiếu báo đúng lỗi.
- [ ] Hai host sống đồng thời có cổng khác nhau, DI tách biệt; dọn listener/provider sau thành công, lỗi callback, lỗi startup; dọn scope khi Razor lỗi.
- [ ] Null argument báo ArgumentNullException; Dispose gọi nhiều lần an toàn, environment còn nguyên.
- [ ] `dotnet build AITeam.sln` thành công, 0 warning khi đã triển khai xong.
- [ ] `dotnet test AITeam.sln` pass toàn bộ; không sửa/Skip test nghiệm thu.

## Bàn giao khung (Codex)
- Hai helper chỉ có chữ ký/XML và `throw new NotImplementedException(); // TODO(agent)`; phần thân do Antigravity điền.
- Test mới dùng HTTP trong tiến trình, Razor thật và fake hiện có; không có harness tiến trình riêng.
- Mã model chọn theo yêu cầu task có host đồng thời: `gemini-4-argon-high`. Chưa xác minh được danh sách: `agy models` trả mã 1, hết thời gian chờ do lỗi kết nối proxy/đăng nhập. Quản lý cần kiểm lại trước khi giao chạy.
- Đã viết 15 ca (10 Fact và 2 Theory với tổng 5 dòng dữ liệu), chưa xác nhận discovery/chạy.
- Build lần 1: `dotnet build AITeam.sln` → exit 1, output `Build FAILED`, 0 warning, 0 error, 38,79 giây; không có chẩn đoán cụ thể.
- Build lần 2: `dotnet build AITeam.sln --no-restore -m:1 -nr:false -v:normal` → exit 1, 0 warning, 5 lỗi NETSDK1004 thiếu assets NuGet ở 5 project. Chưa đến bước biên dịch C#; đã đủ giới hạn 2 lần build.
- `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceW102HubHelpersTests --blame-hang-timeout 2m` → exit 0 nhưng không có output/tổng test, không tính là pass hay bằng chứng test đỏ.

| Điều kiện bàn giao | Trạng thái / bằng chứng |
|---|---|
| Đặc tả, ngân sách, phạm vi và khung TODO | Đã chuẩn bị; chỉ hai helper được triển khai |
| Test HTTP/Razor, token, encode, ViewData, đồng thời và dọn lỗi | Đã viết 15 ca trong file nghiệm thu; chưa chạy được |
| BOM UTF-8 | Kiểm byte EF-BB-BF trên cả 4 file bàn giao |
| Build 0 lỗi | Chưa đạt do thiếu assets NuGet; chưa xác minh biên dịch |
| Test đỏ vì TODO | Chưa xác minh; dự kiến NotImplementedException khi có môi trường build |
| Model có trong agy models | Chưa xác minh vì lỗi kết nối; giữ mã theo chỉ định kế hoạch |

Điều chưa chắc: cần môi trường restore NuGet hoạt động để build và chạy xác nhận test đỏ trước khi giao coder; không nhận bộ khung này là đã qua cổng build. Không chạy git theo yêu cầu phiên chuẩn bị.

## Báo cáo của agent
Agent điền kết quả triển khai, không nhận khung TODO là đã hoàn thành.

| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Từng tiêu chí phía trên | Chưa triển khai |

File đã đổi và lý do:

Quyết định tự đưa ra:

Điều chưa chắc / chưa xác minh:

## Review (Claude)
<!-- Claude điền sau khi review. -->

## Kết quả
<!-- Claude điền khi nghiệm thu đạt. -->