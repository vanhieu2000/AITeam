# R-015 · Thiết kế chức năng (giao AI làm lõi, Claude làm giao diện)

Ngày 04/11/2026 · Claude. Đề xuất gốc: `docs/design/R-015-ra-soat-van-phong-ai.md` (chủ dự án duyệt 04/11).
Giao diện các trang (Dự án, Yêu cầu, Đội AI) do Claude làm sau khi lõi xong; task lõi KHÔNG sửa `hub/Pages/**`, `hub/wwwroot/**`.

| Task | Việc | Ai | Phụ thuộc |
|---|---|---|---|
| F1-06 | Bộ đọc bảng việc dùng chung, chịu dòng trống / thiếu `\|` | Codex | — |
| F1-02 | Tiến độ yêu cầu (đếm task đúng), đóng / hủy phiếu | Codex | F1-06 |
| F1-01 | Quản lý dự án: thêm / sửa / trạng thái | Codex | — |
| F1-05 | Nhập bài học có sẵn vào kho bài học | Antigravity | — |
| F1-03 | Đội AI: kiểm đăng nhập + hạn mức thật, bỏ dòng trùng | Antigravity | — |
| ~~F4~~ | "Claude tự quyết" = 0: KHÔNG phải thiếu code. `team.ps1 merge` đã ghi `approvals/claude-decisions.jsonl` nhưng Claude merge tay bằng `git merge` ⇒ sửa quy trình (CLAUDE.md), không giao task. | — | — |

## F1-06 · Bộ đọc bảng việc dùng chung
**Vấn đề (04/11):** 1 dòng trống giữa bảng `tasks/README.md` làm Hub mất 38 task. Có 4 bộ đọc bảng riêng (`TaskBoardService`, `WorkOverviewService`, `PerformanceService.ReadStatuses`, `WorkProgressService`); 2 bộ lấy trạng thái ở `cells[^2]` nên dòng thiếu `|` cuối đọc nhầm cột.

**Hợp đồng** — `hub/Services/BoardTable.cs`:
```csharp
public sealed record BoardRow(string Id, string Title, string File, string Agent, string Branch, string Status, int Line);
public static class BoardTable { public static IReadOnlyList<BoardRow> Parse(string? markdown); }
```
- Bảng = dòng tiêu đề đầu tiên (bắt đầu `|`) có ô `Task` (không phân biệt hoa thường) mà dòng KẾ TIẾP là dòng kẻ (`|---|…`, chỉ gồm `-`, `:`, khoảng trắng, `|`). Không có ⇒ rỗng.
- Dòng dữ liệu: sau dòng kẻ; **dòng trống bỏ qua, đọc tiếp**; dừng ở dòng không trống đầu tiên không bắt đầu bằng `|` (chữ thường, `## …`). Bảng ở mục sau KHÔNG đọc.
- Ô: cắt theo `|`; dấu `|` cuối dòng có thể thiếu. Trim mọi ô. Cột tìm theo tên tiêu đề: `Task`; `Agent`; chứa `Nhánh`/`Branch`; chứa `Trạng thái`/`Status`. Thiếu cột ⇒ chuỗi rỗng. Thiếu ô ⇒ chuỗi rỗng.
- Ô Task: dạng link `[ID Tiêu đề](file.md)` ⇒ File = tên file trong link (`Path.GetFileName`); dạng chữ thường `ID · Tiêu đề` ⇒ File = "". Id = khớp `^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9]*` ở đầu chữ; Title = phần còn lại bỏ các ký tự phân cách đầu (`·`, `-`, `:`, `–`, `—`, khoảng trắng). Không khớp Id ⇒ bỏ dòng.
- Nhánh: bỏ dấu `` ` ``. Trạng thái: giữ nguyên chữ (đã Trim). Line = số dòng (bắt đầu 1) trong markdown.
- Id trùng ⇒ giữ dòng đầu. Không bao giờ ném lỗi với nội dung hỏng.

**Dùng chung:** 4 dịch vụ trên chuyển sang `BoardTable.Parse` (giữ nguyên kết quả cũ cho bảng chuẩn: thứ tự task, `TaskState` theo emoji 🟡🔵🔴✅, `OfficeTaskStatus` qua `WorkOverviewService.ParseStatus(row.Status, …)`).

## F1-02 · Tiến độ yêu cầu
**Vấn đề:** Văn phòng hiện "0/0" cho mọi phiếu vì chỉ đếm bảng "## 2. Chia việc" trong phiếu (hầu như không điền). Task thật gắn phiếu bằng dòng `- **Yêu cầu:** R-xxx` trong file task. Không có cách đóng / hủy phiếu ⇒ 7 phiếu cũ treo "Đang làm".

**Hợp đồng** — `hub/Services/RequestProgressService.cs`:
```csharp
public enum RequestState { Open, Done, Cancelled }
public sealed record RequestTaskStatus(string Project, string Code, string Title, OfficeTaskStatus Status);
public sealed record RequestProgress(string Id, RequestState State, int Total, int Merged, int Running, int Review, int NeedsFix,
    bool AllMerged, IReadOnlyList<RequestTaskStatus> Tasks);
