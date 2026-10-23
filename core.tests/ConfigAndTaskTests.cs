using AITeam.Core.Providers;
using AITeam.Core.Tasks;

namespace AITeam.Core.Tests;

public sealed class ConfigAndTaskTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("aiteam-core-").FullName;
    public void Dispose() => Directory.Delete(_dir, true);

    private void Write(string name, string json) => File.WriteAllText(Path.Combine(_dir, name), json);

    [Fact]
    public void Without_providers_section_defaults_to_agy_developer_and_codex_reviewer()
    {
        Write("team.json", """{ "agy": { "exe": "C:\\Tools\\agy\\bin\\agy.exe" } }""");
        var s = ProviderSettings.Load(_dir);
        Assert.Equal(["agy", "codex"], s.Routing[AgentRole.Developer]);
        Assert.Equal(["codex"], s.Routing[AgentRole.Reviewer]);
        Assert.Equal(@"C:\Tools\agy\bin\agy.exe", s.Providers.Single(p => p.Id == "agy").Exe);
        Assert.Equal(2, s.Providers.Single(p => p.Id == "agy").MaxConcurrent);
    }

    [Fact]
    public void Local_file_overrides_exe_per_machine()
    {
        Write("team.json", """{ "agy": { "exe": "C:\\a.exe" } }""");
        Write("team.local.json", """{ "providers": { "codex": { "exe": "E:\\codex.js", "enabled": false } } }""");
        var codex = ProviderSettings.Load(_dir).Providers.Single(p => p.Id == "codex");
        Assert.Equal(@"E:\codex.js", codex.Exe);
        Assert.False(codex.Enabled);
    }

    [Fact]
    public void Routing_to_unknown_provider_fails_loudly()
    {
        Write("team.json", """{ "routing": { "Reviewer": ["khongco"] } }""");
        Assert.Throws<InvalidDataException>(() => ProviderSettings.Load(_dir));
    }

    [Fact]
    public void Broken_json_fails_instead_of_silent_defaults()
    {
        Write("team.json", """{ "agy": { "exe": """);
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ProviderSettings.Load(_dir));
    }

    [Fact]
    public void Duplicate_provider_ids_rejected()
    {
        Write("team.json", """{ "providers": [ { "id": "x", "kind": "Codex" }, { "id": "X", "kind": "Codex" } ] }""");
        Assert.Throws<InvalidDataException>(() => ProviderSettings.Load(_dir));
    }

    [Fact]
    public void Task_file_fields_and_sections_are_parsed()
    {
        var text = """
            # T-009-demo · Thử đọc task

            - **Agent:** B
            - **Nhánh:** `agent-b/T-009-demo`
            - **Ưu tiên:** cao
            - **Ngân sách:** ~400 dòng
            - **Yêu cầu:** R-002
            - **Thời gian tối đa:** 25 phút
            - **Model:** `gemini-3.8-flash-medium`

            ## Tóm tắt
            - Không có chức năng mới.

            ## Tiêu chí hoàn thành
            - [ ] Build 0 warning
            """;
        var t = TaskFile.Parse("T-009-demo.md", text);
        Assert.Equal("T-009-demo", t.Id);
        Assert.Equal("Thử đọc task", t.Title);
        Assert.Equal("B", t.Agent);
        Assert.Equal("agent-b/T-009-demo", t.Branch);
        Assert.Equal("R-002", t.Request);
        Assert.Equal(400, t.BudgetLines);
        Assert.Equal(25, t.MaxMinutes);
        Assert.Equal("gemini-3.8-flash-medium", t.Model);
        Assert.Contains("Không có chức năng mới", t.Section("Tóm tắt"));
        Assert.Contains("Build 0 warning", t.Section("Tiêu chí"));
    }

    [Fact]
    public void Template_placeholder_request_is_not_taken_as_real()
    {
        var t = TaskFile.Parse("x.md", "# T-1 · a\n- **Yêu cầu:** R-xxx\n");
        Assert.Null(t.Request);
    }
}
