# Test hồi quy cho cổng kiểm tra `team.ps1 verify` (IMP-001 giải pháp 1).
# Chạy mỗi khi sửa team.ps1:  powershell -File tools\test-verify.ps1
# Dựng thư mục tạm có: bản sao team.ps1 + team.json/projects.json giả, remote git trần, repo dự án "Demo",
# worktree agent với commit khung "Test nghiệm thu … (Claude)" + commit của agent; rồi chạy verify và kiểm kết quả.
# Không đụng repo / cấu hình thật. Không cần mạng.
param([switch]$Keep, [string]$TeamPs1 = "")   # -TeamPs1: thử với bản team.ps1 khác (vd. bản cũ) để chắc test bắt được lỗi
$ErrorActionPreference = "Stop"
$src = Split-Path $PSScriptRoot -Parent
$T = Join-Path $env:TEMP ("aiteam-verify-test-" + [guid]::NewGuid().ToString("N").Substring(0, 8))
$Bom = New-Object Text.UTF8Encoding $true
$NoBom = New-Object Text.UTF8Encoding $false
$script:failed = 0

function W($path, $text, $enc) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, $enc) }
function G { $ErrorActionPreference = "Continue"; $o = & git @args 2>&1; if ($LASTEXITCODE -ne 0) { throw "git $args : $o" }; $o }
function Check($name, [bool]$cond, $out) {
    if ($cond) { Write-Host "  [ĐẠT] $name" -ForegroundColor Green }
    else { Write-Host "  [HỎNG] $name" -ForegroundColor Red; $script:failed++; if ($out) { Write-Host ($out | Out-String) -ForegroundColor DarkGray } }
}
function Lines($n, $prefix) { (1..$n | ForEach-Object { "    // $prefix dòng $_" }) -join "`r`n" }

# Dựng 1 kịch bản: $agentChanges = scriptblock nhận $wt (worktree) để agent sửa file rồi commit.
function New-Scenario($name, [scriptblock]$agentChanges, [int]$budget = 20, [string]$build = "") {
    $S = Join-Path $T $name
    $root = Join-Path $S "AITeam"; $demo = Join-Path $S "Demo"; $work = Join-Path $S "work"; $origin = Join-Path $S "origin.git"
    New-Item -ItemType Directory -Force $root, (Join-Path $root "tools") | Out-Null
    Copy-Item $(if ($TeamPs1) { $TeamPs1 } else { Join-Path $src "team.ps1" }) (Join-Path $root "team.ps1")
    Copy-Item (Join-Path $src "tools\secret-scan.ps1") (Join-Path $root "tools")
    Copy-Item (Join-Path $src "tools\workflow-common.ps1") (Join-Path $root "tools")
    $cfg = Get-Content (Join-Path $src "team.json") -Raw -Encoding UTF8 | ConvertFrom-Json
    $cfg.projectsRoot = $S; $cfg.workRoot = $work
    W (Join-Path $root "team.json") ($cfg | ConvertTo-Json -Depth 10) $NoBom
    W (Join-Path $root "projects.json") (ConvertTo-Json -Depth 5 @(@{ name = "Demo"; repo = "local/demo"; path = $demo; workDir = $work; build = $build; test = ""; stack = "test"; areas = @{} })) $NoBom

    G init -q --bare -b main $origin | Out-Null
    G clone -q $origin $demo | Out-Null
    G -C $demo config user.email t@t; G -C $demo config user.name t
    $task = @"
# X-01 · Kịch bản $name

- **Agent:** A
- **Nhánh:** ``agent-a/X-01-demo``
- **Ngân sách:** ~$budget dòng

## Phạm vi (được sửa)
- ``src/Agent.cs``
- ``tests/``

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- ``tests/AcceptanceX01Tests.cs``

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Làm xong | đã chạy build và test thật, kết quả đạt hết, không có cảnh báo nào |
"@
    W (Join-Path $demo "tasks\X-01-demo.md") $task $Bom
    W (Join-Path $demo "README.md") "demo" $NoBom
    G -C $demo add -A; G -C $demo commit -q -m "goc"; G -C $demo push -q origin main 2>$null

    $wt = Join-Path $work "a-x-01"
    G -C $demo worktree add -q -b agent-a/X-01-demo $wt origin/main | Out-Null
    # Commit khung của Claude: 1 file khung KHÔNG được đổi (60 dòng, có BOM), 1 file agent điền (có BOM), test nghiệm thu.
    W (Join-Path $wt "src\Skel.cs") ("// Khung — KHÔNG đổi`r`nclass Skel {`r`n" + (Lines 60 "khung") + "`r`n}`r`n") $Bom
    W (Join-Path $wt "src\Agent.cs") "// Khung agent điền — có chữ Việt`r`nclass Agent {`r`n    // TODO(agent)`r`n}`r`n" $Bom
    W (Join-Path $wt "tests\AcceptanceX01Tests.cs") "// test nghiệm thu`r`nclass AcceptanceX01Tests { }`r`n" $Bom
    G -C $wt add -A; G -C $wt commit -q -m "Test nghiệm thu X-01 + khung (Claude)"
    & $agentChanges $wt
    G -C $wt add -A; G -C $wt commit -q -m "X-01: agent làm"
    G -C $wt push -q -u origin agent-a/X-01-demo 2>$null
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "team.ps1") verify -Project Demo -Task X-01 2>&1 | Out-String
    [pscustomobject]@{ Out = $out; Wt = $wt }
}

