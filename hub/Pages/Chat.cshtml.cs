using System.Text;
using System.Text.Json;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

[RequestSizeLimit(65536)]
public sealed class ChatModel(LocalChatStore store, ILocalChatClient client) : PageModel
{
    public void OnGet() { }

    private IActionResult Guard(Func<IActionResult> action)
    {
        // Hub chỉ dành cho máy cục bộ; chống Host giả trỏ vào loopback.
        if (!IsLocalHost()) return BadRequest(new { error = "Chat chỉ dùng trên máy cục bộ." });
        try { return action(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(409, new { error = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { error = "Không đọc/ghi được dữ liệu chat. Giữ nguyên dữ liệu cũ; kiểm tra quyền truy cập thư mục lưu trữ." }); }
    }
    private bool IsLocalHost() => Request.Host.Host is "localhost" or "127.0.0.1" or "::1" or "[::1]";

    public IActionResult OnGetState(string? conversationId) => Guard(() => new JsonResult(new
    {
        demo = store.IsDemo,
        location = store.Location,
        conversations = store.Conversations(),
        messages = string.IsNullOrEmpty(conversationId) ? [] : store.Messages(conversationId),
        settings = store.Settings(),
        contextNote = "AI nhận tối đa 40 tin nhắn gần nhất và câu hỏi hiện tại, giới hạn 48.000 ký tự. Lịch sử đầy đủ vẫn lưu trên máy."
    }));

    public IActionResult OnPostCreate() => Guard(() => new JsonResult(new { id = store.CreateConversation() }));
    public IActionResult OnPostSend(string conversationId, string text, string requestKey) => Guard(() =>
        new JsonResult(new { messageId = store.Enqueue(conversationId, text, requestKey) }));
    public IActionResult OnPostCancel(string conversationId) => Guard(() => { store.Cancel(conversationId); return new JsonResult(new { ok = true }); });
    public IActionResult OnPostDelete(string conversationId) => Guard(() => { store.Delete(conversationId); return new JsonResult(new { ok = true }); });
    public IActionResult OnPostSettings(string? model, string? codexPath, string? baseUrl, int timeoutSeconds, string? instructions) => Guard(() =>
    {
        store.SaveSettings(new ChatSettings { Model = model ?? "", CodexPath = codexPath ?? "", BaseUrl = baseUrl ?? "", TimeoutSeconds = timeoutSeconds, Instructions = instructions ?? "" });
        return new JsonResult(new { ok = true });
    });
    public async Task<IActionResult> OnPostCheckAsync(CancellationToken ct)
    {
        if (!IsLocalHost()) return BadRequest(new { error = "Chat chỉ dùng trên máy cục bộ." });
        try { return new JsonResult(await client.CheckAsync(store.Settings(), ct)); }
        catch (OperationCanceledException) { return StatusCode(408, new { error = "Kiểm tra bị gián đoạn. Anh có thể thử lại." }); }
        catch (Exception) { return StatusCode(500, new { error = "Chưa kiểm tra được CLI. Xem lại cài đặt và đường dẫn Codex." }); }
    }
    public IActionResult OnGetExport(string conversationId) => Guard(() =>
    {
        var messages = store.Messages(conversationId, all: true);
        var data = JsonSerializer.Serialize(new { version = 1, conversationId, messages }, new JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(data), "application/json; charset=utf-8", $"aiteam-chat-{conversationId}.json");
    });
}
