# H3-03 · Hub v3: trang "Cần anh duyệt" (duyệt / từ chối trên web)

- **Agent:** B
- **Nhánh:** `agent-b/H3-03-hub-approvals`
- **Ưu tiên:** cao
- **Ngân sách:** ~280 dòng
- **Yêu cầu:** R-003
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới:** trang "Cần anh duyệt" — chủ dự án bấm **Duyệt / Từ chối** (kèm ghi chú) cho việc chờ duyệt: merge, quyết định, kế hoạch cải tiến.
- Hub chỉ ghi quyết định vào file; không tự merge hay chạy lệnh. Claude (team.ps1) đọc quyết định rồi mới làm.
- Chặn tên file lạ, không cho đổi quyết định đã ghi, không in thô nội dung (chống chèn mã).

## Đọc (CHỈ những file này)
- `hub/Models/ApprovalModels.cs` (model đã chốt — KHÔNG đổi)
- `hub/Services/ApprovalService.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Approvals.cshtml.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Approvals.cshtml` (viết theo mục Bố cục dưới đây)
- `hub.tests/AcceptanceH303Tests.cs` (test nghiệm thu — đặc tả chính xác)
- `hub/Pages/Inbox.cshtml` (chỉ để xem cách dùng class CSS có sẵn: `panel`, `hd`, `bd`, `btn pri`, `btn danger`, `pill`, `tabs`, `tab`, `empty`, `inp`)

## Bối cảnh
Thiết kế: `docs/design/HUB-V3.md` mục 2–3; mockup `docs/design/hub-v3-mockup.html` trang "Cần anh duyệt" (chỉ để hình dung, KHÔNG chép CSS của mockup).

## Bố cục giao diện (`Approvals.cshtml`)
1. Đầu trang: hộp thông báo `Message` (xanh) / `ErrorMessage` (đỏ) giống `Inbox.cshtml` dòng 7–21.
2. Đoạn chữ phụ (class `muted`): "Hub chỉ ghi quyết định; merge / lệnh hệ thống do Claude chạy bằng team.ps1. Code đổi sau khi duyệt ⇒ phải duyệt lại."
3. `div.tabs` 2 tab bằng JS nhỏ trong trang (như Inbox): "Chờ duyệt" + `span.c` số lượng `Pending.Count`; "Đã quyết" + số `Decided.Count`. Tab đầu mở sẵn.
4. Tab "Chờ duyệt": mỗi mục 1 `div.panel` (cách nhau 12px), trong `div.bd` là lưới 2 cột `grid-template-columns: 1fr 260px; gap: 14px`:
   - Cột trái:
     - dòng 1: pill loại — Merge ⇒ `pill rev` "MERGE"; Decision ⇒ `pill run` "QUYẾT ĐỊNH"; Improvement ⇒ `pill fix` "CẢI TIẾN QUY TRÌNH"; rồi `<b>@a.Title</b>`;
     - dòng 2 (class `muted`, cỡ 12px): `Id · Project · Task (nếu có) · commit 7 ký tự đầu (nếu có) · RequestedBy · RequestedAt "HH:mm dd/MM"`;
     - `@MarkdownRenderer.Render(a.Detail)`;
     - Links: mỗi link 1 dòng chữ `code` (không tạo thẻ `<a>`).
   - Cột phải: `<form method="post" asp-page-handler="Decide">` có `@Html.AntiForgeryToken()`, `<input type="hidden" name="id" value="@a.Id">`, `<textarea class="inp" name="note" rows="3" maxlength="500" placeholder="Ghi chú (tùy chọn)">`, rồi 2 nút cùng hàng:
     `<button class="btn pri" name="decision" value="approve">Duyệt</button>` và `<button class="btn danger" name="decision" value="reject">Từ chối</button>`.
   - Không có mục: `div.empty` "Không có việc nào chờ anh duyệt."
5. Tab "Đã quyết": cùng thẻ nhưng cột phải thay form bằng: pill `ok` "ĐÃ DUYỆT" hoặc `err` "ĐÃ TỪ CHỐI", dòng `DecidedAt "HH:mm dd/MM"`, ghi chú (nếu có) in bằng `@a.Note` (Razor tự mã hóa). Không có ⇒ `div.empty` "Chưa có quyết định nào."
6. Bề rộng tối thiểu 900px không bị tràn ngang; Title dài xuống dòng (`overflow-wrap:anywhere`).
7. KHÔNG dùng `Html.Raw`. Không thêm mục menu (Claude thêm khi merge).

## Mục tiêu
Điền `ApprovalService` (đọc / ghi JSON bằng `System.Text.Json` + `JsonNode`, theo chú thích khung), `ApprovalsModel` và viết `Approvals.cshtml` theo bố cục trên.

