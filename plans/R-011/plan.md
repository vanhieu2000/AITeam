# Kế hoạch R-011
## Hiểu yêu cầu
Chuẩn hóa helper test và checklist QC để task mới không dựng lại harness.
Bổ sung mockup và chặn `prep` trước khi quản lý duyệt kế hoạch/giao diện.
HieuDV chọn dự án, chọn yêu cầu/task trên trang “Việc” để xem bước, người phụ trách, thời gian và lỗi.
Chỉ nghiên cứu, chưa sửa file hoặc chạy build/test vì các lệnh này tạo dữ liệu.
## Research
- `projects.json:1,23`: cấu hình gốc là mảng; A thiên về Services/test, B thiên về Pages/CSS; phân công chỉ gợi ý.
- `AGENTS.md:48`: phải giữ BOM, cô lập dữ liệu test, chống test luôn pass, XSS và đường dẫn thoát thư mục.
- `core.tests/Support/TestShell.cs:16,30,55`: đã có PowerShell UTF-8, timeout, dọn biến Git và `TempRepo`; chưa có `RealFile`/`TeamScript`.
- `core.tests/AcceptanceS104SkillPromptsTests.cs:8,27`; `core.tests/AcceptanceV801WorkflowTests.cs:17,43`: harness AST/chép nguồn/chạy tiến trình đang lặp. CHƯA XÁC MINH harness V8-02 trên nhánh đang làm.
- `hub.tests/TestEnvironment.cs:5,21`: lớp dùng chung hiện có là `TestEnvironment`, cung cấp `HubPaths` tạm; không phải bộ dựng trang.
- `hub.tests/AcceptanceH607PerformanceTests.cs:136`; `hub.tests/AcceptanceH707MapTests.cs:36`: có hàm riêng dựng Razor và HTTP thật; cần gom thành helper chung.
- `team.ps1:943,958`: `plan` ghi kết quả; `prep` đọc mục task rồi tạo worktree, chưa kiểm duyệt.
- `templates/codex-plan.md:9`; `templates/codex-prep.md:12`: mẫu plan chưa có Mockup; prep đã yêu cầu dữ liệu thật/helper chung.
- `tools/workflow-common.ps1:169,231`: nhật ký hỗ trợ phiếu có slug, giữ BOM/xuống dòng; sự kiện chỉ Prep/Assign/Verify/Qc/Merge.
- `hub/Services/RequestService.cs:80`: parser hiện chỉ nhận nhật ký dạng bảng, bỏ qua dòng workflow dạng gạch đầu dòng.
- `team.ps1:144,727,1513`: log nằm trong `<WorkDir>/logs`, không mặc định `<repo>/logs`; tên verify/review đúng brief.
- `requests/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md:46,52,57`; `tasks/README.md:46`: có bằng chứng Merge và các task S1 đã ✅.
- `requests/R-008-day-viec-khoi-claude-day-chuyen-tu-chay.md:41`; `tasks/README.md:55`: V8-02 Verify lượt 3 KHÔNG ĐẠT, bảng ghi đang chạy 20:58.
- `plans/R-009/plan.md:114`; `requests/R-009-hub-v61-theo-gop-y-van-phong-the-nghien.md:41`: kế hoạch đã duyệt nhưng H7-07 đã Merge, khác giả định “task chưa làm” trong brief.
- `hub/Pages/Shared/_Layout.cshtml:84,91`; `hub/Pages/Project.cshtml.cs:9`: có Đơn duyệt/menu dự án và bảng việc để tận dụng.
- `core/AITeam.Core/Review/ReviewService.cs:24`: JSON reviewer có `verdict/findings/unverified`; skill QC hiện mô tả đầu ra Markdown (`skills/09-qc/SKILL.md:9`).
## Rủi ro
- Mất BOM/CRLF hoặc sai tên fixture → chép byte nguyên bản, chỉ biến đổi bản sao tạm; không sửa test nghiệm thu cũ.
- Harness gọi AI/push thật → nạp hàm bằng AST, giả lập ranh giới có ghi nhận lời gọi, chặn mặc định mọi lệnh ngoài cho phép.
- Duyệt giả qua ví dụ/comment hoặc sửa mockup sau duyệt → chỉ nhận mục quyết định chính thức; duyệt gắn dấu vân tay nội dung.
- XSS/thoát đường dẫn từ HTML, mã phiếu hoặc tên dự án → whitelist ID, chuẩn hóa/chặn reparse point; mockup tĩnh, Razor encode.
- Nhật ký thiếu, lỗi cũ sau Merge, nhiều task khác bước → tính từng task, giữ lịch sử lỗi, không lấy bước lớn nhất làm trạng thái cả yêu cầu.
- Trùng R-009/V8-02 → không sửa `tools/workflow-pipeline.ps1`; bàn giao menu theo mã thực tế, mọi task sửa `team.ps1` chạy tuần tự.
## Task
Codex chuẩn bị hợp đồng/khung/test trước; mỗi task ≤400 dòng triển khai, ≤15 phút; task khó dùng `gemini-4-argon-high`.
W1-01 và W1-02 song song; sau đó W1-03/W1-05 song song; W1-04 sau W1-03. W1-06 có thể chạy cùng nhánh quy trình; W1-07 sau duyệt mockup.
Mỗi task phải build 0 warning, test pass và báo cáo tiêu chí → bằng chứng; kiểm tra trình duyệt/ảnh do Claude ở bước nghiệm thu.
### W1-01 · Helper nguồn thật và sandbox PowerShell
- **Agent:** A
- **Ngân sách:** ~350 dòng
- **Phạm vi:** `core.tests/Support/RealFile.cs` · `core.tests/Support/TeamScript.cs`
- **Làm:**
  - `RealFile.Copy(relativePathInRepo, targetDir)` trả đường dẫn bản sao, giữ cây tương đối, tên và byte; đường dẫn sai báo “Đường dẫn bản sao không hợp lệ.”
  - `TeamScript` dùng `TempRepo`, chép nguồn thật cùng `tools/`, `scripts/`, `templates/`; thư mục tùy chọn thiếu được bỏ qua.
  - Nạp các hàm được chọn bằng AST, không chạy dispatcher; dùng `TestShell`, fake agy/codex/git push và ghi đối số/kết quả; không sửa acceptance cũ.
