using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

/// <summary>Nghiệm thu H7-06: render Razor trong bộ nhớ, không mở app hay chạy tiến trình.</summary>
public sealed class AcceptanceH706NavigationTests
{
    private static string Root([CallerFilePath] string file = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, ".."));
    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
    private static string[] Elements(string html, string tag) => Regex.Matches(html, $@"<{tag}\b[^>]*>[\s\S]*?</{tag}>", RegexOptions.IgnoreCase).Select(m => m.Value).ToArray();
    private static ProjectInfo Project(string name) => new(name, "owner/repo", "", "", "C#", "", "", new Dictionary<string, string>(), false);
    private static Approval Item(ApprovalStatus status, ApprovalKind kind = ApprovalKind.Merge) => new(
        "A-001", kind, "Duyệt thay đổi", "Nội dung cũ", "AITeam", "H7-06", null, [], "Claude",
        new DateTime(2026, 10, 24, 13, 45, 0), status, status == ApprovalStatus.Pending ? null : new DateTime(2026, 10, 24, 14, 0, 0), "Ghi chú cũ");

    // Proxy chỉ thay dịch vụ I/O; Razor, TagHelper, antiforgery và PageModel đều là mã thật.
    public class ServiceDouble : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
    }
    private static T Fake<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var service = DispatchProxy.Create<T, ServiceDouble>();
        ((ServiceDouble)(object)service).Handler = handler;
        return service;
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly TestEnvironment _env = new();
        public IReadOnlyList<ProjectInfo> Projects = [];
        public IReadOnlyList<Approval> Approvals = [];
        public int ConfigReads, BoardReads, RunReads, Writes;
        public Exception? ConfigError, DecisionError;
        public (string Id, bool Approve, string? Note)? LastDecision;
        public ITeamConfigService Config { get; }
        public IApprovalService ApprovalService { get; }
        public Fixture()
        {
            Config = Fake<ITeamConfigService>((m, _) =>
            {
                Assert.Equal("GetProjects", m.Name);
                ConfigReads++;
                if (ConfigError is not null) throw ConfigError;
                return Projects;
            });
            ApprovalService = Fake<IApprovalService>((m, args) =>
            {
                if (m.Name == "GetAll") return Approvals;
                Assert.Equal("Decide", m.Name);
                Writes++;
                LastDecision = ((string)args![0]!, (bool)args[1]!, (string?)args[2]);
                if (DecisionError is not null) throw DecisionError;
                return Approvals.Single(a => a.Id == (string)args[0]!);
            });
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(ProjectsModel).Assembly.GetName().Name,
                ContentRootPath = _env.AITeamRoot, EnvironmentName = "Development"
            });
            builder.Services.AddRazorPages().AddApplicationPart(typeof(ProjectsModel).Assembly);
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddSingleton(_env.Paths);
            builder.Services.AddSingleton(Config);
            builder.Services.AddSingleton(ApprovalService);
            builder.Services.AddSingleton(Fake<ITaskBoardService>((_, args) =>
            {
                BoardReads++;
                return new ProjectBoard(Projects.Single(p => p.Name == (string)args![0]!), [], "");
            }));
            builder.Services.AddSingleton(Fake<IAgentRunService>((m, _) =>
            {
                RunReads++;
                return Array.CreateInstance(m.ReturnType.GetGenericArguments()[0], 0);
            }));
            _app = builder.Build();
            _app.MapRazorPages();
        }
        public async Task<string> Render(string page, object model, string? routePage = null)
        {
            using var scope = _app.Services.CreateScope();
            var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            http.Request.Scheme = "http";
            http.Request.Host = new HostString("localhost");
            http.Request.Path = page;
            var route = new RouteData();
            route.Values["page"] = routePage ?? page;
            http.Request.RouteValues = route.Values;
            http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Nghiệm thu"));
            var action = new ActionContext(http, route, new CompiledPageActionDescriptor { RouteValues = new Dictionary<string, string?> { ["page"] = routePage ?? page } });
            var services = scope.ServiceProvider;
            var currentPageName = routePage ?? page;
            string BuildUrl(object? values)
            {
                var dict = new RouteValueDictionary(values);
                var handler = dict.TryGetValue("handler", out var hv) ? hv as string : null;
                var pageValue = dict.TryGetValue("page", out var pv) ? pv as string : null;
                var controller = dict.TryGetValue("controller", out var cv) ? cv as string : null;
                var actionName = dict.TryGetValue("action", out var av) ? av as string : null;
                var target = pageValue
                    ?? (controller is not null && actionName is not null ? "/" + controller + "/" + actionName : currentPageName);
                return handler is null ? target : target + "?handler=" + handler;
            }
            var urlHelper = Fake<IUrlHelper>((method, args) => method.Name switch
            {
                "get_ActionContext" => action,
                "Content" => "/" + (args![0] as string ?? string.Empty).TrimStart('~').TrimStart('/'),
                "IsLocalUrl" => true,
                "RouteUrl" when args![0] is UrlRouteContext routeContext => BuildUrl(routeContext.Values),
                "RouteUrl" when args![1] is object routeValues => BuildUrl(routeValues),
                "Action" when args![0] is UrlActionContext actionContext => BuildUrl(actionContext.Values),
                "Link" when args![1] is object linkValues => BuildUrl(linkValues),
                _ => null
            });
            var urlFactory = Fake<IUrlHelperFactory>((_, _) => urlHelper);
            IServiceProvider? providerProxy = null;
            IHtmlGenerator? generator = null;
            providerProxy = Fake<IServiceProvider>((_, args) =>
            {
                var serviceType = (Type)args![0]!;
                if (serviceType == typeof(IUrlHelperFactory)) return urlFactory;
                if (serviceType == typeof(IHtmlGenerator)) return generator ??= ActivatorUtilities.CreateInstance<DefaultHtmlGenerator>(providerProxy!);
                return services.GetService(serviceType);
            });
            http.RequestServices = providerProxy;
            var factory = services.GetRequiredService<IRazorPageFactoryProvider>();
            var result = factory.CreateFactory($"/Pages{page}.cshtml");
            Assert.True(result.Success, $"Không tìm thấy trang Razor {page}.");
            var razor = result.RazorPageFactory!();
            razor.Layout = "/Pages/Shared/_Layout.cshtml";
            var view = new RazorView(services.GetRequiredService<IRazorViewEngine>(),
                services.GetRequiredService<IRazorPageActivator>(), [], razor, HtmlEncoder.Default,
                services.GetRequiredService<DiagnosticListener>());
            var metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
            var data = (ViewDataDictionary)Activator.CreateInstance(
                typeof(ViewDataDictionary<>).MakeGenericType(model.GetType()), metadataProvider, new ModelStateDictionary())!;
            data.Model = model;
            using var writer = new StringWriter();
            var context = new ViewContext(action, view, data,
                new TempDataDictionary(http, services.GetRequiredService<ITempDataProvider>()), writer, new HtmlHelperOptions());
            if (razor is Page razorPage)
            {
                razorPage.PageContext = new PageContext(action) { ViewData = data };
                razorPage.ViewContext = context;
                if (model is PageModel pageModel)
                {
                    pageModel.PageContext = razorPage.PageContext;
                }
            }
            await view.RenderAsync(context);
            return writer.ToString();
        }
        public async Task<string> RenderProjects(string? route = null)
        {
            var model = new ProjectsModel(Config);
            model.OnGet();
            return await Render("/Projects", model, route);
        }
        public async Task<string> RenderApprovals()
        {
            var model = new ApprovalsModel(ApprovalService);
            model.OnGet();
            return await Render("/Approvals", model);
        }
        public async Task<AuthorizationFilterContext> MissingToken()
        {
            using var scope = _app.Services.CreateScope();
            var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            http.Request.Method = "POST";
            http.Request.Path = "/Approvals";
            var filter = (IAsyncAuthorizationFilter)new ValidateAntiForgeryTokenAttribute().CreateInstance(scope.ServiceProvider);
            // Bộ lọc antiforgery chỉ chạy khi chính nó là chính sách gần nhất trong Filters (IsEffectivePolicy).
            var context = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), [(IFilterMetadata)filter]);
            await filter.OnAuthorizationAsync(context);
            return context;
        }
        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync();
            _env.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Danh_sach_moi_du_an_ke_ca_chua_co_cuc_bo(int count)
    {
        await using var f = new Fixture();
        f.Projects = Enumerable.Range(0, count).Select(i => Project("DuAn" + i)).ToArray();
        var html = await f.RenderProjects();
        var main = Regex.Match(html, @"<main\b[\s\S]*?</main>").Value;
        Assert.Equal(count, Regex.Matches(main, "href=\"/p/").Count);
        foreach (var p in f.Projects) Assert.Contains(p.Name, Text(main));
        Assert.Equal(count == 0, Text(main).Contains("Chưa có dự án.", StringComparison.Ordinal));
        Assert.Equal(1, f.ConfigReads);
        Assert.Equal(0, f.BoardReads);
        Assert.Equal(0, f.RunReads);
    }

    [Fact]
    public async Task Loi_cau_hinh_khong_gia_lam_danh_sach_rong()
    {
        await using var f = new Fixture();
        var error = new IOException("Không đọc được cấu hình.");
        f.ConfigError = error;
        Assert.Same(error, Assert.Throws<IOException>(() => new ProjectsModel(f.Config).OnGet()));
    }

    [Theory]
    [InlineData("/Office", "/")]
    [InlineData("/Approvals", "/Approvals")]
    [InlineData("/Projects", "/Projects")]
    [InlineData("/Project", "/Projects")]
    [InlineData("/Performance", "/Performance")]
    [InlineData("/Process", "/Process")]
    [InlineData("/Settings", "/Settings")]
    [InlineData("/AI", "/AI")]
    [InlineData("/Map", "/Map")]
    public async Task Menu_chinh_xac_va_chi_mot_muc_active(string page, string active)
    {
        await using var f = new Fixture();
        var html = await f.RenderProjects(page);
        var side = Regex.Match(html, @"<aside\b[\s\S]*?</aside>").Value;
        var links = Elements(side, "a");
        // H8-14 (R-012): thêm "Trò chuyện AI" ngay sau Văn phòng.
        Assert.Equal(new[] { "/", "/Chat", "/Approvals", "/Projects", "/Performance", "/Process", "/Settings", "/AI", "/Map" },
            links.Select(a => Regex.Match(a, "href=\"([^\"]+)\"").Groups[1].Value));
        var selected = Assert.Single(links, a => Regex.IsMatch(a, "class=\"[^\"]*\\bon\\b"));
        Assert.Contains($"href=\"{active}\"", selected);
        foreach (var label in new[] { "Văn phòng", "Trò chuyện AI", "Đơn duyệt", "Dự án", "Đánh giá", "Hiệu suất", "Quy trình & cải tiến", "Hệ thống", "Cài đặt", "Tài khoản", "Map" })
            Assert.Contains(label, Text(side));
        Assert.DoesNotContain("Antigravity", Text(side));
        Assert.Equal(0, f.BoardReads);
        Assert.Equal(0, f.RunReads);
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending, "CHỜ DUYỆT")]
    [InlineData(ApprovalStatus.Approved, "ĐÃ DUYỆT")]
    [InlineData(ApprovalStatus.Rejected, "ĐÃ TỪ CHỐI")]
    public async Task Bang_sau_cot_trang_thai_va_nut_trong_dong(ApprovalStatus status, string label)
    {
        await using var f = new Fixture();
        f.Approvals = [Item(status)];
        var html = await f.RenderApprovals();
        var table = Assert.Single(Elements(html, "table"), t => t.Contains("A-001", StringComparison.Ordinal));
        Assert.Equal(new[] { "Mã", "Loại", "Dự án/task", "Nội dung tóm tắt", "Lúc tạo", "Trạng thái" },
            Elements(table, "th").Select(h => Text(h).Trim()));
        var row = Assert.Single(Elements(table, "tr"), t => t.Contains("A-001", StringComparison.Ordinal));
        Assert.Equal(6, Elements(row, "td").Length);
        foreach (var value in new[] { "AITeam", "H7-06", "Duyệt thay đổi", "13:45 24/10", label }) Assert.Contains(value, Text(row));
        var forms = Elements(row, "form");
        if (status == ApprovalStatus.Pending)
        {
            var form = Assert.Single(forms);
            Assert.Contains("handler=Decide", form);
            Assert.Contains("__RequestVerificationToken", form);
            Assert.Contains("name=\"note\"", form);
            Assert.Contains("value=\"approve\"", form);
            Assert.Contains("value=\"reject\"", form);
        }
        else
        {
            Assert.Empty(forms);
            Assert.Contains("Ghi chú cũ", Text(row));
        }
    }

    [Theory]
    [InlineData(ApprovalKind.Merge, "MERGE")]
    [InlineData(ApprovalKind.Decision, "QUYẾT ĐỊNH")]
    [InlineData(ApprovalKind.Improvement, "CẢI TIẾN QUY TRÌNH")]
    public async Task Du_lieu_cu_task_commit_null_van_hien(ApprovalKind kind, string label)
    {
        await using var f = new Fixture();
        f.Approvals = [Item(ApprovalStatus.Pending, kind) with { Task = null, Commit = null, Links = [] }];
        var html = await f.RenderApprovals();
        Assert.Contains(label, Text(html));
        Assert.Contains("A-001", Text(html));
    }

    [Fact]
    public async Task Trang_duyet_rong_giu_thong_bao()
    {
        await using var f = new Fixture();
        var html = await f.RenderApprovals();
        Assert.Contains("Không có việc nào chờ anh duyệt.", Text(html));
        Assert.Contains("Chưa có quyết định nào.", Text(html));
    }

    [Theory]
    [InlineData("approve", true)]
    [InlineData("reject", false)]
    public async Task Handler_giu_note_va_PRG(string decision, bool approve)
    {
        await using var f = new Fixture();
        f.Approvals = [Item(ApprovalStatus.Pending)];
        var model = new ApprovalsModel(f.ApprovalService);
        Assert.IsType<RedirectToPageResult>(model.OnPostDecide("A-001", decision, "Ghi chú"));
        Assert.Equal(("A-001", approve, "Ghi chú"), f.LastDecision);
        Assert.Equal(1, f.Writes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Approve")]
    [InlineData("delete")]
    public async Task Quyet_dinh_sai_khong_goi_dich_vu(string decision)
    {
        await using var f = new Fixture();
        var model = new ApprovalsModel(f.ApprovalService);
        Assert.IsType<RedirectToPageResult>(model.OnPostDecide("A-001", decision, null));
        Assert.Equal("Quyết định không hợp lệ.", model.ErrorMessage);
        Assert.Equal(0, f.Writes);
    }

    [Theory]
    [InlineData("repeat")]
    [InlineData("missing")]
    [InlineData("invalid")]
    public async Task Loi_dich_vu_quyet_dinh_duoc_giu_nguyen(string error)
    {
        await using var f = new Fixture();
        f.DecisionError = error switch { "repeat" => new InvalidOperationException("Đã quyết định."), "missing" => new KeyNotFoundException("Không tìm thấy đơn."), _ => new ArgumentException("Mã không hợp lệ.") };
        var model = new ApprovalsModel(f.ApprovalService);
        Assert.IsType<RedirectToPageResult>(model.OnPostDecide("A-001", "approve", null));
        Assert.Equal(f.DecisionError.Message, model.ErrorMessage);
        Assert.Equal(1, f.Writes);
    }

    [Fact]
    public async Task POST_thieu_token_bi_chan_truoc_khi_ghi()
    {
        await using var f = new Fixture();
        var result = await f.MissingToken();
        Assert.IsAssignableFrom<BadRequestResult>(result.Result);
        Assert.Equal(0, f.Writes);
        Assert.NotNull(typeof(ApprovalsModel).GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task Ten_du_an_encode_va_URL_chi_mot_doan()
    {
        await using var f = new Fixture();
        const string name = "Tên /?#<script>alert(1)</script>";
        f.Projects = [Project(name)];
        var html = await f.RenderProjects();
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains(name, Text(html));
        Assert.Contains("/p/" + Uri.EscapeDataString(name), html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Du_lieu_duyet_khong_chen_HTML_thuc_thi()
    {
        await using var f = new Fixture();
        const string attack = "<img src=x onerror=alert(1)>";
        f.Approvals = [Item(ApprovalStatus.Rejected) with { Title = attack, Project = attack, Task = attack, Note = attack, Detail = attack }];
        var html = await f.RenderApprovals();
        Assert.DoesNotContain(attack, html);
        Assert.Contains(attack, Text(html));
    }

    [Theory]
    [InlineData("/Agents", true)]
    [InlineData("/Inbox", true)]
    [InlineData("/Providers", false)]
    [InlineData("/AiAccounts", false)]
    [InlineData("/Accounts", false)]
    [InlineData("/Secrets", false)]
    [InlineData("/Requests", false)]
    [InlineData("/Research", false)]
    [InlineData("/Team", false)]
    [InlineData("/Overview", false)]
    public async Task H703_chi_redirect_hai_dia_chi_cu(string path, bool redirect)
    {
        var calls = 0;
        var middleware = new OfficeChatRedirectMiddleware(context => { calls++; context.Response.StatusCode = 204; return Task.CompletedTask; });
        var http = new DefaultHttpContext();
        http.Request.Method = "GET";
        http.Request.Path = path;
        await middleware.InvokeAsync(http);
        Assert.Equal(redirect ? 302 : 204, http.Response.StatusCode);
        Assert.Equal(redirect ? 0 : 1, calls);
        Assert.Equal(redirect ? "/" : "", http.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Chuoi_dai_co_markup_va_quy_tac_co_cot()
    {
        await using var f = new Fixture();
        var longText = new string('x', 600);
        f.Projects = [Project(longText)];
        Assert.Contains(longText, Text(await f.RenderProjects()));
        f.Approvals = [Item(ApprovalStatus.Pending) with { Title = longText, Project = longText }];
        var html = await f.RenderApprovals();
        Assert.Contains(longText, Text(html));
        Assert.Contains("approvals-table", html);
        var css = File.ReadAllText(Path.Combine(Root(), "hub", "wwwroot", "css", "hub.css"));
        Assert.Matches(@"(?s)\.approvals-table\s*\{[^}]*table-layout\s*:\s*fixed", css);
        Assert.Matches(@"(?s)\.approvals-table\s+(?:th\s*,\s*\.approvals-table\s+)?td\s*\{[^}]*white-space\s*:\s*normal", css);
        Assert.Matches(@"(?s)\.approvals-table\s+(?:th\s*,\s*\.approvals-table\s+)?td\s*\{[^}]*overflow-wrap\s*:\s*anywhere", css);
    }
}
