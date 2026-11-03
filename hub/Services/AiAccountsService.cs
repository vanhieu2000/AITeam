using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Accounts;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Research;
using AITeam.Core.Routing;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// Trang "Tài khoản AI" (R-005 / V5-06). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C3.

/// <summary>Một liên kết quản lý lấy từ team.json → accounts[].links; chỉ nhận URL https.</summary>
public sealed record AiAccountLink(string Label, string Url);

/// <summary>
/// Thẻ tài khoản theo mockup H8-13. Số liệu thiếu là null (hiện "chưa đo"/"không rõ"), không thay bằng 0.
/// QuotaSource: "provider" · "ước tính" · "không rõ".
/// </summary>
public sealed record AiAccountCardView(
    AiService Service,
    string CliText,
    string LoginText,
    int RunningJobs,
    string AllowedModels,
    string PlanText,
    long? TokensToday,
    long? TokensLast7Days,
    decimal? UsedPercent,
    decimal? RemainingPercent,
    string QuotaSource,
    DateTime? ResetsAt,
    DateTime? ExhaustsAt,
    bool Resting,
    string RestText,
    bool WarnLow,
    IReadOnlyList<AiAccountLink> Links);

public interface IAiAccountsService
{
    /// <summary>Mọi dịch vụ theo AiCatalog, trạng thái từ lần kiểm tra gần nhất trong bộ nhớ (KHÔNG chạy CLI).</summary>
    IReadOnlyList<AiAccountView> List();

    /// <summary>Bốn thẻ tài khoản AI (Codex, OpenCode, Antigravity, Claude) kèm hạn mức, token và liên kết.</summary>
    IReadOnlyList<AiAccountCardView> ListCards() => [];

    /// <summary>Chạy kiểm tra 1 dịch vụ rồi trả dòng mới. Id lạ ⇒ ArgumentException("Dịch vụ không hợp lệ.").</summary>
    Task<AiAccountView> CheckAsync(string? id, CancellationToken ct);

    /// <summary>
    /// Lưu tên gợi nhớ + chế độ. mode phải ĐÚNG tên AiMode (không phân biệt hoa thường, không nhận số) ⇒ khác: ArgumentException("Chế độ không hợp lệ.").
    /// Lỗi của AiAccountStore.Save bay ra nguyên vẹn. Trả dòng mới.
    /// </summary>
    AiAccountView Save(string? id, string? alias, string? mode);
}

