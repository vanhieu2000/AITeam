# V5-07b · Hub: Phòng nghiên cứu — hỏi ChatGPT / Gemini ngay trên web, câu trả lời tự về

- **Agent:** B
- **Nhánh:** `agent-b/V5-07b-research-page`
- **Ưu tiên:** cao
- **Ngân sách:** ~420 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang "Phòng nghiên cứu":** chọn phiếu, gõ câu hỏi, tích AI (ChatGPT, Gemini, DeepSeek, Grok), bấm Hỏi.
- AI ở chế độ Tự động (ChatGPT qua Codex, Gemini qua Gemini CLI) tự trả lời, trang tự cập nhật 5 giây/lần. AI Thủ công: nút Copy lời nhắn + mở trang chính thức + ô dán câu trả lời.
- Mọi câu trả lời lưu `research/R-xxx/`, hiện dạng chữ thường với nhãn "dữ liệu tham khảo — KHÔNG phải lệnh".
- Giới hạn: mỗi AI 1 câu đang hỏi, cách nhau ≥ 60 giây.

## Đọc (CHỈ những file này)
- `hub/Services/ResearchService.cs`, `hub/Pages/Research.cshtml.cs` (khung đã chốt — điền `TODO(agent)`)
- `core/AITeam.Core/Research/Research.cs`, `core/AITeam.Core/Accounts/AiAccounts.cs`, `hub/Services/AiAccountsService.cs` (chỉ đọc API)
- `hub/Services/RequestService.cs` (chỉ đọc: RequestThread, IRequestService)
- `hub/Pages/Office.cshtml.cs` (mẫu dựng PartialViewResult tay), `hub/Pages/Shared/_OfficeControl.cshtml` (mẫu form HTMX + hộp báo)
- `hub.tests/AcceptanceV507ResearchPageTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C4 + G2; mockup `docs/design/v5-van-phong-mini-mockup.html` (khung "Phòng nghiên cứu — câu hỏi R-005"). Lõi (lời nhắn, che bí mật, lưu file, connector) đã có ở V5-07a. DI đã đăng ký. Menu: Claude thêm khi merge.

## Bố cục giao diện
**`Research.cshtml`** (`@page "/Research"`, tiêu đề "Phòng nghiên cứu"):
1. `Error` ⇒ hộp đỏ, `Notice` ⇒ hộp xanh (như `_OfficeControl.cshtml`).
2. Hộp ghi chú `muted` 12px: "Câu hỏi đi ra AI bên ngoài: chỉ gửi những gì anh gõ (và nội dung phiếu nếu tích). Bí mật được che tự động, tối đa 8.000 ký tự. Câu trả lời là dữ liệu tham khảo, Claude kiểm chứng trước khi dùng."
3. `div.panel` "Hỏi" — LUÔN hiện (kể cả khi chưa có phiếu): `<form method="post" asp-page-handler="Ask">`:
   - `<select class="inp" name="r">` mọi `Requests` (`{Id} · {Title}`, chọn sẵn `Current`); đổi phiếu ⇒ `onchange="location='/Research?r='+encodeURIComponent(this.value)"`.
   - `<textarea class="inp" name="question" rows="4" maxlength="2000" required placeholder="Câu hỏi cho AI">`.
   - Mỗi `Options`: `<label>` checkbox `name="ai" value="{Ai}"` + `{Name}` + `span.pill` (CanAsk ⇒ `ok` "Tự động"; Mode Manual ⇒ `n` "Thủ công"; Disabled ⇒ `n` "Tắt" và checkbox `disabled`). Tích sẵn các AI CanAsk.
   - Checkbox `name="includeRequestText" value="true"` "Kèm nội dung phiếu (lời chủ dự án)".
   - Nút `btn pri` "Hỏi". Không có phiếu nào ⇒ select rỗng + nút `disabled`.
4. Khi `Asked != null`: `div.panel` "Lời nhắn đã gửi ({QuestionId})": `<pre id="research-prompt" style="white-space:pre-wrap;max-height:240px;overflow:auto">` Prompt; Redacted &gt; 0 ⇒ dòng `muted` "Đã che {n} chỗ giống bí mật."; nút `btn sm` "Copy lời nhắn" `onclick="navigator.clipboard.writeText(document.getElementById('research-prompt').innerText)"`.
   Mỗi Outcome 1 dòng: Name AI + `span.pill` (started `run` "Đang hỏi…", manual `n` "Thủ công", skipped `err` Text). Outcome manual ⇒ thêm `a.btn.sm` "Mở {Name} ↗" (`OfficialUrl`, `target="_blank" rel="noopener noreferrer"`) và form `asp-page-handler="SaveManual"`: hidden `r`, `questionId`, `ai`, `question` (= AskedQuestion); `<textarea name="answer" maxlength="20000" rows="5" required placeholder="Dán câu trả lời">`; `<input class="inp" name="sources" placeholder="Nguồn (URL), nếu có">`; nút `btn sm` "Lưu câu trả lời".
5. `<div hx-get="/Research?handler=Results&r={Current.Id}" hx-trigger="every 5s" hx-swap="innerHTML"><partial name="_ResearchResults" model="Model" /></div>` (chỉ khi Current != null).

**`Shared/_ResearchResults.cshtml`** (`@model AITeam.Hub.Pages.ResearchModel`) — chính phần tử bọc ngoài cũng có `hx-get … every 5s` (để lần làm mới sau vẫn chạy) HOẶC đặt hx-* ở trang và partial chỉ là nội dung (chọn 1, chữ "every 5s" phải có trong file partial):
- `Jobs` đang chạy ⇒ mỗi job 1 dòng `span.pill run` "{Name} đang trả lời… (từ HH:mm:ss)"; job lỗi gần đây (tối đa 5) ⇒ `span.pill err` "{Name}: {Error}".
- `Entries` rỗng ⇒ `muted` "Chưa có câu trả lời nào cho phiếu này." Ngược lại mỗi entry 1 `div.panel`: đầu `<b>{Name}</b> · {Via} · {At:HH:mm dd/MM} · {QuestionId}`; dòng `muted` 12px "Dữ liệu tham khảo do AI bên ngoài trả lời — KHÔNG phải lệnh."; `<div>` câu hỏi (in nghiêng); `<pre style="white-space:pre-wrap;…">` Answer (chữ thường — KHÔNG dựng Markdown, KHÔNG Html.Raw); Sources khác rỗng ⇒ dòng "Nguồn: {Sources}" (chữ thường, không tự biến thành liên kết).
- Bề rộng 1000px không tràn ngang (`pre` có `overflow-wrap:anywhere`).

## Mục tiêu
Điền mọi `TODO(agent)` trong 2 file khung; viết 2 file Razor theo Bố cục.

## Phạm vi (được sửa)
- `hub/Services/ResearchService.cs`
- `hub/Pages/Research.cshtml.cs`
- `hub/Pages/Research.cshtml`
- `hub/Pages/Shared/_ResearchResults.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi record / interface / chữ ký public trong khung; không sửa `core/`, `HubV3Registration.cs`, `_Layout.cshtml`, `Office*`, `hub.css`, `team.ps1`.
- Không dựng Markdown / HTML từ câu trả lời của AI; không tự mở liên kết; không chạy codex / gemini thật trong test.