- **Test nghiệm thu:** BOM/CRLF/tên có dấu; thiếu file; traversal/tuyệt đối/reparse; AST lỗi; fake thành công/thất bại; timeout; không gọi AI/push thật.
- **Phụ thuộc:** —
### W1-02 · Helper dựng trang Hub
- **Agent:** B
- **Ngân sách:** ~190 dòng
- **Phạm vi:** `hub.tests/Support/HubTestHost.cs` · `hub.tests/Support/HubPageRenderer.cs`
- **Làm:**
  - Gom cách dựng HTTP/Razor hiện có; nhận `TestEnvironment`, cấu hình DI và model/ViewData từ test.
  - HTTP chỉ nghe loopback cổng tự chọn, giữ cookie/antiforgery; render trả HTML nguyên bản để kiểm encode.
  - Dọn host khi thành công/lỗi; không đọc profile/repo thật, không sửa các test nghiệm thu đã khóa.
- **Test nghiệm thu:** GET thật, partial/ViewData, POST thiếu/đủ token, XSS còn encode, host song song không trùng cổng, dọn sau lỗi.
- **Phụ thuộc:** —
### W1-03 · Sinh mockup và khóa prep trước duyệt
- **Agent:** A
- **Ngân sách:** ~320 dòng
- **Phạm vi:** `team.ps1` · `templates/codex-plan.md` · `templates/codex-prep.md`
- **Làm:**
  - Mẫu plan khai báo task có giao diện, yêu cầu đúng một khối HTML tĩnh ≤120 dòng trong `## Mockup`; không xác định được loại task thì yêu cầu bổ sung.
  - `plan` kiểm khối HTML, chỉ cho CSS Hub/tĩnh, ghi `docs/design/<R-xxx>-mockup.html` với đường dẫn CSS mở được tại máy; không thực thi HTML.
  - `prep` kiểm trước fetch/worktree/provider: mục “Quyết định của quản lý …” phải có ĐÃ DUYỆT; giao diện cần duyệt mockup kèm dấu vân tay nội dung hiện tại.
  - Thiếu duyệt báo “Kế hoạch chưa được duyệt.”; thiếu/sai/đổi mockup báo “Mockup chưa được duyệt hoặc đã thay đổi.”; plan cũ chỉ lõi vẫn nhận duyệt hiện có.
