# V10-05 · Trợ lý chat (IChatAssistant): hỏi đáp chọn AI, giao việc, nghiên cứu nhiều AI

- **Agent:** B
- **Model:** `gemini-3.8-flash-high`
- **Nhánh:** `agent-o/V10-05-tro-ly-chat`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~450 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** M
- **Phụ thuộc:** V10-02 đã merge (IAiProviderFactory) — có trên main.

## Tóm tắt
- Không có màn hình mới (khung chat nổi đã có; Claude nối vào sau, UI-08 / UI-09).
- Lõi cho nút chat nổi: chọn AI để **hỏi đáp**, **giao việc** (luôn qua Claude, vào hộp thư), **nghiên cứu** (hỏi nhiều AI cùng lúc, gộp câu trả lời).
- Cài đặt trợ lý (AI mặc định, chế độ mặc định, danh sách AI nghiên cứu, có lưu lịch sử không) lưu cục bộ trong `office.db`.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.5b, 8.
- `core/AITeam.Core/AiServices/AiServiceModels.cs`, `core/AITeam.Core/AiServices/Adapters/` (V10-02).
- `hub/Services/LocalChatClient.cs`, `hub/Services/LocalChatStore.cs` (lịch sử chat hiện có `chat.db`).
- `hub/Services/OfficeChatService.cs` (gửi vào hộp thư cho Claude).

## Mục tiêu
**Khung code đã có trên nhánh** (commit "Test nghiệm thu V10-05 (Claude)"): `hub/Services/ChatAssistant.cs` (chữ ký chốt, thân `NotImplementedException`). Agent điền thân, KHÔNG đổi chữ ký. Hợp đồng:
```csharp
namespace AITeam.Hub.Services;

public enum ChatMode { Ask, Task, Research }

public sealed record AssistantSettings(string DefaultAi, ChatMode DefaultMode, IReadOnlyList<string> ResearchAis, bool KeepHistory);
public sealed record AssistantOption(string Id, string Name, bool Available, string? Reason);   // Reason tiếng Việt khi !Available
public sealed record AssistantReply(string? ConversationId, ChatMode Mode, string Text, string? Error, long? Tokens);

public interface IChatAssistant
{
    AssistantSettings Settings();                 // chưa lưu ⇒ ("codex", Ask, ["codex","opencode"], true)
    void SaveSettings(AssistantSettings settings); // DefaultAi / ResearchAis phải có trong Options(); ResearchAis 1–4 mục, không trùng ⇒ sai thì ArgumentException tiếng Việt
    IReadOnlyList<AssistantOption> Options();     // dịch vụ có vai trò Chat từ registry (cả đang tắt, Available=false, Reason "Đang tắt")
    Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct);
}
```
- `ChatAssistant(IAiServiceRegistry registry, IAiProviderFactory factory, ILocalChatClient codexChat, LocalChatStore store, IOfficeChatService office)`.
- **Settings** lưu bằng `registry.GetSetting/SetSetting`, khóa: `assistant.defaultAi`, `assistant.defaultMode` (`ask|task|research`), `assistant.researchAis` (id cách nhau dấu phẩy), `assistant.keepHistory` (`1|0`). Giá trị hỏng ⇒ dùng mặc định cho riêng khóa đó.
- **Ask**: `ai` null ⇒ DefaultAi. `ai == "codex"` ⇒ dùng `codexChat.ReplyAsync` (đường đã kiểm chứng) với `store.Settings()`. AI khác ⇒ `factory.Create(service).RunAsync` chế độ `ReadOnly`, thư mục tạm riêng (xóa sau), prompt file = lời dặn hội thoại (như `CodexChatClient`) + lịch sử JSON + câu hỏi, timeout 180 s.
- **Task**: không gọi AI. `office.Send(project, text)` (project null ⇒ ArgumentException "Chọn dự án trước khi giao việc."). Text trả về: "Đã gửi cho Claude. Claude sẽ tạo phiếu và báo trên trang Theo dõi việc.".
- **Research**: gọi song song mọi id trong `ResearchAis` (bỏ id đang tắt), mỗi AI timeout 180 s; Text = các khối `### <Tên AI>\n<trả lời hoặc "(lỗi: …)">`; Error chỉ khi TẤT CẢ hỏng; Tokens = tổng số đã biết.
- **Lịch sử** (`KeepHistory = true`, chế độ Ask / Research): `conversationId` null ⇒ `store.CreateConversation()`; thêm hàm mới vào `LocalChatStore`: `public void AppendExchange(string conversationId, string userText, string answerText, string model, long? tokens, string? error)` (ghi 2 dòng user / assistant trạng thái hoàn tất, cùng transaction). Lịch sử đưa vào prompt = 20 tin gần nhất, mỗi tin ≤ 2000 ký tự. `KeepHistory = false` ⇒ không tạo / ghi hội thoại, ConversationId null.
- DI trong `hub/Services/HubV3Registration.cs`: `IChatAssistant` singleton.

