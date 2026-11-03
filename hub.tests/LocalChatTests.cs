using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class LocalChatTests
{
    [Fact]
    public void History_and_settings_survive_reopening_database()
    {
        using var env = new TestEnvironment();
        var file = Path.Combine(env.UserProfile, "chat.db");
        string id;
        using (var db = new LocalChatStore(file))
        {
            id = db.CreateConversation();
            db.SaveSettings(new ChatSettings { Model = "gpt-example", TimeoutSeconds = 90 });
            db.Enqueue(id, "Nhớ tôi đang làm AITeam", Guid.NewGuid().ToString("N"));
            var turn = Assert.IsType<ChatTurn>(db.Claim());
            db.Finish(turn.MessageId, "Đã ghi nhớ.", null, 123);
        }
        using var reopened = new LocalChatStore(file);
        Assert.Equal("gpt-example", reopened.Settings().Model);
        var messages = reopened.Messages(id);
        Assert.Equal(2, messages.Count);
        Assert.Equal("Nhớ tôi đang làm AITeam", messages[0].Text);
        Assert.Equal("Đã ghi nhớ.", messages[1].Text);
        Assert.Equal(123, messages[1].Tokens);
    }

    [Fact]
    public void Duplicate_submission_is_idempotent_and_busy_conversation_is_rejected()
    {
        using var db = new LocalChatStore(":memory:");
        var id = db.CreateConversation();
        var key = Guid.NewGuid().ToString("N");
        var first = db.Enqueue(id, "Xin chào", key);
        Assert.Equal(first, db.Enqueue(id, "Xin chào", key));
        Assert.Throws<ArgumentException>(() => db.Enqueue(id, "Nội dung khác", key));
        Assert.Throws<InvalidOperationException>(() => db.Enqueue(id, "Lần hai", Guid.NewGuid().ToString("N")));
        Assert.Equal(2, db.Messages(id).Count);
    }

    [Fact]
    public void Failed_or_cancelled_turns_are_excluded_from_ai_memory()
    {
        using var db = new LocalChatStore(":memory:");
        var id = db.CreateConversation();
        db.Enqueue(id, "Câu hỏi đầu", Guid.NewGuid().ToString("N"));
        var turn = Assert.IsType<ChatTurn>(db.Claim());
        db.Finish(turn.MessageId, "", "Chưa đăng nhập", null);
        db.Enqueue(id, "Câu hỏi mới", Guid.NewGuid().ToString("N"));
        turn = Assert.IsType<ChatTurn>(db.Claim());
        Assert.Single(turn.Context);
        Assert.Equal("Câu hỏi mới", turn.Context[0].Text);
        db.Cancel(id);
        db.Finish(turn.MessageId, "Phản hồi muộn", null, 10);
        Assert.Equal("cancelled", db.Messages(id)[^1].Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    public void Invalid_conversation_never_becomes_a_path(string id)
    {
        using var db = new LocalChatStore(":memory:");
        Assert.Throws<ArgumentException>(() => db.Messages(id));
    }

    [Fact]
    public void Settings_reject_shell_arguments_and_untrusted_proxy_urls()
    {
        using var db = new LocalChatStore(":memory:");
        Assert.Throws<ArgumentException>(() => db.SaveSettings(new ChatSettings { Model = "gpt; echo bad" }));
        Assert.Throws<ArgumentException>(() => db.SaveSettings(new ChatSettings { Model = "--dangerously-bypass-approvals-and-sandbox" }));
        Assert.Throws<ArgumentException>(() => db.SaveSettings(new ChatSettings { BaseUrl = "http://example.com" }));
        Assert.Throws<ArgumentException>(() => db.SaveSettings(new ChatSettings { TimeoutSeconds = 0 }));
        Assert.Throws<ArgumentException>(() => db.SaveSettings(new ChatSettings { CodexPath = "powershell.exe" }));
        Assert.Equal("", db.Settings().Model);
    }

    [Fact]
    public void Deleting_conversation_removes_messages_but_not_settings()
    {
        using var db = new LocalChatStore(":memory:");
        var id = db.CreateConversation();
        db.SaveSettings(new ChatSettings { Model = "gpt-example" });
        db.Enqueue(id, "Một câu hỏi", Guid.NewGuid().ToString("N"));
        db.Cancel(id);
        db.Delete(id);
        Assert.Empty(db.Conversations());
        Assert.Throws<KeyNotFoundException>(() => db.Messages(id));
        Assert.Equal("gpt-example", db.Settings().Model);
    }

    [Fact]
    public void Interrupted_response_is_visible_after_restart()
    {
        using var env = new TestEnvironment();
        var file = Path.Combine(env.UserProfile, "chat.db");
        string id;
        using (var db = new LocalChatStore(file))
        {
            id = db.CreateConversation();
            db.Enqueue(id, "Đang hỏi", Guid.NewGuid().ToString("N"));
            Assert.NotNull(db.Claim());
        }
        using var reopened = new LocalChatStore(file);
        Assert.Equal("error", reopened.Messages(id)[^1].Status);
        Assert.Contains("gián đoạn", reopened.Messages(id)[^1].Error);
        Assert.Null(reopened.Claim());
    }

    [Fact]
    public void Queued_turn_keeps_model_and_instructions_at_submission_time()
    {
        using var db = new LocalChatStore(":memory:");
        var id = db.CreateConversation();
        db.SaveSettings(new ChatSettings { Model = "model-one", Instructions = "Nhớ dự án AITeam" });
        db.Enqueue(id, "Xin chào", Guid.NewGuid().ToString("N"));
        db.SaveSettings(new ChatSettings { Model = "model-two" });
        var turn = Assert.IsType<ChatTurn>(db.Claim());
        Assert.Equal("model-one", turn.Settings.Model);
        Assert.Contains("AITeam", turn.Settings.Instructions);
    }

    [Fact]
    public void Second_hub_cannot_interrupt_first_hubs_database()
    {
        using var env = new TestEnvironment();
        var file = Path.Combine(env.UserProfile, "chat.db");
        using var first = new LocalChatStore(file);
        var id = first.CreateConversation();
        first.Enqueue(id, "Đang hỏi", Guid.NewGuid().ToString("N"));
        Assert.NotNull(first.Claim());
        Assert.Throws<IOException>(() => new LocalChatStore(file));
        Assert.Equal("generating", first.Messages(id)[^1].Status);
    }
}
