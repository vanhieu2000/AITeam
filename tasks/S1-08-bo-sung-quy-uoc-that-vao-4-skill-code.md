# S1-08 · Bổ sung quy ước thật vào 4 skill code

- **Agent:** B
- **Nhánh:** `agent-b/S1-08-bo-sung-quy-uoc-that-vao-4-skill-code`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~200 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng mới cho người dùng; chỉ bổ sung tài liệu hướng dẫn lập trình.
- Ghi quy ước từ code thật của HDVTool, ChinaAndroidToolbox, AITeam Hub và core CLI vào bốn skill code.
- Nêu tên helper/lớp, cách test và bài học lỗi đã gặp, kèm nguồn kiểm tra được.
- Giữ mỗi skill tối đa 150 dòng để agent đọc nhanh và tránh áp dụng hướng dẫn chung sai dự án.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-010/brief.md` (mục Skill code) và task này.
- `skills/code-winforms/SKILL.md`, `skills/code-web/SKILL.md`, `skills/code-app/SKILL.md`, `skills/code-test/SKILL.md`.
- Hub: `hub/Pages/Office.cshtml`, `hub/Pages/Office.cshtml.cs`, `hub/Pages/Approvals.cshtml`, `hub/Pages/Approvals.cshtml.cs`, `hub/Services/OfficeService.cs`.
- Hub: `hub/Pages/Shared/_OfficeControl.cshtml`, `hub/Pages/Shared/_OfficeDesks.cshtml`, `hub.tests/PageTests.cs`, `hub.tests/AcceptanceV504OfficeTests.cs`; tối đa 15 file của Hub, phần còn lại chỉ là helper/test render được các file này gọi trực tiếp.
- Core CLI: `core/AITeam.Cli/Program.cs`, `core.tests/ReviewCliTests.cs`, `core.tests/DevRunnerCliTests.cs`; chỉ thêm lớp tiến trình/project test được các file này gọi/tham chiếu trực tiếp, tổng tối đa 15 file của core.
- HDVTool và ChinaAndroidToolbox (chỉ đọc, mỗi dự án tối đa 15 file): form chính + Designer tương ứng, hai UserControl tiêu biểu + Designer tương ứng, helper UiThread/LayoutChecker/UiSnapshot nếu có, hai test giao diện và project test chứa chúng.
- Với hai dự án WinForms: đường dẫn gốc chưa được xác minh trong lần chuẩn bị này. Chỉ tìm tên file trong thư mục dự án đã có trên máy, không quét toàn ổ đĩa/repo khác, không clone/cài thêm. Ghi danh sách đường dẫn thực tế đã chọn trong báo cáo; không vượt nhóm nguồn và hạn mức trên.
- Không đọc phiếu/task cũ, `bin/`, `obj/`, `logs/`, dữ liệu người dùng hay tài liệu không nằm trong danh sách. Nguồn lỗi dùng AGENTS.md và mã lỗi đã được ghi ở đó.

## Bối cảnh
Làm theo `plans/R-010/brief.md`, mục Skill code và kế hoạch S1-08 đã duyệt. Bốn skill đã có nội dung chung nhưng thiếu quy ước gắn với triển khai thật. Task chỉ bổ sung tài liệu; các nguồn ứng dụng/test đều chỉ đọc.

## Mục tiêu
Thêm mục `## Quy ước của dự án` vào từng skill. Mỗi quy ước ghi thao tác cụ thể, tên lớp/helper/hàm thật và nguồn theo dạng `đường-dẫn-tương-đối/file.cs:dòng`. Với dự án ngoài AITeam, thêm tên dự án trước nguồn để phân biệt; không ghi đường dẫn tuyệt đối chứa tên tài khoản máy.

