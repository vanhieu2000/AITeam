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
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Id qua AiCatalog.Find (lạ ⇒ ArgumentException). Gọi _probe(service.Id, token) với CancellationTokenSource liên kết ct + CheckTimeout.
    /// Thành công ⇒ đệm AiCheck(health, null, now); ném lỗi / quá giờ / ct bị huỷ ⇒ AiCheck(null, ex.Message (quá CheckTimeout: "Quá 30 giây"), now) — KHÔNG ném ra ngoài.
    /// Đệm dùng chung mọi request (Hub đăng ký singleton) ⇒ khóa lock. Trả dòng của id đó (như List()).
    /// </summary>
    public Task<AiAccountView> CheckAsync(string? id, CancellationToken ct)
        => throw new NotImplementedException(); // TODO(agent)

    public AiAccountView Save(string? id, string? alias, string? mode)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Kiểm tra thật theo id:
    ///   "antigravity" ⇒ provider "agy", "codex" / "chatgpt" ⇒ provider "codex" — lấy từ ProviderSettings.Load(AITeamRoot) rồi
    ///     settings.Create(cfg, new ProcessRunner()).CheckAsync(ct) (không có trong cấu hình ⇒ ProviderHealth(id, false, null, "Chưa cấu hình provider {pid}"));
    ///   "gemini" ⇒ CliProbes.Gemini(PATH, paths.UserProfile); "claude" ⇒ CliProbes.Claude(PATH) (PATH = Environment.GetEnvironmentVariable("PATH"));
    ///   khác ⇒ ProviderHealth(id, false, null, "Chưa có connector tự động").
    /// </summary>
    public static Func<string, CancellationToken, Task<ProviderHealth>> DefaultProbe(HubPaths paths)
        => throw new NotImplementedException(); // TODO(agent)
}
