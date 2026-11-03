using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.DevData;

public static class FakeServices
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<ITeamConfigService, FakeTeamConfigService>();
        services.AddSingleton<ITaskBoardService, FakeTaskBoardService>();
        services.AddSingleton<IAgentRunService, FakeAgentRunService>();
        services.AddSingleton<IHealthService, FakeHealthService>();
        services.AddSingleton<IVaultService, FakeVaultService>();
        services.AddSingleton<IInboxService, FakeInboxService>();
        services.AddSingleton<IProcessStarter, NoopProcessStarter>(); // V5-05: dữ liệu giả ⇒ không chạy team.ps1 thật
        services.AddSingleton<IApprovalService, FakeApprovalService>();
        services.AddHubV3(); // _Layout cần IApprovalService
    }
}

public sealed class FakeTeamConfigService : ITeamConfigService
{
    private readonly List<ProjectInfo> _projects = new()
    {
        new ProjectInfo(
            Name: "AITeam",
            Repo: "HieuDV/AITeam",
            Path: @"D:\AITeam",
            WorkDir: @"D:\AITeam-work\AITeam",
            Stack: ".NET 10, ASP.NET Core, HTMX",
            Build: "dotnet build AITeam.sln",
            Test: "dotnet test AITeam.sln",
            Areas: new Dictionary<string, string>
            {
                ["A"] = "hub/Services/, hub/Models/",
                ["B"] = "hub/Pages/, hub/wwwroot/"
            },
            ExistsLocally: true),
        new ProjectInfo(
            Name: "HDVTool",
            Repo: "HieuDV/HDVTool",
            Path: @"D:\HDVTool",
            WorkDir: @"D:\AITeam-work\HDVTool",
            Stack: ".NET 9, WPF, WebView2",
            Build: "dotnet build HDVTool.sln",
            Test: "dotnet test HDVTool.sln",
            Areas: new Dictionary<string, string>
            {
                ["A"] = "src/Core/, src/Services/",
                ["B"] = "src/UI/, src/Modules/"
            },
            ExistsLocally: true),
        new ProjectInfo(
            Name: "ChinaAndroidToolbox",
            Repo: "HieuDV/ChinaAndroidToolbox",
            Path: @"D:\ChinaAndroidToolbox",
            WorkDir: @"D:\AITeam-work\ChinaAndroidToolbox",
            Stack: "C# .NET WinForms, ADB, SQLite",
            Build: "dotnet build ChinaAndroidToolbox.sln",
            Test: "dotnet test ChinaAndroidToolbox.sln",
            Areas: new Dictionary<string, string>
            {
                ["A"] = "Core, Adb, Device (hồ sơ), FixCenter (engine + actions), Data, build",
                ["B"] = "App/Views, Diagnostics (khảo sát), Backup, resources/debloat, resources/packs"
            },
            ExistsLocally: true)
    };

    public IReadOnlyList<ProjectInfo> GetProjects() => _projects;

    public ProjectInfo? FindProject(string name) =>
        _projects.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public TeamSettings GetSettings() => new(
        DefaultModel: "gemini-2.5-pro",
        MaxReviewRounds: 3,
        Developers: new List<DeveloperInfo>
        {
            new("A", "Agent A", "Kiến trúc hệ thống, Dịch vụ backend, Tích hợp công cụ"),
            new("B", "Agent B", "Giao diện người dùng, Razor Pages, CSS, Nghiệp vụ màn hình")
        },
        ProjectsRoot: @"D:\",
        WorkRoot: @"D:\AITeam-work",
        GitHubOwner: "HieuDV",
        AgyAccounts: new List<string> { "Chính (hieudv.work@gmail.com)", "Phụ (hieudv.dev@gmail.com)" },
        AgyExe: @"C:\Users\ADMIN\.gemini\antigravity-cli\agy.exe");

    public void SaveSettings(TeamSettings settings) { }

    public void SaveProject(ProjectInfo project) { }
}

