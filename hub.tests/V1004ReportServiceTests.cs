using System.Globalization;
using System.Text.Json;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>
/// Nghiệm thu V10-04: dịch vụ số liệu Báo cáo &amp; thống kê.
/// Fixture tạm: 2 dự án, 3 người (codex / opencode / A), 8 task, 12 lượt, 1 phiếu R-901.
/// Ngày: d1 = 2026-10-26, d2 = 2026-10-27, d3 = 2026-11-03 (ISO tuần T44 rồi T45).
/// </summary>
public sealed class V1004ReportServiceTests
{
    private static readonly DateOnly D1 = new(2026, 10, 26);
    private static readonly DateOnly D2 = new(2026, 10, 27);
    private static readonly DateOnly D3 = new(2026, 11, 3);
    private static readonly ReportFilter All = new(D1, new DateOnly(2026, 11, 8));

    [Fact]
    public void Filters_by_project_person_and_difficulty_change_task_counts()
    {
        using var fixture = BuildFixture();
        Assert.Equal(8, Sum(fixture.Service.Query(All)));
        Assert.Equal(5, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Project: "P1"))));
        Assert.Equal(3, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Project: "P2"))));
        Assert.Equal(4, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Person: "codex"))));
        Assert.Equal(4, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Person: "A"))));
        Assert.Equal(2, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Difficulty: "L"))));
        Assert.Equal(3, Sum(fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Difficulty: "S"))));

        var data = fixture.Service.Query(All);
        Assert.Equal(4, data.DifficultyCounts.Count);
        Assert.Equal(1d, data.DifficultyCounts.Single(d => d.Name == "chưa rõ").Value);
        Assert.Contains("P1", data.Projects);
        Assert.Contains("OpenCode", data.People);
    }

    [Fact]
    public void Kpis_compare_to_previous_window_of_same_length_and_stay_null_when_unmeasured()
    {
        using var fixture = BuildFixture();
        var data = fixture.Service.Query(new ReportFilter(D3, new DateOnly(2026, 11, 9)));
        Assert.Equal(1d, Kpi(data, "done").Value!.Value);
        Assert.Equal(2d, Kpi(data, "done").Previous!.Value);
        Assert.Equal(100d, Kpi(data, "first_pass_pct").Value!.Value);
        Assert.Equal(100d, Kpi(data, "first_pass_pct").Previous!.Value);
        Assert.Equal(70d, Kpi(data, "ai_minutes").Value!.Value);
        Assert.Equal(16d, Kpi(data, "ai_minutes").Previous!.Value);
        Assert.Equal(0d, Kpi(data, "errors_tk").Value!.Value);

        var empty = fixture.Service.Query(new ReportFilter(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 7)));
        Assert.Null(Kpi(empty, "first_pass_pct").Value);
        Assert.Null(Kpi(empty, "ai_minutes").Value);
        Assert.Null(Kpi(empty, "errors_tk").Value);
        Assert.Equal(0d, Kpi(empty, "done").Value!.Value);
    }

    [Fact]
    public void Weeks_use_iso_labels_and_include_empty_weeks_with_null_first_pass()
    {
        using var fixture = BuildFixture();
        var data = fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 15)));
        Assert.Equal(3, data.Weeks.Count);
        Assert.Equal(new WeekPoint("T44", D1, 5, 80, 2, 1, 0), data.Weeks[0]);
        Assert.Equal("T45", data.Weeks[1].Week);
        Assert.Equal(1, data.Weeks[1].Done);
        Assert.Equal("T46", data.Weeks[2].Week);
        Assert.Equal(0, data.Weeks[2].Done);
        Assert.Null(data.Weeks[2].FirstPassPct);
    }

    [Fact]
    public void Score_matches_hand_calculation_and_redistributes_missing_components()
    {
        using var fixture = BuildFixture();
        var codex = fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Project: "P1"))
            .Scores.Single(s => s.Person == "codex");
        Assert.Equal(3, codex.Tasks);
        Assert.Equal(63, codex.Score);
        Assert.Equal(66.67, codex.FirstPassPct!.Value, 2);
        Assert.Equal(50d, codex.InBudgetPct!.Value);
        Assert.Equal(66.67, codex.OnTimePct!.Value, 2);
        Assert.Equal(1, codex.AgErrors);

        var withoutBudget = fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Project: "P2"))
            .Scores.Single(s => s.Person == "codex");
        Assert.Equal(1, withoutBudget.Tasks);
        Assert.Null(withoutBudget.InBudgetPct);
        Assert.Equal(73, withoutBudget.Score);
    }

    [Fact]
    public void Error_matrix_groups_by_cause_and_category_with_task_codes()
    {
        using var fixture = BuildFixture();
        var cells = fixture.Service.Query(new ReportFilter(D1, new DateOnly(2026, 11, 8), Project: "P1")).ErrorMatrix;
        var tk = cells.Single(c => c.Cause == "TK" && c.Category == "test");
        Assert.Equal(2, tk.Count);
        Assert.Equal(new[] { "P1-02" }, tk.Tasks);
        var ag = cells.Single(c => c.Cause == "AG" && c.Category == "giao diện");
        Assert.Equal(1, ag.Count);
        Assert.Equal(new[] { "P1-02" }, ag.Tasks);
    }

    [Fact]
    public void Timeline_follows_the_ticket_log_and_adds_run_minutes_tokens_rounds_findings_and_long_waits()
    {
        using var fixture = BuildFixture();
        var timeline = fixture.Service.Timeline("R-901");
        Assert.NotNull(timeline);
        Assert.Equal("Việc thử báo cáo", timeline!.Title);
        Assert.Equal(5, timeline.Steps.Count);
        Assert.Equal("Nhận yêu cầu", timeline.Steps[0].Name);
        Assert.Null(timeline.Steps[1].WaitMinutesBefore);

        var review = timeline.Steps.Single(s => s.Name.StartsWith("Review vòng 1", StringComparison.Ordinal));
        Assert.Equal(50d, review.WaitMinutesBefore!.Value);
        Assert.Equal(50d, review.Minutes!.Value);
        Assert.Equal(450L, review.Tokens!.Value);
        Assert.Equal(1, review.Rounds);
        Assert.Equal(2, review.Runs);
        Assert.Equal(3, review.Findings.Count);
        Assert.Contains("Codex", review.Actor!);
        Assert.Equal(new[] { "P1-02" }, review.Tasks);

        Assert.Throws<ArgumentException>(() => fixture.Service.Timeline("../x"));
        Assert.Null(fixture.Service.Timeline("R-999"));
    }

    [Fact]
    public void ExportCsv_has_bom_escapes_formula_cells_and_rejects_unknown_table()
    {
        using var fixture = BuildFixture();
        var csv = fixture.Service.ExportCsv(All, "tasks");
        Assert.Equal('\uFEFF', csv[0]);
        Assert.Contains("'=SUM(1)", csv);

        Assert.StartsWith("Week,Start,Done", fixture.Service.ExportCsv(All, "weeks")[1..]);
        Assert.StartsWith("Person,Display,Score", fixture.Service.ExportCsv(All, "scores")[1..]);
        Assert.StartsWith("Cause,Category,Count", fixture.Service.ExportCsv(All, "errors")[1..]);
        Assert.Throws<ArgumentException>(() => fixture.Service.ExportCsv(All, "unknown"));
    }

    [Fact]
    public void Broken_task_file_goes_to_warnings_instead_of_throwing()
    {
        using var env = new TestEnvironment();
        var folder = Path.Combine(env.RootDir, "broken");
        Directory.CreateDirectory(Path.Combine(folder, "tasks"));
        File.WriteAllBytes(Path.Combine(folder, "tasks", "T-001.md"), [0x80, 0x81, 0x00]);
        var project = new ProjectInfo("B", "o/b", folder, folder, "", "", "", new Dictionary<string, string>(), true);
        var board = new FakeBoard(project, [new TaskCard("B", "T-001", "Hỏng", "A", "", TaskState.Merged, "T-001.md", 0, null)]);
        var service = new ReportService(env.Paths, new FakeConfig([project]), board);

        var data = service.Query(new ReportFilter(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7)));
        Assert.Contains(data.Warnings, w => w.Contains("Không đọc được file task T-001", StringComparison.Ordinal));
        Assert.Empty(data.TasksByPerson);
    }

    private static ReportKpi Kpi(ReportData data, string key) => data.Kpis.Single(k => k.Key == key);
    private static int Sum(ReportData data) => (int)data.TasksByPerson.Sum(p => p.Value);

    private sealed class FakeConfig(IReadOnlyList<ProjectInfo> projects) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => projects;
        public ProjectInfo? FindProject(string name) => projects.FirstOrDefault(p => p.Name == name);
        public TeamSettings GetSettings() => new("", 3, [], "", "", "", [], "");
        public void SaveSettings(TeamSettings settings) => throw new NotSupportedException();
        public void SaveProject(ProjectInfo project) => throw new NotSupportedException();
    }

    private sealed class FakeBoard(ProjectInfo project, IReadOnlyList<TaskCard> cards) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string name) => new(project, cards, "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestEnvironment _env = new();
        private readonly List<ProjectInfo> _projects = [];
        private readonly Dictionary<string, List<TaskCard>> _cards = new(StringComparer.OrdinalIgnoreCase);
        private int _runFile;
        public ReportService Service { get; }

        public Fixture()
        {
            Directory.CreateDirectory(Path.Combine(_env.AITeamRoot, "metrics", "runs"));
            Directory.CreateDirectory(Path.Combine(_env.AITeamRoot, "requests"));
            AddProject("P1");
            AddProject("P2");

            AddTask("P1", "P1-01", "A", "S", "### Vòng 1 — ĐẠT\n- OK.", 100, 30, TaskState.Merged);
            AddTask("P1", "P1-02", "A", "M", "### Vòng 1 — CẦN SỬA\n- [TK] (test) Thiếu kiểm tra biên.\n- [TK] (test) Sai thứ tự tham số.\n- [AG] (giao diện) Nút lệch chỗ.", 100, 30, TaskState.Merged);
            AddTask("P1", "P1-03", "B", "L", "### Vòng 1 — ĐẠT", 100, 30, TaskState.Merged);
            AddTask("P1", "P1-04", "B", null, null, 100, 30, TaskState.Assigned);
            AddTask("P1", "P1-05", "A", "S", "### Vòng 1 — ĐẠT", null, 30, TaskState.Merged);
            AddTask("P2", "P2-01", "B", "M", "### Vòng 1 — ĐẠT", 50, 30, TaskState.Merged);
            AddTask("P2", "P2-02", "A", "L", "### Vòng 1 — ĐẠT", null, 30, TaskState.Assigned);
            AddTask("P2", "P2-03", "B", "S", "### Vòng 1 — ĐẠT", 100, 30, TaskState.Merged, title: "=SUM(1)");

            AddRun("P1", "P1-01", 1, At(D1, 10, 0), "codex", "gpt-5", 600, 100, 50);
            AddRun("P1", "P1-02", 1, At(D1, 11, 0), "codex", "gpt-5", 2400, 400, 200);
            AddRun("P1", "P1-02", 2, At(D1, 12, 0), "codex", "gpt-5", 600, 50, 50);
            AddRun("P1", "P1-03", 1, At(D2, 9, 0), "opencode", "deepseek", 300, 50, 30);
            AddRun("P1", "P1-03", 2, At(D2, 9, 30), "opencode", "deepseek", 180, 10, 0);
            AddRun("P1", "P1-04", 1, At(D2, 10, 0), "opencode", "deepseek", 120, 20, 10);
            AddRun("P1", "P1-05", 1, At(D3, 9, 0), "codex", "gpt-5", 600, 100, 10);
            AddRun("P2", "P2-01", 1, At(D1, 12, 0), "opencode", "deepseek", 600, 100, 100);
            AddRun("P2", "P2-01", 2, At(D1, 13, 0), "opencode", "deepseek", 120, 20, 0);
            AddRun("P2", "P2-02", 1, At(D3, 10, 0), "codex", "gpt-5", 3600, 1000, 5);
            AddRun("P2", "P2-03", 1, At(D2, 11, 0), "opencode", "deepseek", 300, 50, 20);
            AddRun("P2", "P2-03", 2, At(D2, 11, 30), "opencode", "deepseek", 60, 10, 0);

            WriteRequest();
            Service = new ReportService(_env.Paths, new FakeConfig(_projects), new FakeBoardFor(_projects, _cards));
        }

        public HubPaths Paths => _env.Paths;

        private void AddProject(string name)
        {
            var path = Path.Combine(_env.RootDir, "work", name);
            Directory.CreateDirectory(Path.Combine(path, "tasks"));
            _projects.Add(new ProjectInfo(name, "owner/" + name, path, path, "", "", "", new Dictionary<string, string>(), true));
            _cards[name] = [];
        }

        private void AddTask(string project, string code, string agent, string? difficulty, string? review,
            int? budget, int maxMinutes, TaskState state, string? title = null)
        {
            var projectPath = _projects.Single(p => p.Name == project).Path;
            title ??= "Việc " + code;
            var text = "# " + code + " · " + title + "\n\n- **Agent:** " + agent + "\n";
            if (budget.HasValue) text += "- **Ngân sách:** ~" + budget + " dòng\n";
            text += "- **Yêu cầu:** R-901\n- **Thời gian tối đa:** " + maxMinutes + " phút\n";
            if (difficulty != null) text += "- **Độ khó:** " + difficulty + "\n";
            if (review != null) text += "\n## Review (Claude)\n" + review + "\n";
            var fileName = code + ".md";
            File.WriteAllText(Path.Combine(projectPath, "tasks", fileName), text);
            _cards[project].Add(new TaskCard(project, code, title, agent, "", state, fileName, 0, null));
        }

        private void AddRun(string project, string task, int run, DateTimeOffset at, string provider, string model,
            double seconds, long tokens, int added)
        {
            var dir = Path.Combine(_env.AITeamRoot, "metrics", "runs");
            var json = JsonSerializer.Serialize(new
            {
                project, task, run, at = at.ToString("O", CultureInfo.InvariantCulture), provider, model,
                durationSec = seconds == 0 ? (double?)null : seconds,
                tokensKnown = true, totalTokens = tokens, linesAdded = added, status = "SUCCESS"
            });
            File.WriteAllText(Path.Combine(dir, $"{++_runFile:D4}-{project}-{task}-{run}.json"), json);
        }

        private void WriteRequest()
        {
            var text = """
                # R-901 · Việc thử báo cáo

                - **Nhận lúc:** 20:00 26/10/2026 · máy test
                - **Trạng thái:** 🟡 Đang làm

                ## 2. Chia việc
                | Task | Dự án | Ai | Ngân sách |
                |---|---|---|---|
                | P1-01 | P1 | A | ~100 dòng |
                | P1-02 | P1 | A | ~100 dòng |

                ## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
                | Giờ | Sự kiện | Ghi chú |
                |---|---|---|
                | 20:00 | Nhận yêu cầu | |
                | 20:10 | Giao task P1-01 | |
                | 20:20 | Agent xong P1-01 | |
                | 21:10 | Review vòng 1 P1-02 | |
                | 21:20 | Merge P1-01 | |
                """;
            File.WriteAllText(Path.Combine(_env.AITeamRoot, "requests", "R-901-viec-thu.md"), text);
        }

        private static DateTimeOffset At(DateOnly day, int hour, int minute)
            => new(day.ToDateTime(new TimeOnly(hour, minute)));

        public void Dispose() => _env.Dispose();

        private sealed class FakeBoardFor(IReadOnlyList<ProjectInfo> projects, Dictionary<string, List<TaskCard>> cards) : ITaskBoardService
        {
            public ProjectBoard GetBoard(string name)
                => new(projects.Single(p => p.Name == name), cards.TryGetValue(name, out var value) ? value : [], "");
            public TaskDetail? GetTask(string project, string taskId) => null;
        }
    }

    private static Fixture BuildFixture() => new();
}
