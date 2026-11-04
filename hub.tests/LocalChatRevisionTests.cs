using System.Text.Json;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

public sealed class LocalChatRevisionTests
{
    [Fact]
    public async Task Office_shows_only_latest_shortcut_after_workspace_and_chat_has_active_menu()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, Configure);
        var store = host.Services.GetRequiredService<LocalChatStore>();
        var id = store.CreateConversation();
        store.Enqueue(id, "Hội thoại gần nhất <script>không chạy</script>", Guid.NewGuid().ToString("N"));
        var html = await host.Client.GetStringAsync("/");
        Assert.DoesNotContain("id=\"local-chat\"", html);
        Assert.DoesNotContain("local-chat.js", html);
        // R-014 (Claude, chủ dự án duyệt 03/11): lối tắt "Trò chuyện AI" trên Văn phòng thay bằng nút chat nổi có trên mọi trang.
        Assert.DoesNotContain("<script>không chạy</script>", html);
        Assert.Contains("id=\"fab\"", html);
        var chat = await host.Client.GetStringAsync("/Chat");
        // R-014 (Claude, chủ dự án duyệt 03/11): Trò chuyện không còn trong menu, mở bằng nút chat nổi trên mọi trang.
        Assert.DoesNotContain("<span>Trò chuyện AI</span>", chat);
        Assert.Contains("id=\"fab\"", chat);
        // R-014 UI-09: khung chat nối thật (không còn iframe nhúng /Chat).
        Assert.Contains("id=\"chatp\"", chat);
        Assert.Contains("chat-panel.js", chat);
    }

    [Fact]
    public async Task Token_summary_counts_all_measured_turns_not_just_displayed_messages()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, Configure);
        var store = host.Services.GetRequiredService<LocalChatStore>();
        var id = store.CreateConversation();
        for (var i = 0; i < 102; i++)
        {
            store.Enqueue(id, "Câu hỏi " + i, Guid.NewGuid().ToString("N"));
            var turn = Assert.IsType<ChatTurn>(store.Claim());
            store.Finish(turn.MessageId, "Trả lời", null, i == 101 ? null : 10);
        }
        using var state = JsonDocument.Parse(await host.Client.GetStringAsync("/Chat?handler=State&conversationId=" + id));
        var summary = state.RootElement.GetProperty("tokenSummary");
        Assert.Equal(1010, summary.GetProperty("totalTokens").GetInt64());
        Assert.Equal(1, summary.GetProperty("unmeasuredTurns").GetInt32());
        Assert.Equal(200, state.RootElement.GetProperty("messages").GetArrayLength());
        var html = await host.Client.GetStringAsync("/Chat");
        Assert.Contains("id=\"lc-token-summary\"", html);
        Assert.Contains("Mỗi tin nhắn qua Codex CLI tốn khoảng 13.000 token đầu vào do hướng dẫn hệ thống của CLI.", html);
    }

    [Fact]
    public void Codex_path_requires_existing_file_in_path_or_npm_tree()
    {
        using var env = new TestEnvironment();
        var bin = Path.Combine(env.RootDir, "bin"); Directory.CreateDirectory(bin);
        var exe = Path.Combine(bin, "codex.exe"); File.WriteAllText(exe, "fixture");
        ChatSettings.ValidateCodexPath(exe, bin, env.UserProfile);
        Assert.Throws<ArgumentException>(() => ChatSettings.ValidateCodexPath(Path.Combine(bin, "codex.js"), bin, env.UserProfile));
        Assert.Throws<ArgumentException>(() => ChatSettings.ValidateCodexPath(exe, Path.Combine(env.RootDir, "other"), env.UserProfile));
        var nested = Path.Combine(env.UserProfile, "npm", "node_modules", "@openai", "codex", "bin"); Directory.CreateDirectory(nested);
        var js = Path.Combine(nested, "codex.js"); File.WriteAllText(js, "fixture");
        ChatSettings.ValidateCodexPath(js, "", env.UserProfile);
        Assert.Throws<ArgumentException>(() => ChatSettings.ValidateCodexPath(Path.Combine(nested, "..", "..", "..", "..", "..", "bin", "codex.exe"), "", env.UserProfile));
    }

    private static void Configure(IServiceCollection services)
    {
        services.AddLocalChat(true);
        services.Remove(services.Single(d => d.ImplementationType == typeof(LocalChatWorker)));
    }

    [Fact]
    public void Missing_saved_executable_does_not_hide_history_but_cannot_be_saved_again()
    {
        using var env = new TestEnvironment();
        var file = Path.Combine(env.UserProfile, "chat.db");
        using var store = new LocalChatStore(file);
        var settings = new ChatSettings { CodexPath = Path.Combine(env.RootDir, "removed", "codex.exe") };
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = file }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO chat_settings(id,json) VALUES(1,$json)";
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings));
        command.ExecuteNonQuery();
        Assert.Equal(settings.CodexPath, store.Settings().CodexPath);
        Assert.Throws<ArgumentException>(() => store.SaveSettings(settings));
    }
}