public sealed record RequestStateResult(bool Ok, string? Error);
public interface IRequestProgressService
{
    IReadOnlyList<RequestProgress> GetAll();          // theo IRequestService.GetAll(), cùng thứ tự
    RequestProgress? Get(string id);                  // id sai dạng ^R-\d{3}$ ⇒ null
    RequestStateResult SetState(string id, RequestState state, string? note);
}
```
- **Task của phiếu** = mọi `<dự án>/tasks/*.md` (trừ README.md) của mọi dự án có `ExistsLocally`, có dòng khớp `^- \*\*Yêu cầu:\*\*\s*(R-\d{3})\s*$` đúng id (CLAUDE.md bắt buộc dòng này). Code = Id lấy từ tên file (cùng quy tắc Id F1-06); Title = dòng `# …` đầu file bỏ phần `Code ·`. KHÔNG dùng bảng "Chia việc" của phiếu (ô kiểu "UI-01..09", "V10-01, V10-04" không đếm được). Sắp theo dự án rồi Code.
- Trạng thái task: dòng bảng việc (`BoardTable.Parse` của dự án) ⇒ `WorkOverviewService.ParseStatus(row.Status, fallback)` với fallback theo emoji của chữ trạng thái (🟡 Assigned · 🔵 Review · 🔴 NeedsFix · ✅ Merged · khác Unknown) — bảng ghi "🟡 Đang chạy · máy …" phải ra Running; không có trên bảng ⇒ `Planned`.
- Đếm: Merged = Merged; Running = Running + Gate; Review = Review; NeedsFix = NeedsFix + Blocked; Cancelled không tính vào Total. AllMerged = Total > 0 && Merged == Total.
- State từ dòng `- **Trạng thái:**`: chứa `✅` ⇒ Done; chứa `❌` hoặc "Hủy" (không phân biệt hoa thường) ⇒ Cancelled; còn lại Open.
- **SetState** ghi đúng dòng `- **Trạng thái:** …` của file phiếu (dòng khác giữ nguyên từng byte, giữ BOM nếu có, giữ kiểu xuống dòng):
  - Done ⇒ `✅ Xong HH:mm dd/MM/yyyy`; Cancelled ⇒ `❌ Hủy HH:mm dd/MM/yyyy`; Open ⇒ `🟡 Đang làm`. Có note ⇒ thêm ` — <note>`.
  - note: thay ký tự điều khiển / xuống dòng bằng khoảng trắng, gộp khoảng trắng liên tiếp thành 1, Trim, cắt tối đa 200 ký tự; rỗng ⇒ không thêm.
  - Thêm 1 dòng vào cuối bảng "## 3. Nhật ký" (nếu có bảng): `| HH:mm dd/MM | Đổi trạng thái phiếu: <Xong/Hủy/Mở lại> (Hub) | <note> |` — ký tự `|` trong note thay bằng `/`.
  - id sai dạng / không có phiếu / không có dòng Trạng thái ⇒ `Ok=false`, Error tiếng Việt cố định, KHÔNG ghi. Ghi nguyên tử (file tạm cùng thư mục rồi `File.Replace`/`Move`), chặn reparse point ở file và thư mục `requests`.
  - Giờ lấy từ `TimeProvider` (constructor), để test được.

## F1-01 · Quản lý dự án
Nguồn: `projects.json` (chung 2 máy) + `team.local.json` → `paths` (riêng máy). Đường dẫn trên máy KHÔNG BAO GIỜ ghi vào `projects.json`.

**Trường mới trong `projects.json`** (tùy chọn, thiếu ⇒ mặc định): `description` (""), `status` ("active" | "paused" | "archived", mặc định "active"), `branch` ("main"), `try` (""), `defaultAi` ("" | "codex" | "opencode" | "antigravity" | "claude").

**Hợp đồng** — `hub/Services/ProjectAdminService.cs`:
```csharp
public enum ProjectStatus { Active, Paused, Archived }
public enum ProjectAddMode { New, GitHub, Folder }
public sealed record ProjectProfile(string Name, string Description, ProjectStatus Status, string Branch, string Repo,
    string LocalPath, bool ExistsLocally, string Stack, string Build, string Test, string Try, string DefaultAi,
    IReadOnlyDictionary<string, string> Areas);
public sealed record ProjectEdit(string Description, string Branch, string Stack, string Build, string Test, string Try,
    string DefaultAi, IReadOnlyDictionary<string, string> Areas, string LocalPath);
public sealed record ProjectAdd(string Name, ProjectAddMode Mode, string? Repo, string? LocalPath);
public sealed record ProjectResult(bool Ok, string? Error);
public interface IProjectScriptRunner { (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout); }
public interface IProjectAdminService
{
    IReadOnlyList<ProjectProfile> List();
    ProjectProfile? Get(string name);
    ProjectResult Update(string name, ProjectEdit edit);
    ProjectResult SetStatus(string name, ProjectStatus status);
    ProjectResult Add(ProjectAdd add);
}
```
- **List / Get:** theo thứ tự `projects.json`; Get không phân biệt hoa thường. `LocalPath` / `ExistsLocally` = đường dẫn đã giải của `TeamConfigService` (`ProjectInfo.Path`). `status` lạ / thiếu ⇒ Active; `branch` thiếu ⇒ "main".
- **Kiểm tra (sai ⇒ `Ok=false` + lỗi tiếng Việt, không ghi gì):** tên `^[A-Za-z][A-Za-z0-9._-]{0,63}$`; repo `^[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$`; đường dẫn: tuyệt đối (`Path.IsPathFullyQualified`), không có đoạn `..`, ≤ 240 ký tự, không ký tự điều khiển; lệnh build/test/try, stack, nhánh: 1 dòng, ≤ 300 ký tự, không ký tự điều khiển; nhánh `^[A-Za-z0-9._/-]{1,100}$`; mô tả ≤ 200 ký tự 1 dòng; defaultAi trong danh sách; khóa vùng `^[A-Z]$`, giá trị ≤ 300 ký tự 1 dòng.
- **Update:** dự án phải có. Ghi các trường vào phần tử của dự án trong `projects.json`, GIỮ nguyên trường lạ và các dự án khác, giữ `path`/`workDir` cũ y nguyên. `LocalPath` khác rỗng ⇒ ghi `team.local.json` → `paths.<Tên>` (giữ mọi khóa khác). Ghi nguyên tử + `.bak` (dùng lại cách ghi của `TeamConfigService`).
- **SetStatus:** chỉ đổi `status`. Không có xóa dự án (Lưu trữ = ẩn khỏi giao việc, repo còn nguyên).
- **Add:** tên chưa có. New: không cần repo/path. GitHub: bắt buộc Repo. Folder: bắt buộc LocalPath. Gọi runner với danh sách đối số (KHÔNG ghép chuỗi lệnh): `add-project -Project <Tên>` + `-New` | `-Repo <repo>` | `-Path <đường dẫn>`. Runner thật chạy `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <AITeamRoot>\team.ps1 …` bằng `ProcessStartInfo.ArgumentList`, stdin rỗng, thời hạn 10 phút. Mã thoát ≠ 0 ⇒ `Ok=false`, Error = "Thêm dự án thất bại: " + dòng cuối không rỗng của output (≤ 300 ký tự).

## F1-05 · Nhập bài học có sẵn
**Vấn đề:** kho bài học (`lessons/`) có 0 mục trong khi `CLAUDE.md` mục "## Bài học vận hành" và `AGENTS.md` mục "## Lỗi hay gặp…" có ~40 bài học.

**Hợp đồng** — `core/AITeam.Core/Lessons/LessonImporter.cs`:
```csharp
public sealed record ImportedLesson(string Title, string Body, string Source);
public sealed record LessonImportResult(int Added, int Skipped, IReadOnlyList<string> Warnings);
public static class LessonImporter
{
    public static IReadOnlyList<ImportedLesson> Extract(string markdown, string source);
    public static LessonImportResult Import(ILessonService lessons, IEnumerable<ImportedLesson> items);
}
```
- Extract: lấy mọi mục `## …` có tiêu đề bắt đầu "Bài học" hoặc "Lỗi hay gặp" (không phân biệt hoa thường); mỗi gạch đầu dòng cấp 1 (`- …`, kể cả các dòng con thụt lề nối theo) là 1 bài học. Body = nguyên văn gạch đầu dòng (bỏ `- ` đầu), Title: bỏ `**` trước, rồi lấy tới dấu phân cách đầu tiên: `.` hoặc `:` đứng trước khoảng trắng / cuối dòng ("5.1", "`.ps1`" KHÔNG phải phân cách), `→`, `⇒`, ` — `; Trim, cắt ≤ 120 ký tự. Dòng con thụt lề (bắt đầu bằng khoảng trắng, không phải `- ` cấp 1) nối vào Body của gạch đầu dòng trước. Source = tham số (vd. "CLAUDE.md").
- Import: bỏ qua bài đã có (so Title đã chuẩn hóa: bỏ khoảng trắng thừa, không phân biệt hoa thường với mọi bài học hiện có, mọi trạng thái); còn lại `AddDraft(title, body + "\n\nNguồn: " + source, "meo", null, null, null, null)`. Body > 4000 ⇒ cắt 4000. Chạy 2 lần ⇒ lần 2 Added = 0.
- Lệnh `team.ps1 lessons-import` gọi `aiteam lessons-import` (hoặc tương đương trong CLI hiện có) cho `CLAUDE.md`, `AGENTS.md` của AITeam; in "Đã thêm N, bỏ qua M".

## F1-03 · Đội AI
Bổ sung sau khi Claude đọc xong `AiServices` / `AiAccounts` (thiết kế riêng mục này, giao Antigravity).
