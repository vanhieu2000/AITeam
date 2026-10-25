# H7-07 · Giao diện Map

- **Agent:** B
- **Nhánh:** `agent-b/H7-07-giao-dien-map`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~170 dòng
- **Yêu cầu:** R-009
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có chức năng mới: trang Map cho người dùng gán tác nhân cho đủ 11 nghiệp vụ của quy trình.
- Mỗi nghiệp vụ có lựa chọn một trong sáu tác nhân và ghi chú quyền hạn riêng.
- Nạp phân vai mặc định v4 hoặc dữ liệu đã lưu; lưu an toàn và báo “Đã lưu phân vai.”.
- Lỗi lưu giữ nội dung vừa nhập; giải thích gán vai không tự cấp quyền thực thi hoặc duyệt.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-009/brief.md` mục 6 và Ràng buộc (mô tả giao diện đã duyệt, không có mockup riêng).
- `hub/Pages/Map.cshtml`
- `hub/Pages/Map.cshtml.cs`
- `hub/wwwroot/css/hub.css`
- `hub/Services/WorkflowRolesService.cs` (hợp đồng H7-01, chỉ đọc).
- `hub.tests/AcceptanceH707MapTests.cs` (chỉ đọc).
- `hub.tests/TestEnvironment.cs` và `hub.tests/AcceptanceH608AiTests.cs` (mẫu dựng Razor thật, chỉ đọc).

## Bối cảnh
Làm theo brief R-009 mục 6. H7-01 đã có `IWorkflowRolesService`, `WorkflowRole`, `WorkflowRolesSnapshot` và dịch vụ lưu nguyên tử. Trang Razor mới được tự phát hiện bằng `@page`, PageModel nhận dịch vụ H7-01 qua DI hiện có; không tạo dịch vụ mới. Không thay đổi hợp đồng chung hay đọc/ghi team.json từ PageModel.

## Mục tiêu
Hoàn thiện khung `MapModel.OnGet()` và `OnPost()`, dựng Razor và CSS cục bộ để xem, sửa và lưu phân vai. Khung cố ý ném NotImplementedException; thay TODO bằng logic đúng mô tả XML và test.

## Phạm vi (được sửa)
- `hub/Pages/Map.cshtml`
- `hub/Pages/Map.cshtml.cs`
- `hub/wwwroot/css/hub.css`
- `tasks/H7-07-giao-dien-map.md` (chỉ Báo cáo của agent).

## Không làm
- Không sửa `hub.tests/AcceptanceH707MapTests.cs`; đây là test nghiệm thu được viết trước.
- Không sửa dịch vụ H7-01, DI nền, `Contracts.cs`, `HubModels.cs`, menu, team.ps1 hay định dạng cấu hình.
- Không chạy agy/codex, không tự thực thi nghiệp vụ hay tự cấp quyền; không thêm cơ chế cấp quyền.
- Không triển khai lại lưu file, khóa, xử lý symlink hoặc nguyên tử ở tầng trang.
- Không chạy app hoặc chụp ảnh; Claude kiểm giao diện thật ở bước 7.

## Ràng buộc
- UTF-8 có BOM cho .cs/.cshtml. Mọi thông báo, nhãn, comment tiếng Việt có dấu.
- Hợp đồng nhập: `Roles[i].Code`, `Roles[i].Actor`, `Roles[i].Note`; hidden `Revision`; POST `/Map` mặc định. Giữ public API của khung.
- 11 mã đúng thứ tự H7-01: receive, idea, research, plan, approve_plan, prep, code, gate, qc, accept, publish. Nhãn đúng brief; không dịch Research, Code, Test / QC, Publish git (merge + push).
- Sáu giá trị actor: claude/Claude, codex/Codex, minh/Minh, lan/Lan, gate/Cổng KT, owner/Chủ dự án; mỗi dòng luôn có đủ sáu lựa chọn.
- GET gọi GetRoles một lần; lấy mặc định từ dịch vụ, không ghi cứng mặc định tại trang. Thiếu file/workflow/roles là dữ liệu cũ hợp lệ; null/rỗng/hỏng là lỗi, không ghi đè bằng mặc định.
- POST phải có antiforgery thật. Kiểm tra ModelState và toàn bộ danh sách trước SaveRoles: đúng 11 mã duy nhất, không mã/tác nhân lạ, phân biệt hoa thường; ghi chú tối đa 500 đơn vị UTF-16; revision không rỗng. Ghi chú rỗng (binder có thể trả null) chuẩn hóa thành chuỗi rỗng, giữ nguyên khoảng trắng khác.
- Sai dữ liệu báo “Phân vai không hợp lệ.”, không gọi SaveRoles. Không bind thuộc tính khác thành quyền hay đường dẫn. Không lấy revision mới bằng GET để bỏ qua xung đột.
- Thành công trả Page với “Đã lưu phân vai.” và snapshot/revision mới; lỗi giữ dữ liệu nhập, lựa chọn và revision cũ, không báo thành công.
- GET IOException: “Không đọc được cấu hình phân vai.”; GET InvalidDataException: “Phân vai không hợp lệ.”. Khi GET lỗi không dựng form có thể lưu, không có hidden Revision.
- POST IOException: “Không lưu được cấu hình phân vai.”; InvalidDataException: “Phân vai không hợp lệ.”; InvalidOperationException: “Cấu hình đã thay đổi. Vui lòng tải lại.”. Không hiện exception.Message hay thông tin cấu hình gốc. Lưu ý InvalidDataException phải được phân loại đúng.
- Mọi ghi chú/giá trị từ form/file/query đều không tin cậy: dùng mã hóa Razor mặc định; không Html.Raw, không chèn vào script; hiển thị ghi chú XSS như văn bản ở cả GET, POST thành công và lỗi.
- Test chỉ dùng thư mục tạm + HubPaths; không đọc dữ liệu AITeam hay profile thật. Không có fixture thiết bị trong task này; tương thích cấu hình cũ được dựng bằng JSON tối thiểu theo hợp đồng H7-01.

## Bố cục giao diện
- Bọc nội dung bằng `.map-page`, tiêu đề Map theo khung Hub hiện có.
- Một form POST, bảng ba cột Nghiệp vụ / Tác nhân / Ghi chú quyền hạn; 11 dòng theo thứ tự brief. Mỗi select và ô ghi chú có nhãn truy cập phân biệt theo nghiệp vụ; mã nghiệp vụ hidden.
- Ghi chú dùng input hoặc textarea, maxlength=500; hidden Revision, token antiforgery thật, nút “Lưu phân vai”.
- Đặt giải thích “Gán vai không tự cấp quyền thực thi hoặc duyệt.” gần form; thông báo thành công/lỗi rõ ràng bằng thành phần alert của Hub.
- CSS chỉ trong `.map-page`: cột nghiệp vụ khoảng 35%, tác nhân 20%, ghi chú 45%; table-layout: fixed; th/td cho phép xuống dòng, overflow-wrap: anywhere; select/input/textarea width:100%, min-width:0. Không min-width cố định gây tràn; chiều cao ghi chú tự nhiên. Ở ngưỡng 1000px cho nhãn dài xuống dòng và có thể xếp ô thành dòng; không ẩn thông tin.
- Mục tiêu thiết kế là phù hợp 1280×800 và không tràn ngang ở 1000px. Agent xác minh bằng test dựng trang/markup; Claude nghiệm thu trực quan ở bước 7.

## Test nghiệm thu
`hub.tests/AcceptanceH707MapTests.cs`

## Tiêu chí hoàn thành
- [x] GET Razor thật trả 200; đủ 11 nghiệp vụ đúng nhãn/thứ tự, mỗi dòng đủ sáu tác nhân, có ghi chú và giải thích quyền hạn.
- [x] Thiếu cấu hình và cấu hình cũ nạp mặc định v4, không ghi file; dữ liệu đã lưu được nạp lại đúng.
- [x] POST có antiforgery; lưu/tải lại đủ dòng, ghi chú rỗng và 500 ký tự, giữ khóa cấu hình khác và cập nhật revision.
- [x] Chặn dữ liệu giả mạo/thiếu/trùng/thừa/hoa thường sai/quá dài/revision rỗng trước lời gọi lưu; CSRF trả 400; XSS chỉ là văn bản.
- [x] Lỗi đọc không cho lưu; lỗi ghi và xung đột giữ form/revision, không lộ thông tin nhạy cảm, không báo thành công.
- [x] CSS cục bộ, đủ nhãn truy cập, nội dung dài xuống dòng; kiểm bằng test dựng trang và markup.
- [x] `dotnet build AITeam.sln` 0 lỗi, 0 warning sau triển khai.
- [x] `dotnet test AITeam.sln` pass hết, bao gồm toàn bộ test nghiệm thu không sửa.

## Ghi nhận chuẩn bị bước 4 (Codex)
- Đã chuẩn bị đặc tả, khung PageModel/Razor và 27 ca nghiệm thu; chưa viết thân handler. CSS chỉ có chú thích TODO.
- `dotnet build AITeam.sln`: lượt 1 trả mã 1 nhưng công cụ báo 0 lỗi/0 warning, không có chẩn đoán.
- `dotnet build AITeam.sln -m:1 -v:normal`: lượt 2 thành công, 0 lỗi, 2 warning (CS9113 của khung được phép; xUnit2031 đã sửa thành Assert.Single với predicate sau build). Không build lần 3; sửa analyzer chưa được biên dịch lại.
- `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceH707MapTests --logger 'console;verbosity=quiet'`: 27 failed, 0 passed, 0 skipped; chạy assembly từ build lượt 2. Chưa chạy toàn bộ suite; nghiệm thu đang đỏ ở bước khung.
- Chỉ đọc 5 file mã liên quan ngoài phạm vi; không chạy git, không chạy app để kiểm giao diện. Test có host Razor nội bộ theo mẫu H6-08, dùng dịch vụ H7-01 trên thư mục tạm.
- Chưa xác minh trực quan 1280×800/1000px; Claude kiểm ở bước 7. Không có mockup/fixture thật riêng trong brief.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
| --- | --- |
| Dựng trang GET & POST Map hợp lệ, antiforgery, đủ 11 vai/6 actor | AcceptanceH707MapTests: 27/27 pass |
| Chặn dòng thừa (Roles[12], roles case-insensitive, overflow index), XSS, lỗi đọc/ghi | Form key validation chặn Roles[12]/roles/ROLES và TryParse chống overflow, Writes = 0 |
| Bảo toàn ký tự xuống dòng đầu trong textarea Note | Thêm LF đệm ngay sau mở textarea (@Html.Raw("\n")), 1-2 LF đầu giữ nguyên |
| Build & Test toàn bộ giải pháp | dotnet build: 0 lỗi, 0 warning; dotnet test: 886/886 pass (527 Hub + 359 Core) |

- **File đã đổi:** hub/Pages/Map.cshtml (LF đệm sau textarea), hub/Pages/Map.cshtml.cs (chặn dòng thừa Roles[12], OrdinalIgnoreCase, int.TryParse), tasks/H7-07-giao-dien-map.md (báo cáo).
- **Quyết định tự đưa ra:** Dùng RegexOptions.IgnoreCase và StringComparison.OrdinalIgnoreCase cho tiền tố Roles; dùng int.TryParse chặn số vượt Int32; không commit file test mới vào hub.tests để tuân thủ phạm vi task.
- **Chưa chắc:** Kiểm tra giao diện trực quan 1280x800/1000px trên trình duyệt thật (bàn giao Claude nghiệm thu ở bước 7).
## Review (Claude)
### Vòng 1 — ĐẠT (nghiệm thu, 25/10/2026, Claude)
- Lan 2 vòng (sửa 2 điểm QC: textarea xuống dòng đầu, …); cổng KT ĐẠT; Codex QC PASS 0 phát hiện.
- Claude mở Hub giả 1000×800: `/Map` 11 nghiệp vụ, mỗi dòng chọn 6 tác nhân, không tràn ngang.

## Kết quả
- **Chức năng mới — trang Map** (`/Map`): gán thủ công tác nhân + ghi chú quyền hạn cho 11 nghiệp vụ từ tiếp nhận tới publish git; lưu team.json (workflow.roles).

## Đánh giá quy trình
<!-- Claude điền sau nghiệm thu. -->
