using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// R-014 UI-09 (Claude): khung chat nổi nối thật với IChatAssistant (không còn iframe).
public sealed class UI09ChatPanelTests
{
    private sealed class FakeAssistant : IChatAssistant
    {
        public List<(string? Conv, ChatMode Mode, string? Ai, string Text, string? Project)> Calls { get; } = [];
        public Exception? Throw { get; set; }
        public AssistantSettings Settings() => new("codex", ChatMode.Research, ["codex", "mycli"], true);
        public void SaveSettings(AssistantSettings settings) { }
        public IReadOnlyList<AssistantOption> Options() => [new("codex", "Codex", true, null), new("off", "Tắt <b>", false, "Đang tắt")];
        public Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct)
        {
            Calls.Add((conversationId, mode, ai, text, project));
            if (Throw != null) throw Throw;
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Nội dung không được để trống.");
            return Task.FromResult(new AssistantReply("0123456789abcdef0123456789abcdef", mode, "<script>alert(1)</script> trả lời", null, 7));
        }
    }

    private static T Json<T>(IActionResult r) => Assert.IsType<T>(Assert.IsType<JsonResult>(r).Value);

    private static AssistantModel Page(FakeAssistant a, LocalChatStore store) => new(a, new StubConfig()) { Store = store };

    private sealed class StubConfig : ITeamConfigService
    {
        public IReadOnlyList<AITeam.Hub.Models.ProjectInfo> GetProjects() => throw new IOException("không đọc được");
        public AITeam.Hub.Models.ProjectInfo? FindProject(string name) => null;
        public AITeam.Hub.Models.TeamSettings GetSettings() => throw new NotSupportedException();
        public void SaveSettings(AITeam.Hub.Models.TeamSettings settings) => throw new NotSupportedException();
        public void SaveProject(AITeam.Hub.Models.ProjectInfo project) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Send_passes_mode_ai_project_and_drops_bad_conversation_id()
    {
        var a = new FakeAssistant();
        using var store = new LocalChatStore(":memory:");
        var r = Json<AssistantModel.SendResult>(await Page(a, store).OnPostSendAsync("../x", "ask", " codex ", "Xin chào", null));
        Assert.True(r.Ok);
        Assert.Equal("0123456789abcdef0123456789abcdef", r.ConversationId);
        Assert.Equal((null, ChatMode.Ask, "codex", "Xin chào", null), a.Calls[0]);

        await Page(a, store).OnPostSendAsync("0123456789abcdef0123456789abcdef", "task", null, "Làm báo cáo", "AITeam");
        Assert.Equal(ChatMode.Task, a.Calls[1].Mode);
        Assert.Equal("AITeam", a.Calls[1].Project);
        Assert.Equal("0123456789abcdef0123456789abcdef", a.Calls[1].Conv);
    }

    [Theory]
    [InlineData("xoa")]
    [InlineData("7")]
    [InlineData(null)]
    public async Task Bad_mode_is_400_without_calling_assistant(string? mode)
    {
        var a = new FakeAssistant();
        using var store = new LocalChatStore(":memory:");
        var json = Assert.IsType<JsonResult>(await Page(a, store).OnPostSendAsync(null, mode, null, "x", null));
        Assert.Equal(400, json.StatusCode);
        Assert.Empty(a.Calls);
    }

    [Fact]
    public async Task Errors_become_json_messages()
    {
        using var store = new LocalChatStore(":memory:");
        var empty = Assert.IsType<JsonResult>(await Page(new FakeAssistant(), store).OnPostSendAsync(null, "ask", null, "  ", null));
        Assert.Equal(400, empty.StatusCode);
        Assert.Contains("trống", ((AssistantModel.SendResult)empty.Value!).Error);

        var gone = Json<AssistantModel.SendResult>(await Page(new FakeAssistant { Throw = new KeyNotFoundException("x") }, store)
            .OnPostSendAsync("0123456789abcdef0123456789abcdef", "ask", null, "hỏi", null));
        Assert.True(gone.ResetConversation);
        Assert.Null(gone.ConversationId);

        var io = Assert.IsType<JsonResult>(await Page(new FakeAssistant { Throw = new IOException("đĩa đầy") }, store).OnPostSendAsync(null, "ask", null, "hỏi", null));
        Assert.Equal(500, io.StatusCode);
    }

    [Fact]
    public void Options_survive_project_read_error()
    {
        using var store = new LocalChatStore(":memory:");
        var o = Json<AssistantModel.OptionsResult>(Page(new FakeAssistant(), store).OnGetOptions());
        Assert.Equal("codex", o.DefaultAi);
        Assert.Equal("research", o.DefaultMode);
        Assert.Equal(2, o.Ais.Count);
        Assert.False(o.Ais[1].Available);
        Assert.Empty(o.Projects);
        Assert.NotNull(o.Error);
    }

    [Fact]
    public void History_returns_last_messages_or_empty_for_bad_id()
    {
        using var store = new LocalChatStore(":memory:");
        var id = store.CreateConversation();
        store.AppendExchange(id, "câu hỏi", "trả lời", "m", 3, null);
        var items = Json<List<AssistantModel.HistoryItem>>(Page(new FakeAssistant(), store).OnGetHistory(id));
        Assert.Equal(["user", "assistant"], items.Select(i => i.Role));
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<AssistantModel.HistoryItem>>(Assert.IsType<JsonResult>(Page(new FakeAssistant(), store).OnGetHistory("../x")).Value));
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<AssistantModel.HistoryItem>>(Assert.IsType<JsonResult>(Page(new FakeAssistant(), store).OnGetHistory(new string('a', 32))).Value));
    }

    [Fact]
    public async Task Layout_has_lazy_panel_and_post_requires_antiforgery()
    {
        var a = new FakeAssistant();
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s => s.AddSingleton<IChatAssistant>(a));

        var home = await host.Client.GetStringAsync("/Projects");
        Assert.Contains("id=\"fab\"", home);
        Assert.Contains("data-lazy=\"1\"", home);
        Assert.Contains("chat-panel.js", home);
        Assert.DoesNotContain("<iframe", home);

        var panelResp = await host.Client.GetAsync("/Assistant?handler=Panel");
        var panel = await panelResp.Content.ReadAsStringAsync();
        Assert.True(panelResp.IsSuccessStatusCode, panel.Length > 1500 ? panel[..1500] : panel);
        Assert.DoesNotContain("<html", panel);
        Assert.Matches("data-csrf=\"[^\"]{20,}\"", panel);
        Assert.Contains("data-chat-form", panel);

        var noToken = await host.Client.PostAsync("/Assistant?handler=Send",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["mode"] = "ask", ["text"] = "x" }));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        Assert.Empty(a.Calls);

        var full = await host.Client.GetStringAsync("/Assistant");
        Assert.Contains("id=\"chatfull\"", full);
        Assert.DoesNotContain("id=\"fab\"", full);

        var opts = JsonDocument.Parse(await host.Client.GetStringAsync("/Assistant?handler=Options"));
        Assert.Equal("codex", opts.RootElement.GetProperty("defaultAi").GetString());
        Assert.Equal("Tắt <b>", opts.RootElement.GetProperty("ais")[1].GetProperty("name").GetString());
    }

    [Fact]
    public void Script_never_sets_ai_text_as_html()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "hub", "wwwroot", "js", "chat-panel.js"))) dir = dir.Parent;
        var js = File.ReadAllText(Path.Combine(dir!.FullName, "hub", "wwwroot", "js", "chat-panel.js"));
        // innerHTML chỉ dùng một chỗ cho HTML khung do Hub dựng; chữ từ AI / người dùng luôn qua textContent.
        Assert.Single(Regex.Matches(js, @"\.innerHTML\s*="));
        Assert.Contains("p.textContent = body", js);
        Assert.Contains("o.textContent = it.name", js);
    }
}
