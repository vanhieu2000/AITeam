using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class AccountsPageTests
{
    private readonly FakeHealthService _fakeHealth = new();
    private readonly FakeTeamConfigService _fakeConfig = new();

    private AccountsModel CreatePageModel()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new AccountsModel(_fakeHealth, _fakeConfig)
        {
            PageContext = pageContext
        };
    }

    [Fact]
    public void OnGet_loads_github_antigravity_claude_info()
    {
        var page = CreatePageModel();
        page.OnGet();

        Assert.NotNull(page.GitHubHealth);
        Assert.NotNull(page.AgyHealth);
        Assert.NotNull(page.ClaudeHealth);
        Assert.Equal("HieuDV", page.GitHubOwner);
        Assert.Equal(2, page.AgyAccounts.Count);
    }

    [Fact]
    public async Task CheckAgyLogin_returns_health_item_for_normal_request()
    {
        var page = CreatePageModel();
        var result = await page.OnPostCheckAgyLoginAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.AgyLoginCheckResult);
        Assert.Equal(HealthLevel.Ok, page.AgyLoginCheckResult.Level);
    }

    [Fact]
    public async Task CheckAgyLogin_returns_partial_for_htmx_request()
    {
        var page = CreatePageModel();
        page.HttpContext.Request.Headers["HX-Request"] = "true";

        var result = await page.OnPostCheckAgyLoginAsync(CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_AgyLoginResult", partial.ViewName);
        var item = Assert.IsType<HealthItem>(partial.Model);
        Assert.Equal(HealthLevel.Ok, item.Level);
    }

    [Fact]
    public void SwitchAccount_sets_instruction_message()
    {
        var page = CreatePageModel();
        var result = page.OnPostSwitchAccount();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.Message);
        Assert.Contains("/logout", page.Message);
        Assert.Contains("/quit", page.Message);
    }

    [Fact]
    public void InstallClaude_sets_success_message()
    {
        var page = CreatePageModel();
        var result = page.OnPostInstallClaude();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.Message);
        Assert.Contains("skill aiteam", page.Message);
    }
}