public sealed class FakeTaskBoardService : ITaskBoardService
{
    private static readonly DateTime Now = DateTime.Now;

    public ProjectBoard GetBoard(string project)
    {
        var config = new FakeTeamConfigService();
        var proj = config.FindProject(project) ?? config.GetProjects().First();

        var tasks = new List<TaskCard>
        {
            new(
                Project: proj.Name,
                Id: "H-001",
                Title: "Khung hub/ và toàn bộ Services/ + Models/",
                Agent: "A",
                Branch: "agent-a/H-001-hub-services",
                State: TaskState.Assigned,
                FileName: "H-001-hub-services.md",
                Runs: 1,
                LastRun: new AgentRun(
                    Project: proj.Name,
                    Task: "H-001",
                    Attempt: 1,
                    State: RunState.Running,
                    Status: null,
                    Error: null,
                    Response: null,
                    DurationSeconds: null,
                    TotalTokens: null,
                    Time: Now.AddMinutes(-12),
                    LogPath: @"D:\AITeam-work\AITeam\logs\H-001-1.json")),
            new(
                Project: proj.Name,
                Id: "H-002",
                Title: "Giao diện khung + Tổng quan / Dự án / Task",
                Agent: "B",
                Branch: "agent-b/H-002-hub-ui",
                State: TaskState.Review,
                FileName: "H-002-hub-ui.md",
                Runs: 2,
                LastRun: new AgentRun(
                    Project: proj.Name,
                    Task: "H-002",
                    Attempt: 2,
                    State: RunState.Success,
                    Status: "SUCCESS",
                    Error: null,
                    Response: "Đã hoàn thành toàn bộ giao diện cho 3 trang và bộ MarkdownRenderer.",
                    DurationSeconds: 45.2,
                    TotalTokens: 14200,
                    Time: Now.AddMinutes(-30),
                    LogPath: @"D:\AITeam-work\AITeam\logs\H-002-2.json")),
            new(
                Project: proj.Name,
                Id: "H-003",
                Title: "Nối giao diện với dịch vụ thật + Agent, Hộp thư",
                Agent: "B",
                Branch: "agent-b/H-003-hub-connect",
                State: TaskState.NeedsFix,
                FileName: "H-003-hub-connect.md",
                Runs: 3,
                LastRun: new AgentRun(
                    Project: proj.Name,
                    Task: "H-003",
                    Attempt: 3,
                    State: RunState.Error,
                    Status: "ERROR",
                    Error: "Lỗi kết nối IPC: Tiến trình dịch vụ phản hồi timeout sau 120s.",
                    Response: null,
                    DurationSeconds: 120.0,
                    TotalTokens: 8500,
                    Time: Now.AddHours(-2),
                    LogPath: @"D:\AITeam-work\AITeam\logs\H-003-3.json")),
            new(
                Project: proj.Name,
                Id: "H-000",
                Title: "Khởi tạo repo và tài liệu kiến trúc Hub",
                Agent: "A",
                Branch: "agent-a/H-000-init",
                State: TaskState.Merged,
                FileName: "H-000-init.md",
                Runs: 1,
                LastRun: new AgentRun(
                    Project: proj.Name,
                    Task: "H-000",
                    Attempt: 1,
                    State: RunState.Success,
                    Status: "SUCCESS",
                    Error: null,
                    Response: "Khởi tạo thành công cấu trúc dự án và tài liệu HUB-DESIGN.md.",
                    DurationSeconds: 18.5,
                    TotalTokens: 6400,
                    Time: Now.AddDays(-1),
                    LogPath: @"D:\AITeam-work\AITeam\logs\H-000-1.json"))
        };

        var planMarkdown = """
            ## Kế hoạch triển khai AITeam Hub
            - [x] **Giai đoạn 1**: Khung nền tảng, dịch vụ đọc file, giao diện Tổng quan / Dự án / Task.
            - [ ] **Giai đoạn 2**: Nối dịch vụ thật, màn hình Agent, Hộp thư, Cài đặt, Tài khoản, Bí mật.
            - [ ] **Giai đoạn 3**: Tích hợp WebView2 vào HDV Tool.

            ### Quy tắc phối hợp
            1. Agent A và Agent B thực hiện độc lập theo hợp đồng `Contracts.cs`.
            2. Toàn bộ thay đổi phải có unit test và build 0 warning.
            """;

        return new ProjectBoard(proj, tasks, planMarkdown);
    }

