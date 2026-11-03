using System.Text;
using AITeam.Core.AiServices;

namespace AITeam.Core.Tests;

// Test nghiệm thu V10-01: danh bạ dịch vụ AI trong SQLite cục bộ + xuất / nhập .ini.
public sealed class V1001AiServiceRegistryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aiteam-v1001-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(_dir, "root");
    private string Db => Path.Combine(_dir, "hub", "office.db");

    public V1001AiServiceRegistryTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static AiService Service(
        string id, string? command = "codex", AiAdapter adapter = AiAdapter.CliJson,
        string? baseUrl = null, string? name = null) =>
        new(id, name ?? "Dịch vụ " + id, adapter, command, ["--json"], baseUrl, ["model-1"],
            [AiRole.Code], 1, false, null, null, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static void AssertError(AiServiceValidation validation, string contains)
    {
        Assert.False(validation.Ok);
        Assert.Contains(validation.Errors, e => e.Contains(contains, StringComparison.Ordinal));
    }

    private static object Shape(AiService service) => new
    {
        service.Id,
        service.Name,
        service.Adapter,
        service.Command,
        Args = string.Join('\u0001', service.Args),
        service.BaseUrl,
        Models = string.Join('\u0001', service.Models),
        Roles = string.Join(',', service.Roles),
        service.MaxJobs,
        service.Paid,
        service.SecretKeyName,
        service.ManageUrl,
        service.Enabled,
    };

    [Fact]
    public void First_open_creates_schema_and_imports_legacy_once()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "team.json"), """
            {
              "projectsRoot": "D:\\",
              "providers": [
                { "id": "codex", "kind": "Codex", "exe": "codex", "model": "gpt-6.1-sol",
                  "roles": ["Developer", "Reviewer"], "maxConcurrent": 1, "enabled": true },
                { "id": "opencode", "kind": "OpenCode", "exe": "opencode",
                  "roles": ["Developer"], "maxConcurrent": 2, "enabled": true }
              ]
            }
            """);
        File.WriteAllText(Path.Combine(Root, "team.local.json"), """
            { "providers": { "codex": { "exe": "C:\\Tools\\codex\\codex.exe" } } }
            """);
        var accounts = Path.Combine(_dir, "accounts.json");
        File.WriteAllText(accounts, """
            { "version": 1, "accounts": [
              { "id": "codex", "alias": "Codex chính", "mode": "Automatic" },
              { "id": "gemini", "alias": "Google cá nhân", "mode": "Manual" }
            ] }
            """);

        int count;
        using (var db = new SqliteAiServiceRegistry(Db, Root, accounts))
        {
            var all = db.List();
            count = all.Count;
            Assert.True(all.Single(s => s.Id == "codex").Enabled);
            Assert.True(all.Single(s => s.Id == "opencode").Enabled);
            Assert.Equal("C:\\Tools\\codex\\codex.exe", all.Single(s => s.Id == "codex").Command);
            Assert.Equal(["gpt-6.1-sol"], all.Single(s => s.Id == "codex").Models);
            Assert.Equal(2, all.Single(s => s.Id == "opencode").MaxJobs);

            foreach (var id in new[] { "antigravity", "chatgpt", "gemini", "claude", "deepseek", "grok" })
                Assert.False(all.Single(s => s.Id == id).Enabled, id);

            Assert.Contains(db.Accounts("codex"), a => a.Alias == "Codex chính" && a.Mode == "Automatic");
            Assert.Contains(db.Accounts(), a => a.ServiceId == "gemini" && a.Alias == "Google cá nhân");
            Assert.False(string.IsNullOrWhiteSpace(db.GetSetting("migrated_at")));
        }

        using (var again = new SqliteAiServiceRegistry(Db, Root, accounts))
            Assert.Equal(count, again.List().Count);
    }

    [Fact]
    public void Upsert_get_list_and_disable_keep_row()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        var saved = db.Upsert(Service("codex", name: "Codex"));
        Assert.Equal("Codex", saved.Name);

        db.Upsert(saved with { Name = "Codex đổi tên" });
        Assert.Equal("Codex đổi tên", db.Get("codex")!.Name);
        Assert.Single(db.List());

        db.SetEnabled("codex", false);
        Assert.Empty(db.List(false));
        Assert.Single(db.List(true));
        Assert.False(db.Get("codex")!.Enabled);
    }

    [Fact]
    public void Validate_reports_vietnamese_errors()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        var good = Service("codex");
        Assert.True(db.Validate(good).Ok);

        AssertError(db.Validate(good with { Id = "X" }), "Mã dịch vụ");
        AssertError(db.Validate(good with { Id = "a" }), "Mã dịch vụ");
        AssertError(db.Validate(good with { Command = "codex & calc" }), "shell");
        AssertError(db.Validate(good with { Name = "   " }), "Tên dịch vụ");
        AssertError(db.Validate(good with { SecretKeyName = "abc" }), "Tên khóa");
        AssertError(db.Validate(good with { MaxJobs = 0 }), "1 đến 8");
        AssertError(db.Validate(good with { Roles = [] }), "vai trò");

        var http = good with { Adapter = AiAdapter.OpenAiCompatibleHttp, Command = null };
        AssertError(db.Validate(http with { BaseUrl = "http://evil.com" }), "https://");
        AssertError(db.Validate(http with { BaseUrl = "https://user:pass@api.example.com" }), "tài khoản");
        AssertError(db.Validate(http with { BaseUrl = "https://api.example.com", ManageUrl = "http://evil.com" }), "https://");
    }

    [Fact]
    public void Export_import_roundtrip_on_empty_db()
    {
        using var source = new SqliteAiServiceRegistry(":memory:", Root);
        source.Upsert(Service("alpha", command: "codex", name: "Alpha"));
        source.Upsert(new AiService("beta", "Beta", AiAdapter.OpenAiCompatibleHttp, null, [],
            "https://api.example.com/v1", ["m-a", "m-b"], [AiRole.Research, AiRole.Chat], 3, true,
            "BETA_API_KEY", "https://example.com/quan-ly", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var ini = source.ExportIni();
        using var target = new SqliteAiServiceRegistry(":memory:", Root);
        var result = target.ImportIni(ini);

        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.Empty(result.Skipped);
        foreach (var expected in source.List())
            Assert.Equal(Shape(expected), Shape(target.Get(expected.Id)!));

        var again = target.ImportIni(ini);
        Assert.Equal(0, again.Added);
        Assert.Equal(2, again.Updated);
    }

    [Fact]
    public void Import_drops_secret_keys_and_never_stores_values()
    {
        var secret = string.Concat("sk-", new string('a', 32));
        var ini = $"""
            [service:alpha]
            name=Alpha
            adapter=cli-json
            command=codex
            args=--json
            roles=code,qc
            max_jobs=1
            paid=false
            enabled=true
            api_key={secret}
            token={secret}
            """;

        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        var result = db.ImportIni(ini);
        Assert.Equal(1, result.Added);
        Assert.Contains(result.Skipped, s => s.Contains("api_key", StringComparison.Ordinal));
        Assert.Contains(result.Skipped, s => s.Contains("token", StringComparison.Ordinal));
        Assert.Equal(["code", "qc"], db.Get("alpha")!.Roles.Select(r => r == AiRole.Code ? "code" : "qc"));
        Assert.Null(db.GetSetting("api_key"));

        var bad = db.ImportIni("""
            [service:Bad]
            name=Bad
            adapter=cli-json
            command=codex
            roles=code
            """);
        Assert.Equal(0, bad.Added);
        Assert.Contains(bad.Skipped, s => s.Contains("Mã dịch vụ", StringComparison.Ordinal));

        // Đọc lại toàn bộ file DB (kể cả WAL / SHM): không được thấy chuỗi bí mật ở đâu.
        var needle = Encoding.UTF8.GetBytes(secret);
        var bytes = Directory.GetFiles(_dir, "*", SearchOption.AllDirectories).SelectMany(File.ReadAllBytes).ToArray();
        Assert.False(ContainsSequence(bytes, needle));
    }

    [Fact]
    public void Import_rejects_more_than_256_kb()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        var tooBig = new string('x', SqliteAiServiceRegistry.MaxIniBytes + 1);
        Assert.Throws<ArgumentException>(() => db.ImportIni(tooBig));
    }

    [Fact]
    public async Task Concurrent_upserts_keep_all_rows()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        static void Lane(SqliteAiServiceRegistry registry, int lane)
        {
            for (var i = 0; i < 50; i++) registry.Upsert(Service($"t{lane}-{i:00}", name: $"Dịch vụ {lane}-{i}"));
        }
        await Task.WhenAll(Task.Run(() => Lane(db, 0)), Task.Run(() => Lane(db, 1)));
        Assert.Equal(100, db.List().Count);
    }

    [Fact]
    public void Settings_get_set_and_null()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        Assert.Null(db.GetSetting("assistant"));
        db.SetSetting("assistant", "codex");
        Assert.Equal("codex", db.GetSetting("assistant"));
        db.SetSetting("assistant", null);
        Assert.Null(db.GetSetting("assistant"));
    }

    [Fact]
    public void Quotas_keep_latest_per_service_and_account()
    {
        using var db = new SqliteAiServiceRegistry(":memory:", Root);
        db.Upsert(Service("codex"));
        var now = DateTimeOffset.UtcNow;
        db.RecordQuota(new AiQuota("codex", null, 80, null, "probe", now.AddMinutes(-5)));
        db.RecordQuota(new AiQuota("codex", null, 40, now.AddHours(1), "probe", now));
        db.RecordQuota(new AiQuota("codex", "acc-1", 25, null, "probe", now));

        var rows = db.LatestQuotas();
        Assert.Equal(2, rows.Count);
        Assert.Equal(40, rows.Single(q => q.AccountId == null).RemainingPct);
        Assert.Equal(25, rows.Single(q => q.AccountId == "acc-1").RemainingPct);
    }

    [Fact]
    public void Memory_database_does_not_create_files()
    {
        using (var db = new SqliteAiServiceRegistry(":memory:", Root))
        {
            db.Upsert(Service("codex"));
            Assert.StartsWith("Trong bộ nhớ", db.Location, StringComparison.Ordinal);
        }
        Assert.Empty(Directory.GetFiles(_dir, "*", SearchOption.AllDirectories));
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { match = false; break; }
            if (match) return true;
        }
        return false;
    }
}
