using System.Collections.Concurrent;
using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;

namespace AITeam.Hub.Services;

// F1-03 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-03.

/// <summary>Trạng thái 1 thành viên Đội AI. LoginState: "ok" | "logged-out" | "unknown" | "not-installed" | "error".</summary>
public sealed record AiMemberStatus(string Id, string Name, bool Enabled, IReadOnlyList<AiRole> Roles, IReadOnlyList<string> Models,
    string LoginState, DateTimeOffset? CheckedAt, string? Detail,
    double? RemainingPct, DateTimeOffset? RestUntil, string? QuotaSource, DateTimeOffset? QuotaObservedAt);

public interface IAiTeamStatusService
{
    /// <summary>Bật trước, rồi theo tên; ẩn dịch vụ tắt trùng tên với dịch vụ đang bật.</summary>
    IReadOnlyList<AiMemberStatus> List();

    /// <summary>Kiểm tra đăng nhập thật và LƯU kết quả. Id lạ / dịch vụ tắt ⇒ null, không chạy gì.</summary>
    Task<AiMemberStatus?> CheckAsync(string id, CancellationToken ct);
}

public sealed class AiTeamStatusService : IAiTeamStatusService
{
    private readonly IAiServiceRegistry _registry;
    private readonly IAiProviderFactory _factory;
    private readonly TimeProvider _clock;

    public AiTeamStatusService(IAiServiceRegistry registry, IAiProviderFactory factory, TimeProvider clock)
    {
        _registry = registry;
        _factory = factory;
        _clock = clock;
    }

    private readonly ConcurrentDictionary<string, string?> _details = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AiMemberStatus> List()
    {
        var services = _registry.List(includeDisabled: true);
        var enabledNames = new HashSet<string>(
            services.Where(s => s.Enabled).Select(s => s.Name.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var filtered = services.Where(s => s.Enabled || !enabledNames.Contains(s.Name.Trim()));
        var sorted = filtered
            .OrderByDescending(s => s.Enabled)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var accounts = _registry.Accounts();
        var quotas = _registry.LatestQuotas();
        var now = _clock.GetUtcNow();

        var result = new List<AiMemberStatus>(sorted.Count);
        foreach (var s in sorted)
        {
            var svcAccounts = accounts.Where(a => string.Equals(a.ServiceId, s.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            var acc = svcAccounts.FirstOrDefault(a => string.Equals(a.Id, $"{s.Id}:default", StringComparison.OrdinalIgnoreCase))
                ?? svcAccounts.OrderByDescending(a => a.CheckedAt).FirstOrDefault();

            var loginState = acc?.LoginState ?? "unknown";
            var checkedAt = acc?.CheckedAt;
            _details.TryGetValue(s.Id, out var detail);

            var latestQuota = quotas
                .Where(q => string.Equals(q.ServiceId, s.Id, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.ObservedAt)
                .FirstOrDefault();

            double? remainingPct = null;
            DateTimeOffset? restUntil = null;
            string? quotaSource = null;
            DateTimeOffset? quotaObservedAt = null;

            if (latestQuota != null)
            {
                quotaSource = latestQuota.Source;
                quotaObservedAt = latestQuota.ObservedAt;
                if (latestQuota.RestUntil.HasValue && latestQuota.RestUntil.Value <= now)
                {
                    remainingPct = null;
                    restUntil = null;
                }
                else
                {
                    remainingPct = latestQuota.RemainingPct;
                    restUntil = latestQuota.RestUntil;
                }
            }

            result.Add(new AiMemberStatus(
                s.Id,
                s.Name,
                s.Enabled,
                s.Roles,
                s.Models,
                loginState,
                checkedAt,
                detail,
                remainingPct,
                restUntil,
                quotaSource,
                quotaObservedAt));
        }

        return result;
    }

    public async Task<AiMemberStatus?> CheckAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var service = _registry.Get(id);
        if (service == null || !service.Enabled) return null;

        var provider = _factory.Create(service);
        string state;
        string? detail = null;
        try
        {
            var health = await provider.CheckAsync(ct);
            detail = health.Detail;
            if (!health.Installed)
            {
                state = "not-installed";
            }
            else if (health.LoggedIn == true)
            {
                state = "ok";
            }
            else if (health.LoggedIn == false)
            {
                state = "logged-out";
            }
            else
            {
                state = "unknown";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            state = "error";
            detail = ex.Message;
        }

        var now = _clock.GetUtcNow();
        _registry.UpsertAccount(new AiAccount($"{service.Id}:default", service.Id, "Mặc định", "local", state, now));
        _details[service.Id] = detail;

        return List().FirstOrDefault(m => string.Equals(m.Id, service.Id, StringComparison.OrdinalIgnoreCase));
    }
}
