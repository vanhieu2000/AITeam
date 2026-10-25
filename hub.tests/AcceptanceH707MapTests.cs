using System.Net;
using System.Text.RegularExpressions;
using AITeam.Hub.DevData;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// Test nghiệm thu H7-07: agent KHÔNG sửa. Chỉ dùng thư mục tạm và HTTP nội bộ, không chạy CLI.
public sealed class AcceptanceH707MapTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private static readonly string[] Codes = ["receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish"];
    private static readonly string[] Actors = ["claude", "codex", "minh", "lan", "gate", "owner"];
    private static readonly string[] Defaults = ["claude", "claude", "codex", "codex", "claude", "codex", "minh", "gate", "codex", "claude", "claude"];
    private static readonly string[] Labels = ["Tiếp nhận yêu cầu", "Lên ý tưởng", "Research", "Lên kế hoạch", "Duyệt kế hoạch", "Chia việc / chuẩn bị task", "Code", "Kiểm thử tự động (cổng KT)", "Test / QC", "Duyệt cuối (nghiệm thu)", "Publish git (merge + push)"];
    public void Dispose() => _env.Dispose();

    private sealed class Observed(IWorkflowRolesService inner) : IWorkflowRolesService
    {
        public Exception? ReadError { get; set; }
        public Exception? WriteError { get; set; }
        public int Writes { get; private set; }
        public WorkflowRolesSnapshot GetRoles() => ReadError is null ? inner.GetRoles() : throw ReadError;
        public WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision)
        {
            Writes++;
            if (WriteError is not null) throw WriteError;
            return inner.SaveRoles(roles, expectedRevision);
        }
    }

    private async Task WithHub(Func<HttpClient, Observed, Task> act)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { ApplicationName = typeof(MapModel).Assembly.GetName().Name });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        FakeServices.Register(builder.Services);
        builder.Services.AddSingleton(_env.Paths);
        var service = new Observed(new WorkflowRolesService(_env.Paths));
        builder.Services.AddSingleton<IWorkflowRolesService>(service);
        builder.Services.AddRazorPages().AddApplicationPart(typeof(MapModel).Assembly);
        builder.Services.AddAntiforgery();
        await using var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();
        await app.StartAsync();
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.First()) };
            await act(client, service);
        }
        finally { await app.StopAsync(); }
    }

    private static string Input(string html, string name)
    {
        var tag = Regex.Matches(html, "<input\\b[^>]*>").Cast<Match>()
            .Single(x => Regex.IsMatch(x.Value, "name=\"" + Regex.Escape(name) + "\""));
        var value = Regex.Match(tag.Value, "value=\"([^\"]*)\"");
        Assert.True(value.Success, "Thiếu giá trị input " + name);
        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }

    private static string Select(string html, int index) => Regex.Matches(html,
        "<select\\b[^>]*>.*?</select>", RegexOptions.Singleline).Cast<Match>()
        .Single(x => x.Value.Contains("name=\"Roles[" + index + "].Actor\"", StringComparison.Ordinal)).Value;

    private static void Selected(string html, int index, string actor)
    {
        var options = Regex.Matches(Select(html, index), "<option\\b[^>]*>").Cast<Match>();
        var chosen = Assert.Single(options, x => Regex.IsMatch(x.Value, "\\bselected(?:=|\\s|>)"));
        Assert.Contains("value=\"" + actor + "\"", chosen.Value);
    }

    private static Dictionary<string, string> Form(string html)
    {
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Input(html, "__RequestVerificationToken"),
            ["Revision"] = Input(html, "Revision")
        };
        for (var i = 0; i < Codes.Length; i++)
        {
            form[$"Roles[{i}].Code"] = Codes[i];
            form[$"Roles[{i}].Actor"] = "lan";
            form[$"Roles[{i}].Note"] = "Ghi chú " + i;
        }
        return form;
    }

    private static async Task<string> Post(HttpClient client, Dictionary<string, string> form)
    {
        var response = await client.PostAsync("/Map", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"workflow\":{}}")]
    [InlineData("{\"legacy\":42,\"workflow\":{\"other\":true}}")]
    public async Task Missing_and_legacy_config_render_v4_defaults_without_writing(string? json)
    {
        if (json is not null) File.WriteAllText(_env.Paths.TeamJson, json);
        await WithHub(async (client, service) =>
        {
            var response = await client.GetAsync("/Map");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            for (var i = 0; i < Codes.Length; i++)
            {
                Assert.Equal(Codes[i], Input(html, $"Roles[{i}].Code"));
                Selected(html, i, Defaults[i]);
            }
            Assert.Equal(0, service.Writes);
            Assert.Equal(json is not null, File.Exists(_env.Paths.TeamJson));
            if (json is not null) Assert.Equal(json, File.ReadAllText(_env.Paths.TeamJson));
        });
    }

    [Fact]
    public Task Real_razor_has_eleven_rows_six_choices_and_authority_explanation() => WithHub(async (client, _) =>
    {
        var html = await client.GetStringAsync("/Map");
        Assert.Equal(11, Regex.Matches(html, "<select\\b").Count);
        foreach (var label in Labels) Assert.Contains(label, WebUtility.HtmlDecode(html));
        for (var i = 0; i < 11; i++)
        {
            var select = Select(html, i);
            Assert.Equal(6, Regex.Matches(select, "<option\\b").Count);
            foreach (var actor in Actors) Assert.Contains("value=\"" + actor + "\"", select);
            Assert.Contains($"name=\"Roles[{i}].Note\"", html);
        }
        foreach (var label in new[] { "Claude", "Codex", "Minh", "Lan", "Cổng KT", "Chủ dự án", "Ghi chú quyền hạn" })
            Assert.Contains(label, WebUtility.HtmlDecode(html));
        Assert.Contains("Gán vai không tự cấp quyền thực thi hoặc duyệt.", WebUtility.HtmlDecode(html));
        Assert.Matches("<form[^>]*method=\"post\"", html);
        Assert.Contains("map-page", html);
        Assert.NotEmpty(Input(html, "__RequestVerificationToken"));
    });

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public Task Save_reload_preserves_notes_and_unknown_config(int length)
    {
        File.WriteAllText(_env.Paths.TeamJson, "{\"legacy\":42,\"workflow\":{\"other\":true}}");
        return WithHub(async (client, service) =>
        {
            var form = Form(await client.GetStringAsync("/Map"));
            var note = length == 0 ? "" : "  " + new string('ệ', length - 4) + "  ";
            form["Roles[0].Note"] = note;
            var savedHtml = await Post(client, form);
            Assert.Contains("Đã lưu phân vai.", WebUtility.HtmlDecode(savedHtml));
            Assert.NotEqual(form["Revision"], Input(savedHtml, "Revision"));
            Assert.Equal(1, service.Writes);
            var saved = new WorkflowRolesService(_env.Paths).GetRoles();
            Assert.Equal(note, saved.Roles[0].Note);
            Assert.All(saved.Roles, x => Assert.Equal("lan", x.Actor));
            var reload = await client.GetStringAsync("/Map");
            for (var i = 0; i < 11; i++) Selected(reload, i, "lan");
            Assert.Contains("\"legacy\": 42", File.ReadAllText(_env.Paths.TeamJson));
            Assert.Contains("\"other\": true", File.ReadAllText(_env.Paths.TeamJson));
        });
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("case")]
    [InlineData("code")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("long")]
    [InlineData("revision")]
    public Task Tampered_form_never_calls_save(string kind) => WithHub(async (client, service) =>
    {
        var form = Form(await client.GetStringAsync("/Map"));
        switch (kind)
        {
            case "actor": form["Roles[0].Actor"] = "../owner"; break;
            case "case": form["Roles[0].Actor"] = "Claude"; break;
            case "code": form["Roles[0].Code"] = "unknown"; break;
            case "duplicate": form["Roles[1].Code"] = "receive"; break;
            case "missing": foreach (var key in form.Keys.Where(x => x.StartsWith("Roles[10]", StringComparison.Ordinal)).ToArray()) form.Remove(key); break;
            case "extra": form["Roles[11].Code"] = "publish"; form["Roles[11].Actor"] = "lan"; form["Roles[11].Note"] = ""; break;
            case "long": form["Roles[0].Note"] = new string('a', 501); break;
            case "revision": form["Revision"] = ""; break;
        }
        var html = await Post(client, form);
        Assert.Contains("Phân vai không hợp lệ.", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("Đã lưu phân vai.", WebUtility.HtmlDecode(html));
        Assert.Equal(0, service.Writes);
        Assert.False(File.Exists(_env.Paths.TeamJson));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Missing_or_forged_csrf_is_rejected(bool forged) => WithHub(async (client, service) =>
    {
        var form = Form(await client.GetStringAsync("/Map"));
        if (forged) form["__RequestVerificationToken"] = "invalid";
        else form.Remove("__RequestVerificationToken");
        var response = await client.PostAsync("/Map", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, service.Writes);
        Assert.False(File.Exists(_env.Paths.TeamJson));
    });

    [Theory]
    [InlineData("io", "Không đọc được cấu hình phân vai.")]
    [InlineData("data", "Phân vai không hợp lệ.")]
    public Task Read_error_is_safe_and_disables_save(string kind, string message) => WithHub(async (client, service) =>
    {
        var secret = string.Concat("sk-", new string('z', 32));
        service.ReadError = kind == "io" ? new IOException(secret) : new InvalidDataException(secret);
        var html = await client.GetStringAsync("/Map");
        Assert.Contains(message, WebUtility.HtmlDecode(html));
        Assert.DoesNotContain(secret, html);
        Assert.DoesNotContain("name=\"Revision\"", html);
        Assert.Equal(0, service.Writes);
    });

    [Theory]
    [InlineData("io", "Không lưu được cấu hình phân vai.")]
    [InlineData("data", "Phân vai không hợp lệ.")]
    [InlineData("conflict", "Cấu hình đã thay đổi. Vui lòng tải lại.")]
    public Task Write_error_keeps_form_and_revision_without_leaking(string kind, string message) => WithHub(async (client, service) =>
    {
        var form = Form(await client.GetStringAsync("/Map"));
        const string payload = "<script>alert(707)</script>\"&";
        form["Roles[0].Note"] = payload;
        var secret = string.Concat("sk-", new string('w', 32));
        service.WriteError = kind switch { "io" => new IOException(secret), "data" => new InvalidDataException(secret), _ => new InvalidOperationException(secret) };
        var html = await Post(client, form);
        Assert.Contains(message, WebUtility.HtmlDecode(html));
        Assert.DoesNotContain(secret, html);
        Assert.DoesNotContain(payload, html);
        Assert.Contains(payload, WebUtility.HtmlDecode(html));
        Assert.Equal(form["Revision"], Input(html, "Revision"));
        for (var i = 0; i < 11; i++) Selected(html, i, "lan");
        Assert.DoesNotContain("Đã lưu phân vai.", WebUtility.HtmlDecode(html));
        Assert.Equal(1, service.Writes);
        Assert.False(File.Exists(_env.Paths.TeamJson));
    });

    [Fact]
    public Task Saved_xss_is_encoded_on_get_and_success() => WithHub(async (client, _) =>
    {
        var form = Form(await client.GetStringAsync("/Map"));
        const string payload = "</textarea><script>alert(707)</script>";
        form["Roles[3].Note"] = payload;
        var success = await Post(client, form);
        var reload = await client.GetStringAsync("/Map?drawer=" + Uri.EscapeDataString(payload));
        foreach (var html in new[] { success, reload })
        {
            Assert.DoesNotContain(payload, html);
            Assert.Contains(payload, WebUtility.HtmlDecode(html));
        }
        Assert.Equal(payload, new WorkflowRolesService(_env.Paths).GetRoles().Roles[3].Note);
    });

    [Theory]
    [InlineData("{broken", "Không đọc được cấu hình phân vai.")]
    [InlineData("{\"workflow\":{\"roles\":null}}", "Phân vai không hợp lệ.")]
    [InlineData("{\"workflow\":{\"roles\":[]}}", "Phân vai không hợp lệ.")]
    public Task Corrupt_existing_config_is_not_replaced_by_defaults(string json, string message)
    {
        File.WriteAllText(_env.Paths.TeamJson, json);
        return WithHub(async (client, service) =>
        {
            var html = await client.GetStringAsync("/Map");
            Assert.Contains(message, WebUtility.HtmlDecode(html));
            Assert.DoesNotContain("name=\"Revision\"", html);
            Assert.Equal(json, File.ReadAllText(_env.Paths.TeamJson));
            Assert.Equal(0, service.Writes);
        });
    }

    [Fact]
    public Task Stale_revision_is_not_silently_refreshed() => WithHub(async (client, service) =>
    {
        var form = Form(await client.GetStringAsync("/Map"));
        const string changed = "{\"external\":true}";
        File.WriteAllText(_env.Paths.TeamJson, changed);
        var html = await Post(client, form);
        Assert.Contains("Cấu hình đã thay đổi. Vui lòng tải lại.", WebUtility.HtmlDecode(html));
        Assert.Equal(form["Revision"], Input(html, "Revision"));
        Assert.Equal(changed, File.ReadAllText(_env.Paths.TeamJson));
        Assert.Equal(1, service.Writes);
    });
}
