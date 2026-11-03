using System.Net;
using System.Text.RegularExpressions;
using AITeam.Core.Accounts;
using AITeam.Core.Research;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

public sealed class H705EmbeddedResearchTests
{
    [Fact]
    public async Task Missing_selection_disables_research_and_posts_need_antiforgery()
    {
        using var env = new TestEnvironment(); var research = new ResearchFake();
        await using var host = await Host(env, research);
        var html = await host.Client.GetStringAsync("/");
        Assert.Contains("Chưa có phiếu để nghiên cứu.", WebUtility.HtmlDecode(html));
        Assert.Contains("handler=ResearchAsk", html);
        var response = await host.Client.PostAsync("/?handler=ResearchAsk", new FormUrlEncodedContent(new Dictionary<string, string> { ["r"] = "R-001", ["question"] = "Hỏi" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, research.AskCalls);
    }

    [Fact]
    public async Task Embedded_ask_and_manual_save_share_service_and_stay_with_selected_request()
    {
        using var env = new TestEnvironment(); var research = new ResearchFake();
        await using var host = await Host(env, research);
        var html = await host.Client.GetStringAsync("/?RequestId=R-001");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token); host.Client.DefaultRequestHeaders.Add("RequestVerificationToken", token);
        var asked = await host.Client.PostAsync("/?handler=ResearchAsk", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["r"] = "R-001", ["question"] = "Câu hỏi <script>bad</script>", ["ai"] = "manual" }));
        // H8-01 (R-012, Claude): hỏi thành công ⇒ Post-Redirect-Get về Văn phòng, giữ phiếu đang chọn;
        // trang GET sau redirect hiện lại câu hỏi vừa gửi và form lưu thủ công.
        Assert.Equal(HttpStatusCode.Redirect, asked.StatusCode);
        var location = asked.Headers.Location!;
        Assert.False(location.IsAbsoluteUri && location.Host != host.Client.BaseAddress!.Host);
        Assert.Contains("R-001", location.OriginalString);
        html = await host.Client.GetStringAsync(location);
        Assert.Equal("R-001", research.LastRequest);
        Assert.Contains("handler=ResearchSaveManual", html);
        Assert.DoesNotContain("<script>bad</script>", html);
        var saved = await host.Client.PostAsync("/?handler=ResearchSaveManual", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["r"] = "R-001", ["questionId"] = "Q-1", ["ai"] = "manual", ["question"] = "Câu hỏi", ["answer"] = "Kết quả riêng R-001 <script>answer</script>", ["sources"] = "" }));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        html = await host.Client.GetStringAsync("/?RequestId=R-001");
        Assert.Contains("Kết quả riêng R-001", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("<script>answer</script>", html);
        var other = await host.Client.GetStringAsync("/?handler=Research&requestId=R-002");
        Assert.Contains("R-002", other); Assert.DoesNotContain("Kết quả riêng R-001", WebUtility.HtmlDecode(other));
        Assert.DoesNotContain("office-chat-thread", other);
        var standalone = await host.Client.GetStringAsync("/Research?r=R-001");
        Assert.Contains("handler=Ask", standalone);
        Assert.Contains("Kết quả riêng R-001", WebUtility.HtmlDecode(standalone));
    }

    [Fact]
    public async Task Invalid_request_cannot_call_ai_and_automatic_error_is_displayed_safely()
    {
        using var env = new TestEnvironment(); var research = new ResearchFake { Fail = true };
        await using var host = await Host(env, research);
        var html = await host.Client.GetStringAsync("/?RequestId=R-001");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        host.Client.DefaultRequestHeaders.Add("RequestVerificationToken", token);
        foreach (var id in new[] { "../escape", "R-999", "R-001" })
        {
            var response = await host.Client.PostAsync("/?handler=ResearchAsk", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["r"] = id, ["question"] = "Hỏi", ["ai"] = "chatgpt" }));
            html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("<script>failure</script>", html);
        }
        Assert.Equal(1, research.AskCalls);
        Assert.Contains("failure", html);
    }

    private static Task<HubTestHost> Host(TestEnvironment env, ResearchFake research) => HubTestHost.StartAsync(env, s =>
    { s.AddSingleton<IRequestService>(new RequestsFake()); s.AddSingleton<IResearchService>(research); });

    private sealed class RequestsFake : IRequestService
    {
        public IReadOnlyList<RequestThread> GetAll() => [new("R-001", "Phiếu một", "", "", "", [], [], false, ""), new("R-002", "Phiếu hai", "", "", "", [], [], false, "")];
        public RequestThread? Get(string id) => GetAll().FirstOrDefault(r => r.Id == id);
    }
    private sealed class ResearchFake : IResearchService
    {
        private readonly List<ResearchEntry> _entries = [];
        public int AskCalls { get; private set; }
        public string? LastRequest { get; private set; }
        public bool Fail { get; init; }
        public IReadOnlyList<ResearchAiOption> Options() => [new("chatgpt", "ChatGPT", AiMode.Automatic, AiStatus.Connected, "Sẵn sàng", true, ""), new("manual", "Thủ công", AiMode.Manual, AiStatus.ManualOnly, "Thủ công", false, "")];
        public ResearchAskResult Ask(string? requestId, string? question, IReadOnlyList<string>? ais, bool includeRequestText)
        {
            AskCalls++; LastRequest = requestId;
            if (Fail) throw new IOException("<script>failure</script>");
            return new("Q-1", question ?? "", 0, [new("manual", "manual", "Dán kết quả")]);
        }
        public ResearchEntry SaveManual(string? requestId, string? questionId, string? ai, string? question, string? answer, string? sources)
        {
            var entry = new ResearchEntry(requestId!, questionId!, ai!, "manual", DateTime.UtcNow, question!, answer!, sources ?? "", "");
            _entries.Add(entry); return entry;
        }
        public IReadOnlyList<ResearchJob> Jobs(string? requestId) => [];
        public IReadOnlyList<ResearchEntry> Entries(string? requestId) => _entries.Where(e => e.RequestId == requestId).ToList();
        public int RunningCount() => 0;
    }
}
