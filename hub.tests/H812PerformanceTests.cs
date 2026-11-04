using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

public sealed class H812PerformanceTests
{
    private sealed class Config(ProjectInfo project) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => [project];
        public ProjectInfo? FindProject(string name) => name == project.Name ? project : null;
        public TeamSettings GetSettings() => new("", 3, [], "", "", "", [], "");
        public void SaveSettings(TeamSettings settings) => throw new NotSupportedException();
        public void SaveProject(ProjectInfo project) => throw new NotSupportedException();
    }

    private sealed class Board(ProjectInfo project, IReadOnlyList<TaskCard> tasks) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string name) => new(project, tasks, "");
        public TaskDetail? GetTask(string project, string id) => null;
    }

    [Fact]
    public void Week_metrics_and_scoreboard_use_real_runs_without_faking_missing_measurements()
    {
        using var env = new TestEnvironment();
        var now = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(7));
        var (config, board) = BuildProject(env, now);
        var service = new PerformanceService(env.Paths, config, board);

        var snapshot = service.Read(now);

        Assert.Equal(5, snapshot.Tasks.Count);
        var codex = Assert.Single(snapshot.Scores, s => s.Provider == "codex" && s.Model == "m1");
        Assert.True(codex.EnoughData);
        Assert.Equal(3, codex.TaskCount);
        Assert.Equal(2.0 / 3 * 100, codex.Round1Rate!.Value, 3);
        Assert.Equal(4.0 / 3, codex.AverageRounds!.Value, 3);
        Assert.Equal("giao diện", codex.Suggested);

        var opencode = Assert.Single(snapshot.Scores, s => s.Provider == "opencode" && s.Model == "deepseek");
        Assert.False(opencode.EnoughData);
        Assert.Equal(2, opencode.TaskCount);
        Assert.Equal(1, opencode.QuotaExhausted);

        Assert.Equal(1, snapshot.Causes.Single(c => c.Cause == "khác").Current);
        var unknownToken = Assert.Single(snapshot.Tasks, t => t.Code == "T-002");
        Assert.Null(unknownToken.Tokens);
        Assert.Equal("M", unknownToken.Difficulty);
        Assert.Contains("PowerShell", unknownToken.WorkTypes);
        Assert.True(Assert.Single(snapshot.Tasks, t => t.Code == "T-001").PassedFirstRound);
        Assert.False(Assert.Single(snapshot.Tasks, t => t.Code == "T-004").PassedFirstRound);
    }

    private static (ITeamConfigService Config, ITaskBoardService Board) BuildProject(TestEnvironment env, DateTimeOffset now)
    {
        var projectRoot = Path.Combine(env.RootDir, "P");
        var tasksDir = Path.Combine(projectRoot, "tasks");
        Directory.CreateDirectory(tasksDir);
        var project = new ProjectInfo("P", "owner/P", projectRoot, projectRoot, "", "", "", new Dictionary<string, string>(), true);
        var cards = new List<TaskCard>
        {
            new("P", "T-001", "Việc một", "C", "b1", TaskState.Unknown, "T-001.md", 1, null),
            new("P", "T-002", "Việc hai", "C", "b2", TaskState.Unknown, "T-002.md", 1, null),
            new("P", "T-003", "Việc ba", "C", "b3", TaskState.Unknown, "T-003.md", 1, null),
            new("P", "T-004", "Việc bốn", "C", "b4", TaskState.Unknown, "T-004.md", 1, null),
            new("P", "T-005", "Tiêu đề <script>alert(1)</script>", "C", "b5", TaskState.Unknown, "T-005.md", 1, null)
        };
        File.WriteAllText(Path.Combine(tasksDir, "README.md"), string.Join('\n',
            "| Task | Agent | Nhánh | Trạng thái |",
            "|---|---|---|---|",
            "| [T-001](T-001.md) | C | b1 | ✅ Đã merge |",
            "| [T-002](T-002.md) | C | b2 | ✅ Đã merge |",
            "| [T-003](T-003.md) | C | b3 | ✅ Đã merge |",
            "| [T-004](T-004.md) | C | b4 | ✅ Đã merge |",
            "| [T-005](T-005.md) | C | b5 | 🟡 Đã giao |"));
        WriteTask(tasksDir, "T-001.md", "S", "hub/Pages/Test.cshtml", "### Vòng 1 — ĐẠT\n- [AG] (giao diện) đã sửa");
        WriteTask(tasksDir, "T-002.md", "M", "tools/test.ps1", "### Vòng 1 — CẦN SỬA\n- [TN] (test) harness\n### Vòng 2 — ĐẠT");
        WriteTask(tasksDir, "T-003.md", "L", "core/AITeam.Core/Foo.cs", "### Vòng 1 — ĐẠT\n- [NV] (nghiệp vụ) mã cũ");
        WriteTask(tasksDir, "T-004.md", "M", "hub.tests/FooTests.cs", "### Vòng 1 — CẦN SỬA\n- [AG] (test) assert");
        WriteTask(tasksDir, "T-005.md", "S", "hub/wwwroot/css/x.css", "### Vòng 1 — ĐẠT");

        var runsDir = Path.Combine(env.AITeamRoot, "metrics", "runs");
        Directory.CreateDirectory(runsDir);
        WriteRun(runsDir, "t1.json", new { project = "P", task = "T-001", run = 1, at = now.AddDays(-1).ToString("O"), durationSec = 60, totalTokens = 100, tokensKnown = true, provider = "codex", model = "m1", status = "SUCCESS", flags = Array.Empty<string>() });
        WriteRun(runsDir, "t2.json", new { project = "P", task = "T-002", run = 1, at = now.AddDays(-1).ToString("O"), durationSec = 120, totalTokens = 0, tokensKnown = false, provider = "codex", model = "m1", status = "SUCCESS", flags = Array.Empty<string>() });
        WriteRun(runsDir, "t3.json", new { project = "P", task = "T-003", run = 1, at = now.AddDays(-1).ToString("O"), durationSec = 180, totalTokens = 300, tokensKnown = true, provider = "opencode", model = "deepseek", status = "SUCCESS", flags = Array.Empty<string>() });
        WriteRun(runsDir, "t4.json", new { project = "P", task = "T-004", run = 1, at = now.AddDays(-1).ToString("O"), durationSec = 240, totalTokens = 400, tokensKnown = true, provider = "opencode", model = "deepseek", status = "QUOTA", flags = Array.Empty<string>() });
        WriteRun(runsDir, "t5.json", new { project = "P", task = "T-005", run = 1, at = now.AddDays(-2).ToString("O"), durationSec = 60, totalTokens = 50, tokensKnown = true, provider = "codex", model = "m1", status = "SUCCESS", flags = Array.Empty<string>() });

        return (new Config(project), new Board(project, cards));
    }

    private static void WriteTask(string tasksDir, string file, string difficulty, string scope, string review) =>
        File.WriteAllText(Path.Combine(tasksDir, file),
            "# " + Path.GetFileNameWithoutExtension(file) + " · Việc\n- **Độ khó:** " + difficulty +
            "\n## Phạm vi\n- `" + scope + "`\n## Review (Claude)\n" + review + "\n");

    private static void WriteRun(string runsDir, string file, object value) =>
        File.WriteAllText(Path.Combine(runsDir, file), JsonSerializer.Serialize(value));
}