## Phạm vi (được sửa)
- `hub/Services/ApprovalService.cs`
- `hub/Pages/Approvals.cshtml.cs`
- `hub/Pages/Approvals.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `hub/Models/ApprovalModels.cs`, `Program.cs`, `_Layout.cshtml`, `hub.css`, `team.ps1`. Không tạo file trong `approvals/` thật.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH303Tests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 17 ca nghiệm thu H3-03).
- [x] `Approvals.cshtml` đúng mục Bố cục (Claude kiểm bằng cách chạy Hub khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết (gồm 17 ca H3-03) | `dotnet build AITeam.sln --no-restore` → `Build succeeded. 0 Warning(s) 0 Error(s)`; `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m` → `Passed! Core.Tests: 59/59, Hub.Tests: 176/176 (AcceptanceH303Tests: 17/17), Failed: 0, Skipped: 0` |
| `Approvals.cshtml` đúng mục Bố cục | Kiểm qua `AcceptanceH303Tests.Razor_markup_follows_layout_and_is_xss_safe` (pass) và rà soát đủ 7 mục bố cục (thông báo, chữ phụ, 2 tab, lưới 2 cột `1fr 260px`, pill loại/trạng thái, `MarkdownRenderer.Render`, không `Html.Raw`, `overflow-wrap: anywhere`). |

- **Đã làm:** Điền `ApprovalService` (`GetAll`, `Get`, `Decide` bằng `JsonNode`, ghi nguyên tử qua file tạm UTF-8 không BOM, giữ nguyên trường lạ, chặn đường dẫn/id sai mẫu và quyết định lại), `ApprovalsModel` (`OnGet`, `OnPostDecide` PRG) và giao diện `Approvals.cshtml`.
- **File đã đổi:** `hub/Services/ApprovalService.cs`, `hub/Pages/Approvals.cshtml.cs`, `hub/Pages/Approvals.cshtml`, `tasks/H3-03-hub-approvals.md`.
- **Điều chưa chắc:** Không có (giao diện web chờ Claude chạy Hub kiểm tra trực quan khi review).

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 17/17 ca nghiệm thu; 59 + 176 test; build 0 warning; ~7 phút, 1 lượt; giữ BOM đủ.
- Code: chặn id lạ (`..`, đường dẫn tuyệt đối, `.json`), không đổi quyết định đã ghi, ghi file tạm rồi đổi tên, giữ nguyên trường lạ, không in thô nội dung.
- Giao diện: đúng 7 mục Bố cục (đã đọc markup); xem trực quan sau khi Claude đăng ký DI + menu lúc merge.
- [CK] (phạm vi) cổng kiểm tra tính khung của Claude vào ngân sách ⇒ báo 363/280 (thật 255/280) — đã sửa.
- Optional: 2 tab trình duyệt bấm duyệt cùng lúc có thể ghi đè nhau (dùng cá nhân, chấp nhận).

Nhân viên: Lan (B) — 1 lượt, ~7 phút, 255/280 dòng.

## Kết quả
- **Chức năng mới:** trang "Cần anh duyệt" — anh bấm **Duyệt / Từ chối** kèm ghi chú cho việc merge, quyết định, kế hoạch cải tiến.
- `team.ps1 merge` tự dừng nếu việc còn chờ duyệt, bị từ chối, hoặc code đổi sau khi duyệt (Claude làm, đã thử 7 trường hợp).
- **Kiểm chứng:** 17 ca nghiệm thu (bảo mật, ghi file, trang); 235 test pass.
- **Cần cải tiến:** Hub chưa tự báo khi có mục mới chờ duyệt (sẽ hiện số trên menu).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-003) | — | — | — |
| 2. Ý tưởng | Claude | 5 | (chung R-003) | Tách "Hub ghi quyết định / team.ps1 làm việc thật" ⇒ an toàn, không cần quyền chạy lệnh từ web | — | Giữ |
| 3. Plan / thiết kế | Claude | 5 | ~15' | Hợp đồng JSON + lỗi từng trường hợp ghi trong khung | — | — |
| 4. Giao việc | Claude | 5 | ~10' | Bố cục 7 mục cụ thể ⇒ không phải mò | — | Giữ "bố cục giao diện" dạng danh sách đánh số |
| 5. Thực hiện code | Lan (B) | 5 | ~7' (1 lượt) | Đúng đặc tả, giữ BOM, có báo cáo đối chiếu từng mục bố cục | — | — |
| 6. Review / QC | Cổng KT + Claude | 4 | ~10' | Cổng KT báo vượt ngân sách nhầm | CK | Đã sửa (tính từ commit khung) |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |
