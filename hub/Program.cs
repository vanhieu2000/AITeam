using AITeam.Hub.Models;
using AITeam.Hub.Services;

// AITeam Hub — thiết kế: ../docs/HUB-DESIGN.md. Chỉ nghe 127.0.0.1.
var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("HUB_PORT") ?? "5057";
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

builder.Services.AddSingleton(HubPaths.Detect());
builder.Services.AddSingleton<IWorkflowRolesService, WorkflowRolesService>();
builder.Services.AddSingleton<IWorkProgressService, WorkProgressService>();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

if (Environment.GetEnvironmentVariable("HUB_EMPTY") == "1")
{
    builder.Services.AddSingleton<ITeamConfigService, EmptyTeamConfigService>();
    builder.Services.AddSingleton<ITaskBoardService, EmptyTaskBoardService>();
    builder.Services.AddSingleton<IAgentRunService, EmptyAgentRunService>();
    builder.Services.AddSingleton<IHealthService, AITeam.Hub.DevData.FakeHealthService>();
    builder.Services.AddSingleton<IVaultService, AITeam.Hub.DevData.FakeVaultService>();
    builder.Services.AddSingleton<IInboxService, AITeam.Hub.DevData.FakeInboxService>();
    builder.Services.AddSingleton<IProcessStarter, NoopProcessStarter>();
}
else if (Environment.GetEnvironmentVariable("HUB_FAKE") == "1")
{
    // H-002: dữ liệu giả để làm / xem giao diện.
    AITeam.Hub.DevData.FakeServices.Register(builder.Services);
}
else
{
    // H-001: dịch vụ thật đọc file AITeam + repo dự án.
    builder.Services.AddSingleton<ITeamConfigService, TeamConfigService>();
    builder.Services.AddSingleton<IAgentRunService, AgentRunService>();
    builder.Services.AddSingleton<ITaskBoardService, TaskBoardService>();
    builder.Services.AddSingleton<IHealthService, HealthService>();
    builder.Services.AddSingleton<IVaultService, VaultService>();
    builder.Services.AddSingleton<IInboxService, InboxService>();
}

builder.Services.AddHubV3();
builder.Services.AddLocalChat(Environment.GetEnvironmentVariable("HUB_FAKE") == "1" || Environment.GetEnvironmentVariable("HUB_EMPTY") == "1");
builder.Services.AddSingleton<IOfficeChatService, OfficeChatService>();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseMiddleware<OfficeChatRedirectMiddleware>();
app.MapRazorPages();
app.Run();

/// <summary>Cho test tích hợp (WebApplicationFactory) nếu cần.</summary>
public partial class Program;

internal sealed class EmptyTeamConfigService : ITeamConfigService
{
    public IReadOnlyList<ProjectInfo> GetProjects() => Array.Empty<ProjectInfo>();
    public ProjectInfo? FindProject(string name) => null;
    public TeamSettings GetSettings() => new("", 3, Array.Empty<DeveloperInfo>(), "", "", "", Array.Empty<string>(), "");
    public void SaveSettings(TeamSettings settings) { }
    public void SaveProject(ProjectInfo project) { }
}

internal sealed class EmptyTaskBoardService : ITaskBoardService
{
    public ProjectBoard GetBoard(string project) => new(
        new ProjectInfo(project, "", "", "", "", "", "", new Dictionary<string, string>(), false),
        Array.Empty<TaskCard>(),
        "");
    public TaskDetail? GetTask(string project, string taskId) => null;
}

internal sealed class EmptyAgentRunService : IAgentRunService
{
    public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => Array.Empty<AgentRun>();
    public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => Array.Empty<AgentRun>();
    public IReadOnlyList<RunningAgent> GetRunning() => Array.Empty<RunningAgent>();
    public bool Stop(int pid) => false;
}
