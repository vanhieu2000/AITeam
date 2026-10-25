# Kế hoạch R-010
## Hiểu yêu cầu
Chuẩn hóa dây chuyền bằng 11 skill nghiệp vụ và 4 skill code dùng chung mọi dự án trong `skills/`.
Codex soạn nội dung; Minh/Lan nối lời nhắn, lệnh ghi bài học và quy trình nghiệm thu.
Skill ngắn, có nguồn, ví dụ tốt/xấu; thiếu file không làm gián đoạn công cụ.
Lỗi mới sau đánh giá được bổ sung có chống trùng và commit riêng.
## Research
- `AGENTS.md:45–60`: bắt buộc giữ BOM, tránh test luôn pass, bảo vệ đường dẫn, bắt ngoại lệ CLI; nguồn lỗi đã có V4-08, R-006, H-007, H-008.
- `projects.json:20–25`: Hub dùng Razor Pages + HTMX; A thiên dịch vụ/test, B thiên giao diện. Hai dự án còn lại dùng WinForms (`:7`, `:33`).
- `team.ps1:65,108,398,838,904`: có gốc AITeam và thư mục dự án riêng; prompt giao code, plan, prep được dựng tại đây. Không thể giả định `skills/` nằm trong thư mục chạy agent.
- `core/AITeam.Core/Review/ReviewRunner.cs:87`, `core/AITeam.Cli/Program.cs:254–259`: review dựng prompt riêng; CLI đã biết `root`, cần truyền ngữ cảnh skill xuống mà giữ tương thích lời gọi cũ.
- `templates/codex-prep.md:3–15`: Codex chỉ dựng khung/test, không chạy git; agent không nghiệm thu trực quan. Skill phải giữ phân vai này.
- `%USERPROFILE%/.claude/skills/winforms-expert/SKILL.md:19–36,127`: có hướng dẫn Designer, kiểm chứng và sửa bố cục theo chuỗi container.
- `D:/HDVTool/src/HDVTool.UI/Controls/Layouts.cs:9,20`; `D:/ChinaAndroidToolbox/tests/ChinaAndroidToolbox.Tests/UiLayout/LayoutChecker.cs:55–56`: có FlowLayoutPanel và bộ kiểm bố cục thật.
- `D:/HDVTool/tests/HDVTool.Core.Tests/UiLayout/UiThread.cs:14–36`: STA, timeout, truyền lại ngoại lệ; `ViewLayoutTests.cs:92,104` vẫn gọi `Show()` — không sao chép hành vi này vào skill.
- `requests/R-001-hoan-tat-kiem-tra-bo-cuc-giao-dien-tu-do.md:35–38`: test UI từng mở hộp thoại khiến agent và cổng kiểm tra treo.
- `hub/Pages/Shared/_OfficeDesks.cshtml:96–101`, `hub/Pages/Index.cshtml.cs:101`: xác nhận trên form và partial có ViewData dựng tay.
- Đã tìm thấy skill gốc `create-datadriven-aspnetcore`, `dotnet-webapi`, `author-component`, `scaffold-dotnet-test-project`, bộ `maui-*` dưới `%USERPROFILE%/.claude/skills/`; CHƯA XÁC MINH đầy đủ nội dung toàn bộ bộ MAUI.
- `templates/improvement.md:18–24`, `CLAUDE.md:93–112`: đã có phân loại TK/AG/CK và bước đánh giá; chưa có nhắc `skill-note`.
- `plans/R-009/brief.md:8`, `plans/R-009/plan.md:100–108`: Map có 11 nghiệp vụ; hiện chưa có `hub/Pages/Map.cshtml`. Liên kết skill ghi hàng chờ.
- Hiện chưa có thư mục `skills/`. Chỉ đọc; chưa chạy build/test vì có thể sinh file.
## Rủi ro
- Skill tương đối bị đọc sai ở dự án khác → xuất đường dẫn tuyệt đối từ gốc AITeam, không ghi cứng ổ đĩa.
- Stack/file cấu hình cũ thiếu dữ liệu → chỉ thêm skill tồn tại; stack rỗng/lạ vẫn dùng skill nghiệp vụ.
- `skill-note` bị lợi dụng ghi ngoài repo hoặc chèn Markdown → chỉ nhận 15 ID cho phép, chặn liên kết ra ngoài, Text/Source một dòng, giới hạn độ dài.
- Ghi bài học làm hỏng nội dung hoặc commit lẫn việc khác → kiểm tra trước ghi, giữ BOM/newline, chỉ stage đúng file; từ chối file đích đang sửa.
- Nguồn cũ trái luật hiện tại hoặc chứa thông tin riêng → tóm lược có nguồn, ưu tiên luật hiện hành, không chép bí mật hay đường dẫn cá nhân vào ví dụ.
- R-009 sửa cùng `team.ps1` → task nối prompt làm trên nền mới nhất, kiểm hồi quy phân vai; không tự triển khai Map trong R-010.
## Task
S1-01…03 do Codex soạn trực tiếp, chỉ đổi `skills/**`, không giao agy; A/B là gợi ý điều phối theo khung. Các task công cụ chia đều người rảnh; mỗi task ≤15 phút, ngân sách không tính test nghiệm thu do Codex viết trước.
### S1-01 · Skill tiếp nhận đến duyệt kế hoạch
- **Agent:** A
- **Ngân sách:** ~300 dòng
- **Phạm vi:** `skills/01-tiep-nhan/SKILL.md` · `skills/02-y-tuong/SKILL.md` · `skills/03-research/SKILL.md` · `skills/04-ke-hoach/SKILL.md` · `skills/05-duyet-ke-hoach/SKILL.md`
- **Làm:**
  - Codex viết 5 skill, mỗi file khoảng 60 dòng, đủ 7 mục theo brief; dùng mục tiêu/đầu vào/đầu ra rõ ràng.
  - Phân biệt ý tưởng, bằng chứng research và quyết định duyệt; research chỉ đọc, ghi `file:dòng` và “CHƯA XÁC MINH”.
  - Kế hoạch chia task ≤400 dòng, ≤15 phút; duyệt kế hoạch kiểm phạm vi, phụ thuộc, rủi ro và khả năng chạy song song.
