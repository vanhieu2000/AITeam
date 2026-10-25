# Khung V8-03 — Antigravity điền phần thân, không đổi hợp đồng nghiệm thu.
# /// <summary>Đọc các file TaskId.accept-N.json (N nguyên dương), không khớp tiền tố gần giống.
# /// Envelope mới: {task,head,run,verify:{ok,source},qc:{verdict,findings,source},metrics:{source}}.
# /// findings: {id,severity,open,classification}; classification chỉ Optional do quản lý truyền
# /// qua -Optional mới cho phép hoãn Required; Blocker mở luôn chặn. Không tin phân loại AI tự ghi.
# /// Chọn run lớn nhất khớp task chính xác và SHA đầy đủ Head; cùng lượt mâu thuẫn ⇒ không rõ.
# /// Đọc metrics.source JSON theo mẫu thật, kiểm project/task/run/head trước lấy durationSec/60,
# /// totalTokens; tên task là mã hoặc mã-slug có ranh giới, không nhận V8-030 cho V8-03.
# /// Trả {TaskId,Head,Run,VerifyOk,Verdict,Findings,Minutes,Tokens,Sources,Score}.
# /// Thiếu/hỏng/log cũ không SHA ⇒ VerifyOk=$false, Verdict/Minutes/Tokens/Score='không rõ'.
# /// Sources giữ đường dẫn nguồn; điểm chỉ lấy từ nguồn có thật, không suy ra từ PASS.
# /// Chặn .., tuyệt đối và reparse point trong source; không ghi file, không gọi tiến trình.
# /// Lỗi đọc nguồn chỉ làm trường tương ứng không rõ, không ném lỗi đọc log.</summary>
function Get-WorkflowAcceptEvidence {
    param([string]$TaskId, [string]$Head, [string]$LogsPath, [string]$MetricsPath)
    $result = [pscustomobject]@{ TaskId=$TaskId; Head=$Head; Run=$null; VerifyOk=$false; Verdict='không rõ'; Findings=@(); Minutes='không rõ'; Tokens='không rõ'; Sources=@(); Score='không rõ' }
    function ReadSource($rootPath, $relative, [switch]$Json) {
        if (-not $relative -or [IO.Path]::IsPathRooted($relative) -or $relative -match '[:*?]|(^|[\\/])[^\\/]*[. ]([\\/]|$)' -or @($relative -split '[\\/]') -contains '..') { throw 'Nguồn không an toàn.' }
        $full=[IO.Path]::GetFullPath((Join-Path $rootPath $relative))
        for ($node=$full; $node; $node=[IO.Path]::GetDirectoryName($node)) {
            if ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) { throw 'Nguồn không an toàn.' }
        }
        $value=[IO.File]::ReadAllText($full,(New-Object Text.UTF8Encoding($false,$true)))
        if ($Json) { return ($value | ConvertFrom-Json) }; return $value
    }
    if ([string]::IsNullOrWhiteSpace($LogsPath)) { return $result }
    for ($node=[IO.Path]::GetFullPath($LogsPath); $node; $node=[IO.Path]::GetDirectoryName($node)) {
        try { if ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) { return $result } } catch { return $result }
    }
    if ($TaskId -notmatch '^[A-Za-z0-9]+-\d+[a-z]?$' -or $Head -notmatch '^[0-9a-fA-F]{40}$') { return $result }
    $records=@()
    try {
        foreach ($file in Get-ChildItem -LiteralPath $LogsPath -File -ErrorAction Stop) {
            $m=[regex]::Match($file.Name,'^'+[regex]::Escape($TaskId)+'\.accept-([1-9][0-9]*)\.json$')
            if (-not $m.Success) { continue }
            try {
                $record=ReadSource $LogsPath $file.Name -Json
                if ($record.task -ceq $TaskId -and $record.head -ceq $Head -and [string]$record.run -eq $m.Groups[1].Value) { $records+=$record }
            } catch { continue } # Chỉ số hỏng không phải bằng chứng.
        }
    } catch { return $result }
    if (-not $records.Count) { return $result }
    $latest=@($records | Sort-Object { [long]$_.run } -Descending); $record=$latest[0]; $result.Run=$record.run
    if (@($latest | Where-Object { $_.run -eq $record.run }).Count -ne 1) { return $result }
    try {
        $null=ReadSource $LogsPath $record.verify.source
        if ($record.verify.ok -is [bool]) { $result.VerifyOk=$record.verify.ok; $result.Sources+=$record.verify.source }
    } catch { $result.VerifyOk=$false }
    try {
        $qcSource=ReadSource $LogsPath $record.qc.source -Json
        if ($qcSource.verdict -cne $record.qc.verdict) { throw 'Kết luận QC không khớp nguồn.' }
        $sourceFindings=@($qcSource.findings | Where-Object { $null -ne $_ })
        $indexedFindings=@($record.qc.findings | Where-Object { $null -ne $_ })
        if ($sourceFindings.Count -ne $indexedFindings.Count) { throw 'Finding QC không khớp nguồn.' }
        for ($i=0; $i -lt $sourceFindings.Count; $i++) {
            foreach ($field in @('id','severity','open','classification')) {
                if ($sourceFindings[$i].$field -cne $indexedFindings[$i].$field) { throw 'Finding QC không khớp nguồn.' }
            }
        }
        foreach ($finding in $indexedFindings) {
            if ($null -eq $finding -or -not $finding.id -or $finding.severity -cnotin @('Blocker','Required','Optional') -or $finding.open -isnot [bool]) { throw 'Finding không hợp lệ.' }
        }
        if ($record.qc.verdict -cin @('PASS','CHANGES_REQUESTED','BLOCKED')) {
            $result.Verdict=$record.qc.verdict; $result.Findings=@($record.qc.findings); $result.Sources+=$record.qc.source
        }
    } catch { $result.Verdict='không rõ' }
    try {
        $metric=ReadSource $MetricsPath $record.metrics.source -Json
        if (-not $record.project -or $metric.project -cne $record.project -or $metric.task -cnotmatch ('^'+[regex]::Escape($TaskId)+'(?:-.+)?$') -or
            [string]$metric.run -ne [string]$record.run -or $metric.head -cne $Head) { return $result }
        $number=0.0
        if ($null -ne $metric.durationSec -and [double]::TryParse([string]$metric.durationSec,[ref]$number) -and $number -ge 0 -and -not [double]::IsInfinity($number)) { $result.Minutes=$number/60 }
        if ($null -ne $metric.totalTokens -and [double]::TryParse([string]$metric.totalTokens,[ref]$number) -and $number -ge 0 -and -not [double]::IsInfinity($number)) { $result.Tokens=$(if ([math]::Floor($number) -eq $number) {$number} else {'không rõ'}) }
        $result.Sources+=$record.metrics.source
    } catch { $result.Minutes='không rõ'; $result.Tokens='không rõ' }
    return $result
}

