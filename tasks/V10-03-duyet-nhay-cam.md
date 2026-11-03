# V10-03 · Duyệt chỉ việc nhạy cảm + nhật ký "Claude tự quyết"

- **Agent:** C
- **Nhánh:** `agent-c/V10-03-duyet-nhay-cam`
- **Yêu cầu:** R-014
- **Loại việc:** Bảo mật
- **Ngân sách:** ~400 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** L

## Tóm tắt
- Không có màn hình mới trong task này (giao diện "Cần anh duyệt" do Claude làm sau, UI-04).
- Chủ dự án chỉ phải duyệt 5 loại: **bí mật**, **bảo mật**, **dữ liệu nhạy cảm**, **hành động ra ngoài**, **quyết định sản phẩm**. Merge / QC / cải tiến do Claude tự quyết và ghi nhật ký để chủ dự án xem lại, có thể "mở lại".
- Cổng kiểm tra tự tạo mục duyệt **bảo mật** khi diff chạm file nhạy cảm; `merge` bị chặn tới khi chủ dự án đồng ý.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 1, 4.2.
- `hub/Models/ApprovalModels.cs`, `hub/Services/ApprovalService.cs`.
- `team.ps1`: `Invoke-ApprovalRequest`, `Assert-MergeApproval`, `Get-Approvals`, `Invoke-Merge`, `Invoke-Verify` (chỉ đoạn cần chèn), khối `param(...)`, bảng lệnh `switch ($Command)`.
- `hub.tests/` file test hiện có của Approval (tìm bằng tên `*Approval*Tests.cs`) — để biết mẫu test và KHÔNG sửa assert cũ.

## Mục tiêu
### C# (Hub, ngoài Pages)
```csharp
// hub/Models/ApprovalModels.cs — mở rộng, giữ tương thích file cũ
public enum ApprovalKind { Merge, Decision, Improvement, Secret, Security, SensitiveData, External }
// JSON: merge | decision | improvement | secret | security | sensitive-data | external
public enum ApprovalRisk { Low, Medium, High }          // JSON: low | medium | high; thiếu ⇒ Medium
// Approval: THÊM thuộc tính init (không đổi tham số vị trí của record):
//   public ApprovalRisk Risk { get; init; } = ApprovalRisk.Medium;
//   public string? SecretName { get; init; }            // chỉ TÊN khóa trong két, ^[A-Z][A-Z0-9_]{1,63}$, sai ⇒ null
//   public IReadOnlyList<string> Files { get; init; } = []; // file nhạy cảm cổng KT phát hiện

public static class ApprovalPolicy
{
    public static bool RequiresOwner(ApprovalKind kind);   // true: Secret, Security, SensitiveData, External, Decision
}

// IApprovalService THÊM:
//   IReadOnlyList<Approval> OwnerPending();   // Pending && RequiresOwner — huy hiệu menu + trang Cần anh duyệt dùng hàm này

public sealed record ClaudeDecision(
    string Id, DateTimeOffset At, string Kind, string Title,
    string? Project, string? Task, string? Request, string? Detail,
    bool Reopened, string? ReopenNote, DateTimeOffset? ReopenedAt);

public interface IClaudeDecisionLog
{
    IReadOnlyList<ClaudeDecision> List(int max = 200);      // mới nhất trước
    ClaudeDecision Reopen(string id, string note);          // id sai mẫu / không có ⇒ ArgumentException / KeyNotFoundException; note rỗng ⇒ ArgumentException
}
```
- Nhật ký: `approvals/claude-decisions.jsonl`, chỉ thêm dòng (append-only). Dòng quyết định: `{"type":"decision","id":"D-20261103214500-ab12","at":…,"kind":"merge|qc|improvement|rule|other","title":…,"project":…,"task":…,"request":…,"detail":…}`. Mở lại = dòng mới `{"type":"reopen","ref":"<id>","note":…,"at":…}`. `List` gộp hai loại. Dòng hỏng ⇒ bỏ qua. Id: `^D-\d{14}-[a-z0-9]{4}$`.
- Ghi file: khóa + ghi nối an toàn (như `ApprovalService` đang làm), chặn reparse point.
- DI trong `hub/Services/HubV3Registration.cs`: `IClaudeDecisionLog`.
- Huy hiệu menu hiện đang đếm mọi mục Pending — KHÔNG sửa `_Layout` (Claude làm); chỉ cần `OwnerPending()` đúng.