    public TaskDetail? GetTask(string project, string taskId)
    {
        var board = GetBoard(project);
        var card = board.Tasks.FirstOrDefault(t => string.Equals(t.Id, taskId, StringComparison.OrdinalIgnoreCase));
        if (card == null)
        {
            // Trả về task giả định nếu ID không có trong danh sách
            card = new TaskCard(
                Project: project,
                Id: taskId,
                Title: $"Task mẫu {taskId}",
                Agent: "B",
                Branch: $"agent-b/{taskId}-feature",
                State: TaskState.Review,
                FileName: $"{taskId}-feature.md",
                Runs: 1,
                LastRun: null);
        }

        var sections = new List<TaskSection>
        {
            new("Bối cảnh", """
                Thiết kế đầy đủ nằm ở `docs/HUB-DESIGN.md`, đọc kỹ mục 2 và 4.
                Claude đã dựng khung và hợp đồng dữ liệu:
                - Model: `hub/Models/HubModels.cs`
                - Interface: `hub/Services/Contracts.cs`
                """),
            new("Mục tiêu", """
                Xây dựng giao diện đẹp, gọn, hỗ trợ đầy đủ tiếng Việt có dấu.
                Hỗ trợ chế độ sáng / tối tự động chuyển đổi qua nút bấm và lưu vào localStorage.
                """),
            new("Phạm vi", """
                - `hub/Pages/`: Razor Pages và PageModels.
                - `hub/wwwroot/`: hub.css và thư viện htmx.min.js cục bộ.
                - `hub/DevData/`: Dữ liệu giả định phục vụ phát triển giao diện độc lập.
                """),
            new("Báo cáo của agent", """
                - Đã hoàn thành bộ chuyển đổi Markdown sang HTML chống XSS.
                - Đã xây dựng 3 màn hình: Tổng quan, Dự án (Kanban 4 cột), và Chi tiết Task.
                - Đã kiểm tra build thành công 0 warning, 100% unit tests pass.
                """),
            new("Review (Claude)", """
                Giao diện đáp ứng đúng thiết kế `HUB-DESIGN.md`, phong cách gọn gàng, bố cục rõ ràng.
                """)
        };

        var criteria = new List<ChecklistItem>
        {
            new("`dotnet build AITeam.sln` 0 warning, `dotnet test AITeam.sln` pass", true),
            new("Chạy thử `HUB_FAKE=1 dotnet run --project hub`, mở http://127.0.0.1:5057", true),
            new("Không gọi mạng lúc chạy (htmx là file cục bộ)", true)
        };

        var runs = new List<AgentRun>
        {
            new(
                Project: project,
                Task: taskId,
                Attempt: 2,
                State: RunState.Success,
                Status: "SUCCESS",
                Error: null,
                Response: """
                    ### Kết quả thực hiện lần 2
                    - Đã bổ sung bộ chuyển đổi Markdown thuần C# không phụ thuộc bên ngoài.
                    - Thêm các kiểm tra bảo mật chống XSS cho thẻ script và img onerror.
                    - Build 0 warning, 0 error.
                    """,
                DurationSeconds: 45.2,
                TotalTokens: 14200,
                Time: Now.AddMinutes(-30),
                LogPath: $@"D:\AITeam-work\{project}\logs\{taskId}-2.json"),
            new(
                Project: project,
                Task: taskId,
                Attempt: 1,
                State: RunState.Error,
                Status: "ERROR",
                Error: "Bộ chuyển đổi Markdown gặp cảnh báo biên dịch CS0219.",
                Response: "Đã dừng lại để kiểm tra và sửa cảnh báo.",
                DurationSeconds: 15.1,
                TotalTokens: 5300,
                Time: Now.AddHours(-1),
                LogPath: $@"D:\AITeam-work\{project}\logs\{taskId}-1.json")
        };

        return new TaskDetail(card, sections, criteria, runs);
    }
}

