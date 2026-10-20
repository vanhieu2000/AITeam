using AITeam.Hub.Services;

// AITeam Hub — thiết kế: ../docs/HUB-DESIGN.md. Chỉ nghe 127.0.0.1.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5057");

builder.Services.AddSingleton(HubPaths.Detect());
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

if (Environment.GetEnvironmentVariable("HUB_FAKE") == "1")
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

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorPages();
app.Run();

/// <summary>Cho test tích hợp (WebApplicationFactory) nếu cần.</summary>
public partial class Program;
