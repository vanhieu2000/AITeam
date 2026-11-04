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

    public IReadOnlyList<AiMemberStatus> List() => throw new NotImplementedException(); // TODO(agent)

    public Task<AiMemberStatus?> CheckAsync(string id, CancellationToken ct) => throw new NotImplementedException(); // TODO(agent)
}