### team.ps1
1. `approval-request -Kind` nhận thêm `secret | security | sensitive-data | external`; tham số mới `-Risk low|medium|high` (mặc định `medium`); kind `secret` bắt buộc `-Name <TÊN_KHÓA>` (khớp mẫu trên) ⇒ ghi `secretName`. Không bao giờ nhận / ghi giá trị khóa.
2. Hàm thuần `Get-SensitiveChanges([string[]]$Files, $cfg)` ⇒ danh sách file nhạy cảm. Mẫu mặc định (ghi đè được bằng `team.json` `approvals.sensitivePatterns`): `(?i)(^|/)(auth|secret|vault|credential|password|crypto|token)[^/]*\.(cs|ps1|cshtml|json)$`, `(?i)(^|/)\.env`, `(?i)(^|/)Migrations/`, `(?i)SecretRedactor\.cs$`, `(?i)tools/secret-scan\.ps1$`, `(?i)(^|/)vault`.
3. Trong `Invoke-Verify` (sau khi có diff nhánh): file nhạy cảm ≠ rỗng và CHƯA có mục `security` cho (task, commit HEAD) ⇒ tạo mục `security`, `risk=high` nếu chạm `.env`/vault/secret, ngược lại `medium`, `files=[…]`, `detail` liệt kê file. Ghi dòng "Cổng KT: tạo mục duyệt bảo mật A-xxx" trong báo cáo verify. Không tạo trùng.
4. `Assert-MergeApproval`: mục `merge` cũ KHÔNG còn bắt buộc / chặn (bỏ qua `requireMerge`, giữ đọc để không lỗi). CHẶN merge nếu task có mục loại `secret|security|sensitive-data|external` đang `pending` hoặc `rejected`; mục `approved` cho `security` phải đúng commit HEAD (như cũ).
5. Hàm `Add-ClaudeDecision -Kind -Title [-Task] [-Request] [-Detail]` ghi dòng vào `approvals/claude-decisions.jsonl`; `Invoke-Merge` gọi sau khi merge thành công (kind `merge`, kèm mã yêu cầu đọc từ file task). Lệnh mới `team.ps1 claude-decision -Kind qc -Title "…" -Task X` để Claude ghi tay.
6. Help đầu file cập nhật.

