# Khung V8-01: hợp đồng đã chốt; agent chỉ điền phần TODO.
# /// <summary>Chỉ trả true khi Code khác 0, mọi dòng lỗi đều thuộc danh sách hạ tầng.
# /// Build/Test: MSB3491 kèm denied/being used; Access to the path '…' is denied;
# /// The process cannot access the file '…' because it is being used by another process.
# /// MCP: chỉ dòng chính xác MCP transport error: Connection reset by peer (os error 10054).
# /// Lỗi code, warning code (trừ NU1900), test fail/hang, lỗi khác hoặc output rỗng luôn false.</summary>
function Test-InfrastructureFailure {
    param([ValidateSet('Build', 'Test', 'Mcp')][string]$Stage, [int]$Code, [string]$Output)
    if ($Code -eq 0 -or [string]::IsNullOrEmpty($Output)) { return $false }

    $lines = @($Output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -eq 0) { return $false }

    if ($Stage -eq 'Mcp') {
        $mcpTarget = 'MCP transport error: Connection reset by peer (os error 10054).'
        $hasMcp = $false
        foreach ($l in $lines) {
            $trimmed = $l.Trim()
            if ($trimmed -eq $mcpTarget) {
                $hasMcp = $true
            } else {
                if ($trimmed -match '(?i)error|fail|timeout|denied') { return $false }
            }
        }
        return $hasMcp
    }

    # Build / Test: kiểm tra từng dòng theo allowlist khắt khe
    $hasInfra = $false
    foreach ($l in $lines) {
        $trimmed = $l.Trim()

        # 1. Warning: chỉ cho phép duy nhất NU1900
        if ($trimmed -match ':\s*warning\s+(?!NU1900\b)[A-Za-z0-9]+:' -or $trimmed -match 'warning\s+CS\d+' -or $trimmed -match '\bCS\d{4}\b') {
            return $false
        }

        # 2. Test fail / hang
        if ($trimmed -match 'Failed!\s+Failed:\s*[1-9]' -or $trimmed -match 'The active test run was aborted' -or $trimmed -match '(?i)blame-hang|hang timeout') {
            return $false
        }

        # 3. Dòng lỗi hạ tầng hợp lệ
        $isInfra = ($trimmed -match 'MSB3491.*?(denied|being used)') -or
                   ($trimmed -match "Access to the path '[^']+' is denied\.") -or
                   ($trimmed -match "The process cannot access the file '[^']+' because it is being used by another process\.")
        if ($isInfra) {
            $hasInfra = $true
            continue
        }

        # 4. Bất kỳ lỗi code / build / NuGet / MSBuild / runtime khác ngoài danh sách hạ tầng cho phép
        if ($trimmed -match ':\s*error\s+[A-Za-z0-9]+:' -or $trimmed -match '^\s*error\s+[A-Za-z0-9]+:' -or $trimmed -match '(?i)\berror\s+(NU|MSB|CS|FS|TS)\d+:' -or $trimmed -match '(?i)\b(error|syntaxerror)\b') {
            return $false
        }
        if ($trimmed -match '(?i)\b(exception|failed)\b' -and $trimmed -notmatch '(?i)(0\s+failed|0\s+error)') {
            return $false
        }
    }

    return $hasInfra
}

# /// <summary>Gọi Run với số lần 1; chỉ lỗi hạ tầng mới gọi lại với số lần 2, không lần 3.
# /// Run trả {Code,Output}; trả {Code,Output,Attempts}, Output cuối giữ nguyên,
# /// Attempts là mảng {Number,Code,Output} giữ nguyên cả stdout/stderr đã hợp nhất.
# /// Không bỏ qua lỗi của lần 2; ngoại lệ callback truyền ra, không biến thành đạt.</summary>
function Invoke-WorkflowCheck {
    param([ValidateSet('Build', 'Test', 'Mcp')][string]$Stage, [scriptblock]$Run)
    $attempts = New-Object Collections.ArrayList
    $res1 = & $Run 1
    $null = $attempts.Add([pscustomobject]@{ Number = 1; Code = $res1.Code; Output = $res1.Output })

    $last = $res1
    if (Test-InfrastructureFailure -Stage $Stage -Code $res1.Code -Output $res1.Output) {
        $res2 = & $Run 2
        $null = $attempts.Add([pscustomobject]@{ Number = 2; Code = $res2.Code; Output = $res2.Output })
        $last = $res2
    }

    return [pscustomobject]@{
        Code = $last.Code
        Output = $last.Output
        Attempts = @($attempts)
    }
}

