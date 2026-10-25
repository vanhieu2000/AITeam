using AITeam.Core.Skills;

namespace AITeam.Core.Tests;

// Claude 25/10: projects.json THẬT là mảng ở gốc — bản đầu chỉ đọc {"projects": [...]} ⇒ mọi Codex QC bị BLOCKED (JsonException).
public sealed class SkillPromptRealProjectsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-skp-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    [Fact]
    public void Reads_real_array_format_of_projects_json()
    {
        var proj = Directory.CreateDirectory(Path.Combine(_root, "HDVTool")).FullName;
        var work = Directory.CreateDirectory(Path.Combine(_root, "HDVTool-agents", "a-t-001")).FullName;
        foreach (var id in new[] { "09-qc", "code-test", "code-winforms" })
        {
            Directory.CreateDirectory(Path.Combine(_root, "skills", id));
            File.WriteAllText(Path.Combine(_root, "skills", id, "SKILL.md"), "# " + id);
        }
        File.WriteAllText(Path.Combine(_root, "projects.json"),
            "﻿[\n  {\n    \"name\": \"HDVTool\",\n    \"path\": " + System.Text.Json.JsonSerializer.Serialize(proj) +
            ",\n    \"workDir\": " + System.Text.Json.JsonSerializer.Serialize(Path.Combine(_root, "HDVTool-agents")) +
            ",\n    \"stack\": \"C# .NET 10 WinForms, SQLite\"\n  }\n]");

        var text = SkillPromptBuilder.BuildReviewInstructions(_root, work, Path.Combine(work, "tasks", "T-001-x.md"));

        Assert.Contains("code-winforms", text);
        Assert.Contains("09-qc", text);
    }
}