## Phạm vi (được sửa những gì)
- `hub/Models/ApprovalModels.cs`
- `hub/Services/ApprovalService.cs`
- `hub/Services/ClaudeDecisionLog.cs` (mới)
- `hub/Services/HubV3Registration.cs`
- `team.ps1`
- `hub.tests/V1003ApprovalPolicyTests.cs` (mới)
- `core.tests/V1003SensitiveGateTests.cs` (mới, harness PowerShell)
- `tasks/V10-03-duyet-nhay-cam.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không sửa assert test cũ; test cũ đòi "merge cần duyệt" ⇒ DỪNG, ghi tên test vào báo cáo.
- Không đọc két / bí mật.

## Ràng buộc
- File `approvals/A-*.json` cũ (kind merge, không có risk) vẫn đọc được, Risk = Medium.
- Mục `secret`: Hub và log không bao giờ chứa giá trị khóa — chỉ `secretName`.
- jsonl có thể bị sửa tay ⇒ đọc phòng thủ, giới hạn 5 MB (vượt ⇒ chỉ đọc 5 MB cuối).

## Test nghiệm thu
- `hub.tests/V1003ApprovalPolicyTests.cs`: đọc JSON có `sensitive-data`, `risk:"high"`, `secretName`; file cũ không có risk ⇒ Medium; `secretName:"bad name"` ⇒ null; `OwnerPending` loại merge / improvement, giữ 5 loại; ClaudeDecisionLog: thêm 2 quyết định, mở lại 1 ⇒ List có `Reopened=true` + note; dòng hỏng bị bỏ; Reopen id sai ⇒ ArgumentException; note rỗng ⇒ ArgumentException.
- `core.tests/V1003SensitiveGateTests.cs` (harness PowerShell theo mẫu test hiện có, thư mục tạm, không push thật): `Get-SensitiveChanges` bắt `hub/Services/VaultService.cs`, `.env.local`, `Migrations/001.sql`, bỏ qua `hub/Pages/Tasks.cshtml`; `Assert-MergeApproval` chặn khi có `security` pending, chặn khi `rejected`, cho qua khi chỉ có `merge` pending cũ; `approval-request -Kind secret` thiếu `-Name` ⇒ lỗi; `Add-ClaudeDecision` ghi đúng một dòng JSON hợp lệ.

## Tiêu chí hoàn thành
- Build 0 warning; toàn bộ test xanh; giữ BOM.
- Commit: `V10-03: duyệt chỉ việc nhạy cảm, nhật ký Claude tự quyết, cổng KT bắt file nhạy cảm (Codex)`.

## Báo cáo của agent

## Review (Claude)

## Review (Claude)
### Vòng 1 — CẦN SỬA
- Phiên OpenCode bị ngắt sau khi chạy test (hết lượt miễn phí lúc 23:36) ⇒ báo cáo agent để trống; Claude commit hộ code của OpenCode (4f570eb), không sửa code.
- Đã kiểm: build 0 warning; Hub 843/843; Core đạt (S106 chỉ hỏng khi chạy song song, chạy riêng đạt); ApprovalKind / ApprovalRisk / OwnerPending / ClaudeDecisionLog đúng hợp đồng, đọc phòng thủ, chặn reparse point, append-only, không chứa giá trị khóa; không chạm giao diện.
- **Required 1 — mục duyệt của commit cũ chặn merge mãi** (	eam.ps1 Assert-MergeApproval):
  - Vấn đề: cổng KT tạo mục security theo (task, commit HEAD). Agent sửa lại ⇒ commit mới ⇒ mục mới. Mục cũ (pending hoặc approved cho commit cũ) vẫn nằm trong $sensitive: pending ⇒ "đang chờ chủ dự án duyệt"; approved ⇒ "duyệt commit X nhưng nhánh giờ là Y ⇒ xin duyệt lại" — kể cả khi mục cho HEAD đã được duyệt.
  - Mong muốn: chỉ xét mục nhạy cảm có commit rỗng HOẶC bằng HEAD. Mục của commit khác coi là cũ (bỏ qua). Còn mục pending / rejected trong tập đó ⇒ chặn như hiện tại.
  - Test xác nhận (thêm vào core.tests/V1003SensitiveGateTests.cs): A-001 security pending commit X, A-002 security approved commit Y, HEAD = Y ⇒ merge KHÔNG bị chặn; A-003 security pending commit Y ⇒ bị chặn.
- **Required 2 — nhật ký Claude tự quyết không lên git**: Add-ClaudeDecision chỉ ghi file, không git add/commit/push ⇒ máy kia không thấy tab "Claude đã tự quyết". Mong muốn: sau khi ghi, git -C $Root add approvals/claude-decisions.jsonl + commit "Claude tự quyết: <title>" + push (lỗi git chỉ cảnh báo, như Save-ApprovalFile).
- **Required 3 — mất BOM**: core.tests/V1003SensitiveGateTests.cs, hub.tests/V1003ApprovalPolicyTests.cs, hub/Services/ClaudeDecisionLog.cs không có BOM UTF-8.
- Optional: -Kind secret bắt -Name chữ HOA trong khi két nhận cả chữ thường; mẫu 	oken bắt nhầm file kiểu TokenSummary.cs (chỉ thêm 1 mục duyệt thừa).
- Lỗi: [AG] (BOM) 3 file mới thiếu BOM · [AG] (chất lượng) mục duyệt theo commit cũ chặn merge · [TK] (chất lượng) task chưa nói rõ xử lý mục của commit cũ.