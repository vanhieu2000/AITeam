using AITeam.Core.Tests.Support;
using System.Text;

namespace AITeam.Core.Tests;

// Fixture là bản sao nguyên byte plan/bảng việc thật; mọi biến thể chỉ sửa bản tạm.
public sealed class AcceptanceV802PipelineTests
{
    [Fact]
    public Task Doc_plan_that_va_phu_thuoc() => Check("""
        $tasks = @(Read-WorkflowPipelinePlan $plan)
        Need ($tasks.Count -eq 3) 'Phải đọc đủ ba task thật'
        Need ($tasks[1].Id -eq 'V8-02') 'Sai mã task'
        Need ($tasks[1].Title -eq 'Điều phối pipeline theo phụ thuộc') 'Sai tiêu đề tiếng Việt'
        Need (($tasks[2].DependsOn -join ',') -eq 'V8-01,V8-02') 'Mất phụ thuộc'
        """);

    [Fact]
    public Task Plan_cu_khong_BOM_va_CRLF_khong_bi_ghi_lai() => Check("""
        $text = [IO.File]::ReadAllText($plan) -replace '\r?\n',"`r`n"
        [IO.File]::WriteAllText($plan,$text,(New-Object Text.UTF8Encoding $false))
        $before = [Convert]::ToBase64String([IO.File]::ReadAllBytes($plan))
        Need (@(Read-WorkflowPipelinePlan $plan).Count -eq 3) 'Không đọc plan cũ'
        Need ([Convert]::ToBase64String([IO.File]::ReadAllBytes($plan)) -ceq $before) 'Đã sửa BOM/xuống dòng của plan'
        """);

