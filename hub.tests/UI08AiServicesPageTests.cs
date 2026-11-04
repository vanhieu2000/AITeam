using System.Text;
using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Core.Providers;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// R-014 UI-08 (Claude): Tài khoản AI — thêm / sửa / tắt dịch vụ, kiểm tra kết nối, .ini, cài đặt trợ lý chat.
public sealed class UI08AiServicesPageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-ui08-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteAiServiceRegistry _registry;
    private readonly FakeFactory _factory = new();
    private readonly FakeAssistant _assistant = new();

    public UI08AiServicesPageTests()
    {
        Directory.CreateDirectory(_root);
        _registry = new SqliteAiServiceRegistry(":memory:", _root);
    }

    public void Dispose()
    {
        _registry.Dispose();
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private sealed class FakeProvider(ProviderHealth health) : IAgentProvider
    {
        public string Id => health.ProviderId;
        public ProviderKind Kind => ProviderKind.Generic;
        public bool HasReadOnlySandbox => true;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(health);
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new InvalidOperationException("Không được chạy prompt khi kiểm tra.");
    }

    private sealed class FakeFactory : IAiProviderFactory
    {
        public ProviderHealth Health { get; set; } = new("x", true, true, "");
        public IAgentProvider Create(AiService service) => new FakeProvider(Health with { ProviderId = service.Id });
        public IReadOnlyList<AiService> ForRole(AiRole role) => [];
    }

    private sealed class FakeAssistant : IChatAssistant
    {
        public AssistantSettings Saved { get; private set; } = new("codex", ChatMode.Ask, ["codex"], true);
        public AssistantSettings Settings() => Saved;
        public void SaveSettings(AssistantSettings settings)
        {
            if (settings.ResearchAis.Count is < 1 or > 4) throw new ArgumentException("Nghiên cứu cần 1–4 AI.");
            Saved = settings;
        }
        public IReadOnlyList<AssistantOption> Options() => [new("codex", "Codex", true, null), new("off", "Tắt", false, "đang tắt")];
        public Task<AssistantReply> SendAsync(string? conversationId, ChatMode mode, string? ai, string text, string? project, CancellationToken ct) => throw new NotSupportedException();
    }

    private AiServicesModel Page() => new(_registry, _factory, _assistant)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
    };

    private static AiServiceInput Http(string id = "my-llm") => new()
    {
        Id = id, Name = "LLM nội bộ", Adapter = "openai-compatible-http", BaseUrl = "https://llm.example.test/v1",
        Models = "m1, m2", Roles = ["chat", "research"], MaxJobs = 2, SecretKeyName = "MY_LLM_KEY", ManageUrl = "https://example.test",
    };

    [Fact]
    public void Add_valid_service_then_edit_keeps_created_and_enabled()
    {
        var p = Page();
        Assert.IsType<RedirectToPageResult>(p.OnPostSave(Http(), isEdit: false));
        var s = _registry.Get("my-llm")!;
        Assert.Equal(["m1", "m2"], s.Models);
        Assert.Equal([AiRole.Research, AiRole.Chat], s.Roles.OrderBy(r => r));
        Assert.Equal("MY_LLM_KEY", s.SecretKeyName);
        Assert.True(s.Enabled);

        var edit = Http();
        edit.Name = "LLM đổi tên";
        Assert.IsType<RedirectToPageResult>(Page().OnPostSave(edit, isEdit: true));
        Assert.Equal("LLM đổi tên", _registry.Get("my-llm")!.Name);
        Assert.Equal(s.CreatedAt, _registry.Get("my-llm")!.CreatedAt);

        var get = Page();
        get.Edit = "my-llm";
        get.OnGet();
        Assert.True(get.IsEdit);
        Assert.Equal(["chat", "research"], get.Form.Roles.Order());
    }

    [Fact]
    public void Duplicate_or_invalid_input_returns_page_with_errors()
    {
        Page().OnPostSave(Http(), false);

        var dup = Page();
        Assert.IsType<PageResult>(dup.OnPostSave(Http(), isEdit: false));
        Assert.Contains("đã có", Assert.Single(dup.FormErrors));

        var bad = Http("bad-url");
        bad.BaseUrl = "http://evil.example/v1";
        bad.Roles = [];
        var p = Page();
        Assert.IsType<PageResult>(p.OnPostSave(bad, false));
        Assert.Contains(p.FormErrors, e => e.Contains("https://"));
        Assert.Contains(p.FormErrors, e => e.Contains("vai trò"));
        Assert.Null(_registry.Get("bad-url"));
        Assert.Equal("http://evil.example/v1", p.Form.BaseUrl);   // giữ bản đang nhập

        var shell = Http("shell-cmd");
        shell.Adapter = "cli-text"; shell.Command = "calc.exe & del x";
        var q = Page();
        Assert.IsType<PageResult>(q.OnPostSave(shell, false));
        Assert.Contains(q.FormErrors, e => e.Contains("shell"));

        var adapter = Http("x-ad");
        adapter.Adapter = "ssh";
        Assert.IsType<PageResult>(Page().OnPostSave(adapter, false));
        Assert.IsType<PageResult>(Page().OnPostSave(Http("khong-co"), isEdit: true));
    }

    [Fact]
    public void Toggle_disables_and_reenables()
    {
        Page().OnPostSave(Http(), false);
        var p = Page();
        p.OnPostToggle("my-llm", false);
        Assert.False(_registry.Get("my-llm")!.Enabled);
        p.OnPostToggle("my-llm", true);
        Assert.True(_registry.Get("my-llm")!.Enabled);
        p.OnPostToggle("khong-co", false);
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
    }

    [Fact]
    public async Task Check_reports_installed_and_login_without_running_prompt()
    {
        Page().OnPostSave(Http(), false);
        var ok = Page();
        await ok.OnPostCheckAsync("my-llm");
        Assert.Contains("có cài", ok.Message);
        Assert.Contains("đã đăng nhập", ok.Message);

        _factory.Health = new("x", false, null, "không tìm thấy lệnh");
        var bad = Page();
        await bad.OnPostCheckAsync("my-llm");
        Assert.Contains("chưa cài", bad.ErrorMessage);

        var missing = Page();
        await missing.OnPostCheckAsync("khong-co");
        Assert.Equal("Không tìm thấy dịch vụ.", missing.ErrorMessage);
    }

    private static IFormFile File(string text) =>
        new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(text)), 0, Encoding.UTF8.GetByteCount(text), "file", "a.ini");

    [Fact]
    public async Task Export_then_import_roundtrip_and_size_limit()
    {
        Page().OnPostSave(Http(), false);
        var export = Assert.IsType<FileContentResult>(Page().OnGetExport());
        var ini = Encoding.UTF8.GetString(export.FileContents).TrimStart('﻿');
        Assert.Contains("my-llm", ini);
        Assert.DoesNotContain("sk-", ini);

        using var other = new SqliteAiServiceRegistry(":memory:", _root);
        var p = new AiServicesModel(other, _factory, _assistant) { TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()) };
        await p.OnPostImportAsync(File(ini));
        Assert.Null(p.ErrorMessage);
        Assert.NotNull(other.Get("my-llm"));

        var big = Page();
        await big.OnPostImportAsync(File(new string('x', AiServicesModel.MaxIniBytes + 1)));
        Assert.Contains("256 KB", big.ErrorMessage);
        var none = Page();
        await none.OnPostImportAsync(null);
        Assert.False(string.IsNullOrEmpty(none.ErrorMessage));
    }

    [Fact]
    public void Assistant_settings_save_and_reject_bad_mode()
    {
        var p = Page();
        Assert.IsType<RedirectToPageResult>(p.OnPostAssistant("codex", "Research", ["codex"], false));
        Assert.Equal(ChatMode.Research, _assistant.Saved.DefaultMode);
        Assert.False(_assistant.Saved.KeepHistory);

        p.OnPostAssistant("codex", "99", ["codex"], true);
        Assert.False(string.IsNullOrEmpty(p.ErrorMessage));
        p.ErrorMessage = null;
        p.OnPostAssistant("codex", "Ask", [], true);
        Assert.Contains("1–4", p.ErrorMessage);
    }

    [Fact]
    public async Task Page_renders_cards_form_and_never_a_secret_value_field()
    {
        Page().OnPostSave(Http(), false);
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton<IAiServiceRegistry>(_registry);
            s.AddSingleton<IAiProviderFactory>(_factory);
            s.AddSingleton<IChatAssistant>(_assistant);
        });
        var html = await host.Client.GetStringAsync("/AiServices");
        Assert.Contains("my-llm", html);
        Assert.Contains("handler=Check", html);
        Assert.Contains("handler=Toggle", html);
        Assert.Contains("handler=Assistant", html);
        Assert.Contains("id=\"tro-ly\"", html);
        Assert.DoesNotContain("type=\"password\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
    }
}
