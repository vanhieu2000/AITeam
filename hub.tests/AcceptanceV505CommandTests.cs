using System.Net;
using System.Text;
using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU V5-05 (Claude viết trước — agent KHÔNG sửa). Điều khiển team từ Văn phòng: lệnh cố định, xác nhận, nhật ký.
public sealed class AcceptanceV505CommandTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private DateTime _now = new(2026, 10, 25, 14, 5, 9);

    public void Dispose() => _env.Dispose();

    private static ProjectInfo Proj(string name, bool exists = true) =>
        new(name, "vanhieu2000/" + name, @"D:\" + name, @"D:\" + name + "-work", "", "", "", new Dictionary<string, string>(), exists);

    private static TaskCard Card(string project, string id, TaskState state, string agent = "A", string file = "x.md") =>
        new(project, id, "Việc " + id, agent, "agent-a/" + id, state, file == "x.md" ? id + ".md" : file, 0, null);

    private sealed class Config(params ProjectInfo[] projects) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => projects;
        public ProjectInfo? FindProject(string name) => projects.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        public TeamSettings GetSettings() => throw new NotSupportedException();
        public void SaveSettings(TeamSettings settings) => throw new NotSupportedException();
        public void SaveProject(ProjectInfo project) => throw new NotSupportedException();
    }

    private sealed class Board(Dictionary<string, TaskCard[]> boards) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string project) => new(Proj(project), boards[project], "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private sealed record Call(string Exe, IReadOnlyList<string> Args, string WorkDir, string LogFile, Action<int> OnExit);

    private sealed class FakeStarter : IProcessStarter
    {
        public readonly List<Call> Calls = new();
        public bool ExitImmediately;
        public Exception? Throw;
        public void Start(string exe, IReadOnlyList<string> args, string workDir, string logFile, Action<int> onExit)
        {
            if (Throw != null) throw Throw;
            Calls.Add(new Call(exe, args, workDir, logFile, onExit));
            if (ExitImmediately) onExit(0);
        }
    }

    private readonly FakeStarter _starter = new();

    private CommandService Service(ITaskBoardService? board = null) => new(
        _env.Paths,
        new Config(Proj("AITeam"), Proj("HDVTool"), Proj("Gone", exists: false)),
        board ?? new Board(new()
        {
            ["AITeam"] = new[]
            {
                Card("AITeam", "V5-06", TaskState.Unknown),
                Card("AITeam", "V5-05", TaskState.Assigned, "B"),
                Card("AITeam", "V5-01", TaskState.Merged),
                Card("AITeam", "H3-06", TaskState.Review, "B"),
                Card("AITeam", "V4-10", TaskState.NeedsFix, "C"),
                Card("AITeam", "Z-1", TaskState.Unknown, file: ""),
            },
            ["HDVTool"] = new[] { Card("HDVTool", "T-020", TaskState.Unknown) },
            ["Gone"] = new[] { Card("Gone", "G-1", TaskState.Unknown) },
        }),
        _starter, () => _now);

    // ───────────── Kiểm tra lệnh ─────────────

    [Theory]
    [InlineData(HubCommand.Assign, "Khong", "V5-06", null, "Dự án không hợp lệ.")]
    [InlineData(HubCommand.Assign, "AITeam", "../V5-06", null, "Mã task không hợp lệ.")]
    [InlineData(HubCommand.Assign, "AITeam", "V5-06 -Force", null, "Mã task không hợp lệ.")]
    [InlineData(HubCommand.Assign, "AITeam", "-V5", null, "Mã task không hợp lệ.")]
    [InlineData(HubCommand.Assign, "AITeam", "", null, "Mã task không hợp lệ.")]
    [InlineData(HubCommand.Assign, "AITeam", "V9-99", null, "Không thấy task V9-99 trên bảng việc.")]
    [InlineData(HubCommand.Assign, "AITeam", "V5-05", null, "Chỉ giao được task chưa giao hoặc cần sửa.")]
    [InlineData(HubCommand.AssignCodex, "AITeam", "V5-01", null, "Chỉ giao được task chưa giao hoặc cần sửa.")]
    [InlineData(HubCommand.Stop, "AITeam", "H3-06", null, "Task H3-06 không đang chạy.")]
    [InlineData(HubCommand.Resume, "AITeam", "V5-06", "sửa", "Chỉ giao lại được task chờ review hoặc cần sửa.")]
    [InlineData(HubCommand.Resume, "AITeam", "H3-06", null, "Cần nội dung góp ý.")]
    [InlineData(HubCommand.Resume, "AITeam", "H3-06", " \r\n\t ", "Cần nội dung góp ý.")]
    [InlineData(HubCommand.Verify, "AITeam", "V5-06", null, "Task V5-06 chưa có bản làm việc để kiểm tra.")]
    [InlineData(HubCommand.Verify, "AITeam", "V5-01", null, "Task V5-01 chưa có bản làm việc để kiểm tra.")]
    public void Validate_rejects_with_exact_message(HubCommand cmd, string project, string task, string? feedback, string error)
        => Assert.Equal(error, Service().Validate(new CommandRequest(cmd, project, task, feedback)));

    [Theory]
    [InlineData(HubCommand.Assign, "aiteam", "v5-06", null)]   // không phân biệt hoa thường
    [InlineData(HubCommand.AssignCodex, "AITeam", "V4-10", null)]   // cần sửa ⇒ giao được
    [InlineData(HubCommand.Stop, "AITeam", "V5-05", null)]
    [InlineData(HubCommand.Resume, "AITeam", "V4-10", "Sửa test")]
    [InlineData(HubCommand.Resume, "AITeam", "H3-06", "Sửa test")]
    [InlineData(HubCommand.Verify, "AITeam", "V5-05", null)]
    [InlineData(HubCommand.Verify, "AITeam", "H3-06", null)]
    [InlineData(HubCommand.Assign, "HDVTool", "T-020", null)]
    public void Validate_accepts(HubCommand cmd, string project, string task, string? feedback)
        => Assert.Null(Service().Validate(new CommandRequest(cmd, project, task, feedback)));

    [Fact]
    public void Feedback_length_limit_uses_original_length()
    {
        var s = Service();
        Assert.Null(s.Validate(new CommandRequest(HubCommand.Resume, "AITeam", "H3-06", new string('a', 2000))));
        Assert.Equal("Góp ý quá dài (tối đa 2000 ký tự).", s.Validate(new CommandRequest(HubCommand.Resume, "AITeam", "H3-06", new string('a', 1990) + new string(' ', 11))));
    }

    [Fact]
    public void Running_job_blocks_same_task_and_limit_of_three()
    {
        var s = Service();
        s.Start(new CommandRequest(HubCommand.Stop, "AITeam", "V5-05"));
        Assert.Equal("Đang có lệnh chạy cho V5-05.", s.Validate(new CommandRequest(HubCommand.Verify, "aiteam", "v5-05")));

        s.Start(new CommandRequest(HubCommand.Assign, "AITeam", "V5-06"));
        s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06"));
        Assert.Equal("Đang chạy tối đa 3 lệnh, đợi xong rồi thử lại.", s.Validate(new CommandRequest(HubCommand.Assign, "HDVTool", "T-020")));

        _starter.Calls[0].OnExit(0);   // xong 1 lệnh ⇒ chạy tiếp được, và V5-05 hết bị chặn
        Assert.Null(s.Validate(new CommandRequest(HubCommand.Assign, "HDVTool", "T-020")));
        Assert.Null(s.Validate(new CommandRequest(HubCommand.Verify, "AITeam", "V5-05")));
    }

    // ───────────── Tham số ─────────────

    private static readonly string[] Head = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @"D:\AITeam\team.ps1" };

    [Fact]
    public void Build_arguments_for_each_command()
    {
        string[] Args(HubCommand c, string? fb = null) => CommandService.BuildArguments(new CommandRequest(c, "AITeam", "V5-06", fb), @"D:\AITeam\team.ps1").ToArray();
        Assert.Equal(Head.Concat(new[] { "assign", "-Project", "AITeam", "-Task", "V5-06" }), Args(HubCommand.Assign));
        Assert.Equal(Head.Concat(new[] { "assign", "-Project", "AITeam", "-Task", "V5-06", "-Dev", "codex" }), Args(HubCommand.AssignCodex));
        Assert.Equal(Head.Concat(new[] { "stop", "-Task", "V5-06" }), Args(HubCommand.Stop));
        Assert.Equal(Head.Concat(new[] { "assign", "-Project", "AITeam", "-Task", "V5-06", "-Resume", "-Feedback", "Sửa 'a' / dòng 2" }), Args(HubCommand.Resume, "Sửa \"a\"\r\ndòng 2"));
        Assert.Equal(Head.Concat(new[] { "verify", "-Project", "AITeam", "-Task", "V5-06" }), Args(HubCommand.Verify));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  ok  ", "ok")]
    [InlineData("a\r\nb\nc\rd", "a / b / c / d")]
    [InlineData("say \"hi\" `whoami`", "say 'hi' 'whoami'")]
    [InlineData("a\u0000b\u001bc\td", "abcd")]
    [InlineData("$(Remove-Item x); echo", "$(Remove-Item x); echo")]   // giữ nguyên: là 1 tham số ArgumentList, không qua shell
    public void Clean_feedback(string? input, string expected) => Assert.Equal(expected, CommandService.CleanFeedback(input));

    [Fact]
    public void Labels_and_no_merge_command()
    {
        Assert.Equal(new[] { "Assign", "AssignCodex", "Stop", "Resume", "Verify" }, Enum.GetNames<HubCommand>());   // merge KHÔNG có ở Hub
        Assert.Equal(new[] { "Giao task", "Giao cho Codex", "Dừng", "Giao lại kèm góp ý", "Kiểm tra lại" }, Enum.GetValues<HubCommand>().Select(CommandService.Label));
    }

    // ───────────── Chạy + nhật ký ─────────────

    [Fact]
    public void Start_launches_powershell_and_writes_audit()
    {
        var s = Service();
        var job = s.Start(new CommandRequest(HubCommand.Resume, "AITeam", "H3-06", "Sửa\nlại"));

        Assert.Equal("20261025-140509-H3-06", job.Id);
        Assert.Equal(Path.Combine(_env.AITeamRoot, ".state", "hub-jobs", job.Id + ".log"), job.LogFile);
        Assert.True(Directory.Exists(Path.GetDirectoryName(job.LogFile)));
        Assert.True(job.Running);
        Assert.Equal(_now, job.StartedAt);

        var call = Assert.Single(_starter.Calls);
        Assert.Equal(CommandService.PowerShellExe, call.Exe);
        Assert.EndsWith("powershell.exe", call.Exe, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(_env.AITeamRoot, call.WorkDir);
        Assert.Equal(job.LogFile, call.LogFile);
        Assert.Equal(Path.Combine(_env.AITeamRoot, "team.ps1"), call.Args[4]);
        Assert.Equal("Sửa / lại", call.Args[^1]);

        var a = Assert.Single(s.Audit());
        Assert.Equal((_now, "Resume", "AITeam", "H3-06", "started", "Sửa / lại"), (a.At, a.Action, a.Project, a.Task, a.Result, a.Detail));

        _now = _now.AddMinutes(3);
        call.OnExit(2);
        var done = Assert.Single(s.Jobs());
        Assert.Equal((2, _now, false), (done.ExitCode, done.EndedAt, done.Running));
        Assert.Equal(new[] { "exit 2", "started" }, s.Audit().Select(x => x.Result));
    }

    [Fact]
    public void Rejected_command_is_audited_and_throws()
    {
        var s = Service();
        var ex = Assert.Throws<InvalidOperationException>(() => s.Start(new CommandRequest(HubCommand.Stop, "AITeam", "H3-06")));
        Assert.Equal("Task H3-06 không đang chạy.", ex.Message);
        Assert.Empty(_starter.Calls);
        Assert.Empty(s.Jobs());
        var a = Assert.Single(s.Audit());
        Assert.Equal(("Stop", "rejected", "Task H3-06 không đang chạy."), (a.Action, a.Result, a.Detail));
    }

    [Fact]
    public void Audit_file_is_camelcase_json_lines_without_bom()
    {
        var s = Service();
        s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06"));
        var bytes = File.ReadAllBytes(s.AuditFile);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "audit.jsonl không được có BOM");
        var line = Assert.Single(Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("\"action\":\"Verify\"", line);
        Assert.Contains("\"result\":\"started\"", line);
    }

    [Fact]
    public void Audit_skips_bad_lines_newest_first_and_max()
    {
        var s = Service();
        Directory.CreateDirectory(Path.GetDirectoryName(s.AuditFile)!);
        File.WriteAllText(s.AuditFile, string.Join("\n",
            "{\"at\":\"2026-10-25T09:00:00\",\"action\":\"Assign\",\"project\":\"AITeam\",\"task\":\"A-1\",\"result\":\"started\",\"detail\":null}",
            "không phải json",
            "{\"at\":\"2026-10-25T09:01:00\",\"action\":\"Stop\",\"project\":\"AITeam\",\"task\":\"A-2\",\"result\":\"started\",\"detail\":null}",
            "",
            "{\"at\":\"2026-10-25T09:02:00\",\"action\":\"Verify\",\"project\":\"AITeam\",\"task\":\"A-3\",\"result\":\"exit 0\",\"detail\":null}") + "\n");
        Assert.Equal(new[] { "A-3", "A-2", "A-1" }, s.Audit().Select(a => a.Task));
        Assert.Equal(new[] { "A-3", "A-2" }, s.Audit(2).Select(a => a.Task));
    }

    [Fact]
    public void Audit_without_file_is_empty() => Assert.Empty(Service().Audit());

    [Fact]
    public void Same_second_gets_unique_id_and_jobs_newest_first()
    {
        _starter.ExitImmediately = true;   // onExit gọi ngay trong Start: không được deadlock
        var s = Service();
        var j1 = s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06"));
        var j2 = s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06"));
        Assert.Equal(("20261025-140509-H3-06", "20261025-140509-H3-06-2"), (j1.Id, j2.Id));
        Assert.Equal(new[] { j2.Id, j1.Id }, s.Jobs().Select(j => j.Id));
        Assert.All(s.Jobs(), j => Assert.Equal(0, j.ExitCode));
    }

    [Fact]
    public void Jobs_keeps_at_most_two_hundred()
    {
        // H8-05 (R-012): giữ 200 job gần nhất, bỏ job cũ khỏi bộ nhớ (không chỉ cắt khi hiển thị).
        _starter.ExitImmediately = true;
        var s = Service();
        for (int i = 0; i < 205; i++) { _now = _now.AddSeconds(1); s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06")); }
        var jobs = s.Jobs();
        Assert.Equal(200, jobs.Count);
        Assert.Equal(_now.AddSeconds(-199), jobs[^1].StartedAt);
        Assert.Equal(_now, jobs[0].StartedAt);
    }

    [Fact]
    public void Starter_failure_ends_job_and_rethrows()
    {
        _starter.Throw = new IOException("không thấy powershell");
        var s = Service();
        Assert.Throws<IOException>(() => s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06")));
        var j = Assert.Single(s.Jobs());
        Assert.Equal(-1, j.ExitCode);
        Assert.Equal(("exit -1", "không thấy powershell"), (s.Audit()[0].Result, s.Audit()[0].Detail));
        Assert.Null(s.Validate(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06")));   // không còn bị chặn
    }

    [Fact]
    public void Assignable_and_resumable_lists()
    {
        var s = Service();
        Assert.Equal(new[] { "AITeam/V5-06", "AITeam/V4-10", "HDVTool/T-020" }, s.Assignable().Select(c => c.Project + "/" + c.Id));   // bỏ Z-1 (không có file), Gone (không có trên máy)
        Assert.Equal(new[] { "AITeam/H3-06", "AITeam/V4-10" }, s.Resumable().Select(c => c.Project + "/" + c.Id));
    }

    // ───────────── Tiến trình thật (cmd.exe, không PowerShell) ─────────────

    [Fact]
    public async Task Real_starter_captures_output_and_exit_code()
    {
        var log = Path.Combine(_env.RootDir, "job.log");
        var done = new TaskCompletionSource<int>();
        var cmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        new RealProcessStarter().Start(cmd, new[] { "/c", "echo xin-chao& echo loi-ra-stderr 1>&2& exit 3" }, _env.RootDir, log, code => done.TrySetResult(code));
        var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(done.Task, finished);
        Assert.Equal(3, await done.Task);
        var text = File.ReadAllText(log);
        Assert.Contains("xin-chao", text);
        Assert.Contains("loi-ra-stderr", text);
    }

    // ───────────── Trang ─────────────

    private sealed class FakeOffice : IOfficeService
    {
        public OfficeView Get() => new(Array.Empty<Desk>(), 0, 0, 0, 0, new DateTime(2026, 10, 25, 14, 0, 0));
    }

    [Fact]
    public void Post_command_success_and_errors()
    {
        var s = Service();
        var p = new OfficeModel(new FakeOffice(), s);
        var r = Assert.IsType<PartialViewResult>(p.OnPostCommand("assign", "AITeam/V5-06", null));
        Assert.Equal("_OfficeControl", r.ViewName);
        Assert.Same(p, r.Model);
        Assert.Equal("Đã bắt đầu: Giao task V5-06.", p.Notice);
        Assert.Null(p.CommandError);
        Assert.Single(p.Jobs);
        Assert.Single(_starter.Calls);

        foreach (var bad in new[] { null, "", "0", "4", "Merge", "Assign;Stop" })
        {
            var q = new OfficeModel(new FakeOffice(), s);
            q.OnPostCommand(bad, "AITeam/V5-06", null);
            Assert.Equal("Lệnh không hợp lệ.", q.CommandError);
            Assert.Null(q.Notice);
        }
        Assert.Single(_starter.Calls);

        var rej = new OfficeModel(new FakeOffice(), s);
        rej.OnPostCommand("Stop", "AITeam/H3-06", null);
        Assert.Equal("Task H3-06 không đang chạy.", rej.CommandError);

        var noSlash = new OfficeModel(new FakeOffice(), s);
        noSlash.OnPostCommand("Verify", "H3-06", null);
        Assert.Equal("Dự án không hợp lệ.", noSlash.CommandError);

        var traversal = new OfficeModel(new FakeOffice(), s);
        traversal.OnPostCommand("Verify", "AITeam/../H3-06", null);
        Assert.Equal("Mã task không hợp lệ.", traversal.CommandError);

        var readOnly = new OfficeModel(new FakeOffice());
        Assert.False(readOnly.ControlsEnabled);
        readOnly.OnPostCommand("Verify", "AITeam/H3-06", null);
        Assert.Equal("Hub đang ở chế độ chỉ xem.", readOnly.CommandError);
    }

    [Fact]
    public void Get_loads_control_lists()
    {
        var p = new OfficeModel(new FakeOffice(), Service());
        p.OnGet();
        Assert.True(p.ControlsEnabled);
        Assert.Equal(3, p.Assignable.Count);
        Assert.Equal(2, p.Resumable.Count);
        Assert.Empty(p.Jobs);
    }

    private sealed class BrokenBoard : ITaskBoardService
    {
        public ProjectBoard GetBoard(string project) => throw new IOException("README hỏng");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    [Fact]
    public void Broken_board_shows_error_not_crash()
    {
        var p = new OfficeModel(new FakeOffice(), Service(new BrokenBoard()));
        p.OnGet();
        Assert.Equal("Không đọc được danh sách task: README hỏng", p.CommandError);
        Assert.Empty(p.Assignable);
    }

    [Fact]
    public void Job_log_only_for_known_jobs_and_tail()
    {
        var s = Service();
        var job = s.Start(new CommandRequest(HubCommand.Verify, "AITeam", "H3-06"));
        var p = new OfficeModel(new FakeOffice(), s);
        Assert.Equal("(chưa có output)", Assert.IsType<ContentResult>(p.OnGetJobLog(job.Id)).Content);

        File.WriteAllText(job.LogFile, new string('x', 9000) + "CUỐI");
        var c = Assert.IsType<ContentResult>(p.OnGetJobLog(job.Id));
        Assert.Equal(8000, c.Content!.Length);
        Assert.EndsWith("CUỐI", c.Content);
        Assert.StartsWith("text/plain", c.ContentType);

        Assert.IsType<NotFoundResult>(p.OnGetJobLog(null));
        Assert.IsType<NotFoundResult>(p.OnGetJobLog(@"..\..\team.local"));
        Assert.IsType<NotFoundResult>(p.OnGetJobLog(job.Id.ToUpperInvariant() + "x"));
        Assert.IsType<NotFoundResult>(new OfficeModel(new FakeOffice()).OnGetJobLog(job.Id));
    }

    [Fact]
    public void Jobs_handler_returns_partial()
    {
        var p = new OfficeModel(new FakeOffice(), Service());
        var r = Assert.IsType<PartialViewResult>(p.OnGetJobs());
        Assert.Equal("_OfficeJobs", r.ViewName);
        Assert.Same(p, r.Model);
    }

    private static string Source(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, relative))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, relative));
    }

    [Fact]
    public void Razor_markup_follows_layout()
    {
        var control = Source(Path.Combine("hub", "Pages", "Shared", "_OfficeControl.cshtml"));
        Assert.Contains("id=\"office-control\"", control);
        Assert.Contains("asp-page-handler=\"Command\"", control);
        Assert.Contains("hx-post=\"/?handler=Command\"", control);
        Assert.Contains("hx-confirm", control);
        Assert.Contains("value=\"AssignCodex\"", control);
        Assert.Contains("value=\"Resume\"", control);
        Assert.Contains("maxlength=\"2000\"", control);
        Assert.Contains("_OfficeJobs", control);
        Assert.DoesNotContain("Merge", control);
        var jobs = Source(Path.Combine("hub", "Pages", "Shared", "_OfficeJobs.cshtml"));
        Assert.Contains("handler=JobLog", jobs);
        var desks = Source(Path.Combine("hub", "Pages", "Shared", "_OfficeDesks.cshtml"));
        Assert.Contains("value=\"Stop\"", desks);
        Assert.Contains("value=\"Verify\"", desks);
        Assert.Contains("hx-confirm", desks);
        Assert.Contains("ControlsEnabled", desks);
        var page = Source(Path.Combine("hub", "Pages", "Office.cshtml"));
        Assert.Contains("_OfficeControl", page);
        foreach (var src in new[] { control, jobs, desks, page }) Assert.DoesNotContain("Html.Raw", src);
    }

    // ───────────── Dựng thật: chống CSRF + trang có bảng điều khiển ─────────────

    private async Task WithHub(Func<HttpClient, Task> act)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(OfficeModel).Assembly.GetName().Name });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(new HubPaths(_env.AITeamRoot, _env.UserProfile));
        builder.Services.AddRazorPages().AddApplicationPart(typeof(OfficeModel).Assembly);
        builder.Services.AddAntiforgery();
        FakeServices.Register(builder.Services);
        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorPages();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            await act(client);
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public Task Post_without_antiforgery_token_is_rejected() => WithHub(async c =>
    {
        var r = await c.PostAsync("/?handler=Command", new FormUrlEncodedContent(new Dictionary<string, string> { ["command"] = "Assign", ["target"] = "AITeam/T-001" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False(File.Exists(Path.Combine(_env.AITeamRoot, ".state", "audit.jsonl")));
    });

    [Fact]
    public Task Office_page_renders_control_panel_in_fake_mode() => WithHub(async c =>
    {
        var html = await c.GetStringAsync("/");
        Assert.Contains("id=\"office-control\"", html);
        Assert.Contains("__RequestVerificationToken", html);
        var jobs = await c.GetAsync("/?handler=Jobs");
        Assert.Equal(HttpStatusCode.OK, jobs.StatusCode);
    });

    [Fact]
    public void Fake_mode_never_starts_real_processes()
    {
        var services = new ServiceCollection();
        FakeServices.Register(services);
        Assert.Contains(services, d => d.ServiceType == typeof(IProcessStarter) && d.ImplementationType == typeof(NoopProcessStarter));
        Assert.Contains(services, d => d.ServiceType == typeof(ICommandService));
    }
}
