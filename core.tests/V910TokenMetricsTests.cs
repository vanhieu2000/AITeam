using System.Text.Json;
using AITeam.Core.Process;
using AITeam.Core.Providers;

namespace AITeam.Core.Tests;

public sealed class V910TokenMetricsTests
{
    [Fact]
    public void Archived_real_codex_event_preserves_usage_and_reasoning_without_double_count()
    {
        var sample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "codex-exec-sample.jsonl"));
        var result = CodexProvider.Parse("codex", new ProcessResult(0, sample, "", TimeSpan.FromSeconds(1), false), "Xong", null);
        Assert.True(result.Tokens.IsKnown);
        Assert.Equal(79889, result.Tokens.Input);
        Assert.Equal(934, result.Tokens.Output);
        Assert.Equal(54, result.Tokens.Thinking);
        Assert.Equal(80823, result.Tokens.Total);
        using var json = JsonDocument.Parse(DevRunner.OutcomeJson(new(0, "Succeeded", "", result)));
        Assert.True(json.RootElement.GetProperty("tokensKnown").GetBoolean());
        Assert.Equal(80823, json.RootElement.GetProperty("totalTokens").GetInt64());
        Assert.Equal(54, json.RootElement.GetProperty("thinkingTokens").GetInt64());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"input_tokens\":12}")]
    [InlineData("{\"input_tokens\":-1,\"output_tokens\":2}")]
    [InlineData("{\"input_tokens\":\"bad\",\"output_tokens\":2}")]
    public void Missing_or_invalid_usage_is_unknown_not_zero(string usage)
    {
        var result = CodexProvider.Parse("codex", new ProcessResult(0, "{\"type\":\"turn.completed\",\"usage\":" + usage + "}", "", TimeSpan.Zero, false), "Xong", null);
        Assert.False(result.Tokens.IsKnown);
        using var json = JsonDocument.Parse(DevRunner.OutcomeJson(new(0, "Succeeded", "", result)));
        Assert.False(json.RootElement.GetProperty("tokensKnown").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("totalTokens").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("thinkingTokens").ValueKind);
    }
}
