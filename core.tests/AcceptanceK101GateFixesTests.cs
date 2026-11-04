using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

// K1-01 (Claude viết trước): 3 lỗi [CK] của cổng kiểm tra trong team.ps1 (R-014).
//  1. Báo nhầm HẾT HẠN MỨC khi lượt agy lỗi mạng (V10-02: báo cáo của agent có chữ "429").
//  2. Cổng chỉ nhận commit ĐẦU nhánh làm bản gốc test nghiệm thu; bản sửa test sau đó của quản lý bị coi là agent sửa (W2-02).
//  3. Cổng (Get-TaskContext) đoán thư mục theo tên a-/b-<task>; worktree tên khác ⇒ không kiểm (V10-03). Nhánh đã chuẩn bị sẵn thì không đổi Agent.
public sealed class AcceptanceK101GateFixesTests
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
        $ErrorActionPreference='Stop'
        . {{{Quote(Path.Combine(Root, "tools/workflow-common.ps1"))}}}
        $tokens=$null; $errors=$null
        $ast=[Management.Automation.Language.Parser]::ParseFile({{{Quote(Path.Combine(Root, "team.ps1"))}}},[ref]$tokens,[ref]$errors)
        if ($errors.Count) { throw 'Lỗi cú pháp' }
        $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]}, $true) | ForEach-Object { Invoke-Expression $_.Extent.Text }
        function Say($text,$color) { Write-Output $text }
        function G { $ErrorActionPreference = 'Continue'; $o = & git @args 2>&1; if ($LASTEXITCODE -ne 0) { throw "git $args : $o" }; @($o | Where-Object { $_ -is [string] }) | Select-Object -Last 1 }

        """;

    private static async Task Run(string workDir, string body)
    {
        var r = await TestShell.PowerShell(workDir, Load + body + "\n'OK-K101'", 120);
        Assert.True(r.Code == 0, r.Output + r.Error);
        Assert.Contains("OK-K101", r.Output);
    }

    // Log thật V10-02 (rút gọn): báo cáo agent có "429 → QuotaExhausted", lỗi thật là mạng.
    private const string V1002Log = """{"status":"ERROR","response":"Đã làm adapter: phân loại mã lỗi (429 $\\to$ `QuotaExhausted`, 401/403 $\\to$ `NotLoggedIn`). Đã push.","error":"API error (attempt 2): request failed: Post \"https://daily-cloudcode-pa.googleapis.com/v1internal:streamGenerateContent?alt=sse\": dial tcp: lookup daily-cloudcode-pa.googleapis.com: no such host","duration_seconds":1511.4,"usage":{"input_tokens":488277,"output_tokens":85270,"thinking_tokens":50449,"total_tokens":573547},"finished_reason":"error"}""";

    [Fact]
    public async Task Network_error_is_not_quota_even_when_agent_report_mentions_429()
    {
        using var tmp = new TempRepo(false);
        File.WriteAllText(Path.Combine(tmp.Root, "v1002.json"), V1002Log);
        await Run(tmp.Root, """
            $log = [IO.File]::ReadAllText((Join-Path (Get-Location) 'v1002.json'))
            if (Test-QuotaExhausted $log '') { throw 'V10-02: lỗi mạng bị báo HẾT HẠN MỨC' }
            if (-not (Test-NetworkError $log '')) { throw 'V10-02: không nhận ra lỗi mạng' }
            # Báo cáo agent nhắc "quota" / "rate limit" nhưng lỗi rỗng ⇒ không phải hạn mức.
            $talk = '{"status":"ERROR","response":"Thêm xử lý quota và rate limit, mã 429.","error":"exit status 1"}'
            if (Test-QuotaExhausted $talk '') { throw 'Chữ trong báo cáo agent bị coi là hạn mức' }
            if (Test-NetworkError $talk '') { throw 'exit status 1 không phải lỗi mạng' }
            """);
    }

    [Fact]
    public async Task Real_quota_signals_still_detected()
    {
        using var tmp = new TempRepo(false);
        await Run(tmp.Root, """
            $e1 = '{"status":"ERROR","response":"","error":"RESOURCE_EXHAUSTED: Quota exceeded for model"}'
            if (-not (Test-QuotaExhausted $e1 '')) { throw 'Trường error có RESOURCE_EXHAUSTED' }
            if (-not (Test-QuotaExhausted '' 'Error: 429 Too Many Requests')) { throw 'stderr có 429' }
            # Log không phải JSON (agy hỏng giữa chừng) ⇒ giữ cách dò cũ trên toàn văn.
            if (-not (Test-QuotaExhausted 'fatal: You have exceeded your quota' '')) { throw 'Log thô có quota' }
            $ok = '{"status":"SUCCESS","response":"xong","error":"RESOURCE_EXHAUSTED"}'
            if (Test-QuotaExhausted $ok '') { throw 'SUCCESS không bao giờ là hạn mức' }
            # Số token chứa 429 không khớp.
            if (Test-QuotaExhausted '{"status":"ERROR","error":"x","usage":{"thinking_tokens":42953}}' '') { throw '42953 khớp nhầm' }
            foreach ($n in @('dial tcp 1.2.3.4:443: connectex: No connection could be made','read: connection reset by peer','lookup x: no such host','context deadline exceeded (Client.Timeout exceeded)')) {
                if (-not (Test-NetworkError ('{"status":"ERROR","error":"' + $n + '"}') '')) { throw "Không nhận lỗi mạng: $n" }
            }
            """);
    }

    [Fact]
    public async Task Manager_fix_commit_after_first_commit_becomes_acceptance_reference()
    {
        using var tmp = new TempRepo(false);
        await Run(tmp.Root, """
            $w = Join-Path (Get-Location) 'w'; New-Item -ItemType Directory $w | Out-Null; Set-Location $w
            G init -q; G config user.name 'HieuDV'; G config user.email 'vanhieu2000@users.noreply.github.com'
            [IO.File]::WriteAllText("$w/a.txt", 'goc'); G add -A; G commit -q -m 'goc'
            $base = (G rev-parse HEAD)
            New-Item -ItemType Directory "$w/tests" | Out-Null
            [IO.File]::WriteAllText("$w/tests/T.cs", 'v1'); G add -A; G commit -q -m 'Test nghiệm thu K-9 (Claude)'
            $first = (G rev-parse HEAD)
            # Agent sửa code (không đụng test).
            [IO.File]::WriteAllText("$w/a.txt", 'agent'); G add -A
            G -c user.name='Agent B' -c user.email='agent-b@aiteam.local' commit -q -m 'K-9: code'
            $mgr = 'vanhieu2000@users.noreply.github.com'
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/T.cs' -ManagerEmail $mgr) -ne $first) { throw 'Chưa có bản sửa ⇒ phải là commit đầu' }
            # Quản lý sửa test sau đó, đúng danh tính + "(Claude)".
            [IO.File]::WriteAllText("$w/tests/T.cs", 'v2'); G add -A; G commit -q -m 'Sửa test nghiệm thu K-9 so ordinal (Claude)'
            $fix = (G rev-parse HEAD)
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/T.cs' -ManagerEmail $mgr) -ne $fix) { throw 'Không nhận bản sửa test của quản lý' }
            # Agent giả "(Claude)" nhưng danh tính agent ⇒ không được nhận.
            [IO.File]::WriteAllText("$w/tests/T.cs", 'v3'); G add -A
            G -c user.name='Agent B' -c user.email='agent-b@aiteam.local' commit -q -m 'Sửa test (Claude)'
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/T.cs' -ManagerEmail $mgr) -ne $fix) { throw 'Commit agent mạo danh (Claude) được nhận' }
            # Danh tính quản lý nhưng không ghi "(Claude)" / "Test nghiệm thu" ⇒ không nhận (vd. team.ps1 tự commit hộ Codex).
            [IO.File]::WriteAllText("$w/tests/T.cs", 'v4'); G add -A; G commit -q -m 'K-9: codex viết code'
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/T.cs' -ManagerEmail $mgr) -ne $fix) { throw 'Commit quản lý không đánh dấu được nhận' }
            """);
    }

    [Fact]
    public async Task Acceptance_test_already_on_main_uses_base_unless_manager_fixed_it()
    {
        using var tmp = new TempRepo(false);
        await Run(tmp.Root, """
            $w = Join-Path (Get-Location) 'w'; New-Item -ItemType Directory $w | Out-Null; Set-Location $w
            G init -q; G config user.name 'HieuDV'; G config user.email 'vanhieu2000@users.noreply.github.com'
            New-Item -ItemType Directory "$w/tests" | Out-Null
            [IO.File]::WriteAllText("$w/tests/M.cs", 'v1'); G add -A; G commit -q -m 'Test nghiệm thu trên main (Claude)'
            $base = (G rev-parse HEAD)
            $mgr = 'vanhieu2000@users.noreply.github.com'
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/M.cs' -ManagerEmail $mgr) -ne $base) { throw 'Test trên main ⇒ phải là base' }
            [IO.File]::WriteAllText("$w/tests/M.cs", 'v2'); G add -A; G commit -q -m 'Sửa test nghiệm thu (Claude)'
            $fix = (G rev-parse HEAD)
            if ((Get-AcceptRef -Work $w -Base $base -Path 'tests/M.cs' -ManagerEmail $mgr) -ne $fix) { throw 'Bản sửa trên nhánh của test có sẵn trên main không được nhận' }
            if (Get-AcceptRef -Work $w -Base $base -Path 'tests/KhongCo.cs' -ManagerEmail $mgr) { throw 'File không có ⇒ phải rỗng' }
            # Hàm verify dùng Get-AcceptRef (không còn tự tìm commit đầu).
            $verify = ($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Invoke-Verify'}, $true) | Select-Object -First 1).Extent.Text
            if ($verify -notmatch 'Get-AcceptRef') { throw 'Invoke-Verify chưa gọi Get-AcceptRef' }
            """);
    }

    [Fact]
    public async Task Task_context_and_assign_use_existing_worktree_of_branch_and_prepared_branch_keeps_agent()
    {
        using var tmp = new TempRepo(false);
        await Run(tmp.Root, """
            $root = (Get-Location).Path
            $remote = Join-Path $root 'remote.git'; G init -q --bare $remote
            $main = Join-Path $root 'main'; G init -q $main; Set-Location $main
            G config user.name 'HieuDV'; G config user.email 'vanhieu2000@users.noreply.github.com'
            [IO.File]::WriteAllText("$main/a.txt", 'x'); G add -A; G commit -q -m 'goc'; G branch -M main
            G remote add origin $remote; G push -q -u origin main
            $workRoot = Join-Path $root 'work'; New-Item -ItemType Directory $workRoot | Out-Null
            $r = [pscustomobject]@{ Path = $main; Work = $workRoot }
            $custom = Join-Path $workRoot 'rv\V10-03'
            G worktree add -q -b 'agent-c/V10-03-duyet' $custom main
            $default = Join-Path $workRoot 'c-v10-03'
            $got = Resolve-TaskWork $r 'agent-c/V10-03-duyet' $default
            if ((Resolve-Path $got).Path -ne (Resolve-Path $custom).Path) { throw "Không dùng worktree có sẵn của nhánh: $got" }
            if ((Resolve-TaskWork $r 'agent-a/K-1-moi' (Join-Path $workRoot 'a-k-1')) -ne (Join-Path $workRoot 'a-k-1')) { throw 'Nhánh chưa có worktree ⇒ phải là mặc định' }
            # Nhánh đã chuẩn bị sẵn (có worktree hoặc có trên origin) ⇒ không chia lại Agent.
            if (-not (Test-BranchPrepared $r 'agent-c/V10-03-duyet')) { throw 'Có worktree mà không coi là đã chuẩn bị' }
            G branch -q 'agent-b/K-2-san' main; G push -q origin 'agent-b/K-2-san'; G branch -q -D 'agent-b/K-2-san'
            if (-not (Test-BranchPrepared $r 'agent-b/K-2-san')) { throw 'Có trên origin mà không coi là đã chuẩn bị' }
            if (Test-BranchPrepared $r 'agent-a/K-3-chua') { throw 'Nhánh chưa có ở đâu bị coi là đã chuẩn bị' }
            # Get-TaskContext và Invoke-Assign dùng chung Resolve-TaskWork; chia đều chỉ khi nhánh chưa chuẩn bị.
            $fn = { param($name) ($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name}, $true) | Select-Object -First 1).Extent.Text }
            if ((& $fn 'Get-TaskContext') -notmatch 'Resolve-TaskWork') { throw 'Get-TaskContext chưa dùng Resolve-TaskWork' }
            $assign = & $fn 'Invoke-Assign'
            if ($assign -notmatch 'Resolve-TaskWork') { throw 'Invoke-Assign chưa dùng Resolve-TaskWork' }
            if ($assign -notmatch 'Test-BranchPrepared') { throw 'Invoke-Assign chưa bỏ chia đều cho nhánh đã chuẩn bị' }
            """);
    }
}