### Nội dung cần bổ sung
1. `code-winforms`: tổng hợp riêng HDVTool và ChinaAndroidToolbox từ form chính, hai UserControl, helper và hai test giao diện của mỗi dự án. Ghi cách bố trí TableLayoutPanel/FlowLayoutPanel, AutoScaleMode/MinimumSize, chuỗi dài, quản lý luồng STA/UiThread, kiểm LayoutChecker và vai trò UiSnapshot đúng API đã đọc. Phân biệt test nền dựng ẩn với công cụ nghiệm thu; không chạy form/công cụ chụp ảnh.
2. `code-web`: Office/Approvals làm ví dụ Razor Pages; chỉ rõ PageModel, handler/partial, dịch vụ OfficeService và test thật. Bao phủ antiforgery, HTMX, hx-confirm trên phần tử phát request, ViewData của partial, encode dữ liệu và selector khớp thẻ. Dẫn chứng từng quy tắc bằng file thực sự có hành vi đó; không gán hx-confirm cho Approvals nếu markup không có.
3. `code-app`: bổ sung quy ước core CLI về điểm vào, xử lý lỗi/stderr/mã thoát, parser, lớp chạy tiến trình và cách kiểm thử. Giữ hướng dẫn MAUI hiện có, không tuyên bố đã kiểm app MAUI. Phân biệt yêu cầu bắt mọi lỗi với hiện trạng code: Program.cs có khối khởi tạo trước try; không mô tả nhầm toàn bộ entry point đã được bao try/catch.
4. `code-test`: ghi tên lớp/test/helper thực tế của WinForms, Hub và CLI, cách dựng trang/kiểm markup, cô lập dữ liệu và test tiến trình. Phân biệt test PageModel (PageTests) với test render Razor thật; chỉ khẳng định render khi đã thấy hạ tầng render và assert tương ứng. Ghi cách ProjectReference/AppContext.BaseDirectory thật nếu có, không suy ra từ ví dụ chung.
5. Cập nhật mục lỗi/nguồn trong từng skill: chọn bài học liên quan từ R-001, V5-05, H-007/H-008, R-006, V4-08, V5-07a; dẫn `AGENTS.md:dòng` và mã lỗi. Không bịa IMP hay mở task cũ để tìm nguồn.
6. Sau khi đọc được nguồn WinForms, bỏ câu “Chưa đọc code HDVTool/ChinaAndroidToolbox …”. Nếu thiếu dự án/helper/test thì thay bằng ghi chú chính xác nguồn nào chưa truy cập/không tìm thấy; không coi tiêu chí đọc nguồn tương ứng đã hoàn thành.

## Phạm vi (được sửa)
- `skills/code-winforms/SKILL.md`
- `skills/code-web/SKILL.md`
- `skills/code-app/SKILL.md`
- `skills/code-test/SKILL.md`
- `tasks/S1-08-bo-sung-quy-uoc-that-vao-4-skill-code.md` — chỉ điền mục Báo cáo của agent, không sửa đặc tả/metadata.

## Không làm
- Khi thực hiện: không sửa code sản phẩm, test, hợp đồng Hub, DI, solution/project hoặc helper của bất kỳ dự án nào.
- Không thêm interface/record/lớp/chữ ký public hay thân hàm; đây là task tài liệu, không có khung code cần tạo.
- Không tạo test nghiệm thu mới theo kế hoạch; mục Test nghiệm thu để trống vì không có file test mới.
- Không sửa `team.ps1`, `CLAUDE.md`, `team.json`, `projects.json`, `skill/`, `tools/` hoặc skill ngoài bốn file được giao.
- Không chạy agy/codex thật, không mở app/Designer/trình duyệt, không chụp ảnh, không đo bố cục.
- Không thay đổi cấu hình máy, không thêm tính năng hay xử lý lỗi phát hiện ngoài phạm vi; ghi chúng vào báo cáo.

## Ràng buộc
- Tổng ngân sách khoảng 200 dòng thay đổi nội dung skill; mỗi SKILL.md sau sửa tối đa 150 dòng, tính cả frontmatter, dòng trống và code fence.
- Giữ name/description/frontmatter và các mục bắt buộc hiện có; rút gọn chỗ trùng lặp để có chỗ cho quy ước thật. Không thay cả file bằng nội dung chung mới.
- Tiếng Việt có dấu; tên API/lớp đúng code thật; trích dẫn ngắn, không chép nguyên văn dài.
- Mọi nhận định “quy ước của dự án” phải có file:dòng đã đọc; số dòng theo nguồn tại thời điểm làm, không lấy số dòng ví dụ trong task làm bằng chứng mới.
- Hướng dẫn bắt buộc nhưng code chưa tuân thủ phải ghi rõ là yêu cầu/bài học, không hợp thức hóa code sai (ví dụ catch rỗng hoặc khởi tạo ngoài try).
- Tên lớp/helper/API không tồn tại hoặc nguồn không truy cập được: ghi thiếu và ảnh hưởng trong báo cáo, không tự tạo API, không khẳng định đã xác minh.
- Không ghi khóa/token/dữ liệu cá nhân hay đường dẫn riêng của máy. Ví dụ giống bí mật chỉ ghép lúc chạy; không thêm chuỗi bí mật trực tiếp.
- Giữ BOM UTF-8 của file đã có BOM. Không sửa file .cs/.cshtml/.ps1 trong task này.
- Không có hợp đồng ngoài, xử lý dữ liệu cũ mới, dịch vụ hoặc entry point mới; các ràng buộc bảo mật/CLI được tài liệu hóa, không triển khai trong S1-08.
- Không chạy lệnh git trong phiên này; hệ thống tự commit. Giữ nguyên các dòng Agent/Nhánh/Yêu cầu/Review AI đã cấp dù nhãn Agent khác kế hoạch.

