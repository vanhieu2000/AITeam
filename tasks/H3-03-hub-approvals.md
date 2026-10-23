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
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 17 ca nghiệm thu H3-03).
- [ ] `Approvals.cshtml` đúng mục Bố cục (Claude kiểm bằng cách chạy Hub khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| | |

- **Đã làm:**
- **File đã đổi:**
- **Điều chưa chắc:**

## Review (Claude)

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Lan (B) | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
