# W2-01 · Cấu hình loại việc → bước → người phụ trách / skill / luật (config/workflow.json)

- **Agent:** O
- **Nhánh:** `agent-o/W2-01-cau-hinh-loai-viec`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~350 dòng
- **Thời gian tối đa:** 35 phút
- **Độ khó:** M

## Tóm tắt
- Không có màn hình mới trong task này (màn "Quy trình & vai trò" do Claude làm sau, UI-07).
- Mỗi **loại việc** (giao diện, lõi, sửa lỗi, tài liệu, nghiên cứu, bảo mật) có danh sách bước riêng; mỗi bước ghi ai phụ trách, AI dự phòng, skill gắn kèm, luật chuẩn, giới hạn phút / vòng, có cần chủ dự án duyệt không.
- Lưu trong repo `config/workflow.json` để 2 máy dùng chung qua git.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.5, 5.
- `hub/Services/WorkflowRolesService.cs` (cách đọc / ghi JSON an toàn có revision, khóa, chặn reparse point — dùng lại cách làm này).
- `skills/` (chỉ liệt kê tên thư mục).

## Mục tiêu
Thư mục mới `core/AITeam.Core/Workflow/`. Hợp đồng (Claude chốt, giao diện gọi đúng chữ ký — KHÔNG đổi):
```csharp
namespace AITeam.Core.Workflow;

public sealed record WorkflowStep(
    string Code,                    // ^[a-z][a-z0-9_]{1,31}$, duy nhất trong loại việc
    string Name,                    // tiếng Việt, 1–40 ký tự
    string Owner,                   // "claude" | "owner" | "gate" | id dịch vụ AI (^[a-z0-9][a-z0-9-]{1,31}$)
    string? Fallback,               // như Owner, khác Owner, có thể null
    IReadOnlyList<string> Skills,   // tên thư mục trong skills/ — phải tồn tại
    IReadOnlyList<string> Rules,    // mã luật ^L-\d{3}$ (chưa kiểm tồn tại: W2-02 làm)
    int? MaxMinutes,                // 1–240
    int? MaxRounds,                 // 1–5
    bool OwnerApproval);

public sealed record TaskType(
    string Id,                      // ^[a-z][a-z0-9-]{1,31}$
    string Name,
    IReadOnlyList<string> CodePrefixes,   // vd. ["H"] — dùng suy loại việc từ mã task khi file task không ghi
    IReadOnlyList<WorkflowStep> Steps);

public sealed record WorkflowConfig(int Version, IReadOnlyList<TaskType> TaskTypes, string Revision);  // Revision = SHA-256 nội dung file (rỗng nếu dùng mặc định)
public sealed record WorkflowValidation(bool Ok, IReadOnlyList<string> Errors);    // lỗi tiếng Việt, ghi rõ loại việc / bước

public interface IWorkflowConfigService
{
    WorkflowConfig Load();                                            // không có file ⇒ cấu hình mặc định (6 loại), không ghi file
    WorkflowValidation Validate(WorkflowConfig config);
    WorkflowConfig Save(WorkflowConfig config, string expectedRevision); // Revision lệch ⇒ InvalidOperationException("Cấu hình đã bị sửa ở nơi khác…"); Validate lỗi ⇒ ArgumentException
    TaskType Resolve(string taskId, string? taskText);               // dòng "- **Loại việc:** <Tên hoặc id>" trong file task; không có ⇒ theo CodePrefixes; không khớp ⇒ loại "chua-phan-loai" (Steps = của "tinh-nang-loi")
    IReadOnlyList<string> KnownSkills();                             // tên thư mục trong skills/, sắp xếp
}
```
- `WorkflowConfigService(string aiTeamRoot)`; file `<root>/config/workflow.json` (UTF-8 có BOM, thụt lề 2, camelCase). Ghi: file tạm + đổi tên, khóa, chặn reparse point (như `WorkflowRolesService`).
- **Cấu hình mặc định** (6 loại, bước theo `docs/design/R-014-van-phong-ai.md` mục 4.5):
  - `giao-dien` "Tính năng có giao diện", tiền tố `H`: tiep_nhan(claude) → thiet_ke(claude) → mockup_duyet(owner, OwnerApproval) → chuan_bi(codex) → code_loi(codex, fallback opencode) → giao_dien(claude) → cong_kt(gate) → qc(claude) → nghiem_thu(claude).
  - `tinh-nang-loi` "Tính năng lõi / CLI", tiền tố `V`,`O`: tiep_nhan → ke_hoach(claude) → chuan_bi(codex) → code(codex, fallback opencode) → cong_kt(gate) → qc(claude) → nghiem_thu(claude).
  - `sua-loi` "Sửa lỗi": tiep_nhan → chan_doan(claude) → code(opencode, fallback codex) → cong_kt → qc → nghiem_thu.
  - `tai-lieu` "Tài liệu / dữ liệu", tiền tố `D`: tiep_nhan → code(opencode) → qc(claude).
  - `nghien-cuu` "Nghiên cứu (chỉ đọc)", tiền tố `R`: tiep_nhan → nghien_cuu(codex, fallback opencode) → tong_hop(claude).
  - `bao-mat` "Bảo mật": như `tinh-nang-loi` + bước `duyet_bao_mat`(owner, OwnerApproval) trước `nghiem_thu`.
  - Skill gắn sẵn theo bước: tiep_nhan→`01-tiep-nhan`, ke_hoach→`04-ke-hoach`, chuan_bi→`06-chuan-bi-task`, code/code_loi→`07-code`,`code-test`, cong_kt→`08-kiem-thu`, qc→`09-qc`, nghiem_thu→`10-nghiem-thu`, nghien_cuu→`03-research`; giao_dien→`code-web`.
  - Giới hạn mặc định: code 30 phút / 3 vòng; chuan_bi 25 phút.
- **Validate**: id / code trùng hoặc sai mẫu; Owner / Fallback sai mẫu; Fallback = Owner; skill không có trong `skills/`; MaxMinutes / MaxRounds ngoài khoảng; loại việc không có bước; Name rỗng / quá dài; CodePrefixes trùng giữa 2 loại; tối đa 30 loại việc, 20 bước / loại.
- DI trong `hub/Services/HubV3Registration.cs`: `IWorkflowConfigService` với `HubPaths.AITeamRoot`.
- Ghi file mặc định vào repo: tạo `config/workflow.json` bằng chính `Save(Load(), "")` trong test? KHÔNG — agent tạo file `config/workflow.json` bằng tay đúng cấu hình mặc định (để chủ dự án đọc / sửa được) và test kiểm `Load()` file này hợp lệ.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Workflow/` (mới)
- `config/workflow.json` (mới)
- `hub/Services/HubV3Registration.cs`
- `core.tests/W201WorkflowConfigTests.cs` (mới)
- `tasks/W2-01-cau-hinh-loai-viec.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không sửa `WorkflowRolesService` / trang Map (giữ chạy song song tới khi UI-07 thay).
- Không đổi `team.ps1` (W2-02 sẽ dùng cấu hình này khi giao việc).

## Ràng buộc
- File do người sửa tay có thể hỏng ⇒ `Load()` ném `InvalidDataException` với thông báo tiếng Việt chỉ rõ chỗ hỏng; KHÔNG âm thầm dùng mặc định khi file có mà hỏng.
- Không nhận đường dẫn trong tên skill (`..`, `/`, `\`).

## Test nghiệm thu
`core.tests/W201WorkflowConfigTests.cs` (thư mục tạm, có `skills/` giả):
- Không có file ⇒ 6 loại mặc định, Revision rỗng, Validate OK (với skills giả đủ tên).
- `config/workflow.json` trong repo (đọc file thật qua đường dẫn repo) ⇒ Load + Validate OK.
- Save rồi Load ⇒ bằng nhau; Revision đổi; Save với revision cũ ⇒ InvalidOperationException.
- Validate bắt: code trùng, skill không tồn tại, skill `../x`, Fallback = Owner, MaxRounds 9, prefix trùng.
- Resolve: dòng `**Loại việc:** Bảo mật` ⇒ `bao-mat`; không có dòng, mã `H9-03` ⇒ `giao-dien`; mã `ZZ-1` ⇒ `chua-phan-loai`.
- File JSON hỏng ⇒ InvalidDataException.

## Tiêu chí hoàn thành
- Build 0 warning; toàn bộ test xanh; giữ BOM.
- Commit: `W2-01: cấu hình loại việc, bước, skill, luật trong config/workflow.json (OpenCode)`.

## Báo cáo của agent

## Review (Claude)
