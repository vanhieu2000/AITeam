using AITeam.Hub.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>R-015 (Claude): trang đã bỏ chuyển hướng về trang thay thế; đường dẫn khác không bị đụng.</summary>
public sealed class R015LegacyRedirectTests
{
    private static async Task<(int Status, string? Location, int Calls)> Run(string method, string path)
    {
        var calls = 0;
        var mw = new LegacyPageRedirectMiddleware(_ => { calls++; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        ctx.Request.QueryString = new QueryString("?x=1");
        await mw.InvokeAsync(ctx);
        return (ctx.Response.StatusCode, ctx.Response.Headers.Location.ToString(), calls);
    }

    [Theory]
    [InlineData("/Overview", "/")]
    [InlineData("/work", "/")]
    [InlineData("/Performance", "/Reports")]
    [InlineData("/Team", "/Reports?tab=people")]
    [InlineData("/AI", "/AiServices")]
    [InlineData("/Accounts", "/AiServices")]
    [InlineData("/Providers", "/AiServices")]
    [InlineData("/Process", "/Reports/Lessons")]
    [InlineData("/Map", "/Workflow")]
    public async Task Old_page_get_redirects_to_replacement(string path, string target)
    {
        var (status, location, calls) = await Run("GET", path);
        Assert.Equal(302, status);
        Assert.Equal(target, location);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("GET", "/Reports")]
    [InlineData("GET", "/Work/x")]
    [InlineData("GET", "/Tracking")]
    [InlineData("POST", "/Overview")]
    public async Task Other_requests_pass_through(string method, string path)
    {
        var (status, location, calls) = await Run(method, path);
        Assert.Equal(1, calls);
        Assert.Equal(200, status);
        Assert.True(string.IsNullOrEmpty(location));
    }
}
