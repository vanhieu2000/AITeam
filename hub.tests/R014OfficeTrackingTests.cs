using System.Net;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

/// <summary>R-014 · UI-01..03 (Claude): dựng trang thật Văn phòng (bảng điều khiển), Theo dõi việc (3 cách xem), nút chat nổi.</summary>
public sealed class R014OfficeTrackingTests
{
    private sealed class Config(ProjectInfo project) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => [project];
        public ProjectInfo? FindProject(string name) => name == project.Name ? project : null;
        public TeamSettings GetSettings() => new("", 3, [], "", "", "", [], "");
        public void SaveSettings(TeamSettings s) => throw new NotSupportedException();
        public void SaveProject(ProjectInfo p) => throw new NotSupportedException();
    }
    private sealed class Board(ProjectInfo project, IReadOnlyList<TaskCard> tasks) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string name) => new(project, tasks, "");
        public TaskDetail? GetTask(string project, string id) => null;
    }
    private sealed class History : IGitHistory { public DateTime? MergedAt(string repo, string task) => DateTime.Now.AddHours(-2); }

    private static async Task<HubTestHost> Start(TestEnvironment env)
    {
        var path = Path.Combine(env.RootDir, "P");
        Directory.CreateDirectory(Path.Combine(path, "tasks"));
        var project = new ProjectInfo("P", "owner/P", path, path, "", "", "", new Dictionary<string, string>(), true);
        // 4 task: C đang làm (độ khó L, R-014), O chờ review (M), O cần sửa (S), C đã merge hôm nay.
        var rows = new (string Id, string Agent, string State, string Diff, string? Req)[]
        {
            ("V10-01", "C", "🟡 Đang làm", "L", "R-014"),
            ("H9-03", "O", "🔵 Chờ review", "M", "R-014"),
            ("V9-18", "O", "🔴 Cần sửa", "S", null),
            ("W2-01", "C", "✅ Đã merge 03/11", "M", "R-014"),
        };
        var cards = new List<TaskCard>();
        foreach (var r in rows)
        {
            var file = r.Id + "-x.md";
            File.WriteAllText(Path.Combine(path, "tasks", file),
                $"# {r.Id} · Tên <script>bad</script> {r.Id}\n\n- **Agent:** {r.Agent}\n- **Độ khó:** {r.Diff}\n" + (r.Req == null ? "" : $"- **Yêu cầu:** {r.Req}\n"));
            cards.Add(new TaskCard("P", r.Id, $"Tên <script>bad</script> {r.Id}", r.Agent, "b", TaskState.Unknown, file, 1, null));
        }
        File.WriteAllText(Path.Combine(path, "tasks", "README.md"), "| Task | Agent | Nhánh | Trạng thái |\n|---|---|---|---|\n" + string.Join('\n', rows.Select(r => $"| {r.Id} | {r.Agent} | b | {r.State} |")));
        return await HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton<ITeamConfigService>(new Config(project));
            s.AddSingleton<ITaskBoardService>(new Board(project, cards));
            s.AddSingleton<IGitHistory>(new History());
        });
    }

    [Fact]
    public async Task Office_dashboard_counts_link_to_tracking_and_has_floating_chat()
    {
        using var env = new TestEnvironment();
        await using var host = await Start(env);
        var html = await host.Client.GetStringAsync("/");
        Assert.DoesNotContain("<script>bad</script>", html);
        foreach (var (state, n) in new[] { ("running", 1), ("review", 1), ("done", 1), ("fix", 1) })
            Assert.Matches($"href=\"/Tracking\\?state={state}\"[^>]*>[\\s\\S]*?class=\"v num\">{n}<", html);
        Assert.Contains("id=\"fab\"", html);
        Assert.Contains("data-chat=\"task\"", html);           // nút "+ Giao việc mới" mở khung chat
        Assert.DoesNotContain("id=\"office-text\"", html);      // khung soạn chỉ nằm trong chế độ nhúng
        Assert.Contains("Sự kiện gần đây", WebUtility.HtmlDecode(html));
    }

    [Theory]
    [InlineData("person")]
    [InlineData("list")]
    [InlineData("kanban")]
    public async Task Tracking_renders_three_views_and_encodes_titles(string view)
    {
        using var env = new TestEnvironment();
        await using var host = await Start(env);
        var html = await host.Client.GetStringAsync("/Tracking?view=" + view);
        Assert.DoesNotContain("<script>bad</script>", html);
        foreach (var code in new[] { "V10-01", "H9-03", "V9-18", "W2-01" }) Assert.Contains(code, html);
        // R-015 (Claude): thanh chọn cách xem đổi từ .tabs sang .seg — đúng 1 mục đang chọn.
        Assert.Single(Regex.Matches(html, "<a class=\"on\" href=\"[^\"]*view=").Cast<Match>());
        if (view == "person")
        {
            Assert.Equal(2, Regex.Matches(html, "class=\"lane\"").Count);   // Codex + OpenCode
            Assert.Contains("Codex", html);
            Assert.Contains("OpenCode", html);
        }
        if (view == "list")
        {
            Assert.Contains("href=\"/Request?id=R-014\"", html);
            Assert.Contains("không gắn yêu cầu", WebUtility.HtmlDecode(html));   // V9-18 không có R-xxx
        }
        if (view == "kanban") Assert.Equal(5, Regex.Matches(html, "class=\"kcol\"").Count);
    }

    [Fact]
    public async Task Tracking_filters_by_person_state_difficulty_and_ignores_bad_values()
    {
        using var env = new TestEnvironment();
        await using var host = await Start(env);
        var byPerson = await host.Client.GetStringAsync("/Tracking?view=list&person=O");
        Assert.Contains("H9-03", byPerson); Assert.DoesNotContain("V10-01", byPerson);
        var byState = await host.Client.GetStringAsync("/Tracking?view=list&state=fix");
        Assert.Contains("V9-18", byState); Assert.DoesNotContain("H9-03", byState);
        var byDiff = await host.Client.GetStringAsync("/Tracking?view=list&difficulty=L");
        Assert.Contains("V10-01", byDiff); Assert.DoesNotContain("W2-01", byDiff);
        var bad = await host.Client.GetStringAsync("/Tracking?view=../x&state=<x>&difficulty=Z&project=nope&person=zz");
        foreach (var code in new[] { "V10-01", "H9-03", "V9-18", "W2-01" }) Assert.Contains(code, bad);
        Assert.DoesNotContain("<x>", bad);
        var none = await host.Client.GetStringAsync("/Tracking?q=khong-co-task-nao");
        Assert.Contains("Không có task nào khớp bộ lọc.", WebUtility.HtmlDecode(none));
    }

    [Fact]
    public async Task Embedded_pages_have_no_sidebar_and_no_nested_chat_button()
    {
        using var env = new TestEnvironment();
        await using var host = await Start(env);
        var html = await host.Client.GetStringAsync("/Tracking?embed=1");
        Assert.DoesNotContain("class=\"side\"", html);
        Assert.DoesNotContain("id=\"fab\"", html);
        Assert.Contains("class=\"embed\"", html);
    }
}