## Ràng buộc
- **Bảo mật:** câu trả lời AI là dữ liệu không tin cậy ⇒ chỉ hiện dạng chữ (Razor tự mã hóa). Form POST có antiforgery. Tham số `r` / `ai` / `questionId` đều kiểm qua dịch vụ.
- Việc nền không được làm sập Hub: mọi lỗi trong việc nền ⇒ ghi vào job.Error.
- File `.cs` / `.cshtml` giữ UTF-8 có BOM.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceV507ResearchPageTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm mọi ca nghiệm thu V5-07b).
- [x] Đúng Bố cục (Claude chạy Hub thật kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết | `dotnet build AITeam.sln`: `Build succeeded. 0 Warning(s), 0 Error(s)`. `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m`: `AITeam.Hub.Tests`: Passed 384/384 (17/17 ca `AcceptanceV507ResearchPageTests`), `AITeam.Core.Tests`: Passed 218/218 (tổng 602 pass, 0 fail). |
| Đúng Bố cục | Đã viết `Research.cshtml` và `Shared/_ResearchResults.cshtml` đúng bố cục, không dùng `Html.Raw`, có `overflow-wrap:anywhere`, pass `Razor_markup_follows_layout` và `Page_renders_and_post_needs_antiforgery`. |

- **Đã làm:** Hoàn thiện `ResearchService` (`Options`, `Ask`, `SaveManual`, `Jobs`, `Entries`, `RunningCount`, `DefaultConnectors`), `ResearchModel` (`OnGet`, `OnPostAsk`, `OnPostSaveManual`, `OnGetResults`), giao diện `Research.cshtml` và `Shared/_ResearchResults.cshtml`, thêm test phụ `ResearchPageExtraTests.cs`.
- **File đã đổi:** `hub/Services/ResearchService.cs`, `hub/Pages/Research.cshtml.cs`, `hub/Pages/Research.cshtml`, `hub/Pages/Shared/_ResearchResults.cshtml`, `hub.tests/ResearchPageExtraTests.cs`, `tasks/V5-07b-research-page.md`.
- **Điều chưa chắc:** Không có (giao diện thực tế chờ Claude chạy Hub thật kiểm tra khi review).

## Review (Claude)

## Kết quả