public sealed class AiAccountsService : IAiAccountsService
{
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);

    private readonly HubPaths _paths;
    private readonly AiAccountStore _store;
    private readonly Func<string, CancellationToken, Task<ProviderHealth>> _probe;
    private readonly IAgentRunService _runs;
    private readonly Func<DateTime> _now;
    private readonly object _lock = new();
    private readonly Dictionary<string, AiCheck> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OpenCode chưa nằm trong AiCatalog (giữ danh mục cũ 7 dòng) nhưng có thẻ riêng theo H8-13.</summary>
    internal static readonly AiService OpenCodeService = new("opencode", "OpenCode", "Lập trình viên (OpenCode)", "opencode CLI",
        "https://opencode.ai", "Đăng nhập bằng `opencode auth login` trong terminal.", AiMode.Automatic, true, "opencode");

    private static readonly IReadOnlyList<AiService> CardServices =
    [
        AiCatalog.Find("codex")!,
        OpenCodeService,
        AiCatalog.Find("antigravity")!,
        AiCatalog.Find("claude")!,
    ];

    private static readonly Regex VersionPattern = new(@"\b\d+\.\d+(?:\.\d+)?\b", RegexOptions.CultureInvariant);

    /// <param name="probe">id dịch vụ ⇒ kết quả kiểm tra. null ⇒ DefaultProbe(paths).</param>
    public AiAccountsService(HubPaths paths, IAgentRunService runs, AiAccountStore? store = null,
        Func<string, CancellationToken, Task<ProviderHealth>>? probe = null, Func<DateTime>? now = null)
    {
        _paths = paths;
        _runs = runs;
        _store = store ?? new AiAccountStore(AiAccountStore.DefaultFile);
        _probe = probe ?? DefaultProbe(paths);
        _now = now ?? (() => DateTime.Now);
    }

    /// <summary>
    /// Mỗi dòng = AiStatusResolver.Resolve(service, setting (từ _store.Load()), check (bộ nhớ đệm theo id; chưa có ⇒ new AiCheck(null, null, null)),
    ///   rest = service.ProviderId != null ? new ProviderStateStore(AITeamRoot).RestOf(ProviderId, now) : null,
    ///   busy = Id "antigravity" và _runs.GetRunning().Count &gt; 0 (lỗi đọc ⇒ false), now).
    /// </summary>
    public IReadOnlyList<AiAccountView> List()
    {
        var now = _now();
        var settings = _store.Load().ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        var stateStore = new ProviderStateStore(_paths.AITeamRoot);
        var agyBusy = false;
        try { agyBusy = _runs.GetRunning().Count > 0; } catch { }

        Dictionary<string, AiCheck> checks;
        lock (_lock) checks = new Dictionary<string, AiCheck>(_cache, StringComparer.OrdinalIgnoreCase);

        var result = new List<AiAccountView>(AiCatalog.All.Count);
        foreach (var svc in AiCatalog.All)
        {
            var setting = settings.TryGetValue(svc.Id, out var s) ? s : new AiAccountSetting(svc.Id, "", svc.DefaultMode);
            var check = checks.TryGetValue(svc.Id, out var c) ? c : new AiCheck(null, null, null);
            var rest = svc.ProviderId != null ? stateStore.RestOf(svc.ProviderId, now) : null;
            var busy = string.Equals(svc.Id, "antigravity", StringComparison.OrdinalIgnoreCase) && agyBusy;
            result.Add(AiStatusResolver.Resolve(svc, setting, check, rest, busy, now));
        }
        return result;
    }

    /// <summary>
    /// Bốn thẻ Codex, OpenCode, Antigravity, Claude. Đọc hạn mức từ ProviderStateStore (kèm nguồn),
    /// token từ metrics/runs, liên kết https từ team.json. Lỗi đọc nguồn phụ ⇒ coi như thiếu dữ liệu, không ném.
    /// </summary>
    public IReadOnlyList<AiAccountCardView> ListCards()
    {
        var now = _now();
        var settings = _store.Load().ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<ProviderRest> rests;
        try { rests = new ProviderStateStore(_paths.AITeamRoot).All(); }
        catch { rests = []; }

        IReadOnlyList<RunningAgent> running;
        try { running = _runs.GetRunning(); }
        catch { running = []; }

        IReadOnlyList<HubMetric> metrics;
        try { metrics = HubMetricReader.Read(_paths).Runs; }
        catch { metrics = []; }

        var team = ReadTeamAccounts(_paths);

        Dictionary<string, AiCheck> checks;
        lock (_lock) checks = new Dictionary<string, AiCheck>(_cache, StringComparer.OrdinalIgnoreCase);

        var result = new List<AiAccountCardView>(CardServices.Count);
        foreach (var svc in CardServices)
        {
            var providerId = svc.ProviderId ?? svc.Id;
            var setting = settings.TryGetValue(svc.Id, out var s) ? s : new AiAccountSetting(svc.Id, "", svc.DefaultMode);
            var check = checks.TryGetValue(svc.Id, out var c) ? c : new AiCheck(null, null, null);
            var rest = rests.Where(r => string.Equals(r.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.At).FirstOrDefault();
            var busy = running.Count(r => CommandMatches(r.CommandLine, providerId));
            var (today, week) = SumTokens(metrics, providerId, now);
            var used = rest?.UsedPercent;
            decimal? remaining = used is decimal u ? Math.Clamp(100m - u, 0m, 100m) : null;
            var resting = rest != null && rest.Until > now;
            DateTime? resets = rest?.ResetsAt is DateTime ra && ra > now
                ? ra
                : resting ? rest!.Until : null;

            result.Add(new AiAccountCardView(
                svc,
                CliText(check),
                LoginText(check),
                busy,
                AllowedModels(svc.Id),
                PlanText(svc.Id, team.Plans),
                today,
                week,
                used,
                remaining,
                SourceText(rest),
                resets,
                rest?.ExhaustsAt is DateTime ex && ex > now ? ex : null,
                resting,
                resting ? RestText(rest!, now) : "",
                remaining is decimal r && r < 20m,
                team.Links.TryGetValue(svc.Id, out var links) ? links : []));
        }
        return result;
    }

    private static string CliText(AiCheck check)
    {
        if (check.ProbeError != null) return "Lỗi kiểm tra";
        if (check.Probe == null) return "Chưa kiểm tra";
        if (!check.Probe.Installed) return "Chưa cài";
        var detail = SecretRedactor.Redact(check.Probe.Detail).Text;
        var version = VersionPattern.Match(detail).Value;
        return string.IsNullOrEmpty(version) ? "Đã cài" : $"Đã cài · v{version}";
    }

    private static string LoginText(AiCheck check)
    {
        if (check.ProbeError != null) return "Không rõ";
        if (check.Probe?.LoggedIn == true) return "Đã đăng nhập";
        if (check.Probe?.LoggedIn == false) return "Chưa đăng nhập";
        return "Không rõ";
    }

    private static string AllowedModels(string id) => id switch
    {
        "opencode" => "Chỉ DeepSeek",
        "antigravity" => "Chỉ Argon 4",
        _ => "Theo tài khoản",
    };

    private static string PlanText(string id, IReadOnlyDictionary<string, string> plans)
        => plans.TryGetValue(id, out var plan) ? plan : "Chưa xác minh";

    private static string SourceText(ProviderRest? rest)
    {
        if (rest?.UsedPercent is null) return "không rõ";
        return rest.Source switch
        {
            "provider" => "provider",
            "estimate" => "ước tính",
            _ => "không rõ",
        };
    }

    private static string RestText(ProviderRest rest, DateTime now)
    {
        var reason = rest.Reason switch
        {
            RunStatus.QuotaExhausted => "Hết hạn mức",
            RunStatus.NotLoggedIn => "Chưa đăng nhập",
            RunStatus.Failed => "Lỗi liên tiếp",
            RunStatus.TimedOut => "Quá thời gian",
            RunStatus.ConfigError => "Lỗi cấu hình",
            RunStatus.Unavailable => "Không chạy được",
            _ => "Tạm nghỉ",
        };
        var until = rest.Until > now ? rest.Until : now;
        var detail = SecretRedactor.Redact(rest.Detail).Text.Trim();
        return string.IsNullOrEmpty(detail)
            ? $"{reason} · nghỉ tới {until:HH:mm dd/MM}"
            : $"{reason} · nghỉ tới {until:HH:mm dd/MM} · {detail}";
    }

    private static bool CommandMatches(string command, string providerId)
    {
        var needle = providerId switch
        {
            "agy" => "agy",
            "codex" => "codex",
            "opencode" => "opencode",
            "claude" => "claude",
            _ => providerId,
        };
        return command.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static (long? Today, long? Week) SumTokens(IReadOnlyList<HubMetric> metrics, string providerId, DateTime now)
    {
        long today = 0, week = 0;
        bool todayKnown = false, weekKnown = false;
        var from = now.AddDays(-7);
        foreach (var metric in metrics)
        {
            if (metric.Tokens is not long tokens || !ProviderMatches(metric.Provider, providerId)) continue;
            var at = metric.At.LocalDateTime;
            if (at.Date == now.Date)
            {
                today += tokens;
                todayKnown = true;
            }
            if (at >= from)
            {
                week += tokens;
                weekKnown = true;
            }
        }
        return (todayKnown ? today : null, weekKnown ? week : null);
    }

    private static bool ProviderMatches(string metricProvider, string providerId)
    {
        var value = metricProvider.Trim().ToLowerInvariant();
        return providerId switch
        {
            "agy" => value is "agy" or "antigravity" or "gemini",
            "codex" => value is "codex" or "chatgpt",
            "opencode" => value == "opencode",
            "claude" => value == "claude",
            _ => value == providerId.ToLowerInvariant(),
        };
    }

    private sealed record TeamAccounts(
        IReadOnlyDictionary<string, IReadOnlyList<AiAccountLink>> Links,
        IReadOnlyDictionary<string, string> Plans);

    private static TeamAccounts ReadTeamAccounts(HubPaths paths)
    {
        var links = new Dictionary<string, IReadOnlyList<AiAccountLink>>(StringComparer.OrdinalIgnoreCase);
        var plans = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(paths.TeamJson)) return new(links, plans);
            using var doc = JsonDocument.Parse(File.ReadAllText(paths.TeamJson, Encoding.UTF8));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(links, plans);

            if (root.TryGetProperty("accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array)
            {
                foreach (var account in accounts.EnumerateArray())
                {
                    if (account.ValueKind != JsonValueKind.Object) continue;
                    var list = new List<AiAccountLink>();
                    if (account.TryGetProperty("links", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arr.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.Object) continue;
                            var url = CleanUrl(Text(item, "url"));
                            if (url == null) continue;
                            list.Add(new AiAccountLink(CleanLabel(Text(item, "label")), url));
                            if (list.Count >= 10) break;
                        }
                    }
                    foreach (var key in new[] { Text(account, "id"), Text(account, "name") })
                        if (!string.IsNullOrWhiteSpace(key)) links[key!] = list;
                }
            }

            if (root.TryGetProperty("opencode", out var opencode) && opencode.ValueKind == JsonValueKind.Object
                && opencode.TryGetProperty("paid", out var paid) && paid.ValueKind is JsonValueKind.True or JsonValueKind.False)
                plans["opencode"] = paid.GetBoolean() ? "Trả phí" : "Free";

            if (root.TryGetProperty("providers", out var providers) && providers.ValueKind == JsonValueKind.Array)
            {
                foreach (var provider in providers.EnumerateArray())
                {
                    if (provider.ValueKind != JsonValueKind.Object) continue;
                    var id = Text(provider, "id");
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (provider.TryGetProperty("paid", out var providerPaid) && providerPaid.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        plans[id!] = providerPaid.GetBoolean() ? "Trả phí" : "Free";
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return new(links, plans);
    }

    private static string? Text(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string CleanLabel(string? raw)
    {
        var label = SecretRedactor.Redact(raw?.Trim()).Text;
        if (label.Length > 60) label = label[..60].Trim();
        return string.IsNullOrWhiteSpace(label) ? "Liên kết" : label;
    }

    private static string? CleanUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = raw.Trim();
        if (value.Length > 2048 || value.Any(char.IsControl)) return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(uri.Host)) return null;
        if (SecretRedactor.Redact(value).Count > 0) return null;
        return value;
    }

    /// <summary>
    /// Id qua AiCatalog.Find (lạ ⇒ ArgumentException). Gọi _probe(service.Id, token) với CancellationTokenSource liên kết ct + CheckTimeout.
    /// Thành công ⇒ đệm AiCheck(health, null, now); ném lỗi / quá giờ / ct bị huỷ ⇒ AiCheck(null, ex.Message (quá CheckTimeout: "Quá 30 giây"), now) — KHÔNG ném ra ngoài.
    /// Đệm dùng chung mọi request (Hub đăng ký singleton) ⇒ khóa lock. Trả dòng của id đó (như List()).
    /// </summary>
    public async Task<AiAccountView> CheckAsync(string? id, CancellationToken ct)
    {
        var svc = FindService(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        using var timeoutCts = new CancellationTokenSource(CheckTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        AiCheck check;
        try
        {
            var health = await _probe(svc.Id, linkedCts.Token);
            check = new AiCheck(RedactHealth(health), null, _now());
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            check = new AiCheck(null, "Quá 30 giây", _now());
        }
        catch (Exception ex)
        {
            var message = SecretRedactor.Redact(ex.Message).Text;
            check = new AiCheck(null, string.IsNullOrWhiteSpace(message) ? "Lỗi kiểm tra" : message, _now());
        }

        lock (_lock) _cache[svc.Id] = check;
        var row = List().FirstOrDefault(r => r.Service.Id == svc.Id);
        if (row != null) return row;

        // OpenCode không nằm trong AiCatalog: dựng dòng tạm cho thẻ, không thêm vào danh mục cũ.
        var now = _now();
        ProviderRest? rest = null;
        try { rest = new ProviderStateStore(_paths.AITeamRoot).RestOf(svc.ProviderId ?? svc.Id, now); }
        catch { }
        return AiStatusResolver.Resolve(svc, new AiAccountSetting(svc.Id, "", svc.DefaultMode), check, rest, false, now);
    }

    private static AiService? FindService(string? id)
        => string.Equals(id, OpenCodeService.Id, StringComparison.OrdinalIgnoreCase) ? OpenCodeService : AiCatalog.Find(id);

    private static ProviderHealth RedactHealth(ProviderHealth health)
    {
        var detail = SecretRedactor.Redact(health.Detail).Text;
        return string.Equals(detail, health.Detail, StringComparison.Ordinal) ? health : health with { Detail = detail };
    }

    public AiAccountView Save(string? id, string? alias, string? mode)
    {
        var svc = AiCatalog.Find(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        if (string.IsNullOrWhiteSpace(mode) || !Enum.GetValues<AiMode>().Any(m => string.Equals(mode, m.ToString(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Chế độ không hợp lệ.");

        var parsedMode = Enum.Parse<AiMode>(mode, ignoreCase: true);
        _store.Save(svc.Id, alias, parsedMode);
        return List().Single(r => r.Service.Id == svc.Id);
    }

    /// <summary>
    /// Kiểm tra thật theo id:
    ///   "antigravity" / "gemini" ⇒ provider "agy", "codex" / "chatgpt" ⇒ provider "codex" — lấy từ ProviderSettings.Load(AITeamRoot) rồi
    ///     settings.Create(cfg, new ProcessRunner()).CheckAsync(ct) (không có trong cấu hình ⇒ ProviderHealth(id, false, null, "Chưa cấu hình provider {pid}"));
    ///   "claude" ⇒ CliProbes.Claude(PATH) (PATH = Environment.GetEnvironmentVariable("PATH"));
    ///   khác ⇒ ProviderHealth(id, false, null, "Chưa có connector tự động").
    /// </summary>
    public static Func<string, CancellationToken, Task<ProviderHealth>> DefaultProbe(HubPaths paths) => async (id, ct) =>
    {
        var key = (id ?? "").ToLowerInvariant();
        if (key is "antigravity" or "gemini" or "codex" or "chatgpt")
        {
            var pid = key is "antigravity" or "gemini" ? "agy" : "codex";
            var settings = ProviderSettings.Load(paths.AITeamRoot);
            var cfg = settings.Providers.FirstOrDefault(p => string.Equals(p.Id, pid, StringComparison.OrdinalIgnoreCase));
            if (cfg == null) return new ProviderHealth(key, false, null, $"Chưa cấu hình provider {pid}");
            return await settings.Create(cfg, new ProcessRunner()).CheckAsync(ct);
        }
        if (key == "opencode")
        {
            var config = OpenCodeProvider.Configuration(paths.AITeamRoot, publicRepository: false);
            return await new OpenCodeProvider(config, new ProcessRunner()).CheckAsync(ct);
        }
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        return key switch
        {
            "claude" => CliProbes.Claude(pathEnv),
            _ => new ProviderHealth(key, false, null, "Chưa có connector tự động"),
        };
    };
}

/// <summary>Chế độ HUB_FAKE: tài khoản AI giả trong bộ nhớ, không đọc/ghi accounts.json hay team.json thật.</summary>
public sealed class FakeAiAccountsService : IAiAccountsService
{
    private readonly Dictionary<string, AiAccountSetting> _settings = AiCatalog.All
        .ToDictionary(s => s.Id, s => new AiAccountSetting(s.Id, "", s.DefaultMode), StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AiCheck> _checks = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AiAccountView> List()
    {
        var now = DateTime.Now;
        return AiCatalog.All.Select(s =>
        {
            var setting = _settings.TryGetValue(s.Id, out var current) ? current : new AiAccountSetting(s.Id, "", s.DefaultMode);
            var check = _checks.TryGetValue(s.Id, out var cached) ? cached : new AiCheck(null, null, null);
            return AiStatusResolver.Resolve(s, setting, check, null, false, now);
        }).ToList();
    }

    public Task<AiAccountView> CheckAsync(string? id, CancellationToken ct)
    {
        var svc = FindService(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        _checks[svc.Id] = new AiCheck(new ProviderHealth(svc.Id, true, true, "Đã đăng nhập (dữ liệu giả)"), null, DateTime.Now);
        return Task.FromResult(List().SingleOrDefault(r => r.Service.Id == svc.Id)
            ?? AiStatusResolver.Resolve(svc, new AiAccountSetting(svc.Id, "", svc.DefaultMode), _checks[svc.Id], null, false, DateTime.Now));
    }

    public AiAccountView Save(string? id, string? alias, string? mode)
    {
        var svc = AiCatalog.Find(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        if (string.IsNullOrWhiteSpace(mode) || !Enum.GetValues<AiMode>().Any(m => string.Equals(mode, m.ToString(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Chế độ không hợp lệ.");
        var parsed = Enum.Parse<AiMode>(mode, ignoreCase: true);
        if (parsed == AiMode.Automatic && !svc.AutomaticAvailable)
            throw new InvalidOperationException($"{svc.Name} chưa hỗ trợ chế độ tự động.");
        _settings[svc.Id] = new AiAccountSetting(svc.Id, AiAccountStore.CleanAlias(alias), parsed);
        return List().Single(r => r.Service.Id == svc.Id);
    }

    public IReadOnlyList<AiAccountCardView> ListCards()
    {
        var now = DateTime.Now;
        return
        [
            Card(AiCatalog.Find("codex")!, "Đã cài · v1.2.3", "Đã đăng nhập", 0, "Theo tài khoản", "Chưa xác minh",
                120_000, 540_000, 45m, "provider", now.AddHours(3), false, []),
            Card(AiAccountsService.OpenCodeService, "Đã cài · v0.9.1", "Không rõ", 0, "Chỉ DeepSeek", "Free",
                80_000, 300_000, 82m, "ước tính", now.AddHours(2), true,
                [new AiAccountLink("Tài liệu", "https://opencode.ai/docs")]),
            Card(AiCatalog.Find("antigravity")!, "Đã cài · v2.1.0", "Đã đăng nhập", 1, "Chỉ Argon 4", "Chưa xác minh",
                230_000, 900_000, 96m, "provider", now.AddHours(1), true,
                [new AiAccountLink("Hạn mức", "https://antigravity.google/account")]),
            Card(AiCatalog.Find("claude")!, "Chưa kiểm tra", "Không rõ", 0, "Theo tài khoản", "Chưa xác minh",
                null, null, null, "không rõ", null, false,
                [new AiAccountLink("Thanh toán", "https://console.anthropic.com/settings/billing")]),
        ];
    }

    private static AiAccountCardView Card(AiService service, string cli, string login, int running, string models, string plan,
        long? today, long? week, decimal? used, string source, DateTime? resets, bool resting, IReadOnlyList<AiAccountLink> links)
    {
        decimal? remaining = used is decimal value ? Math.Clamp(100m - value, 0m, 100m) : null;
        return new AiAccountCardView(service, cli, login, running, models, plan, today, week, used, remaining, source, resets,
            null, resting, resting ? "Hết hạn mức · nghỉ tới " + (resets?.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture) ?? "không rõ") : "",
            remaining is decimal r && r < 20m, links);
    }

    private static AiService? FindService(string? id)
        => string.Equals(id, AiAccountsService.OpenCodeService.Id, StringComparison.OrdinalIgnoreCase)
            ? AiAccountsService.OpenCodeService
            : AiCatalog.Find(id);
}
