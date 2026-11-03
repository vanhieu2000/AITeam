using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Process;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AITeam.Core.Accounts;
using AITeam.Core.AiServices;
using AITeam.Core.Workflow;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Dịch vụ Hub v3 (R-003): đọc file AITeam thật ở mọi chế độ (thật / giả / rỗng) — dữ liệu nhỏ, chỉ đọc + ghi quyết định duyệt.
/// _Layout dùng IApprovalService nên mọi cấu hình dịch vụ (kể cả test dựng trang) phải gọi hàm này.</summary>
public static class HubV3Registration
{
    public static IServiceCollection AddHubV3(this IServiceCollection services)
    {
        services.TryAddSingleton<IApprovalService, ApprovalService>();
        // V10-03: nhật ký "Claude tự quyết" (approvals/claude-decisions.jsonl, append-only).
        services.TryAddSingleton<IClaudeDecisionLog, ClaudeDecisionLog>();
        services.TryAddSingleton<IGitHistory, GitHistory>();
        services.TryAddSingleton<WorkOverviewService>();
        services.TryAddSingleton<WorkDetailService>();
        services.TryAddSingleton<PerformanceService>();
        // V10-04: số liệu báo cáo & thống kê (lọc ngày/dự án/người/độ khó, điểm, ma trận lỗi, dòng thời gian, CSV).
        services.TryAddSingleton<IReportService>(sp => new ReportService(
            sp.GetRequiredService<HubPaths>(), sp.GetRequiredService<ITeamConfigService>(),
            sp.GetRequiredService<ITaskBoardService>(), sp.GetRequiredService<IGitHistory>()));
        services.TryAddSingleton<ITeamInsightsService, TeamInsightsService>();
        services.TryAddSingleton<IRequestService, RequestService>();
        services.TryAddSingleton<IAiReviewService, AiReviewService>();
        services.TryAddSingleton<IOfficeService, OfficeService>();
        // W2-01 / R-014: cấu hình loại việc → bước → người phụ trách (config/workflow.json).
        services.TryAddSingleton<IWorkflowConfigService>(sp => new WorkflowConfigService(
            sp.GetRequiredService<HubPaths>().AITeamRoot));
        // V5-06: tài khoản AI (chỉ metadata, %LOCALAPPDATA%\AITeam\config\accounts.json).
        // V5-07b: phòng nghiên cứu (connector thật: Codex CLI, Gemini CLI).
        services.TryAddSingleton<IResearchService>(sp => new ResearchService(
            sp.GetRequiredService<HubPaths>(), sp.GetRequiredService<IRequestService>(), sp.GetRequiredService<IAiAccountsService>()));
        // H8-13: HUB_FAKE dùng tài khoản giả trong bộ nhớ, không ghi accounts.json thật.
        if (Environment.GetEnvironmentVariable("HUB_FAKE") == "1")
            services.TryAddSingleton<IAiAccountsService, FakeAiAccountsService>();
        else
            services.TryAddSingleton<IAiAccountsService>(sp => new AiAccountsService(sp.GetRequiredService<HubPaths>(), sp.GetRequiredService<IAgentRunService>()));
        // V10-01: danh bạ dịch vụ AI trong SQLite cục bộ (không lưu giá trị bí mật).
        services.TryAddSingleton<IAiServiceRegistry>(sp =>
        {
            var paths = sp.GetRequiredService<HubPaths>();
            if (Environment.GetEnvironmentVariable("HUB_FAKE") == "1")
            {
                var demo = new SqliteAiServiceRegistry(":memory:", paths.AITeamRoot);
                demo.SeedSampleServices();
                return demo;
            }
            if (Environment.GetEnvironmentVariable("HUB_EMPTY") == "1")
                return new SqliteAiServiceRegistry(":memory:", paths.AITeamRoot);
            return new SqliteAiServiceRegistry(
                Path.Combine(paths.UserProfile, ".aiteam", "hub", "office.db"),
                paths.AITeamRoot,
                AiAccountStore.DefaultFile);
        });
        // V5-05: điều khiển từ Văn phòng. Chế độ giả / rỗng đăng ký NoopProcessStarter TRƯỚC (không chạy lệnh thật).
        services.TryAddSingleton<IProcessStarter, RealProcessStarter>();
        services.TryAddSingleton<ICommandService>(sp => new CommandService(
            sp.GetRequiredService<HubPaths>(), sp.GetRequiredService<ITeamConfigService>(), sp.GetRequiredService<ITaskBoardService>(), sp.GetRequiredService<IProcessStarter>()));
        // V10-02 (R-014): Chạy AI theo danh bạ - Factory và ISecretLookup
        services.TryAddSingleton<VaultService>(sp =>
        {
            var vault = sp.GetService<IVaultService>() as VaultService;
            return vault ?? new VaultService(sp.GetRequiredService<HubPaths>(), sp.GetService<ITeamConfigService>());
        });
        services.TryAddSingleton<ISecretLookup, VaultSecretLookup>();
        services.TryAddSingleton<IAiProviderFactory>(sp => new AiProviderFactory(
            sp.GetRequiredService<IAiServiceRegistry>(),
            sp.GetService<IProcessRunner>() ?? new ProcessRunner(),
            sp.GetRequiredService<ISecretLookup>()));
        // V10-05 (R-014): trợ lý chat
        services.TryAddSingleton<IChatAssistant, ChatAssistant>();
        return services;
    }
}
