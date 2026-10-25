using AITeam.Core.Accounts;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Hub.Services;

// Trang "Tài khoản AI" (R-005 / V5-06). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C3.

public interface IAiAccountsService
{
    /// <summary>Mọi dịch vụ theo AiCatalog, trạng thái từ lần kiểm tra gần nhất trong bộ nhớ (KHÔNG chạy CLI).</summary>
    IReadOnlyList<AiAccountView> List();

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
    /// Id qua AiCatalog.Find (lạ ⇒ ArgumentException). Gọi _probe(service.Id, token) với CancellationTokenSource liên kết ct + CheckTimeout.
    /// Thành công ⇒ đệm AiCheck(health, null, now); ném lỗi / quá giờ / ct bị huỷ ⇒ AiCheck(null, ex.Message (quá CheckTimeout: "Quá 30 giây"), now) — KHÔNG ném ra ngoài.
    /// Đệm dùng chung mọi request (Hub đăng ký singleton) ⇒ khóa lock. Trả dòng của id đó (như List()).
    /// </summary>
    public async Task<AiAccountView> CheckAsync(string? id, CancellationToken ct)
    {
        var svc = AiCatalog.Find(id) ?? throw new ArgumentException("Dịch vụ không hợp lệ.");
        using var timeoutCts = new CancellationTokenSource(CheckTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        AiCheck check;
        try
        {
            var health = await _probe(svc.Id, linkedCts.Token);
            check = new AiCheck(health, null, _now());
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            check = new AiCheck(null, "Quá 30 giây", _now());
        }
        catch (Exception ex)
        {
            check = new AiCheck(null, string.IsNullOrWhiteSpace(ex.Message) ? "Lỗi kiểm tra" : ex.Message, _now());
        }

        lock (_lock) _cache[svc.Id] = check;
        return List().Single(r => r.Service.Id == svc.Id);
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
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        return key switch
        {
            "claude" => CliProbes.Claude(pathEnv),
            _ => new ProviderHealth(key, false, null, "Chưa có connector tự động"),
        };
    };
}