# /// <summary>Gọi Verify một lần; chỉ khi Verify.Ok mới gọi Qc một lần, kể cả đã phục hồi lỗi máy.
# /// Trả {Verify,Review}; Review null khi trượt hoặc QC chủ động bỏ qua.
# /// Không giữ khóa repo trong Verify/Qc; ngoại lệ truyền ra để điểm vào CLI bắt.</summary>
function Invoke-WorkflowGate {
    param([scriptblock]$Verify, [scriptblock]$Qc)
    $vResult = & $Verify
    $reviewResult = $null
    if ($vResult -and $vResult.Ok -and $Qc) {
        $reviewResult = & $Qc
    }
    return [pscustomobject]@{
        Verify = $vResult
        Review = $reviewResult
        Ok = [bool]($vResult -and $vResult.Ok)
    }
}

# /// <summary>Khóa liên tiến trình theo đường dẫn repo chuẩn hóa, không phân biệt hoa thường trên Windows.
# /// Cùng repo dùng chung khóa cho bảng việc/metrics/nhật ký, repo khác độc lập.
# /// Gọi Action và trả output; luôn nhả khóa khi lỗi; tái nhập cùng tiến trình được phép.
# /// Hết TimeoutSeconds báo 'Không lấy được khóa giao dịch repo.'; không gọi AI trong Action.</summary>
function Invoke-RepoTransaction {
    param([string]$RepoPath, [scriptblock]$Action, [int]$TimeoutSeconds = 30)
    if ([string]::IsNullOrWhiteSpace($RepoPath)) { throw "RepoPath không được để trống." }
    $norm = [System.IO.Path]::GetFullPath($RepoPath).TrimEnd('\', '/').ToLowerInvariant()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($norm)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hash = [System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-'
    $mutexName = "Local\AITeam_Repo_" + $hash

    $mutex = New-Object System.Threading.Mutex($false, $mutexName)
    $acquired = $false
    try {
        try {
            $acquired = $mutex.WaitOne([TimeSpan]::FromSeconds($TimeoutSeconds))
        } catch [System.Threading.AbandonedMutexException] {
            $acquired = $true
        }
        if (-not $acquired) {
            throw 'Không lấy được khóa giao dịch repo.'
        }
        & $Action
    } finally {
        if ($acquired) {
            $mutex.ReleaseMutex()
        }
        $mutex.Dispose()
    }
}

# /// <summary>Dưới khóa repo, đọc tasks/TaskFile (chỉ tên file .md trực tiếp, không wildcard),
# /// lấy **Yêu cầu:** R-nnn và thêm đúng một dòng vào ## Nhật ký của requests/R-nnn.md.
# /// Dòng: - yyyy-MM-dd HH:mm:ss zzz | TaskId | Event | lượt Run | Result <!-- workflow:TaskId:Event:Run -->
# /// Event chỉ Prep/Assign/Verify/Qc/Merge; cùng TaskId/Event/Run bỏ trùng dù Result khác.
# /// Chèn trước heading ## kế tiếp, giữ nguyên phần khác, BOM và xuống dòng của phiếu.
# /// Thiếu Yêu cầu ở task cũ trả false và không sửa file. Thành công mới true; trùng trả false.
# /// Thiếu/hỏng task hoặc phiếu, ID không hợp lệ, nhiều Nhật ký, thiếu Nhật ký, UTF-8 hỏng,
# /// đường dẫn tuyệt đối/../reparse point ở bất cứ thành phần nào: ném 'Không ghi được nhật ký phiếu.'
# /// Không tạo/thay phiếu hỏng; TaskId/Event/Result không nhận CR/LF, dấu | hoặc HTML để chống chèn dòng.
# /// Hàm chỉ sửa file, caller commit cùng bảng việc dưới cùng khóa; không gọi git/AI tại đây.</summary>
function Update-WorkflowJournal {
    param([string]$RepoPath, [string]$TaskFile, [string]$TaskId,
        [ValidateSet('Prep', 'Assign', 'Verify', 'Qc', 'Merge')][string]$Event,
        [int]$Run, [string]$Result, [datetimeoffset]$At = [datetimeoffset]::Now)

    $failMsg = 'Không ghi được nhật ký phiếu.'

    if ([string]::IsNullOrWhiteSpace($RepoPath) -or [string]::IsNullOrWhiteSpace($TaskFile) -or [string]::IsNullOrWhiteSpace($TaskId)) {
        throw $failMsg
    }
    if ($Run -lt 1) { throw $failMsg }
    if ($TaskFile -match '[\\/]' -or $TaskFile -match '\.\.' -or $TaskFile -match '[\*\?]' -or -not $TaskFile.EndsWith('.md') -or [System.IO.Path]::IsPathRooted($TaskFile)) {
        throw $failMsg
    }
    if ($TaskId -match '[\r\n\|<>]') { throw $failMsg }
    if ($Result -match '[\r\n\|<>]') { throw $failMsg }

    function Test-Reparse([string]$path) {
        if (-not (Test-Path -LiteralPath $path)) { return $false }
        $attr = [System.IO.File]::GetAttributes($path)
        return [bool]($attr -band [System.IO.FileAttributes]::ReparsePoint)
    }

    if (Test-Reparse $RepoPath) { throw $failMsg }
    $tasksDir = Join-Path $RepoPath 'tasks'
    if (Test-Reparse $tasksDir) { throw $failMsg }
    $reqDir = Join-Path $RepoPath 'requests'
    if (Test-Reparse $reqDir) { throw $failMsg }

    $taskPath = Join-Path $tasksDir $TaskFile
    if (-not (Test-Path -LiteralPath $taskPath -PathType Leaf)) { throw $failMsg }
    if (Test-Reparse $taskPath) { throw $failMsg }

    $utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
    try {
        $taskBytes = [System.IO.File]::ReadAllBytes($taskPath)
        $taskText = $utf8Strict.GetString($taskBytes)
        if ($taskText.StartsWith([char]0xFEFF)) { $taskText = $taskText.Substring(1) }
    } catch {
        throw $failMsg
    }

    $mReq = [regex]::Match($taskText, '\*\*Yêu cầu:\*\*\s*([^\r\n]+)')
    if (-not $mReq.Success) {
        return $false
    }
    $reqVal = $mReq.Groups[1].Value.Trim()
    if ($reqVal -notmatch '^R-\d+$') {
        throw $failMsg
    }

    $reqPath = Join-Path $reqDir "$reqVal.md"
    if (-not (Test-Path -LiteralPath $reqPath -PathType Leaf)) { throw $failMsg }
    if (Test-Reparse $reqPath) { throw $failMsg }

    return Invoke-RepoTransaction -RepoPath $RepoPath -Action {
        try {
            $reqBytes = [System.IO.File]::ReadAllBytes($reqPath)
        } catch {
            throw $failMsg
        }

        $hasBom = ($reqBytes.Length -ge 3 -and $reqBytes[0] -eq 0xEF -and $reqBytes[1] -eq 0xBB -and $reqBytes[2] -eq 0xBF)
        try {
            $reqText = $utf8Strict.GetString($reqBytes)
            if ($reqText.StartsWith([char]0xFEFF)) { $reqText = $reqText.Substring(1) }
        } catch {
            throw $failMsg
        }

        $journalMatches = [regex]::Matches($reqText, '(?m)^##\s+Nhật ký\s*$')
        if ($journalMatches.Count -ne 1) {
            throw $failMsg
        }

        $marker = "<!-- workflow:${TaskId}:${Event}:${Run} -->"
        if ($reqText.Contains($marker)) {
            return $false
        }

        $nl = if ($reqText.Contains("`r`n")) { "`r`n" } else { "`n" }
        $timeStr = $At.ToString('yyyy-MM-dd HH:mm:ss zzz')
        $newEntry = "- $timeStr | $TaskId | $Event | lượt $Run | $Result $marker"

        $jMatch = $journalMatches[0]
        $afterJournal = $reqText.Substring($jMatch.Index + $jMatch.Length)
        $nextHeading = [regex]::Match($afterJournal, '(?m)^##\s+')

        if ($nextHeading.Success) {
            $insertPos = $jMatch.Index + $jMatch.Length + $nextHeading.Index
            $before = $reqText.Substring(0, $insertPos)
            $after = $reqText.Substring($insertPos)
            if ($before.EndsWith($nl)) {
                $updated = $before + $newEntry + $nl + $after
            } else {
                $updated = $before + $nl + $newEntry + $nl + $after
            }
        } else {
            if ($reqText.EndsWith($nl)) {
                $updated = $reqText + $newEntry + $nl
            } else {
                $updated = $reqText + $nl + $newEntry + $nl
            }
        }

        $enc = New-Object System.Text.UTF8Encoding($hasBom)
        [System.IO.File]::WriteAllText($reqPath, $updated, $enc)
        return $true
    }
}