- **Test nghiệm thu:** kiểm đủ ID/mục, ≤150 dòng/file, ví dụ tốt/xấu và nguồn lỗi thật; review nội dung không nhận giả định làm bằng chứng.
- **Phụ thuộc:** —
### S1-02 · Skill chuẩn bị đến publish
- **Agent:** B
- **Ngân sách:** ~360 dòng
- **Phạm vi:** `skills/06-chuan-bi-task/SKILL.md` · `skills/07-code/SKILL.md` · `skills/08-kiem-thu/SKILL.md` · `skills/09-qc/SKILL.md` · `skills/10-nghiem-thu/SKILL.md` · `skills/11-publish-git/SKILL.md`
- **Làm:**
  - Codex viết 6 skill đủ cấu trúc; prep dựng khung/test trước, code giữ phạm vi và test nghiệm thu.
  - Tách cổng KT, QC độc lập và nghiệm thu trực quan; nêu bằng chứng thực chạy, commit được kiểm và điều chưa xác minh.
  - Nghiệm thu nhắc lỗi TK/AG/CK mới → `skill-note`; publish dành cho người có quyền, kiểm private, duyệt merge, clone sạch/build/test rồi push, cấm force-push main.
- **Test nghiệm thu:** đủ mục/nguồn/ví dụ, giới hạn dòng; không cho coder tự merge, không nhận build thay bằng chứng UI, không báo pass khi chưa chạy.
- **Phụ thuộc:** —
### S1-03 · Bốn skill code theo stack
- **Agent:** A
- **Ngân sách:** ~380 dòng
- **Phạm vi:** `skills/code-winforms/SKILL.md` · `skills/code-web/SKILL.md` · `skills/code-app/SKILL.md` · `skills/code-test/SKILL.md`
- **Làm:**
  - Codex tóm lược skill gốc có đường dẫn nguồn; đọc bổ sung đúng phần MAUI cần dùng, không sao chép nguyên văn dài.
  - WinForms: Table/Flow layout, DPI 100–200%, MinimumSize, chuỗi dài, Designer, STA/UiThread; test nền dựng ẩn, LayoutChecker; UiSnapshot thuộc nghiệm thu.
  - Web: Razor/HTMX, antiforgery, xác nhận trên form, tránh Html.Raw, partial ViewData, test render thật, rộng 1000px không tràn; API/Blazor chỉ áp dụng khi dự án dùng.
  - App/test: MAUI lifecycle/binding/DI/navigation; CLI bắt mọi ngoại lệ, stderr `LỖI: …`, mã khác 0, ArgumentList; xUnit, DOS names, bí mật ghép lúc chạy, ProjectReference.
