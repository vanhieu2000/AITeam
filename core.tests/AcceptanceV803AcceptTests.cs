using System.Text;
using AITeam.Core.Tests.Support;
using Xunit;

namespace AITeam.Core.Tests;

/// <summary>Nghiệm thu V8-03: chỉ PowerShell giả và dữ liệu tạm, không gọi AI hoặc git thật.</summary>
public sealed class AcceptanceV803AcceptTests
{
    private static string SourceRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("Không thấy mã nguồn nghiệm thu.");
        }
    }

    private static string Fixture(string name) => Path.Combine(SourceRoot, "tests", "fixtures", "V8-03", name);
    private const string MetricName = "20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json";

    private static TempRepo Sandbox()
    {
        var repo = new TempRepo(false);
        foreach (var file in new[] { "workflow-accept.ps1", "workflow-pipeline.ps1" })
        {
            Directory.CreateDirectory(Path.Combine(repo.Root, "tools"));
            File.Copy(Path.Combine(SourceRoot, "tools", file), Path.Combine(repo.Root, "tools", file));
        }
        repo.Write("task.md", TaskText);
        return repo;
    }

    // Khung task mới lấy bố cục từ templates/task.md; phiếu/metrics bên dưới là bản sao byte thật.
    private const string TaskText = "# V8-03 · Nghiệm thu\r\n\r\n- **Agent:** A\r\n- **Nhánh:** `agent-a/V8-03-demo`\r\n- **Yêu cầu:** R-008\r\n- **Ngân sách:** ~390 dòng\r\n\r\n## Báo cáo của agent\r\nGiữ nguyên báo cáo.\r\n\r\n## Review (Claude)\r\n### Vòng 2 — CẦN SỬA\r\nSửa lỗi.\r\n\r\n## Kết quả\r\nChưa nghiệm thu.\r\n\r\n## Đánh giá quy trình\r\n| Điểm | Nguồn |\r\n|---|---|\r\n| không rõ | không rõ |\r\n";

    private const string Prelude = """
        . ./tools/workflow-pipeline.ps1
        . ./tools/workflow-accept.ps1
        $head = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
        $newHead = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
        $e = [pscustomobject]@{ TaskId='V8-03'; Head=$head; Run=3; VerifyOk=$true;
            Verdict='PASS'; Findings=@(); Minutes='không rõ'; Tokens='không rõ'; Score='không rõ';
            Sources=@('V8-03-verify-3.md','V8-03.review-3.json') }
        $item = [pscustomobject]@{ Id='V8-03'; Evidence=$e; Added=7; Budget=390;
            Changes=@('hub/Pages/Requests.cshtml'); DependsOn=@('V8-01','V8-02') }
        $original = [IO.File]::ReadAllText((Join-Path $PWD 'task.md'))
        $files = @{ 'tasks/V8-03-demo.md'=$original; 'src/Keep.cs'='giữ code'; 'tests/Keep.cs'='giữ test' }
        $ctx = [pscustomobject]@{ TaskId='V8-03'; Head=$head; TaskFile='tasks/V8-03-demo.md'; Files=$files; Merged=$false }
        $script:calls = @(); $script:decision=$null
        $read = { param($id,$sha) $script:calls += 'read'; $e }
        $write = { param($d)
            $script:calls += 'write'; $script:decision=$d
            $copy=@{}; foreach($key in $files.Keys) { $copy[$key]=$files[$key] }
            $copy[$ctx.TaskFile]=$original.Replace('Chưa nghiệm thu.', 'Đã nghiệm thu.')
            $copy
        }
        $publish = { param($updated) $script:calls+='publish'; @{Ok=$true;Head=$newHead} }
        $approval = { param($sha) $script:calls+='approval'; if($sha -cne $newHead){throw 'Sai SHA approval'}; $true }
        $merge = { param($sha) $script:calls+='merge'; if($sha -cne $newHead){throw 'Sai SHA merge'}; @{Ok=$true} }
        $resume = { param($again,$feedback) $script:calls+='resume'; $script:resumed=@($again,$feedback) }
        $argsAccept = @{ Context=$ctx;ReadEvidence=$read;WriteReview=$write;Publish=$publish;
            CheckApproval=$approval;Merge=$merge;ResumeTask=$resume }
        """;

    private static async Task<string> Run(TempRepo repo, string script)
    {
        var r = await TestShell.PowerShell(repo.Root, Prelude + "\r\n" + script);
        Assert.True(r.Code == 0, r.Output + "\n" + r.Error);
        return r.Output;
    }

    [Fact]
    public async Task Line_count_excludes_task_acceptance_and_binary_from_seed_diff()
    {
        using var repo = Sandbox();
        Assert.Equal("7", (await Run(repo, """
            $stat=@("7`t2`tsrc/Agent.cs", "90`t0`ttasks/V8-03-demo.md", "120`t0`ttests/Acceptance.cs", "-`t-`timage.png")
            Get-WorkflowAcceptLineCount -NumStat $stat -TaskFile 'tasks/V8-03-demo.md' -AcceptanceFiles @('tests/Acceptance.cs')
            """)).Trim());
    }

    [Fact]
    public async Task Line_count_empty_is_zero()
    {
        using var repo = Sandbox();
        Assert.Equal("0", (await Run(repo, "Get-WorkflowAcceptLineCount -NumStat @() -TaskFile 'tasks/V8-03-demo.md' -AcceptanceFiles @()")).Trim());
    }

    [Fact]
    public async Task Codex_writer_runs_under_shared_lock_and_unknown_scores_are_not_invented()
    {
        using var repo = Sandbox();
        Assert.Contains("LOCKED", await Run(repo, """
            $script:inside=$false; $script:lockCount=0
            function Invoke-WorkflowCodexLock { param([scriptblock]$Action)
                $script:lockCount++; $script:inside=$true
                try { & $Action } finally { $script:inside=$false }
            }
            $argsAccept.WriteReview={param($d)
                if(-not $script:inside){throw 'Codex không có khóa chung'}
                if($d.Evidence.Score -cne 'không rõ' -or $d.Evidence.Tokens -cne 'không rõ'){throw 'Bịa điểm/token'}
                & $write $d
            }
            $r=Invoke-WorkflowAccept @argsAccept
            if($r.Status -cne 'MERGED' -or $script:lockCount -ne 1 -or $script:inside){throw 'Không nhả khóa'}
            'LOCKED'
            """));
    }

    [Fact]
    public async Task Pack_includes_all_tasks_findings_budget_pages_and_dependencies_without_writes()
    {
        using var repo = Sandbox();
        var before = File.ReadAllBytes(Path.Combine(repo.Root, "task.md"));
        var text = await Run(repo, """
            $e.Findings=@(@{id='F1';severity='Required';open=$true},@{id='F2';severity='Optional';open=$true})
            $other=[pscustomobject]@{Id='V8-04';Evidence=$e;Added=1;Budget=20;Changes=@();DependsOn=@()}
            Get-WorkflowAcceptPack -Items @($item,$other)
            """);
        foreach (var value in new[] { "V8-03", "V8-04", "F1", "F2", "390", "7", "/Requests", "V8-01", "V8-02", "PASS" })
            Assert.Contains(value, text);
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(repo.Root, "task.md")));
    }

    [Fact]
    public async Task Pack_empty_is_explicit()
    {
        using var repo = Sandbox();
        Assert.Equal("Không có task chờ nghiệm thu.", (await Run(repo, "Get-WorkflowAcceptPack -Items @()")).Trim());
    }

    [Theory]
    [InlineData("hub/wwwroot/site.css", "không rõ")]
    [InlineData("hub/Pages/Shared/_Layout.cshtml", "không rõ")]
    [InlineData("core/Foo.cs", "Không")]
    [InlineData("hub/Pages/Requests.cshtml.cs", "/Requests")]
    public async Task Pack_reports_ui_uncertainty(string path, string expected)
    {
        using var repo = Sandbox();
        Assert.Contains(expected, await Run(repo, "$item.Changes=@('" + path + "'); Get-WorkflowAcceptPack @($item)"));
    }

    [Fact]
    public async Task Pack_never_drops_open_findings_to_fit_forty_lines()
    {
        using var repo = Sandbox();
        var text = await Run(repo, """
            $e.Findings=@(1..45 | ForEach-Object { @{id=('OPEN-'+$_);severity='Required';open=$true} })
            Get-WorkflowAcceptPack @($item)
            """);
        for (var n = 1; n <= 45; n++) Assert.Contains("OPEN-" + n, text);
        Assert.Contains("40", text);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("broken")]
    [InlineData("legacy")]
    [InlineData("old-sha")]
    [InlineData("similar-id")]
    public async Task Evidence_missing_corrupt_legacy_stale_or_near_id_is_unknown(string variant)
    {
        using var repo = Sandbox();
        Directory.CreateDirectory(Path.Combine(repo.Root, "logs"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "metrics"));
        File.Copy(Fixture(MetricName), Path.Combine(repo.Root, "metrics", MetricName));
        repo.Write("mode.txt", variant);
        var output = await Run(repo, """
            $mode=[IO.File]::ReadAllText((Join-Path $PWD 'mode.txt')).Trim()
            $envelope=@{task='V8-03';head=$head;run=3;verify=@{ok=$true;source='V8-03-verify-3.md'};
                qc=@{verdict='PASS';findings=@();source='V8-03.review-3.json'};metrics=@{source='20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json'}}
            if($mode -eq 'old-sha'){$envelope.head=$newHead}
            if($mode -eq 'similar-id'){$envelope.task='V8-030'}
            if($mode -eq 'legacy'){$envelope.Remove('head')}
            if($mode -ne 'missing'){
                $json=if($mode -eq 'broken'){'{hỏng'}else{$envelope|ConvertTo-Json -Depth 8}
                [IO.File]::WriteAllText((Join-Path $PWD 'logs/V8-03.accept-3.json'),$json)
            }
            $v=Get-WorkflowAcceptEvidence V8-03 $head (Join-Path $PWD logs) (Join-Path $PWD metrics)
            if($v.VerifyOk){throw 'Tin nhầm KT'}
            if($v.Verdict -cne 'không rõ' -or $v.Score -cne 'không rõ'){throw 'Bịa kết luận hoặc điểm'}
            Write-Output 'không rõ'
            """);
        Assert.Contains("không rõ", output);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("test")]
    [InlineData("report")]
    [InlineData("heading")]
    [InlineData("new-file")]
    public async Task Documents_block_changes_outside_three_sections(string variant)
    {
        using var repo = Sandbox();
        var body = variant switch
        {
            "code" => "$after['src/Keep.cs']='sửa code'",
            "test" => "$after['tests/Keep.cs']='sửa test'",
            "report" => "$after[$ctx.TaskFile]=$original.Replace('Giữ nguyên báo cáo.','sửa báo cáo')",
            "heading" => "$after[$ctx.TaskFile]=$original.Replace('## Kết quả','## Phạm vi')",
            _ => "$after['new.md']='file lạ'"
        };
        Assert.Contains("Codex sửa ngoài mục nghiệm thu cho phép.", await Run(repo, """
            $after=@{};foreach($k in $files.Keys){$after[$k]=$files[$k]}
            """ + body + """

            try { Test-WorkflowAcceptDocuments $files $after $ctx.TaskFile; throw 'Đã cho sửa ngoài phạm vi' }
            catch { if($_.Exception.Message -cne 'Codex sửa ngoài mục nghiệm thu cho phép.'){throw}; $_.Exception.Message }
            """));
    }

    [Fact]
    public async Task Documents_allow_review_result_and_process_only()
    {
        using var repo = Sandbox();
        Assert.Contains("True", await Run(repo, """
            $after=@{};foreach($k in $files.Keys){$after[$k]=$files[$k]}
            $after[$ctx.TaskFile]=$original.Replace('Sửa lỗi.','Đã kiểm tra.').Replace('Chưa nghiệm thu.','Đạt.').Replace('| không rõ | không rõ |','| không rõ | log |')
            Test-WorkflowAcceptDocuments $files $after $ctx.TaskFile
            """));
    }

    [Fact]
    public async Task Bound_evidence_uses_real_metric_fields_and_keeps_unknown_score()
    {
        using var repo = Sandbox();
        Directory.CreateDirectory(Path.Combine(repo.Root, "metrics"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "logs"));
        File.Copy(Fixture(MetricName), Path.Combine(repo.Root, "metrics", MetricName));
        Assert.Contains("VALID", await Run(repo, """
            $metricPath=Join-Path $PWD 'metrics/20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json'
            $m=Get-Content $metricPath -Raw -Encoding UTF8|ConvertFrom-Json
            $m.task='V8-03'; $m|Add-Member -NotePropertyName head -NotePropertyValue $head
            [IO.File]::WriteAllText($metricPath,($m|ConvertTo-Json -Depth 8))
            # Envelope là hợp đồng MỚI của V8-03; hai source giả không mô phỏng định dạng log cũ.
            [IO.File]::WriteAllText((Join-Path $PWD 'logs/V8-03-verify-3.md'),'# Cổng kiểm tra V8-03 — ĐẠT')
            [IO.File]::WriteAllText((Join-Path $PWD 'logs/V8-03.review-3.json'),'{"verdict":"PASS"}')
            $record=@{task='V8-03';project='HDVTool';head=$head;run=3;
                verify=@{ok=$true;source='V8-03-verify-3.md'};
                qc=@{verdict='PASS';findings=@();source='V8-03.review-3.json'};
                metrics=@{source='20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json'}}
            [IO.File]::WriteAllText((Join-Path $PWD 'logs/V8-03.accept-3.json'),($record|ConvertTo-Json -Depth 8))
            $v=Get-WorkflowAcceptEvidence V8-03 $head (Join-Path $PWD logs) (Join-Path $PWD metrics)
            if(-not $v.VerifyOk -or $v.Verdict -cne 'PASS'){throw 'Mất bằng chứng đã gắn SHA'}
            if([math]::Abs([double]$v.Minutes-(7645.0/60)) -gt 0.01 -or $v.Tokens -ne 1642151){throw 'Đọc sai mẫu metrics thật'}
            if($v.Score -cne 'không rõ' -or @($v.Sources).Count -lt 2){throw 'Bịa điểm/mất nguồn'}
            'VALID'
            """));
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("C:/outside.json")]
    public async Task Unsafe_metric_sources_are_unknown(string source)
    {
        using var repo = Sandbox();
        Directory.CreateDirectory(Path.Combine(repo.Root, "logs"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "metrics"));
        repo.Write("source.txt", source);
        Assert.Contains("SAFE", await Run(repo, """
            $record=@{task='V8-03';head=$head;run=3;verify=@{ok=$true;source='../outside.md'};
                qc=@{verdict='PASS';source='../outside.json'};
                metrics=@{source=([IO.File]::ReadAllText((Join-Path $PWD 'source.txt')).Trim())}}
            [IO.File]::WriteAllText((Join-Path $PWD 'logs/V8-03.accept-3.json'),($record|ConvertTo-Json -Depth 8))
            $v=Get-WorkflowAcceptEvidence V8-03 $head (Join-Path $PWD logs) (Join-Path $PWD metrics)
            if($v.VerifyOk -or $v.Tokens -cne 'không rõ'){throw 'Tin đường dẫn ngoài'}
            'SAFE'
            """));
    }

    [Fact]
    public async Task Head_changed_during_codex_blocks_publish()
    {
        using var repo = Sandbox();
        Assert.Contains("Chưa đủ bằng chứng nghiệm thu.", await Run(repo, """
            $argsAccept.WriteReview={param($d) $ctx.Head=$newHead; & $write $d }
            try { Invoke-WorkflowAccept @argsAccept; throw 'Đã push khi SHA đổi' }
            catch { if($_.Exception.Message -cne 'Chưa đủ bằng chứng nghiệm thu.'){throw}; $_.Exception.Message }
            if($script:calls -contains 'publish'){throw 'Đã publish sau đổi SHA'}
            """));
    }

    [Theory]
    [InlineData("gate")]
    [InlineData("qc")]
    [InlineData("sha")]
    [InlineData("required")]
    [InlineData("blocker")]
    [InlineData("sources")]
    public async Task Accept_requires_current_gate_qc_and_classified_findings(string variant)
    {
        using var repo = Sandbox();
        var arrange = variant switch
        {
            "gate" => "$e.VerifyOk=$false",
            "qc" => "$e.Verdict='BLOCKED'",
            "sha" => "$e.Head=$newHead",
            "sources" => "$e.Sources=@()",
            _ => "$e.Findings=@(@{id='F1';severity='" + (variant == "blocker" ? "Blocker" : "Required") + "';open=$true;classification=''})"
        };
        Assert.Contains("Chưa đủ bằng chứng nghiệm thu.", await Run(repo, arrange + """

            try { Invoke-WorkflowAccept @argsAccept; throw 'Đã cho nghiệm thu thiếu bằng chứng' }
            catch { if($_.Exception.Message -cne 'Chưa đủ bằng chứng nghiệm thu.'){throw}; $_.Exception.Message }
            if($script:calls -contains 'write' -or $script:calls -contains 'publish'){throw 'Đã ghi trước khi đủ bằng chứng'}
            """));
    }

    [Fact]
    public async Task Accept_pass_uses_next_round_preserves_vietnamese_and_new_sha()
    {
        using var repo = Sandbox();
        var output = await Run(repo, """
            $note='Ghi chú tiếng Việt: "a"; $env:HOME | <tag> ` & ký tự'
            $e.Findings=@(@{id='F1';severity='Required';open=$true;classification=''})
            $r=Invoke-WorkflowAccept @argsAccept -Optional 'F1: hoãn cải tiến' -Note $note
            if($r.Status -cne 'MERGED' -or $r.Head -cne $newHead){throw 'Sai kết quả'}
            if($script:decision.Round -ne 3 -or $script:decision.Verdict -cne 'ĐẠT'){throw 'Sai vòng'}
            if($script:decision.Note -cne $note -or $script:decision.Optional -cne 'F1: hoãn cải tiến'){throw 'Hỏng tiếng Việt/ký tự'}
            if(($script:calls -join ',') -notmatch 'write,publish,approval,merge$'){throw ($script:calls -join ',')}
            $r.Status
            """);
        Assert.Contains("MERGED", output);
    }

    [Fact]
    public async Task Reject_publishes_then_resumes_with_feedback_without_merge()
    {
        using var repo = Sandbox();
        Assert.Contains("REJECTED", await Run(repo, """
            $e.VerifyOk=$false
            $reason='Sửa lỗi tiếng Việt "x"; & | $ ký tự'
            $r=Invoke-WorkflowAccept @argsAccept -Reject $reason
            if($r.Status -cne 'REJECTED' -or $script:decision.Verdict -cne 'CẦN SỬA'){throw 'Sai luồng reject'}
            if(-not $script:resumed[0] -or $script:resumed[1] -cne $reason){throw 'Sai Resume/Feedback'}
            if(($script:calls -join ',') -notmatch 'write,publish,resume$' -or $script:calls -contains 'merge' -or $script:calls -contains 'approval'){throw 'Reject đi vào luồng ĐẠT'}
            $r.Status
            """));
    }

    [Fact]
    public async Task Already_merged_is_idempotent_without_callbacks()
    {
        using var repo = Sandbox();
        Assert.Contains("ALREADY_MERGED", await Run(repo, """
            $ctx.Merged=$true
            1..2|ForEach-Object { $r=Invoke-WorkflowAccept @argsAccept; if($r.Status -cne 'ALREADY_MERGED'){throw 'Sai lặp'} }
            if($script:calls.Count -ne 0){throw 'Đã gọi callback khi merge rồi'}
            $r.Status
            """));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    [InlineData("wrong-sha")]
    [InlineData("push-failed")]
    [InlineData("merge-failed")]
    public async Task Approval_publish_and_merge_failures_never_report_success(string variant)
    {
        using var repo = Sandbox();
        var setup = variant switch
        {
            "push-failed" => "$argsAccept.Publish={param($f) $script:calls+='publish'; @{Ok=$false;Head=$newHead}}",
            "merge-failed" => "$argsAccept.Merge={param($s) $script:calls+='merge'; @{Ok=$false}}",
            _ => "$argsAccept.CheckApproval={param($s) $script:calls+='approval'; $false}"
        };
        Assert.Contains("Không hoàn tất nghiệm thu.", await Run(repo, setup + """

            try { Invoke-WorkflowAccept @argsAccept; throw 'Đã báo thành công giả' }
            catch { if($_.Exception.Message -cne 'Không hoàn tất nghiệm thu.'){throw}; $_.Exception.Message }
            if($script:calls -contains 'resume'){throw 'Giao lại sai luồng'}
            """ + (variant == "merge-failed" ? "" : "\nif($script:calls -contains 'merge'){throw 'Merge khi chưa được phép'}")));
    }

    [Fact]
    public async Task Fake_codex_editing_code_is_blocked_before_publish()
    {
        using var repo = Sandbox();
        Assert.Contains("Codex sửa ngoài mục nghiệm thu cho phép.", await Run(repo, """
            $argsAccept.WriteReview={param($d) @{ 'src/Keep.cs'='Codex sửa code' }}
            try { Invoke-WorkflowAccept @argsAccept; throw 'Đã lọt Codex sửa code' }
            catch { if($_.Exception.Message -cne 'Codex sửa ngoài mục nghiệm thu cho phép.'){throw}; $_.Exception.Message }
            if($script:calls -contains 'publish'){throw 'Đã push code lạ'}
            """));
    }

    [Fact]
    public async Task Real_request_and_projects_are_copied_byte_for_byte_and_not_rewritten()
    {
        using var repo = Sandbox();
        const string request = "R-008-day-viec-khoi-claude-day-chuyen-tu-chay.md";
        foreach (var name in new[] { request, "projects.json", MetricName }) File.Copy(Fixture(name), Path.Combine(repo.Root, name));
        var original = File.ReadAllBytes(Path.Combine(repo.Root, request));
        Assert.Contains("## 3. Nhật ký (", Encoding.UTF8.GetString(original));
        Assert.Contains("\r\n", Encoding.UTF8.GetString(original));
        Assert.StartsWith("[", File.ReadAllText(Path.Combine(repo.Root, "projects.json")).Trim());
        Assert.Contains("V8-03", await Run(repo, "Get-WorkflowAcceptPack @($item)"));
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(repo.Root, request)));
    }
}