public sealed class FakeAgentRunService : IAgentRunService
{
    private static readonly DateTime Now = DateTime.Now;

    public int? LastStoppedPid { get; private set; }

    public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null)
    {
        var board = new FakeTaskBoardService().GetBoard(project);
        var runs = new List<AgentRun>();
        foreach (var task in board.Tasks)
        {
            if (taskId != null && !string.Equals(task.Id, taskId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (task.LastRun != null)
                runs.Add(task.LastRun);
        }
        return runs;
    }

    public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20)
    {
        return new FakeTaskBoardService().GetBoard("AITeam").Tasks
            .Where(t => t.LastRun != null)
            .Select(t => t.LastRun!)
            .Take(count)
            .ToList();
    }

    public IReadOnlyList<RunningAgent> GetRunning()
    {
        return new List<RunningAgent>
        {
            new(
                Pid: 14280,
                StartedAt: Now.AddMinutes(-12).AddSeconds(-25),
                Project: "AITeam",
                Task: "H-001",
                CommandLine: @"C:\Users\ADMIN\.gemini\antigravity-cli\agy.exe --prompt D:\AITeam-work\AITeam\logs\H-001-1.prompt.md")
        };
    }

    public bool Stop(int pid)
    {
        LastStoppedPid = pid;
        return true;
    }
}

public sealed class FakeHealthService : IHealthService
{
    public IReadOnlyList<HealthItem> Check()
    {
        return new List<HealthItem>
        {
            new("Git", HealthLevel.Ok, "Phiên bản 2.44.0.windows.1", null),
            new("GitHub CLI (gh)", HealthLevel.Ok, "Đã đăng nhập HieuDV (gh auth status)", null),
            new("Antigravity CLI (agy)", HealthLevel.Ok, "v2.1.0 sẵn sàng tại %USERPROFILE%\\.gemini\\antigravity-cli\\agy.exe", null),
            new("Claude Skill AITeam", HealthLevel.Ok, "Đã cài đặt tại ~/.claude/skills/aiteam/SKILL.md", null),
            new("Claude Config", HealthLevel.Warning, "Chưa tìm thấy khối AITeam trong ~/.claude/CLAUDE.md", "team.ps1 setup")
        };
    }

    public Task<HealthItem> CheckAgyLoginAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new HealthItem("Antigravity Đăng nhập", HealthLevel.Ok, "Ping phản hồi OK (0.8s)", null));
    }

    public void OpenSwitchAccountWindow() { }

    public void InstallClaudeIntegration() { }
}

public sealed class FakeVaultService : IVaultService
{
    private readonly List<string> _names = new() { "GITHUB_TOKEN", "GEMINI_API_KEY", "CLAUDE_COOKIE" };

    public IReadOnlyList<string> ListNames() => _names;

    public void Set(string name, string value)
    {
        if (!_names.Contains(name)) _names.Add(name);
    }

    public bool Remove(string name) => _names.Remove(name);

    public void ExportEnv(string project, IReadOnlyList<string> names) { }
}

public sealed class FakeInboxService : IInboxService
{
    private readonly List<InboxItem> _items = new()
    {
        new(1, DateTime.Now.AddHours(-3), "AITeam", "Thêm hỗ trợ xem log chi tiết của từng tiến trình agy", false),
        new(2, DateTime.Now.AddDays(-1), "HDVTool", "Cập nhật tài liệu hướng dẫn phím tắt cho module Hub", true)
    };

    public IReadOnlyList<InboxItem> GetItems() => _items;

    public void Add(string project, string text)
    {
        _items.Add(new InboxItem(_items.Count + 1, DateTime.Now, project, text, false));
    }
}