## Test nghiệm thu

## Tiêu chí hoàn thành
- [ ] Cả bốn skill có mục Quy ước của dự án, nội dung riêng theo phân công trên, có tên lớp/helper/test thật và dẫn chứng file:dòng.
- [ ] Đã đọc đủ nhóm nguồn của HDVTool và ChinaAndroidToolbox; mỗi dự án không quá 15 file. Nguồn thiếu được báo rõ và tiêu chí liên quan chưa đánh dấu đạt.
- [ ] Quy ước Hub bao phủ Office, Approvals, dịch vụ và cách test thật; quy ước CLI dẫn điểm vào, lỗi/mã thoát và test thực tế.
- [ ] Mỗi skill có bài học lỗi liên quan kèm mã và nguồn; bỏ tuyên bố chưa đọc WinForms sau khi đã xác minh, không bịa bằng chứng.
- [ ] Mỗi skill ≤150 dòng; giữ frontmatter/cấu trúc bắt buộc, chỉ sửa đúng phạm vi và không lộ dữ liệu riêng.
- [ ] `dotnet build AITeam.sln` thành công, 0 lỗi/0 warning mới; không chạy build quá hai lần trong phiên chuẩn bị.
- [ ] `dotnet test AITeam.sln` pass toàn bộ test hiện có; báo số pass/fail/skip thật, không sửa/tắt test. Không cần test mới vì chỉ tài liệu.
- [ ] Báo cáo có bảng từng tiêu chí → bằng chứng, số dòng từng skill, danh sách nguồn đã đọc và file thay đổi/lý do, quyết định tự đưa ra, điều chưa xác minh.