    [Fact]
    public Task Chan_reparse_point_o_thu_muc_cha_cua_plan() => Check("""
        $real = Join-Path $caseRoot 'real'; [IO.Directory]::CreateDirectory($real) | Out-Null
        Copy-Item $plan (Join-Path $real 'plan.md')
        $link = Join-Path $caseRoot 'link'
        New-Item -ItemType Junction -Path $link -Target $real | Out-Null
        try {
            Expect-Error { Read-WorkflowPipelinePlan (Join-Path $link 'plan.md') } 'Đường dẫn kế hoạch không an toàn.'
        } finally { [IO.Directory]::Delete($link) }
        """);

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    [InlineData("outside")]
    public Task Chi_tin_muc_quyet_dinh_duyet(string variant) => Check($$"""
        $text = [IO.File]::ReadAllText($plan)
        switch ('{{variant}}') {
            pending { $text = $text.Replace('ĐÃ DUYỆT','CHỜ DUYỆT') }
            rejected { $text += "`r`n- TỪ CHỐI triển khai." }
            outside { $text = $text -replace '(?ms)^## Quyết định của quản lý.*\z',''; $text = "ĐÃ DUYỆT`r`n" + $text }
        }
        Save-Plan $text
        Expect-Error { Read-WorkflowPipelinePlan $plan } 'Kế hoạch chưa được duyệt.'
        """);

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("cycle")]
    [InlineData("self")]
    [InlineData("path")]
    public Task Tu_choi_toan_bo_do_thi_truoc_chay(string variant) => Check($$"""
        $text = [IO.File]::ReadAllText($plan)
        switch ('{{variant}}') {
            duplicate { $text = $text.Replace('### V8-03','### v8-02') }
            missing { $text = $text.Replace('**Phụ thuộc:** V8-01 · V8-02','**Phụ thuộc:** V8-99') }
            cycle { $text = $text.Replace('**Phụ thuộc:** —','**Phụ thuộc:** V8-03') }
            self { $text = $text.Replace('**Phụ thuộc:** —','**Phụ thuộc:** V8-01') }
            path { $text = $text.Replace('### V8-01','### ../V8-01') }
        }
        Save-Plan $text
        Expect-Error { Invoke-WorkflowPipeline @pipelineArgs } 'Kế hoạch có phụ thuộc không hợp lệ.'
        Need (@(Get-ChildItem $trace -Filter '*.start').Count -eq 0) 'Đã chạy trước khi kiểm tra đủ plan'
        """);

    [Theory]
    [InlineData("R-008/../../x")]
    [InlineData("C:\\R-008")]
    [InlineData("")]
    public Task Chan_request_khong_hop_le(string request) => Check($$"""
        $pipelineArgs.RequestId = '{{request}}'
        Expect-Error { Invoke-WorkflowPipeline @pipelineArgs } 'Thiếu / sai -Request (vd. R-006)'
        Need (@(Get-ChildItem $trace -Filter '*.start').Count -eq 0) 'Đã gọi worker với request sai'
        """);

    [Theory]
    [InlineData("merged", 1)]
    [InlineData("review", 0)]
    [InlineData("prefix", 0)]
    [InlineData("tick", 0)]
    public Task Phu_thuoc_chi_san_sang_khi_dung_hang_da_merge(string variant, int count) => Check($$"""
        $board = [IO.File]::ReadAllText($boardFile)
        switch ('{{variant}}') {
            review { $board = $board.Replace('✅ Đã merge','🔵 Chờ review · AI review PASS') }
            prefix { $board = $board.Replace('V8-01','V8-010') }
            tick { $board = $board.Replace('✅ Đã merge','✅') }
        }
        $ready = @(Get-WorkflowReadyTasks -Tasks @(Read-WorkflowPipelinePlan $plan) -BoardText $board -TaskExists { param($id) $id -eq 'V8-01' })
        Need ($ready.Count -eq {{count}}) 'Nhận sai trạng thái merge'
        if ($ready.Count -gt 0) { Need ($ready[0].Id -eq 'V8-02') 'Nhận sai task sẵn sàng' }
        """);

    [Fact]
    public Task Hai_task_chay_chong_task_thu_ba_cho_va_moi_task_mot_ket_qua() => Check("""
        Independent-Plan
        $pipelineArgs.StartTask = { param($item) Start-Fake $item.Id 1400 0 -Together }
        $rows = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need ($rows.Count -eq 3) 'Phải có đúng một kết quả mỗi task'
        Need (($rows.Id -join ',') -eq 'V8-01,V8-02,V8-03') 'Sai thứ tự/kết quả bị lẫn log'
        Need (@($rows | Where-Object Status -eq DONE).Count -eq 3) 'Không chạy đủ task'
        $a = Interval V8-01; $b = Interval V8-02; $c = Interval V8-03
        Need ([Math]::Max($a[0],$b[0]) -lt [Math]::Min($a[1],$b[1])) 'Hai task độc lập không chạy chồng'
        Need ($c[0] -ge [Math]::Min($a[1],$b[1])) 'Task thứ ba vượt giới hạn hai tiến trình'
        """);

    [Fact]
    public Task Task_co_file_khong_chay_lai_va_phu_thuoc_cho_merge() => Check("""
        $pipelineArgs.TaskExists = { param($id) $id -eq 'V8-01' }
        $pipelineArgs.ReadBoard = { [IO.File]::ReadAllText($boardFile).Replace('✅ Đã merge','🔵 Chờ review') }
        $rows = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need (($rows.Status -join ',') -eq 'EXISTING,WAITING,WAITING') 'Tự chạy lại task hoặc nhận nhầm QC là merge'
        Need (@(Get-ChildItem $trace -Filter '*.start').Count -eq 0) 'Đã tự prep/assign task có file'
        """);

    [Fact]
    public Task Doc_lai_bang_origin_sau_luot_nhung_khong_tu_merge() => Check("""
        $pipelineArgs.TaskExists = { param($id) $id -eq 'V8-01' }
        $rows = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need (($rows.Status -join ',') -eq 'EXISTING,DONE,WAITING') 'Phụ thuộc đã QC nhưng chưa merge phải chờ'
        Need (-not (Test-Path (Join-Path $trace 'V8-03.start'))) 'Đã chạy task phụ thuộc chưa merge'
        """);

    [Fact]
    public Task Plan_doi_giua_luot_khong_khoi_chay_task_moi() => Check("""
        Independent-Plan
        $pipelineArgs.StartTask = { param($item)
            $p = Start-Fake $item.Id 900 0
            [IO.File]::AppendAllText($plan,"`r`n<!-- plan đã đổi -->")
            $p
        }
        $rows = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need (@(Get-ChildItem $trace -Filter '*.start').Count -eq 1) 'Tiếp tục khởi chạy trên plan đã đổi'
        Need ($rows.Count -eq 3) 'Mất trạng thái task chưa chạy'
        """);

    [Theory]
    [InlineData("pass", 1, 1, 1, "PASS")]
    [InlineData("verify", 2, 2, 0, "VERIFY_FAILED")]
    [InlineData("qc", 2, 2, 2, "QC_FAILED")]
    [InlineData("mixed", 3, 3, 1, "VERIFY_FAILED")]
    [InlineData("quota", 1, 0, 0, "QUOTA")]
    [InlineData("blocked", 1, 1, 1, "BLOCKED")]
    [InlineData("error", 1, 0, 0, "ERROR")]
    [InlineData("prep-fail", 0, 0, 0, "ERROR")]
    [InlineData("resume-prep", 1, 1, 1, "PASS")]
    public Task Bo_dem_rieng_khong_reset_khong_fallback(string mode, int agents, int verifies, int reviews, string status) => Check($$"""
        $script:ac = 0; $script:vc = 0; $script:qc = 0; $script:pc = 0
        $mode = '{{mode}}'
        $r = Invoke-WorkflowPipelineTask -TaskId V8-02 -Prep {
            $script:pc++
            if ($mode -eq 'resume-prep') { [IO.File]::WriteAllText((Join-Path $trace 'old-prep'),'khung giữ lại') }
            Stage 'prep' $script:pc
            @{Ok=($mode -ne 'prep-fail')}
        } -Agent {
            param($resume,$feedback)
            $script:ac++; Stage 'agy' $script:ac
            Need ($resume -eq ($script:ac -gt 1)) 'Sai cờ Resume'
            if ($resume) { Need (-not [string]::IsNullOrWhiteSpace($feedback)) 'Thiếu phản hồi giao lại' }
            if ($mode -eq 'error') { throw 'Tiến trình giả lỗi' }
            @{Ok=($mode -ne 'quota');Quota=($mode -eq 'quota')}
        } -Verify {
            $script:vc++; Stage 'verify' $script:vc
            @{Ok=($mode -ne 'verify' -and -not ($mode -eq 'mixed' -and $script:vc -ne 2));Feedback='Cổng KT cần sửa'}
        } -Qc {
            $script:qc++; Stage 'qc' $script:qc
            $verdict = if ($mode -in @('qc','mixed')) {'CHANGES_REQUESTED'} elseif ($mode -eq 'blocked') {'BLOCKED'} else {'PASS'}
            @{Verdict=$verdict;Feedback='QC cần sửa'}
        }
        Need ($r.Id -eq 'V8-02' -and $r.Status -eq '{{status}}') 'Sai kết quả worker'
        Need ($script:pc -eq 1) 'Prep lặp theo lượt sửa'
        Need ($script:ac -eq {{agents}} -and $script:vc -eq {{verifies}} -and $script:qc -eq {{reviews}}) 'Sai số lượt callback'
        Need (@(Get-ChildItem $trace -Filter 'agy*.start').Count -eq {{agents}}) 'Sai số tiến trình Antigravity giả thực tế'
        Need (@(Get-ChildItem $trace -Filter 'qc*.start').Count -eq {{reviews}}) 'Sai số tiến trình QC giả thực tế'
        Need (-not (Test-Path (Join-Path $trace 'codex-dev.start'))) 'Đã chuyển Codex viết code'
        """);

    [Fact]
    public Task Khoa_codex_lien_tien_trinh_va_nha_sau_exception() => Check("""
        $a = Start-Fake prep-lock 1100 0 -Locked
        $b = Start-Fake qc-lock 1100 0 -Locked
        $a.WaitForExit(); $b.WaitForExit()
        Need ($a.ExitCode -eq 0 -and $b.ExitCode -eq 0) 'Khóa làm tiến trình lỗi'
        $x = Interval prep-lock; $y = Interval qc-lock
        Need ($x[1] -le $y[0] -or $y[1] -le $x[0]) 'Prep và QC chồng Codex'
        Expect-Error { Invoke-WorkflowCodexLock -Action { throw 'lỗi giả' } } 'lỗi giả'
        $value = Invoke-WorkflowCodexLock -TimeoutSeconds 2 -Action { 42 }
        Need ($value -eq 42) 'Không nhả khóa sau exception'
        """);

    [Fact]
    public Task Tien_trinh_chet_nha_khoa_codex() => Check("""
        $p = Start-Fake crashed 100 7 -Locked
        $p.WaitForExit()
        Need ($p.ExitCode -eq 7) 'Tiến trình giả chưa chết như dự kiến'
        Need ((Invoke-WorkflowCodexLock -TimeoutSeconds 2 -Action { 42 }) -eq 42) 'Không thu hồi khóa bỏ rơi'
        """);

    [Fact]
    public Task Worker_that_boc_khoa_cho_ca_prep_va_qc() => Check("""
        $a = Start-Fake worker-a 800 0 -Worker
        $b = Start-Fake worker-b 800 0 -Worker
        $a.WaitForExit(); $b.WaitForExit()
        Need ($a.ExitCode -eq 0 -and $b.ExitCode -eq 0) 'Worker lỗi'
        $names = @('worker-a-prep','worker-a-qc','worker-b-prep','worker-b-qc')
        for ($i=0;$i -lt $names.Count;$i++) {
            for ($j=$i+1;$j -lt $names.Count;$j++) {
                $x = Interval $names[$i]; $y = Interval $names[$j]
                Need ($x[1] -le $y[0] -or $y[1] -le $x[0]) 'Worker chưa bọc khóa chung cho prep/QC'
            }
        }
        """);

    [Fact]
    public Task Child_loi_khong_mat_ket_qua_va_nha_khoa_pipeline() => Check("""
        Independent-Plan
        $pipelineArgs.StartTask = { param($item) Start-Fake $item.Id 50 9 }
        $first = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need (@($first | Where-Object Status -eq ERROR).Count -eq 3) 'Mất lỗi child'
        $pipelineArgs.StartTask = { param($item) Start-Fake $item.Id 50 0 }
        $second = @(Invoke-WorkflowPipeline @pipelineArgs)
        Need (@($second | Where-Object Status -eq DONE).Count -eq 3) 'Pipeline giữ khóa sau child lỗi'
        """);

    [Fact]
    public Task Hai_pipeline_cung_phieu_khong_chay_trung() => Check("""
        Independent-Plan
        $driver = Join-Path $caseRoot 'driver.ps1'
        $driverText = [IO.File]::ReadAllText($PSCommandPath)
        $marker = '# NOI_DUNG_CA_TEST'
        $driverText = $driverText.Substring(0,$driverText.IndexOf($marker)) + "`r`nInvoke-WorkflowPipeline @pipelineArgs | Out-Null"
        [IO.File]::WriteAllText($driver,$driverText,(New-Object Text.UTF8Encoding $true))
        $one = Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$driver+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $caseRoot 'one.out') -RedirectStandardError (Join-Path $caseRoot 'one.err')
        $two = Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$driver+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $caseRoot 'two.out') -RedirectStandardError (Join-Path $caseRoot 'two.err')
        $null = $one.Handle; $null = $two.Handle
        $one.WaitForExit(); $two.WaitForExit()
        Need ($one.ExitCode -eq 0 -and $two.ExitCode -eq 0) 'Pipeline trùng lỗi'
        Need (@(Get-ChildItem $trace -Filter '*.event').Count -eq 3) 'Pipeline cùng phiếu đã chạy trùng'
        """);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Adapter_CLI_doc_origin_main_va_chan_fetch_loi(bool failFetch) => Check($$"""
        $Root = $caseRoot; $Project = 'AITeam'; $Request = 'R-008'
        $Work = Join-Path $caseRoot 'work'
        [IO.Directory]::CreateDirectory((Join-Path $caseRoot 'tasks')) | Out-Null
        [IO.File]::WriteAllText((Join-Path $caseRoot 'tasks/README.md'),'Bảng local khác hoàn toàn')
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $caseRoot 'team-source.ps1'),[ref]$null,[ref]$null)
        $fn = $ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Invoke-Pipeline'},$true)
        Need ($null -ne $fn) 'Thiếu adapter pipeline'
        . ([scriptblock]::Create($fn.Extent.Text))
        function Resolve-Project { [pscustomobject]@{Path=$caseRoot;Work=$Work;Logs=(Join-Path $Work 'logs');Info=[pscustomobject]@{name='AITeam'} } }
        function Get-PlanDir { $caseRoot }
        function Say($message) { Write-Output $message }
        $script:gitCalls = @(); $script:coreCalled = $false
        function git {
            $script:gitCalls += ($args -join ' ')
            $global:LASTEXITCODE = 0
            if ($args -contains 'fetch' -and ${{failFetch.ToString().ToLowerInvariant()}}) { $global:LASTEXITCODE = 1; return }
            if ($args -contains 'show') { [IO.File]::ReadAllText($boardFile) }
        }
        function Invoke-WorkflowPipeline {
            param($PlanPath,$ProjectKey,$RequestId,$ReadBoard,$TaskExists,$StartTask)
            $script:coreCalled = $true
            $actual = & $ReadBoard
            Need (($actual -join "`r`n").Contains('✅ Đã merge')) 'Adapter dùng bảng local hoặc mất dữ liệu origin'
            Need (-not (($actual -join "`r`n").Contains('Bảng local khác'))) 'Adapter đọc bảng local'
            [pscustomobject]@{Id='V8-02';Status='WAITING'}
        }
        if (${{failFetch.ToString().ToLowerInvariant()}}) {
            $caught = $null
            try { Invoke-Pipeline | Out-Null } catch { $caught = $_ }
            Need ($null -ne $caught) 'Fetch lỗi phải báo lỗi'
        } else {
            $rows = @(Invoke-Pipeline)
            Need ($script:coreCalled) 'CLI không gọi lõi điều phối'
            Need (($rows -join "`n").Contains('V8-02')) 'CLI không in kết quả task'
        }
        Need (($script:gitCalls -join "`n").Contains('fetch')) 'Không fetch origin'
        if (-not ${{failFetch.ToString().ToLowerInvariant()}}) {
            Need (($script:gitCalls -join "`n").Contains('origin/main:tasks/README.md')) 'Không đọc bảng origin/main'
        }
        """);

    private static async Task Check(string body)
    {
        using var repo = new TempRepo(false);
        var root = repo.Root;
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "V802", "plan.md"), Path.Combine(root, "plan.md"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "V802", "board.md"), Path.Combine(root, "board.md"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "V802Runtime", "workflow-pipeline.ps1"), Path.Combine(root, "module.ps1"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "S1-06Runtime", "team.ps1"), Path.Combine(root, "team-source.ps1"));
        File.WriteAllText(Path.Combine(root, "fake.ps1"), Fake, new UTF8Encoding(true));
        // Dùng timeout chuẩn 60 giây: ca mixed khởi chạy tối đa tám PowerShell con.
        var result = await TestShell.PowerShell(root, Harness + "\r\n# NOI_DUNG_CA_TEST\r\n" + body);
        Assert.True(result.Code == 0, $"PowerShell mã {result.Code}\n{result.Output}\n{result.Error}");
    }

    private const string Harness = """
        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
        $caseRoot = (Get-Location).Path
        $plan = Join-Path $caseRoot 'plan.md'; $boardFile = Join-Path $caseRoot 'board.md'
        $trace = Join-Path $caseRoot 'trace'; [IO.Directory]::CreateDirectory($trace) | Out-Null
        . (Join-Path $caseRoot 'module.ps1')
        function Need($ok,$message) { if (-not $ok) { throw $message } }
        function Expect-Error([scriptblock]$action,$message) {
            $caught = $null
            try { & $action | Out-Null } catch { $caught = $_.Exception.Message }
            Need ($null -ne $caught -and $caught.Contains($message)) "Cần lỗi '$message', thực tế '$caught'"
        }
        function Save-Plan($text) { [IO.File]::WriteAllText($plan,$text,(New-Object Text.UTF8Encoding $true)) }
        function Independent-Plan { Save-Plan ([regex]::Replace([IO.File]::ReadAllText($plan),'(?m)(\*\*Phụ thuộc:\*\*)[^\r\n]*','$1 —')) }
        function Start-Fake($name,[int]$delay=900,[int]$code=0,[switch]$Locked,[switch]$Worker,[switch]$Together) {
            $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+(Join-Path $caseRoot 'fake.ps1')+'"'),'-Name',$name,'-Delay',$delay,'-Code',$code)
            if ($Locked) { $arguments += '-Locked' }
            if ($Worker) { $arguments += '-Worker' }
            if ($Together) { $arguments += '-Together' }
            $p = Start-Process powershell.exe -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $trace "$name.out") -RedirectStandardError (Join-Path $trace "$name.err")
            # Giữ handle trước khi child thoát để đọc ExitCode trên PowerShell 5.1.
            $null = $p.Handle
            $p
        }
        function Stage($name,$number) { $p = Start-Fake "$name$number" 20 0; $p.WaitForExit(); Need ($p.ExitCode -eq 0) 'Tiến trình giai đoạn giả lỗi'; $p.Dispose() }
        function Interval($name) { @([long][IO.File]::ReadAllText((Join-Path $trace "$name.start")),[long][IO.File]::ReadAllText((Join-Path $trace "$name.end"))) }
        $pipelineArgs = @{
            PlanPath=$plan;ProjectKey=$caseRoot;RequestId='R-008'
            ReadBoard={ [IO.File]::ReadAllText($boardFile) }
            TaskExists={ param($id) $false }
            StartTask={ param($item) Start-Fake $item.Id 1400 0 }
        }
        """;

    private const string Fake = """
        param([string]$Name,[int]$Delay,[int]$Code,[switch]$Locked,[switch]$Worker,[switch]$Together)
        $ErrorActionPreference = 'Stop'
        [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
        . (Join-Path $PSScriptRoot 'module.ps1')
        # Đồng hồ logic dùng chung: giữ thứ tự sự kiện, không phụ thuộc độ phân giải giờ Windows.
        function Write-TraceStamp([string]$file) {
            $clockName = 'Local\AITeam.TestClock.' + (Split-Path $PSScriptRoot -Leaf)
            $clockLock = New-Object Threading.Mutex($false, $clockName)
            $owned = $false
            try {
                $owned = $clockLock.WaitOne(20000)
                if (-not $owned) { throw 'Không lấy được khóa đồng hồ test.' }
                $clockFile = Join-Path $PSScriptRoot 'clock.txt'
                $tick = if (Test-Path $clockFile) { [long][IO.File]::ReadAllText($clockFile) + 1 } else { 1 }
                [IO.File]::WriteAllText($clockFile, [string]$tick)
                [IO.File]::WriteAllText($file, [string]$tick)
            } finally { if ($owned) { $clockLock.ReleaseMutex() }; $clockLock.Dispose() }
        }
        $script:fakeAction = {
            $trace = Join-Path $PSScriptRoot 'trace'
            Write-TraceStamp (Join-Path $trace "$Name.start")
            [IO.File]::WriteAllText((Join-Path $trace "$Name-$PID.event"),'đã gọi tiến trình giả')
            if ($Together) {
                # Chỉ kết thúc khi cả hai child thực sự đã ghi bắt đầu; task thứ ba thấy dấu có sẵn.
                $ready = [Threading.SpinWait]::SpinUntil([Func[bool]]{ @(Get-ChildItem $trace -Filter '*.start').Count -ge 2 }, 20000)
                if (-not $ready) { throw 'Không có hai child thực sự bắt đầu trong giới hạn.' }
            } else {
                # Đây là khối lượng công việc giả cho test tranh chấp khóa, không dùng để đoán child đã sẵn sàng.
                Start-Sleep -Milliseconds $Delay
            }
            if ($Code -ne 0) { [Environment]::Exit($Code) }
            Write-TraceStamp (Join-Path $trace "$Name.end")
            Write-Output 'Log dài tiếng Việt chỉ lưu trong file'
        }
        if ($Worker) {
            $workerName = $Name
            $result = Invoke-WorkflowPipelineTask -TaskId $workerName -Prep {
                $Name = "$workerName-prep"; & $script:fakeAction | Out-Null; @{Ok=$true}
            } -Agent { @{Ok=$true;Quota=$false} } -Verify { @{Ok=$true} } -Qc {
                $Name = "$workerName-qc"; & $script:fakeAction | Out-Null; @{Verdict='PASS'}
            }
            if ($result.Status -ne 'PASS') { exit 1 }
        } elseif ($Locked) { Invoke-WorkflowCodexLock -Action $script:fakeAction } else { & $script:fakeAction }
        """;
}
