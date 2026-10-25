using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class AcceptanceH701WorkflowRolesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "H701-" + Guid.NewGuid());
    private readonly HubPaths _paths;
    private static readonly string[] Codes = ["receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish"];
    private static readonly string[] Actors = ["claude", "claude", "codex", "codex", "claude", "codex", "minh", "gate", "codex", "claude", "claude"];

    public AcceptanceH701WorkflowRolesTests()
    {
        Directory.CreateDirectory(_root);
        _paths = new HubPaths(_root, Path.Combine(_root, "profile"));
    }

    private WorkflowRolesService Service() => new(_paths);
    private static WorkflowRole[] Defaults() => Codes.Select((c, i) => new WorkflowRole(c, Actors[i], "")).ToArray();
    private void Write(string json, bool bom = false) => File.WriteAllText(_paths.TeamJson, json, new UTF8Encoding(bom));
    private JsonObject Read() => JsonNode.Parse(File.ReadAllText(_paths.TeamJson))!.AsObject();
    private static string Wrap(string roles) => "{\"workflow\":{\"roles\":" + roles + "}}";
    private static string Json(WorkflowRole[] roles) => new JsonArray(roles.Select(r => (JsonNode)new JsonObject
        { ["code"] = r.Code, ["actor"] = r.Actor, ["note"] = r.Note }).ToArray()).ToJsonString();
    private void InvalidSave(IReadOnlyList<WorkflowRole> roles, string? revision = null)
    {
        Write("{}");
        var before = File.ReadAllBytes(_paths.TeamJson);
        var version = Service().GetRoles().Revision;
        var error = Assert.Throws<InvalidDataException>(() => Service().SaveRoles(roles, revision ?? version));
        Assert.Equal("Phân vai không hợp lệ.", error.Message);
        Assert.Equal(before, File.ReadAllBytes(_paths.TeamJson));
        Assert.Single(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"workflow\":{\"other\":1}}")]
    public void Missing_uses_exact_v4_without_writing(string? json)
    {
        if (json is not null) Write(json);
        var snapshot = Service().GetRoles();
        Assert.Equal(Defaults(), snapshot.Roles);
        Assert.Equal(json is null ? "missing" : Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_paths.TeamJson))), json is null ? snapshot.Revision : snapshot.Revision.ToUpperInvariant());
        Assert.Equal(json, File.Exists(_paths.TeamJson) ? File.ReadAllText(_paths.TeamJson) : null);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("42")]
    [InlineData("\"x\"")]
    [InlineData("[null]")]
    public void Present_invalid_roles_are_not_defaults(string roles)
    {
        Write(Wrap(roles));
        var bytes = File.ReadAllBytes(_paths.TeamJson);
        Assert.Equal("Phân vai không hợp lệ.", Assert.Throws<InvalidDataException>(() => Service().GetRoles()).Message);
        Assert.Equal(bytes, File.ReadAllBytes(_paths.TeamJson));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("7")]
    public void Invalid_workflow_is_rejected(string workflow)
    {
        Write("{\"workflow\":" + workflow + "}");
        Assert.Equal("Phân vai không hợp lệ.", Assert.Throws<InvalidDataException>(() => Service().GetRoles()).Message);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    public void Broken_json_read_and_save_preserve_bytes(string json)
    {
        Write("{}");
        var revision = Service().GetRoles().Revision;
        Write(json);
        var before = File.ReadAllBytes(_paths.TeamJson);
        Assert.Equal("Không đọc được cấu hình phân vai.", Assert.Throws<IOException>(() => Service().GetRoles()).Message);
        Assert.Equal("Không đọc được cấu hình phân vai.", Assert.Throws<IOException>(() => Service().SaveRoles(Defaults(), revision)).Message);
        Assert.Equal(before, File.ReadAllBytes(_paths.TeamJson));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("case")]
    [InlineData("null-note")]
    [InlineData("null-row")]
    [InlineData("long-note")]
    public void Invalid_rows_rejected_on_both_read_and_save(string kind)
    {
        var rows = Defaults();
        rows = kind switch
        {
            "actor" => [.. rows.Select((r, i) => i == 0 ? r with { Actor = "../../outside" } : r)],
            "duplicate" => [.. rows.Take(10), rows[0]],
            "missing" => [.. rows.Take(10)],
            "unknown" => [rows[0] with { Code = "unknown" }, .. rows.Skip(1)],
            "case" => [rows[0] with { Actor = "Claude" }, .. rows.Skip(1)],
            "null-note" => [rows[0] with { Note = null! }, .. rows.Skip(1)],
            "null-row" => [null!, .. rows.Skip(1)],
            _ => [rows[0] with { Note = new string('a', 501) }, .. rows.Skip(1)]
        };
        InvalidSave(rows);
        var array = JsonNode.Parse(Json(kind == "null-row" ? Defaults() : rows))!.AsArray();
        if (kind == "null-row") array[0] = null;
        Write(Wrap(array.ToJsonString()));
        Assert.Equal("Phân vai không hợp lệ.", Assert.Throws<InvalidDataException>(() => Service().GetRoles()).Message);
    }

    [Fact]
    public void Null_input_and_empty_revision_rejected()
    {
        InvalidSave(null!);
        InvalidSave(Defaults(), "");
        Write("{}");
        Assert.Equal("Phân vai không hợp lệ.", Assert.Throws<InvalidDataException>(() => Service().SaveRoles(Defaults(), null!)).Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void Notes_roundtrip_without_trimming(int length)
    {
        var rows = Defaults();
        rows[0] = rows[0] with { Note = new string(' ', length) };
        var result = Service().SaveRoles(rows, Service().GetRoles().Revision);
        Assert.Equal(rows, result.Roles);
        Assert.Equal(rows, Service().GetRoles().Roles);
        Assert.Equal(result.Revision, Service().GetRoles().Revision);
        Assert.NotEqual("missing", result.Revision);
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    [InlineData("minh")]
    [InlineData("lan")]
    [InlineData("gate")]
    [InlineData("owner")]
    public void All_six_actors_and_untrusted_note_roundtrip(string actor)
    {
        var note = "<script>alert(1)</script> ../ " + string.Concat("sk-", new string('a', 32));
        var rows = Defaults().Select(r => r with { Actor = actor, Note = note }).ToArray();
        Service().SaveRoles(rows, "missing");
        Assert.Equal(rows, Service().GetRoles().Roles);
        Assert.Single(Directory.GetFiles(_root));
        Assert.False(File.Exists(_paths.TeamLocalJson));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preserve_unknown_keys_and_bom_across_roles_and_settings(bool bom)
    {
        Write("{\"custom\":{\"a\":[1,true,null]},\"workflow\":{\"future\":\"giữ\"},\"agy\":{\"unknown\":42}}", bom);
        var original = Read();
        var snapshot = Service().SaveRoles(Defaults(), Service().GetRoles().Revision);
        var config = new TeamConfigService(_paths);
        config.SaveSettings(config.GetSettings() with { DefaultModel = "mới" });
        var node = Read();
        Assert.True(JsonNode.DeepEquals(original["custom"], node["custom"]));
        Assert.Equal("giữ", node["workflow"]!["future"]!.GetValue<string>());
        Assert.Equal(42, node["agy"]!["unknown"]!.GetValue<int>());
        Assert.Equal("mới", node["agy"]!["model"]!.GetValue<string>());
        Assert.Equal(snapshot.Roles, Service().GetRoles().Roles);
        Assert.Equal(bom, File.ReadAllBytes(_paths.TeamJson).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }));
        Assert.True(File.Exists(_paths.TeamJson + ".bak"));
    }

    [Fact]
    public void External_edit_and_settings_edit_invalidate_revision_without_loss()
    {
        Write("{}");
        var snapshot = Service().GetRoles();
        Write("{\"external\":true}");
        AssertConflict(snapshot);
        snapshot = Service().GetRoles();
        var config = new TeamConfigService(_paths);
        config.SaveSettings(config.GetSettings() with { DefaultModel = "changed" });
        AssertConflict(snapshot);
    }

    private void AssertConflict(WorkflowRolesSnapshot snapshot)
    {
        var before = File.ReadAllBytes(_paths.TeamJson);
        Assert.Equal("Cấu hình đã thay đổi. Vui lòng tải lại.", Assert.Throws<InvalidOperationException>(() => Service().SaveRoles(Defaults(), snapshot.Revision)).Message);
        Assert.Equal(before, File.ReadAllBytes(_paths.TeamJson));
    }

    [Fact]
    public void Locked_destination_keeps_original_and_cleans_temp()
    {
        Write("{}");
        var snapshot = Service().GetRoles();
        var before = File.ReadAllBytes(_paths.TeamJson);
        using (var held = new FileStream(_paths.TeamJson, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Equal("Không lưu được cấu hình phân vai.", Assert.Throws<IOException>(() => Service().SaveRoles(Defaults(), snapshot.Revision)).Message);
        Assert.Equal(before, File.ReadAllBytes(_paths.TeamJson));
        Assert.Single(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Concurrent_instances_allow_only_one_save_for_same_revision()
    {
        Write("{}");
        var revision = Service().GetRoles().Revision;
        using var start = new ManualResetEventSlim();
        Task<Exception> Run(string note) => Task.Run(() =>
        {
            start.Wait();
            return Record.Exception(() => Service().SaveRoles(Defaults().Select(r => r with { Note = note }).ToArray(), revision));
        });
        var first = Run("một");
        var second = Run("hai");
        start.Set();
        var errors = await Task.WhenAll(first, second);
        Assert.Single(errors, e => e is null);
        var error = Assert.Single(errors, e => e is not null);
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Cấu hình đã thay đổi. Vui lòng tải lại.", error.Message);
        var rows = Service().GetRoles().Roles;
        Assert.Contains(rows[0].Note, new[] { "một", "hai" });
        Assert.All(rows, r => Assert.Equal(rows[0].Note, r.Note));
        Assert.Equal(11, rows.Count);
    }

    [Theory]
    [InlineData("code", "null")]
    [InlineData("actor", "42")]
    [InlineData("note", "true")]
    [InlineData("note", null)]
    [InlineData("actor", null)]
    public void Missing_or_wrong_typed_fields_are_rejected(string field, string? json)
    {
        var array = JsonNode.Parse(Json(Defaults()))!.AsArray();
        var row = array[0]!.AsObject();
        if (json is null) row.Remove(field);
        else row[field] = JsonNode.Parse(json);
        Write(Wrap(array.ToJsonString()));
        Assert.Equal("Phân vai không hợp lệ.", Assert.Throws<InvalidDataException>(() => Service().GetRoles()).Message);
    }

    [Fact]
    public async Task Settings_and_map_concurrently_preserve_both_updates()
    {
        Write("{\"custom\":true}");
        var config = new TeamConfigService(_paths);
        var settings = config.GetSettings() with { DefaultModel = "đồng thời" };
        var snapshot = Service().GetRoles();
        var roles = Defaults().Select(r => r with { Note = "mới" }).ToArray();
        using var start = new ManualResetEventSlim();
        var map = Task.Run(() =>
        {
            start.Wait();
            return Record.Exception(() => Service().SaveRoles(roles, snapshot.Revision));
        });
        var save = Task.Run(() => { start.Wait(); config.SaveSettings(settings); });
        start.Set();
        await save;
        var error = await map;
        if (error is not null)
        {
            Assert.IsType<InvalidOperationException>(error);
            Assert.Equal("Cấu hình đã thay đổi. Vui lòng tải lại.", error.Message);
            Service().SaveRoles(roles, Service().GetRoles().Revision);
        }
        Assert.Equal(roles, Service().GetRoles().Roles);
        Assert.Equal("đồng thời", config.GetSettings().DefaultModel);
        Assert.True(Read()["custom"]!.GetValue<bool>());
    }
    public void Dispose() => Directory.Delete(_root, recursive: true);
}