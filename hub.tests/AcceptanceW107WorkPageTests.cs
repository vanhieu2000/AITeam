using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Nghiệm thu W1-07: HTTP thật, dữ liệu phiếu thật đã chép nguyên byte; không chạy agent/git.</summary>
public sealed class AcceptanceW107WorkPageTests
{
    private static readonly string[] Labels =
        ["Tiếp nhận", "Kế hoạch", "Duyệt kế hoạch", "Chuẩn bị", "Code", "Cổng KT", "QC", "Nghiệm thu", "Merge"];

    private static string Fixture([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../tests/fixtures/W1-07/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md"));

    private static WorkProjectProgress Snapshot(string project = "AITeam", string? title = null, bool empty = false, bool failed = false)
    {
        var info = new ProjectInfo(project, "test/repo", "unused", "unused", "", "", "",
            new Dictionary<string, string>(), true);
        var bytes = File.ReadAllBytes(Fixture());
        var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        if (failed) text = text[..text.IndexOf("- 17:16", StringComparison.Ordinal)];
        var progress = WorkProgressParser.Calculate("S1-04", WorkProgressParser.Parse(text), true, true);
        progress = progress with { Steps = progress.Steps.Select(s => s with { ActorText = "Người kiểm thử" }).ToArray() };
        var task = new WorkTaskProgress("S1-04", "Task từ phiếu thật", "B", progress, []);
        var other = new WorkTaskProgress("S1-07", "Task thứ hai", "A",
            WorkProgressParser.Calculate("S1-07", WorkProgressParser.Parse(text), true, true), []);
        var request = new WorkRequestProgress("R-010", title ?? "Yêu cầu từ phiếu thật", progress, [task, other],
            Enum.GetValues<WorkStep>().ToDictionary(s => s, s => new[] { task, other }.Count(t => t.Progress.CurrentStep == s)));
        var pending = WorkProgressParser.Calculate("R-009", [], true, true);
        return new(info, empty ? [] : [request, new("R-009", "Yêu cầu chưa có task", pending, [],
            Enum.GetValues<WorkStep>().ToDictionary(s => s, _ => 0))], []);
    }

    private sealed class WorkSource(WorkProjectProgress snapshot, bool fail = false) : IWorkProgressService
    {
        public List<string> Calls { get; } = [];
        public WorkProjectProgress GetProject(string project)
        {
            Calls.Add(project);
            if (fail) throw new IOException("D:/private/secret-source " + "<script>leak()</script>");
            if (!project.Equals(snapshot.Project.Name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Việc không hợp lệ.");
            return snapshot;
        }
        public WorkRequestProgress GetRequest(string project, string requestId) =>
            GetProject(project).Requests.Single(r => r.Id.Equals(requestId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Config(ProjectInfo project) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => [project];
        public ProjectInfo? FindProject(string name) => name.Equals(project.Name, StringComparison.OrdinalIgnoreCase) ? project : null;
        public TeamSettings GetSettings() => new("", 1, [], "", "", "", [], "");
        public void SaveSettings(TeamSettings settings) => throw new InvalidOperationException("Không được ghi.");
        public void SaveProject(ProjectInfo value) => throw new InvalidOperationException("Không được ghi.");
    }
    private sealed class Board(ProjectInfo project) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string name) => new(project, [], "");
        public TaskDetail? GetTask(string projectName, string taskId) => null;
    }
    private static async Task<HubTestHost> Host(TestEnvironment env, WorkSource source, ProjectInfo project) =>
        await HubTestHost.StartAsync(env, services =>
        {
            services.AddSingleton<IWorkProgressService>(source);
            services.AddSingleton<ITeamConfigService>(new Config(project));
            services.AddSingleton<ITaskBoardService>(new Board(project));
        });

    private static string WorkHtml(string html)
    {
        Assert.Contains("data-work-page", html);
        return html;
    }
    private static void Steps(string html)
    {
        foreach (var label in Labels) Assert.Contains(label, WebUtility.HtmlDecode(html));
        Assert.Equal(9, Regex.Matches(html, "data-work-step=\"[0-8]\"").Count);
    }

    [Fact]
    public async Task Default_renders_real_request_nine_steps_and_existing_approval_link()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot();
        var source = new WorkSource(snapshot);
        await using var host = await Host(env, source, snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Steps(html);
        Assert.Contains("Việc", WebUtility.HtmlDecode(html));
        Assert.Contains("href=\"/Approvals\"", html);
        Assert.Contains("data-work-request=\"R-010\"", html);
        Assert.Single(source.Calls);
        Assert.Equal("AITeam", source.Calls[0]);
    }

    [Theory]
    [InlineData("AITeam", "R-010", "S1-04")]
    [InlineData("Other", "r-010", "s1-07")]
    public async Task Selection_uses_requested_project_and_task(string project, string request, string task)
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot(project);
        var source = new WorkSource(snapshot);
        await using var host = await Host(env, source, snapshot.Project);
        var response = await host.Client.GetAsync($"/p/{project}?requestId={request}&taskId={task}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Assert.Contains($"data-selected-task=\"{task.ToUpperInvariant()}\"", html);
        Assert.Contains("data-selected-request=\"R-010\"", html);
        Steps(html);
        Assert.Equal(new[] { project }, source.Calls);
        if (task.Equals("S1-04", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("Người kiểm thử", WebUtility.HtmlDecode(html));
            Assert.Contains("17:32", WebUtility.HtmlDecode(html));
        }
    }

    [Theory]
    [InlineData("requestId=R-999")]
    [InlineData("requestId=R-009&taskId=S1-04")]
    [InlineData("requestId=R-010&taskId=missing")]
    [InlineData("requestId=..%2F..%2Fprivate")]
    [InlineData("requestId=https%3A%2F%2Fevil.invalid")]
    [InlineData("requestId=R-010&taskId=%3Cscript%3Ex()%3C%2Fscript%3E")]
    public async Task Forged_selection_is_404_without_redirect_or_leak(string query)
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot();
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam?" + query);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("<script>x()</script>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_project_is_404()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot();
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/p/Unknown")).StatusCode);
    }

    [Fact]
    public async Task Empty_project_has_explicit_empty_state_and_no_fake_steps()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot(empty: true);
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Assert.Contains("Chưa có việc.", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("data-work-step=", html);
    }

    [Fact]
    public async Task Request_without_tasks_keeps_nine_steps_and_missing_data()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot();
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam?requestId=R-009");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Steps(html);
        Assert.Contains("Chưa có dữ liệu", WebUtility.HtmlDecode(html));
        Assert.Contains("data-selected-request=\"R-009\"", html);
        Assert.DoesNotContain("data-selected-task=\"S1-04\"", html);
    }

    [Fact]
    public async Task Source_error_has_safe_visible_message()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot();
        await using var host = await Host(env, new(snapshot, fail: true), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Không đọc được tiến độ công việc.", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("secret-source", html);
        Assert.DoesNotContain("leak()", html);
    }

    [Fact]
    public async Task Long_untrusted_title_is_encoded_and_work_section_is_read_only()
    {
        using var env = new TestEnvironment();
        var title = new string('X', 140) + "<script>alert(107)</script><a href=\"javascript:evil()\">x</a>";
        var snapshot = Snapshot(title: title);
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam?requestId=R-010&taskId=S1-04");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Assert.Contains(new string('X', 140), html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert(107)", html);
        Assert.DoesNotContain("href=\"javascript:", html);
        Assert.DoesNotMatch("(?i)<form[^>]*method=[\"']?post", html);
        Assert.DoesNotContain("hx-post", html);
        Assert.DoesNotContain("+ Task", WebUtility.HtmlDecode(html));
        Assert.Single(Regex.Matches(html, "aria-current=\"step\"").Cast<Match>());
    }

    [Fact]
    public async Task Latest_failure_from_real_legacy_journal_is_visible()
    {
        using var env = new TestEnvironment();
        var snapshot = Snapshot(failed: true);
        await using var host = await Host(env, new(snapshot), snapshot.Project);
        var response = await host.Client.GetAsync("/p/AITeam?requestId=R-010&taskId=S1-04");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WorkHtml(await response.Content.ReadAsStringAsync());
        Steps(html);
        Assert.Contains("CẦN SỬA", WebUtility.HtmlDecode(html));
        Assert.Contains("17:03", WebUtility.HtmlDecode(html));
        Assert.Contains("Người kiểm thử", WebUtility.HtmlDecode(html));
    }

    [Fact]
    public void Work_css_wraps_long_titles_and_steps_at_small_width()
    {
        // Chỉ đọc tài sản tĩnh trong source; không đọc cấu hình hay hồ sơ thật.
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Fixture())!, "../../.."));
        var css = File.ReadAllText(Path.Combine(root, "hub/wwwroot/css/hub.css"));
        Assert.Matches(@"(?s)\.work-steps\s*\{[^}]*display:\s*grid", css);
        Assert.Matches(@"(?s)@media[^{]*max-width:\s*(1000|1100)px[^}]*\{.*?\.work-steps\s*\{[^}]*repeat\(\s*[1-5]\s*,", css);
        Assert.Matches(@"(?s)\.work-[^{]+\{[^}]*overflow-wrap:\s*anywhere", css);
        Assert.Matches(@"(?s)\.work-steps[^{}]*\.current\s*\{[^}]*font-weight:\s*(600|700|bold)", css);
    }}