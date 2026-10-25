using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using AITeam.Core.Accounts;
using AITeam.Core.Providers;
using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H6-08 — agent triển khai KHÔNG sửa.
public sealed class AcceptanceH608AiTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private int _probes;
    private string StoreFile => Path.Combine(_env.RootDir, "LocalAppData", "AITeam", "config", "accounts.json");
    public void Dispose() => _env.Dispose();

    private sealed class Runs : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => [];
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => [];
        public IReadOnlyList<RunningAgent> GetRunning() => [];
        public bool Stop(int pid) => throw new InvalidOperationException("Không được dừng tiến trình trong test.");
    }

    private AiAccountsService Service() => new(_env.Paths, new Runs(), new AiAccountStore(StoreFile),
        (id, ct) => { _probes++; return Task.FromResult(new ProviderHealth(id, true, true, "Đã đăng nhập giả")); },
        () => new DateTime(2026, 10, 25, 15, 0, 0));

    // Bọc dịch vụ để đo lời gọi/lỗi đọc, không phụ thuộc cách hiện thực hợp đồng.
    public class ObservedAccounts : DispatchProxy
    {
        public IAiAccountsService Inner { get; set; } = null!;
        public int Lists { get; private set; }
        public Exception? ReadError { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(method);
            if (method.Name == "List")
            {
                Lists++;
                if (ReadError != null) throw ReadError;
            }
            try { return method.Invoke(Inner, args); }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
        }
    }

    private (IAiAccountsService Service, ObservedAccounts Spy) Observe()
    {
        var service = DispatchProxy.Create<IAiAccountsService, ObservedAccounts>();
        var spy = (ObservedAccounts)service;
        spy.Inner = Service();
        return (service, spy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("STATUS")]
    [InlineData("unknown")]
    [InlineData("../vault")]
    [InlineData("<script>alert(1)</script>")]
    public void Unknown_tab_defaults_without_probe(string? tab)
    {
        var (service, spy) = Observe();
        var page = new AIModel(service);
        page.OnGet(tab);
        Assert.Equal("status", page.Tab);
        Assert.Equal(1, spy.Lists);
        Assert.Equal(AiCatalog.All.Select(x => x.Id), page.Rows.Select(x => x.Service.Id));
        Assert.Null(page.Error);
        Assert.Equal(0, _probes);
    }

    [Theory]
    [InlineData("status", 1)]
    [InlineData("accounts", 0)]
    [InlineData("providers", 0)]
    [InlineData("vault", 0)]
    public void Allowed_tab_loads_only_its_data(string tab, int lists)
    {
        var (service, spy) = Observe();
        var page = new AIModel(service);
        page.OnGet(tab);
        Assert.Equal(tab, page.Tab);
        Assert.Equal(lists, spy.Lists);
        Assert.Equal(lists == 1 ? AiCatalog.All.Count : 0, page.Rows.Count);
        Assert.Equal(0, _probes);
    }

    [Fact]
    public void Read_error_has_fixed_safe_message()
    {
        var (service, spy) = Observe();
        spy.ReadError = new IOException(string.Concat("sk-", new string('a', 32)));
        var page = new AIModel(service);
        page.OnGet();
        Assert.Empty(page.Rows);
        Assert.Equal("Không đọc được danh sách tài khoản AI.", page.Error);
        Assert.Equal(1, spy.Lists);
    }

    private async Task WithHub(IAiAccountsService service, Func<HttpClient, Task> act)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { ApplicationName = typeof(AIModel).Assembly.GetName().Name });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddSingleton(service);
        builder.Services.AddRazorPages().AddApplicationPart(typeof(AIModel).Assembly);
        builder.Services.AddAntiforgery();
        FakeServices.Register(builder.Services);
        await using var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();
        await app.StartAsync();
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.First()) };
            await act(client);
        }
        finally { await app.StopAsync(); }
    }

    [Theory]
    [InlineData("status", "Trạng thái")]
    [InlineData("accounts", "Tài khoản")]
    [InlineData("providers", "Nhà cung cấp")]
    [InlineData("vault", "Kho bí mật")]
    public Task Every_tab_renders_real_razor(string tab, string label) => WithHub(Service(), async client =>
    {
        var response = await client.GetAsync("/AI?tab=" + tab);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(raw);
        Assert.Contains("AI & tài khoản", html);
        Assert.Contains("aria-label=\"Các tab AI\"", html);
        foreach (var key in new[] { "status", "accounts", "providers", "vault" })
            Assert.Contains("/AI?tab=" + key, html);
        var selected = Regex.Matches(html, "<a\\b[^>]*aria-current=\"page\"[^>]*>.*?</a>", RegexOptions.Singleline);
        Assert.Single(selected.Cast<Match>());
        Assert.Contains("/AI?tab=" + tab, selected[0].Value);
        Assert.Contains(label, selected[0].Value);
        Assert.Equal(0, _probes);
    });

    [Theory]
    [InlineData("accounts", "/AiAccounts")]
    [InlineData("providers", "/Providers")]
    [InlineData("vault", "/Vault")]
    public Task Shell_links_to_existing_page(string tab, string target) => WithHub(Service(), async client =>
    {
        var html = await client.GetStringAsync("/AI?tab=" + tab);
        Assert.Contains("href=\"" + target + "\"", html);
        Assert.DoesNotContain("<tr id=\"acc-", html);
    });

    [Fact]
    public Task Default_and_malicious_tab_render_status() => WithHub(Service(), async client =>
    {
        foreach (var url in new[] { "/AI", "/AI?tab=" + Uri.EscapeDataString("<script>alert(42)</script>") })
        {
            var html = await client.GetStringAsync(url);
            Assert.Contains("id=\"acc-codex\"", html);
            Assert.DoesNotContain("<script>alert(42)</script>", html);
            Assert.Contains("__RequestVerificationToken", html);
        }
    });

    [Fact]
    public Task Existing_modes_alias_and_forms_render_safely()
    {
        var service = Service();
        const string alias = "<b>tài khoản cũ</b>";
        service.Save("codex", alias, "Disabled");
        return WithHub(service, async client =>
        {
            var html = await client.GetStringAsync("/AI");
            var decoded = WebUtility.HtmlDecode(html);
            Assert.Contains("Chưa kiểm tra", decoded);
            Assert.Contains("Thủ công", decoded);
            Assert.Contains("Tắt", decoded);
            Assert.Contains("Không lưu mật khẩu, cookie hay khóa API.", decoded);
            Assert.Contains("&lt;b&gt;", html);
            Assert.DoesNotContain(alias, html);
            foreach (var row in service.List()) Assert.Contains("id=\"acc-" + row.Service.Id + "\"", html);
            var forms = Regex.Matches(html, "<form\\b[^>]*>.*?</form>", RegexOptions.Singleline);
            Assert.NotEmpty(forms.Cast<Match>());
            foreach (Match form in forms)
            {
                Assert.Contains("/AiAccounts?handler=", form.Value);
                Assert.Matches("handler=(Save|Disable)", form.Value);
                Assert.Contains("__RequestVerificationToken", form.Value);
            }
            Assert.Contains("/AiAccounts?handler=Check", decoded);
            Assert.Equal(0, _probes);
        });
    }

    [Fact]
    public Task Read_error_does_not_leak_secret_in_html()
    {
        var (service, spy) = Observe();
        var secret = string.Concat("sk-", new string('z', 32));
        spy.ReadError = new IOException(secret);
        return WithHub(service, async client =>
        {
            var html = await client.GetStringAsync("/AI");
            Assert.Contains("Không đọc được danh sách tài khoản AI.", WebUtility.HtmlDecode(html));
            Assert.DoesNotContain(secret, html);
        });
    }

    [Theory]
    [InlineData("Save")]
    [InlineData("Disable")]
    public Task New_page_forms_require_antiforgery_on_old_endpoint(string handler) => WithHub(Service(), async client =>
    {
        var html = await client.GetStringAsync("/AI");
        Assert.Contains("__RequestVerificationToken", html);
        var response = await client.PostAsync("/AiAccounts?handler=" + handler,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "codex", ["mode"] = "Manual" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(File.Exists(StoreFile));
    });

    [Fact]
    public Task Old_url_and_check_use_fake_probe() => WithHub(Service(), async client =>
    {
        Assert.Contains("id=\"acc-codex\"", await client.GetStringAsync("/AI"));
        Assert.Contains("id=\"acc-codex\"", await client.GetStringAsync("/AiAccounts"));
        Assert.Equal(0, _probes);
        var response = await client.GetAsync("/AiAccounts?handler=Check&id=codex");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Đã đăng nhập giả", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(1, _probes);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/AiAccounts?handler=Check&id=..%2Fx")).StatusCode);
    });

    [Theory]
    [InlineData("Save")]
    [InlineData("Disable")]
    public Task Valid_token_keeps_old_redirect_and_alias(string handler)
    {
        var service = Service();
        service.Save("codex", "Nhãn cũ", "Manual");
        return WithHub(service, async client =>
        {
            var html = await client.GetStringAsync("/AI");
            var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(token.Success, "Trang mới phải dựng token thật.");
            var response = await client.PostAsync("/AiAccounts?handler=" + handler,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
                    ["id"] = "codex", ["alias"] = "Nhãn mới", ["mode"] = "Manual"
                }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/AiAccounts", response.Headers.Location?.OriginalString);
            var saved = service.List().Single(x => x.Service.Id == "codex");
            Assert.Equal(handler == "Disable" ? "Nhãn cũ" : "Nhãn mới", saved.Alias);
            Assert.Equal(handler == "Disable" ? AiMode.Disabled : AiMode.Manual, saved.Mode);
        });
    }

    private sealed class NoTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    [Fact]
    public void Save_error_keeps_legacy_message_and_data()
    {
        var service = Service();
        var newPage = new AIModel(service);
        newPage.OnGet();
        Assert.NotEmpty(newPage.Rows);
        var oldPage = new AiAccountsModel(service)
        { TempData = new TempDataDictionary(new DefaultHttpContext(), new NoTempData()) };
        Assert.IsType<RedirectToPageResult>(oldPage.OnPostSave("codex", "Không lưu", "9"));
        Assert.Equal("Chế độ không hợp lệ.", oldPage.ErrorMessage);
        Assert.False(File.Exists(StoreFile));
    }
}