using System.Text;
using System.Text.Json.Nodes;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>V10-03 (R-014): loại duyệt nhạy cảm, mức rủi ro, tên khóa trong két và nhật ký Claude tự quyết.</summary>
public sealed class V1003ApprovalPolicyTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private string Dir => Path.Combine(_env.AITeamRoot, "approvals");

    public V1003ApprovalPolicyTests() => Directory.CreateDirectory(Dir);

    public void Dispose() => _env.Dispose();

    private void PutRaw(string id, string json) =>
        File.WriteAllText(Path.Combine(Dir, id + ".json"), json, new UTF8Encoding(false));

    private void PutKind(string id, string kind, string status = "pending", string task = "V10-03")
    {
        PutRaw(id, $$"""
            {
              "id": "{{id}}", "kind": "{{kind}}", "title": "{{kind}} {{id}}", "detail": "",
              "project": "AITeam", "task": "{{task}}", "commit": null, "links": [],
              "requestedBy": "Claude", "requestedAt": "2026-11-03T21:00:00", "status": "{{status}}", "decidedAt": null, "note": null
            }
            """);
    }

    [Fact]
    public void Reads_sensitive_kind_risk_secret_name_and_files()
    {
        PutRaw("A-001", """
            {
              "id": "A-001", "kind": "sensitive-data", "title": "Xuất dữ liệu khách", "detail": "",
              "project": "AITeam", "task": "V10-03", "commit": null, "links": [],
              "requestedBy": "Claude", "requestedAt": "2026-11-03T21:00:00", "status": "pending",
              "decidedAt": null, "note": null, "risk": "high", "secretName": "DB_PASSWORD",
              "files": ["hub/Auth.cs", ".env.local"]
            }
            """);

        var a = new ApprovalService(_env.Paths).Get("A-001");

        Assert.NotNull(a);
        Assert.Equal(ApprovalKind.SensitiveData, a!.Kind);
        Assert.Equal(ApprovalRisk.High, a.Risk);
        Assert.Equal("DB_PASSWORD", a.SecretName);
        Assert.Equal(new[] { "hub/Auth.cs", ".env.local" }, a.Files);
    }

    [Fact]
    public void Old_file_without_risk_defaults_to_medium()
    {
        PutRaw("A-001", """
            { "id": "A-001", "kind": "merge", "title": "Merge cũ", "detail": "",
              "project": "AITeam", "task": "V10-03", "requestedAt": "2026-10-24T01:30:00",
              "status": "pending" }
            """);

        var a = new ApprovalService(_env.Paths).Get("A-001");

        Assert.NotNull(a);
        Assert.Equal(ApprovalKind.Merge, a!.Kind);
        Assert.Equal(ApprovalRisk.Medium, a.Risk);
        Assert.Null(a.SecretName);
        Assert.Empty(a.Files);
    }

    [Fact]
    public void Invalid_secret_name_becomes_null()
    {
        PutRaw("A-001", """
            { "id": "A-001", "kind": "secret", "title": "Cần khóa", "detail": "",
              "project": "AITeam", "task": "V10-03", "requestedAt": "2026-11-03T21:00:00",
              "status": "pending", "risk": "medium", "secretName": "bad name" }
            """);

        var a = new ApprovalService(_env.Paths).Get("A-001");

        Assert.NotNull(a);
        Assert.Equal(ApprovalKind.Secret, a!.Kind);
        Assert.Null(a.SecretName);
    }

    [Fact]
    public void Owner_pending_keeps_five_sensitive_kinds_and_drops_merge_improvement()
    {
        PutKind("A-001", "merge");
        PutKind("A-002", "improvement");
        PutKind("A-003", "secret");
        PutKind("A-004", "security");
        PutKind("A-005", "sensitive-data");
        PutKind("A-006", "external");
        PutKind("A-007", "decision");
        PutKind("A-008", "security", status: "approved");

        var pending = new ApprovalService(_env.Paths).OwnerPending();

        Assert.Equal(new[] { "A-003", "A-004", "A-005", "A-006", "A-007" }, pending.Select(a => a.Id).OrderBy(x => x));
        Assert.All(pending, a => Assert.Equal(ApprovalStatus.Pending, a.Status));
        Assert.False(ApprovalPolicy.RequiresOwner(ApprovalKind.Merge));
        Assert.False(ApprovalPolicy.RequiresOwner(ApprovalKind.Improvement));
        Assert.True(ApprovalPolicy.RequiresOwner(ApprovalKind.Decision));
        Assert.True(ApprovalPolicy.RequiresOwner(ApprovalKind.SensitiveData));
    }

    private string LogFile => Path.Combine(Dir, "claude-decisions.jsonl");

    private void PutLog(params string[] lines) =>
        File.WriteAllText(LogFile, string.Join("\n", lines) + "\n", new UTF8Encoding(false));

    [Fact]
    public void Decision_log_lists_decisions_and_reopen_without_failing_on_broken_line()
    {
        PutLog(
            """{"type":"decision","id":"D-20261103214500-ab12","at":"2026-11-03T21:45:00+07:00","kind":"merge","title":"Merge V10-03","project":"AITeam","task":"V10-03","request":"R-014","detail":"đạt"}""",
            """{"type":"decision","id":"D-20261103215000-cd34","at":"2026-11-03T21:50:00+07:00","kind":"qc","title":"QC đạt","project":"AITeam","task":"V10-03"}""",
            """{"type":"reopen","ref":"D-20261103214500-ab12","note":"Chưa đồng ý","at":"2026-11-03T22:00:00+07:00"}""",
            "{ dòng hỏng");

        var list = new ClaudeDecisionLog(_env.Paths).List();

        Assert.Equal(2, list.Count);
        var reopened = Assert.Single(list, d => d.Id == "D-20261103214500-ab12");
        Assert.True(reopened.Reopened);
        Assert.Equal("Chưa đồng ý", reopened.ReopenNote);
        Assert.NotNull(reopened.ReopenedAt);
        var plain = Assert.Single(list, d => d.Id == "D-20261103215000-cd34");
        Assert.False(plain.Reopened);
        Assert.Null(plain.ReopenNote);
    }

    [Fact]
    public void Reopen_appends_a_line_and_validation_errors_are_reported()
    {
        PutLog("""{"type":"decision","id":"D-20261103214500-ab12","at":"2026-11-03T21:45:00+07:00","kind":"merge","title":"Merge V10-03","project":"AITeam","task":"V10-03"}""");
        var log = new ClaudeDecisionLog(_env.Paths);

        var reopened = log.Reopen("D-20261103214500-ab12", "  Mở lại để xem  ");

        Assert.True(reopened.Reopened);
        Assert.Equal("Mở lại để xem", reopened.ReopenNote);
        Assert.True(log.List().Single(d => d.Id == "D-20261103214500-ab12").Reopened);

        var lines = File.ReadAllLines(LogFile, Encoding.UTF8);
        Assert.Equal(2, lines.Length);
        Assert.Equal("reopen", (string?)JsonNode.Parse(lines[1])!["type"]);
        Assert.Equal("D-20261103214500-ab12", (string?)JsonNode.Parse(lines[1])!["ref"]);

        Assert.Throws<ArgumentException>(() => log.Reopen("D-1", "x"));
        Assert.Throws<ArgumentException>(() => log.Reopen("D-20261103214500-ab12", "   "));
        Assert.Throws<KeyNotFoundException>(() => log.Reopen("D-20260101000000-zz99", "x"));
    }
}