- **Test nghiệm thu:** đủ 4 file/mục/nguồn/ví dụ, ≤150 dòng/file; có checklist R-001 và các lỗi AGENTS; không đưa `Show()` vào hướng dẫn test nền.
- **Phụ thuộc:** —
### S1-04 · Nối skill vào plan, prep và giao code
- **Agent:** A
- **Ngân sách:** ~280 dòng
- **Phạm vi:** `team.ps1` · `scripts/SkillPrompts.ps1`
- **Làm:**
  - Thêm helper dựng dòng `Đọc skill: ...` từ gốc AITeam; mỗi đường dẫn chỉ xuất một lần và phải tồn tại.
  - Plan nhận 03-research + 04-ke-hoach; prep nhận 06-chuan-bi-task + code-test; giao agy/Codex nhận 07-code + code-test.
  - Bổ sung skill stack không phân biệt hoa thường: WinForms → code-winforms, ASP.NET/Blazor → code-web, MAUI/console/CLI → code-app; stack hỗn hợp nhận đủ.
  - Giữ quyền chỉ đọc của plan; cho phép đọc các skill được chèn dù prompt cũ giới hạn danh sách file.
- **Test nghiệm thu:** prompt thật qua seam giả lập provider; đủ các luồng agy/Codex/fallback, gốc có dấu/khoảng trắng, dự án ngoài AITeam, thiếu file, stack null/lạ/hỗn hợp; không gọi AI thật.
- **Phụ thuộc:** —
### S1-05 · Nối skill vào review độc lập
- **Agent:** B
- **Ngân sách:** ~220 dòng
- **Phạm vi:** `core/AITeam.Core/Skills/SkillPromptBuilder.cs` · `core/AITeam.Core/Review/ReviewRunner.cs` · `core/AITeam.Cli/Program.cs`
- **Làm:**
  - Truyền ngữ cảnh tùy chọn từ CLI `--root` xuống runner; lời gọi cũ không cung cấp vẫn hoạt động.
  - Prompt review thêm 09-qc + code-test + skill stack; xác định dự án từ cấu hình và WorkDir/task, không đoán stack từ diff.
  - Thiếu skill hoặc không xác định được dự án → bỏ phần tương ứng; giữ nguyên schema kết quả, hash diff và cấm tự duyệt.
- **Test nghiệm thu:** provider giả ghi nhận prompt cuối; review trực tiếp và tự động, worktree khác root, cấu hình cũ/stack rỗng, thiếu skill, hash/schema không đổi; chỉ dùng root/profile tạm.
- **Phụ thuộc:** —
### S1-06 · Lệnh ghi bài học có commit
- **Agent:** B
- **Ngân sách:** ~300 dòng
- **Phạm vi:** `team.ps1` · `scripts/SkillNotes.ps1`
- **Làm:**
  - Thêm `skill-note -Skill <id> -Text <text> [-Source <source>]`; chèn bullet vào đúng mục “Lỗi hay gặp”, trước mục kế tiếp.
  - Chống trùng theo Text đã trim/gộp khoảng trắng; lần lặp không ghi/commit lại; Source có thì ghi kèm, thiếu thì không bịa nguồn.
  - ID sai → `LỖI: Skill không hợp lệ.`; Text rỗng/nhiều dòng/quá 1000 ký tự hoặc Source nhiều dòng/quá 100 ký tự → `LỖI: Nội dung ghi chú không hợp lệ.`
  - Thiếu file/mục → `LỖI: Không tìm thấy skill hoặc mục Lỗi hay gặp.`; file đang sửa → `LỖI: File skill đang có thay đổi chưa commit.`; commit đúng file, thất bại báo lỗi, không push.
