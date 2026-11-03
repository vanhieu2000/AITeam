# W2-02 · Luật chuẩn (rules/L-xxx.md) + bài học (lessons/LS-xxx.md) + tự chèn luật khi giao việc

- **Agent:** O
- **Nhánh:** `agent-o/W2-02-luat-chuan-va-bai-hoc`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~550 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** M

## Tóm tắt
- Không có màn hình mới (màn "Mẹo & bài học" và "Quy trình & vai trò" do Claude làm sau, UI-06 / UI-07).
- Mẹo / đề xuất cải tiến được lưu thành **bài học nháp**; Claude nâng bài học thành **luật chuẩn**.
- Luật đang hiệu lực **tự chèn vào lời giao việc** cho đúng loại việc / bước, nên AI không lặp lại lỗi cũ.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.4 (Mẹo & bài học), 4.5 (Luật chuẩn), 5.
- `core/AITeam.Core/Workflow/WorkflowConfig.cs`, `WorkflowConfigService.cs` (W2-01: `Resolve`, cách ghi file an toàn — dùng lại).
- `team.ps1` dòng 770–812 (dựng `$prompt`, `Add-SkillPrompt`) và hàm `Get-AITeamExe`.
- `core/AITeam.Cli/Program.cs` (cách thêm lệnh CLI).

## Mục tiêu
Thư mục mới `core/AITeam.Core/Lessons/`. Hợp đồng (Claude chốt — KHÔNG đổi chữ ký):
```csharp
namespace AITeam.Core.Lessons;

public enum LessonStatus { Draft, Promoted, Dropped }
public enum RuleStatus { Active, Retired }

public sealed record Lesson(
    string Id,                 // ^LS-\d{3}$, cấp tăng dần
    string Title,              // 1–120 ký tự
    string Body,               // markdown, ≤ 4000 ký tự
    string Kind,               // "meo" | "cai-tien"
    string? RequestId,         // ^R-\d{3}$
    string? TaskId,            // ^[A-Z][A-Z0-9]*-\d{2,3}[a-z0-9-]*$ (chỉ mã, vd. V10-02)
    string? Step,              // mã bước trong workflow.json, vd. "code"
    string? Ai,                // id dịch vụ AI
    LessonStatus Status, string? RuleId, DateTimeOffset CreatedAt);

public sealed record Rule(
    string Id,                 // ^L-\d{3}$
    string Title, string Body,
    IReadOnlyList<string> TaskTypes,   // id loại việc; rỗng = mọi loại
    IReadOnlyList<string> Steps,       // mã bước; rỗng = mọi bước
    IReadOnlyList<string> Ais,         // id AI; rỗng = mọi AI
    string? SourceLesson, RuleStatus Status, DateTimeOffset CreatedAt);

public interface ILessonService
{
    IReadOnlyList<Lesson> Lessons(LessonStatus? status = null);
    IReadOnlyList<Rule> Rules(bool activeOnly = false);
    Lesson AddDraft(string title, string body, string kind, string? requestId, string? taskId, string? step, string? ai);
    Rule Promote(string lessonId, string title, string body, IReadOnlyList<string> taskTypes, IReadOnlyList<string> steps, IReadOnlyList<string> ais);
    void Drop(string lessonId);                 // chỉ Draft ⇒ Dropped; khác ⇒ InvalidOperationException
    void Retire(string ruleId);
    /// Luật Active khớp (taskType ∈ TaskTypes hoặc rỗng) && (step ∈ Steps hoặc rỗng) && (ai ∈ Ais hoặc rỗng), sắp theo Id.
    IReadOnlyList<Rule> ForTask(string taskType, string step, string? ai);
    /// Khối chữ chèn vào prompt: "LUẬT CHUẨN ÁP DỤNG CHO VIỆC NÀY (bắt buộc):\n- [L-001] Tiêu đề: thân rút gọn ≤ 300 ký tự\n…"; không có luật ⇒ "".
    string PromptSection(string taskType, string step, string? ai);
}
```
- `LessonService(string aiTeamRoot)`. Mỗi bài học = `<root>/lessons/LS-xxx.md`; mỗi luật = `<root>/rules/L-xxx.md`. Định dạng file (đọc / ghi đúng thế này để người sửa tay được):
  ```
  # L-001 · Tiêu đề
  - **Trạng thái:** Active
  - **Loại việc:** tinh-nang-loi, sua-loi
  - **Bước:** code
  - **AI:** opencode
  - **Nguồn:** LS-004
  - **Ngày:** 2026-11-04T08:00:00+07:00

  Thân luật (markdown)…
  ```
  (Bài học tương tự với `Loại` / `Yêu cầu` / `Task` / `Bước` / `AI` / `Luật`.) UTF-8 có BOM. Ghi: file tạm + đổi tên + khóa, chặn reparse point (như `WorkflowConfigService`). Cấp mã mới = max hiện có + 1, giữ khóa trong lúc cấp.
