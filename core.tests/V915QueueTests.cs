using System.Text;
using AITeam.Core.Queue;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

/// <summary>Nghiệm thu V9-15: khóa phạm vi file (.state/locks.json) và hàng đợi task theo phụ thuộc.</summary>
public sealed class V915QueueTests
{
    [Fact]
    public void Locks_on_the_same_file_do_not_share()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.True(store.TryAcquire("A-1", "agy", ["core/A.cs"], DateTime.Now, null, out var holder));
        Assert.Null(holder);
        Assert.False(store.TryAcquire("B-1", "codex", ["core/A.cs"], DateTime.Now, null, out holder));
        Assert.Equal("A-1", holder);
        Assert.Equal("A-1", Assert.Single(store.All()).Task);
    }

    [Fact]
    public void Directory_scope_covers_files_inside_it()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.True(store.TryAcquire("A-1", "agy", ["core/Queue"], DateTime.Now, null, out _));
        Assert.False(store.TryAcquire("B-1", "codex", ["core/Queue/TaskQueue.cs"], DateTime.Now, null, out var holder));
        Assert.Equal("A-1", holder);
    }

    [Fact]
    public void Same_task_can_refresh_its_lock_but_other_tasks_wait()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.True(store.TryAcquire("A-1", "agy", ["a.cs"], DateTime.Now, null, out _));
        Assert.True(store.TryAcquire("A-1", "codex", ["b.cs"], DateTime.Now, null, out _));
        Assert.True(store.TryAcquire("B-1", "agy", ["a.cs"], DateTime.Now, null, out _));
        var lockA = Assert.Single(store.All(), l => l.Task == "A-1");
        Assert.Equal("codex", lockA.Provider);
        Assert.Equal(["b.cs"], lockA.Scope);
    }

    [Fact]
    public void Finished_task_releases_scope_for_the_next_one()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.True(store.TryAcquire("A-1", "agy", ["a.cs"], DateTime.Now, null, out _));
        Assert.False(store.TryAcquire("B-1", "codex", ["a.cs"], DateTime.Now, null, out _));
        Assert.True(store.TryAcquire("B-1", "codex", ["a.cs"], DateTime.Now, task => task == "A-1", out _));
        Assert.Equal("B-1", Assert.Single(store.All()).Task);
    }

    [Fact]
    public void Corrupt_lock_file_is_backed_up_byte_for_byte()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);
        var broken = new byte[] { 0x5B, 0x7B, 0xFF, 0xFE, 0x00, 0x7D };
        File.WriteAllBytes(store.FilePath, broken);

        Assert.Empty(store.All());

        var backup = Assert.Single(Directory.GetFiles(Path.GetDirectoryName(store.FilePath)!, "locks.json.hong-*"));
        Assert.Equal(broken, File.ReadAllBytes(backup));
        Assert.False(File.Exists(store.FilePath));
    }

    [Theory]
    [InlineData("../escape.cs")]
    [InlineData("D:/x.cs")]
    [InlineData("/etc/passwd")]
    [InlineData("./a.cs")]
    public void Unsafe_scope_paths_are_rejected(string path)
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.Throws<ArgumentException>(() => store.TryAcquire("A-1", "agy", [path], DateTime.Now, null, out _));
        Assert.False(File.Exists(store.FilePath));
    }

    [Fact]
    public void Release_removes_only_the_named_task()
    {
        using var repo = new TempRepo(false);
        var store = new ScopeLockStore(repo.Root);
        Assert.True(store.TryAcquire("A-1", "agy", ["a.cs"], DateTime.Now, null, out _));
        Assert.True(store.TryAcquire("B-1", "codex", ["b.cs"], DateTime.Now, null, out _));
        Assert.True(store.Release("A-1"));
        Assert.False(store.Release("A-1"));
        Assert.Equal("B-1", Assert.Single(store.All()).Task);
    }

    [Fact]
    public void Board_rows_and_scope_paths_are_parsed_from_markdown()
    {
        const string board = "| Việc | Agent | Nhánh | Trạng thái |\n" +
                             "| [A-1 Việc A](A-1-viec-a.md) | C | `b-a` | ✅ Đã merge |\n" +
                             "| [B-2 Việc B](B-2-viec-b.md) | O | `b-b` | 🟣 Chờ làm |\n";
        var rows = TaskQueue.ParseBoard(board);
        Assert.Equal(2, rows.Count);
        Assert.Equal("A-1", rows[0].Id);
        Assert.True(TaskQueue.IsMerged(rows[0].Status));
        Assert.True(TaskQueue.IsPending(rows[1].Status));
        Assert.Equal("B-2-viec-b.md", rows[1].FileName);

        const string text = "## Phạm vi\n- `core/A.cs` (sửa)\n- `core/Queue/`\n## Không làm\n- `khac.cs`\n";
        Assert.Equal(["core/A.cs", "core/Queue/"], TaskQueue.ScopePaths(text));
        Assert.Equal(["A-1", "B-2"], TaskQueue.Dependencies("- **Phụ thuộc:** A-1, `B-2`\n"));
    }

    [Fact]
    public void Dependency_is_ready_only_after_it_is_merged()
    {
        var tasks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A-1-viec-a.md"] = "## Phạm vi\n- `a.cs`\n",
            ["B-2-viec-b.md"] = "## Phạm vi\n- `b.cs`\n- **Phụ thuộc:** A-1\n",
        };
        string? Read(string name) => tasks.TryGetValue(name, out var text) ? text : null;
        string Board(string statusA) =>
            "| Việc | Agent | Nhánh | Trạng thái |\n" +
            $"| [A-1 Việc A](A-1-viec-a.md) | C | `b-a` | {statusA} |\n" +
            "| [B-2 Việc B](B-2-viec-b.md) | O | `b-b` | 🟣 Chờ làm |\n";

        var waiting = Assert.Single(TaskQueue.Evaluate(TaskQueue.ParseBoard(Board("🟡 Đang chạy")), Read), t => t.Id == "B-2");
        Assert.False(waiting.Ready);
        Assert.Contains("A-1", waiting.Reason);

        var ready = Assert.Single(TaskQueue.Evaluate(TaskQueue.ParseBoard(Board("✅ Đã merge")), Read), t => t.Id == "B-2");
        Assert.True(ready.Ready);
        Assert.Equal(["A-1"], ready.DependsOn);
    }

    [Fact]
    public async Task PowerShell_queue_reads_board_and_dependencies()
    {
        using var sandbox = new TeamScript();
        var root = sandbox.Root;
        Directory.CreateDirectory(Path.Combine(root, "tasks"));
        File.WriteAllText(Path.Combine(root, "tasks", "README.md"),
            "| Việc | Agent | Nhánh | Trạng thái |\r\n" +
            "| [A-1 Việc A](A-1-viec-a.md) | C | `b-a` | ✅ Đã merge |\r\n" +
            "| [B-2 Việc B](B-2-viec-b.md) | O | `b-b` | 🟣 Chờ làm |\r\n" +
            "| [C-3 Việc C](C-3-viec-c.md) | A | `b-c` | 🟣 Chờ làm |\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "tasks", "A-1-viec-a.md"), "## Phạm vi\r\n- `a.cs`\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "tasks", "B-2-viec-b.md"), "## Phạm vi\r\n- `b.cs`\r\n- **Phụ thuộc:** A-1\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "tasks", "C-3-viec-c.md"), "## Phạm vi\r\n- `c.cs`\r\n- **Phụ thuộc:** B-2\r\n", new UTF8Encoding(false));
        var result = await sandbox.RunAsync("team.ps1", ["Get-QueueReadyTasks", "Test-QueuePending"], $$"""
            $r = [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = 'P' } }
            $items = @(Get-QueueReadyTasks $r)
            "COUNT=$($items.Count)"
            "READY=" + ((@($items | Where-Object { $_.Ready }) | ForEach-Object { $_.Id }) -join ',')
            "WAIT=" + ((@($items | Where-Object { -not $_.Ready }) | ForEach-Object { $_.Id + ':' + $_.Reason }) -join ';')
            """);
        Assert.Equal(0, result.Process.Code);
        Assert.Contains("COUNT=2", result.Process.Output);
        Assert.Contains("READY=B-2", result.Process.Output);
        Assert.Contains("WAIT=C-3:Chờ phụ thuộc: B-2", result.Process.Output);
    }

    [Fact]
    public async Task PowerShell_auto_queue_exits_when_nothing_is_ready()
    {
        using var sandbox = new TeamScript();
        var result = await sandbox.RunAsync("team.ps1", ["Invoke-AutoQueue"], """
            $Project = 'P'
            $Max = 2
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root } }
            function Get-AITeamExe { 'C:\fake\aiteam.exe' }
            function Get-QueueReadyTasks($r) { @() }
            function Get-AssignProcesses($r) { @() }
            function Say($text, $color) { $text }
            Invoke-AutoQueue
            """);
        Assert.Equal(0, result.Process.Code);
        Assert.Contains("không còn task chờ làm", result.Process.Output);
        Assert.False(File.Exists(Path.Combine(sandbox.Root, ".state", "auto.pid")));
    }

    [Fact]
    public async Task PowerShell_assign_waits_when_scope_is_locked()
    {
        using var sandbox = new TeamScript();
        var root = sandbox.Root;
        Directory.CreateDirectory(Path.Combine(root, "tasks"));
        Directory.CreateDirectory(Path.Combine(root, ".state"));
        File.WriteAllText(Path.Combine(root, "tasks", "B-2-viec-b.md"),
            "## Phạm vi\r\n- `core/a.cs`\r\n- **Agent:** A\r\n- **Nhánh:** `agent-a/B-2`\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, ".state", "locks.json"),
            """[{"task":"A-1","provider":"codex","at":"2026-01-01T00:00:00","scope":["core/a.cs"]}]""",
            new UTF8Encoding(false));
        var result = await sandbox.RunAsync("team.ps1", ["Invoke-Assign"], """
            $Project = 'P'; $Task = 'B-2'; $Resume = $true; $Dev = 'agy'
            function Get-Config { [pscustomobject]@{ developers = @(); agy = [pscustomobject]@{ exe = 'agy'; model = '' }; github = [pscustomobject]@{ owner = 'x' } } }
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root; Work = $Root } }
            function Find-TaskFile($r, $id) { Get-Item (Join-Path $Root "tasks/$id-viec-b.md") }
            function Stop-ReviewReassignment { $false }
            function Assert-NotClaimedElsewhere { }
            function Get-ScopeOverlaps { @() }
            function Set-BoardStatus { }
            function Say($text, $color) { $text }
            try { Invoke-Assign } catch { $_.Exception.Message }
            """);
        Assert.Equal(0, result.Process.Code);
        Assert.Contains("Chờ khóa phạm vi: A-1 đang giữ.", result.Process.Output);
        Assert.Contains("Phạm vi task đang bị khóa bởi A-1.", result.Process.Output);
        Assert.Contains("\"task\":\"A-1\"", File.ReadAllText(Path.Combine(sandbox.Root, ".state", "locks.json")));
    }

    [Fact]
    public async Task PowerShell_assign_releases_lock_when_later_step_fails()
    {
        using var sandbox = new TeamScript();
        var root = sandbox.Root;
        Directory.CreateDirectory(Path.Combine(root, "tasks"));
        File.WriteAllText(Path.Combine(root, "tasks", "B-2-viec-b.md"),
            "## Phạm vi\r\n- `core/b.cs`\r\n- **Agent:** A\r\n- **Nhánh:** `agent-a/B-2`\r\n", new UTF8Encoding(false));
        var result = await sandbox.RunAsync("team.ps1", ["Invoke-Assign"], """
            $Project = 'P'; $Task = 'B-2'; $Resume = $true; $Dev = 'agy'
            function Get-Config { [pscustomobject]@{ developers = @(); agy = [pscustomobject]@{ exe = 'agy'; model = '' }; github = [pscustomobject]@{ owner = 'x' } } }
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root; Work = $Root } }
            function Find-TaskFile($r, $id) { Get-Item (Join-Path $Root "tasks/$id-viec-b.md") }
            function Stop-ReviewReassignment { $false }
            function Assert-NotClaimedElsewhere { }
            function Get-ScopeOverlaps { @() }
            function Get-ClaimText { 'tạm giữ' }
            function Set-BoardStatus { throw 'dừng thử sau khi đã lấy khóa' }
            function Say($text, $color) { $text }
            try { Invoke-Assign } catch { $_.Exception.Message }
            """);
        Assert.Equal(0, result.Process.Code);
        Assert.Contains("dừng thử sau khi đã lấy khóa", result.Process.Output);
        Assert.False(File.Exists(Path.Combine(sandbox.Root, ".state", "locks.json")));
    }

    [Fact]
    public async Task PowerShell_stop_all_sets_stop_flag_for_auto_queue()
    {
        using var sandbox = new TeamScript();
        var result = await sandbox.RunAsync("team.ps1", ["Invoke-Stop", "Stop-TaskProcesses", "Get-AssignProcesses"], """
            $Project = 'P'
            $All = $true
            $Task = $null
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root } }
            function Get-CimInstance { @() }
            function Say($text, $color) { $text }
            $stateDir = Join-Path $Root '.state'
            $null = New-Item -ItemType Directory -Force $stateDir
            [IO.File]::WriteAllText((Join-Path $stateDir 'auto.pid'), '0')
            Invoke-Stop
            """);
        Assert.Equal(0, result.Process.Code);
        var stopFile = Path.Combine(sandbox.Root, ".state", "auto.stop");
        Assert.True(File.Exists(stopFile));
        Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(stopFile, Encoding.UTF8)));
        Assert.Contains("cờ dừng", result.Process.Output);
    }
}