- **Test nghiệm thu:** plan lõi/UI; HTML 120/121 dòng, nhiều khối, script/event handler/URL ngoài; duyệt trong comment không hợp lệ; sửa sau duyệt; từ chối không phát sinh thao tác phụ.
- **Phụ thuộc:** W1-01
### W1-04 · Cổng kiểm tra và tài liệu QC cố định
- **Agent:** B
- **Ngân sách:** ~100 dòng — Claude/Codex làm thẳng, ≤15 phút
- **Phạm vi:** `team.ps1` · `docs/TEST-CHUAN.md` · `skills/09-qc/SKILL.md`
- **Làm:**
  - Cổng KT cảnh báo file test mới dùng `new ProcessStartInfo`, kể cả namespace đầy đủ; nhận diện bằng diff thêm file, miễn helper Support được chỉ định.
  - Viết hướng dẫn ≤60 dòng: ca bắt buộc cho lệnh/dịch vụ/trang/tài liệu-skill, helper tương ứng và một mẫu test.
  - Checklist QC cố định: đúng commit, phạm vi, test bất biến, dữ liệu thật, bảo mật, lỗi/biên/hồi quy; đầu ra JSON giữ schema hiện tại, phần chưa chạy vào `unverified`.
- **Test nghiệm thu:** file mới bị cảnh báo, file cũ không bị truy hồi, helper được miễn; đường dẫn có dấu; giới hạn tài liệu; QC JSON hợp lệ và không tự nhận pass.
- **Phụ thuộc:** W1-01 · W1-02 · W1-03
### W1-05 · Parser tiến độ và quy tắc chuyển bước
- **Agent:** A
- **Ngân sách:** ~280 dòng
- **Phạm vi:** `hub/Services/WorkProgressParser.cs`
- **Làm:**
  - Parse nhật ký bảng cũ/dòng workflow thật thành sự kiện có thời gian, task, lượt, kết quả; không đổi định dạng nguồn.
  - Tạo model riêng và hàm thuần tính chín bước; Prep đạt → Code, Verify đạt → QC, QC đạt → Nghiệm thu, Merge đạt/✅ → Merge; lỗi giữ bước cần sửa.
  - Phân biệt bước đang chờ/đã xong; thiếu giờ/người hiện “Chưa có dữ liệu”; Assign tổng kết không được che Verify/QC lỗi cùng lượt.
- **Test nghiệm thu:** CRLF/BOM, heading đánh số, dòng trùng/sai, lượt 2/10, timezone, lỗi rồi thành công, QC BLOCKED, Merge sau lỗi cũ, không suy diễn đã nghiệm thu.
- **Phụ thuộc:** —
### W1-06 · Dịch vụ tổng hợp việc theo dự án
- **Agent:** B
- **Ngân sách:** ~300 dòng
- **Phạm vi:** `hub/Services/WorkProgressService.cs` · `hub/Program.cs`
- **Làm:**
  - Nhận dự án từ `ITeamConfigService`; đọc requests/plans/tasks trong repo dự án, nối bằng dòng Yêu cầu; log theo WorkDir, người theo cấu hình hiện có.
  - Trả yêu cầu kèm từng task và tiến độ W1-05; yêu cầu chưa có task vẫn hiện; nhiều task hiện số lượng theo bước.
  - Chỉ đọc, không migrate; ID sai báo “Việc không hợp lệ.”; nguồn hỏng báo “Không đọc được tiến độ công việc.” kèm tên nguồn an toàn.