- **CLI** (`core/AITeam.Cli`): `aiteam rules-for-task --root <AITeam> --task-file <đường dẫn file task> --step <mã bước> [--ai <id>]` ⇒ in `PromptSection` ra stdout (UTF-8), loại việc lấy bằng `WorkflowConfigService.Resolve(mã task từ tên file, nội dung file)`. Lỗi ⇒ stderr + mã thoát 2, stdout rỗng.
- **team.ps1** (`assign`): sau dòng `$prompt = Add-SkillPrompt …` gọi CLI trên với `--step code --ai <$Dev>`; có chữ thì nối vào `$prompt`. CLI lỗi / không có exe ⇒ ghi 1 dòng cảnh báo `Write-Warning` và giao tiếp bình thường (KHÔNG chặn giao việc).
- **`skill-note` dùng chung**: `team.ps1 skill-note` sau khi ghi vào skill như cũ thì gọi thêm `aiteam lesson-add --root … --title <80 ký tự đầu của Text> --body <Text> --kind meo --task <Source>` (CLI mới, in Id ra stdout). Lỗi CLI chỉ cảnh báo.
- DI trong `hub/Services/HubV3Registration.cs`: `ILessonService` với `HubPaths.AITeamRoot`.
- Tạo thư mục `rules/` và `lessons/` trong repo, mỗi thư mục có `README.md` 3–5 dòng giải thích định dạng.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Lessons/` (mới)
- `core/AITeam.Cli/` (thêm 2 lệnh)
- `team.ps1` (chỉ 2 chỗ nêu trên + hàm trợ giúp nhỏ)
- `rules/README.md`, `lessons/README.md` (mới)
- `hub/Services/HubV3Registration.cs`
- `core.tests/W202LessonServiceTests.cs` (mới)
- `tasks/W2-02-luat-chuan-va-bai-hoc.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không sửa `config/workflow.json`, `WorkflowConfigService` (chỉ gọi).
- Không đổi cách `skill-note` ghi vào skill (chỉ thêm bước ghi bài học).

## Ràng buộc
- Chữ trong luật / bài học là dữ liệu do người / AI viết ⇒ khi chèn vào prompt: bỏ ký tự điều khiển, cắt ≤ 300 ký tự / luật, tối đa 15 luật (ghi "…còn N luật" nếu nhiều hơn).
- Mã / tên lấy từ tham số không được chứa đường dẫn (`..`, `/`, `\`); file trong `rules/` không khớp mẫu `L-\d{3}.md` bị bỏ qua (không ném lỗi). File hỏng ⇒ bỏ qua + ghi vào `Lessons()`/`Rules()` không có nó; `PromptSection` không bao giờ ném lỗi vì 1 file hỏng.
- team.ps1 lưu UTF-8 có BOM.

## Test nghiệm thu
`core.tests/W202LessonServiceTests.cs` (thư mục tạm):
- AddDraft ⇒ LS-001, file có BOM, đọc lại bằng nhau; thêm lần 2 ⇒ LS-002.
- Promote LS-001 ⇒ L-001 Active, bài học thành Promoted + RuleId; Promote lại ⇒ InvalidOperationException; Drop bài học Promoted ⇒ InvalidOperationException.
- ForTask: luật A (mọi loại, bước code), luật B (chỉ `giao-dien`), luật C (Retired) ⇒ `("tinh-nang-loi","code",null)` chỉ có A.
- PromptSection: thân 1000 ký tự ⇒ cắt ≤ 300; chuỗi có `\u001b` ⇒ bị bỏ; 20 luật ⇒ 15 + "…còn 5 luật"; không luật ⇒ "".
- File `rules/L-009.md` hỏng + `rules/x.md` ⇒ `Rules()` không ném lỗi, không chứa chúng.
- Tham số taskId `../x` ⇒ ArgumentException.
- CLI `rules-for-task` với file task có `**Loại việc:** Sửa lỗi` ⇒ stdout chứa luật của `sua-loi`.

## Tiêu chí hoàn thành
- Build 0 warning; toàn bộ test xanh (gồm test PowerShell AST có sẵn của team.ps1); giữ BOM.
- Commit: `W2-02: luật chuẩn + bài học, assign tự chèn luật theo loại việc (OpenCode)`.

## Báo cáo của agent
