using System.Text;
using AITeam.Core.Tests.Support;
using Xunit;

namespace AITeam.Core.Tests;

public class AcceptanceV808AutoCommitTests
{
    private const string TaskText = "# V8-08 Tiêu đề tiếng Việt\n\n- **Agent:** A\n- **Nhánh:** `agent-a/V8-08`\n\n### Vòng 1 — ĐẠT\n";
    private static string LoadFunctions()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "team.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root.FullName, "team.ps1").Replace("'", "''");
        return """
            $tokens=$null; $errors=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile('__SOURCE__',[ref]$tokens,[ref]$errors)
            if ($errors.Count) { throw 'Không phân tích được team.ps1' }
            $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in @('Invoke-Merge','Set-BoardStatus','Get-BoardInsertIndex','Test-HasBom','Get-TaskId')},$false) | ForEach-Object { . ([scriptblock]::Create($_.Extent.Text)) }
            function Invoke-RepoTransaction { param($RepoPath,$Action) & $Action }
            function Find-TaskFile { param($r,$taskId) Get-Item (Join-Path $r.Path 'tasks/V8-08.md') }
            function Say { param($text,$color) Write-Output $text }
            $r=[pscustomobject]@{Path=(Get-Location).Path;Info=[pscustomobject]@{build='build';test='test'}}
            """.Replace("__SOURCE__", path);
    }

    private static void Initialize(TempRepo repo, TempRepo remote)
    {
        remote.Git("init", "--bare", "-q");
        repo.Git("checkout", "-b", "main");
        repo.Write("tasks/V8-08.md", TaskText);
        repo.Write("tasks/README.md", "# Bảng việc\n\n| Task | Agent | Nhánh | Trạng thái |\n| --- | --- | --- | --- |\n| [V8-07 Cũ](V8-07.md) | B | `old` | Xong |\n\n## Ghi chú\nGiữ nguyên.\n");
        repo.Write("requests/R-008.md", "Nhật ký cũ\n");
        repo.Write("code.cs", "// Mã cũ\n");
        repo.Git("add", "."); repo.Git("commit", "-qm", "Khởi tạo");
        repo.Git("remote", "add", "origin", remote.Root);
        repo.Git("push", "-qu", "origin", "main");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Merge_commits_only_allowed_changes_and_blocks_dirty_code(bool dirtyCode)
    {
        using var repo = new TempRepo(); using var remote = new TempRepo(false);
        Initialize(repo, remote);
        repo.Git("checkout", "-b", "agent-a/V8-08");
        repo.Write("feature.txt", "Thay đổi nhánh\n");
        repo.Git("add", "."); repo.Git("commit", "-qm", "Tính năng");
        repo.Git("push", "-q", "origin", "agent-a/V8-08"); repo.Git("checkout", "main");
        repo.Write("requests/R-008.md", "Nhật ký mới\n");
        repo.Write("metrics/runs/new.json", "{}");
        File.AppendAllText(Path.Combine(repo.Root, "tasks/README.md"), "Ghi chú mới\n");
        if (dirtyCode) repo.Write("code.cs", "// Mã chưa commit\n");
        var before = repo.Git("rev-parse", "HEAD");
        var result = await TestShell.PowerShell(repo.Root, LoadFunctions() + """

            $Task='V8-08'; $env:AITEAM_MERGE_MUTEX='AITeam_V808_'+[guid]::NewGuid().ToString('N')
            function Get-Config { @{} }
            function Resolve-Project { $r }
            function Assert-MergeApproval { $null }
            function Get-StaffName { 'Agent A' }
            function Invoke-ShellCapture { [pscustomobject]@{Code=0;Output='Passed! Passed: 1';Warnings=@()} }
            function Invoke-FailedTestRetry { param($Code,$Output,$TestCommand,$Run) [pscustomobject]@{Code=$Code;Output=$Output;Warnings=@()} }
            function Update-WorkflowJournal { }
            function Set-BoardStatus { }
            Invoke-Merge
            """);
        if (dirtyCode)
        {
            Assert.NotEqual(0, result.Code); Assert.Contains("Main local chưa sạch", result.Error);
            Assert.Equal(before, repo.Git("rev-parse", "HEAD")); Assert.Equal(before, remote.Git("rev-parse", "refs/heads/main"));
        }
        else
        {
            Assert.True(result.Code == 0, result.Error + result.Output);
            Assert.True(File.Exists(Path.Combine(repo.Root, "feature.txt")));
            Assert.Contains("Nhật ký / bảng / số đo (tự commit trước merge)", repo.Git("log", "--format=%s"));
            Assert.Equal(repo.Git("rev-parse", "HEAD"), remote.Git("rev-parse", "refs/heads/main"));
            Assert.Empty(repo.Git("status", "--porcelain").Trim());
        }
    }

    [Fact]
    public async Task Board_inserts_missing_task_once_then_updates_status()
    {
        using var repo = new TempRepo(); using var remote = new TempRepo(false);
        Initialize(repo, remote);
        var result = await TestShell.PowerShell(repo.Root, LoadFunctions() + "\nSet-BoardStatus $r 'V8-08' 'Đang chạy'\nSet-BoardStatus $r 'V8-08' 'Đã xong'\n");
        Assert.True(result.Code == 0, result.Error + result.Output);
        var board = File.ReadAllText(Path.Combine(repo.Root, "tasks/README.md"));
        Assert.Equal(1, board.Split("[V8-08").Length - 1);
        Assert.Contains("| [V8-08 Tiêu đề tiếng Việt](V8-08.md) | A | `agent-a/V8-08` | Đã xong |", board);
        Assert.True(board.IndexOf("[V8-07", StringComparison.Ordinal) < board.IndexOf("[V8-08", StringComparison.Ordinal));
        Assert.True(board.IndexOf("[V8-08", StringComparison.Ordinal) < board.IndexOf("## Ghi chú", StringComparison.Ordinal));
        Assert.Equal(new byte[] { 0xef, 0xbb, 0xbf }, File.ReadAllBytes(Path.Combine(repo.Root, "tasks/README.md"))[..3]);
        Assert.Equal(repo.Git("rev-parse", "HEAD"), remote.Git("rev-parse", "refs/heads/main"));
    }
}
