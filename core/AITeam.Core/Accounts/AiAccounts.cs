using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Accounts;

// Tài khoản AI (R-005 / V5-06). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C3 + G2.
// CHỈ metadata (tên gợi nhớ + chế độ). KHÔNG BAO GIỜ lưu mật khẩu, cookie, khóa API, token; không đọc NỘI DUNG tệp đăng nhập của CLI (chỉ kiểm tra có / không).

/// <summary>8 trạng thái chuẩn.</summary>
public enum AiStatus { Disconnected, LoginRequired, Connected, ManualOnly, Busy, RateLimited, Unsupported, Error }

/// <summary>Chế độ do chủ dự án chọn: tắt · thủ công (copy / dán) · tự động (qua CLI chính thức / trình duyệt riêng).</summary>
public enum AiMode { Disabled, Manual, Automatic }

/// <summary>
/// Một dịch vụ AI. ProviderId: khóa trong .state/providers.json (trạng thái nghỉ) — "agy" / "codex" / null.
/// AutomaticAvailable false ⇒ chưa có connector tự động (DeepSeek, Grok tới V5-10 / V5-11).
/// </summary>
public sealed record AiService(string Id, string Name, string Role, string Via, string OfficialUrl, string LoginHint,
    AiMode DefaultMode, bool AutomaticAvailable, string? ProviderId);

/// <summary>Danh mục cố định, đúng thứ tự hiển thị. (Claude viết.)</summary>
public static class AiCatalog
{
    public static readonly IReadOnlyList<AiService> All =
    [
        new("antigravity", "Antigravity", "Lập trình viên chính (Minh, Lan)", "agy CLI", "https://antigravity.google",
            "Đăng nhập / đổi tài khoản ở trang Tài khoản Antigravity.", AiMode.Automatic, true, "agy"),
        new("codex", "Codex", "Review + dự phòng (Codex)", "codex CLI", "https://chatgpt.com/codex",
            "Chạy `codex login` trong terminal.", AiMode.Automatic, true, "codex"),
        new("chatgpt", "ChatGPT", "Phòng nghiên cứu", "codex CLI (tài khoản ChatGPT)", "https://chatgpt.com",
            "Dùng chung đăng nhập Codex: `codex login`.", AiMode.Manual, true, "codex"),
        // 25/10/2026: Gemini CLI báo IneligibleTierError với tài khoản Google cá nhân miễn phí ("migrate to Antigravity") ⇒ hỏi Gemini qua agy.
        new("gemini", "Gemini", "Phòng nghiên cứu", "Antigravity (agy · model Gemini)", "https://gemini.google.com",
            "Dùng chung đăng nhập Antigravity (trang Tài khoản Antigravity). Gemini CLI không còn nhận tài khoản cá nhân miễn phí.", AiMode.Manual, true, "agy"),
        new("claude", "Claude", "Quản lý (phiên Claude Code) · CLI tùy chọn", "claude CLI", "https://claude.ai",
            "Tùy chọn: cài Claude Code CLI rồi chạy `claude` để đăng nhập (dùng chung hạn mức với phiên quản lý).", AiMode.Disabled, true, null),
        new("deepseek", "DeepSeek", "Phòng nghiên cứu", "trình duyệt riêng của AITeam (V5-10)", "https://chat.deepseek.com",
            "Chưa có: bật được ở V5-10. Hiện dùng thủ công (copy / dán).", AiMode.Disabled, false, null),
        new("grok", "Grok", "Phòng nghiên cứu", "trình duyệt riêng của AITeam (V5-11)", "https://grok.com",
            "Chưa có: bật được ở V5-11. Hiện dùng thủ công (copy / dán).", AiMode.Disabled, false, null),
    ];

    public static AiService? Find(string? id) => All.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
}

public sealed record AiAccountSetting(string Id, string Alias, AiMode Mode);

/// <summary>
/// %LOCALAPPDATA%\AITeam\config\accounts.json — định dạng:
/// {"version":1,"accounts":[{"id":"gemini","alias":"Google cá nhân","mode":"Automatic"}, …]} (camelCase, mode ghi CHỮ, UTF-8 không BOM, thụt lề).
/// </summary>
public sealed class AiAccountStore(string file)
{
    public const int MaxAlias = 40;
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    public static string DefaultFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AITeam", "config", "accounts.json");

    public string FilePath { get; } = file;