- **Test nghiệm thu:** bản sao R-010/V8-02/R-009; R-009 có H7-07 Merge; dự án rỗng/khác repo, task cũ thiếu liên kết, log mất/hỏng, traversal/reparse, GET không thay byte nguồn.
- **Phụ thuộc:** W1-05
### W1-07 · Trang Việc và thanh bước
- **Agent:** B
- **Ngân sách:** ~330 dòng
- **Phạm vi:** `hub/Pages/Project.cshtml*` · `hub/Pages/Shared/_Layout.cshtml` · `hub/wwwroot/css/hub.css`
- **Làm:**
  - Tận dụng trang dự án/bảng việc hiện có thành tab “Việc”; chọn dự án dùng menu chung, tránh tạo danh sách/bảng trùng R-009.
  - Chọn yêu cầu rồi task → chín bước, bước hiện tại đậm, giờ/người/lỗi gần nhất; link Đơn duyệt giữ handler duyệt hiện có.
  - Chỉ GET, không nút chạy lệnh; rỗng hiện “Chưa có việc.”; chuỗi 140 ký tự xuống dòng, thanh bước tự xuống hàng ở 1000px.
- **Test nghiệm thu:** HTTP/Razor thật bằng helper; lọc/chọn đúng dự án/task; đủ chín bước; dữ liệu thiếu, XSS, URL giả; Claude kiểm ảnh 1280×800/1000px và đối chiếu nguồn thật.
- **Phụ thuộc:** W1-02 · W1-06 · mockup được duyệt
## Mockup
```html
<!doctype html><html lang="vi"><meta charset="utf-8"><title>Việc · AITeam</title>
<link rel="stylesheet" href="../../hub/wwwroot/css/hub.css">
<style>body{margin:24px;background:var(--bg);color:var(--text)}main{max-width:1100px;margin:auto}nav,ol{display:flex;flex-wrap:wrap;gap:16px}section{padding:16px;margin-top:16px;background:var(--surface);border:1px solid var(--line)}p,a,li{overflow-wrap:anywhere}li{padding:6px}li[aria-current]{font-weight:700;color:var(--accent)}</style>
<main><nav><a href="#">Văn phòng</a><b>Việc</b><a href="#">Dự án: AITeam ▾</a><a href="#">Đơn duyệt</a></nav>
<h1>Việc · AITeam</h1><p class="muted">Dữ liệu minh họa để duyệt bố cục</p>
<section><h2>Yêu cầu</h2><p>R-010 · Bộ skill — ✅ Merge</p><p>R-009 · Hub v6.1 — Kế hoạch đã duyệt · có task đã Merge</p><p><b>R-008 · Dây chuyền tự chạy → V8-02</b></p></section>
<section><h2>V8-02 · Điều phối pipeline</h2>
<ol><li>Tiếp nhận</li><li>Kế hoạch</li><li>Duyệt kế hoạch</li><li>Chuẩn bị</li><li>Code</li><li aria-current="step">Cổng KT</li><li>QC</li><li>Nghiệm thu</li><li>Merge</li></ol>
<p>20:58 25/10/2026 · Cổng KT · KHÔNG ĐẠT, lượt 3</p><p>Phụ trách code: Minh · xem chi tiết lỗi từ báo cáo kiểm tra</p></section></main></html>
```
## Câu hỏi cho quản lý
- Chốt cập nhật tiêu chí R-009 thành “kế hoạch đã duyệt, hiển thị đúng từng task, gồm H7-07 đã Merge” theo dữ liệu thật?

## Quyết định của quản lý (Claude, 25/10 21:45)
- ĐÃ DUYỆT. Chủ dự án duyệt mockup `docs/design/R-011-mockup.html` (bản Codex vẽ lại) — W1-07 làm theo bố cục này, không theo khối HTML ở mục Mockup trên.
- Thứ tự ưu tiên: trang Việc trước — W1-05 + W1-02 song song → W1-06 → W1-07; sau đó W1-01 → W1-03 → W1-04.
- R-009: tiêu chí đổi thành "kế hoạch đã duyệt, hiển thị đúng từng task, gồm H7-07 đã Merge".
- Test dùng `core.tests/Support/TestShell.cs`; task khó model `gemini-4-argon-high`; W1-04 Codex làm thẳng.