## Phạm vi (được sửa những gì)
- `hub/Services/ChatAssistant.cs` (khung có sẵn — điền thân)
- `hub/Services/LocalChatStore.cs` (chỉ thêm hàm AppendExchange)
- `hub/Services/HubV3Registration.cs`
- `tasks/V10-05-tro-ly-chat.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không đổi `CodexChatClient`, hàng đợi chat cũ (`Enqueue/Claim/Finish`), `OfficeChatService`.
- Không gọi AI thật trong test (factory / codexChat giả).

## Ràng buộc
- `text` rỗng ⇒ ArgumentException "Nội dung không được để trống."; > 4000 ký tự ⇒ ArgumentException "Nội dung quá dài (tối đa 4000 ký tự).".
- `ai` không có trong `Options()` hoặc đang tắt ⇒ AssistantReply có Error "AI không hợp lệ hoặc đang tắt." (không ném).
- Prompt và câu trả lời đi qua `SecretRedactor.Redact` trước khi ghi file tạm / lưu SQLite.
- Hết hạn mức (`RunStatus.QuotaExhausted`) ⇒ Error "<Tên AI> đã hết hạn mức, chọn AI khác." và `registry.RecordQuota` với RemainingPct 0, Source "chat".
- Hủy `ct` ⇒ dừng mọi lời gọi đang chạy, không ghi lịch sử nửa chừng.

## Test nghiệm thu
`hub.tests/V1005ChatAssistantTests.cs` — Claude đã viết sẵn, đang đỏ 8/8 (registry SQLite thư mục tạm, factory / codexChat / office giả). Gồm:
- Settings mặc định; Save rồi đọc lại; ResearchAis 5 mục hoặc id lạ ⇒ ArgumentException.
- Ask với codex ⇒ gọi codexChat, không gọi factory; với mycli ⇒ gọi provider giả ReadOnly, thư mục tạm bị xóa.
- Ask lần 2 cùng ConversationId ⇒ prompt chứa câu hỏi lần 1; KeepHistory=false ⇒ ConversationId null, store không có hội thoại mới.
- Task ⇒ office.Send đúng project + text, không gọi AI; project null ⇒ ArgumentException.
- Research 2 AI (1 ok, 1 lỗi) ⇒ Text có 2 khối, Error null; cả 2 lỗi ⇒ Error khác null.
- QuotaExhausted ⇒ Error đúng mẫu + RecordQuota được gọi.
- ai = id đang tắt ⇒ Error "AI không hợp lệ hoặc đang tắt.".

## Tiêu chí hoàn thành
- [ ] 8 test trong file nghiệm thu xanh; không sửa file nghiệm thu.
- [ ] Build 0 warning; toàn bộ test xanh; giữ BOM (cả file mới).
- [ ] Không chạm `hub/Pages/**`, `hub/wwwroot/**`; không gọi AI thật trong test.
- [ ] Commit: "V10-05: trợ lý chat — hỏi đáp chọn AI, giao việc, nghiên cứu nhiều AI (Antigravity)".

## Báo cáo của agent
