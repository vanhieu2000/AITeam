using AITeam.Core.Messages;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU V5-04 (Claude viết trước — agent KHÔNG sửa). Trang Văn phòng: bàn làm việc từng thành viên.
public sealed class AcceptanceV504OfficeTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 10, 42, 30);

    private static readonly StaffMember[] Staff =
    {
        new("C", "Codex", "Dự phòng", "codex"),
        new("A", "Minh", "Nền tảng", "agy"),
        new("B", "Lan", "Tính năng", "agy"),
    };

    private static TaskCard Card(string id, string agent, TaskState state, DateTime? lastRun = null, string project = "AITeam") =>
        new(project, id, "Việc " + id, agent, $"agent-{agent.ToLower()}/{id}", state, id + ".md", 0,
            lastRun == null ? null : new AgentRun(project, id, 1, RunState.Success, "SUCCESS", null, null, 60, 1, lastRun.Value, "x.json"));

    private static TaskMessage Msg(string task, int min, string text) => new(Now.AddMinutes(-min), task, "Minh (A)", "progress", text);

    private static OfficeInput Input(
        IReadOnlyList<TaskCard>? tasks = null, IReadOnlyList<RunningAgent>? running = null, IReadOnlyList<ProviderRest>? rests = null,
        RequestThread? req = null)
    {
        var msgs = new Dictionary<string, IReadOnlyList<TaskMessage>>
        {
            ["AITeam/V5-01"] = new[] { Msg("V5-01", 7, "Bắt đầu"), Msg("V5-01", 6, "Đọc xong khung"), Msg("V5-01", 3, "Đã viết get_task"), Msg("V5-01", 1, "Đang chạy test") },
        };
        return new OfficeInput(Now, Staff,
            tasks ?? new[]
            {
                Card("V5-01", "A", TaskState.Assigned),
                Card("V5-04", "B", TaskState.Assigned),
                Card("V4-11", "C", TaskState.Merged),
                Card("H3-06", "B", TaskState.Review, Now.AddHours(-2)),
                Card("V4-10", "C", TaskState.Review, Now.AddHours(-1)),
            },
            running ?? new[] { new RunningAgent(4242, Now.AddMinutes(-7).AddSeconds(-20), "AITeam", "V5-01", "agy.exe -p …") },
            rests ?? Array.Empty<ProviderRest>(),
            (p, t) => msgs.TryGetValue($"{p}/{t}", out var m) ? m : Array.Empty<TaskMessage>(),
            req);
    }

    [Fact]
    public void Desk_order_and_agent_states()
    {
        var v = OfficeBuilder.Build(Input());
        Assert.Equal(new[] { "claude", "A", "B", "C", "gate", "research" }, v.Desks.Select(d => d.Key));
        Assert.Equal(Now, v.At);

        var minh = v.Desks[1];
        Assert.Equal(("Minh", "Nền tảng", DeskState.Working, "Đang làm 7′"), (minh.Name, minh.Role, minh.State, minh.StateText));
        Assert.Equal(("AITeam", "V5-01", "Việc V5-01", (int?)7), (minh.Task!.Project, minh.Task.Id, minh.Task.Title, minh.Task.ElapsedMin));
        Assert.Equal(new[] { "Đang chạy test", "Đã viết get_task", "Đọc xong khung" }, minh.Feed.Select(f => f.Text));   // 3 tin, mới nhất trước

        var lan = v.Desks[2];
        Assert.Equal((DeskState.Working, "Đang làm (máy khác / chưa chạy)", (int?)null), (lan.State, lan.StateText, lan.Task!.ElapsedMin));
        Assert.Empty(lan.Feed);

        var codex = v.Desks[3];
        Assert.Equal((DeskState.Idle, "Rảnh"), (codex.State, codex.StateText));
        Assert.Null(codex.Task);

        Assert.Equal((2, 2, 1, 0), (v.Working, v.Review, v.Idle, v.Resting));
    }

    [Fact]
    public void Running_task_is_preferred_when_agent_has_several()
    {
        var tasks = new[] { Card("X-1", "A", TaskState.Assigned), Card("V5-01", "A", TaskState.Assigned) };
        var v = OfficeBuilder.Build(Input(tasks));
        Assert.Equal("V5-01", v.Desks.Single(d => d.Key == "A").Task!.Id);
        var noRun = OfficeBuilder.Build(Input(tasks, running: Array.Empty<RunningAgent>()));
        Assert.Equal("X-1", noRun.Desks.Single(d => d.Key == "A").Task!.Id);
    }

    [Fact]
    public void Resting_provider_and_expired_rest()
    {
        var rests = new[]
        {
            new ProviderRest("CODEX", new DateTime(2026, 10, 25, 16, 19, 0), RunStatus.QuotaExhausted, "usage limit", Now.AddHours(-1)),
            new ProviderRest("agy", Now.AddMinutes(-1), RunStatus.QuotaExhausted, "hết", Now.AddHours(-2)),   // đã hết hạn
        };
        var v = OfficeBuilder.Build(Input(rests: rests));
        var codex = v.Desks.Single(d => d.Key == "C");
        Assert.Equal((DeskState.Resting, "Nghỉ tới 16:19 25/10"), (codex.State, codex.StateText));
        Assert.Equal(DeskState.Working, v.Desks.Single(d => d.Key == "A").State);   // đang làm thì không hiện nghỉ
        Assert.Equal((2, 0, 1), (v.Working, v.Idle, v.Resting));
    }

    [Fact]
    public void Gate_desk_counts_review_and_shows_latest()
    {
        var gate = OfficeBuilder.Build(Input()).Desks.Single(d => d.Key == "gate");
        Assert.Equal(("Cổng kiểm tra + Review AI", DeskState.Review, "2 chờ review", "V4-10"), (gate.Name, gate.State, gate.StateText, gate.Task!.Id));

        var none = OfficeBuilder.Build(Input(new[] { Card("V5-01", "A", TaskState.Assigned) })).Desks.Single(d => d.Key == "gate");
        Assert.Equal((DeskState.Idle, "Không có việc chờ"), (none.State, none.StateText));
        Assert.Null(none.Task);
    }

    [Fact]
    public void Claude_desk_shows_current_request()
    {
        var req = new RequestThread("R-005", "Văn phòng mini", "🟡 Đang làm", "01:05", "…", Array.Empty<RequestTaskRef>(),
            new[] { new RequestEvent("01:05", "Nhận yêu cầu", ""), new RequestEvent("01:15", "PoC MCP chạy thật", ""), new RequestEvent("01:30", "Giao V5-01", "") },
            false, "R-005.md");
        var claude = OfficeBuilder.Build(Input(req: req)).Desks[0];
        Assert.Equal(("Claude", DeskState.Manager, "🟡 Đang làm"), (claude.Name, claude.State, claude.StateText));
        Assert.Equal(("", "R-005", "Văn phòng mini"), (claude.Task!.Project, claude.Task.Id, claude.Task.Title));
        Assert.Equal(new[] { "Giao V5-01", "PoC MCP chạy thật" }, claude.Feed.Select(f => f.Text));
        Assert.Equal(new[] { Now.Date.AddHours(1).AddMinutes(30), Now.Date.AddHours(1).AddMinutes(15) }, claude.Feed.Select(f => f.At));   // giờ của sự kiện, không phải Now
        Assert.All(claude.Feed, f => Assert.Equal(("Claude", "note"), (f.From, f.Kind)));

        var empty = OfficeBuilder.Build(Input()).Desks[0];
        Assert.Equal(("Không có phiếu đang làm", (DeskTask?)null), (empty.StateText, empty.Task));
    }

    [Fact]
    public void Research_desk_placeholder()
    {
        var r = OfficeBuilder.Build(Input()).Desks.Last();
        Assert.Equal(("research", "Phòng nghiên cứu", DeskState.Idle, "Sắp có (V5-07)"), (r.Key, r.Name, r.State, r.StateText));
    }

    [Theory]
    [InlineData("A-minh.md", "# Minh\n- **Tên:** Minh\n- **Chuyên môn:** Nền tảng: kiến trúc, build, đóng gói, CSDL, hạ tầng\n- **Chạy bằng:** Antigravity CLI (agy)", "A", "Minh", "agy")]
    [InlineData("C-codex.md", "- **Tên:** Codex\n- **Chuyên môn:** Dự phòng\n- **Chạy bằng:** Codex CLI (`aiteam run`)", "C", "Codex", "codex")]
    public void Parse_staff(string file, string md, string agent, string name, string provider)
    {
        var s = OfficeService.ParseStaff(file, md)!;
        Assert.Equal((agent, name, provider), (s.Agent, s.Name, s.Provider));
        Assert.True(s.Role.Length <= 40);
        Assert.Null(OfficeService.ParseStaff("X-y.md", "# không có tên"));
    }

    // ───────────── Trang ─────────────

    private sealed class FakeOffice(Func<OfficeView> get) : IOfficeService { public OfficeView Get() => get(); }

    [Fact]
    public void Page_loads_view_or_reports_error()
    {
        var v = OfficeBuilder.Build(Input());
        var p = new OfficeModel(new FakeOffice(() => v));
        p.OnGet();
        Assert.Same(v, p.View);
        Assert.Null(p.Error);

        var bad = new OfficeModel(new FakeOffice(() => throw new IOException("đĩa")));
        bad.OnGet();
        Assert.Null(bad.View);
        Assert.Equal("Không đọc được dữ liệu văn phòng: đĩa", bad.Error);
    }

    [Fact]
    public void Desks_handler_returns_partial()
    {
        var p = new OfficeModel(new FakeOffice(() => OfficeBuilder.Build(Input())));
        var r = Assert.IsType<PartialViewResult>(p.OnGetDesks());
        Assert.Equal("_OfficeDesks", r.ViewName);
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
        var page = Source(Path.Combine("hub", "Pages", "Office.cshtml"));
        Assert.Contains("@page \"/Office\"", page);
        Assert.Contains("hx-get=\"?handler=Desks\"", page);
        Assert.Contains("every 10s", page);
        Assert.Contains("_OfficeDesks", page);
        Assert.DoesNotContain("Html.Raw", page);
        var desks = Source(Path.Combine("hub", "Pages", "Shared", "_OfficeDesks.cshtml"));
        Assert.Contains("@model AITeam.Hub.Pages.OfficeModel", desks);
        Assert.Contains("/t/", desks);
        Assert.Contains("?tab=report", desks);
        Assert.DoesNotContain("Html.Raw", desks);
    }
}
