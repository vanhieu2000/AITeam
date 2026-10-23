using System.Net;
using AITeam.Hub.DevData;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class AntiforgerySecurityTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Theory]
    [InlineData("/Settings?handler=SaveSettings")]
    [InlineData("/Settings?handler=SaveProject")]
    [InlineData("/Accounts?handler=CheckAgyLogin")]
    [InlineData("/Accounts?handler=SwitchAccount")]
    [InlineData("/Accounts?handler=InstallClaude")]
    [InlineData("/Secrets?handler=Add")]
    [InlineData("/Secrets?handler=Delete")]
    [InlineData("/Secrets?handler=ExportEnv")]
    public async Task Post_without_antiforgery_token_is_rejected_with_bad_request(string pathAndQuery)
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(SettingsModel).Assembly.GetName().Name
        };
        var builder = WebApplication.CreateBuilder(options);
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // Cổng tạm thời (ephemeral)

        // Cấu hình dịch vụ
        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddRazorPages().AddApplicationPart(typeof(SettingsModel).Assembly);
        builder.Services.AddAntiforgery();
        FakeServices.Register(builder.Services);

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();

        await app.StartAsync();

        try
        {
            string url = app.Urls.First();
            using var client = new HttpClient { BaseAddress = new Uri(url) };

            // Gửi POST không có token xác thực antiforgery
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["dummyKey"] = "dummyValue"
            });

            var response = await client.PostAsync(pathAndQuery, content);

            // Mọi POST thiếu antiforgery token đều bị từ chối 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Theory]
    [InlineData("/Settings")]
    [InlineData("/Accounts")]
    [InlineData("/Secrets")]
    // Hub v3 (R-003): dựng trang thật để bắt lỗi thiếu đăng ký dịch vụ (H3-05: /Requests trắng trang).
    [InlineData("/")]
    [InlineData("/Approvals")]
    [InlineData("/Team")]
    [InlineData("/Process")]
    [InlineData("/Requests")]
    public async Task Pages_render_successfully_with_fake_services(string path)
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(SettingsModel).Assembly.GetName().Name
        };
        var builder = WebApplication.CreateBuilder(options);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddRazorPages().AddApplicationPart(typeof(SettingsModel).Assembly);
        builder.Services.AddAntiforgery();
        FakeServices.Register(builder.Services);

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();

        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string html = await response.Content.ReadAsStringAsync();
            Assert.Contains("<!DOCTYPE html>", html);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