## Kiểm tra ở bước chuẩn bị (Codex)
- Chỉ điền đặc tả task; chưa sửa bốn skill. Không tạo khung code/test mới vì kế hoạch chỉ tài liệu.
- Giữ nguyên Agent/Nhánh/Yêu cầu/Review AI; file task lưu UTF-8 có BOM. Không chạy git.
- Build lần 1: `dotnet build AITeam.sln` trả 1, ghi 0 warning/0 error nhưng không có chẩn đoán nguyên nhân.
- Build lần 2: `dotnet build AITeam.sln --disable-build-servers -m:1 -v:normal` thành công, 0 lỗi, 8 warning NU1900 do không truy cập được dịch vụ NuGet kiểm tra lỗ hổng. Đã dùng hết giới hạn hai lần build; chưa xác nhận build 0 warning.
- `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: thành công; Hub 457 pass, core 358 pass; tổng 815 pass, 0 fail, 0 skip.
- Đã đọc đúng năm file mã liên quan: Office.cshtml, Approvals.cshtml, OfficeService.cs, PageTests.cs và core/AITeam.Cli/Program.cs; chưa xác minh nguồn hai dự án WinForms. Antigravity cần hoàn thành việc đọc nguồn và nội dung skill theo tiêu chí trên.
## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| 1. Bốn mục quy ước có nguồn | Kiểm bằng PowerShell ReadAllText/Contains: đủ mục bắt buộc; dẫn lớp/helper/test và file:dòng đã đọc trong cả bốn skill. |
| 2. Nhóm nguồn WinForms | Đọc HDVTool 9 file, ChinaAndroidToolbox 13 file (danh sách dưới), không vượt 15; không tìm thấy Designer tương ứng của HDVTool, ghi đúng cách dựng bằng code; chưa xác minh Designer round trip. |
| 3. Hub/CLI | Đọc 9 file Hub và 5 file core dưới đây; phân biệt PageModel/đọc mã Razor với render, khởi tạo CLI trước try, lỗi stdout/stderr và timeout còn thiếu. |
| 4. Bài học/nguồn | V5-05/H-007/H-008/R-006/V4-08 dẫn AGENTS.md:48/50/52/55/56; R-001 chỉ có brief:6, AGENTS.md không có R-001/V5-07a nên không bịa nguồn; bỏ câu chưa đọc WinForms. |
| 5. Giới hạn/cấu trúc/BOM | PowerShell ReadAllLines/ReadAllBytes: winforms 97, web 100, app 111, test 108 dòng; đủ frontmatter/mục cũ, BOM cả bốn file; không đổi code/test/cấu hình. |
| 6. Build | dotnet build AITeam.sln --disable-build-servers -m:1; lần 2 thêm --no-restore: exit 0, 0 lỗi, 4 warning NU1900 (NuGet không truy cập được, cùng loại đã ghi ở bước chuẩn bị); chưa đạt 0 warning tuyệt đối. |
| 7. Test | dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m: lần cuối exit 0, Hub 457 + core 358 = 815 pass/0 fail/0 skip; lần đầu 1 fail regex Show đã sửa cách diễn đạt tài liệu, không sửa test. |
| 8. Phạm vi/quyết định/chưa kiểm | Chỉ đổi bốn skills/code-{winforms,web,app,test}/SKILL.md để thêm quy ước/nguồn và mục báo cáo task này; giữ BOM. Không chạy app/UI/MAUI/UiSnapshot; tự ghi khác biệt Show/Visible và giới hạn CLI, không sửa ngoài phạm vi; không chạy git theo chỉ dẫn, hệ thống tự commit/push. |
| Nguồn HDVTool (đường dẫn tương đối gốc dự án) | src/HDVTool.App/Shell/MainForm.cs:101; src/HDVTool.App/Views/HomeView.cs:28; src/HDVTool.App/Views/SettingsView.cs:23; tests/HDVTool.Core.Tests/UiLayout/{UiThread.cs:7,LayoutChecker.cs:17,ViewLayoutTests.cs:79,VietnameseWordBreakTests.cs:19}; tests/HDVTool.Core.Tests/HDVTool.Core.Tests.csproj:19; tools/UiSnapshot/Program.cs:18. |
| Nguồn ChinaAndroidToolbox (đường dẫn tương đối gốc dự án) | src/ChinaAndroidToolbox.App/{MainForm.cs:22,MainForm.Designer.cs:309,Views/DashboardView.cs:23,Views/DashboardView.Designer.cs:82,Views/SettingsView.cs:14,Views/SettingsView.Designer.cs:233}; tests/ChinaAndroidToolbox.Tests/{AcceptanceC011Tests.cs:37,ChinaAndroidToolbox.Tests.csproj:4,UiLayout/ViewLayoutTests.cs:37,UiLayout/UiThread.cs:9,UiLayout/LayoutChecker.cs:19,UiLayout/TestUiContext.cs:22}; tools/UiSnapshot/Program.cs:11. |
| Nguồn AITeam | Hub: hub/Pages/{Office.cshtml:22,Office.cshtml.cs:17,Approvals.cshtml:61,Approvals.cshtml.cs:9,Shared/_OfficeControl.cshtml:19,Shared/_OfficeDesks.cshtml:96}; hub/Services/OfficeService.cs:41; hub.tests/{PageTests.cs:14,AcceptanceV504OfficeTests.cs:151}. Core: core/AITeam.Cli/Program.cs:9; core/AITeam.Core/Process/ProcessRunner.cs:7; core.tests/{ReviewCliTests.cs:13,DevRunnerCliTests.cs:14,AITeam.Core.Tests.csproj:19}. Hướng dẫn: AGENTS.md:47, plans/R-010/brief.md:5 và bốn skill/task được giao. |
## Review (Claude)
### Vòng 1 — ĐẠT (nghiệm thu, 25/10/2026, Claude)
- Codex đọc code thật HDVTool / ChinaAndroidToolbox / Hub, thêm mục 'Quy ước của dự án' (file:dòng) vào 4 skill code; cổng KT ĐẠT; QC BLOCKED đúng luật (Codex không tự duyệt).
- Optional (hàng chờ): LayoutChecker bỏ qua cây control không Visible ⇒ test bố cục trên form ẩn có thể đạt giả — cần task sửa helper ở HDVTool / Toolbox.

## Kết quả
- Skill code có quy ước THẬT của từng dự án (helper UiThread / LayoutChecker / UiSnapshot, bố cục từng view, cách test Hub).

## Đánh giá quy trình
<!-- Claude điền ngay sau khi merge. Điểm 1–5 (5 = không vấn đề). Nguyên nhân: TK (task / thiết kế Claude) · AG (agent) · CK (công cụ / cổng kiểm tra) · NV (nghiệp vụ / chủ dự án) · HT (hạ tầng / provider). -->
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