# /// <summary>Items đã lọc theo Request và bảng việc chờ nghiệm thu, theo thứ tự plan.
# /// Mỗi item {Id,Evidence,Added,Budget,Changes,DependsOn}; Changes là đường dẫn tương đối.
# /// Trả string[]: KT/QC, từng finding mở, Added/Budget (Added đã loại khung/test nghiệm thu),
# /// UI: /TênTrang từ hub/Pages/*.cshtml(.cs); wwwroot/shared không xác định ⇒ 'không rõ'.
# /// Không UI ghi 'Không'; không có item trả đúng 'Không có task chờ nghiệm thu.'.
# /// Không bỏ finding để đủ 40 dòng: gói quá 40 dòng ghi rõ vượt giới hạn vì giữ đủ bằng chứng.
# /// Thiếu dữ liệu ghi 'không rõ', không thay null bằng 0; không ghi/commit/fetch/chạy AI.</summary>
function Get-WorkflowAcceptPack {
    param([object[]]$Items)
    if (-not $Items.Count) { return 'Không có task chờ nghiệm thu.' }
    $lines=@()
    foreach ($item in $Items) {
        $e=$item.Evidence
        $added=if ($null -eq $item.Added) {'không rõ'} else {$item.Added}
        $budget=if ($null -eq $item.Budget) {'không rõ'} else {$item.Budget}
        $pages=@(); $ui=$false
        foreach ($path in $item.Changes) {
            if ($path -match '^hub/(Pages|wwwroot)/') { $ui=$true }
            if ($path -match '^hub/Pages/(?!Shared/)(.+)\.cshtml(?:\.cs)?$') { $pages+='/'+$Matches[1] }
        }
        $page=if ($pages.Count) {($pages | Select-Object -Unique) -join ', '} elseif ($ui) {'không rõ'} else {'Không'}
        $gate=if ($e -and $e.VerifyOk) {'ĐẠT'} else {'không rõ'}
        $qc=if ($e -and $e.Verdict) {$e.Verdict} else {'không rõ'}
        $lines+="$($item.Id): KT $gate; QC $qc; Added/Budget $added/$budget; UI: $page; DependsOn: $($item.DependsOn -join ', ')"
        foreach ($finding in $e.Findings) {
            if ($finding.open -eq $true) { $lines+="  $($finding.id) · $($finding.severity) · nguồn: $($e.Sources -join ', ')" }
        }
    }
    if ($lines.Count -gt 40) { $lines+='Vượt 40 dòng để giữ đủ finding và bằng chứng.' }
    return $lines
}

