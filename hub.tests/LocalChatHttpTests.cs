using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;

namespace AITeam.Hub.Tests;

public sealed class LocalChatHttpTests
{
    [Fact]
    public async Task Http_chat_requires_antiforgery_and_persists_real_demo_response()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, services => services.AddLocalChat(demo: true));
        var html = await host.Client.GetStringAsync("/Chat");
        Assert.Contains("Trò chuyện với ChatGPT", html);
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        var missingToken = await host.Client.PostAsync("/Chat?handler=Create", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        host.Client.DefaultRequestHeaders.Add("RequestVerificationToken", token);
        using var created = await Post(host.Client, "Create", []);
        var id = created.RootElement.GetProperty("id").GetString()!;
        using var sent = await Post(host.Client, "Send", new() { ["conversationId"] = id, ["text"] = "Tên dự án là AITeam <script>alert(1)</script>", ["requestKey"] = Guid.NewGuid().ToString("N") });
        Assert.True(sent.RootElement.GetProperty("messageId").GetInt64() > 0);
        JsonDocument? state = null;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                state?.Dispose();
                state = JsonDocument.Parse(await host.Client.GetStringAsync("/Chat?handler=State&conversationId=" + id, timeout.Token));
                var messages = state.RootElement.GetProperty("messages");
                if (messages.GetArrayLength() == 2 && messages[1].GetProperty("status").GetString() == "complete") break;
                await Task.Delay(50, timeout.Token);
            }
            Assert.True(state.RootElement.GetProperty("demo").GetBoolean());
            Assert.Contains("AITeam", state.RootElement.GetProperty("messages")[1].GetProperty("text").GetString());
            Assert.True(state.RootElement.GetProperty("messages")[1].GetProperty("tokens").ValueKind == JsonValueKind.Null);
        }
        finally { state?.Dispose(); }
        var exported = await host.Client.GetAsync("/Chat?handler=Export&conversationId=" + id);
        Assert.Equal("application/json", exported.Content.Headers.ContentType?.MediaType);
        Assert.False(File.Exists(Path.Combine(env.UserProfile, ".aiteam", "hub", "chat.db")));
    }

    [Fact]
    public async Task Invalid_ids_and_remote_host_are_rejected()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, services => services.AddLocalChat(demo: true));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/Chat?handler=State&conversationId=..%2fsecret")).StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Chat?handler=State"); request.Headers.Host = "untrusted.example";
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.SendAsync(request)).StatusCode);
    }

    private static async Task<JsonDocument> Post(HttpClient client, string handler, Dictionary<string, string> values)
    {
        using var response = await client.PostAsync("/Chat?handler=" + handler, new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
