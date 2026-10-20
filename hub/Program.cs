using AITeam.Hub.Services;

// AITeam Hub — thiết kế: ../docs/HUB-DESIGN.md. Chỉ nghe 127.0.0.1.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5057");

builder.Services.AddSingleton(HubPaths.Detect());
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

// H-001 (Agent A): đăng ký các dịch vụ thật khi biến môi trường HUB_FAKE khác 1
if (Environment.GetEnvironmentVariable("HUB_FAKE") != "1")
{
    builder.Services.AddSingleton<ITeamConfigService, TeamConfigService>();
    builder.Services.AddSingleton<IAgentRunService, AgentRunService>();
    builder.Services.AddSingleton<ITaskBoardService, TaskBoardService>();
    builder.Services.AddSingleton<IHealthService, HealthService>();
    builder.Services.AddSingleton<IVaultService, VaultService>();
    builder.Services.AddSingleton<IInboxService, InboxService>();
}

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorPages();
app.Run();

/// <summary>Cho test tích hợp (WebApplicationFactory) nếu cần.</summary>
public partial class Program;