try {
    Write-Host "Kiểm tra tĩnh: hàm không gán biến cục bộ trùng tên tham số script (PowerShell không phân biệt hoa thường — R-004: `$dev đè -Dev)" -ForegroundColor Cyan
    $teamFile = if ($TeamPs1) { $TeamPs1 } else { Join-Path $src "team.ps1" }
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($teamFile, [ref]$null, [ref]$null)
    $params = @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
    # Cho phép: biến gán có chủ đích thay giá trị tham số trong hàm không đọc tham số đó (đã rà 24/10).
    $allow = @{ "Invoke-Assign" = @("prompt", "model"); "Set-ManagerIdentity" = @("name"); "Invoke-SecretList" = @("names"); "Save-RunMetrics" = @("name"); "Invoke-ApprovalRequest" = @("title") }
    $bad = @()
    foreach ($fn in $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)) {
        foreach ($as in $fn.FindAll({ $args[0] -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
            $v = $as.Left; if ($v -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
            $nm = $v.VariablePath.UserPath; if ($nm -match ':') { continue }
            if (($params | Where-Object { $_ -ieq $nm }) -and -not ($allow[$fn.Name] -contains $nm.ToLower())) { $bad += "$($fn.Name):$($v.Extent.StartLineNumber) `$$nm" }
        }
    }
    Check "không biến cục bộ nào đè tham số script ngoài danh sách cho phép" ($bad.Count -eq 0) ($bad -join "`n")
    # IMP-005 (V5-04): dòng ghi "🔵 Chờ review" sau cổng KT từng bị mất ⇒ Văn phòng hiện agent "đang làm" mãi.
    $assignFn = $ast.Find({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $args[0].Name -eq "Invoke-Assign" }, $true)
    Check "Invoke-Assign ghi bảng việc 🔵 Chờ review / 🔴 Cần sửa sau cổng KT" ($assignFn -and $assignFn.Extent.Text -match 'Set-BoardStatus[^\r\n]*Chờ review[^\r\n]*Cần sửa') $null

    Write-Host "Kịch bản 1: agent điền file khung nhưng làm MẤT BOM (H3-01), không đụng file khung khác" -ForegroundColor Cyan
    $r = New-Scenario "s1" { param($wt) W (Join-Path $wt "src\Agent.cs") ("// Khung agent điền — có chữ Việt`r`nclass Agent {`r`n" + (Lines 8 "agent") + "`r`n}`r`n") $NoBom }
    Check "tự trả BOM cho src/Agent.cs (file chỉ có trên nhánh)" ($r.Out -match 'ĐÃ TỰ SỬA: trả lại BOM UTF-8 cho src/Agent.cs') $r.Out
    $b = [IO.File]::ReadAllBytes((Join-Path $r.Wt "src\Agent.cs"))
    Check "file trên đĩa đã có BOM lại" ($b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF) $null
    $m = [regex]::Match($r.Out, 'Code thêm mới: (\d+) dòng')
    Check "ngân sách chỉ tính dòng agent viết (≤ 12), không tính 60 dòng khung" ($m.Success -and [int]$m.Groups[1].Value -le 12) $r.Out
    Check "file khung không đổi ⇒ không bị coi là ngoài phạm vi (V4-06)" ($r.Out -notmatch 'NGOÀI PHẠM VI') $r.Out
    Check "kết luận ĐẠT" ($r.Out -match 'Cổng kiểm tra: ĐẠT') $r.Out

    Write-Host "Kịch bản 2: agent SỬA file khung bị cấm (src/Skel.cs)" -ForegroundColor Cyan
    $r = New-Scenario "s2" { param($wt)
        W (Join-Path $wt "src\Agent.cs") "// Khung agent điền — có chữ Việt`r`nclass Agent { int x; }`r`n" $Bom
        W (Join-Path $wt "src\Skel.cs") "// agent sửa khung`r`nclass Skel { }`r`n" $Bom }
    Check "bắt sửa ngoài phạm vi ở src/Skel.cs" ($r.Out -match 'NGOÀI PHẠM VI[^\r\n]*src/Skel\.cs') $r.Out
    Check "kết luận KHÔNG ĐẠT" ($r.Out -match 'Cổng kiểm tra: KHÔNG ĐẠT') $r.Out

    Write-Host "Kịch bản 3: agent viết quá 1,5 lần ngân sách" -ForegroundColor Cyan
    $r = New-Scenario "s3" { param($wt) W (Join-Path $wt "src\Agent.cs") ("// Khung agent điền — có chữ Việt`r`nclass Agent {`r`n" + (Lines 40 "agent") + "`r`n}`r`n") $Bom } -budget 20
    Check "cảnh báo VƯỢT NGÂN SÁCH (chỉ cảnh báo, không trả agent)" ($r.Out -match 'VƯỢT NGÂN SÁCH quá 1,5 lần') $r.Out
    Check "vẫn ĐẠT (ngân sách chỉ cảnh báo)" ($r.Out -match 'Cổng kiểm tra: ĐẠT') $r.Out

    Write-Host "Kịch bản 4: agent sửa test nghiệm thu" -ForegroundColor Cyan
    $r = New-Scenario "s4" { param($wt)
        W (Join-Path $wt "src\Agent.cs") "// Khung agent điền — có chữ Việt`r`nclass Agent { int x; }`r`n" $Bom
        W (Join-Path $wt "tests\AcceptanceX01Tests.cs") "// agent sửa test`r`nclass AcceptanceX01Tests { }`r`n" $Bom }
    Check "bắt sửa test nghiệm thu" ($r.Out -match 'SỬA test nghiệm thu[^\r\n]*AcceptanceX01Tests') $r.Out
    Check "kết luận KHÔNG ĐẠT" ($r.Out -match 'Cổng kiểm tra: KHÔNG ĐẠT') $r.Out

    Write-Host "Kịch bản 5: build chỉ có cảnh báo NU1900 (mất mạng khi NuGet tải dữ liệu lỗ hổng — V4-11 bị giao lại ~28 lần)" -ForegroundColor Cyan
    $r = New-Scenario "s5" { param($wt) W (Join-Path $wt "src\Agent.cs") "// Khung agent điền — có chữ Việt`r`nclass Agent { int x; }`r`n" $Bom } -build 'cmd /c echo D:\x\y.csproj : warning NU1900: Error occurred while getting package vulnerability data'
    Check "NU1900 không làm trượt cổng" ($r.Out -match 'Cổng kiểm tra: ĐẠT') $r.Out
    Check "có ghi chú bỏ qua NU1900" ($r.Out -match 'Bỏ qua 1 cảnh báo NU1900') $r.Out

    Write-Host "Kịch bản 6: build có cảnh báo code thật (CS0168)" -ForegroundColor Cyan
    $r = New-Scenario "s6" { param($wt) W (Join-Path $wt "src\Agent.cs") "// Khung agent điền — có chữ Việt`r`nclass Agent { int x; }`r`n" $Bom } -build 'cmd /c echo D:\x\Agent.cs(3,5): warning CS0168: The variable e is declared but never used'
    Check "cảnh báo code thật vẫn làm trượt" (($r.Out -match 'Build có 1 warning') -and ($r.Out -match 'Cổng kiểm tra: KHÔNG ĐẠT')) $r.Out
}
finally {
    if (-not $Keep) { Get-ChildItem $T -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Attributes = 'Normal' }; Remove-Item $T -Recurse -Force -ErrorAction SilentlyContinue }
    else { Write-Host "Giữ thư mục tạm: $T" }
}
if ($script:failed -gt 0) { Write-Host "$($script:failed) kiểm tra HỎNG" -ForegroundColor Red; exit 1 }
Write-Host "Tất cả kiểm tra ĐẠT" -ForegroundColor Green
