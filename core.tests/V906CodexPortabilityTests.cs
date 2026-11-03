using System.Text.Json;
using AITeam.Core.Process;
using AITeam.Core.Providers;

namespace AITeam.Core.Tests;

public sealed class V906CodexPortabilityTests
{
    [Fact]
    public void Arguments_keep_read_only_and_quote_base_url_as_toml_string()
    {
        var provider = new CodexProvider(new ProviderConfig { Exe = "codex", BaseUrl = "https://example.test/v1" }, new ProcessRunner());
        var args = provider.BuildArguments(new AgentRunRequest("work", "prompt", RunMode.ReadOnly, TimeSpan.FromMinutes(1)), "last");
        Assert.Contains("windows.sandbox=\"unelevated\"", args);
        Assert.Contains("openai_base_url=" + JsonSerializer.Serialize("https://example.test/v1"), args);
        Assert.Contains("read-only", args);
        Assert.DoesNotContain("--dangerously-bypass-approvals-and-sandbox", args);
    }

    [Fact]
    public void Missing_base_url_does_not_override_user_configuration()
    {
        var provider = new CodexProvider(new ProviderConfig { Exe = "custom-codex" }, new ProcessRunner());
        var args = provider.BuildArguments(new AgentRunRequest("work", "prompt", RunMode.Develop, TimeSpan.FromMinutes(1)), "last");
        Assert.DoesNotContain(args, x => x.Contains("openai_base_url", StringComparison.Ordinal));
        Assert.Equal("custom-codex", provider.ResolveLaunch().Exe);
    }

    [Theory]
    [InlineData("file:///tmp/config")]
    [InlineData("https://user:secret@example.test/v1")]
    public void Invalid_endpoint_is_rejected(string endpoint)
    {
        var provider = new CodexProvider(new ProviderConfig { Exe = "codex", BaseUrl = endpoint }, new ProcessRunner());
        Assert.Throws<InvalidDataException>(() => provider.BuildArguments(new AgentRunRequest("work", "prompt", RunMode.Develop, TimeSpan.FromMinutes(1)), "last"));
    }

    [Fact]
    public void Local_base_url_overrides_team_for_codex_only()
    {
        var root = Path.Combine(Path.GetTempPath(), "v906-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "team.json"), "{\"codex\":{\"baseUrl\":\"https://team.test\"}}");
            File.WriteAllText(Path.Combine(root, "team.local.json"), "{\"codex\":{\"baseUrl\":\"https://local.test\"}}");
            var settings = ProviderSettings.Load(root);
            Assert.Equal("https://local.test", Assert.Single(settings.Providers, p => p.Kind == ProviderKind.Codex).BaseUrl);
            Assert.Null(Assert.Single(settings.Providers, p => p.Kind == ProviderKind.Antigravity).BaseUrl);
        }
        finally { Directory.Delete(root, true); }
    }
}
