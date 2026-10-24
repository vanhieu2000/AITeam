using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AITeam.Hub.Services;

/// <summary>Dịch vụ Hub v3 (R-003): đọc file AITeam thật ở mọi chế độ (thật / giả / rỗng) — dữ liệu nhỏ, chỉ đọc + ghi quyết định duyệt.
/// _Layout dùng IApprovalService nên mọi cấu hình dịch vụ (kể cả test dựng trang) phải gọi hàm này.</summary>
public static class HubV3Registration
{
    public static IServiceCollection AddHubV3(this IServiceCollection services)
    {
        services.TryAddSingleton<IApprovalService, ApprovalService>();
        services.TryAddSingleton<IGitHistory, GitHistory>();
        services.TryAddSingleton<ITeamInsightsService, TeamInsightsService>();
        services.TryAddSingleton<IRequestService, RequestService>();
        services.TryAddSingleton<IAiReviewService, AiReviewService>();
        services.TryAddSingleton<IOfficeService, OfficeService>();
        return services;
    }
}
