using System.Text.Json.Nodes;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Test nghiệm thu F1-01 (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-01.
/// Trang Dự án chỉ liệt kê 3 dòng, không thêm / sửa / lưu trữ được ⇒ dịch vụ quản lý dự án.</summary>
public sealed class AcceptanceF101ProjectAdminTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly FakeRunner _runner = new();
    private readonly string _alphaPath;

    private sealed class FakeRunner : IProjectScriptRunner
    {
        public List<IReadOnlyList<string>> Calls { get; } = new();
        public int ExitCode { get; set; }
        public string Output { get; set; } = "Đã tạo\n";
        public (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout)
        {
            Calls.Add(args.ToList());
            return (ExitCode, Output);
        }
    }

    // projects.json THẬT rút gọn (04/11/2026) + 1 trường lạ "private" phải được giữ.
    private const string ProjectsJson = """
        [
            {
                "name":  "AITeam",
                "repo":  "vanhieu2000/AITeam",
                "path":  "D:\\AITeam",
                "workDir":  "D:\\AITeam-work\\AITeam",
                "stack":  "ASP.NET Core .NET 10 Razor Pages + HTMX (AITeam Hub)",
                "build":  "dotnet build AITeam.sln",
                "test":  "dotnet test AITeam.sln",
                "private":  true,
                "areas":  { "A":  "hub/Services/", "B":  "hub/Pages/" }
            },
            {
                "name":  "HDVTool",
                "repo":  "vanhieu2000/HDVTool",
                "path":  "Z:\\Khac\\HDVTool",
                "description":  "Công cụ SCADA",
                "status":  "paused",
                "branch":  "master",
                "try":  "CHAY-THU.cmd",
                "defaultAi":  "codex",
                "areas":  {}
            }
        ]
        """;

    public AcceptanceF101ProjectAdminTests()
    {
        _alphaPath = Path.Combine(_env.RootDir, "AITeamLocal");
        Directory.CreateDirectory(_alphaPath);
        File.WriteAllText(_env.Paths.ProjectsJson, ProjectsJson);
        File.WriteAllText(_env.Paths.TeamLocalJson, """
            { "projectsRoot": "D:\\HIEUDV", "paths": { "HDVTool": "Z:\\Khong\\HDVTool" }, "workDirs": { "AITeam": "D:\\W\\AITeam" } }
            """);
    }

    public void Dispose() => _env.Dispose();

    private ProjectAdminService Service() => new(_env.Paths, new TeamConfigService(_env.Paths), _runner);

    private static ProjectEdit Edit(string localPath = "", string branch = "main", string build = "dotnet build AITeam.sln",
        string description = "Văn phòng AI", string defaultAi = "codex", Dictionary<string, string>? areas = null)
        => new(description, branch, "ASP.NET Core .NET 10", build, "dotnet test AITeam.sln", "CHAY-HUB.cmd", defaultAi,
            areas ?? new Dictionary<string, string> { ["A"] = "hub/Services/", ["B"] = "hub/Pages/" }, localPath);

    private JsonObject Project(string name)
        => JsonNode.Parse(File.ReadAllText(_env.Paths.ProjectsJson))!.AsArray().OfType<JsonObject>()
            .Single(o => (string?)o["name"] == name);

    [Fact]
    public void List_reads_new_fields_with_defaults()
    {
        var list = Service().List();
        Assert.Equal(new[] { "AITeam", "HDVTool" }, list.Select(p => p.Name));

        var a = list[0];
        Assert.Equal(ProjectStatus.Active, a.Status);
        Assert.Equal("main", a.Branch);
        Assert.Equal("", a.Description);
        Assert.Equal("", a.DefaultAi);
        Assert.Equal("vanhieu2000/AITeam", a.Repo);
        Assert.Equal("dotnet build AITeam.sln", a.Build);
        Assert.Equal("hub/Pages/", a.Areas["B"]);

        var h = Service().Get("hdvtool")!;
        Assert.Equal(ProjectStatus.Paused, h.Status);
        Assert.Equal("Công cụ SCADA", h.Description);
        Assert.Equal("master", h.Branch);
        Assert.Equal("CHAY-THU.cmd", h.Try);
        Assert.Equal("codex", h.DefaultAi);
        Assert.Equal("Z:\\Khong\\HDVTool", h.LocalPath);
        Assert.Null(Service().Get("KhongCo"));
    }

    [Fact]
    public void Update_writes_projects_json_keeps_unknown_fields_and_puts_local_path_in_team_local()
    {
        var r = Service().Update("AITeam", Edit(localPath: _alphaPath));
        Assert.True(r.Ok, r.Error);

        var p = Project("AITeam");
        Assert.Equal("Văn phòng AI", (string?)p["description"]);
        Assert.Equal("main", (string?)p["branch"]);
        Assert.Equal("CHAY-HUB.cmd", (string?)p["try"]);
        Assert.Equal("codex", (string?)p["defaultAi"]);
        Assert.Equal("ASP.NET Core .NET 10", (string?)p["stack"]);
        Assert.True((bool)p["private"]!);
        // Đường dẫn máy KHÔNG ghi vào projects.json
        Assert.Equal("D:\\AITeam", (string?)p["path"]);
        Assert.Equal("D:\\AITeam-work\\AITeam", (string?)p["workDir"]);
        Assert.Equal("Công cụ SCADA", (string?)Project("HDVTool")["description"]);

        var local = JsonNode.Parse(File.ReadAllText(_env.Paths.TeamLocalJson))!.AsObject();
        Assert.Equal(_alphaPath, (string?)local["paths"]!["AITeam"]);
        Assert.Equal("Z:\\Khong\\HDVTool", (string?)local["paths"]!["HDVTool"]);
        Assert.Equal("D:\\W\\AITeam", (string?)local["workDirs"]!["AITeam"]);
        Assert.Equal("D:\\HIEUDV", (string?)local["projectsRoot"]);
        Assert.True(File.Exists(_env.Paths.ProjectsJson + ".bak"));

        Assert.Equal(_alphaPath, Service().Get("AITeam")!.LocalPath);
        Assert.True(Service().Get("AITeam")!.ExistsLocally);
    }

    [Fact]
    public void Update_with_empty_local_path_leaves_team_local_untouched()
    {
        var before = File.ReadAllText(_env.Paths.TeamLocalJson);
        Assert.True(Service().Update("AITeam", Edit()).Ok);
        Assert.Equal(before, File.ReadAllText(_env.Paths.TeamLocalJson));
    }

    private static readonly ProjectEdit[] BadEdits =
    [
        Edit(branch: "main; rm -rf"),
        Edit(branch: ""),
        Edit(build: "dotnet build\nshutdown /s"),
        Edit(build: new string('x', 301)),
        Edit(description: new string('d', 201)),
        Edit(description: "hai\ndòng"),
        Edit(defaultAi: "gpt"),
        Edit(localPath: "tuong-doi\\thu-muc"),
        Edit(localPath: "D:\\HIEUDV\\..\\Windows"),
        Edit(localPath: "D:\\" + new string('p', 250)),
        Edit(areas: new Dictionary<string, string> { ["a"] = "x" }),
        Edit(areas: new Dictionary<string, string> { ["AB"] = "x" }),
        Edit(areas: new Dictionary<string, string> { ["A"] = "x\ny" }),
    ];

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)]
    public void Update_rejects_invalid_input_without_writing(int index)
    {
        var edit = BadEdits[index];
        var projects = File.ReadAllText(_env.Paths.ProjectsJson);
        var local = File.ReadAllText(_env.Paths.TeamLocalJson);
        var r = Service().Update("AITeam", edit);
        Assert.False(r.Ok);
        Assert.False(string.IsNullOrWhiteSpace(r.Error));
        Assert.Equal(projects, File.ReadAllText(_env.Paths.ProjectsJson));
        Assert.Equal(local, File.ReadAllText(_env.Paths.TeamLocalJson));
    }

    [Fact]
    public void Update_unknown_project_fails()
        => Assert.False(Service().Update("KhongCo", Edit()).Ok);

    [Fact]
    public void SetStatus_changes_only_status()
    {
        Assert.True(Service().SetStatus("AITeam", ProjectStatus.Archived).Ok);
        var p = Project("AITeam");
        Assert.Equal("archived", (string?)p["status"]);
        Assert.Equal("dotnet build AITeam.sln", (string?)p["build"]);
        Assert.Equal(ProjectStatus.Archived, Service().Get("AITeam")!.Status);

        Assert.True(Service().SetStatus("AITeam", ProjectStatus.Active).Ok);
        Assert.Equal("active", (string?)Project("AITeam")["status"]);
        Assert.False(Service().SetStatus("KhongCo", ProjectStatus.Paused).Ok);
    }

    [Fact]
    public void Add_builds_argument_list_per_mode_never_a_command_string()
    {
        var s = Service();
        Assert.True(s.Add(new ProjectAdd("MoiTinh", ProjectAddMode.New, null, null)).Ok);
        Assert.True(s.Add(new ProjectAdd("TuGitHub", ProjectAddMode.GitHub, "vanhieu2000/TuGitHub", null)).Ok);
        Assert.True(s.Add(new ProjectAdd("TuThuMuc", ProjectAddMode.Folder, null, "D:\\Du An\\TuThuMuc")).Ok);

        Assert.Equal(new[] { "add-project", "-Project", "MoiTinh", "-New" }, _runner.Calls[0]);
        Assert.Equal(new[] { "add-project", "-Project", "TuGitHub", "-Repo", "vanhieu2000/TuGitHub" }, _runner.Calls[1]);
        Assert.Equal(new[] { "add-project", "-Project", "TuThuMuc", "-Path", "D:\\Du An\\TuThuMuc" }, _runner.Calls[2]);
    }

    private static readonly ProjectAdd[] BadAdds =
    [
        new ProjectAdd("AITeam", ProjectAddMode.New, null, null),                 // đã có
        new ProjectAdd("aiteam", ProjectAddMode.New, null, null),                 // đã có (không phân biệt hoa thường)
        new ProjectAdd("Co Dau Cach", ProjectAddMode.New, null, null),
        new ProjectAdd("1BatDauSo", ProjectAddMode.New, null, null),
        new ProjectAdd("-Force", ProjectAddMode.New, null, null),
        new ProjectAdd("", ProjectAddMode.New, null, null),
        new ProjectAdd("X", ProjectAddMode.GitHub, null, null),
        new ProjectAdd("X", ProjectAddMode.GitHub, "vanhieu2000/X; rm -rf /", null),
        new ProjectAdd("X", ProjectAddMode.GitHub, "https://github.com/a/b", null),
        new ProjectAdd("X", ProjectAddMode.Folder, null, null),
        new ProjectAdd("X", ProjectAddMode.Folder, null, "relative\\x"),
        new ProjectAdd("X", ProjectAddMode.Folder, null, "D:\\a\\..\\..\\Windows"),
    ];

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public void Add_rejects_invalid_input_without_running_script(int index)
    {
        var add = BadAdds[index];
        var r = Service().Add(add);
        Assert.False(r.Ok);
        Assert.False(string.IsNullOrWhiteSpace(r.Error));
        Assert.Empty(_runner.Calls);
    }

    [Fact]
    public void Add_script_failure_reports_last_output_line()
    {
        _runner.ExitCode = 1;
        _runner.Output = "Clone vanhieu2000/X → D:\\HIEUDV\\X\r\nLỖI: Clone vanhieu2000/X thất bại.\r\n\r\n";
        var r = Service().Add(new ProjectAdd("X", ProjectAddMode.GitHub, "vanhieu2000/X", null));
        Assert.False(r.Ok);
        Assert.Equal("Thêm dự án thất bại: LỖI: Clone vanhieu2000/X thất bại.", r.Error);
    }

    [Fact]
    public void Services_registered_in_di_and_real_runner_uses_argument_list()
    {
        var root = FindRoot();
        var reg = File.ReadAllText(Path.Combine(root, "hub", "Services", "HubV3Registration.cs"));
        Assert.Contains("IProjectAdminService", reg);
        Assert.Contains("IProjectScriptRunner", reg);
        var src = File.ReadAllText(Path.Combine(root, "hub", "Services", "ProjectAdminService.cs"));
        Assert.Contains("ArgumentList", src);
        Assert.DoesNotContain(".Arguments =", src);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("AITeam.sln");
    }
}