    /// <summary>
    /// Đúng 1 dòng cho MỖI dịch vụ trong AiCatalog.All, đúng thứ tự danh mục.
    /// File không có / hỏng / thiếu dịch vụ ⇒ dòng mặc định (Alias "", Mode = DefaultMode). Id lạ trong file ⇒ bỏ qua. Id trùng ⇒ lấy dòng ĐẦU.
    /// Mode Automatic mà !AutomaticAvailable ⇒ Manual. Mode không đọc được (chữ lạ / số) ⇒ DefaultMode.
    /// Alias qua CleanAlias. Không bao giờ ném lỗi.
    /// </summary>
    public IReadOnlyList<AiAccountSetting> Load()
    {
        var found = new Dictionary<string, AiAccountSetting>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(FilePath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(FilePath, Encoding.UTF8));
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("accounts", out var arr)
                    && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;
                        var rawId = item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String ? idProp.GetString() : null;
                        var svc = AiCatalog.Find(rawId);
                        if (svc == null || found.ContainsKey(svc.Id)) continue;

                        var rawAlias = item.TryGetProperty("alias", out var aliasProp) && aliasProp.ValueKind == JsonValueKind.String ? aliasProp.GetString() : null;
                        var rawMode = item.TryGetProperty("mode", out var modeProp) && modeProp.ValueKind == JsonValueKind.String ? modeProp.GetString() : null;
                        var mode = TryParseMode(rawMode, out var parsed) ? parsed : svc.DefaultMode;
                        if (mode == AiMode.Automatic && !svc.AutomaticAvailable) mode = AiMode.Manual;

                        found[svc.Id] = new AiAccountSetting(svc.Id, CleanAlias(rawAlias), mode);
                    }
                }
            }
        }
        catch { }

        return AiCatalog.All.Select(s => found.TryGetValue(s.Id, out var row) ? row : new AiAccountSetting(s.Id, "", s.DefaultMode)).ToList();
    }

    /// <summary>
    /// Lưu 1 dịch vụ (giữ nguyên các dịch vụ khác theo Load()). Id không có trong danh mục ⇒ ArgumentException("Dịch vụ không hợp lệ.").
    /// Automatic mà !AutomaticAvailable ⇒ InvalidOperationException("{Name} chưa hỗ trợ chế độ tự động.").
    /// Ghi ĐỦ mọi dịch vụ theo thứ tự danh mục, CHỈ 3 trường id / alias / mode (không bao giờ thêm trường khác).
    /// Tạo thư mục; ghi file tạm cùng thư mục rồi File.Move(overwrite: true). Trả bản đã lưu.
    /// </summary>
    public AiAccountSetting Save(string id, string? alias, AiMode mode)
    {
        var svc = AiCatalog.Find(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        if (mode == AiMode.Automatic && !svc.AutomaticAvailable)
            throw new InvalidOperationException($"{svc.Name} chưa hỗ trợ chế độ tự động.");

        var updated = new AiAccountSetting(svc.Id, CleanAlias(alias), mode);
        var rows = Load().Select(r => r.Id == svc.Id ? updated : r).ToList();
        var payload = new
        {
            version = 1,
            accounts = rows.Select(r => new { id = r.Id, alias = r.Alias, mode = r.Mode.ToString() }).ToList(),
        };

        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"accounts.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tmp, JsonSerializer.Serialize(payload, WriteOpts), Utf8NoBom);
        File.Move(tmp, FilePath, overwrite: true);
        return updated;
    }

    /// <summary>Bỏ ký tự điều khiển, Trim, cắt còn MaxAlias ký tự (rồi Trim lại). null ⇒ "".</summary>
    public static string CleanAlias(string? alias)
    {
        if (string.IsNullOrEmpty(alias)) return "";
        var s = new string(alias.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return s.Length > MaxAlias ? s[..MaxAlias].Trim() : s;
    }

    internal static bool TryParseMode(string? raw, out AiMode mode)
    {
        foreach (var m in Enum.GetValues<AiMode>())
        {
            if (string.Equals(raw, m.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                mode = m;
                return true;
            }
        }
        mode = default;
        return false;
    }
}

/// <summary>Kết quả 1 lần kiểm tra (Probe null ⇒ chưa kiểm tra lần nào từ lúc mở Hub). ProbeError: kiểm tra ném lỗi / quá giờ.</summary>
public sealed record AiCheck(ProviderHealth? Probe, string? ProbeError, DateTime? CheckedAt);

public sealed record AiAccountView(AiService Service, string Alias, AiMode Mode, AiStatus Status, string StatusText, string Detail, DateTime? CheckedAt);

public static class AiStatusResolver
{
    /// <summary>
    /// Thứ tự luật (luật đầu tiên khớp thắng), StatusText CHÍNH XÁC:
    /// 1. Mode Disabled ⇒ Disconnected "Đang tắt".
    /// 2. Mode Manual ⇒ ManualOnly "Thủ công (copy / dán)".
    /// 3. (Automatic) !AutomaticAvailable ⇒ Unsupported "Chưa hỗ trợ tự động".
    /// 4. rest != null và rest.Until &gt; now ⇒ RateLimited "Nghỉ tới {Until:HH:mm dd/MM}".
    /// 5. busy ⇒ Busy "Đang chạy việc".
    /// 6. check.ProbeError != null ⇒ Error "Lỗi kiểm tra".
    /// 7. check.Probe null ⇒ Disconnected "Chưa kiểm tra".
    /// 8. !Probe.Installed ⇒ Unsupported "Chưa cài {Via}".
    /// 9. Probe.LoggedIn false ⇒ LoginRequired "Cần đăng nhập".
    /// 10. Probe.LoggedIn true ⇒ Connected "Đã kết nối".
    /// 11. (null) ⇒ Disconnected "Chưa xác minh đăng nhập".
    /// Detail = dòng đầu (không rỗng) của ProbeError ?? Probe.Detail, cắt 160 ký tự; không có ⇒ "".
    /// </summary>
    public static AiAccountView Resolve(AiService service, AiAccountSetting setting, AiCheck check, ProviderRest? rest, bool busy, DateTime now)
    {
        var (status, text) = ResolveStatus(service, setting, check, rest, busy, now);
        var rawDetail = check.ProbeError ?? check.Probe?.Detail;
        var detail = (rawDetail ?? "")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? "";
        if (detail.Length > 160) detail = detail[..160];
        return new AiAccountView(service, setting.Alias, setting.Mode, status, text, detail, check.CheckedAt);
    }

    private static (AiStatus Status, string Text) ResolveStatus(AiService service, AiAccountSetting setting, AiCheck check, ProviderRest? rest, bool busy, DateTime now)
    {
        if (setting.Mode == AiMode.Disabled) return (AiStatus.Disconnected, "Đang tắt");
        if (setting.Mode == AiMode.Manual) return (AiStatus.ManualOnly, "Thủ công (copy / dán)");
        if (!service.AutomaticAvailable) return (AiStatus.Unsupported, "Chưa hỗ trợ tự động");
        if (rest != null && rest.Until > now) return (AiStatus.RateLimited, $"Nghỉ tới {rest.Until:HH:mm dd/MM}");
        if (busy) return (AiStatus.Busy, "Đang chạy việc");
        if (check.ProbeError != null) return (AiStatus.Error, "Lỗi kiểm tra");
        if (check.Probe == null) return (AiStatus.Disconnected, "Chưa kiểm tra");
        if (!check.Probe.Installed) return (AiStatus.Unsupported, $"Chưa cài {service.Via}");
        if (check.Probe.LoggedIn == false) return (AiStatus.LoginRequired, "Cần đăng nhập");
        if (check.Probe.LoggedIn == true) return (AiStatus.Connected, "Đã kết nối");
        return (AiStatus.Disconnected, "Chưa xác minh đăng nhập");
    }
}

/// <summary>Kiểm tra CLI không có lệnh "login status": chỉ xem exe có trên PATH và tệp đăng nhập CÓ hay KHÔNG (không đọc nội dung).</summary>
public static class CliProbes
{
    /// <summary>
    /// Tìm exe trên PATH: tách pathEnv theo ';' (bỏ mục rỗng, Trim, bỏ dấu "), mỗi thư mục thử lần lượt name + ".exe", ".cmd", ".bat";
    /// trả đường dẫn đầy đủ đầu tiên có thật; không có ⇒ null. Tên có ký tự đường dẫn ('\\', '/', ':' hoặc "..") ⇒ null.
    /// </summary>
    public static string? FindOnPath(string name, string? pathEnv)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pathEnv)) return null;
        if (name.Contains('\\') || name.Contains('/') || name.Contains(':') || name.Contains("..")) return null;
        string[] exts = [".exe", ".cmd", ".bat"];
        foreach (var rawDir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var dir = rawDir.Trim().Trim('"').Trim();
            if (dir.Length == 0) continue;
            foreach (var ext in exts)
            {
                try
                {
                    var full = Path.Combine(dir, name + ext);
                    if (File.Exists(full)) return full;
                }
                catch { }
            }
        }
        return null;
    }

    /// <summary>
    /// gemini: exe = FindOnPath("gemini", pathEnv). Không có ⇒ ProviderHealth("gemini", false, null, "Chưa cài gemini CLI").
    /// Có: LoggedIn = File.Exists(userProfile\.gemini\oauth_creds.json); Detail: true ⇒ "Có tệp đăng nhập của gemini CLI";
    /// false ⇒ "Chưa đăng nhập: chạy `gemini` trong terminal, chọn Login with Google".
    /// </summary>
    public static ProviderHealth Gemini(string? pathEnv, string userProfile)
    {
        var exe = FindOnPath("gemini", pathEnv);
        if (exe == null) return new ProviderHealth("gemini", false, null, "Chưa cài gemini CLI");
        var loggedIn = !string.IsNullOrWhiteSpace(userProfile) && File.Exists(Path.Combine(userProfile, ".gemini", "oauth_creds.json"));
        return new ProviderHealth("gemini", true, loggedIn,
            loggedIn ? "Có tệp đăng nhập của gemini CLI" : "Chưa đăng nhập: chạy `gemini` trong terminal, chọn Login with Google");
    }

    /// <summary>claude: exe = FindOnPath("claude", pathEnv). Không có ⇒ ("claude", false, null, "Chưa cài claude CLI"); có ⇒ ("claude", true, null, "Đã cài: " + exe).</summary>
    public static ProviderHealth Claude(string? pathEnv)
    {
        var exe = FindOnPath("claude", pathEnv);
        return exe == null
            ? new ProviderHealth("claude", false, null, "Chưa cài claude CLI")
            : new ProviderHealth("claude", true, null, "Đã cài: " + exe);
    }
}