# /// <summary>NumStat là các dòng git diff --numstat từ commit khung tới HEAD (adapter lấy seed
# /// đúng như Invoke-Verify, nếu không có seed dùng merge-base). Cộng số thêm mới cột đầu;
# /// bỏ TaskFile và AcceptanceFiles đúng đường dẫn, bỏ dòng nhị phân '-', không đọc metrics.linesAdded.
# /// Trả int, rỗng trả 0. Không tự chạy git hoặc đổi file.</summary>
function Get-WorkflowAcceptLineCount {
    param([string[]]$NumStat, [string]$TaskFile, [string[]]$AcceptanceFiles)
    $total=0
    foreach ($line in $NumStat) {
        $parts=$line -split "`t"
        if ($parts.Count -eq 3 -and $parts[0] -match '^\d+$' -and $parts[2] -ne $TaskFile -and $AcceptanceFiles -notcontains $parts[2]) { $total += [int]$parts[0] }
    }
    return $total
}

# /// <summary>Before/After là dictionary đường dẫn → nội dung UTF-8 đã đọc nghiêm ngặt.
# /// Chỉ file TaskFile được đổi, chỉ nội dung ba mục ## Review (Claude), ## Kết quả,
# /// ## Đánh giá quy trình; tiêu đề, báo cáo, test, code và mọi byte ngoài mục phải nguyên.
# /// Không cho xóa mục, thêm file, đổi hoa thường đường dẫn hay chèn heading ## ngoài ba mục.
# /// Sai ném đúng 'Codex sửa ngoài mục nghiệm thu cho phép.'. Hợp lệ trả true.
# /// Caller còn kiểm BOM/CRLF byte gốc; không reset --hard/clean hay ghi đè sửa của người dùng.</summary>
function Test-WorkflowAcceptDocuments {
    param([System.Collections.IDictionary]$Before, [System.Collections.IDictionary]$After, [string]$TaskFile)
    $fail='Codex sửa ngoài mục nghiệm thu cho phép.'
    if (-not $Before -or -not $After -or $Before.Count -ne $After.Count) { throw $fail }
    foreach ($key in $Before.Keys) {
        if (@($After.Keys | Where-Object { $_ -ceq $key }).Count -ne 1) { throw $fail }
        if ($key -cne $TaskFile -and $Before[$key] -cne $After[$key]) { throw $fail }
    }
    if (@($Before.Keys | Where-Object { $_ -ceq $TaskFile }).Count -ne 1) { throw $fail }
    $texts=@([string]$Before[$TaskFile],[string]$After[$TaskFile]); $protected=@()
    foreach ($value in $texts) {
        $headers=[regex]::Matches($value,'(?m)^##[^\S\r\n]+([^\r\n]+)(\r?\n|$)')
        $cursor=0; $outside=''; $seen=@{}
        for ($i=0; $i -lt $headers.Count; $i++) {
            $h=$headers[$i]; $sectionTitle=$h.Groups[1].Value
            if ($sectionTitle -cin @('Review (Claude)','Kết quả','Đánh giá quy trình')) {
                if ($seen.ContainsKey($sectionTitle)) { throw $fail }; $seen[$sectionTitle]=$true
                $outside+=$value.Substring($cursor,$h.Index+$h.Length-$cursor)
                $cursor=if ($i+1 -lt $headers.Count) {$headers[$i+1].Index} else {$value.Length}
            }
        }
        if ($seen.Count -ne 3) { throw $fail }
        $protected+= $outside+$value.Substring($cursor)
    }
    if ($protected[0] -cne $protected[1]) { throw $fail }
    if ($texts[0].Contains("`r`n") -and $texts[1] -match '(?<!\r)\n') { throw $fail }
    return $true
}

