using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Providers;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// Test nghiệm thu V10-05 (Claude): trợ lý chat — hỏi đáp chọn AI, giao việc, nghiên cứu nhiều AI. KHÔNG sửa file này.
public sealed class V1005ChatAssistantTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aiteam-v1005-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteAiServiceRegistry _registry;
    private readonly LocalChatStore _store;
    private readonly FakeFactory _factory;
    private readonly FakeCodex _codex = new();
    private readonly FakeOffice _office = new();

    public V1005ChatAssistantTests()
    {
        Directory.CreateDirectory(_dir);
        _registry = new SqliteAiServiceRegistry(Path.Combine(_dir, "hub", "office.db"), Path.Combine(_dir, "root"));
        _registry.Upsert(Svc("codex", "codex"));
        _registry.Upsert(Svc("mycli", "mycli"));
        _registry.Upsert(Svc("other", "othercli"));
        _registry.Upsert(Svc("off", "offcli", enabled: false));
        _store = new LocalChatStore(Path.Combine(_dir, "chat.db"));
        _factory = new FakeFactory(_registry);
    }

    public void Dispose()
    {
        _store.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static AiService Svc(string id, string command, bool enabled = true) =>
        new(id, "Dịch vụ " + id, AiAdapter.CliText, command, [], null, ["model-x"], [AiRole.Chat],
            1, false, null, null, enabled, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private ChatAssistant Assistant() => new(_registry, _factory, _codex, _store, _office);

    private sealed class FakeProvider(string id) : IAgentProvider
    {
        public RunStatus Status { get; set; } = RunStatus.Succeeded;
        public List<string> Prompts { get; } = [];
        public List<string> WorkDirs { get; } = [];
        public List<RunMode> Modes { get; } = [];
        public string Id => id;
        public ProviderKind Kind => ProviderKind.Generic;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(id, true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
        {
            Prompts.Add(File.ReadAllText(request.PromptFile));
            WorkDirs.Add(request.WorkDir);
            Modes.Add(request.Mode);
            var text = Status == RunStatus.Succeeded ? id + " trả lời" : "";
            return Task.FromResult(new AgentRunResult(id, Status, text, new TokenUsage(5, 0, 3), TimeSpan.FromSeconds(1), "model-x", null, Status.ToString()));
        }
    }

    private sealed class FakeFactory(IAiServiceRegistry registry) : IAiProviderFactory
    {
        public Dictionary<string, FakeProvider> Providers { get; } = new(StringComparer.Ordinal);
        public int Created { get; private set; }
        public IAgentProvider Create(AiService service)
        {
            Created++;
            if (!service.Enabled) throw new InvalidOperationException("Dịch vụ AI " + service.Id + " đang tắt.");
            if (!Providers.TryGetValue(service.Id, out var p)) Providers[service.Id] = p = new FakeProvider(service.Id);
            return p;
        }
        public IReadOnlyList<AiService> ForRole(AiRole role) => registry.List(false).Where(s => s.Roles.Contains(role)).ToList();
        public FakeProvider Get(string id) => Providers.TryGetValue(id, out var p) ? p : Providers[id] = new FakeProvider(id);
    }

    private sealed class FakeCodex : ILocalChatClient
    {
        public List<IReadOnlyList<ChatMessage>> Contexts { get; } = [];
        public Task<ChatAnswer> ReplyAsync(ChatSettings settings, IReadOnlyList<ChatMessage> context, CancellationToken ct)
        {
            Contexts.Add(context);
            return Task.FromResult(new ChatAnswer("codex trả lời", null, 10));
        }
        public Task<ChatConnection> CheckAsync(ChatSettings settings, CancellationToken ct) => Task.FromResult(new ChatConnection(true, true, ""));
    }

    private sealed class FakeOffice : IOfficeChatService
    {
        public List<(string Project, string? Text)> Sent { get; } = [];
        public void Send(string project, string? text, string? requestId = null) => Sent.Add((project, text));
    }

    [Fact]
    public void Settings_default_save_roundtrip_and_validation()
    {
        var a = Assistant();
        var d = a.Settings();
        Assert.Equal("codex", d.DefaultAi);
        Assert.Equal(ChatMode.Ask, d.DefaultMode);
        Assert.Equal(["codex", "opencode"], d.ResearchAis);
        Assert.True(d.KeepHistory);

        a.SaveSettings(new AssistantSettings("mycli", ChatMode.Research, ["mycli", "other"], false));
        var r = Assistant().Settings();
        Assert.Equal("mycli", r.DefaultAi);
        Assert.Equal(ChatMode.Research, r.DefaultMode);
        Assert.Equal(["mycli", "other"], r.ResearchAis);
        Assert.False(r.KeepHistory);

        Assert.Throws<ArgumentException>(() => a.SaveSettings(new AssistantSettings("khong-co", ChatMode.Ask, ["mycli"], true)));
        Assert.Throws<ArgumentException>(() => a.SaveSettings(new AssistantSettings("mycli", ChatMode.Ask, ["mycli", "khong-co"], true)));
        Assert.Throws<ArgumentException>(() => a.SaveSettings(new AssistantSettings("mycli", ChatMode.Ask, ["codex", "mycli", "other", "codex", "mycli"], true)));
    }

    [Fact]
    public void Options_list_chat_services_and_mark_disabled()
    {
        var options = Assistant().Options();
        Assert.Contains(options, o => o.Id == "mycli" && o.Available);
        var off = Assert.Single(options, o => o.Id == "off");
        Assert.False(off.Available);
        Assert.False(string.IsNullOrWhiteSpace(off.Reason));
    }

    [Fact]
    public async Task Ask_codex_uses_codex_chat_and_other_ai_uses_factory_readonly_in_temp_dir()
    {
        var a = Assistant();
        var codex = await a.SendAsync(null, ChatMode.Ask, "codex", "Xin chào", null, CancellationToken.None);
        Assert.Null(codex.Error);
        Assert.Equal("codex trả lời", codex.Text);
        Assert.Single(_codex.Contexts);
        Assert.Equal(0, _factory.Created);

        var mine = await a.SendAsync(null, ChatMode.Ask, "mycli", "Câu hỏi", null, CancellationToken.None);
        Assert.Null(mine.Error);
        Assert.Equal("mycli trả lời", mine.Text);
        var p = _factory.Get("mycli");
        Assert.Equal(RunMode.ReadOnly, Assert.Single(p.Modes));
        Assert.False(Directory.Exists(p.WorkDirs[0]));
    }

    [Fact]
    public async Task Ask_keeps_history_between_turns_and_skips_it_when_disabled()
    {
        var a = Assistant();
        var first = await a.SendAsync(null, ChatMode.Ask, "mycli", "Câu hỏi một", null, CancellationToken.None);
        Assert.NotNull(first.ConversationId);
        await a.SendAsync(first.ConversationId, ChatMode.Ask, "mycli", "Câu hỏi hai", null, CancellationToken.None);
        var prompts = _factory.Get("mycli").Prompts;
        Assert.Contains("Câu hỏi một", prompts[1]);
        Assert.Contains("Câu hỏi hai", prompts[1]);
        Assert.Contains(_store.Messages(first.ConversationId!, true), m => m.Text.Contains("Câu hỏi một", StringComparison.Ordinal));

        var before = _store.Conversations().Count;
        a.SaveSettings(new AssistantSettings("mycli", ChatMode.Ask, ["mycli"], false));
        var noHistory = await a.SendAsync(null, ChatMode.Ask, "mycli", "Không lưu", null, CancellationToken.None);
        Assert.Null(noHistory.ConversationId);
        Assert.Equal(before, _store.Conversations().Count);
    }

    [Fact]
    public async Task Task_mode_sends_to_office_inbox_without_calling_ai()
    {
        var a = Assistant();
        var reply = await a.SendAsync(null, ChatMode.Task, "mycli", "Làm màn hình báo cáo", "AITeam", CancellationToken.None);
        Assert.Null(reply.Error);
        var sent = Assert.Single(_office.Sent);
        Assert.Equal("AITeam", sent.Project);
        Assert.Equal("Làm màn hình báo cáo", sent.Text);
        Assert.Equal(0, _factory.Created);
        Assert.Empty(_codex.Contexts);
        await Assert.ThrowsAsync<ArgumentException>(() => a.SendAsync(null, ChatMode.Task, null, "Không có dự án", null, CancellationToken.None));
    }

    [Fact]
    public async Task Research_merges_answers_and_errors_only_when_all_fail()
    {
        var a = Assistant();
        a.SaveSettings(new AssistantSettings("mycli", ChatMode.Research, ["mycli", "other"], true));
        _factory.Get("other").Status = RunStatus.Failed;
        var mixed = await a.SendAsync(null, ChatMode.Research, null, "So sánh hai cách", null, CancellationToken.None);
        Assert.Null(mixed.Error);
        Assert.Contains("### Dịch vụ mycli", mixed.Text);
        Assert.Contains("mycli trả lời", mixed.Text);
        Assert.Contains("### Dịch vụ other", mixed.Text);

        _factory.Get("mycli").Status = RunStatus.Failed;
        var allBad = await a.SendAsync(null, ChatMode.Research, null, "Lại", null, CancellationToken.None);
        Assert.NotNull(allBad.Error);
    }

    [Fact]
    public async Task Quota_exhausted_is_reported_and_recorded()
    {
        var a = Assistant();
        _factory.Get("mycli").Status = RunStatus.QuotaExhausted;
        var reply = await a.SendAsync(null, ChatMode.Ask, "mycli", "Hỏi", null, CancellationToken.None);
        Assert.NotNull(reply.Error);
        Assert.Contains("hết hạn mức", reply.Error);
        Assert.Contains(_registry.LatestQuotas(), q => q.ServiceId == "mycli" && q.RemainingPct == 0);
    }

    [Fact]
    public async Task Disabled_or_unknown_ai_and_bad_text_are_rejected()
    {
        var a = Assistant();
        var off = await a.SendAsync(null, ChatMode.Ask, "off", "Hỏi", null, CancellationToken.None);
        Assert.Equal("AI không hợp lệ hoặc đang tắt.", off.Error);
        var unknown = await a.SendAsync(null, ChatMode.Ask, "khong-co", "Hỏi", null, CancellationToken.None);
        Assert.Equal("AI không hợp lệ hoặc đang tắt.", unknown.Error);
        await Assert.ThrowsAsync<ArgumentException>(() => a.SendAsync(null, ChatMode.Ask, "mycli", "   ", null, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => a.SendAsync(null, ChatMode.Ask, "mycli", new string('x', 4001), null, CancellationToken.None));
    }
}
