# Khung V8-02 — chỉ điền phần thân, giữ hợp đồng nghiệm thu.
# Giới hạn chờ mỗi tiến trình worker (giây) trước khi dừng cả cây tiến trình và báo ERROR.
$WorkflowPipelineTaskTimeoutSeconds = 14400

# /// <summary>Đọc plan UTF-8 (BOM tùy bản cũ), giữ nguyên file. Chỉ chấp nhận ĐÃ DUYỆT
# /// trong mục Quyết định của quản lý, không có TỪ CHỐI tại mục đó; nếu không, ném
# /// "Kế hoạch chưa được duyệt.". Trả mảng {Id, Title, DependsOn:string[]} theo thứ tự plan.
# /// Mã theo ^[A-Za-z0-9]+-\d+[a-z]?$, duy nhất không phân biệt hoa thường; phụ thuộc
# /// phải có trong plan, không tự tham chiếu/chu trình. Sai ném "Kế hoạch có phụ thuộc không hợp lệ.".
# /// Không thực thi Markdown; chặn reparse point ở file/các thư mục cha trước đọc,
# /// ném "Đường dẫn kế hoạch không an toàn.".</summary>
function Read-WorkflowPipelinePlan {
    param([string]$PlanPath)
    $invalid = 'Kế hoạch có phụ thuộc không hợp lệ.'
    $unsafe = 'Đường dẫn kế hoạch không an toàn.'
    $idRx = '^[A-Za-z0-9]+-[0-9]+[a-z]?\z'
    if ([string]::IsNullOrWhiteSpace($PlanPath) -or (@($PlanPath -split '[\\/]') -contains '..')) { throw $unsafe }
    $full = [IO.Path]::GetFullPath($PlanPath)
    if (-not [IO.File]::Exists($full)) { throw "Không thấy kế hoạch $full" }
    # Chặn junction / symlink ở chính file và mọi thư mục cha trước khi đọc.
    for ($node = $full; $node; $node = [IO.Path]::GetDirectoryName($node)) {
        if (([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw $unsafe }
    }
    $planText = [IO.File]::ReadAllText($full, [Text.Encoding]::UTF8)   # chỉ đọc, không ghi lại plan
    $decision = [regex]::Match($planText, '(?ms)^##[ \t]+Quyết định của quản lý[^\r\n]*\r?\n.*?(?=^##[ \t]|\z)')
    if (-not $decision.Success -or -not $decision.Value.Contains('ĐÃ DUYỆT') -or $decision.Value.Contains('TỪ CHỐI')) {
        throw 'Kế hoạch chưa được duyệt.'
    }
    $section = [regex]::Match($planText, '(?ms)^##[ \t]+Task[^\r\n]*\r?\n(.*?)(?=^##[ \t]|\z)')
    $body = if ($section.Success) { $section.Groups[1].Value } else { $planText }
    $items = New-Object Collections.ArrayList
    $canon = @{}   # khóa Hashtable không phân biệt hoa thường
    foreach ($h in [regex]::Matches($body, '(?ms)^###[ \t]+(?<head>[^\r\n]*)\r?\n?(?<body>.*?)(?=^###[ \t]|\z)')) {
        $hm = [regex]::Match($h.Groups['head'].Value, '^(?<id>[^·]+?)\s*·\s*(?<title>.+?)\s*$')
        if (-not $hm.Success) { throw $invalid }
        $id = $hm.Groups['id'].Value.Trim()
        if ($id -cnotmatch $idRx -or $canon.ContainsKey($id)) { throw $invalid }
        $canon[$id] = $id
        $dm = [regex]::Match($h.Groups['body'].Value, '\*\*Phụ thuộc:\*\*[ \t]*(?<deps>[^\r\n]*)')
        $deps = @()
        if ($dm.Success) { $deps = @($dm.Groups['deps'].Value -split '·' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne '—' }) }
        [void]$items.Add([pscustomobject]@{ Id = $id; Title = $hm.Groups['title'].Value; DependsOn = [string[]]$deps })
    }
    if ($items.Count -eq 0) { throw $invalid }
    foreach ($t in $items) {
        $fixed = @(foreach ($d in $t.DependsOn) {
            if ($d -cnotmatch $idRx -or -not $canon.ContainsKey($d) -or $canon[$d] -eq $t.Id) { throw $invalid }
            $canon[$d]
        })
        $t.DependsOn = [string[]]$fixed
    }
    # Chu trình: DFS ba trạng thái (1 = đang duyệt, 2 = xong).
    $state = @{}
    $visit = {
        param($tid)
        if ($state[$tid] -eq 2) { return }
        if ($state[$tid] -eq 1) { throw $invalid }
        $state[$tid] = 1
        foreach ($d in @($items | Where-Object { $_.Id -eq $tid } | ForEach-Object { $_.DependsOn })) { & $visit $d }
        $state[$tid] = 2
    }
    foreach ($t in $items) { & $visit $t.Id }
    $items.ToArray()
}

# /// <summary>Trả các task chưa có file (TaskExists nhận Id, trả bool), mọi phụ thuộc
# /// có đúng hàng mã task trong BoardText với ✅ Đã merge; không chấp nhận tiền tố mã,
# /// Chờ review/PASS hoặc chỉ ✅. BoardText do adapter đọc origin/main, không đọc bản local.
# /// Không có phụ thuộc thì sẵn sàng. Không sửa Tasks/BoardText hoặc gọi AI.</summary>
function Get-WorkflowReadyTasks {
    param([object[]]$Tasks, [string]$BoardText, [scriptblock]$TaskExists)
    $rows = @(([string]$BoardText -split '\r?\n') | Where-Object { $_ -match '^\s*\|' })
    $merged = {
        param($dep)
        $rx = '^\s*\[?\s*' + [regex]::Escape($dep) + '(?![A-Za-z0-9-])'
        foreach ($row in $rows) {
            $cells = @($row.Trim().Trim('|').Split('|'))
            if ($cells.Count -ge 2 -and $cells[0] -imatch $rx -and
                $cells[$cells.Count - 1].Trim().StartsWith('✅ Đã merge', [StringComparison]::Ordinal)) { return $true }
        }
        $false
    }
    foreach ($t in @($Tasks)) {
        if ([bool](@(& $TaskExists $t.Id) | Select-Object -Last 1)) { continue }
        $ok = $true
        foreach ($d in @($t.DependsOn)) { if (-not (& $merged $d)) { $ok = $false; break } }
        if ($ok) { $t }
    }
}

# /// <summary>Khóa liên tiến trình chung cho toàn bộ Codex trên máy, không theo repo/task.
# /// Chạy Action đúng một lần, trả nguyên kết quả, nhả trong finally kể cả Action ném lỗi
# /// hoặc chủ khóa trước chết (abandoned mutex). Hết TimeoutSeconds ném "Hết thời gian chờ Codex.".
# /// Không giữ khóa giao dịch repo trong khi chờ. Prep/QC/accept dùng cùng hàm này.</summary>
function Invoke-WorkflowCodexLock {
    param([scriptblock]$Action, [int]$TimeoutSeconds = 1800)
    # Mutex có tên là tái nhập trên cùng luồng ⇒ prep/QC gọi lồng trong một worker không tự khóa chết.
    $mutex = [System.Threading.Mutex]::new($false, 'Global\AITeam.Codex')
    $owned = $false
    try {
        try { $owned = $mutex.WaitOne([TimeSpan]::FromSeconds([Math]::Max(0, $TimeoutSeconds))) }
        catch {
            $ex = $_.Exception
            if ($ex -is [System.Threading.AbandonedMutexException] -or $ex.InnerException -is [System.Threading.AbandonedMutexException]) { $owned = $true }
            else { throw }
        }
        if (-not $owned) { throw 'Hết thời gian chờ Codex.' }
        & $Action
    } finally {
        if ($owned) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
}

# /// <summary>Một worker/task: Prep dưới khóa Codex trả {Ok}; Agent nhận Resume:boolean,
# /// Feedback:string và trả {Ok, Quota}; Verify trả {Ok, Feedback}; Qc dưới cùng khóa Codex
# /// trả {Verdict:PASS|CHANGES_REQUESTED|BLOCKED, Feedback}. Mỗi callback chạy đồng bộ.
# /// Agent chỉ Antigravity; quota/lỗi/Prep thất bại thì dừng. Verify trượt giao lại tối đa 1,
# /// QC CHANGES_REQUESTED giao lại tối đa 1; hai bộ đếm riêng không reset, tối đa 3 lượt agent.
# /// QC chỉ sau Verify đạt; BLOCKED/kết quả thiếu hoặc sai dừng, không báo PASS.
# /// Trả {Id, Status} với Status: PASS|VERIFY_FAILED|QC_FAILED|BLOCKED|QUOTA|ERROR.
# /// Callback ném lỗi trả ERROR và nhả khóa, không làm worker khác thất bại.</summary>
function Invoke-WorkflowPipelineTask {
    param([string]$TaskId, [scriptblock]$Prep, [scriptblock]$Agent,
          [scriptblock]$Verify, [scriptblock]$Qc)
    # Tên biến có tiền tố wf: không che tham số script team.ps1 ($Resume, $Feedback…) mà callback đọc.
    $wfResult = { param($s) [pscustomobject]@{ Id = $TaskId; Status = $s } }
    $wfAgentRuns = 0; $wfVerifyRetried = $false; $wfQcRetried = $false
    $wfResume = $false; $wfFeedback = ''
    try {
        $wfPrep = @(Invoke-WorkflowCodexLock -Action $Prep) | Select-Object -Last 1
        if (-not $wfPrep -or -not $wfPrep.Ok) { return (& $wfResult 'ERROR') }
        while ($true) {
            $wfAgentRuns++
            $wfAgentResult = @(& $Agent $wfResume $wfFeedback) | Select-Object -Last 1
            if ($wfAgentResult -and $wfAgentResult.Quota) { return (& $wfResult 'QUOTA') }
            if (-not $wfAgentResult -or -not $wfAgentResult.Ok) { return (& $wfResult 'ERROR') }
            $wfVerifyResult = @(& $Verify) | Select-Object -Last 1
            if (-not $wfVerifyResult -or -not $wfVerifyResult.Ok) {
                if ($wfVerifyRetried -or $wfAgentRuns -ge 3) { return (& $wfResult 'VERIFY_FAILED') }
                $wfVerifyRetried = $true; $wfResume = $true
                $wfFeedback = if ($wfVerifyResult -and $wfVerifyResult.Feedback) { [string]$wfVerifyResult.Feedback } else { 'Cổng kiểm tra tự động KHÔNG ĐẠT.' }
                continue
            }
            $wfQcResult = @(Invoke-WorkflowCodexLock -Action $Qc) | Select-Object -Last 1
            $wfVerdict = if ($wfQcResult) { [string]$wfQcResult.Verdict } else { '' }
            if ($wfVerdict -ceq 'PASS') { return (& $wfResult 'PASS') }
            if ($wfVerdict -cne 'CHANGES_REQUESTED') { return (& $wfResult 'BLOCKED') }
            if ($wfQcRetried -or $wfAgentRuns -ge 3) { return (& $wfResult 'QC_FAILED') }
            $wfQcRetried = $true; $wfResume = $true   # không reset cờ cổng KT
            $wfFeedback = if ($wfQcResult.Feedback) { [string]$wfQcResult.Feedback } else { 'Reviewer AI yêu cầu sửa.' }
        }
    } catch {
        Write-Verbose "Worker $TaskId thất bại: $($_.Exception.Message)"
        return (& $wfResult 'ERROR')
    }
}

# /// <summary>Lõi điều phối: kiểm RequestId ^R-\d{3}$ (sai: "Thiếu / sai -Request (vd. R-006)")
# /// và toàn bộ plan trước callback. Khóa liên tiến trình ProjectKey+RequestId; lượt trùng
# /// trả rỗng, không gọi callback. Cố định SHA256 plan, đổi giữa lượt thì dừng khởi chạy mới.
# /// ReadBoard trả văn bản bảng việc origin/main mới; TaskExists nhận Id, trả bool.
# /// StartTask nhận task, trả System.Diagnostics.Process đã Start, redirect log riêng, không AI thật trong test.
# /// Tối đa 2 tiến trình đồng thời, đợi lượt đang chạy rồi đọc lại bảng và xét tiếp.
# /// Mỗi Id chỉ khởi chạy 1 lần; task có sẵn trả EXISTING; thiếu merge trả WAITING;
# /// exit 0 trả DONE, khác 0 trả ERROR. Trả đúng một {Id,Status} mỗi task theo thứ tự plan,
# /// không phát log callback ra console. Không merge, không tự assign task có file.
# /// Nhả khóa trong finally; child lỗi không làm mất kết quả task khác.</summary>
function Invoke-WorkflowPipeline {
    param([string]$PlanPath, [string]$ProjectKey, [string]$RequestId,
          [scriptblock]$ReadBoard, [scriptblock]$TaskExists, [scriptblock]$StartTask)
    if ([string]$RequestId -cnotmatch '^R-[0-9]{3}\z') { throw 'Thiếu / sai -Request (vd. R-006)' }
    $wfSha = { param($bytes) $h = [Security.Cryptography.SHA256]::Create(); try { [BitConverter]::ToString($h.ComputeHash([byte[]]$bytes)).Replace('-', '') } finally { $h.Dispose() } }
    $wfPlanFull = [IO.Path]::GetFullPath($PlanPath)
    $wfTasks = @(Read-WorkflowPipelinePlan $PlanPath)   # kiểm toàn bộ plan trước mọi callback
    $wfPlanHash = & $wfSha ([IO.File]::ReadAllBytes($wfPlanFull))
    $wfChanged = {
        if (-not [IO.File]::Exists($wfPlanFull)) { return $true }
        (& $wfSha ([IO.File]::ReadAllBytes($wfPlanFull))) -ne $wfPlanHash
    }
    $wfKey = [string]$ProjectKey
    if ($wfKey -and [IO.Path]::IsPathRooted($wfKey)) { $wfKey = [IO.Path]::GetFullPath($wfKey).TrimEnd('\', '/') }
    $wfKeyHash = & $wfSha ([Text.Encoding]::UTF8.GetBytes($wfKey.ToLowerInvariant() + '|' + $RequestId.ToUpperInvariant()))
    $wfTimeout = [int]$WorkflowPipelineTaskTimeoutSeconds
    if ($wfTimeout -le 0) { $wfTimeout = 14400 }
    $wfKill = {
        param($proc)
        if ($proc.HasExited) { return }
        $ki = New-Object Diagnostics.ProcessStartInfo
        $ki.FileName = 'taskkill.exe'; $ki.Arguments = "/T /F /PID $($proc.Id)"
        $ki.UseShellExecute = $false; $ki.CreateNoWindow = $true
        $ki.RedirectStandardOutput = $true; $ki.RedirectStandardError = $true
        $killer = [Diagnostics.Process]::Start($ki)
        try {
            $ko = $killer.StandardOutput.ReadToEndAsync(); $ke = $killer.StandardError.ReadToEndAsync()
            if (-not $killer.WaitForExit(5000)) { $killer.Kill() }
        } finally { $killer.Dispose() }
        if (-not $proc.WaitForExit(5000)) { $proc.Kill() }
    }
    $wfMutex = [System.Threading.Mutex]::new($false, "Global\AITeam.Pipeline.$wfKeyHash")
    $wfOwned = $false
    $wfRunning = New-Object Collections.ArrayList
    try {
        try { $wfOwned = $wfMutex.WaitOne(0) }
        catch {
            $ex = $_.Exception
            if ($ex -is [System.Threading.AbandonedMutexException] -or $ex.InnerException -is [System.Threading.AbandonedMutexException]) { $wfOwned = $true }
            else { throw }
        }
        if (-not $wfOwned) { return }   # pipeline cùng dự án + phiếu đang chạy
        $wfExisting = @{}; $wfStatus = @{}
        foreach ($t in $wfTasks) { if ([bool](@(& $TaskExists $t.Id) | Select-Object -Last 1)) { $wfExisting[$t.Id] = $true } }
        $wfStop = $false
        while ($true) {
            if (-not $wfStop) {
                $wfBoard = (@(& $ReadBoard) | ForEach-Object { [string]$_ }) -join "`n"
                foreach ($t in @(Get-WorkflowReadyTasks -Tasks $wfTasks -BoardText $wfBoard -TaskExists $TaskExists)) {
                    if ($wfRunning.Count -ge 2) { break }
                    if ($wfStatus.ContainsKey($t.Id) -or $wfExisting.ContainsKey($t.Id)) { continue }
                    if (& $wfChanged) { $wfStop = $true; break }
                    $wfStatus[$t.Id] = 'ERROR'   # mỗi Id chỉ khởi chạy một lần
                    try { $wfProc = @(& $StartTask $t) | Where-Object { $_ -is [System.Diagnostics.Process] } | Select-Object -Last 1 }
                    catch { Write-Verbose "Không khởi chạy được $($t.Id): $($_.Exception.Message)"; continue }
                    if ($wfProc) {
                        # Giữ handle ngay để đọc được ExitCode sau khi tiến trình (Start-Process -PassThru) kết thúc.
                        try { $null = $wfProc.Handle } catch { Write-Verbose "Không giữ được handle tiến trình $($t.Id): $($_.Exception.Message)" }
                        [void]$wfRunning.Add([pscustomobject]@{ Id = $t.Id; Proc = $wfProc; Started = [DateTime]::UtcNow })
                    }
                }
                if (-not $wfStop -and (& $wfChanged)) { $wfStop = $true }
            }
            if ($wfRunning.Count -eq 0) { break }
            $wfDone = $null; $wfTimedOut = $false
            while (-not $wfDone) {
                foreach ($w in $wfRunning) {
                    if ($w.Proc.HasExited) { $wfDone = $w; break }
                    if (([DateTime]::UtcNow - $w.Started).TotalSeconds -gt $wfTimeout) { & $wfKill $w.Proc; $wfDone = $w; $wfTimedOut = $true; break }
                }
                if (-not $wfDone) { Start-Sleep -Milliseconds 100 }
            }
            [void]$wfRunning.Remove($wfDone)
            $wfDone.Proc.WaitForExit()
            $wfStatus[$wfDone.Id] = if (-not $wfTimedOut -and $wfDone.Proc.ExitCode -eq 0) { 'DONE' } else { 'ERROR' }
            $wfDone.Proc.Dispose()
        }
        foreach ($t in $wfTasks) {
            $s = if ($wfStatus.ContainsKey($t.Id)) { $wfStatus[$t.Id] } elseif ($wfExisting.ContainsKey($t.Id)) { 'EXISTING' } else { 'WAITING' }
            [pscustomobject]@{ Id = $t.Id; Status = $s }
        }
    } finally {
        foreach ($w in @($wfRunning)) { try { & $wfKill $w.Proc } finally { $w.Proc.Dispose() } }   # không để child mồ côi khi lỗi
        if ($wfOwned) { $wfMutex.ReleaseMutex() }
        $wfMutex.Dispose()
    }
}
