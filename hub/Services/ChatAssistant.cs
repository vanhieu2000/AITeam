using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Providers;
using AITeam.Core.Research;

namespace AITeam.Hub.Services;

// V10-05 (R-014): trợ lý chat cho nút chat nổi.
// Thiết kế: tasks/V10-05-tro-ly-chat.md, docs/design/R-014-van-phong-ai.md mục 4.5b.

public enum ChatMode { Ask, Task, Research }

public sealed record AssistantSettings(string DefaultAi, ChatMode DefaultMode, IReadOnlyList<string> ResearchAis, bool KeepHistory);

public sealed record AssistantOption(string Id, string Name, bool Available, string? Reason);

public sealed record AssistantReply(string? ConversationId, ChatMode Mode, string Text, string? Error, long? Tokens);

public interface IChatAssistant
{
    AssistantSettings Settings();
    void SaveSettings(AssistantSettings settings);
    IReadOnlyList<AssistantOption> Options();
    Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct);
}

public sealed class ChatAssistant(
    IAiServiceRegistry registry, IAiProviderFactory factory, ILocalChatClient codexChat, LocalChatStore store, IOfficeChatService office) : IChatAssistant
{
    private readonly IAiServiceRegistry _registry = registry;
    private readonly IAiProviderFactory _factory = factory;
    private readonly ILocalChatClient _codexChat = codexChat;
    private readonly LocalChatStore _store = store;
    private readonly IOfficeChatService _office = office;
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public AssistantSettings Settings()
    {
        var defaultAi = _registry.GetSetting("assistant.defaultAi")?.Trim();
        if (string.IsNullOrEmpty(defaultAi))
            defaultAi = "codex";

        var modeStr = _registry.GetSetting("assistant.defaultMode")?.Trim().ToLowerInvariant();
        var defaultMode = modeStr switch
        {
            "ask" => ChatMode.Ask,
            "task" => ChatMode.Task,
            "research" => ChatMode.Research,
            _ => ChatMode.Ask
        };

        var researchStr = _registry.GetSetting("assistant.researchAis");
        IReadOnlyList<string> researchAis;
        if (string.IsNullOrWhiteSpace(researchStr))
        {
            researchAis = ["codex", "opencode"];
        }
        else
        {
            var list = researchStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            researchAis = list.Count > 0 ? list : ["codex", "opencode"];
        }

        var histStr = _registry.GetSetting("assistant.keepHistory")?.Trim();
        var keepHistory = histStr switch
        {
            "0" => false,
            "1" => true,
            _ => true
        };

        return new AssistantSettings(defaultAi, defaultMode, researchAis, keepHistory);
    }

    public void SaveSettings(AssistantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var options = Options();

        if (string.IsNullOrWhiteSpace(settings.DefaultAi) || !options.Any(o => string.Equals(o.Id, settings.DefaultAi, StringComparison.Ordinal)))
            throw new ArgumentException("AI mặc định không hợp lệ.");

        if (settings.ResearchAis == null || settings.ResearchAis.Count < 1 || settings.ResearchAis.Count > 4)
            throw new ArgumentException("Danh sách AI nghiên cứu phải có từ 1 đến 4 mục.");

        if (settings.ResearchAis.Distinct(StringComparer.Ordinal).Count() != settings.ResearchAis.Count)
            throw new ArgumentException("Danh sách AI nghiên cứu không được trùng lặp.");

        foreach (var aiId in settings.ResearchAis)
        {
            if (string.IsNullOrWhiteSpace(aiId) || !options.Any(o => string.Equals(o.Id, aiId, StringComparison.Ordinal)))
                throw new ArgumentException($"AI nghiên cứu '{aiId}' không hợp lệ.");
        }

        _registry.SetSetting("assistant.defaultAi", settings.DefaultAi);
        _registry.SetSetting("assistant.defaultMode", settings.DefaultMode switch
        {
            ChatMode.Ask => "ask",
            ChatMode.Task => "task",
            ChatMode.Research => "research",
            _ => "ask"
        });
        _registry.SetSetting("assistant.researchAis", string.Join(",", settings.ResearchAis));
        _registry.SetSetting("assistant.keepHistory", settings.KeepHistory ? "1" : "0");
    }

    public IReadOnlyList<AssistantOption> Options()
    {
        return _registry.List(includeDisabled: true)
            .Where(s => s.Roles.Contains(AiRole.Chat))
            .Select(s => new AssistantOption(s.Id, s.Name, s.Enabled, s.Enabled ? null : "Đang tắt"))
            .ToList();
    }

    public async Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Nội dung không được để trống.");
        if (text.Length > 4000)
            throw new ArgumentException("Nội dung quá dài (tối đa 4000 ký tự).");

        if (mode == ChatMode.Task)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentException("Chọn dự án trước khi giao việc.");
            _office.Send(project, text);
            return new AssistantReply(conversationId, ChatMode.Task, "Đã gửi cho Claude. Claude sẽ tạo phiếu và báo trên trang Theo dõi việc.", null, null);
        }

        var settings = Settings();

        if (mode == ChatMode.Ask)
        {
            var targetAi = string.IsNullOrWhiteSpace(ai) ? settings.DefaultAi : ai;
            var options = Options();
            var opt = options.FirstOrDefault(o => string.Equals(o.Id, targetAi, StringComparison.Ordinal));
            if (opt == null || !opt.Available)
            {
                return new AssistantReply(conversationId, ChatMode.Ask, "", "AI không hợp lệ hoặc đang tắt.", null);
            }

            string? currentConvId = null;
            var historyMessages = new List<ChatMessage>();
            if (settings.KeepHistory)
            {
                currentConvId = string.IsNullOrWhiteSpace(conversationId)
                    ? _store.CreateConversation()
                    : conversationId;
                historyMessages = _store.Messages(currentConvId, all: true).TakeLast(20).ToList();
            }

            string answerText;
            string? error;
            long? tokens;
            string modelName;

            if (string.Equals(targetAi, "codex", StringComparison.OrdinalIgnoreCase))
            {
                var context = new List<ChatMessage>();
                foreach (var m in historyMessages)
                {
                    var content = m.Text.Length > 2000 ? m.Text[..2000] : m.Text;
                    context.Add(m with { Text = content });
                }
                context.Add(new ChatMessage(0, "user", text, "complete", DateTimeOffset.UtcNow.ToString("O"), "", null, null));
                var chatSettings = _store.Settings();
                var chatAnswer = await _codexChat.ReplyAsync(chatSettings, context, ct);
                answerText = chatAnswer.Text;
                error = chatAnswer.Error;
                tokens = chatAnswer.Tokens;
                modelName = !string.IsNullOrWhiteSpace(chatSettings.Model) ? chatSettings.Model : "codex";
            }
            else
            {
                var service = _registry.Get(targetAi);
                if (service == null || !service.Enabled)
                {
                    return new AssistantReply(conversationId, ChatMode.Ask, "", "AI không hợp lệ hoặc đang tắt.", null);
                }
                var (execText, execErr, execTokens, execModel) = await ExecuteAiServiceAsync(service, text, historyMessages, ct);
                answerText = execText;
                error = execErr;
                tokens = execTokens;
                modelName = execModel;
            }

            ct.ThrowIfCancellationRequested();

            if (settings.KeepHistory && currentConvId != null)
            {
                _store.AppendExchange(currentConvId, text, answerText, modelName, tokens, error);
            }

            return new AssistantReply(currentConvId, ChatMode.Ask, answerText, error, tokens);
        }

        if (mode == ChatMode.Research)
        {
            var researchAis = settings.ResearchAis;
            var options = Options();
            var activeServices = new List<AiService>();
            foreach (var id in researchAis)
            {
                var opt = options.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.Ordinal));
                if (opt != null && opt.Available)
                {
                    var svc = _registry.Get(id);
                    if (svc != null && svc.Enabled)
                        activeServices.Add(svc);
                }
            }

            if (activeServices.Count == 0)
            {
                return new AssistantReply(conversationId, ChatMode.Research, "", "AI không hợp lệ hoặc đang tắt.", null);
            }

            string? currentConvId = null;
            var historyMessages = new List<ChatMessage>();
            if (settings.KeepHistory)
            {
                currentConvId = string.IsNullOrWhiteSpace(conversationId)
                    ? _store.CreateConversation()
                    : conversationId;
                historyMessages = _store.Messages(currentConvId, all: true).TakeLast(20).ToList();
            }

            var tasks = activeServices.Select(async service =>
            {
                if (string.Equals(service.Id, "codex", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var context = new List<ChatMessage>();
                        foreach (var m in historyMessages)
                        {
                            var content = m.Text.Length > 2000 ? m.Text[..2000] : m.Text;
                            context.Add(m with { Text = content });
                        }
                        context.Add(new ChatMessage(0, "user", text, "complete", DateTimeOffset.UtcNow.ToString("O"), "", null, null));
                        var chatSettings = _store.Settings();
                        var ans = await _codexChat.ReplyAsync(chatSettings, context, ct);
                        return (Service: service, Text: ans.Text, Error: ans.Error, Tokens: ans.Tokens);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        return (Service: service, Text: "", Error: ex.Message, Tokens: (long?)null);
                    }
                }
                else
                {
                    try
                    {
                        var (t, err, tok, _) = await ExecuteAiServiceAsync(service, text, historyMessages, ct);
                        return (Service: service, Text: t, Error: err, Tokens: tok);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        return (Service: service, Text: "", Error: ex.Message, Tokens: (long?)null);
                    }
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            ct.ThrowIfCancellationRequested();

            var blocks = new List<string>();
            var anySuccess = false;
            long totalTokens = 0;
            var hasTokens = false;

            foreach (var r in results)
            {
                if (r.Tokens.HasValue)
                {
                    totalTokens += r.Tokens.Value;
                    hasTokens = true;
                }

                if (string.IsNullOrEmpty(r.Error) && !string.IsNullOrWhiteSpace(r.Text))
                {
                    anySuccess = true;
                    blocks.Add($"### {r.Service.Name}\n{r.Text}");
                }
                else
                {
                    var errStr = !string.IsNullOrWhiteSpace(r.Error) ? r.Error : "Lỗi không xác định.";
                    blocks.Add($"### {r.Service.Name}\n(lỗi: {errStr})");
                }
            }

            var combinedText = string.Join("\n\n", blocks);
            string? combinedError = anySuccess ? null : "Tất cả AI nghiên cứu đều gặp lỗi.";
            long? finalTokens = hasTokens ? totalTokens : null;

            if (settings.KeepHistory && currentConvId != null)
            {
                _store.AppendExchange(currentConvId, text, combinedText, "research", finalTokens, combinedError);
            }

            return new AssistantReply(currentConvId, ChatMode.Research, combinedText, combinedError, finalTokens);
        }

        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Chế độ chat không hợp lệ.");
    }

    private async Task<(string Text, string? Error, long? Tokens, string Model)> ExecuteAiServiceAsync(
        AiService service, string userText, IReadOnlyList<ChatMessage> historyMessages, CancellationToken ct)
    {
        var dir = Path.Combine(Path.GetTempPath(), "aiteam-chat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var instructions = _store.Settings().Instructions;
            var historyJson = JsonSerializer.Serialize(
                historyMessages.Select(m => new { role = m.Role, content = m.Text.Length > 2000 ? m.Text[..2000] : m.Text }), JsonOptions);
            var promptRaw = "Bạn đang trò chuyện trực tiếp với chủ dự án trong AITeam Hub. Đây là hội thoại, không phải tác vụ code. "
                + "Không chạy lệnh, không đọc hoặc sửa file, không gọi công cụ. Không tự nhận đã giao việc/merge. "
                + "Hãy chỉ trả lời câu hỏi cuối, dùng lịch sử dưới đây để giữ ngữ cảnh. "
                + "Lịch sử cũ có thể được rút gọn; không tự bịa thông tin đã mất.\n"
                + instructions + "\nLịch sử hội thoại (JSON):\n" + historyJson
                + "\n\n" + userText;
            var promptText = SecretRedactor.Redact(promptRaw).Text;
            var promptFile = Path.Combine(dir, "prompt.txt");
            await File.WriteAllTextAsync(promptFile, promptText, new UTF8Encoding(false), ct);

            var model = service.Models.FirstOrDefault() ?? service.Id;
            var provider = _factory.Create(service);
            var runResult = await provider.RunAsync(
                new AgentRunRequest(dir, promptFile, RunMode.ReadOnly, TimeSpan.FromSeconds(180), model), ct);

            long? tokens = runResult.Tokens != null && runResult.Tokens.IsKnown ? runResult.Tokens.Total : null;
            var reportedModel = runResult.Model ?? model;

            if (runResult.Status == RunStatus.QuotaExhausted)
            {
                _registry.RecordQuota(new AiQuota(service.Id, null, 0, null, "chat", DateTimeOffset.UtcNow));
                return ("", $"{service.Name} đã hết hạn mức, chọn AI khác.", tokens, reportedModel);
            }

            if (runResult.Status == RunStatus.Succeeded)
            {
                var safeText = SecretRedactor.Redact(runResult.Text).Text;
                return (safeText, null, tokens, reportedModel);
            }

            var error = string.IsNullOrWhiteSpace(runResult.Detail)
                ? $"{service.Name} không thành công ({runResult.Status})."
                : runResult.Detail;
            return ("", error, tokens, reportedModel);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
