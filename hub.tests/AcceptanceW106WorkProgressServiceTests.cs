using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Tests.Support;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class AcceptanceW106WorkProgressServiceTests
{
    [Fact]
    public void Real_requests_join_tasks_and_keep_H707_merge()
    {
        using var f = new Fixture();
        var result = f.Service.GetProject("Kiểm thử");
        Assert.Equal(new[] { "R-008", "R-009", "R-010" }, result.Requests.Select(r => r.Id));
        foreach (var (request, task) in new[] { ("R-008", "V8-02"), ("R-009", "H7-07"), ("R-010", "S1-07") })
        {
            var item = Assert.Single(result.Requests, r => r.Id == request);
            var child = Assert.Single(item.Tasks);
            Assert.Equal(task, child.Id);
            Assert.True(child.Progress.IsMerged);
            Assert.Equal(WorkStep.Merge, child.Progress.CurrentStep);
            Assert.Equal(1, item.StepCounts[WorkStep.Merge]);
            Assert.Equal(9, item.StepCounts.Count);
            Assert.Equal(1, item.StepCounts.Values.Sum());
        }
        Assert.Empty(result.UnlinkedTasks);
        Assert.Contains("Hub v6.1", result.Requests.Single(r => r.Id == "R-009").Title);
    }

    [Fact]
    public void Missing_board_uses_W105_journal_without_guessing_V802_status()
    {
        using var f = new Fixture();
        File.Delete(f.RepoFile("tasks/README.md"));
        var task = Assert.Single(f.Service.GetRequest("Kiểm thử", "R-008").Tasks);
        var markdown = File.ReadAllText(f.Request("R-008"));
        var expected = WorkProgressParser.Calculate("V8-02", WorkProgressParser.Parse(markdown));
        Assert.Equal(expected.CurrentStep, task.Progress.CurrentStep);
        Assert.Equal(expected.IsMerged, task.Progress.IsMerged);
        Assert.Equal(expected.LatestError, task.Progress.LatestError);
        Assert.Equal(expected.Steps.Select(s => (s.Step, s.State, s.TimeText)),
            task.Progress.Steps.Select(s => (s.Step, s.State, s.TimeText)));
    }

    [Fact]
    public void Request_without_tasks_remains_and_approved_plan_moves_to_prep()
    {
        using var f = new Fixture();
        File.Delete(f.Task("H7-07"));
        File.AppendAllText(f.RepoFile("plans/R-009/plan.md"), "\r\nQuyết định của quản lý: ĐÃ DUYỆT\r\n", Encoding.UTF8);
        var request = f.Service.GetRequest("Kiểm thử", "r-009");
        Assert.Empty(request.Tasks);
        Assert.Equal(WorkStep.Prep, request.Progress.CurrentStep);
        Assert.All(request.StepCounts.Values, count => Assert.Equal(0, count));
    }

    [Fact]
    public void Multiple_tasks_are_counted_individually_and_old_unlinked_tasks_are_visible()
    {
        using var f = new Fixture();
        var source = File.ReadAllText(f.Task("H7-07"));
        f.Write("repo/tasks/H7-08-ban-sao.md", source.Replace("H7-07", "H7-08"));
        f.Write("repo/tasks/H7-09-ban-cu.md", Regex.Replace(source.Replace("H7-07", "H7-09"),
            @"(?m)^- \*\*Yêu cầu:\*\*[^\r\n]*\r?\n", ""));
        var result = f.Service.GetProject("Kiểm thử");
        var request = result.Requests.Single(r => r.Id == "R-009");
        Assert.Equal(new[] { "H7-07", "H7-08" }, request.Tasks.Select(t => t.Id));
        Assert.Equal(2, request.StepCounts.Values.Sum());
        Assert.Equal(1, request.StepCounts[WorkStep.Merge]);
        Assert.Equal("H7-09", Assert.Single(result.UnlinkedTasks).Id);
    }

    [Fact]
    public void Empty_other_repo_does_not_fall_back_to_AITeam_root()
    {
        using var f = new Fixture();
        f.Config.Project = f.Config.Project with { Path = f.EmptyRepo, WorkDir = f.EmptyRepo };
        var result = f.Service.GetProject("Kiểm thử");
        Assert.Empty(result.Requests);
        Assert.Empty(result.UnlinkedTasks);
        Assert.Equal(f.EmptyRepo, result.Project.Path);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData(" ")]
    [InlineData("../R-009")] [InlineData("R-009/../../x")]
    [InlineData("C:\\R-009")] [InlineData("R-009.md")] [InlineData("R-999")]
    public void Invalid_or_missing_request_is_rejected(string? id)
    {
        using var f = new Fixture();
        var ex = Assert.Throws<InvalidDataException>(() => f.Service.GetRequest("Kiểm thử", id!));
        Assert.Equal("Việc không hợp lệ.", ex.Message);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("../repo")] [InlineData("Không tồn tại")]
    public void Invalid_project_is_rejected(string? project)
    {
        using var f = new Fixture();
        var ex = Assert.Throws<InvalidDataException>(() => f.Service.GetProject(project!));
        Assert.Equal("Việc không hợp lệ.", ex.Message);
    }

    [Theory]
    [InlineData("../R-009")] [InlineData("C:\\ngoai")] [InlineData("R-009/../../ngoai")]
    public void Untrusted_link_metadata_is_not_used_as_a_path(string link)
    {
        using var f = new Fixture();
        var task = f.Task("H7-07");
        File.WriteAllText(task, File.ReadAllText(task).Replace("- **Yêu cầu:** R-009", "- **Yêu cầu:** " + link), new UTF8Encoding(true));
        f.SourceError(() => f.Service.GetProject("Kiểm thử"), Path.GetFileName(task));
    }

    [Fact]
    public void Missing_logs_are_empty_and_not_created()
    {
        using var f = new Fixture();
        var result = f.Service.GetProject("Kiểm thử");
        Assert.All(result.Requests.SelectMany(r => r.Tasks), t => Assert.Empty(t.Logs));
        Assert.False(Directory.Exists(Path.Combine(f.WorkDir, "logs")));
    }

    [Fact]
    public void Review_corruption_reports_only_safe_source_without_secret_or_path()
    {
        using var f = new Fixture();
        var secret = string.Concat("sk-", new string('a', 32));
        f.Write("work/logs/H7-07.review-1.json", "{" + secret);
        var ex = f.SourceError(() => f.Service.GetProject("Kiểm thử"), "H7-07.review-1.json");
        Assert.DoesNotContain(secret, ex.ToString());
    }

    [Theory]
    [InlineData("request")] [InlineData("plan")] [InlineData("task")] [InlineData("board")]
    public void Malformed_UTF8_reports_source(string kind)
    {
        using var f = new Fixture();
        var path = kind switch { "request" => f.Request("R-009"), "plan" => f.RepoFile("plans/R-009/plan.md"),
            "task" => f.Task("H7-07"), _ => f.RepoFile("tasks/README.md") };
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xFE, 0xFF });
        f.SourceError(() => f.Service.GetProject("Kiểm thử"), Path.GetFileName(path));
    }

    [Fact]
    public void Logs_are_read_from_WorkDir_and_repo_log_decoys_are_ignored()
    {
        using var f = new Fixture();
        var journal = File.ReadAllText(f.Request("R-009"));
        f.Write("work/logs/H7-07-verify-1.md", journal);
        f.Write("repo/logs/H7-07.review-1.json", "{hỏng");
        var log = Assert.Single(Assert.Single(f.Service.GetRequest("Kiểm thử", "R-009").Tasks).Logs);
        Assert.Equal("H7-07-verify-1.md", log.Name);
        Assert.Equal(journal, log.Content);
    }

    [Theory]
    [InlineData("requests")] [InlineData("plans")] [InlineData("tasks")] [InlineData("logs")]
    public async Task Reparse_source_directory_is_rejected(string source)
    {
        using var f = new Fixture();
        var link = source == "logs" ? Path.Combine(f.WorkDir, source) : f.RepoFile(source);
        if (Directory.Exists(link)) Directory.Delete(link, true);
        var script = "$null = New-Item -ItemType Junction -Path '" + link.Replace("'", "''") +
            "' -Target '" + f.EmptyRepo.Replace("'", "''") + "'";
        var result = await TestShell.PowerShell(f.Root, script);
        Assert.True(result.Code == 0, result.Error);
        try { f.SourceError(() => f.Service.GetProject("Kiểm thử"), source); }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void Duplicate_request_ID_is_a_source_error()
    {
        using var f = new Fixture();
        File.Copy(f.Request("R-009"), f.RepoFile("requests/R-009-trung.md"));
        f.SourceError(() => f.Service.GetProject("Kiểm thử"), "R-009");
    }

    [Fact]
    public void Oversized_source_is_rejected()
    {
        using var f = new Fixture();
        var path = f.Request("R-009");
        File.WriteAllBytes(path, new byte[2 * 1024 * 1024 + 1]);
        f.SourceError(() => f.Service.GetProject("Kiểm thử"), Path.GetFileName(path));
    }

    [Fact]
    public void Configured_people_are_used_without_invented_timestamps()
    {
        using var f = new Fixture();
        File.Delete(f.Task("H7-07"));
        Directory.Delete(f.RepoFile("plans/R-009"), true);
        var progress = f.Service.GetRequest("Kiểm thử", "R-009").Progress;
        var current = progress.Steps.Single(s => s.Step == WorkStep.Received);
        Assert.Equal("Điều phối thử", current.ActorText);
        Assert.Equal("Chưa có dữ liệu", current.TimeText);
        Assert.All(progress.Steps.Where(s => s.State == WorkStepState.Waiting), s =>
            Assert.Equal("Chưa có dữ liệu", s.ActorText));
    }

    [Fact]
    public async Task Repeated_concurrent_GET_preserves_all_source_bytes_and_returns_fresh_data()
    {
        using var f = new Fixture();
        var before = Directory.GetFiles(f.Root, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() => f.Service.GetProject("Kiểm thử"))));
        Assert.All(results, r => Assert.Equal(3, r.Requests.Count));
        Assert.Equal(before.Keys.Order(), Directory.GetFiles(f.Root, "*", SearchOption.AllDirectories).Order());
        foreach (var file in before) Assert.Equal(file.Value, File.ReadAllBytes(file.Key));
        File.Delete(f.Request("R-010"));
        Assert.DoesNotContain(f.Service.GetProject("Kiểm thử").Requests, r => r.Id == "R-010");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TempRepo temp = new(git: false);
        public string Root => temp.Root;
        public string WorkDir => Path.Combine(Root, "work");
        public string EmptyRepo => Path.Combine(Root, "empty");
        public Config Config { get; }
        public IWorkProgressService Service { get; }
        public Fixture()
        {
            var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "W106");
            Assert.True(Directory.Exists(source), "Thiếu bản sao dữ liệu thật W106.");
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(Root, "repo", Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            Directory.CreateDirectory(WorkDir);
            Directory.CreateDirectory(EmptyRepo);
            Config = new Config(new ProjectInfo("Kiểm thử", "test/repo", Path.Combine(Root, "repo"), WorkDir,
                "", "", "", new Dictionary<string, string>(), true));
            Service = new WorkProgressService(new HubPaths(Path.Combine(Root, "repo"), Path.Combine(Root, "profile")), Config, new Roles());
        }
        public string RepoFile(string path) => Path.Combine(Root, "repo", path);
        public string Request(string id) => Directory.GetFiles(RepoFile("requests"), id + "-*.md").Single();
        public string Task(string id) => Directory.GetFiles(RepoFile("tasks"), id + "-*.md").Single();
        public void Write(string path, string content) => temp.Write(path, content);
        public IOException SourceError(Action action, string source)
        {
            var ex = Assert.Throws<IOException>(action);
            Assert.StartsWith("Không đọc được tiến độ công việc. Nguồn: ", ex.Message);
            Assert.Contains(source, ex.Message);
            Assert.DoesNotContain(Root, ex.ToString());
            return ex;
        }
        public void Dispose() => temp.Dispose();
    }

    private sealed class Config(ProjectInfo project) : ITeamConfigService
    {
        public ProjectInfo Project { get; set; } = project;
        public ProjectInfo? FindProject(string name) => name == Project.Name ? Project : null;
        public IReadOnlyList<ProjectInfo> GetProjects() => new[] { Project };
        public TeamSettings GetSettings() => new("", 3, new[] { new DeveloperInfo("claude", "Điều phối thử", ""),
            new DeveloperInfo("B", "Lập trình thử", "") }, "", "", "", Array.Empty<string>(), "");
        public void SaveProject(ProjectInfo value) => throw new InvalidOperationException("Test cấm ghi cấu hình.");
        public void SaveSettings(TeamSettings value) => throw new InvalidOperationException("Test cấm ghi cấu hình.");
    }

    private sealed class Roles : IWorkflowRolesService
    {
        public WorkflowRolesSnapshot GetRoles() => new(new[] { "receive", "idea", "research", "plan", "approve_plan",
            "prep", "code", "gate", "qc", "accept", "publish" }.Select(c => new WorkflowRole(c, "claude", "")).ToArray(), "test");
        public WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision) =>
            throw new InvalidOperationException("Test cấm ghi phân vai.");
    }
}
