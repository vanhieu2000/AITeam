using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;

namespace AITeam.Hub.Services;

// V10-05 (R-014): trợ lý chat cho nút chat nổi. KHUNG do Claude viết — giữ nguyên chữ ký, agent điền phần thân (TODO(agent)).
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

    public AssistantSettings Settings() => throw new NotImplementedException("TODO(agent) V10-05");

    public void SaveSettings(AssistantSettings settings) => throw new NotImplementedException("TODO(agent) V10-05");

    public IReadOnlyList<AssistantOption> Options() => throw new NotImplementedException("TODO(agent) V10-05");

    public Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct) =>
        throw new NotImplementedException("TODO(agent) V10-05");
}
