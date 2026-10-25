using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AITeam.Core.Tests;

// Chỉ sao chép mã nguồn được phép; mọi dữ liệu phiếu, bảng việc và số đo đều dựng trong thư mục tạm.
public sealed class AcceptanceV801WorkflowTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "v801-" + Guid.NewGuid().ToString("N"));
    private static string Source([CallerFilePath] string file = "") => Path.GetDirectoryName(Path.GetDirectoryName(file))!;

    public AcceptanceV801WorkflowTests()
    {
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        foreach (var file in new[] { "team.ps1", "tools/workflow-common.ps1", "tools/test-verify.ps1" })
            File.Copy(Path.Combine(Source(), file), Path.Combine(root, file));
    }

    private void Check(string body)
    {
        var script = """
            $ErrorActionPreference = 'Stop'
            function Expect([bool]$ok, [string]$message) { if (-not $ok) { throw $message } }
            . (Join-Path $PSScriptRoot 'tools/workflow-common.ps1')
            $repo = $PSScriptRoot
            function W($p, $s, [bool]$bom = $true) {
                [IO.Directory]::CreateDirectory((Split-Path $p)) | Out-Null
                [IO.File]::WriteAllText($p, $s, [Text.UTF8Encoding]::new($bom))
            }
            function Seed([bool]$bom = $true) {
                W (Join-Path $repo 'tasks/V8-01-demo.md') "# V8-01`r`n- **Yêu cầu:** R-008`r`n"
                W (Join-Path $repo 'requests/R-008.md') "# Phiếu`r`n## Nội dung`r`nGiữ nguyên`r`n## Nhật ký`r`n- 2026-10-20: dữ liệu cũ`r`n## Kết quả`r`nChưa xong`r`n" $bom
            }
            function Journal([string]$event = 'Verify', [int]$run = 1, [string]$result = 'ĐẠT') {
                Update-WorkflowJournal -RepoPath $repo -TaskFile 'V8-01-demo.md' -TaskId 'V8-01' -Event $event -Run $run -Result $result -At ([datetimeoffset]'2026-10-25T14:03:02+07:00')
            }
            try {
            """ + "\n" + body + "\n} catch { [Console]::Error.WriteLine(('LỖI: ' + $_.Exception.Message)); exit 1 }\n";
        var file = Path.Combine(root, Guid.NewGuid().ToString("N") + ".ps1");
        File.WriteAllText(file, script, new UTF8Encoding(true));
        using var process = new System.Diagnostics.Process { StartInfo = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        }};
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", file })
            process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var exited = process.WaitForExit(60000);
        if (!exited) process.Kill(true);
        Assert.True(exited, "Tiến trình giả quá 60 giây.");
        Assert.True(process.ExitCode == 0, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }

    [Theory]
    [InlineData("Build", 1, "x.csproj : error MSB3491: Could not write lines. Access to the path 'obj/x' is denied.", true)]
    [InlineData("Build", 1, "Access to the path 'obj/a' is denied.", true)]
    [InlineData("Test", 1, "The process cannot access the file 'a.dll' because it is being used by another process.", true)]
    [InlineData("Mcp", 1, "MCP transport error: Connection reset by peer (os error 10054).", true)]
    [InlineData("Build", 0, "Access to the path 'obj/a' is denied.", false)]
    [InlineData("Build", 1, "x.csproj : error MSB3491: invalid syntax", false)]
    [InlineData("Build", 1, "Access to the path 'obj/a' is denied.\nx.cs(1): error CS1002: ; expected", false)]
    [InlineData("Build", 1, "Access to the path 'obj/a' is denied.\nx.cs(1): warning CS0168: unused", false)]
    [InlineData("Test", 1, "Access to the path 'obj/a' is denied.\nFailed! Failed: 1, Passed: 2", false)]
    [InlineData("Test", 1, "Access to the path 'obj/a' is denied.\nThe active test run was aborted", false)]
    [InlineData("Build", 1, "Access to the path 'obj/a' is denied.\nx : error MSB1009: project missing", false)]
    [InlineData("Mcp", 1, "MCP startup failed", false)]
    [InlineData("Mcp", 1, "MCP transport error: Connection reset by peer (os error 10054).\nerror: authentication failed", false)]
    [InlineData("Build", 1, "", false)]
    [InlineData("Test", 1, "Failed! Failed: 1", false)]
    [InlineData("Build", 1, "Access denied", false)]
    public void PhanLoaiChiNhanChuKyDaChot(string stage, int code, string output, bool expected)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(output));
        Check($"$o = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{encoded}')); " +
            $"Expect ((Test-InfrastructureFailure -Stage {stage} -Code {code} -Output $o) -eq ${expected.ToString().ToLowerInvariant()}) 'Phân loại sai' ");
    }

    [Theory]
    [InlineData("Build", true)]
    [InlineData("Test", true)]
    [InlineData("Mcp", true)]
    [InlineData("Build", false)]
    [InlineData("Test", false)]
    [InlineData("Mcp", false)]
    public void ThuLaiDungMotLanVaGiuNguyenHaiOutput(string stage, bool recover)
    {
        Check($"$stage = '{stage}'; $recover = ${recover.ToString().ToLowerInvariant()}\n" + """
            $script:n = 0
            $bad = if ($stage -eq 'Mcp') { 'MCP transport error: Connection reset by peer (os error 10054).' } else { "Access to the path 'obj/a' is denied." }
            $good = "Passed! Total tests: 7`r`n  giữ khoảng trắng  `r`n"
            $r = Invoke-WorkflowCheck -Stage $stage -Run {
                param($number)
                $script:n++
                Expect ($number -eq $script:n) 'Sai số lần callback'
                if ($number -eq 2 -and $recover) { [pscustomobject]@{Code=0;Output=$good} }
                else { [pscustomobject]@{Code=1;Output=$bad} }
            }
            Expect ($script:n -eq 2 -and @($r.Attempts).Count -eq 2) 'Không đúng hai lần'
            Expect ($r.Attempts[0].Number -eq 1 -and $r.Attempts[0].Code -eq 1 -and $r.Attempts[0].Output -ceq $bad) 'Mất lần đầu'
            $last = if ($recover) { $good } else { $bad }
            $code = if ($recover) { 0 } else { 1 }
            Expect ($r.Code -eq $code -and $r.Output -ceq $last) 'Bỏ qua kết quả cuối'
            Expect ($r.Attempts[1].Number -eq 2 -and $r.Attempts[1].Code -eq $code -and $r.Attempts[1].Output -ceq $last) 'Mất lần sau'
            """);
    }

    [Theory]
    [InlineData(0, "OK")]
    [InlineData(1, "x.cs: error CS1002: ; expected")]
    [InlineData(1, "Access to the path 'obj/a' is denied.\nx.cs: error CS1002: ; expected")]
    public void KhongThuLaiThanhCongHoacLoiCode(int code, string output)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(output));
        Check($"$code={code}; $o=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{encoded}'))\n" + """
            $script:n=0
            $r=Invoke-WorkflowCheck Build { $script:n++; [pscustomobject]@{Code=$code;Output=$o} }
            Expect ($script:n -eq 1 -and @($r.Attempts).Count -eq 1 -and $r.Code -eq $code -and $r.Output -ceq $o) 'Thử lại hoặc mất lỗi code'
            """);
    }

    [Fact]
    public void LoiCallbackKhongBiBienThanhDat() => Check("""
        $message=''
        try { Invoke-WorkflowCheck Build { throw 'lỗi giả' } } catch { $message=$_.Exception.Message }
        Expect ($message -eq 'lỗi giả') 'Nuốt ngoại lệ'
        """);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void QcSauCongKeCaPhucHoi(bool recover) => Check($"$recover=${recover.ToString().ToLowerInvariant()}\n" + """
        $script:qc=0; $script:verify=0
        $g=Invoke-WorkflowGate -Verify {
            $script:verify++
            $r=Invoke-WorkflowCheck Build { param($n)
                if ($n -eq 2 -and $recover) { [pscustomobject]@{Code=0;Output='OK'} }
                else { [pscustomobject]@{Code=1;Output="Access to the path 'obj/a' is denied."} }
            }
            [pscustomobject]@{Ok=($r.Code -eq 0);Fails=@();Attempts=$r.Attempts}
        } -Qc { $script:qc++; [pscustomobject]@{Code=3;Out='BLOCKED'} }
        Expect ($script:verify -eq 1 -and $g.Verify.Ok -eq $recover) 'Sai kết quả cổng'
        $want=if ($recover) {1} else {0}
        Expect ($script:qc -eq $want) 'QC không theo kết quả cổng'
        if ($recover) { Expect ($g.Review.Code -eq 3) 'Mất kết quả QC' }
        else { Expect ($null -eq $g.Review) 'QC chạy khi cổng trượt' }
        """);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuNamSuKienDungMucGiuBomVaKhongTrung(bool bom) => Check($"Seed ${bom.ToString().ToLowerInvariant()}\n" + """
        $p=Join-Path $repo 'requests/R-008.md'
        foreach ($e in 'Prep','Assign','Verify','Qc','Merge') {
            Expect (Journal $e 1) 'Không ghi lần đầu'
            Expect (-not (Journal $e 1 'KHÔNG ĐẠT')) 'Không chống trùng'
        }
        Expect (Journal Verify 2 'KHÔNG ĐẠT') 'Mất lượt mới'
        $s=[IO.File]::ReadAllText($p)
        Expect (([regex]::Matches($s,'<!-- workflow:')).Count -eq 6) 'Sai số sự kiện'
        Expect ($s.Contains('- 2026-10-25 14:03:02 +07:00 | V8-01 | Prep | lượt 1 | ĐẠT <!-- workflow:V8-01:Prep:1 -->')) 'Sai định dạng giờ/task/lượt'
        Expect ($s.Contains("## Nội dung`r`nGiữ nguyên`r`n## Nhật ký`r`n- 2026-10-20: dữ liệu cũ`r`n")) 'Mất dữ liệu cũ'
        Expect ($s.EndsWith("## Kết quả`r`nChưa xong`r`n")) 'Ghi sai mục'
        Expect (-not [regex]::IsMatch($s,'(?<!\r)\n')) 'Đổi xuống dòng'
        $b=[IO.File]::ReadAllBytes($p)
        Expect (($b[0] -eq 239 -and $b[1] -eq 187 -and $b[2] -eq 191) -eq $bom) 'Đổi BOM'
        """.Replace("$bom", "$" + bom.ToString().ToLowerInvariant()));

    [Fact]
    public void TaskCuThieuYeuCauKhongTaoPhieu() => Check("""
        Seed
        W (Join-Path $repo 'tasks/V8-01-demo.md') '# Task cũ'
        $p=Join-Path $repo 'requests/R-008.md'; $before=[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))
        Expect (-not (Journal)) 'Task cũ phải bỏ qua nhật ký'
        Expect ([Convert]::ToBase64String([IO.File]::ReadAllBytes($p)) -eq $before) 'Đổi phiếu task cũ'
        """);

    [Theory]
    [InlineData("missing")]
    [InlineData("heading")]
    [InlineData("duplicate")]
    [InlineData("utf8")]
    [InlineData("request")]
    [InlineData("task")]
    public void PhieuThieuHongKhongGhiDe(string kind) => Check($"$kind='{kind}'\n" + """
        Seed
        $p=Join-Path $repo 'requests/R-008.md'
        switch ($kind) {
            missing { Remove-Item -LiteralPath $p }
            heading { W $p '# Không có nhật ký' }
            duplicate { W $p "## Nhật ký`n## Nhật ký`n" }
            utf8 { [IO.File]::WriteAllBytes($p, [byte[]]@(255,254,192,175)) }
            request { W (Join-Path $repo 'tasks/V8-01-demo.md') '- **Yêu cầu:** ../../escape' }
            task { Remove-Item -LiteralPath (Join-Path $repo 'tasks/V8-01-demo.md') }
        }
        $before=if (Test-Path $p) {[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))} else {'missing'}
        $message=''; try { Journal } catch { $message=$_.Exception.Message }
        Expect ($message -eq 'Không ghi được nhật ký phiếu.') 'Sai thông báo'
        $after=if (Test-Path $p) {[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))} else {'missing'}
        Expect ($before -ceq $after) 'Ghi đè phiếu lỗi'
        """);

    [Theory]
    [InlineData("../V8-01-demo.md")]
    [InlineData("tasks/../V8-01-demo.md")]
    [InlineData("V8-*.md")]
    [InlineData("absolute")]
    public void ChanDuongDanThoat(string name) => Check($"$name='{name}'\n" + """
        Seed
        if ($name -eq 'absolute') { $name=Join-Path $repo 'tasks/V8-01-demo.md' }
        $p=Join-Path $repo 'requests/R-008.md'; $before=[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))
        $message=''
        try { Update-WorkflowJournal $repo $name V8-01 Verify 1 'ĐẠT' } catch { $message=$_.Exception.Message }
        Expect ($message -eq 'Không ghi được nhật ký phiếu.') 'Nhận đường dẫn không an toàn'
        Expect ([Convert]::ToBase64String([IO.File]::ReadAllBytes($p)) -eq $before) 'Đổi dữ liệu'
        """);

    [Theory]
    [InlineData("requests")]
    [InlineData("tasks")]
    public void ChanJunctionTrongMoiThanhPhan(string folder) => Check($"$folder='{folder}'\n" + """
        Seed
        $link=Join-Path $repo $folder; $target=Join-Path $repo ('real-'+$folder)
        Move-Item -LiteralPath $link -Destination $target
        New-Item -ItemType Junction -Path $link -Target $target | Out-Null
        try {
            $p=Join-Path $repo 'requests/R-008.md'; $before=[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))
            $message=''; try { Journal } catch { $message=$_.Exception.Message }
            Expect ($message -eq 'Không ghi được nhật ký phiếu.') 'Nhận reparse point'
            Expect ([Convert]::ToBase64String([IO.File]::ReadAllBytes($p)) -eq $before) 'Ghi qua junction'
        } finally { [IO.Directory]::Delete($link) }
        """);

    [Theory]
    [InlineData("x\n## Nhật ký")]
    [InlineData("x|y")]
    [InlineData("<!-- workflow:fake -->")]
    public void ChanChenDongQuaKetQua(string result)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
        Check($"$result=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{encoded}'))\n" + """
            Seed
            $p=Join-Path $repo 'requests/R-008.md'; $before=[Convert]::ToBase64String([IO.File]::ReadAllBytes($p))
            $message=''; try { Journal Verify 1 $result } catch { $message=$_.Exception.Message }
            Expect ($message -eq 'Không ghi được nhật ký phiếu.') 'Nhận chuỗi chèn dòng'
            Expect ([Convert]::ToBase64String([IO.File]::ReadAllBytes($p)) -eq $before) 'Đã ghi chuỗi chèn'
            """);
    }

    [Fact]
    public void HaiTienTrinhKhongMatDongVaChongTrung() => Check("""
        Seed
        $worker=Join-Path $repo 'worker.ps1'
        W $worker @'
        param($repo,$start)
        $ErrorActionPreference='Stop'
        try {
            . (Join-Path $repo 'tools/workflow-common.ps1')
            foreach ($n in 1..12) {
                Update-WorkflowJournal $repo V8-01-demo.md V8-01 Verify ([int]$start+$n) 'ĐẠT' | Out-Null
            }
        } catch { [Console]::Error.WriteLine(('LỖI: '+$_.Exception.Message)); exit 1 }
        '@
        $exe=(Get-Process -Id $PID).Path
        $a=Start-Process $exe -WindowStyle Hidden -PassThru -ArgumentList @('-NoProfile','-File',('"'+$worker+'"'),('"'+$repo+'"'),'0')
        $b=Start-Process $exe -WindowStyle Hidden -PassThru -ArgumentList @('-NoProfile','-File',('"'+$worker+'"'),('"'+$repo+'"'),'6')
        foreach ($p in @($a,$b)) {
            $done=$p.WaitForExit(20000)
            if (-not $done) { $p.Kill() }
            Expect $done 'Tiến trình giả treo'; Expect ($p.ExitCode -eq 0) 'Tiến trình giả lỗi'
        }
        $s=[IO.File]::ReadAllText((Join-Path $repo 'requests/R-008.md'))
        Expect (([regex]::Matches($s,'<!-- workflow:')).Count -eq 18) 'Mất dòng hoặc ghi trùng'
        foreach ($n in 1..18) { Expect ($s.Contains("<!-- workflow:V8-01:Verify:$n -->")) 'Mất sự kiện' }
        """);

    [Fact]
    public void KhoaNhaSauNgoaiLeVaTaiNhap() => Check("""
        $script:n=0; $message=''
        try { Invoke-RepoTransaction $repo { throw 'lỗi giả' } } catch { $message=$_.Exception.Message }
        Expect ($message -eq 'lỗi giả') 'Nuốt lỗi trong khóa'
        Invoke-RepoTransaction $repo { Invoke-RepoTransaction ($repo + '/.') { $script:n++ } -TimeoutSeconds 1 } -TimeoutSeconds 1
        Expect ($script:n -eq 1) 'Không nhả khóa hoặc không tái nhập'
        """);

    [Theory]
    [InlineData("Invoke-Prep", "Invoke-WorkflowCheck")]
    [InlineData("Invoke-Verify", "Invoke-WorkflowCheck")]
    [InlineData("Invoke-Assign", "Invoke-WorkflowGate")]
    [InlineData("Set-BoardStatus", "Invoke-RepoTransaction")]
    [InlineData("Save-RunMetrics", "Invoke-RepoTransaction")]
    [InlineData("Invoke-Prep", "Update-WorkflowJournal")]
    [InlineData("Invoke-Assign", "Update-WorkflowJournal")]
    [InlineData("Invoke-Verify", "Update-WorkflowJournal")]
    [InlineData("Invoke-Qc", "Update-WorkflowJournal")]
    [InlineData("Invoke-Merge", "Update-WorkflowJournal")]
    public void LuongCuPhaiNoiVaoHopDongMoi(string function, string command) => Check("""
        $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'team.ps1'),[ref]$null,[ref]$null)
        $fn=$ast.Find({ $args[0] -is [Management.Automation.Language.FunctionDefinitionAst] -and $args[0].Name -eq 'FUNCTION_NAME' },$true)
        Expect ($null -ne $fn) 'Thiếu hàm cũ'
        $calls=@($fn.FindAll({ $args[0] -is [Management.Automation.Language.CommandAst] -and $args[0].GetCommandName() -eq 'COMMAND_NAME' },$true))
        Expect ($calls.Count -gt 0) 'Hàm cũ chưa nối hợp đồng mới (comment không tính)'
        """.Replace("FUNCTION_NAME", function).Replace("COMMAND_NAME", command));

    [Theory]
    [InlineData("Build", "success", true)]
    [InlineData("Build", "warning", false)]
    [InlineData("Build", "code", false)]
    [InlineData("Build", "dirty", false)]
    [InlineData("Test", "success", true)]
    [InlineData("Test", "failed", false)]
    [InlineData("Test", "hang", false)]
    public void CongThatKiemTraDayDuSauThuLai(string stage, string outcome, bool expected) => Check(
        "$stage='" + stage + "'; $outcome='" + outcome + "'; $expected=$" + expected.ToString().ToLowerInvariant() + "\n" + """
        $script:targetStage=$stage
        $Root=$repo; $Task='V8-01'; $Utf8=[Text.UTF8Encoding]::new($false)
        $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'team.ps1'),[ref]$null,[ref]$null)
        foreach ($fn in $ast.FindAll({ $args[0] -is [Management.Automation.Language.FunctionDefinitionAst] },$false)) {
            . ([scriptblock]::Create($fn.Extent.Text))
        }
        Seed
        $work=Join-Path $repo 'work'; [IO.Directory]::CreateDirectory($work) | Out-Null
        W (Join-Path $work 'src/Agent.cs') '// Mã giả có BOM'
        $text="## Phạm vi`n- ``src/Agent.cs```n## Báo cáo của agent`nĐã chạy đầy đủ build và test trên dữ liệu tạm và xác nhận không mất dữ liệu trước khi báo xong.`n"
        W (Join-Path $work 'tasks/V8-01-demo.md') $text
        $r=[pscustomobject]@{Path=$repo;Logs=(Join-Path $repo 'reports');Info=[pscustomobject]@{name='Demo';build='fake-build';test='fake-test'}}
        $ctx=[pscustomobject]@{R=$r;Work=$work;Branch='agent-a/V8-01';Text=$text;TaskFile=(Get-Item (Join-Path $repo 'tasks/V8-01-demo.md'))}
        function Get-Config { [pscustomobject]@{} }
        function Get-TaskContext { $ctx }
        function Say($text,$color) { Write-Output $text | Out-Host }
        # Hàm git giả: không khởi chạy executable, không truy cập .git thật.
        function git {
            $global:LASTEXITCODE=0
            $cmd=$args[2]; $tail=$args -join ' '
            switch ($cmd) {
                fetch { }
                'merge-base' { 'base' }
                diff { if ($tail.Contains('--name-only')) { 'src/Agent.cs' } }
                status { if ($outcome -eq 'dirty') { ' M src/Agent.cs' } }
                log { }
                'rev-parse' { if ($tail.Contains('--abbrev-ref')) {'main'} else {'head'} }
                'ls-remote' { "head`trefs/heads/agent-a/V8-01" }
                add { }
                commit { }
                push { }
                merge { }
                default { throw "Lệnh git giả chưa khai báo: $tail" }
            }
        }
        $script:buildCalls=0; $script:testCalls=0
        $bad="Access to the path 'obj/locked.dll' is denied."
        function Invoke-ShellCapture($command,$dir) {
            if ($command -eq 'fake-build') { $script:buildCalls++; $n=$script:buildCalls; $current='Build' }
            elseif ($command -eq 'fake-test') { $script:testCalls++; $n=$script:testCalls; $current='Test' }
            else { throw "Lệnh giả chưa khai báo: $command" }
            $code=0; $o='Passed! Failed: 0, Passed: 7, Total tests: 7'
            if ($current -eq $script:targetStage) {
                if ($n -eq 1) { $code=1; $o=$bad }
                elseif ($outcome -eq 'warning') { $o='a.cs(1): warning CS0168: unused' }
                elseif ($outcome -eq 'code') { $code=1; $o='a.cs(1): error CS1002: ; expected' }
                elseif ($outcome -eq 'failed') { $code=1; $o='Failed! Failed: 1, Passed: 6, Total tests: 7' }
                elseif ($outcome -eq 'hang') { $o='The active test run was aborted' }
            }
            [pscustomobject]@{Code=$code;Output=$o}
        }
        $v=Invoke-Verify
        Expect ($v.Ok -eq $expected) 'Cổng bỏ qua lỗi sau thử lại hoặc không phục hồi'
        $calls=if ($stage -eq 'Build') {$script:buildCalls} else {$script:testCalls}
        Expect ($calls -eq 2) 'Cổng không thử lại đúng một lần'
        $report=[IO.File]::ReadAllText($v.Report)
        Expect ($report.Contains($bad)) 'Báo cáo cổng làm mất output lần đầu'
        Expect ($report.Contains('Passed!') -or $report.Contains('CS0168') -or $report.Contains('CS1002') -or $report.Contains('aborted')) 'Báo cáo mất output lần hai'
        """);

    [Fact]
    public void KhoaChanTienTrinhKhacVaTimeout() => Check("""
        $worker=Join-Path $repo 'lock-worker.ps1'
        W $worker @'
        param($repo)
        $ErrorActionPreference='Stop'
        try {
            . (Join-Path $repo 'tools/workflow-common.ps1')
            Invoke-RepoTransaction $repo {
                [IO.File]::WriteAllText((Join-Path $repo 'ready'), 'ok')
                $until=[DateTime]::UtcNow.AddSeconds(12)
                while (-not (Test-Path (Join-Path $repo 'release'))) {
                    if ([DateTime]::UtcNow -gt $until) { throw 'Hết hạn chờ test' }
                    Start-Sleep -Milliseconds 20
                }
            }
        } catch { [Console]::Error.WriteLine(('LỖI: '+$_.Exception.Message)); exit 1 }
        '@
        $exe=(Get-Process -Id $PID).Path
        $p=Start-Process $exe -WindowStyle Hidden -PassThru -ArgumentList @('-NoProfile','-File',('"'+$worker+'"'),('"'+$repo+'"'))
        try {
            $until=[DateTime]::UtcNow.AddSeconds(5)
            while (-not (Test-Path (Join-Path $repo 'ready')) -and [DateTime]::UtcNow -lt $until) { Start-Sleep -Milliseconds 20 }
            Expect (Test-Path (Join-Path $repo 'ready')) 'Tiến trình chưa lấy được khóa'
            $message=''; $script:entered=$false
            try { Invoke-RepoTransaction ($repo.ToUpperInvariant() + '/.') { $script:entered=$true } -TimeoutSeconds 1 } catch { $message=$_.Exception.Message }
            Expect ($message -eq 'Không lấy được khóa giao dịch repo.' -and -not $script:entered) 'Khóa không chặn tiến trình khác'
            $other=Join-Path $repo 'other'; [IO.Directory]::CreateDirectory($other) | Out-Null
            Invoke-RepoTransaction $other { $script:entered=$true } -TimeoutSeconds 1
            Expect $script:entered 'Khóa repo khác bị chặn'
        } finally {
            W (Join-Path $repo 'release') 'ok'
            $done=$p.WaitForExit(15000)
            if (-not $done) { $p.Kill() }
        }
        Expect $done 'Không nhả khóa'; Expect ($p.ExitCode -eq 0) 'Tiến trình giữ khóa lỗi'
        """);

    public void Dispose() => Directory.Delete(root, true);
}
