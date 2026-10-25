using System.Text;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V5-06 (Claude viết trước — agent KHÔNG sửa). Tài khoản AI: chỉ metadata, trạng thái chuẩn.
public sealed class AcceptanceV506AccountsTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 25, 15, 0, 0);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aiteam-acc-" + Guid.NewGuid().ToString("N"));
    private string File_ => Path.Combine(_dir, "config", "accounts.json");
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    // ───────────── Danh mục + kho ─────────────

    [Fact]
    public void Catalog_is_fixed()
    {
        Assert.Equal(new[] { "antigravity", "codex", "chatgpt", "gemini", "claude", "deepseek", "grok" }, AiCatalog.All.Select(s => s.Id));
        Assert.Equal("gemini", AiCatalog.Find("GEMINI")!.Id);
        Assert.Null(AiCatalog.Find("../x"));
        Assert.Null(AiCatalog.Find(null));
    }

    [Fact]
    public void Load_without_file_gives_defaults_in_catalog_order()
    {
        var rows = new AiAccountStore(File_).Load();
        Assert.Equal(AiCatalog.All.Select(s => (s.Id, "", s.DefaultMode)), rows.Select(r => (r.Id, r.Alias, r.Mode)));
        Assert.False(File.Exists(File_));   // đọc không tạo file
    }

    [Theory]
    [InlineData("không phải json")]
    [InlineData("{\"version\":1,\"accounts\":null}")]
    [InlineData("[]")]
    [InlineData("")]
    public void Load_bad_file_gives_defaults(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
        File.WriteAllText(File_, content);
        Assert.Equal(AiCatalog.All.Select(s => s.DefaultMode), new AiAccountStore(File_).Load().Select(r => r.Mode));
    }

    [Fact]
    public void Load_cleans_and_ignores_unknown_and_duplicates()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
        File.WriteAllText(File_, """
            {"version":1,"accounts":[
              {"id":"gemini","alias":"  Google\u0007 cá nhân  ","mode":"automatic"},
              {"id":"gemini","alias":"thứ hai","mode":"Disabled"},
              {"id":"evil","alias":"x","mode":"Automatic","password":"hunter2"},
              {"id":"deepseek","alias":"ds","mode":"Automatic"},
              {"id":"codex","alias":"a","mode":"7"},
              {"id":"grok","alias":"g","mode":"Banana"}
            ]}
            """);
        var rows = new AiAccountStore(File_).Load().ToDictionary(r => r.Id);
        Assert.Equal(7, rows.Count);
        Assert.Equal(("Google cá nhân", AiMode.Automatic), (rows["gemini"].Alias, rows["gemini"].Mode));
        Assert.Equal(AiMode.Manual, rows["deepseek"].Mode);   // chưa có tự động ⇒ Manual
        Assert.Equal(AiMode.Automatic, rows["codex"].Mode);   // "7" không nhận ⇒ mặc định
        Assert.Equal(AiMode.Disabled, rows["grok"].Mode);
    }

    [Fact]
    public void Save_writes_only_metadata_for_all_services()
    {
        var store = new AiAccountStore(File_);
        var saved = store.Save("GEMINI", "Google\r\ncá nhân", AiMode.Automatic);
        Assert.Equal(("gemini", "Googlecá nhân", AiMode.Automatic), (saved.Id, saved.Alias, saved.Mode));
        store.Save("codex", null, AiMode.Disabled);

        var bytes = File.ReadAllBytes(File_);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB, "không BOM");
        var json = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"version\": 1", json);
        Assert.Contains("\"mode\": \"Automatic\"", json);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var accounts = doc.RootElement.GetProperty("accounts").EnumerateArray().ToList();
        Assert.Equal(AiCatalog.All.Select(s => s.Id), accounts.Select(a => a.GetProperty("id").GetString()));
        Assert.All(accounts, a => Assert.Equal(new[] { "id", "alias", "mode" }, a.EnumerateObject().Select(p => p.Name)));

        var again = new AiAccountStore(File_).Load().ToDictionary(r => r.Id);
        Assert.Equal(AiMode.Automatic, again["gemini"].Mode);
        Assert.Equal(AiMode.Disabled, again["codex"].Mode);
        Assert.Equal(new[] { File_ }, Directory.GetFiles(Path.GetDirectoryName(File_)!));   // không để lại file tạm
    }

    [Fact]
    public void Save_rejects_unknown_and_unsupported()
    {
        var store = new AiAccountStore(File_);
        Assert.Equal("Dịch vụ không hợp lệ.", Assert.Throws<ArgumentException>(() => store.Save("..\\evil", "x", AiMode.Manual)).Message);
        Assert.Equal("DeepSeek chưa hỗ trợ chế độ tự động.", Assert.Throws<InvalidOperationException>(() => store.Save("deepseek", "x", AiMode.Automatic)).Message);
        Assert.False(File.Exists(File_));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  a\tb  ", "ab")]
    [InlineData("1234567890123456789012345678901234567890XYZ", "1234567890123456789012345678901234567890")]
    [InlineData("123456789012345678901234567890123456789 Z", "123456789012345678901234567890123456789")]
    public void Clean_alias(string? input, string expected) => Assert.Equal(expected, AiAccountStore.CleanAlias(input));

    // ───────────── Trạng thái ─────────────

    private static AiService S(string id) => AiCatalog.Find(id)!;
    private static AiAccountSetting Set(string id, AiMode m, string alias = "") => new(id, alias, m);
    private static readonly AiCheck NotChecked = new(null, null, null);
    private static AiCheck Probe(bool installed, bool? logged, string detail = "ok") => new(new ProviderHealth("x", installed, logged, detail), null, Now);

    public static IEnumerable<object?[]> Cases() => new[]
    {
        new object?[] { "gemini", AiMode.Disabled, Probe(true, true), null, false, AiStatus.Disconnected, "Đang tắt" },
        new object?[] { "gemini", AiMode.Manual, Probe(true, true), null, false, AiStatus.ManualOnly, "Thủ công (copy / dán)" },
        new object?[] { "grok", AiMode.Automatic, Probe(true, true), null, false, AiStatus.Unsupported, "Chưa hỗ trợ tự động" },
        new object?[] { "codex", AiMode.Automatic, Probe(true, true), new ProviderRest("codex", new DateTime(2026, 10, 25, 16, 19, 0), RunStatus.QuotaExhausted, "", Now), false, AiStatus.RateLimited, "Nghỉ tới 16:19 25/10" },
        new object?[] { "codex", AiMode.Automatic, Probe(true, true), new ProviderRest("codex", Now.AddMinutes(-1), RunStatus.QuotaExhausted, "", Now), false, AiStatus.Connected, "Đã kết nối" },
        new object?[] { "antigravity", AiMode.Automatic, Probe(true, true), null, true, AiStatus.Busy, "Đang chạy việc" },
        new object?[] { "antigravity", AiMode.Automatic, new AiCheck(null, "Quá 30 giây", Now), null, false, AiStatus.Error, "Lỗi kiểm tra" },
        new object?[] { "antigravity", AiMode.Automatic, NotChecked, null, false, AiStatus.Disconnected, "Chưa kiểm tra" },
        new object?[] { "gemini", AiMode.Automatic, Probe(false, null), null, false, AiStatus.Unsupported, "Chưa cài Antigravity (agy · model Gemini)" },
        new object?[] { "gemini", AiMode.Automatic, Probe(true, false), null, false, AiStatus.LoginRequired, "Cần đăng nhập" },
        new object?[] { "claude", AiMode.Automatic, Probe(true, null), null, false, AiStatus.Disconnected, "Chưa xác minh đăng nhập" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Resolve_rules(string id, AiMode mode, AiCheck check, ProviderRest? rest, bool busy, AiStatus status, string text)
    {
        var v = AiStatusResolver.Resolve(S(id), Set(id, mode, "bí danh"), check, rest, busy, Now);
        Assert.Equal((status, text), (v.Status, v.StatusText));
        Assert.Equal((id, "bí danh", mode, check.CheckedAt), (v.Service.Id, v.Alias, v.Mode, v.CheckedAt));
    }

    [Fact]
    public void Resolve_detail_is_first_line_clipped()
    {
        var v = AiStatusResolver.Resolve(S("codex"), Set("codex", AiMode.Automatic), Probe(true, true, "\n\n  Logged in using ChatGPT\nsecond line"), null, false, Now);
        Assert.Equal("Logged in using ChatGPT", v.Detail);
        var err = AiStatusResolver.Resolve(S("codex"), Set("codex", AiMode.Automatic), new AiCheck(Probe(true, true).Probe, new string('e', 300), Now), null, false, Now);
        Assert.Equal(new string('e', 160), err.Detail);
        Assert.Equal("", AiStatusResolver.Resolve(S("codex"), Set("codex", AiMode.Automatic), NotChecked, null, false, Now).Detail);
    }

    // ───────────── CLI không có lệnh login status ─────────────

    private string Bin(string name, params string[] files)
    {
        var d = Path.Combine(_dir, name);
        Directory.CreateDirectory(d);
        foreach (var f in files) File.WriteAllText(Path.Combine(d, f), "@echo off");
        return d;
    }

    [Fact]
    public void Find_on_path()
    {
        var a = Bin("a");
        var b = Bin("b", "gemini.cmd", "claude.bat");
        var c = Bin("c", "gemini.exe");
        var path = $";{a}; \"{b}\" ;;{c};X:\\khong-co";
        Assert.Equal(Path.Combine(b, "gemini.cmd"), CliProbes.FindOnPath("gemini", path));
        Assert.Equal(Path.Combine(b, "claude.bat"), CliProbes.FindOnPath("claude", path));
        Assert.Null(CliProbes.FindOnPath("codex", path));
        Assert.Null(CliProbes.FindOnPath("gemini", null));
        Assert.Null(CliProbes.FindOnPath("..\\b\\gemini", a));
        Assert.Null(CliProbes.FindOnPath("b/gemini", _dir));
        Assert.Null(CliProbes.FindOnPath("C:gemini", b));
        Assert.Equal(Path.Combine(c, "gemini.exe"), CliProbes.FindOnPath("gemini", c + ";" + b));   // .exe trước .cmd trong CÙNG thư mục; thư mục trước thắng
    }

    [Fact]
    public void Gemini_probe_checks_presence_of_login_file_only()
    {
        var bin = Bin("g", "gemini.cmd");
        var home = Path.Combine(_dir, "home");
        Directory.CreateDirectory(home);
        Assert.Equal(new ProviderHealth("gemini", false, null, "Chưa cài gemini CLI"), CliProbes.Gemini(Bin("empty"), home));
        Assert.Equal(new ProviderHealth("gemini", true, false, "Chưa đăng nhập: chạy `gemini` trong terminal, chọn Login with Google"), CliProbes.Gemini(bin, home));

        Directory.CreateDirectory(Path.Combine(home, ".gemini"));
        File.WriteAllText(Path.Combine(home, ".gemini", "oauth_creds.json"), "{\"access_token\":\"BI-MAT-KHONG-DUOC-LO\"}");
        var ok = CliProbes.Gemini(bin, home);
        Assert.Equal((true, (bool?)true, "Có tệp đăng nhập của gemini CLI"), (ok.Installed, ok.LoggedIn, ok.Detail));
        Assert.DoesNotContain("BI-MAT", ok.Detail);
    }

    [Fact]
    public void Claude_probe()
    {
        Assert.Equal(new ProviderHealth("claude", false, null, "Chưa cài claude CLI"), CliProbes.Claude(Bin("e2")));
        var bin = Bin("cl", "claude.exe");
        Assert.Equal(new ProviderHealth("claude", true, null, "Đã cài: " + Path.Combine(bin, "claude.exe")), CliProbes.Claude(bin));
    }
}