# /// <summary>Context={TaskId,Head,TaskFile,Files,Merged}; Files là snapshot sạch của nhánh task.
# /// Merged trả {Status='ALREADY_MERGED'} trước mọi callback, không thêm vòng.
# /// ReadEvidence nhận TaskId,Head. ĐẠT cần VerifyOk và QC PASS cùng Head, Sources đầy đủ;
# /// Required mở chưa được quản lý phân loại Optional, Blocker mở, thiếu/sai SHA ⇒ ném
# /// 'Chưa đủ bằng chứng nghiệm thu.'. Optional là chuỗi ID finding được hoãn kèm lý do;
# /// không tự chuyển mọi finding thành Optional. Reject khác rỗng bỏ luồng ĐẠT, không cần KT đạt.
# /// WriteReview nhận decision={Round,Verdict,Optional,Note,Reject,Evidence}, trả Files sau sửa;
# /// Round=max vòng trong riêng Review +1, Verdict=ĐẠT/CẦN SỬA. Gọi dưới Invoke-WorkflowCodexLock.
# /// Kiểm diff bằng Test-WorkflowAcceptDocuments; kiểm SHA vẫn Context.Head trước Publish.
# /// Publish nhận Files, trả {Ok,Head} sau commit/push chỉ tài liệu nhánh task; lỗi thì dừng.
# /// ĐẠT: CheckApproval nhận SHA SAU commit tài liệu, phải true; rồi Merge nhận SHA này,
# /// trả {Ok}; false/ngoại lệ ⇒ ném 'Không hoàn tất nghiệm thu.', không ghi nhật ký thành công.
# /// Reject: Publish xong mới Resume nhận $true,Reject; không CheckApproval/Merge.
# /// Trả {Status='MERGED'|'REJECTED',Head}; giữ snapshot/không phá dữ liệu khi lỗi.
# /// Callback giả là ranh giới I/O cho test; adapter thật gọi git/AI hữu hạn, không nhận diện test.</summary>
function Invoke-WorkflowAccept {
    param($Context, [scriptblock]$ReadEvidence, [scriptblock]$WriteReview,
          [scriptblock]$Publish, [scriptblock]$CheckApproval, [scriptblock]$Merge,
          [scriptblock]$ResumeTask, [string]$Optional = '', [string]$Note = '', [string]$Reject = '')
    if ($Context.Merged) { return @{Status='ALREADY_MERGED'} }
    $headBefore=$Context.Head; $e=& $ReadEvidence $Context.TaskId $headBefore
    if (-not $Reject) {
        $invalid='Chưa đủ bằng chứng nghiệm thu.'
        if (-not $e -or $e.Head -cne $headBefore -or -not $e.VerifyOk -or $e.Verdict -cne 'PASS' -or @($e.Sources).Count -lt 2) { throw $invalid }
        $deferred=@{}
        if ($Optional) {
            foreach ($entry in $Optional.Split(';')) {
                if ($entry -notmatch '^\s*([^:;]+):\s*(\S.*)$') { throw $invalid }
                $id=$Matches[1].Trim()
                if (@($e.Findings | Where-Object { $_.id -ceq $id -and $_.open -eq $true -and $_.severity -eq 'Required' }).Count -ne 1 -or $deferred.ContainsKey($id)) { throw $invalid }
                $deferred[$id]=$Matches[2]
            }
        }
        foreach ($f in $e.Findings) {
            if ($f.open -ne $true) { continue }
            if ($f.severity -eq 'Blocker' -or ($f.severity -eq 'Required' -and -not $deferred.ContainsKey([string]$f.id)) -or $f.severity -notin @('Blocker','Required','Optional')) { throw $invalid }
        }
    }
    $review=[regex]::Match($Context.Files[$Context.TaskFile],'(?ms)^## Review \(Claude\)\r?\n(.*?)(?=^## |\z)').Groups[1].Value
    $round=0
    foreach ($m in [regex]::Matches($review,'(?m)^### Vòng (\d+)\b')) { $round=[math]::Max($round,[int]$m.Groups[1].Value) }
    $decision=[pscustomobject]@{Round=($round+1);Verdict=$(if ($Reject) {'CẦN SỬA'} else {'ĐẠT'});Optional=$Optional;Note=$Note;Reject=$Reject;Evidence=$e}
    $after=Invoke-WorkflowCodexLock -Action { & $WriteReview $decision }
    $null=Test-WorkflowAcceptDocuments $Context.Files $after $Context.TaskFile
    if ($Context.Head -cne $headBefore) { throw 'Chưa đủ bằng chứng nghiệm thu.' }
    try {
        $published=& $Publish $after
        if (-not $published -or $published.Ok -ne $true -or $published.Head -notmatch '^[a-fA-F0-9]{40}$') { throw 'Publish thất bại.' }
        if ($Reject) { $null=& $ResumeTask $true $Reject; return @{Status='REJECTED';Head=$published.Head} }
        $approved=& $CheckApproval $published.Head
        if ($approved -isnot [bool] -or -not $approved) { throw 'Chưa có phê duyệt.' }
        $merged=& $Merge $published.Head
        if (-not $merged -or $merged.Ok -ne $true) { throw 'Merge thất bại.' }
        return @{Status='MERGED';Head=$published.Head}
    } catch {
        if ($_.Exception.Message -ceq 'Chưa đủ bằng chứng nghiệm thu.') { throw }
        throw 'Không hoàn tất nghiệm thu.'
    }
}
