using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

/// <summary>Nghiệm thu H7-04: thực thi handler/Razor thật, không gọi agent hay đọc dữ liệu máy.</summary>
public class AcceptanceH704OfficeTests
{
    private const string Success = "Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.";
    private static T Fake<T>(Func<MethodInfo, object?[]?, object?> call) where T : class
    {
        var value = DispatchProxy.Create<T, ServiceProxy>();
        ((ServiceProxy)(object)value).Call = call;
        return value;
    }
    public class ServiceProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Call = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!, args);
    }
    private static object Empty(MethodInfo method) => Array.CreateInstance(method.ReturnType.GetGenericArguments().Single(), 0);
    private static IOfficeService Office(bool full) => Fake<IOfficeService>((_, _) => new OfficeView(
        full ? new[] { "claude", "C", "A", "B", "gate" }.Select((key, i) => new Desk(key,
            new[] { "Claude", "Codex", "Minh", "Lan", "Cổng KT" }[i] + " <img src=x onerror=alert(1)>",
            "Vai trò " + new string('x', 160), DeskState.Working, "Đang làm <script>alert(2)</script>",
            new DeskTask("demo", "T-" + i, "Tiêu đề dài " + new string('x', 180), 1), [])).ToArray() : [],
        3, 1, 0, 0, new DateTime(2026, 10, 26, 9, 30, 0)));
    private static OfficeModel Page(IOfficeChatService? chat = null) => new(Office(false), null,
        Fake<IRequestService>((m, _) => Empty(m)), chat,
        Fake<ITeamConfigService>((m, _) => Empty(m)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void New_request_loads_empty_chat_without_selecting_another_thread(string? id)
    {
        var page = Page();
        page.RequestId = id;
        page.LoadChat();
        Assert.Empty(page.Threads);
        Assert.Empty(page.ChatProjects);
        Assert.Null(page.SelectedThread);
    }

    [Theory]
    [InlineData("../R-009")]
    [InlineData("R-009\n")]
    [InlineData("R-999")]
    [InlineData("<script>alert(1)</script>")]
    public void Invalid_or_missing_selection_is_reported(string id)
    {
        var page = Page();
        page.RequestId = id;
        page.LoadChat();
        Assert.Equal("Phiếu không hợp lệ.", page.ChatError);
        Assert.Null(page.SelectedThread);
    }

    [Fact]
    public async Task Partial_keeps_viewdata_and_validation_state()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var page = Page();
        page.PageContext = new PageContext
        {
            ViewData = new ViewDataDictionary(host.Services.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary())
        };
        page.ViewData["context"] = "giữ lại";
        page.ModelState.AddModelError("text", "lỗi nội dung");
        var result = Assert.IsType<PartialViewResult>(page.OnGetChat());
        Assert.Equal("_OfficeChat", result.ViewName);
        Assert.Same(page, result.Model);
        Assert.Equal("giữ lại", result.ViewData["context"]);
        Assert.False(result.ViewData.ModelState.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("R-009")]
    public void Send_calls_H703_once_and_reports_next_session(string? id)
    {
        var calls = new List<object?[]>();
        var page = Page(Fake<IOfficeChatService>((_, args) => { calls.Add(args!); return null; }));
        var result = Assert.IsType<PartialViewResult>(page.OnPostChat("demo", "  bổ sung  ", id));
        var call = Assert.Single(calls);
        Assert.Equal(new object?[] { "demo", "  bổ sung  ", id }, call);
        Assert.Equal(Success, page.ChatNotice);
        Assert.Null(page.ChatError);
        Assert.Equal("_OfficeChat", result.ViewName);
        Assert.Equal(id, page.RequestId);
    }

    [Theory]
    [InlineData("Nội dung yêu cầu không được để trống.")]
    [InlineData("Nội dung quá dài (tối đa 4000 ký tự).")]
    [InlineData("Dự án không hợp lệ.")]
    [InlineData("Phiếu không hợp lệ.")]
    public void Validation_error_is_preserved_without_success(string message)
    {
        var count = 0;
        var page = Page(Fake<IOfficeChatService>((_, _) => { count++; throw new ArgumentException(message); }));
        Assert.IsType<PartialViewResult>(page.OnPostChat("demo", "nội dung", null));
        Assert.Equal(1, count);
        Assert.Equal(message, page.ChatError);
        Assert.Null(page.ChatNotice);
    }

    [Fact]
    public void Unexpected_send_error_is_reported_without_retry()
    {
        var calls = 0;
        var page = Page(Fake<IOfficeChatService>((_, _) => { calls++; throw new IOException("inbox hỏng"); }));
        page.OnPostChat("demo", "nội dung", null);
        Assert.Equal(1, calls);
        Assert.Equal("Không gửi được yêu cầu: inbox hỏng", page.ChatError);
        Assert.Null(page.ChatNotice);
    }

    [Fact]
    public void Missing_chat_service_is_readonly()
    {
        var page = new OfficeModel(Office(false));
        page.OnPostChat("demo", "nội dung", null);
        Assert.Equal("Chat đang ở chế độ chỉ xem.", page.ChatError);
        Assert.Null(page.ChatNotice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_renders_real_page_and_partial_encodes_external_data(bool full)
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, services =>
        {
            services.AddSingleton(Office(full));
            services.AddSingleton(Fake<IRequestService>((m, _) => Empty(m)));
        });
        // R-014 (Claude, chủ dự án duyệt 03/11): "/" là bảng điều khiển (bàn làm việc); khung giao việc chuyển vào nút chat nổi ("/?embed=1").
        var html = await host.Client.GetStringAsync("/");
        Assert.Contains("office-agents", html);
        Assert.DoesNotContain("id=\"office-text\"", html);
        Assert.DoesNotContain("<img src=x", html);
        var embedded = await host.Client.GetStringAsync("/?embed=1");
        Assert.Contains("office-workspace", embedded);
        Assert.Contains("office-chat", embedded);
        Assert.DoesNotContain("class=\"side\"", embedded);
        Assert.DoesNotContain("<img src=x", embedded);
        Assert.DoesNotContain("<script>alert(2)", embedded);
        Assert.DoesNotContain("<script>alert(2)", html);
        if (full)
        {
            foreach (var key in new[] { "claude", "C", "A", "B", "gate" })
                Assert.Contains("data-desk-key=\"" + key + "\"", html);
            Assert.Contains("&lt;img", html);
        }
        else Assert.Contains("Giao việc cho Claude", WebUtility.HtmlDecode(embedded));
        var page = new OfficeModel(Office(full));
        page.OnGet();
        var desks = await new HubPageRenderer(host.Services).RenderAsync("_OfficeDesks", page);
        Assert.Contains("office-desks", desks);
        Assert.DoesNotContain("<img src=x", desks);
        var chatHtml = await new HubPageRenderer(host.Services).RenderAsync("_OfficeChat", Page());
        Assert.Contains("id=\"office-chat-thread\"", chatHtml);
        Assert.DoesNotContain("<textarea", chatHtml);
        Assert.DoesNotContain("<select", chatHtml);
        Assert.DoesNotContain("<form", chatHtml);
    }

    [Fact]
    public async Task Post_without_csrf_is_rejected_before_chat_service()
    {
        using var env = new TestEnvironment();
        var calls = 0;
        await using var host = await HubTestHost.StartAsync(env, services =>
            services.AddSingleton(Fake<IOfficeChatService>((_, _) => { calls++; return null; })));
        using var response = await host.Client.PostAsync("/?handler=Chat", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["project"] = "demo", ["text"] = "nội dung" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Composer_has_token_and_lives_outside_every_polling_target()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, services => services.AddSingleton(Office(false)));
        var html = await host.Client.GetStringAsync("/");
        var embedded = await host.Client.GetStringAsync("/?embed=1");   // R-014: khung giao việc nằm trong nút chat nổi
        var form = Regex.Matches(embedded, @"<form\b[^>]*>[\s\S]*?</form>", RegexOptions.IgnoreCase)
            .Cast<Match>().Select(m => m.Value).Single(f => f.Contains("handler=Chat", StringComparison.Ordinal));
        Assert.Contains("__RequestVerificationToken", form);
        Assert.Contains("name=\"text\"", form);
        Assert.Contains("name=\"requestId\"", form);
        Assert.Contains("name=\"project\"", form);
        Assert.Contains("maxlength=\"4000\"", form);
        var partial = await host.Client.GetStringAsync("/?handler=Chat");
        Assert.Contains("office-chat-thread", partial);
        Assert.DoesNotContain("<textarea", partial);
        Assert.DoesNotContain("<select", partial);
        var desks = await host.Client.GetStringAsync("/?handler=Desks");
        Assert.DoesNotContain("name=\"text\"", desks);
        foreach (var commandForm in Regex.Matches(html, @"<form\b[^>]*>[\s\S]*?</form>").Cast<Match>()
                     .Where(m => m.Value.Contains("handler=Command")))
        {
            Assert.Contains("hx-confirm=", commandForm.Value.Split('>')[0]);
            Assert.Contains("__RequestVerificationToken", commandForm.Value);
        }
    }

    [Fact]
    public void Css_defines_two_columns_and_stacks_chat_at_1000px()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "hub", "wwwroot", "css", "hub.css")))
            root = root.Parent;
        Assert.NotNull(root);
        var css = File.ReadAllText(Path.Combine(root!.FullName, "hub", "wwwroot", "css", "hub.css"));
        Assert.Matches(@"(?s)\.office-workspace\s*\{[^}]*display\s*:\s*grid", css);
        Assert.Matches(@"(?s)\.office-workspace\s*\{[^}]*grid-template-columns\s*:\s*minmax\(0,\s*1fr\)\s+minmax\(0,\s*1fr\)", css);
        Assert.Matches(@"(?s)@media\s*\(max-width\s*:\s*1000px\)\s*\{\s*\.office-workspace\s*\{[^}]*grid-template-columns\s*:\s*1fr", css);
    }
    [Fact]
    public async Task Working_agent_buttons_render_confirmed_post_with_exact_task_target()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var commands = Fake<ICommandService>((m, _) => Empty(m));
        var page = new OfficeModel(Office(true), commands);
        page.OnGet();
        var html = await new HubPageRenderer(host.Services).RenderAsync("_OfficeDesks", page);
        var forms = Regex.Matches(html, @"<form\b[^>]*>[\s\S]*?</form>").Cast<Match>().Select(m => m.Value).ToArray();
        var stops = forms.Where(f => f.Contains("value=\"Stop\"", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, stops.Length);
        foreach (var i in new[] { 1, 2, 3 })
        {
            var form = Assert.Single(stops, f => f.Contains("value=\"demo/T-" + i + "\"", StringComparison.Ordinal));
            Assert.Contains("hx-confirm=", form.Split('>')[0]);
            Assert.Contains("__RequestVerificationToken", form);
            Assert.Contains("handler=Command", form);
        }
        var gate = Assert.Single(forms, f => f.Contains("value=\"Verify\"", StringComparison.Ordinal)
            && f.Contains("value=\"demo/T-4\"", StringComparison.Ordinal));
        Assert.Contains("hx-confirm=", gate.Split('>')[0]);
    }

    [Fact]
    public async Task Chat_read_failure_is_reported_and_encoded_in_real_partial()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env);
        var page = new OfficeModel(Office(false), null,
            Fake<IRequestService>((_, _) => throw new IOException("<img src=x onerror=alert(1)>")), null,
            Fake<ITeamConfigService>((m, _) => Empty(m)));
        page.LoadChat();
        Assert.Equal("Không đọc được hội thoại: <img src=x onerror=alert(1)>", page.ChatError);
        var html = await new HubPageRenderer(host.Services).RenderAsync("_OfficeChat", page);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }
    [Theory]
    [InlineData("Assign")]
    [InlineData("AssignCodex")]
    [InlineData("Stop")]
    [InlineData("Resume")]
    [InlineData("Verify")]
    public void Existing_command_handler_preserves_command_target_and_feedback(string command)
    {
        CommandRequest? captured = null;
        var commands = Fake<ICommandService>((method, args) =>
        {
            if (method.Name == "Start") { captured = (CommandRequest)args![0]!; throw new InvalidOperationException("từ chối thử nghiệm"); }
            return Empty(method);
        });
        var page = new OfficeModel(Office(false), commands);
        page.OnPostCommand(command, "demo/T-123", "góp ý");
        Assert.NotNull(captured);
        Assert.Equal(command, captured.Command.ToString());
        Assert.Equal("demo", captured.Project);
        Assert.Equal("T-123", captured.Task);
        Assert.Equal("từ chối thử nghiệm", page.CommandError);
    }
}