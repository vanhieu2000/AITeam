using System.Text.RegularExpressions;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>
/// Trợ lý chat (/Assistant) — R-014 UI-09. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.5b.
/// Khung chat nổi trên mọi trang gọi ?handler=Send (POST + antiforgery, trả JSON) và ?handler=History.
/// Trang GET là bản mở rộng toàn màn hình của cùng khung chat.
/// </summary>
[ValidateAntiForgeryToken]
public sealed partial class AssistantModel(IChatAssistant assistant, ITeamConfigService config) : PageModel
{
    /// <summary>Kho lịch sử chat; null ⇒ lấy từ DI khi cần (cấu hình thiếu kho ⇒ lịch sử rỗng, trang vẫn chạy).</summary>
    public LocalChatStore? Store { get; init; }

    public const int MaxText = 4000;

    [GeneratedRegex(@"\A[0-9a-f]{32}\z")]
    private static partial Regex ConversationIdRx();

    public sealed record SendResult(bool Ok, string? ConversationId, string Mode, string Text, string? Error, long? Tokens = null, bool ResetConversation = false);

    public sealed record HistoryItem(string Role, string Text, string? Error, string At);

    public sealed record OptionItem(string Id, string Name, bool Available, string? Reason);

    public sealed record OptionsResult(string DefaultAi, string DefaultMode, bool KeepHistory, IReadOnlyList<string> ResearchAis,
        IReadOnlyList<OptionItem> Ais, IReadOnlyList<string> Projects, string? Error);

    public void OnGet() { }

    /// <summary>HTML khung chat nổi (tải khi bấm nút chat lần đầu).</summary>
    public PartialViewResult OnGetPanel() => Partial("_ChatPanel", false);

    /// <summary>Cài đặt + danh sách AI / dự án cho khung chat (tải khi mở khung lần đầu).</summary>
    public IActionResult OnGetOptions()
    {
        string? error = null;
        AssistantSettings? s = null;
        IReadOnlyList<OptionItem> ais = [];
        IReadOnlyList<string> projects = [];
        try
        {
            s = assistant.Settings();
            ais = assistant.Options().Select(o => new OptionItem(o.Id, o.Name, o.Available, o.Reason)).ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or ArgumentException)
        {
            error = "Không đọc được cài đặt trợ lý: " + ex.Message;
        }
        try { projects = config.GetProjects().Select(p => p.Name).ToList(); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { error ??= "Không đọc được danh sách dự án."; }
        return new JsonResult(new OptionsResult(s?.DefaultAi ?? "", (s?.DefaultMode ?? ChatMode.Ask).ToString().ToLowerInvariant(),
            s?.KeepHistory ?? true, s?.ResearchAis ?? [], ais, projects, error));
    }

    public async Task<IActionResult> OnPostSendAsync(string? conversationId, string? mode, string? ai, string? text, string? project)
    {
        if (!Enum.TryParse<ChatMode>(mode, ignoreCase: true, out var m) || !Enum.IsDefined(m))
            return new JsonResult(new SendResult(false, conversationId, mode ?? "", "", "Chế độ chat không hợp lệ.")) { StatusCode = 400 };
        if (conversationId != null && !ConversationIdRx().IsMatch(conversationId)) conversationId = null;
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
        try
        {
            var r = await assistant.SendAsync(conversationId, m, Blank(ai), text ?? "", Blank(project), ct);
            return new JsonResult(new SendResult(r.Error == null, r.ConversationId, r.Mode.ToString(), r.Text, r.Error, r.Tokens));
        }
        catch (ArgumentException ex)
        {
            return new JsonResult(new SendResult(false, conversationId, m.ToString(), "", ex.Message)) { StatusCode = 400 };
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException && conversationId != null)
        {
            // Hội thoại cũ đã bị xóa / không còn trên máy này ⇒ báo để khung chat bắt đầu cuộc mới.
            return new JsonResult(new SendResult(false, null, m.ToString(), "", "Cuộc trò chuyện cũ không còn. Gửi lại để bắt đầu cuộc mới.", null, ResetConversation: true));
        }
        catch (OperationCanceledException)
        {
            return new JsonResult(new SendResult(false, conversationId, m.ToString(), "", "Đã hủy.")) { StatusCode = 400 };
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or InvalidDataException or HttpRequestException or System.ComponentModel.Win32Exception)
        {
            return new JsonResult(new SendResult(false, conversationId, m.ToString(), "", "Trợ lý gặp lỗi: " + ex.Message)) { StatusCode = 500 };
        }
    }

    /// <summary>40 tin gần nhất của một hội thoại (để khung chat hiện lại sau khi chuyển trang).</summary>
    public IActionResult OnGetHistory(string? c)
    {
        var store = Store ?? HttpContext?.RequestServices.GetService(typeof(LocalChatStore)) as LocalChatStore;
        if (store == null || c == null || !ConversationIdRx().IsMatch(c)) return new JsonResult(Array.Empty<HistoryItem>());
        try
        {
            var items = store.Messages(c).TakeLast(40)
                .Select(x => new HistoryItem(x.Role, x.Text, x.Error, x.At)).ToList();
            return new JsonResult(items);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException or IOException)
        {
            return new JsonResult(Array.Empty<HistoryItem>());
        }
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
