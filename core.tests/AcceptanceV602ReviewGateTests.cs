using System.Text;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceV602ReviewGateTests
{
    private static string Runtime => Path.Combine(AppContext.BaseDirectory, "S1-06Runtime", "team.ps1");
    private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "Fixtures", "V602");

    private static async Task<TestShell.Result> Run(string body, string cli = "real")
    {
        using var tmp = new TempRepo(false);
        foreach (var file in Directory.GetFiles(Fixtures))
            File.Copy(file, Path.Combine(tmp.Root, Path.GetFileName(file)));
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        var command = cli == "real" ? $"@dotnet \"{dll}\" %*\r\n" : cli == "error"
            ? "@exit /b 9\r\n" : $"@echo {cli}\r\n";
        tmp.Write("counter.cmd", command, false);
        var setup = """
            $tokens=$null; $errors=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile('__TEAM__',[ref]$tokens,[ref]$errors)
            if ($errors.Count) { throw ($errors | Out-String) }
            foreach ($fn in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) {
                . ([scriptblock]::Create($fn.Extent.Text))
            }
            $Root=(Get-Location).Path; $Utf8=New-Object Text.UTF8Encoding $false
            $Project='Demo'; $Task='V6-02'; $Resume=$true; $Force=$true; $NoVerify=$true
            $cfg=Get-Content team.json -Raw -Encoding UTF8 | ConvertFrom-Json
            $r=[pscustomobject]@{ Path=$Root; Work=(Join-Path $Root 'work'); Logs=(Join-Path $Root 'logs'); Info=@{name='Demo'} }
            New-Item -ItemType Directory -Force $r.Logs,(Join-Path $Root 'tasks'),(Join-Path $Root 'requests') | Out-Null
            $tf=Get-Item 'V6-02-chan-giao-lai-va-xin-quyet-dinh.md'
            function Get-AITeamExe { Join-Path $Root 'counter.cmd' }
            function Resolve-Project { $r }
            function Say($message) { Write-Output $message }
            function Invoke-RepoTransaction { param($RepoPath,$Action) & $Action }
            function git { $global:LASTEXITCODE=0 }
            function Set-BoardStatus { param($r,$taskId,$status,$Journal) $script:board=$status }
            function Set-Rounds($n) {
                $text=[IO.File]::ReadAllText($tf.FullName)
                $text=($text -split '## Review \(Claude\)')[0] + "## Review (Claude)`r`n"
                for ($i=1; $i -le $n; $i++) { $text+="### Vòng $i — CẦN SỬA`r`n" }
                [IO.File]::WriteAllText($tf.FullName,$text,(New-Object Text.UTF8Encoding $true))
            }
            """.Replace("__TEAM__", Runtime.Replace("'", "''"));
        return await TestShell.PowerShell(tmp.Root, setup + "\r\n" + body);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Resume_below_limit_reaches_existing_assign_path(int rounds)
    {
        var result = await Run($"Set-Rounds {rounds}\r\nfunction Find-TaskFile {{ $tf }}\r\nfunction Assert-NotClaimedElsewhere {{ throw 'EXISTING_ASSIGN_PATH' }}\r\nInvoke-Assign");
        Assert.NotEqual(0, result.Code);
        Assert.Contains("EXISTING_ASSIGN_PATH", result.Output + result.Error);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public async Task Gate_uses_inclusive_limit(int rounds, bool blocked)
    {
        var result = await Run($"Set-Rounds {rounds}\r\n$blocked=Stop-ReviewReassignment $r $tf.FullName $Task $cfg\r\nWrite-Output ('BLOCKED='+$blocked)");
        Assert.Equal(0, result.Code);
        Assert.Contains("BLOCKED=" + blocked, result.Output, StringComparison.OrdinalIgnoreCase);
        if (blocked) Assert.Contains($"⏸ Chờ anh quyết · quá {rounds} vòng review", (await Run($"Set-Rounds {rounds}\r\n$null=Stop-ReviewReassignment $r $tf.FullName $Task $cfg\r\n$script:board")).Output);
    }

    [Theory]
    [InlineData("$null", 3)]
    [InlineData("0", 3)]
    [InlineData("-1", 3)]
    [InlineData("2.5", 3)]
    [InlineData("'abc'", 3)]
    [InlineData("'5'", 5)]
    [InlineData("4", 4)]
    public async Task Limit_defaults_only_when_not_positive_integer(string value, int expected)
    {
        var result = await Run($"$cfg=[pscustomobject]@{{review=[pscustomobject]@{{maxRounds={value}}}}}\r\nWrite-Output ('LIMIT='+(Get-ReviewRoundLimit $cfg))");
        Assert.Equal(0, result.Code);
        Assert.Contains("LIMIT=" + expected, result.Output);
    }

    [Fact]
    public async Task Missing_review_configuration_defaults_to_three()
    {
        var result = await Run("Write-Output ('LIMIT='+(Get-ReviewRoundLimit ([pscustomobject]@{})))");
        Assert.Equal(0, result.Code);
        Assert.Contains("LIMIT=3", result.Output);
    }

    [Theory]
    [InlineData("error")]
    [InlineData("noise")]
    [InlineData("-1")]
    [InlineData("2.5")]
    public async Task Operational_cli_failure_stops_assign_before_side_effects(string cli)
    {
        var result = await Run("""
            function Resolve-Project { $r }
            function Find-TaskFile { $tf }
            function Assert-NotClaimedElsewhere { throw 'AGENT_PATH_REACHED' }
            Invoke-Assign
            """, cli);
        Assert.Contains("Không xác định được số vòng review; dừng giao lại.", result.Output + result.Error);
        Assert.DoesNotContain("AGENT_PATH_REACHED", result.Output + result.Error);
    }

    [Theory]
    [InlineData("../X")]
    [InlineData("X*")]
    [InlineData("C:\\X")]
    public async Task Invalid_task_is_rejected_before_lookup(string task)
    {
        var result = await Run($"$Task='{task}'\r\nfunction Find-TaskFile {{ throw 'LOOKUP_REACHED' }}\r\nInvoke-Assign");
        Assert.NotEqual(0, result.Code);
        Assert.DoesNotContain("LOOKUP_REACHED", result.Output + result.Error);
        Assert.Contains("mã task không hợp lệ", result.Output + result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fresh_ai_log_stops_even_without_feedback_or_after_retry(bool retried)
    {
        var result = await Run("""
            Set-Rounds 2
            # R-012: Invoke-Assign đọc task trong worktree ($work/tasks/<tên>) — dựng đủ biến như lúc chạy thật.
            $taskFile=$tf; $work=Join-Path $Root 'wt'
            New-Item -ItemType Directory -Force (Join-Path $work 'tasks') | Out-Null
            Copy-Item -LiteralPath $tf.FullName (Join-Path $work "tasks/$($tf.Name)") -Force
            [IO.File]::WriteAllText((Join-Path $r.Logs 'V6-02.review-1.json'),'{"verdict":"CHANGES_REQUESTED"}')
            $script:ReviewRetried=__RETRIED__; $rv=@{Code=1;Json=@{feedback=''}}; $v=@{Ok=$true}
            $node=$ast.Find({param($n) $n -is [Management.Automation.Language.SwitchStatementAst] -and $n.Extent.Text.Contains('$boardText')},$true)
            if (-not $node) { throw 'Thiếu nhánh review AI' }
            function Invoke-Assign { throw 'AGENT_PATH_REACHED' }
            $action=[scriptblock]::Create($node.Extent.Text + "`r`nSet-BoardStatus $null '' 'OVERWRITTEN'")
            & $action
            $script:board
            """.Replace("__RETRIED__", retried ? "$true" : "$false"));
        Assert.Equal(0, result.Code);
        Assert.Contains("⏸ Chờ anh quyết · quá 3 vòng review", result.Output);
        Assert.DoesNotContain("OVERWRITTEN", result.Output);
        Assert.DoesNotContain("AGENT_PATH_REACHED", result.Output + result.Error);
    }

    // Kịch bản duy nhất được gọi thêm từ tools/test-verify.ps1; git chỉ chạy trong repo tạm.
    [Fact]
    public async Task Resume_three_rounds_creates_one_pending_decision_and_updates_real_board()
    {
        using var repo = new TempRepo();
        using var remote = new TempRepo(false);
        remote.Write("placeholder", "");
        Assert.Equal(0, (await TestShell.Run(remote.Root, "git", 30, "init", "--bare", "origin.git")).Code);
        repo.Git("checkout", "-b", "main");
        repo.Git("remote", "add", "origin", Path.Combine(remote.Root, "origin.git"));
        repo.Write("tasks/README.md", "| [V6-02](V6-02-chan-giao-lai-va-xin-quyet-dinh.md) | A | Chờ |\r\n");
        repo.Git("add", "."); repo.Git("commit", "-m", "Dữ liệu tạm"); repo.Git("push", "-u", "origin", "main");
        // Chạy cùng khung cô lập; Set-BoardStatus thật ghi vào repo/remote tạm ở trên.
        var result = await Run("""
            $r.Path='__REPO__'
            Copy-Item $tf.FullName (Join-Path $r.Path 'tasks')
            New-Item -ItemType Directory -Force (Join-Path $r.Path 'requests') | Out-Null
            Set-Rounds 3
            Copy-Item $tf.FullName (Join-Path $r.Path 'tasks') -Force
            function Resolve-Project { $r }
            function Find-TaskFile { Get-Item (Join-Path $r.Path 'tasks/V6-02-chan-giao-lai-va-xin-quyet-dinh.md') }
            function Assert-NotClaimedElsewhere { throw 'AGENT_PATH_REACHED' }
            Remove-Item Function:git
            $fn=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Set-BoardStatus'},$true)
            . ([scriptblock]::Create($fn.Extent.Text))
            Invoke-Assign; Invoke-Assign
            $items=@(Get-Approvals)
            if ($items.Count -ne 1) { throw 'Approval bị nhân đôi hoặc chưa tạo' }
            $a=$items[0]
            if ($a.kind -ne 'decision' -or $a.status -ne 'pending' -or $a.project -ne 'Demo' -or
                $a.title -cne 'Cần anh duyệt · V6-02 · quá 3 vòng review') { throw 'Nội dung approval sai' }
            foreach ($s in 'Demo','V6-02','Tổng/ngưỡng: 3/3','giao tiếp','Claude tự sửa','bỏ task') {
                if (-not $a.detail.Contains($s)) { throw "Thiếu chi tiết: $s" }
            }
            Get-Content (Join-Path $r.Path 'tasks/README.md') -Raw -Encoding UTF8
            """.Replace("__REPO__", repo.Root.Replace("'", "''")));
        Assert.Equal(0, result.Code);
        Assert.Contains("⏸ Chờ anh quyết · quá 3 vòng review", result.Output);
        Assert.DoesNotContain("AGENT_PATH_REACHED", result.Output + result.Error);
    }
}
