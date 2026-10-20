using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class AgentsInboxPageTests
{
    private sealed class FakeAntiforgery : IAntiforgery
    {
        public bool IsValid { get; set; } = true;

        public Task<AntiforgeryTokenSet> GetAndStoreTokensAsync(HttpContext httpContext) =>
            Task.FromResult(new AntiforgeryTokenSet("request", "cookie", "form", "header"));

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) =>
            new("request", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) =>
            new("request", "cookie", "form", "header");

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) =>
            Task.FromResult(IsValid);

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            if (!IsValid) throw new AntiforgeryValidationException("Token không hợp lệ");
            return Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

    private sealed class TrackingAgentRunService : IAgentRunService
    {
        public int? StoppedPid { get; private set; }
        public bool StopReturnValue { get; set; } = true;

        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => Array.Empty<AgentRun>();
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => Array.Empty<AgentRun>();
        public IReadOnlyList<RunningAgent> GetRunning() => new List<RunningAgent>
        {
            new(9876, DateTime.Now.AddMinutes(-5), "AITeam", "H-003", "agy.exe")
        };

        public bool Stop(int pid)
        {
            StoppedPid = pid;
            return StopReturnValue;
        }
    }

    [Fact]
    public void Agents_page_has_validate_antiforgery_token_attribute()
    {
        var hasAttr = typeof(AgentsModel).IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true);
        Assert.True(hasAttr, "AgentsModel bắt buộc phải có attribute [ValidateAntiForgeryToken].");
    }

    [Fact]
    public void Agents_page_loads_running_agents_and_recent_runs()
    {
        var runService = new FakeAgentRunService();
        var page = new AgentsModel(runService);

        page.OnGet();

        Assert.NotEmpty(page.RunningAgents);
        Assert.Equal(14280, page.RunningAgents[0].Pid);
        Assert.Equal("H-001", page.RunningAgents[0].Task);

        Assert.NotEmpty(page.RecentRuns);
        Assert.True(page.RecentRuns.Count >= 3);
    }

    [Fact]
    public void Agents_page_running_table_handler_returns_partial_view()
    {
        var runService = new FakeAgentRunService();
        var page = new AgentsModel(runService);

        var result = page.OnGetRunningTable();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_AgentsRunningTablePartial", partial.ViewName);
        var model = Assert.IsAssignableFrom<IReadOnlyList<RunningAgent>>(partial.Model);
        Assert.NotEmpty(model);
    }

    [Fact]
    public async Task Agents_stop_calls_stop_with_exact_pid()
    {
        var runService = new TrackingAgentRunService();
        var page = new AgentsModel(runService);

        var result = await page.OnPostStopAsync(9876);

        Assert.Equal(9876, runService.StoppedPid);
        Assert.NotNull(page.Message);
        Assert.Contains("9876", page.Message);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task Agents_stop_failure_sets_error_message()
    {
        var runService = new TrackingAgentRunService { StopReturnValue = false };
        var page = new AgentsModel(runService);

        var result = await page.OnPostStopAsync(5555);

        Assert.Equal(5555, runService.StoppedPid);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("5555", page.ErrorMessage);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task Agents_stop_without_antiforgery_token_is_rejected()
    {
        var runService = new TrackingAgentRunService();
        var fakeAntiforgery = new FakeAntiforgery { IsValid = false };
        var page = new AgentsModel(runService, fakeAntiforgery)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await page.OnPostStopAsync(9876);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Null(runService.StoppedPid); // Không được gọi Stop nếu antiforgery từ chối
    }

    [Fact]
    public void Inbox_page_has_validate_antiforgery_token_attribute()
    {
        var hasAttr = typeof(InboxModel).IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true);
        Assert.True(hasAttr, "InboxModel bắt buộc phải có attribute [ValidateAntiForgeryToken].");
    }

    [Fact]
    public void Inbox_page_loads_projects_and_groups_items()
    {
        var inboxService = new FakeInboxService();
        var configService = new FakeTeamConfigService();
        var page = new InboxModel(inboxService, configService);

        page.OnGet();

        Assert.Equal(3, page.Projects.Count);
        Assert.Contains(page.Projects, p => p.Name == "ChinaAndroidToolbox");

        Assert.NotEmpty(page.PendingItems);
        Assert.All(page.PendingItems, item => Assert.False(item.Done));

        Assert.NotEmpty(page.DoneItems);
        Assert.All(page.DoneItems, item => Assert.True(item.Done));
    }

    [Fact]
    public async Task Inbox_add_with_empty_or_whitespace_text_reports_error_and_does_not_save()
    {
        var inboxService = new FakeInboxService();
        var configService = new FakeTeamConfigService();
        int initialCount = inboxService.GetItems().Count;

        var page = new InboxModel(inboxService, configService)
        {
            Project = "AITeam",
            Text = "   "
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.True(page.ModelState.ContainsKey("Text"));
        Assert.Equal(initialCount, inboxService.GetItems().Count);
    }

    [Fact]
    public async Task Inbox_add_with_valid_text_saves_item_and_redirects()
    {
        var inboxService = new FakeInboxService();
        var configService = new FakeTeamConfigService();
        int initialCount = inboxService.GetItems().Count;

        var page = new InboxModel(inboxService, configService)
        {
            Project = "ChinaAndroidToolbox",
            Text = "Cần bổ sung tính năng kiểm tra cổng ADB"
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var items = inboxService.GetItems();
        Assert.Equal(initialCount + 1, items.Count);
        var lastItem = items.Last();
        Assert.Equal("ChinaAndroidToolbox", lastItem.Project);
        Assert.Contains("kiểm tra cổng ADB", lastItem.Text);
        Assert.False(lastItem.Done);
    }

    [Fact]
    public async Task Inbox_add_with_empty_project_defaults_to_new_project()
    {
        var inboxService = new FakeInboxService();
        var configService = new FakeTeamConfigService();

        var page = new InboxModel(inboxService, configService)
        {
            Project = "",
            Text = "Yêu cầu cho dự án chưa xác định"
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var lastItem = inboxService.GetItems().Last();
        Assert.Equal("Dự án mới", lastItem.Project);
    }

    [Fact]
    public async Task Inbox_without_antiforgery_token_is_rejected()
    {
        var inboxService = new FakeInboxService();
        var configService = new FakeTeamConfigService();
        var fakeAntiforgery = new FakeAntiforgery { IsValid = false };
        int initialCount = inboxService.GetItems().Count;

        var page = new InboxModel(inboxService, configService, fakeAntiforgery)
        {
            Project = "AITeam",
            Text = "Tin nhắn không có token",
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await page.OnPostAsync();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(initialCount, inboxService.GetItems().Count);
    }

    [Fact]
    public void Sidebar_config_service_returns_ChinaAndroidToolbox()
    {
        var config = new FakeTeamConfigService();
        var projects = config.GetProjects();

        Assert.Contains(projects, p => p.Name == "ChinaAndroidToolbox");
        var china = Assert.Single(projects, p => p.Name == "ChinaAndroidToolbox");
        Assert.Equal("HieuDV/ChinaAndroidToolbox", china.Repo);
        Assert.Equal("ChinaAndroidToolbox.sln", china.Build.Split(' ').Last());
    }
}
