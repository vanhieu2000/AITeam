using System.Text;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public class AcceptanceV805SafeMergeTests
{
    private static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Không thấy team.ps1.");
        }
    }

    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    private static string Load => $$$"""
        $env:AITEAM_MERGE_MUTEX={{{Quote("Global\\AITeam_Merge_test_" + Guid.NewGuid().ToString("N"))}}}
        . {{{Quote(Path.Combine(Root, "tools/workflow-common.ps1"))}}}
        $tokens=$null; $errors=$null
        $ast=[Management.Automation.Language.Parser]::ParseFile({{{Quote(Path.Combine(Root, "team.ps1"))}}},[ref]$tokens,[ref]$errors)
        if ($errors.Count) { throw 'Lỗi cú pháp' }
        $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]}, $true) | ForEach-Object { Invoke-Expression $_.Extent.Text }
        function Say($text,$color) { Write-Output $text }
        function Part($function,$pattern) {
            $f=$ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $function},$true)[0]
            $f.FindAll({param($n) $n.Extent.Text -match $pattern},$true) | Where-Object { $_ -is [Management.Automation.Language.IfStatementAst] -or $_ -is [Management.Automation.Language.ForEachStatementAst] -or $_ -is [Management.Automation.Language.AssignmentStatementAst] } | Sort-Object { $_.Extent.Text.Length } | Select-Object -First 1 -ExpandProperty Extent | Select-Object -ExpandProperty Text
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Merge_checks_local_clone_before_push_and_rolls_back_failure(bool pass)
    {
        using var repo = new TempRepo();
        using var remote = new TempRepo(false);
        var bare = await TestShell.Run(remote.Root, "git", 30, "init", "--bare", "-q");
        Assert.Equal(0, bare.Code);
        repo.Git("branch", "-M", "main");
        repo.Write("tasks/V8-05.md", "**Nhánh:** `agent-a/task`\n**Agent:** A\n### Vòng 1 — ĐẠT\n");
        repo.Git("add", "."); repo.Git("commit", "-qm", "Nền");
        repo.Git("remote", "add", "origin", remote.Root);
        repo.Git("push", "-qu", "origin", "main");
        var before = repo.Git("rev-parse", "HEAD").Trim();
        repo.Git("checkout", "-qb", "agent-a/task");
        repo.Write("change.txt", "Thay đổi"); repo.Git("add", "."); repo.Git("commit", "-qm", "Task");
        repo.Git("push", "-q", "origin", "agent-a/task"); repo.Git("checkout", "-q", "main");
        var result = await TestShell.PowerShell(repo.Root, Load + $$$"""
            $Task='V8-05'; $Project='Test'; $Root={{{Quote(repo.Root)}}}
            $mergeMutexName=$env:AITEAM_MERGE_MUTEX
            $r=@{Path={{{Quote(repo.Root)}}}; Info=@{name='Test';build='build';test='test'}}
            function Get-Config { @{} }
            function Resolve-Project { $r }
            function Find-TaskFile { Get-Item (Join-Path $r.Path 'tasks/V8-05.md') }
            function Assert-MergeApproval { $null }
            function Get-StaffName { 'Agent A' }
            function Get-TaskId { 'V8-05' }
            function Update-WorkflowJournal { }
            function Set-BoardStatus { [IO.File]::WriteAllText((Join-Path $r.Path 'board-result'), 'Đã merge') }
            function Invoke-ShellCapture($command,$dir) {
                if ($dir -eq $r.Path -or -not (Test-Path (Join-Path $dir 'change.txt'))) { throw 'Không kiểm clone sạch' }
                $script:clonePath=$dir
                if ($command -eq 'build') { return @{Code=0;Output='Build succeeded. 0 Warning(s)'} }
                if ($env:AITEAM_MERGE_MUTEX -eq $mergeMutexName -or $env:AITEAM_MERGE_MUTEX -notlike 'Global\AITeam_Merge_test_*') { throw 'Test clone chưa cô lập mutex' }
                return @{Code={{{(pass ? 0 : 1)}}};Output={{{Quote(pass ? "Passed! Failed: 0 Passed: 1 Total: 1" : "Không đạt")}}}}
            }
            try { Invoke-Merge; 'MERGED' } catch { "FAILED: $_" }
            if ($env:AITEAM_MERGE_MUTEX -ne $mergeMutexName) { throw 'Chưa khôi phục biến mutex' }
            if (Test-Path $script:clonePath) { throw 'Chưa dọn clone' }
            "PREFERENCE:$ErrorActionPreference"
            """, 60);
        Assert.True(result.Code == 0, result.Output + result.Error);
        var after = repo.Git("rev-parse", "HEAD").Trim();
        var pushed = repo.Git("ls-remote", "origin", "refs/heads/main").Split('\t')[0];
        Assert.Equal(after, pushed);
        Assert.Contains("PREFERENCE:Stop", result.Output);
        if (pass)
        {
            Assert.NotEqual(before, after);
            Assert.Contains("MERGED", result.Output);
            Assert.True(File.Exists(Path.Combine(repo.Root, "board-result")));
        }
        else
        {
            Assert.Equal(before, after);
            Assert.Contains("Clone sạch KHÔNG ĐẠT — đã hủy merge, main không đổi", result.Output);
            Assert.False(File.Exists(Path.Combine(repo.Root, "board-result")));
        }
    }

    [Theory]
    [InlineData("pass", false)]
    [InlineData("test", false)]
    [InlineData("build", false)]
    [InlineData("test", true)]
    public async Task Merge_enforces_order_and_refuses_reset_when_remote_already_contains_merge(string failure, bool pushed)
    {
        using var tmp = new TempRepo(false);
        tmp.Write("tasks/V8-05.md", "**Nhánh:** `agent-a/task`\n**Agent:** A\n### Vòng 1 — ĐẠT\n");
        var result = await TestShell.PowerShell(tmp.Root, Load + $$$"""
            $r=@{Path={{{Quote(tmp.Root)}}};Info=@{name='Test';build='build';test='test'}}; $Root=$r.Path; $Task='V8-05'
            $script:head='old'; $script:remote='old'; $script:events=New-Object Collections.ArrayList
            function Get-Config { @{} }
            function Resolve-Project { $r }
            function Find-TaskFile { Get-Item (Join-Path $r.Path 'tasks/V8-05.md') }
            function Assert-MergeApproval { $null }
            function Get-StaffName { 'Agent A' }
            function Get-TaskId { 'V8-05' }
            function Update-WorkflowJournal { }
            function Set-BoardStatus { [void]$script:events.Add('board') }
            function git {
                $a=@($args); if ($a[0] -eq '-C') { $a=@($a | Select-Object -Skip 2) }
                $global:LASTEXITCODE=0
                switch ($a[0]) {
                    'show' { '### Vòng 1 — ĐẠT' }
                    'branch' { 'main' }
                    'rev-parse' { if ($a[1] -eq 'ORIG_HEAD') { 'old' } else { $script:head } }
                    'rev-list' { if ($a -contains '--first-parent' -and $script:remote -ne $script:head) { $script:head } }
                    'merge' { $script:head='new'; [void]$script:events.Add('merge') }
                    'clone' { $script:clone=$a[-1]; New-Item -ItemType Directory $script:clone | Out-Null; [void]$script:events.Add('clone') }
                    'fetch' { if ({{{(pushed ? "$true" : "$false")}}} -and $script:head -eq 'new') { $script:remote='new' } }
                    'reset' { $script:head='old'; [void]$script:events.Add('reset') }
                    'push' { $script:remote=$script:head; [void]$script:events.Add('push') }
                }
            }
            function Invoke-ShellCapture($command,$dir) {
                if ($dir -ne $script:clone) { throw 'Sai clone' }
                [void]$script:events.Add($command)
                if ($command -eq {{{Quote(failure)}}}) { return @{Code=1;Output='Lỗi có chủ đích'} }
                @{Code=0;Output='Passed! Failed: 0 Passed: 1'}
            }
            try { Invoke-Merge } catch { "ERROR: $_" }
            "EVENTS:$($script:events -join ',')"; "HEAD:$script:head"; "REMOTE:$script:remote"
            if (Test-Path $script:clone) { throw 'Chưa dọn clone' }
            """
            // Test giả lập không dùng dotnet nên không có cờ timeout được nối vào lệnh test.
            , 30);
        Assert.True(result.Code == 0, result.Output + result.Error);
        if (failure == "pass")
        {
            Assert.Contains("EVENTS:merge,clone,build,test,push,board", result.Output);
            Assert.Contains("REMOTE:new", result.Output);
        }
        else
        {
            Assert.DoesNotContain(",push", result.Output);
            Assert.DoesNotContain(",board", result.Output);
            Assert.Contains(pushed ? "Không an toàn để hủy merge" : "đã hủy merge, main không đổi", result.Output);
            Assert.Contains(pushed ? "HEAD:new" : "HEAD:old", result.Output);
        }
    }

    [Fact]
    public async Task Merge_waits_for_machine_mutex_and_releases_on_failure()
    {
        using var tmp = new TempRepo(false);
        var mutexName = "Global\\AITeam_Merge_test_" + Guid.NewGuid().ToString("N");
        var marker = Path.Combine(tmp.Root, "locked");
        var holder = TestShell.PowerShell(tmp.Root, $$$"""
            $m=New-Object Threading.Mutex($false,{{{Quote(mutexName)}}}); if (-not $m.WaitOne(10000)) { throw 'Không lấy được khóa test' }
            try { [IO.File]::WriteAllText({{{Quote(marker)}}},'x'); Start-Sleep -Seconds 3 } finally { $m.ReleaseMutex(); $m.Dispose() }
            """, 15);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!File.Exists(marker) && DateTime.UtcNow < deadline) await Task.Delay(50);
        Assert.True(File.Exists(marker), "Tiến trình giữ khóa chưa khởi động.");
        var result = await TestShell.PowerShell(tmp.Root, Load + "\n$env:AITEAM_MERGE_MUTEX=" + Quote(mutexName) + "; function Get-Config { throw 'ĐÃ VÀO MERGE' }; try { Invoke-Merge } catch { $_ }; $m=New-Object Threading.Mutex($false,$env:AITEAM_MERGE_MUTEX); if (-not $m.WaitOne(0)) { throw 'Khóa chưa nhả' }; $m.ReleaseMutex(); $m.Dispose()", 15);
        Assert.True(holder.IsCompleted, "Merge thứ hai đã vượt khóa.");
        Assert.Equal(0, (await holder).Code);
        Assert.True(result.Code == 0, result.Output + result.Error);
        Assert.Contains("ĐÃ VÀO MERGE", result.Output);
    }

    [Fact]
    public async Task Merge_tests_finish_while_real_machine_mutex_is_held()
    {
        using var tmp = new TempRepo(false);
        var marker = Path.Combine(tmp.Root, "locked");
        var release = Path.Combine(tmp.Root, "release");
        var holder = TestShell.PowerShell(tmp.Root, $$$"""
            $m=New-Object Threading.Mutex($false,'Global\AITeam_Merge'); $locked=$false
            try {
                try { $locked=$m.WaitOne(0) } catch [Threading.AbandonedMutexException] { $locked=$true }
                [IO.File]::WriteAllText({{{Quote(marker)}}},'x')
                $deadline=[datetime]::UtcNow.AddSeconds(20)
                while (-not (Test-Path {{{Quote(release)}}})) {
                    if ([datetime]::UtcNow -ge $deadline) { throw 'Test chưa kết thúc khi đang giữ khóa thật' }
                    Start-Sleep -Milliseconds 50
                }
            } finally { if ($locked) { $m.ReleaseMutex() }; $m.Dispose() }
            """, 25);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!File.Exists(marker) && DateTime.UtcNow < deadline) await Task.Delay(50);
            Assert.True(File.Exists(marker), "Tiến trình kiểm tra khóa thật chưa khởi động.");
            await Merge_waits_for_machine_mutex_and_releases_on_failure();
            Assert.False(holder.IsCompleted, "Tiến trình giữ khóa thật đã kết thúc trước test.");
        }
        finally { await File.WriteAllTextAsync(release, "x"); Assert.Equal(0, (await holder).Code); }
    }

    [Fact]
    public async Task Deleted_tests_are_blocked_and_invalid_model_returns_before_verify()
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, Load + """

            function git { 'core.tests/Regression.cs'; 'src/Normal.cs' }
            $fails=New-Object Collections.ArrayList
            Invoke-Expression (Part 'Invoke-Verify' 'foreach \(\$deleted')
            if ($fails.Count -ne 1 -or $fails[0] -ne 'Xóa file test: core.tests/Regression.cs') { throw 'Không chặn xóa test' }
            $script:status=''; $r=@{}; $Task='V8-05'; $taskFile=@{Name='task.md'}; $logText='INVALID MODEL SELECTION'; $errText=''; $NoVerify=$false
            function Set-BoardStatus($r,$Task,$status,$Journal) { $script:status=$status }
            $branch=Part 'Invoke-Assign' 'if \("\$logText`n\$errText" -match'
            Set-Item function:Run-AgentBranch ([scriptblock]::Create($branch + "; throw 'Đã chạy verify'"))
            Run-AgentBranch
            if ($script:status -ne '🔴 Lỗi agent · model không hợp lệ') { throw 'Sai trạng thái' }
            'OK'
            """);
        Assert.True(result.Code == 0, result.Output + result.Error);
        Assert.Contains("OK", result.Output);
    }

    [Fact]
    public async Task Models_ignores_progress_stderr_and_keeps_ids()
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, Load + """

            function Get-Config { @{agy=@{exe='Fake-Agy'}} }
            function Fake-Agy { Write-Error 'Fetching available models...'; 'model-id'; $global:LASTEXITCODE=0 }
            Invoke-Models
            if ($ErrorActionPreference -ne 'Stop') { throw 'Sai preference' }
            """);
        Assert.True(result.Code == 0, result.Output + result.Error);
        Assert.Contains("model-id", result.Output);
        Assert.DoesNotContain("Fetching available models...", result.Output);
    }

    [Fact]
    public async Task Prep_retries_infrastructure_once_and_verify_waits_twenty_seconds()
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, Load + """

            $r=@{Info=@{build='build'}}; $work='test'; $script:calls=0; $script:delay=0; $script:shutdown=0
            function Invoke-ShellCapture { $script:calls++; if ($script:calls -eq 1) { @{Code=1;Output='error MSB3491: Access denied obj'} } else { @{Code=0;Output='OK'} } }
            function dotnet { $script:shutdown++ }
            function Start-Sleep($Seconds) { $script:delay+=$Seconds }
            Invoke-Expression (Part 'Invoke-Prep' '^\$bCheck = Invoke-WorkflowCheck')
            if ($bCheck.Code -ne 0 -or $script:calls -ne 2 -or $script:delay -ne 20 -or $script:shutdown -ne 1) { throw 'Sai retry prep' }
            $script:calls=0; $script:delay=0
            Invoke-Expression (Part 'Invoke-Verify' '^\$bCheck = Invoke-WorkflowCheck')
            if ($bCheck.Code -ne 0 -or $script:delay -ne 20) { throw 'Sai retry verify' }
            if (-not (Test-InfrastructureFailure Build 1 'error MSB3027: Exceeded retry count. Failed. The file is being used')) { throw 'Thiếu MSB3027' }
            if (Test-InfrastructureFailure Build 1 "error MSB3027: Exceeded retry count`nerror CS1002: ; expected") { throw 'Đã nuốt lỗi code' }
            $devProvider='codex'
            Invoke-Expression (Part 'Invoke-Assign' "if \(\`$devProvider -eq 'codex'\)")
            if ($script:shutdown -ne 2) { throw 'Thiếu shutdown sau Codex' }
            'OK'
            """);
        Assert.True(result.Code == 0, result.Output + result.Error);
        Assert.Contains("OK", result.Output);
    }

    [Fact]
    public async Task Recent_assignments_balance_without_double_count_and_expire()
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, Load + $$$"""
            $r=@{Path={{{Quote(tmp.Root)}}}}; $Task='T-01'
            if ((Select-FreeAgent $r A) -ne 'A') { throw 'Sai gợi ý đầu' }
            $Task='T-02'; if ((Select-FreeAgent $r A) -ne 'B') { throw 'Không chia đều' }
            if ((Select-FreeAgent $r A) -ne 'B') { throw 'Không giữ đặt chỗ' }
            New-Item -ItemType Directory -Force (Join-Path $r.Path 'tasks') | Out-Null
            [IO.File]::WriteAllText((Join-Path $r.Path 'tasks/README.md'), '| [T-01](T-01.md) | A | 🟡 |')
            $Task='T-03'; if ((Select-FreeAgent $r A) -ne 'A') { throw 'Đếm trùng bảng và đặt chỗ' }
            $state=Join-Path $r.Path '.state/assign-recent.json'
            $entries=Get-Content $state -Raw | ConvertFrom-Json
            $entries | ForEach-Object { $_.At=[datetimeoffset]::Now.AddMinutes(-11).ToString('o') }
            [IO.File]::WriteAllText($state,(ConvertTo-Json -InputObject @($entries | ForEach-Object { $_ })))
            $Task='T-04'; if ((Select-FreeAgent $r A) -ne 'B') { throw 'Chưa hết hạn đặt chỗ' }
            'OK'
            """);
        Assert.True(result.Code == 0, result.Output + result.Error);
        Assert.Contains("OK", result.Output);
    }
}