- **Test nghiệm thu:** CRLF/LF, BOM, Unicode, mục giữa/cuối, trùng khác Source, `..`/đường dẫn tuyệt đối/DOS names/liên kết ra ngoài; repo tạm có file khác đang sửa không bị stage; Git lỗi không báo thành công.
- **Phụ thuộc:** S1-04
### S1-07 · Nối nghiệm thu, IMP và hàng chờ Map
- **Agent:** A
- **Ngân sách:** ~140 dòng
- **Phạm vi:** `CLAUDE.md` · `templates/improvement.md` · `templates/task.md` · `plans/R-010/map-skill-pending.md`
- **Làm:**
  - Nghiệm thu tự có dòng đọc 10-nghiem-thu và skill stack tồn tại; dùng helper S1-04, không yêu cầu quản lý tự ghép đường dẫn.
  - Mẫu IMP/đánh giá nhắc chọn skill đúng bước, gọi `skill-note` với nguồn task/IMP cho lỗi TK/AG/CK mới; lỗi trùng không ghi lại.
  - Ghi hàng chờ 11 nghiệp vụ Map → 11 ID skill và tiêu chí liên kết đọc an toàn; không tạo trang Map trước R-009.
- **Test nghiệm thu:** sinh mẫu có hướng dẫn và lời đọc skill đúng stack/thiếu file; đủ 11 ánh xạ; kiểm hồi quy không sửa hợp đồng Hub, không thay quyền duyệt/publish.
- **Phụ thuộc:** S1-04, S1-06
## Câu hỏi cho quản lý
—

## Quyết định của quản lý (Claude, 25/10 12:40)
- ĐÃ DUYỆT 7 task. Ưu tiên cao nhất (chủ dự án: skill + hệ thống chạy trước, sửa giao diện sau).
- S1-01, S1-02, S1-03 (nội dung skill): CODEX viết (assign -Dev codex) — test nghiệm thu chỉ kiểm cấu trúc (đủ file, đủ mục bắt buộc, ≤ 150 dòng, có nguồn), không kiểm văn phong.
- S1-08 (thêm 25/10 13:30): Codex bổ sung quy ước thật vào skill code — ĐÃ DUYỆT.
- S1-04, S1-06 sửa team.ps1 ⇒ chạy SAU V8-01 (đang code), tuần tự. S1-05 (core) chạy song song được. S1-07 cuối cùng.

### S1-08 · Bổ sung quy ước thật vào 4 skill code
- **Agent:** C (Codex viết, assign -Dev codex)
- **Ngân sách:** ~200 dòng
- **Phạm vi:** `skills/code-winforms/SKILL.md` · `skills/code-web/SKILL.md` · `skills/code-app/SKILL.md` · `skills/code-test/SKILL.md`
- **Làm:**
  - Đọc code THẬT (chỉ đọc, tối đa ~15 file mỗi dự án): HDVTool + ChinaAndroidToolbox (WinForms: form chính, 2 UserControl tiêu biểu, helper UiThread / LayoutChecker / UiSnapshot, 2 test giao diện), AITeam Hub (Razor Pages + HTMX: Office, Approvals, 1 dịch vụ, test dựng trang), AITeam core CLI.
  - Thêm mục "Quy ước của dự án" vào từng skill code: tên helper / lớp thật, cách test thật, lỗi đã gặp (task / IMP) — ghi `file:dòng`. Giữ ≤ 150 dòng / skill; bỏ câu "chưa đọc code HDVTool".
- **Test nghiệm thu:** không cần test mới (chỉ tài liệu); cổng KT build + test hiện có.
- **Phụ thuộc:** —
