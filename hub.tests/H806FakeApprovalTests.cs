using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Tests.Support;
namespace AITeam.Hub.Tests;
public sealed class H806FakeApprovalTests
{
    [Fact] public async Task Fake_page_has_two_pending_rows_with_inline_actions_and_writes_no_files()
    {
        using var environment = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(environment);
        var before = Directory.GetFiles(environment.RootDir, "*", SearchOption.AllDirectories);
        var html = await host.Client.GetStringAsync("/Approvals");
        Assert.Contains("A-901", html); Assert.Contains("A-902", html);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "value=\"approve\"").Count);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "value=\"reject\"").Count);
        var service = (AITeam.Hub.Services.IApprovalService)host.Services.GetService(typeof(AITeam.Hub.Services.IApprovalService))!;
        Assert.IsType<FakeApprovalService>(service);
        Assert.Equal(ApprovalStatus.Approved, service.Decide("A-901", true, "  Đã xem mẫu  ").Status);
        Assert.Equal(ApprovalStatus.Rejected, service.Decide("A-902", false, null).Status);
        Assert.Throws<InvalidOperationException>(() => service.Decide("A-901", false, null));
        Assert.Equal(before, Directory.GetFiles(environment.RootDir, "*", SearchOption.AllDirectories));
    }
}
