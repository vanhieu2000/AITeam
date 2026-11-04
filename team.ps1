<#
.SYNOPSIS
  Điều phối team AI: Claude (quản lý) giao việc cho các agent Antigravity (agy) chạy ngầm, mỗi task một git worktree riêng.

.DESCRIPTION
  .\team.ps1 setup                                  Kiểm tra công cụ + đăng nhập, gắn quy trình team vào Claude (chạy 1 lần mỗi máy)
  .\team.ps1 doctor                                 Chỉ kiểm tra, không thay đổi gì
  .\team.ps1 add-project -Project Ten -Repo owner/ten        Thêm dự án đã có trên GitHub (tự clone nếu máy chưa có)
  .\team.ps1 add-project -Project Ten -Path D:\Ten           Thêm dự án có sẵn trên máy (tự tạo kho GitHub private nếu chưa có)
  .\team.ps1 add-project -Project Ten -New                   Tạo dự án mới (thư mục + git + kho private)
  .\team.ps1 new-task -Project Ten -Task T-003 -Title "..." -Agent B
  .\team.ps1 plan -Request R-006 [-Project Ten] [-Name V6]   Quy trình v4: Codex (CHỈ ĐỌC) đọc plans\R-006\brief.md + repo ⇒ plans\R-006\plan.md
  .\team.ps1 deputy [-Force] | deputy-on | deputy-off       Codex QUẢN LÝ PHỤ chạy 1 vòng khi Claude vắng > 20 phút (QUAN-LY-PHU.md; không merge) · bật / tắt lịch 30 phút
  .\team.ps1 prep -Request R-006 -Task V6-01 [-NoLaunch]     Quy trình v4: Codex viết file task + khung + test nghiệm thu ⇒ giao Antigravity (Codex QC sau cổng KT)
  .\team.ps1 assign -Project Ten -Task T-003 [-Resume] [-Feedback "..."] [-NoVerify] [-Force]
  .\team.ps1 assign -Project Ten -Task T-003 -Dev codex   Giao cho Codex viết code (mặc định agy; agy hết hạn mức tự chuyển Codex, -Dev agy để tắt)
                                                    Giao task (chờ tới khi agent xong). Ghi "đang chạy · máy X" lên bảng việc;
                                                    xong thì tự chạy cổng kiểm tra, không đạt thì tự giao lại 1 lần.
  .\team.ps1 verify -Project Ten -Task T-003        Cổng kiểm tra: commit/push, BOM (tự sửa), phạm vi, test nghiệm thu, build 0 warning, test
  .\team.ps1 metrics [-Project Ten]                 Số liệu review (vòng, tự sửa, lỗi) + hiệu suất lượt chạy (phút, token, dòng code, cổng kiểm tra)
  .\team.ps1 new-request -Title "..."               Tạo phiếu yêu cầu R-xxx (requests/) để theo dõi 1 yêu cầu của chủ dự án
  .\team.ps1 staff-report                           Bảng điểm từng nhân viên (tên trong staff/) + lỗi do quản lý / công cụ
  .\team.ps1 request-report -Request R-001          Tổng hợp mọi task của yêu cầu: lượt, phút agent, token, dòng / ngân sách, cổng kiểm tra, vòng review
  .\team.ps1 log-run -Project Ten -Task H8-12 -Dev opencode -Log <file>   Ghi số đo 1 lượt chạy trực tiếp (OpenCode/Codex) vào metrics\runs
  .\team.ps1 status [-Project Ten]                  Agent đang chạy, nhánh, kết quả lần chạy gần nhất
  .\team.ps1 cleanup -Project Ten -Task T-003       Xóa worktree sau khi đã merge
  .\team.ps1 switch-account                         Mở agy để /logout và đăng nhập tài khoản Antigravity khác
  .\team.ps1 ask -Project Ten -Prompt "..."          Việc CHỈ ĐỌC (review, phân tích, ý kiến thứ hai) — agy chạy --sandbox, không sửa file
  .\team.ps1 models                                 Danh sách model Antigravity dùng được
  .\team.ps1 install-hooks [-Project Ten]           Cài git hook chặn commit có bí mật (mặc định: mọi dự án)
  .\team.ps1 secret-set -Name DB_PASSWORD           Lưu bí mật vào kho CỤC BỘ (DPAPI, chỉ user Windows này giải mã được)
  .\team.ps1 secret-list | secret-remove -Name X    Xem tên (không xem giá trị) / xóa bí mật
  .\team.ps1 secret-env -Project Ten -Names A,B     Ghi .env.local (đã bị .gitignore) cho dự án chạy thử trên máy
  .\team.ps1 approval-request -Kind secret|security|sensitive-data|external|decision -Title "…" [-Risk low|medium|high] [-Name KHÓA]
  .\team.ps1 claude-decision -Kind qc|merge|improvement|rule|other -Title "…" [-Task X] [-Request R-xxx] [-Detail "…"]
                                                     Ghi nhật ký "Claude tự quyết" (approvals/claude-decisions.jsonl, chỉ thêm dòng)
  .\team.ps1 hub                                    Mở bảng điều khiển AITeam Hub (chạy ngầm)
  .\team.ps1 review -Project Ten -Task T-003        Gói review gọn: cổng kiểm tra, file đổi, báo cáo agent, ảnh
  .\team.ps1 merge -Project Ten -Task T-003         Merge (cần "Vòng N — ĐẠT"), bảng việc ✅, clone sạch, dọn worktree, làm mới bản chạy thử
  .\team.ps1 stop -Task T-003                       Dừng hẳn task: cả lệnh assign bao ngoài lẫn agent (không bị tự giao lại)
  .\team.ps1 stop -All                              Dừng vòng auto và mọi assign nó đã sinh ra trên máy này
  .\team.ps1 auto -Project Ten [-Max 2]             Lấy task "Chờ làm" đủ phụ thuộc, giao song song tối đa Max (mặc định 2)
  .\team.ps1 try -Project Ten [-NoLaunch]           Build bản ĐÃ MERGE ra thư mục riêng (AITeam-try) rồi mở app để chủ dự án chạy thử
#>
param(
    [Parameter(Position = 0)] [string] $Command = "help",
    [string] $Project,
    [string] $Task,
    [string] $Feedback = "",
    [switch] $Resume,
    [switch] $Restart,
    [string] $Repo,
    [string] $Path,
    [switch] $New,
    [string] $Build = "",
    [string] $Test = "",
    [string] $Stack = "",
    [string] $Title = "",
    [string] $Agent = "A",
    [string] $Request = "",
    [string] $Kind = "",
    [ValidateSet("low", "medium", "high")] [string] $Risk = "medium",
    [ValidateSet("", "agy", "codex", "opencode")] [string] $Dev = "",
    [switch] $NoLaunch,
    [string] $Name = "",
    [string[]] $Names = @(),
    [string] $Prompt = "",
    [string] $Model = "",
    [switch] $NoVerify,
    [switch] $NoReview,
    [switch] $Force,
    [switch] $All,
    [int] $Max = 2,
    [string] $Skill = "",
    [AllowEmptyString()][string] $Text = "",
    [AllowEmptyString()][string] $Source = "",
    [string] $Optional = "",
    [string] $Note = "",
    [string] $Detail = "",
    [string] $Reject = "",
    [string] $Log = ""
)
$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Utf8 = New-Object Text.UTF8Encoding $false
# PowerShell 5.1 và Hub đọc cùng UTF-8, không phụ thuộc code page của máy.
[Console]::OutputEncoding = $Utf8
$OutputEncoding = $Utf8
# skill-note chỉ dùng kho skill, không phụ thuộc module điều phối pipeline.
. (Join-Path $Root "scripts/SkillPrompts.ps1")
. (Join-Path $Root "scripts/SkillNotes.ps1")
# Khóa chung Set-BoardStatus/Save-RunMetrics và ghi Prep/Assign/Verify/Qc/Merge cùng cập nhật bảng việc.

function Get-NextJournalRun([string]$repoPath, [string]$taskFileName, [string]$taskId, [string]$event) {
    try {
        $taskPath = Join-Path (Join-Path $repoPath "tasks") $taskFileName
        if (-not (Test-Path $taskPath)) { return 1 }
        $taskBytes = [IO.File]::ReadAllBytes($taskPath)
        $taskText = [Text.Encoding]::UTF8.GetString($taskBytes)
        $mReq = [regex]::Match($taskText, '\*\*Yêu cầu:\*\*\s*(R-\d+)')
        if (-not $mReq.Success) {
            $mReq = [regex]::Match($taskText, '\*\*Y[^\*]+cầu:\*\*\s*(R-\d+)')
        }
        if (-not $mReq.Success) { return 1 }
        $reqPath = Join-Path (Join-Path $repoPath "requests") "$($mReq.Groups[1].Value).md"
        if (-not (Test-Path $reqPath)) { return 1 }
        $reqBytes = [IO.File]::ReadAllBytes($reqPath)
        $reqText = [Text.Encoding]::UTF8.GetString($reqBytes)
        $pattern = [regex]::Escape("<!-- workflow:${taskId}:${event}:") + "(\d+) -->"
        $matches = [regex]::Matches($reqText, $pattern)
        $max = 0
        foreach ($m in $matches) {
            $val = [int]$m.Groups[1].Value
            if ($val -gt $max) { $max = $val }
        }
        return ($max + 1)
    } catch {
        return 1
    }
}

# ───────────────────────── Cấu hình ─────────────────────────

function Read-Json($file) { Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json }

function Get-Config {
    $cfg = Read-Json (Join-Path $Root "team.json")
    $localFile = Join-Path $Root "team.local.json"   # ghi đè theo máy (không lên git)
    if (Test-Path $localFile) {
        $local = Read-Json $localFile
        foreach ($p in "projectsRoot", "workRoot") { if ($local.$p) { $cfg.$p = $local.$p } }
        if ($local.agyExe) { $cfg.agy.exe = $local.agyExe }
    }
    return $cfg
}

# V8-06: hàm chọn thuần; thứ tự cấu hình được truyền vào, không đọc file trong hàm.
function Resolve-AgyModel($wanted, [string[]]$available, [string[]]$fallback = @(
    'claude-opus-4-6-thinking', 'claude-opus-5-5-medium', 'gemini-4-argon-high',
    'claude-sonnet-5-5-high', 'claude-sonnet-4-6', 'gemini-3.8-flash-high')) {
    if (-not $wanted) { return '' }
    if ($wanted -in $available) { return $wanted }
    foreach ($candidate in $fallback) { if ($candidate -in $available) { return $candidate } }
    return ''
}

function ConvertFrom-AgyModels([string]$output) {
    @($output -split '\r?\n' | ForEach-Object {
        if ($_ -match '^\s*([a-z0-9][a-z0-9.-]*)\t') { $Matches[1] }
    } | Select-Object -Unique)
}

function Get-AgyModels($cfg) {
    $cache = Join-Path $Root '.state/agy-models.json'
    if (Test-Path -LiteralPath $cache) {
        try {
            $saved = Read-Json $cache
            $age = ([DateTime]::UtcNow - [DateTime]::Parse($saved.fetchedAt).ToUniversalTime()).TotalSeconds
            if ($age -ge 0 -and $age -lt 3600 -and $null -ne $saved.models) { return $saved }
        } catch { Write-Warning "Không đọc được cache model: $_" }
    }
    $out = $null; $err = $null
    try {
        $null = New-Item -ItemType Directory -Force (Split-Path $cache)
        $out = Join-Path (Split-Path $cache) ('models-' + [guid]::NewGuid().ToString('N') + '.out')
        $err = $out + '.err'
        $proc = Start-Process -FilePath $cfg.agy.exe -ArgumentList 'models' -PassThru -NoNewWindow -RedirectStandardOutput $out -RedirectStandardError $err
        $null = $proc.Handle
        try {
            if (-not $proc.WaitForExit(60000)) { $proc.Kill(); throw 'agy models quá 60 giây.' }
            $proc.WaitForExit()
            if ($proc.ExitCode -ne 0) { throw "agy models trả mã $($proc.ExitCode)." }
        } finally { $proc.Dispose() }
        $models = @(ConvertFrom-AgyModels ([IO.File]::ReadAllText($out)))
        if ($models.Count -eq 0) { throw 'Không đọc được danh sách model tab-separated.' }
        $saved = @{ fetchedAt = [DateTime]::UtcNow.ToString('o'); models = $models }
        [IO.File]::WriteAllText($cache, ($saved | ConvertTo-Json), (New-Object Text.UTF8Encoding $true))
        return $saved
    } catch { Write-Warning "Giữ model cũ: $_"; return $null }
    finally {
        foreach ($file in @($out, $err)) { if ($file -and (Test-Path -LiteralPath $file)) { Remove-Item -LiteralPath $file } }
    }
}

function Get-Projects {
    # PowerShell 5.1: ConvertFrom-Json trả mảng JSON như MỘT đối tượng → phải "trải" ra từng phần tử.
    $raw = Read-Json (Join-Path $Root "projects.json")
    return @($raw | ForEach-Object { $_ })
}

function Save-Projects($list) {
    $json = ConvertTo-Json @($list) -Depth 6
    [IO.File]::WriteAllText((Join-Path $Root "projects.json"), $json, $Utf8)
}

function Resolve-Project([string]$name) {
    if (-not $name) { throw "Thiếu -Project. Dự án hiện có: $((Get-Projects).name -join ', ')" }
    $cfg = Get-Config
    $p = Get-Projects | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if (-not $p) { throw "Chưa có dự án '$name' trong projects.json. Dùng: .\team.ps1 add-project -Project $name ..." }
    $localPath = $p.path
    $work = if ($p.workDir) { $p.workDir } else { Join-Path $cfg.workRoot $name }
    $localFile = Join-Path $Root "team.local.json"
    if (Test-Path $localFile) {
        $local = Read-Json $localFile
        if ($local.paths -and $local.paths.$name) { $localPath = $local.paths.$name }
        if ($local.workDirs -and $local.workDirs.$name) { $work = $local.workDirs.$name }   # thư mục agent riêng của máy này
    }
    if (-not $localPath -or -not (Test-Path $localPath)) { $localPath = Join-Path $cfg.projectsRoot $name }
    [pscustomobject]@{ Info = $p; Path = $localPath; Work = $work; Logs = (Join-Path $work "logs") }
}

function Get-Gh {
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $default = "C:\Program Files\GitHub CLI\gh.exe"
    if (Test-Path $default) { return $default }
    return $null
}

function Say($text, $color = "Gray") { Write-Host $text -ForegroundColor $color }

# ───────────────────────── Kiểm tra máy ─────────────────────────

function Invoke-Doctor {
    $cfg = Get-Config
    $ok = $true
    Say "== Kiểm tra công cụ team ==" Cyan
    $ocModel = if ($cfg.opencode.model) { [string]$cfg.opencode.model } else { 'opencode/deepseek-v4.1-flash-free' }
    if ($ocModel -notmatch '(?i)deepseek') { Say '  [LỖI] OpenCode chỉ được dùng DeepSeek.' Red; $ok = $false }
    $ocCli = Get-AITeamExe
    if ($ocCli) {
        $ocInfo = & $ocCli opencode-check --root $Root
        if ($LASTEXITCODE -eq 0) { Say ('  [OK] OpenCode / DeepSeek: ' + ($ocInfo -join ' ')) Green }
        else { Say '  [THIẾU/LỖI] OpenCode CLI hoặc model; kiểm tra opencode.exe và opencode.model.' Yellow; $ok = $false }
    } else { Say '  [THIẾU] Chưa có aiteam CLI để kiểm tra OpenCode.' Yellow; $ok = $false }

    if (Get-Command git -ErrorAction SilentlyContinue) { Say "  [OK] git $((git --version) -replace 'git version ','')" Green } else { Say "  [THIẾU] git: winget install Git.Git" Red; $ok = $false }

    $gh = Get-Gh
    if ($gh) {
        $auth = & $gh auth status 2>&1 | Out-String
        if ($auth -match "Logged in to github.com account (\S+)") { Say "  [OK] GitHub: $($Matches[1])" Green }
        else { Say "  [CHƯA ĐĂNG NHẬP] GitHub: chạy  gh auth login --web" Yellow; $ok = $false }
    } else { Say "  [THIẾU] GitHub CLI: winget install GitHub.cli" Red; $ok = $false }

    if (Test-Path $cfg.agy.exe) {
        $ver = & $cfg.agy.exe --version 2>$null
        Say "  [OK] Antigravity CLI $ver ($($cfg.agy.exe))" Green
        $tmp = Join-Path $env:TEMP "agy-ping.json"
        $proc = Start-Process $cfg.agy.exe -ArgumentList '-p "Tra loi dung mot chu: OK" --output-format json --print-timeout 2m' `
            -NoNewWindow -Wait -PassThru -RedirectStandardInput (Get-EmptyInput) -RedirectStandardOutput $tmp -RedirectStandardError "$tmp.err"
        $out = (Get-Content $tmp -Raw -ErrorAction SilentlyContinue) + (Get-Content "$tmp.err" -Raw -ErrorAction SilentlyContinue)
        if ($out -match '"status":"SUCCESS"') { Say "  [OK] Antigravity đã đăng nhập, chạy ngầm được" Green }
        elseif ($out -match "auth|login|credential") { Say "  [CHƯA ĐĂNG NHẬP] Antigravity: chạy  .\team.ps1 switch-account" Yellow; $ok = $false }
        else { Say "  [LỖI] Antigravity: $($out.Trim() | Select-Object -First 1)" Red; $ok = $false }
    } else {
        Say "  [THIẾU] Antigravity CLI tại $($cfg.agy.exe)" Red
        Say "          Cài: irm https://antigravity.google/cli/install.ps1 -OutFile agy-install.ps1; .\agy-install.ps1 --dir C:\Tools\agy\bin" Red
        $ok = $false
    }

    $claudeMd = Join-Path $env:USERPROFILE ".claude\CLAUDE.md"
    if ((Test-Path $claudeMd) -and (Get-Content $claudeMd -Raw) -match [regex]::Escape("AITeam")) { Say "  [OK] Claude biết quy trình team ($claudeMd)" Green }
    else { Say "  [CHƯA GẮN] Claude chưa biết quy trình team: chạy  .\team.ps1 setup" Yellow; $ok = $false }

    foreach ($p in Get-Projects) {
        $r = Resolve-Project $p.name
        if (Test-Path (Join-Path $r.Path ".git")) { Say "  [OK] Dự án $($p.name): $($r.Path)" Green }
        else { Say "  [CHƯA CÓ TRÊN MÁY] $($p.name): .\team.ps1 add-project -Project $($p.name) -Repo $($p.repo)" Yellow }
    }
    if ($ok) { Say "Sẵn sàng." Green } else { Say "Còn mục cần xử lý (xem ở trên)." Yellow }
}

function Invoke-Setup {
    $claudeDir = Join-Path $env:USERPROFILE ".claude"
    New-Item -ItemType Directory -Force $claudeDir | Out-Null
    $claudeMd = Join-Path $claudeDir "CLAUDE.md"
    $playbook = Join-Path $Root "CLAUDE.md"
    $block = @"

<!-- AITeam: quy trình team AI của HieuDV -->
Khi người dùng giao việc cho "team" / "đội" / nhắc tới Antigravity, agent, dự án mới, hoặc làm việc trong dự án có trong
$Root\projects.json: ĐỌC VÀ LÀM THEO $playbook (bạn là quản lý team, điều phối bằng $Root\team.ps1).
"@
    $current = if (Test-Path $claudeMd) { [IO.File]::ReadAllText($claudeMd) } else { "" }
    # Bỏ khối cũ (có thể trỏ tới thư mục AITeam cũ nếu đã chuyển chỗ) rồi gắn lại khối đúng đường dẫn hiện tại.
    $pattern = '(?s)\r?\n<!-- AITeam: quy trình team AI của HieuDV -->\r?\n.*?team\.ps1\)\.'
    $cleaned = [regex]::Replace($current, $pattern, "")
    if ($cleaned -ne $current) { [IO.File]::WriteAllText($claudeMd, $cleaned, $Utf8) }
    [IO.File]::AppendAllText($claudeMd, $block, $Utf8)
    Say "Đã gắn quy trình team vào $claudeMd ($Root)" Green
    $cfg = Get-Config
    New-Item -ItemType Directory -Force $cfg.workRoot | Out-Null
    Invoke-Doctor
}

# ───────────────────────── Dự án ─────────────────────────

function Copy-Template([string]$name, [string]$target, [hashtable]$vars) {
    if (Test-Path $target) { return $false }
    $text = Get-Content (Join-Path $Root "templates\$name") -Raw -Encoding UTF8
    foreach ($k in $vars.Keys) { $text = $text.Replace("{{$k}}", [string]$vars[$k]) }
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
    [IO.File]::WriteAllText($target, $text, $Utf8)
    return $true
}

function Invoke-AddProject {
    if (-not $Project) { throw "Thiếu -Project" }
    $cfg = Get-Config
    $gh = Get-Gh
    $owner = $cfg.github.owner
    $repoName = if ($Repo) { $Repo } else { "$owner/$Project" }
    $target = if ($Path) { $Path } else { Join-Path $cfg.projectsRoot $Project }

    # Lệnh git/gh in thông báo ra stderr (ví dụ "repo không tồn tại"); PowerShell 5.1 + Stop coi là lỗi → tạm Continue.
    $ErrorActionPreference = "Continue"
    $existing = Get-Projects | Where-Object { $_.name -eq $Project } | Select-Object -First 1
    if (-not (Test-Path (Join-Path $target ".git"))) {
        $exists = $false
        if ($gh) { & $gh repo view $repoName --json name *> $null; $exists = ($LASTEXITCODE -eq 0) }
        if ($exists) {
            # Kho đã có trên GitHub: CHỈ clone. Không bao giờ git init (sẽ ra lịch sử riêng, không nối với GitHub).
            if ((Test-Path $target) -and (Get-ChildItem -Force $target | Select-Object -First 1)) {
                throw "Thư mục $target đã có file nhưng không phải bản clone của $repoName. Đổi tên/xóa thư mục đó rồi chạy lại."
            }
            Say "Clone $repoName → $target"
            & $gh repo clone $repoName $target
            if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $target ".git"))) { throw "Clone $repoName thất bại." }
        } else {
            New-Item -ItemType Directory -Force $target | Out-Null
            git -C $target init -b main | Out-Null
            Say "Đã tạo git trong $target"
        }
    }

    $vars = @{ PROJECT = $Project; BUILD = $(if ($Build) { $Build } else { "(điền lệnh build)" }); TEST = $(if ($Test) { $Test } else { "(điền lệnh test)" }) }
    if (Copy-Template "AGENTS.md" (Join-Path $target "AGENTS.md") $vars) { Say "  + AGENTS.md" }
    if (Copy-Template "board.md" (Join-Path $target "tasks\README.md") $vars) { Say "  + tasks\README.md" }
    if (Copy-Template "gitignore.txt" (Join-Path $target ".gitignore") $vars) { Say "  + .gitignore" }
    if (Copy-Template "editorconfig.txt" (Join-Path $target ".editorconfig") $vars) { Say "  + .editorconfig" }

    # Kho GitHub private
    $remote = git -C $target remote get-url origin 2>$null
    if (-not $remote -and $gh) {
        if (-not (git -C $target config user.name)) {
            git -C $target config user.name "HieuDV"
            git -C $target config user.email "$owner@users.noreply.github.com"
        }
        Install-Hook $target   # hook chặn bí mật có hiệu lực ngay từ commit đầu tiên
        git -C $target add -A
        git -C $target commit -q -m "Khởi tạo dự án $Project (team AI)" 2>$null
        & $gh repo create $repoName --private --source $target --remote origin --push | Out-Null
        Say "  + Kho private https://github.com/$repoName" Green
    }

    if ($existing) {
        # Đã có trong projects.json (thường gặp khi lấy dự án về máy mới): giữ nguyên workDir/build/test/areas.
        Install-Hook $target
        Say "Dự án $Project đã có trong projects.json, giữ nguyên cấu hình." Green
        return
    }
    $list = [Collections.ArrayList]@(Get-Projects)
    [void]$list.Add([pscustomobject]@{
        name = $Project; repo = $repoName; path = $target; stack = $Stack
        build = $Build; test = $Test
        areas = [pscustomobject]@{ A = "nền tảng / hạ tầng"; B = "tính năng" }
    })
    Save-Projects $list
    Install-Hook $target
    Say "Đã thêm dự án $Project vào projects.json" Green
}

function Invoke-NewTask {
    $r = Resolve-Project $Project
    if (-not $Task -or -not $Title) { throw "Cần -Task T-xxx và -Title" }
    $slug = ($Title.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
    if (-not $slug) { $slug = "task" }
    $file = Join-Path $r.Path "tasks\$Task-$slug.md"
    $branch = "agent-$($Agent.ToLower())/$Task-$slug"
    $vars = @{ TASK = $Task; TITLE = $Title; AGENT = $Agent; BRANCH = $branch; BUILD = $r.Info.build; TEST = $r.Info.test }
    if (Copy-Template "task.md" $file $vars) { Say "Đã tạo $file (nhánh $branch)" Green } else { Say "Đã có $file" Yellow }
    Say "Điền đủ các mục Ràng buộc / Dữ liệu mẫu thật / Giao diện / Test nghiệm thu trước khi giao."
    Say "Test nghiệm thu tham chiếu lớp chưa có → commit chúng làm commit đầu tiên trên nhánh $branch (không lên main) rồi mới assign."
}

# ───────────────────────── Giao việc ─────────────────────────

function Find-TaskFile($r, [string] $Task) {
    # Ưu tiên khớp đúng tên file (vd. -Task C-011-ui-layout-tests); sau đó mới khớp tiền tố "<mã>-*.md".
    $dir = Join-Path $r.Path "tasks"
    $exact = Join-Path $dir "$Task.md"
    if (Test-Path $exact) { return Get-Item $exact }
    $found = @(Get-ChildItem $dir -Filter "$Task-*.md")
    if ($found.Count -eq 0) { throw "Không thấy tasks\$Task-*.md trong $($r.Path)" }
    if ($found.Count -gt 1) { throw "Mã '$Task' khớp nhiều task: $(($found | ForEach-Object { $_.BaseName }) -join ', '). Ghi rõ hơn, vd. -Task $($found[0].BaseName)" }
    return $found[0]
}

# Số task 🟡 đang chạy của từng lập trình viên (đọc bảng việc).
function Get-AgentLoad($r) {
    $load = @{ A = 0; B = 0 }
    $board = Join-Path $r.Path "tasks\README.md"
    if (Test-Path $board) {
        foreach ($l in [IO.File]::ReadAllLines($board)) {
            if ($l -match '^\|' -and $l.Contains([string][char]::ConvertFromUtf32(0x1F7E1))) {
                $c = $l.Split('|')
                if ($c.Count -ge 3) { $a = $c[2].Trim().ToUpper(); if ($load.ContainsKey($a)) { $load[$a]++ } }
            }
        }
    }
    $load
}

# Người gợi ý đang bận hơn người kia ⇒ trả người kia; bằng nhau ⇒ giữ gợi ý. Không phải A / B (vd. C = Codex) ⇒ giữ nguyên.
function Select-FreeAgent($r, [string]$suggested) {
    if ($suggested -notin @('A', 'B')) { return $suggested }
    return Invoke-RepoTransaction -RepoPath $r.Path -Action {
    $state = Join-Path $r.Path '.state/assign-recent.json'
    $recent = @(); if (Test-Path -LiteralPath $state) { $recent = Get-Content -LiteralPath $state -Raw -Encoding UTF8 | ConvertFrom-Json }
    $recent = @($recent | Where-Object { [datetimeoffset]$_.At -gt [datetimeoffset]::Now.AddMinutes(-10) })
    $reserved = @($recent | Where-Object { $_.Task -eq $Task })
    if ($reserved.Count) { return $reserved[-1].Agent }
    $load = Get-AgentLoad $r
    $board = Join-Path $r.Path 'tasks/README.md'
    $rows = @(); if (Test-Path $board) { $rows = @([IO.File]::ReadAllLines($board)) }
    foreach ($entry in $recent) {
        $onBoard = @($rows | Where-Object { $_ -match ('(\[|\(|\|\s*)' + [regex]::Escape($entry.Task) + '\b') -and $_.Contains('🟡') })
        if (-not $onBoard.Count -and $load.ContainsKey($entry.Agent)) { $load[$entry.Agent]++ }
    }
    $other = if ($suggested -eq 'A') { 'B' } else { 'A' }
    if ($load[$suggested] -gt $load[$other]) { $suggested = $other }
    if ($Task) {
        $recent += [pscustomobject]@{ Task = $Task; Agent = $suggested; At = [datetimeoffset]::Now.ToString('o') }
        New-Item -ItemType Directory -Force (Split-Path $state) | Out-Null
        [IO.File]::WriteAllText($state, (ConvertTo-Json -InputObject @($recent)), (New-Object Text.UTF8Encoding $true))
    }
    $suggested
    }
}

function Invoke-LintTask([string]$WorkDir = '', [string]$TaskPath = '', [switch]$StructureOnly) {
    $lintProject = Resolve-Project $Project
    if (-not $WorkDir) { $WorkDir = $lintProject.Path }
    if (-not $TaskPath) { $TaskPath = (Find-TaskFile $lintProject $Task).FullName }
    $lintExe = Get-AITeamExe
    if (-not $lintExe) { throw 'Không build được CLI để lint task.' }
    $lintArgs = @('lint-task', '--root', $Root, '--workdir', $WorkDir, '--task-file', $TaskPath)
    if ($StructureOnly -or $Resume) { $lintArgs += '--resume' }
    & $lintExe @lintArgs
    if ($LASTEXITCODE -ne 0) { throw 'Task không đạt kiểm chất lượng TK/TN; chưa giao AI. Xem lý do phía trên.' }
}

# /// Trả ngưỡng từ cấu hình hợp nhất cfg.review.maxRounds; thiếu/không nguyên dương => 3.
function Get-ReviewRoundLimit($cfg) {
    $raw = $null
    try { $raw = $cfg.review.maxRounds } catch { }
    if ($null -eq $raw) { return 3 }
    $n = 0
    if ($raw -is [int] -and $raw -gt 0) { return $raw }
    if ($raw -is [string] -and [int]::TryParse($raw, [ref]$n) -and $n -gt 0) { return $n }
    return 3
}

# /// Chỉ gọi CLI V6-01 review-rounds --task-file <taskFile> --logs <r.Logs> --task <taskId>.
# /// Dùng Get-AITeamExe, truyền đối số rời, không đếm lại bằng PowerShell.
# /// Chỉ nhận mã 0 + đúng một số nguyên không âm; thiếu CLI/lỗi/output sai =>
# /// ném lỗi "Không xác định được số vòng review; dừng giao lại."; không coi là 0.
function Get-ReviewRoundCount($r, [string]$taskFile, [string]$taskId) {
    try {
        $exe = Get-AITeamExe
        if (-not $exe) { throw 'Thiếu CLI' }
        $arguments = @('review-rounds', '--task-file', $taskFile, '--logs', $r.Logs, '--task', $taskId)
        $stdout = @(& $exe @arguments 2>$null)
        $code = $LASTEXITCODE
        $value = 0
        $output = ($stdout -join "`n").Trim()
        if ($code -ne 0 -or $output -notmatch '\A[0-9]+\z' -or
            -not [int]::TryParse($output, [ref]$value)) { throw 'Kết quả CLI không hợp lệ' }
        return $value
    } catch {
        throw 'Không xác định được số vòng review; dừng giao lại.'
    }
}

# /// Trả false dưới ngưỡng. Đạt ngưỡng => tái dùng decision pending cùng project/task
# /// (đối chiếu title theo tiền tố task vì decision cũ có task=null), hoặc gọi
# /// Invoke-ApprovalRequest -Kind decision qua biến script hiện có, khôi phục biến sau gọi.
# /// Title: "Cần anh duyệt · <Task> · quá N vòng review".
# /// Detail: dự án, task, "Tổng/ngưỡng: N/M", "giao tiếp", "Claude tự sửa", "bỏ task".
# /// Ghi đúng "⏸ Chờ anh quyết · quá N vòng review", trả true; lỗi CLI cũng dừng,
# /// báo đúng câu lỗi ở Get-ReviewRoundCount, không tạo decision sai, không chạy agent.
# /// Không tự thực hiện lựa chọn. Lỗi ghi approval/bảng phải báo ra, không nuốt.
function Stop-ReviewReassignment($r, [string]$taskFile, [string]$taskId, $cfg) {
    $ErrorActionPreference = 'Stop'
    $count = Get-ReviewRoundCount $r $taskFile $taskId
    $limit = Get-ReviewRoundLimit $cfg
    if ($count -lt $limit) { return $false }
    # Tái dùng decision pending cùng project/task
    $prefix = "Cần anh duyệt · $taskId $([char]0x00B7) "
    $existing = @(Get-Approvals | Where-Object {
        $_.kind -eq 'decision' -and $_.status -eq 'pending' -and
        $_.project -eq $r.Info.name -and ($_.task -eq $taskId -or
            (-not $_.task -and $_.title -and $_.title.StartsWith($prefix, [StringComparison]::Ordinal)))
    })
    if (-not $existing.Count) {
        # Gọi Invoke-ApprovalRequest qua biến script
        $savedKind = $script:Kind; $savedTitle = $script:Title; $savedPrompt = $script:Prompt; $savedProject = $script:Project
        try {
            $script:Kind = 'decision'
            $script:Title = "Cần anh duyệt · $taskId · quá $count vòng review"
            $script:Prompt = "Dự án: $($r.Info.name)`nTask: $taskId`nTổng/ngưỡng: $count/$limit`n`nLựa chọn:`n- giao tiếp`n- Claude tự sửa`n- bỏ task"
            $script:Project = $r.Info.name
            $null = Invoke-ApprovalRequest
        } finally {
            $script:Kind = $savedKind; $script:Title = $savedTitle; $script:Prompt = $savedPrompt; $script:Project = $savedProject
        }
    }
    $null = Set-BoardStatus $r $taskId "⏸ Chờ anh quyết · quá $count vòng review"
    return $true
}

function Select-TaskDeveloper($r, $taskFile, [string]$text, $cfg) {
    $loads = @{ codex = 0; opencode = 0; agy = 0 }
    $board = Join-Path $r.Path 'tasks/README.md'
    if (Test-Path -LiteralPath $board) {
        foreach ($line in [IO.File]::ReadAllLines($board)) {
            if ($line -notmatch '^\|' -or -not $line.Contains('🟡')) { continue }
            $cells=$line.Split('|')
            if ($cells.Count -gt 3) {
                $id=$cells[2].Trim().ToUpperInvariant()
                if ($id -eq 'C') { $loads.codex++ } elseif ($id -eq 'O') { $loads.opencode++ } elseif ($id -in @('A','B')) { $loads.agy++ }
            }
        }
    }
    $required=if($cfg.agy.requiredModel){[string]$cfg.agy.requiredModel}else{'argon-4'}
    $agyInstalled=($cfg.agy.exe -and (Test-Path -LiteralPath $cfg.agy.exe))
    $models=@()
    if($agyInstalled){try{$modelState=Get-AgyModels $cfg}catch{$modelState=$null; Write-Warning "Không đọc được danh sách model agy: $_"}; if($modelState){$models=@($modelState.models)}}
    $pinned=[regex]::Match($text,'(?im)^\s*-\s*\*\*Dev:\*\*\s*`?(codex|opencode|agy)`?\s*$').Groups[1].Value.ToLowerInvariant()
    $difficulty=[regex]::Match($text,'\*\*Độ khó:\*\*\s*([SML])\b').Groups[1].Value
    if(-not $difficulty){$difficulty='L'}
    $budgetText=[regex]::Match($text,'\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
    $snapshot=@{
        Task=@{ Dev=$pinned; PrivateRepository= -not ($r.Info.private -is [bool] -and -not $r.Info.private); Difficulty=$difficulty;
            PlannedLines=$(if($budgetText){[int]$budgetText}else{$null}); Scope=@(Get-BacktickPaths (Get-Section $text 'Phạm vi'));
            ScopeLocked=(@(Get-ScopeOverlaps $r $taskFile $text).Count -gt 0) }
        ProjectPath=$r.Path; TaskId=$taskFile.BaseName;
        Loads=$loads; AgyModels=$models; AgyInstalled=[bool]$agyInstalled; RequiredAgyModel=$required
    }
    $routeFile=[IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($routeFile,($snapshot|ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding $false))
        $routeExe=Get-AITeamExe
        if(-not $routeExe){throw 'Không build được CLI định tuyến.'}
        $routeJson=@(& $routeExe route-developer --root $Root --input $routeFile)
        if($LASTEXITCODE -ne 0){throw 'Không chọn được người làm: CLI định tuyến lỗi.'}
        ($routeJson -join "`n") | ConvertFrom-Json
    } finally {Remove-Item -LiteralPath $routeFile -ErrorAction SilentlyContinue}
}

function ConvertTo-DeveloperProvider([string]$code) {
    switch ($code.Trim().ToUpperInvariant()) {
        'C' { 'codex' }
        'O' { 'opencode' }
        { $_ -in @('A', 'B') } { 'agy' }
        default { $null }
    }
}

# /// -Resume giữ đúng người đã làm: provider đã ghi trong .state/developer-routes.json,
# /// nếu chưa có thì mã Agent trong file task / bảng việc (C ⇒ codex, O ⇒ opencode, A/B ⇒ agy).
# /// Không xác định được ⇒ trả $null để giữ đường assign cũ, KHÔNG gọi CLI định tuyến.
function Get-RecordedDeveloper($r, $taskFile, [string]$text) {
    $taskKey = [regex]::Match([IO.Path]::GetFileNameWithoutExtension($taskFile.Name), '^[A-Za-z][A-Za-z0-9]*-\d+').Value.ToUpperInvariant()
    $stateFile = Join-Path $Root '.state/developer-routes.json'
    if ($taskKey -and (Test-Path -LiteralPath $stateFile -PathType Leaf)) {
        $full = [IO.Path]::GetFullPath([string]$r.Path).ToUpperInvariant()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($full)) } finally { $sha.Dispose() }
        $key = ([BitConverter]::ToString($hash) -replace '-', '') + ':' + $taskKey
        $map = Get-Content -LiteralPath $stateFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $entry = $map.PSObject.Properties | Where-Object { $_.Name -eq $key } | Select-Object -First 1
        if ($entry) {
            $recorded = ([string]$entry.Value).Trim().ToLowerInvariant()
            if ($recorded -in @('codex', 'opencode', 'agy')) { return $recorded }
        }
    }
    $provider = ConvertTo-DeveloperProvider ([regex]::Match($text, '(?im)^\s*-\s*\*\*Agent:\*\*\s*([A-Za-z]+)').Groups[1].Value)
    if ($provider) { return $provider }
    $board = Join-Path $r.Path 'tasks/README.md'
    if (Test-Path -LiteralPath $board -PathType Leaf) {
        foreach ($line in [IO.File]::ReadAllLines($board)) {
            if ($line -notmatch '^\|') { continue }
            $cells = $line.Split('|')
            if ($cells.Count -lt 4) { continue }
            if ([regex]::Match($cells[1], '[A-Za-z][A-Za-z0-9]*-\d+').Value.ToUpperInvariant() -ne $taskKey) { continue }
            $provider = ConvertTo-DeveloperProvider $cells[2]
            if ($provider) { return $provider }
        }
    }
    return $null
}

# V9-15: khóa phạm vi file do lõi C# (ScopeLockStore) giữ — một nguồn logic với CLI, không đọc/ghi locks.json bằng PowerShell.
# Trả $true khi lấy/nhả xong; trả mã task đang giữ khi CLI báo mã 3 + dòng "HELD <task>"; mã khác 0 thì throw kèm thông báo CLI.
function Invoke-ScopeLock {
    param(
        [ValidateSet('acquire', 'release')][string]$Action,
        [string]$TaskId,
        [string]$Provider,
        [string]$TaskFile,
        [string]$BoardFile,
        [string]$ProjectPath
    )
    $exe = Get-AITeamExe
    if (-not $exe) { throw 'Không build được CLI (AITeam).' }
    $cliArgs = @($(if ($Action -eq 'acquire') { 'lock-acquire' } else { 'lock-release' }), '--root', $Root, '--task', $TaskId)
    if ($Action -eq 'acquire') {
        $cliArgs += @('--provider', $Provider, '--task-file', $TaskFile)
        if ($BoardFile) { $cliArgs += @('--board', $BoardFile) }
    }
    if ($ProjectPath) { $cliArgs += @('--project', $ProjectPath) }
    $output = @(& $exe @cliArgs 2>&1 | ForEach-Object { [string]$_ })
    $code = $LASTEXITCODE
    if ($code -eq 0) { return $true }
    if ($Action -eq 'acquire' -and $code -eq 3) {
        foreach ($line in $output) {
            $held = [regex]::Match($line, '^\s*HELD\s+(?<task>\S+)')
            if ($held.Success) { return $held.Groups['task'].Value }
        }
        return ''
    }
    $message = ($output | Where-Object { $_.Trim() }) -join ' '
    $verb = if ($Action -eq 'acquire') { 'lấy' } else { 'nhả' }
    throw "Không $verb được khóa phạm vi$(if ($message) { ": $message" } else { '.' })"
}

function Invoke-Assign {
    if ($Task -cnotmatch '\A[A-Za-z0-9][A-Za-z0-9-]{0,63}\z') { throw 'LỖI: mã task không hợp lệ.' }
    $cfg = Get-Config
    $r = Resolve-Project $Project
    if (-not $Task) { throw "Thiếu -Task" }
    $taskFile = Find-TaskFile $r $Task
    if ($Resume -and (Stop-ReviewReassignment $r $taskFile.FullName $Task $cfg)) { return }
    $text = Get-Content $taskFile.FullName -Raw -Encoding UTF8
    $routeReason=$null
    $devFromResume=$false
    if (-not $Dev) {
        if ($Resume) {
            # Giao lại sau review: KHÔNG định tuyến lại, giữ người đã làm task này.
            $Dev=Get-RecordedDeveloper $r $taskFile $text
            $devFromResume=[bool]$Dev
        } else {
            $selection=Select-TaskDeveloper $r $taskFile $text $cfg
            $routeReason=[regex]::Replace([string]$selection.Reason,'[\r\n|<>]',' ')
            if (-not $selection.ProviderId) {
                Say "Chờ người làm: $routeReason; thử lại: $($selection.RetryAt)" Yellow
                Set-BoardStatus $r $Task '⏸ Chờ người làm' -Journal @{TaskFile=$taskFile.Name;Event='Assign';Result="ĐỊNH TUYẾN: $routeReason"}
                return
            }
            $Dev=[string]$selection.ProviderId
            Say "Định tuyến: $Dev — $routeReason" Cyan
        }
    }
    if ($Dev -eq 'opencode') {
        $ocModel = if ($cfg.opencode.model) { [string]$cfg.opencode.model } else { 'opencode/deepseek-v4.1-flash-free' }
        if ($ocModel -notmatch '(?i)deepseek') { throw 'OpenCode chỉ được dùng model DeepSeek.' }
        $knownPublic = $r.Info.private -is [bool] -and -not $r.Info.private
        if (-not $knownPublic -and $cfg.opencode.paid -ne $true) { throw 'Repo private/không rõ riêng tư không được chạy OpenCode free; khai báo private=false cho repo public hoặc opencode.paid=true.' }
    }

    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $agentId = [regex]::Match($text, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
    if (-not $branch) { throw 'File task thiếu dòng **Nhánh:** `tên-nhánh`' }

    # Chia đều (chủ dự án 25/10): Minh / Lan dùng chung 1 tài khoản Antigravity, cùng code chính ⇒ người được gợi ý đang bận
    # còn người kia rảnh thì giao người rảnh (ghi lại Agent trong file task + bảng việc trên main). -Resume giữ nguyên người cũ.
    if (-not $Resume -and $agentId -in @('A', 'B') -and -not (Test-Path (Join-Path $r.Work ("$agentId-$Task".ToLower()))) -and -not (Test-BranchPrepared $r $branch)) {
        $free = Select-FreeAgent $r $agentId
        if ($free -ne $agentId) {
            Say "Chia đều việc: Agent $agentId đang bận, Agent $free rảnh ⇒ giao Agent $free." Cyan
            Invoke-RepoTransaction -RepoPath $r.Path -Action {
            $text = ([regex]'(\*\*Agent:\*\*\s*)\S+').Replace($text, "`${1}$free", 1)
            [IO.File]::WriteAllText($taskFile.FullName, $text, (New-Object Text.UTF8Encoding (Test-HasBom ([IO.File]::ReadAllBytes($taskFile.FullName)))))
            $board = Join-Path $r.Path "tasks\README.md"
            if (Test-Path $board) {
                $bt = [IO.File]::ReadAllText($board)
                $bl = $bt -split "`r?`n"
                for ($i = 0; $i -lt $bl.Count; $i++) {
                    if ($bl[$i] -match '^\|' -and $bl[$i] -match ("(\[|\(|\|\s*)" + [regex]::Escape($Task) + "\b")) {
                        $cells = $bl[$i].Split('|'); if ($cells.Count -ge 3) { $cells[2] = " $free "; $bl[$i] = $cells -join '|' }; break
                    }
                }
                $nl = if ($bt -match "`r`n") { "`r`n" } else { "`n" }
                [IO.File]::WriteAllText($board, ($bl -join $nl), (New-Object Text.UTF8Encoding (Test-HasBom ([IO.File]::ReadAllBytes($board)))))
            }
            $eapSw = $ErrorActionPreference; $ErrorActionPreference = "Continue"
            git -C $r.Path add -- "tasks/$($taskFile.Name)" "tasks/README.md" 2>$null
            git -C $r.Path commit -q -m "${Task}: chia đều ⇒ Agent $free" -- "tasks/$($taskFile.Name)" "tasks/README.md" 2>&1 | Out-Null
            git -C $r.Path push -q 2>&1 | Out-Null
            $ErrorActionPreference = $eapSw
            }
            $text = [IO.File]::ReadAllText($taskFile.FullName)
            $agentId = $free
        }
    }

    Assert-NotClaimedElsewhere $r $Task
    foreach ($o in (Get-ScopeOverlaps $r $taskFile $text)) {
        Say "CẢNH BÁO: trùng phạm vi với task đang chạy $o ⇒ dễ xung đột khi merge (cân nhắc giao tuần tự)." Yellow
    }
    # V9-15: khóa các đường dẫn trong Phạm vi trước khi giao; task khác đang giữ vùng giao nhau thì không chạy.
    $lockHeld = $false
    $lockTaskKey = $Task.ToUpperInvariant()
    if ($null -eq $script:ScopeLockOwn) { $script:ScopeLockOwn = @{} }
    # Gọi lại Invoke-Assign trong cùng lượt (tự sửa / review) không khóa lại và không nhả khóa của lượt ngoài.
    if (-not $script:ScopeLockOwn.ContainsKey($lockTaskKey)) {
        $lockHolder = Invoke-ScopeLock -Action acquire -TaskId $Task -Provider $Dev -TaskFile $taskFile.FullName -BoardFile (Join-Path $r.Path 'tasks/README.md') -ProjectPath $r.Path
        if ($lockHolder -is [string]) {
            Say "Chờ khóa phạm vi: $lockHolder đang giữ." Yellow
            throw "Phạm vi task đang bị khóa bởi $lockHolder."
        }
        $script:ScopeLockOwn[$lockTaskKey] = $true
        $lockHeld = $true
    }
    try {
    if ($routeReason) {
        Set-BoardStatus $r $Task (Get-ClaimText) -Journal @{TaskFile=$taskFile.Name;Event='Assign';Result="ĐỊNH TUYẾN $Dev — $routeReason"}
    } else { Set-BoardStatus $r $Task (Get-ClaimText) }

    New-Item -ItemType Directory -Force $r.Logs | Out-Null
    $work = Join-Path $r.Work ("$agentId-$Task".ToLower())
    git -C $r.Path fetch origin --quiet
    if ($LASTEXITCODE -ne 0) { throw "Không fetch được origin trước khi giao task." }
    # Nhánh đã có worktree (vd. lần trước gõ -Task ngắn hơn) ⇒ dùng lại đúng thư mục đó thay vì báo lỗi "already used by worktree".
    $defaultWork = $work
    $work = Resolve-TaskWork $r $branch $defaultWork
    if ($work -ne $defaultWork) {
        Say "Nhánh $branch đã có thư mục làm việc → dùng lại $work" Yellow
    }
    if (-not (Test-Path $work)) {
        $remoteHas = git -C $r.Path ls-remote --heads origin $branch
        if ($remoteHas) { git -C $r.Path worktree add $work -B $branch "origin/$branch" --quiet }
        else { git -C $r.Path worktree add $work -B $branch origin/main --quiet }
    }
    $eap = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        git -C $work fetch origin --quiet 2>$null
        if ($LASTEXITCODE -ne 0) { throw "Không fetch được origin trong worktree; chưa giao task." }
        git -C $work merge origin/main --no-edit --quiet 2>$null
        if ($LASTEXITCODE -ne 0) { throw "Không cập nhật được origin/main (có thể xung đột); chưa giao task." }
    } finally { $ErrorActionPreference = $eap }

    try { Invoke-LintTask -WorkDir $work -TaskPath (Join-Path $work "tasks/$($taskFile.Name)") -StructureOnly:$Resume }
    catch { Set-BoardStatus $r $Task "Bị chặn"; throw }

    $devInfo = $cfg.developers | Where-Object { $_.id -eq $agentId } | Select-Object -First 1
    # Mã agent chưa khai báo trong team.json (vd. C = Codex lần đầu, R-004) ⇒ mặc định, không để null làm sập assign.
    if (-not $devInfo) { $devInfo = [pscustomobject]@{ id = $agentId; name = "Agent $agentId"; focus = "việc được giao trong file task" } }
    # Lưu ý riêng của nhân viên (rút ra từ các lần review trước) — staff/<mã>-*.md, mục "Lưu ý riêng".
    $staffNotes = ""
    $staffFile = Get-ChildItem (Join-Path $Root "staff") -Filter "$agentId-*.md" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($staffFile) {
        $notes = (Get-Section (Get-Content $staffFile.FullName -Raw -Encoding UTF8) "Lưu ý riêng").Trim()
        $notes = ($notes -split "`r?`n" | Where-Object { $_ -match '^\s*-\s+\S' }) -join "`n"
        if ($notes) { $staffNotes = "`nLƯU Ý RIÊNG CHO BẠN (từ các lần review trước — bắt buộc tránh lặp lại):`n$notes" }
    }
    $null = Set-ManagerIdentity $r.Path $cfg.github.owner
    Set-AgentIdentity $r.Path $work $devInfo $r.Info.name
    # V5-02 (R-005): MCP Gateway cho agy ⇒ agent báo tiến độ lên Văn phòng. Codex hiện tắt MCP theo lần gọi.
    $mcpNote = ""
    if ($Dev -notin @("codex", "opencode") -and (Set-McpConfig $r $work $taskFile $agentId)) {
        $mcpNote = "`nBÁO TIẾN ĐỘ QUA MCP (chủ dự án theo dõi trên trang Văn phòng): có công cụ aiteam_post_message. Gọi NGẮN (1 câu): khi bắt đầu; sau mỗi phần chạy được; khi gặp vướng cần quản lý quyết (kind question); khi xong (kind done, kèm số test pass). Không gọi quá 15 lần. Công cụ khác: aiteam_get_test_results (kết quả cổng kiểm tra lần trước)."
    }
    $prompt = @"
Bạn là $($devInfo.name) (mã $($devInfo.id), chuyên: $($devInfo.focus)) trong team AI của dự án $($r.Info.name). Quản lý là Claude.$staffNotes$mcpNote
Thư mục hiện tại là bản làm việc riêng của bạn, đã ở đúng nhánh $branch.
Đọc AGENTS.md rồi làm task tasks/$($taskFile.Name) đến hết "Tiêu chí hoàn thành".
Build: $($r.Info.build)   Test: $($r.Info.test)
Xong thì điền mục "Báo cáo của agent" trong file task, commit rồi git push -u origin $branch.
Bạn đang chạy tự động, KHÔNG có người trả lời câu hỏi: tự quyết theo AGENTS.md.
Kế hoạch của bạn coi như ĐÃ ĐƯỢC DUYỆT: không dừng lại xin xác nhận, làm luôn tới khi push xong.
Nếu bị chặn thật sự, ghi lý do vào báo cáo, commit, push rồi dừng.
File trong mục "Test nghiệm thu" của task do Claude viết trước: KHÔNG sửa, KHÔNG xóa; chỉ sửa code cho chúng pass.
Báo cáo của agent: điền bảng từng tiêu chí hoàn thành → bằng chứng (lệnh đã chạy + kết quả thật). Chưa chạy thì ghi chưa chạy.
Làm theo mục "Lỗi hay gặp" trong AGENTS.md (giữ BOM UTF-8 của file, không test return sớm, giao diện không đặt tọa độ cố định...).
Sau khi bạn push, cổng kiểm tra tự động chạy: đã commit + push hết, BOM, build 0 warning, test pass, đúng phạm vi, test nghiệm thu còn nguyên.
VAI TRÒ CỦA BẠN: CHỈ VIẾT CODE. Claude đã phân tích, thiết kế, viết khung + test nghiệm thu; Claude sẽ review, chụp ảnh giao diện, chạy app.
- CHỈ đọc các file ở mục "Đọc" trong task (và AGENTS.md mục Lỗi hay gặp). Không lục tìm, không đọc tài liệu khác.
- KHÔNG chụp ảnh, KHÔNG chạy app, KHÔNG tự viết công cụ phụ. Giao diện: làm đúng bố cục ghi trong task, kiểm bằng test bố cục có sẵn.
- Commit sớm và thường xuyên (sau mỗi phần chạy được): bị dừng giữa chừng vẫn không mất việc. Có giới hạn thời gian cho task.
- Báo cáo của agent: TỐI ĐA 15 dòng — bảng tiêu chí → lệnh + kết quả thật, file đã đổi, điều chưa chắc. Không viết dài.
CHỈ LÀM ĐÚNG THIẾT KẾ:
- Không thêm tính năng, tùy chọn, lớp, màn hình, file nào ngoài mục "Mục tiêu" và "Phạm vi" của task. Thấy nên làm thêm thì GHI vào báo cáo, không tự làm.
- Có khung code sẵn (chữ ký hàm / interface / TODO do Claude viết) thì giữ nguyên chữ ký, chỉ điền phần thân.
- Test: viết đúng các trường hợp trong "Tiêu chí hoàn thành", không nhân bản thành hàng chục test na ná nhau.
- Task có dòng "Ngân sách" (số dòng code): giữ trong ngân sách; cổng kiểm tra từ chối nếu vượt quá 1,5 lần.KHÔNG ĐỂ TREO (đã mất 30 phút / lượt vì lỗi này):
- Mọi lệnh dotnet test thêm: --blame-hang-timeout 2m. Không chạy lệnh ở chế độ nền rồi ngồi chờ; lệnh nào quá 3 phút không kết thúc thì dừng nó và xem vì sao.
- Test giao diện: TUYỆT ĐỐI không gọi Show(), ShowDialog(), MessageBox, Application.Run, không mở app thật. Dựng form / control ẩn (CreateControl, PerformLayout) trên luồng STA riêng; đặt Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException) để lỗi ném ra thay vì bật hộp thoại. Code dựng màn hình có hộp thoại chào mừng / thông báo thì phải tắt được qua tham số hoặc cờ trong test.LÀM NHANH (thời gian chạy được đo):
- Đọc PROJECT_MAP.md / AGENTS.md rồi chỉ mở đúng file cần sửa; không đọc lại cả file đã đọc, không in nguyên file dài ra màn hình.
- Trong lúc làm: build tăng dần (không clean), thêm --no-restore sau lần restore đầu; chạy RIÊNG test liên quan bằng dotnet test --no-build --filter "FullyQualifiedName~TênLớpTest".
- Chạy đủ lệnh Build + Test của dự án đúng 1 lần ở cuối, trước khi commit.
- Sửa file bằng chỉnh sửa nhỏ đúng chỗ, không viết lại cả file khi chỉ đổi vài dòng.
"@
    $prompt = Add-SkillPrompt -Prompt $prompt -Root $Root -Stage code -Stack $r.Info.stack
    try {
        $projPath = Join-Path $Root "core\AITeam.Cli\AITeam.Cli.csproj"
        if (Test-Path -LiteralPath $projPath) {
            $rulesExe = Get-AITeamExe
            if ($rulesExe -and $rulesExe -ne 'FakeProvider' -and (Test-Path -LiteralPath $rulesExe -PathType Leaf)) {
                $rulesArgs = @('rules-for-task', '--root', $Root, '--task-file', $taskFile.FullName, '--step', 'code')
                if ($Dev) { $rulesArgs += @('--ai', $Dev) }
                $rulesSection = (& $rulesExe @rulesArgs 2>$null) -join "`n"
                if ($LASTEXITCODE -eq 0) {
                    if (-not [string]::IsNullOrWhiteSpace($rulesSection)) {
                        $prompt += "`n`n" + $rulesSection.Trim()
                    }
                } else {
                    Write-Warning "Không lấy được luật chuẩn cho task $($taskFile.Name) (CLI trả mã $LASTEXITCODE)."
                }
            } elseif ($rulesExe -and $rulesExe -ne 'FakeProvider') {
                Write-Warning "Chưa có aiteam CLI để lấy luật chuẩn cho task $($taskFile.Name)."
            }
        }
    } catch {
        if ($_.Exception.Message -ne 'PROVIDER_CAPTURED') {
            Write-Warning "Lỗi khi lấy luật chuẩn cho task $($taskFile.Name): $_"
        } else {
            throw
        }
    }
    if ($Feedback) { $prompt += "`nYÊU CẦU CỦA QUẢN LÝ: $Feedback`nĐọc kỹ mục 'Review (Claude)' trong file task nếu có." }

        $n = @(Get-ChildItem $r.Logs -Filter "$Task-*.json" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'prompt' }).Count + 1
    $log = Join-Path $r.Logs "$Task-$n.json"
    # PowerShell 5.1 truyền sai tham số có dấu ngoặc kép / xuống dòng → lời giao việc nằm trong file, chỉ truyền 1 câu ngắn.
    $promptFile = Join-Path $r.Logs "$Task-$n.prompt.md"
    [IO.File]::WriteAllText($promptFile, $prompt, $Utf8)
    $short = "Doc file $promptFile (UTF-8, tieng Viet) va lam dung theo huong dan trong do."
    # Giới hạn thời gian theo task ("- **Thời gian tối đa:** 25 phút"), mặc định 30 phút — chặn chạy lan man (T-007: 73 phút cho 51 dòng).
    $maxMin = [regex]::Match($text, '\*\*Thời gian tối đa:\*\*\s*(\d+)').Groups[1].Value
    if (-not $maxMin) { $maxMin = "30" }
    $argLine = "-p `"$short`" --output-format json --print-timeout ${maxMin}m --dangerously-skip-permissions --add-dir `"$work`" --add-dir `"$($r.Logs)`""
    # Model theo task (dòng **Model:** `tên` trong file task) > model mặc định trong team.json.
    $taskModel = [regex]::Match($text, '\*\*Model:\*\*\s*`([^`]+)`').Groups[1].Value
    $model = if ($taskModel) { $taskModel } else { $cfg.agy.model }
    if ($Dev -eq 'opencode') { $model = $ocModel }
    if ($Dev -notin @('codex', 'opencode') -and $model) {
        $available = Get-AgyModels $cfg
        if ($null -ne $available) {
            $resolved = if ($null -ne $cfg.agy.modelFallback) {
                Resolve-AgyModel $model $available.models $cfg.agy.modelFallback
            } else { Resolve-AgyModel $model $available.models }
            if ($resolved -ne $model) { Say "Model $model không có trên tài khoản ⇒ dùng $(if ($resolved) { $resolved } else { 'mặc định của agy' })" Yellow }
            $model = $resolved
        }
    }
    if ($model) { $argLine += " --model `"$model`""; Say "Model: $model" }
    if ($Resume) {
        $last = Get-ChildItem $r.Logs -Filter "$Task-*.json" | Where-Object { $_.Length -gt 0 -and $_.Name -notmatch 'prompt' } |
            Sort-Object LastWriteTime | Select-Object -Last 1
        $id = if ($last) { [regex]::Match((Get-Content $last.FullName -Raw), '"conversation_id":"([^"]+)"').Groups[1].Value }
        if ($id) { $argLine += " --conversation $id"; Say "Tiếp tục hội thoại $id" }
    }

    Say "[$(Get-Date -Format HH:mm:ss)] $($r.Info.name) $Task → $(Get-StaffName $agentId), nhánh $branch, thư mục $work" Cyan
    # Start-Process: agy in trạng thái ra stderr; gọi trực tiếp thì PowerShell 5.1 coi là lỗi và dừng script.
    $err = $log -replace '\.json$', '.stderr.txt'
    $mainBefore = (git -C $r.Path rev-parse origin/main 2>$null)
    $devProvider = "agy"
    $logText = ""; $errText = ""
    if ($Dev -notin @("codex", "opencode")) {
        $proc = Start-Process -FilePath $cfg.agy.exe -ArgumentList $argLine -WorkingDirectory $work -NoNewWindow -Wait -PassThru -RedirectStandardInput (Get-EmptyInput) `
            -RedirectStandardOutput $log -RedirectStandardError $err
        $logText = Get-Content $log -Raw -ErrorAction SilentlyContinue
        $errText = Get-Content $err -Raw -ErrorAction SilentlyContinue
        Say "[$(Get-Date -Format HH:mm:ss)] $Task kết thúc, mã $($proc.ExitCode), log: $log" Cyan
    }
    if ("$logText`n$errText" -match '(?i)invalid model selection') {
        Clear-AgyModelCache
        Set-BoardStatus $r $Task '🔴 Lỗi agent · model không hợp lệ' -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = 'LỖI' }
        if ($NoVerify) { return @{ Ok = $false; Quota = $false } }; return
    }
    if ($errText -match 'waiting up to \d+m\d+s for \d+ background task') {
        Say "CẢNH BÁO: agent đã ngồi chờ một lệnh nền bị treo (agy chờ tối đa 30 phút rồi cắt). Lượt này có thể chưa commit / chưa báo cáo." Yellow
    }
    if (Test-QuotaExhausted $logText $errText) {
        Say "HẾT HẠN MỨC tài khoản Antigravity hiện tại → chạy .\team.ps1 switch-account rồi giao lại với -Resume" Yellow
    }
    if (Test-NetworkError $logText $errText) {
        Say "LỖI MẠNG khi agent chạy (mất kết nối tới máy chủ AI) — kiểm tra mạng rồi giao lại với -Resume; KHÔNG phải hết hạn mức." Yellow
    }
    if (Set-ManagerIdentity $r.Path $cfg.github.owner) {
        Say "Agent đã ghi đè tên commit chung của repo → đã trả lại HieuDV (agent chỉ dùng tên riêng của worktree)." Yellow
    }
    $pushed = git -C $r.Path ls-remote --heads origin $branch
    Say ("Nhánh trên GitHub: " + $(if ($pushed) { "đã push" } else { "CHƯA push" }))
    Test-AgentTouchedMain $r $work $mainBefore

    # R-004: -Dev codex ⇒ Codex viết code; agy hết hạn mức ⇒ tự chuyển sang Codex (trừ -Dev agy). Codex không ghi được .git ⇒ team.ps1 commit + push.
    $agyQuota = ($Dev -notin @("codex", "opencode")) -and (Test-QuotaExhausted $logText $errText)
    if ($Dev -in @("codex", "opencode") -or ($agyQuota -and ($Dev -ne "agy" -or $devFromResume))) {
        if ($agyQuota) {
            Save-RunMetrics (Get-RunRecord $r $Task $log $agentId $model $work $null $false)
            Say "Antigravity hết hạn mức → tự chuyển $Task sang Codex (dự phòng)." Yellow
            $log = Join-Path $r.Logs ("$Task-{0}.json" -f ($n + 1))
        }
        $devProvider = if ($Dev -eq 'opencode') { 'opencode' } else { 'codex' }
        $model = if ($devProvider -eq 'opencode') { $ocModel } else { 'codex' }
        $cx = Invoke-CodexDevelop $r $Task $work $branch $prompt $log $maxMin $devProvider $(if ($devProvider -eq 'opencode') { $ocModel } else { $null }) $(if ($devProvider -eq 'opencode') { $knownPublic } else { $false })
        if (-not $cx.Ok) {
            Save-RunMetrics (Get-RunRecord $r $Task $log $agentId $model $work $null $false)
            Set-BoardStatus $r $Task "🟡 Dừng · Codex: $($cx.Status)" -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = 'LỖI' }
            return
        }
    } elseif ($agyQuota) { Save-RunMetrics (Get-RunRecord $r $Task $log $agentId $model $work $null $false); Set-BoardStatus $r $Task "🟡 Dừng · hết hạn mức Antigravity" -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = 'LỖI' }; if ($NoVerify) { return @{ Ok = $false; Quota = $true } }; return }
    if ($NoVerify) {
        Save-RunMetrics (Get-RunRecord $r $Task $log $agentId $model $work $null $false)
        $agentOk = if ($devProvider -in @('codex','opencode')) { [bool]$cx.Ok } else { $proc -and $proc.ExitCode -eq 0 -and $logText -match '"status"\s*:\s*"SUCCESS"' }
        Set-BoardStatus $r $Task $(if ($agentOk) { "🔵 Chờ review" } else { "🔴 Lỗi agent" }) -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = $(if ($agentOk) { 'XONG' } else { 'LỖI' }) }
        return @{ Ok = [bool]$agentOk; Quota = $false }
    }
    if ($devProvider -eq 'codex') { dotnet build-server shutdown 2>&1 | Out-Null }
    $gate = Invoke-WorkflowGate -Verify { Invoke-Verify } -Qc {
        if ($devProvider -eq 'opencode') {
            if ($NoReview) { return $null }
            return @{ Code = 3; Json = @{ summary = 'OpenCode đã code; Claude review theo quy trình v5.' }; Out = '' }
        }
        Invoke-AutoReview $r $taskFile.Name $work $devProvider
    }
    $v = $gate.Verify
    $rv = $gate.Review
    if ($rv) {
        $qcRes = switch ($rv.Code) {
            0 { 'PASS' }
            1 { 'CẦN SỬA' }
            3 { 'BLOCKED' }
            default { 'LỖI' }
        }
        Invoke-RepoTransaction -RepoPath $r.Path -Action {
        $qcRun = Get-NextJournalRun $r.Path $taskFile.Name $Task 'Qc'
        Update-WorkflowJournal -RepoPath $r.Path -TaskFile $taskFile.Name -TaskId $Task -Event 'Qc' -Run $qcRun -Result $qcRes
        }
    }
    # Tự giao lại tối đa 1 lần / lệnh assign (R-004: cờ chưa từng được bật ⇒ V4-11 bị giao lại ~28 lần khi lỗi mạng NU1900).
    $script:AssignRuns = [int]$script:AssignRuns + 1
    if (-not $v.Ok -and -not $script:VerifyRetried -and $script:AssignRuns -lt 3) {
        $script:VerifyRetried = $true
        # Lỗi cơ học (chưa commit / build / test / BOM...) → trả agent sửa luôn 1 lần, không tốn lượt review của Claude.
        $runMetrics = Get-RunRecord $r $Task $log $agentId $model $work $v $true
        Save-RunMetrics $runMetrics
        if ($runMetrics.pause) {
            Set-BoardStatus $r $Task '⏸ Tạm dừng · có thể test nghiệm thu sai (TN)'
            return
        }
        Say "Cổng kiểm tra KHÔNG ĐẠT → tự giao lại cho $(Get-StaffName $agentId) 1 lần kèm danh sách lỗi." Yellow
        Set-BoardStatus $r $Task '🔴 Cần sửa · tự giao lại' -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = 'KHÔNG ĐẠT' }
        $script:Resume = $true
        $script:Feedback = "Cổng kiểm tra tự động KHÔNG ĐẠT. Sửa hết các mục sau, chạy lại build + test, cập nhật báo cáo, commit và push:`n- " + ($v.Fails -join "`n- ")
        Invoke-Assign
        return
    }
    $runMetrics = Get-RunRecord $r $Task $log $agentId $model $work $v $false
    Save-RunMetrics $runMetrics
    if ($runMetrics.pause) {
        Set-BoardStatus $r $Task '⏸ Tạm dừng · có thể test nghiệm thu sai (TN)'
        return
    }
    # Bảng việc theo kết quả cổng KT (dòng này từng bị mất ⇒ task xong vẫn hiện 🟡 trên bảng / Văn phòng — R-005). Review AI bên dưới có thể ghi đè.
    $boardText = $(if ($v.Ok) { "🔵 Chờ review · cổng KT ĐẠT" } else { "🔴 Cần sửa · cổng KT KHÔNG ĐẠT" })
    if ($v.Ok -and $rv) {
        switch ($rv.Code) {
            0 { $boardText = "🔵 Chờ review · cổng KT ĐẠT · AI review PASS" }
            1 {
                if (Stop-ReviewReassignment $r (Join-Path $work "tasks/$($taskFile.Name)") $Task $cfg) { return }
                if (-not $script:ReviewRetried -and $rv.Json -and $rv.Json.feedback) {
                    $script:ReviewRetried = $true; $script:VerifyRetried = $false
                    Say "AI review CẦN SỬA → tự giao lại cho $(Get-StaffName $agentId) 1 lần kèm danh sách lỗi của reviewer." Yellow
                    Set-BoardStatus $r $Task '🔴 Cần sửa · tự giao lại' -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = 'KHÔNG ĐẠT' }
                    $script:Resume = $true
                    $script:Feedback = "Reviewer AI (provider khác) yêu cầu sửa. Sửa đúng các mục Blocker / Required dưới đây, không làm thêm; chạy lại build + test, cập nhật báo cáo, commit và push:`n" + $rv.Json.feedback
                    Invoke-Assign
                    return
                }
                $boardText = "🔵 Chờ review · cổng KT ĐẠT · AI review CẦN SỬA (Claude xem)"
            }
            3 { $boardText = "🔵 Chờ review · cổng KT ĐẠT · AI review BLOCKED (Claude review)" }
        }
    }
    $assignResult = if ($v.Ok) { 'ĐẠT' } else { 'KHÔNG ĐẠT' }
    Set-BoardStatus $r $Task $(if ($boardText) { $boardText } elseif ($v.Ok) { "🔵 Chờ review · cổng KT ĐẠT" } else { "🔴 Cần sửa · cổng KT KHÔNG ĐẠT" }) -Journal @{ TaskFile = $taskFile.Name; Event = 'Assign'; Result = $assignResult }
    } finally {
        if ($lockHeld) {
            $null = Invoke-ScopeLock -Action release -TaskId $Task -ProjectPath $r.Path
            $script:ScopeLockOwn.Remove($lockTaskKey)
        }
    }
}

# R-004: task khác đang chạy (🟡 trên bảng việc) có Phạm vi trùng ⇒ dễ xung đột merge. Bỏ qua thư mục test chung (tests/, core.tests/…).
function Get-ScopeOverlaps($r, $taskFile, [string]$text) {
    $notTests = { $_ -notmatch '(^|/)[\w.-]*tests?/?$' }
    $mine = @(Get-BacktickPaths (Get-Section $text "Phạm vi") | Where-Object $notTests)
    $board = Join-Path $r.Path "tasks\README.md"
    if (-not $mine -or -not (Test-Path $board)) { return @() }
    $out = @()
    foreach ($m in [regex]::Matches((Get-Content $board -Raw -Encoding UTF8), '\]\(([^)]+\.md)\)[^\r\n]*🟡')) {
        $f = Join-Path (Join-Path $r.Path "tasks") $m.Groups[1].Value
        if (-not (Test-Path $f) -or (Resolve-Path $f).Path -eq $taskFile.FullName) { continue }
        $other = @(Get-BacktickPaths (Get-Section (Get-Content $f -Raw -Encoding UTF8) "Phạm vi") | Where-Object $notTests)
        $hit = @($mine | Where-Object { $p = $_; @($other | Where-Object { $_.StartsWith($p) -or $p.StartsWith($_) }).Count -gt 0 })
        if ($hit) { $out += "$([IO.Path]::GetFileNameWithoutExtension($f)): $($hit -join ', ')" }
    }
    $out
}

# B6 (R-004): sau lượt agent, origin/main đổi bởi chính agent (vượt hook pre-push bằng --no-verify…) ⇒ báo động.
function Test-AgentTouchedMain($r, [string]$work, [string]$mainBefore) {
    if (-not $mainBefore) { return }
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $r.Path fetch -q origin 2>$null
    $after = (git -C $r.Path rev-parse origin/main 2>$null)
    $who = (git -C $work config user.name 2>$null)
    if ($after -and $after -ne $mainBefore -and $who) {
        $bad = @(git -C $r.Path log --format="%h|%an|%s" "$mainBefore..$after" 2>$null | Where-Object { ($_ -split '\|')[1] -eq $who })
        if ($bad) { Say "BÁO ĐỘNG: agent '$who' đã đẩy $($bad.Count) commit thẳng lên main: $($bad -join '; ') — kiểm tra / revert ngay." Red }
    }
    $ErrorActionPreference = $eap
}

# V5-02 (R-005): đặt <worktree>\.agents\mcp_config.json để agy gọi được MCP Gateway của AITeam (aiteam mcp) — gắn ĐÚNG task này.
# Thư mục .agents/ được thêm vào info/exclude (không lên git, không làm cổng KT báo "chưa commit"). Trả $true nếu đặt được.
function Set-McpConfig($r, [string]$work, $taskFile, [string]$agentId) {
    $exe = Get-AITeamExe
    if (-not $exe) { Say "Không build được aiteam.exe ⇒ agent chạy không có MCP (không báo tiến độ lên Văn phòng)." Yellow; return $false }
    # Máy chủ MCP chạy suốt phiên agent ⇒ nếu chạy thẳng từ bin\Release thì KHÓA file, không build lại aiteam được
    # (25/10: QC V8-01 kẹt vì MCP của H7-01). Chép bin ra bản riêng theo thời điểm build, MCP chạy từ bản chép.
    try {
        $binDir = Split-Path $exe
        $stampBin = (Get-Item $exe).LastWriteTimeUtc.ToString("yyyyMMddHHmmss")
        $shadow = Join-Path $Root ".state\aiteam-mcp\$stampBin"
        if (-not (Test-Path (Join-Path $shadow "aiteam.exe"))) {
            New-Item -ItemType Directory -Force $shadow | Out-Null
            Copy-Item (Join-Path $binDir "*") $shadow -Recurse -Force -ErrorAction Stop
        }
        $exe = Join-Path $shadow "aiteam.exe"
    } catch { Say "Không chép được aiteam cho MCP ($($_.Exception.Message)) ⇒ dùng bản gốc." Yellow }
    $taskInWork = Join-Path (Join-Path $work "tasks") $taskFile.Name
    $cfg = [ordered]@{ mcpServers = [ordered]@{ aiteam = [ordered]@{
        command = $exe
        args = @("mcp", "--project", $r.Info.name, "--task", (Get-TaskId $taskFile), "--role", "developer",
                 "--task-file", $taskInWork, "--logs", $r.Logs, "--from", (Get-StaffName $agentId), "--root", $Root)
        timeoutSeconds = 30 } } }
    $dir = Join-Path $work ".agents"; New-Item -ItemType Directory -Force $dir | Out-Null
    [IO.File]::WriteAllText((Join-Path $dir "mcp_config.json"), ($cfg | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding $false))
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    $common = git -C $work rev-parse --git-common-dir 2>$null
    $ErrorActionPreference = $eap
    if ($common) {
        if (-not [IO.Path]::IsPathRooted($common)) { $common = Join-Path $work $common }
        $ex = Join-Path $common "info\exclude"; New-Item -ItemType Directory -Force (Split-Path $ex) | Out-Null
        if (-not (Test-Path $ex) -or -not (Select-String -Path $ex -Pattern '^/?\.agents/$' -Quiet)) { Add-Content $ex "/.agents/" -Encoding ASCII }
    }
    return $true
}

# aiteam.exe (lõi C#) bản Release, tự build lại khi mã nguồn core mới hơn.
function Get-AITeamExe {
    $proj = Join-Path $Root "core\AITeam.Cli\AITeam.Cli.csproj"
    $exe = Join-Path $Root "core\AITeam.Cli\bin\Release\net10.0-windows\aiteam.exe"
    $newer = $true
    if (Test-Path $exe) {
        $t0 = (Get-Item $exe).LastWriteTimeUtc
        $newer = [bool](Get-ChildItem (Join-Path $Root "core") -Recurse -File -Include *.cs, *.csproj |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.LastWriteTimeUtc -gt $t0 } | Select-Object -First 1)
    }
    if ($newer) { $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"; dotnet build $proj -c Release -v q -nologo 2>&1 | Out-Null; $ErrorActionPreference = $eap }
    if (Test-Path $exe) { $exe } else { $null }
}

# Thử lại trên cùng worktree; hạn mức thật cần chờ người quản lý xử lý.
function Invoke-CodexRetry($exe, [string[]]$arguments, [string]$out) {
    $fallback = @((Get-Config).codex.modelFallback | Where-Object { $_ } | Select-Object -First 1)
    $current = $arguments
    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out }
        try {
            $lines = @(& $exe @current 2>&1)
            $code = $LASTEXITCODE
        } finally {
            # Chạy cả khi Codex lỗi, bị hủy hoặc -NoVerify; không che kết quả lượt gọi.
            $savedExit = $LASTEXITCODE
            try { dotnet build-server shutdown 2>&1 | Out-Null }
            catch { Say "Không dừng được build server: $($_.Exception.Message)" Yellow }
            $global:LASTEXITCODE = $savedExit
        }
        $lines | ForEach-Object { Say "  $_" }
        $text = ($lines -join "`n")
        if (Test-Path -LiteralPath $out) { $text += "`n" + [IO.File]::ReadAllText($out) }
        $quota = $text -match '(?i)usage.?limit|\bquota\b|insufficient_quota|credits? balance|out of credits'
        $overload = $text -match '(?i)at capacity|overloaded|\b429\b'
        if ($code -eq 0 -or $quota -or -not $overload -or $attempt -eq 2) { break }
        if ($attempt -eq 0) {
            Say 'Codex quá tải ⇒ thử lại sau 60 giây…' Yellow
            Start-Sleep -Seconds 60
        } elseif ($fallback.Count) {
            Say "Codex quá tải ⇒ thử lại với model $($fallback[0])…" Yellow
            $current = @($arguments) + @('--model', [string]$fallback[0])
        } else { break }
    }
    $global:LASTEXITCODE = $code
}

# R-004: Codex viết code trong worktree qua `aiteam run`. Codex KHÔNG ghi được .git (sandbox — thử thật 24/10) ⇒ commit + push ở đây.
# Ghi $logFile cùng định dạng log agy (status / usage / duration_seconds) để số đo, Hub, báo cáo dùng lại được.
function Invoke-CodexDevelop($r, [string]$TaskName, [string]$work, [string]$branch, [string]$prompt, [string]$logFile, [string]$maxMin, [string]$runtimeProvider = 'codex', [string]$runtimeModel = '', [bool]$publicRepository = $false) {
    $exe = Get-AITeamExe
    if (-not $exe) { return [pscustomobject]@{ Ok = $false; Status = "không build được aiteam.exe" } }
    $codexPromptPrefix = "CHẠY BẰNG CODEX (dự phòng Antigravity): KHÔNG chạy lệnh git (sandbox chặn ghi .git) — hệ thống tự commit + push sau khi bạn xong; bỏ qua mọi yêu cầu commit / push bên dưới. Vẫn phải điền mục 'Báo cáo của agent' trong file task.`n`n"
    if ($runtimeProvider -eq 'opencode') { $codexPromptPrefix = "CHẠY BẰNG OPENCODE DEEPSEEK: chỉ làm trong phạm vi task và quyền đã cấp; không dùng model khác.`n`n" }
    $pf = $logFile -replace '\.json$', '.codex.prompt.md'
    [IO.File]::WriteAllText($pf, $codexPromptPrefix + $prompt, (New-Object Text.UTF8Encoding $false))
    $out = $logFile -replace '\.json$', '.codex.json'
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    Say "[$(Get-Date -Format HH:mm:ss)] $TaskName → Codex (aiteam run), thư mục $work" Cyan
    $runArgs = @("run", "--provider", $runtimeProvider, "--workdir", $work, "--prompt-file", $pf, "--logdir", (Join-Path $r.Logs "$TaskName.$runtimeProvider"), "--timeout-min", $maxMin, "--out", $out, "--root", $Root, "--title", $TaskName)
    if ($runtimeModel) { $runArgs += @('--model', $runtimeModel) }
    if ($runtimeProvider -eq 'opencode' -and $publicRepository) { $runArgs += '--public-repo' }
    if ($runtimeProvider -eq 'codex') { Invoke-CodexRetry $exe $runArgs $out } else { & $exe @runArgs }
    $code = $LASTEXITCODE; $global:LASTEXITCODE = 0   # mã của aiteam (4 = bỏ qua…) không rò thành mã thoát của assign
    $j = $null; if (Test-Path $out) { try { $j = Read-Json $out } catch { } }
    $tokensKnown = $j -and $null -ne $j.inputTokens -and $null -ne $j.outputTokens -and $j.inputTokens -ge 0 -and $j.outputTokens -ge 0 -and $j.tokensKnown -ne $false
    $in = if ($tokensKnown) { [long]$j.inputTokens } else { $null }
    $outTok = if ($tokensKnown) { [long]$j.outputTokens } else { $null }
    $agyLike = [ordered]@{
        status = $(if ($code -eq 0) { "SUCCESS" } else { "ERROR" }); provider = $runtimeProvider
        response = $(if ($j) { $j.text } else { "" }); error = $(if ($code -ne 0 -and $j) { $j.summary } else { $null })
        duration_seconds = $(if ($j) { [int]$j.durationSec } else { 0 })
        tokensKnown = [bool]$tokensKnown
        usage = $(if ($tokensKnown) { [ordered]@{ input_tokens = $in; output_tokens = $outTok; thinking_tokens = $j.thinkingTokens; total_tokens = $in + $outTok } } else { $null })
        conversation_id = ""
    }
    if ($runtimeProvider -eq 'opencode') {
        $known = $j -and $null -ne $j.inputTokens -and $j.inputTokens -ge 0 -and $null -ne $j.outputTokens -and $j.outputTokens -ge 0 -and $null -ne $j.totalTokens
        $agyLike.tokensKnown = [bool]$known
        $agyLike.usage = $(if ($known) { [ordered]@{ input_tokens = $j.inputTokens; output_tokens = $j.outputTokens; thinking_tokens = $null; total_tokens = $j.totalTokens } } else { $null })
        $agyLike.error = if ($j) { $j.detail } else { 'Chưa nhận kết quả OpenCode.' }
    }
    [IO.File]::WriteAllText($logFile, ($agyLike | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding $false))
    if ($code -eq 0) {
        if (@(git -C $work status --porcelain 2>$null | Where-Object { $_ }).Count -gt 0) {
            git -C $work add -A 2>$null
            git -C $work commit -q -m "${TaskName}: $runtimeProvider viết code" 2>&1 | Select-Object -Last 3 | ForEach-Object { Say "  $_" Yellow }
        }
        git -C $work push -q -u origin $branch 2>$null
    }
    $ErrorActionPreference = $eap
    Say "[$(Get-Date -Format HH:mm:ss)] $TaskName Codex kết thúc: $(if ($j) { $j.summary } else { "mã $code" })" Cyan
    [pscustomobject]@{ Ok = ($code -eq 0); Status = $(if ($j) { $j.status } else { "mã $code" }); Code = $code }
}

# Review AI cho task vừa qua cổng KT: aiteam review --auto (tự quyết có review không), reviewer ≠ agy (cấm tự duyệt).
# Trả $null nếu bỏ qua / không chạy được; ngược lại { Code (0 PASS · 1 CẦN SỬA · 3 BLOCKED · 4 bỏ qua), Json, Out }.
function Invoke-AutoReview($r, [string]$taskFileName, [string]$work, [string]$devProvider = "agy") {
    $assignment = Resolve-WorkflowAssignment 'qc'
    if ($NoReview) { Say "Bỏ qua review AI (-NoReview)."; return $null }
    $exe = Get-AITeamExe
    if (-not $exe) { Say "Không build được aiteam.exe ⇒ bỏ qua review AI." Yellow; return $null }
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    # Tên "<Task>.review-n.json" (dấu chấm): KHÔNG khớp mẫu log lượt chạy "<Task>-n.json" của assign -Resume và Hub.
    $dir = Join-Path $r.Logs "$Task.review"; New-Item -ItemType Directory -Force $dir | Out-Null
    $n = @(Get-ChildItem $r.Logs -Filter "$Task.review-*.json" -ErrorAction SilentlyContinue).Count + 1
    $diff = Join-Path $dir "diff-$n.txt"
    git -C $work fetch -q origin 2>$null
    $base = (git -C $work merge-base origin/main HEAD 2>$null | Select-Object -First 1)
    # Ghi diff bằng Start-Process (PowerShell 5.1 ">" ghi UTF-16 làm hỏng tiếng Việt).
    Start-Process git -ArgumentList "-C `"$work`" diff $base HEAD" -NoNewWindow -Wait -RedirectStandardOutput $diff -RedirectStandardError "$diff.err" | Out-Null
    $verify = Get-ChildItem $r.Logs -Filter "$Task-verify-*.md" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    $taskInWork = Join-Path (Join-Path $work "tasks") $taskFileName
    $out = Join-Path $r.Logs "$Task.review-$n.json"
    $a = @("review", "--auto", "--task-file", $taskInWork, "--diff-file", $diff, "--workdir", $work, "--logdir", $dir, "--dev", $devProvider, "--out", $out, "--root", $Root)
    if ($verify) { $a += @("--verify-file", $verify.FullName) }
    $qcStartHead=git -C $work rev-parse HEAD
    if ($script:AcceptVerify -and $script:AcceptVerify.task -ceq $Task -and $script:AcceptVerify.head -ceq $qcStartHead) {
        $a=@($a | Select-Object -First ($a.Count - $(if ($verify) {2} else {0})))
        $a+=@('--verify-file',(Join-Path $r.Logs $script:AcceptVerify.verify.source))
    }
    Say "Review AI (aiteam review --auto)…" Cyan
    Invoke-WorkflowRoleRun $assignment $exe $a 2>&1 | ForEach-Object { Say "  $_" }
    $code = $LASTEXITCODE; $global:LASTEXITCODE = 0   # mã của aiteam (4 = bỏ qua…) không rò thành mã thoát của assign
    $ErrorActionPreference = $eap
    $json = $null; if (Test-Path $out) { try { $json = Read-Json $out } catch { } }
    if ($code -eq 4) { return $null }
    # Fail-safe: chỉ tin kết luận khi có file --out đúng định dạng và khớp mã thoát (aiteam cũ không có lệnh review ⇒ in hướng dẫn, mã 0).
    $expect = @{ 0 = "PASS"; 1 = "CHANGES_REQUESTED"; 3 = "BLOCKED" }
    if (-not $json -or -not $expect.ContainsKey([int]$code) -or $json.verdict -ne $expect[[int]$code]) {
        Say "Review AI không ra kết luận hợp lệ (mã $code, file $out) ⇒ coi như BLOCKED, Claude review." Yellow
        return [pscustomobject]@{ Code = 3; Json = $json; Out = $out }
    }
    $qcHead=git -C $work rev-parse HEAD
    if ($script:AcceptVerify -and $script:AcceptVerify.task -ceq $Task -and $script:AcceptVerify.head -ceq $qcHead -and $qcStartHead -ceq $qcHead) {
        $findings=@(foreach ($finding in @($json.findings)) {
            if ($finding) { @{id=$finding.id;severity=$finding.severity;open=$finding.open;classification=$finding.classification} }
        })
        Invoke-RepoTransaction -RepoPath $r.Path -Action {
            $script:AcceptVerify.qc=@{verdict=$json.verdict;findings=$findings;source=[IO.Path]::GetFileName($out)}
            $index=Join-Path $r.Logs "$Task.accept-$($script:AcceptVerify.run).json"
            for ($node=$index; $node; $node=[IO.Path]::GetDirectoryName($node)) {
                if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Nguồn không an toàn.' }
            }
            $temp="$index.$([guid]::NewGuid().ToString('N')).tmp"
            [IO.File]::WriteAllText($temp,($script:AcceptVerify | ConvertTo-Json -Depth 16),$Utf8)
            Move-Item -LiteralPath $temp -Destination $index -Force
        }
    }
    [pscustomobject]@{ Code = $code; Json = $json; Out = $out }
}

# Agent hết hạn mức? Lần chạy báo SUCCESS thì không. Còn lại dò từ khóa, "429"/"quota" phải đứng riêng
# (trước đây "429" khớp nhầm vào số token như "thinking_tokens":42953).
function Get-AgentErrorText([string]$logText, [string]$errText) {
    try {
        $entry = ConvertFrom-Json -InputObject $logText -ErrorAction Stop
        return "$($entry.error)`n$errText"
    } catch { return "$logText`n$errText" }
}

function Test-NetworkError([string]$logText, [string]$errText) {
    if ($logText -match '"status"\s*:\s*"SUCCESS"') { return $false }
    return (Get-AgentErrorText $logText $errText) -match '(?i)no such host|dial tcp|connection (refused|reset)|could not be made|deadline exceeded|Client\.Timeout|network is unreachable|TLS handshake timeout|mất kết nối tới máy chủ AI|Reconnecting|os error 10061|actively refused'
}

function Test-QuotaExhausted([string]$logText, [string]$errText) {
    if ($logText -match '"status"\s*:\s*"SUCCESS"') { return $false }
    return (Get-AgentErrorText $logText $errText) -match '(?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits'
}

# Repo chính luôn commit bằng tên chủ dự án. Đặt lại khi chưa có hoặc bị agent ghi đè (tên "Agent …" hoặc tên nhân viên trong team.json).
# Trả về $true nếu đã phải sửa.
function Set-ManagerIdentity([string]$repoPath, [string]$owner) {
    $name = git -C $repoPath config --local user.name 2>$null
    $agentNames = @((Get-Config).developers | ForEach-Object { $_.name })
    $isAgent = $name -match '^Agent\b' -or $agentNames -contains $name
    if ($name -and -not $isAgent) { return $false }
    git -C $repoPath config --local user.name "HieuDV"
    git -C $repoPath config --local user.email "$owner@users.noreply.github.com"
    return [bool]$name
}

# Worktree của agent dùng chung .git/config với repo chính → đặt tên agent ở cấu hình riêng của worktree.
function Set-AgentIdentity([string]$repoPath, [string]$work, $dev, [string]$project) {
    git -C $repoPath config extensions.worktreeConfig true
    git -C $work config --worktree user.name $dev.name
    git -C $work config --worktree user.email ("agent-{0}@{1}.local" -f $dev.id.ToLower(), ($project.ToLower() -replace '[^a-z0-9]+', '-'))
}

function Invoke-Status {
    $cfg = Get-Config
    $running = @(Get-CimInstance Win32_Process -Filter "Name = 'agy.exe'" -ErrorAction SilentlyContinue)
    Say "Agent đang chạy: $($running.Count)" Cyan
    foreach ($p in $running) { Say ("  PID {0}  từ {1}  {2}" -f $p.ProcessId, $p.CreationDate, ($p.CommandLine -replace '.*Doc file (\S+).*', '$1')) }
    foreach ($p in Get-Projects | Where-Object { -not $Project -or $_.name -eq $Project }) {
        $r = Resolve-Project $p.name
        Say "== $($p.name) ==" Cyan
        if (Test-Path (Join-Path $r.Path ".git")) {
            git -C $r.Path worktree list | ForEach-Object { Say "  $_" }
            git -C $r.Path fetch origin --quiet 2>$null
            $board = git -C $r.Path show origin/main:tasks/README.md 2>$null | Out-String
            foreach ($row in ($board -split "`r?`n" | Where-Object { $_ -match '^\|' -and $_ -match '🟡' })) {
                $id = [regex]::Match($row, '\[([A-Za-z]+-\d+)').Groups[1].Value
                $br = [regex]::Match($row, '`([^`]+)`').Groups[1].Value
                $hasBranch = [bool]($br -and (git -C $r.Path ls-remote --heads origin $br 2>$null))
                $m = [regex]::Match($row, $ClaimPattern)
                if ($m.Success) {
                    $since = [datetime]::ParseExact($m.Groups['t'].Value, 'HH:mm dd/MM/yyyy', $null)
                    $mins = [int]((Get-Date) - $since).TotalMinutes
                    if ($mins -gt 120 -and -not $hasBranch) { Say "  [KẸT?] $id chạy trên máy $($m.Groups['m'].Value.Trim()) đã $mins phút mà chưa có nhánh $br trên GitHub → kiểm tra máy đó / giao lại với -Force" Yellow }
                    else { Say "  [ĐANG CHẠY] $id · máy $($m.Groups['m'].Value.Trim()) · $mins phút · nhánh $(if ($hasBranch) { 'đã có' } else { 'chưa có' })" }
                } elseif (-not $hasBranch) {
                    Say "  [KẸT?] $id ghi 🟡 nhưng chưa có nhánh $br trên GitHub, không rõ máy nào đang chạy" Yellow
                }
            }
        }
        if (Test-Path $r.Logs) {
            Get-ChildItem $r.Logs -Filter "*.json" | Where-Object { $_.Name -notmatch 'prompt' } | Sort-Object LastWriteTime | Select-Object -Last 6 | ForEach-Object {
                $content = "$(Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue)"
                $s = [regex]::Match($content, '"status":"([^"]+)"').Groups[1].Value
                Say ("  {0}  {1:dd/MM HH:mm}  {2}" -f $_.Name, $_.LastWriteTime, $(if ($s) { $s } else { "đang chạy / không có kết quả" }))
            }
        }
    }
}

function Invoke-Cleanup {
    $r = Resolve-Project $Project
    $dirs = Get-ChildItem $r.Work -Directory -Filter "*-$($Task.ToLower())" -ErrorAction SilentlyContinue
    $ErrorActionPreference = "Continue"   # git in ra stderr; PowerShell 5.1 + Stop coi là lỗi
    foreach ($d in $dirs) {
        git -C $r.Path worktree remove $d.FullName --force 2>$null
        git -C $r.Path worktree prune 2>$null
        # Windows có thể còn khóa thư mục vài giây sau khi tắt app đang chạy từ đó → thử xóa lại phần còn sót.
        foreach ($try in 1..5) {
            if (-not (Test-Path $d.FullName)) { break }
            Remove-Item $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
            if (Test-Path $d.FullName) { Start-Sleep -Seconds 2 }
        }
        if (Test-Path $d.FullName) { Say "Chưa xóa được $($d.FullName) (đang bị khóa) — xóa tay sau." Yellow }
        else { Say "Đã xóa worktree $($d.FullName)" }
    }
}

function Clear-AgyModelCache {
    $cacheState = Join-Path $Root '.state'
    $cacheFile = Join-Path $cacheState 'agy-models.json'
    foreach ($cachePath in @($cacheState, $cacheFile)) {
        if ((Test-Path -LiteralPath $cachePath) -and (([IO.File]::GetAttributes($cachePath) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "Không xóa cache model qua symlink/reparse point: $cachePath"
        }
    }
    if (Test-Path -LiteralPath $cacheFile) { Remove-Item -LiteralPath $cacheFile -Force }
}

function Invoke-SwitchAccount {
    Clear-AgyModelCache
    $cfg = Get-Config
    Say "Mở Antigravity CLI trong cửa sổ mới:" Cyan
    Say "  1. Gõ /logout rồi Enter"
    Say "  2. Thoát (Ctrl+C), chạy lại agy → trình duyệt mở → đăng nhập tài khoản khác ($($cfg.agy.accounts -join ' / '))"
    Say "  3. Gõ /quit"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "& '$($cfg.agy.exe)'"
}

# ───────────────────────── Việc chỉ đọc, model ─────────────────────────

function Get-EmptyInput {
    # agy treo vĩnh viễn nếu stdin là pipe đang mở → luôn đưa stdin là một file rỗng.
    $f = Join-Path $env:TEMP "aiteam-empty-stdin.txt"
    if (-not (Test-Path $f)) { [IO.File]::WriteAllText($f, "") }
    return $f
}

function Invoke-Ask {
    $cfg = Get-Config
    $r = Resolve-Project $Project
    if (-not $Prompt) { throw "Thiếu -Prompt" }
    New-Item -ItemType Directory -Force $r.Logs | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $promptFile = Join-Path $r.Logs "ask-$stamp.prompt.md"
    $full = @"
VIỆC CHỈ ĐỌC trong dự án $($r.Info.name) ($($r.Path)). TUYỆT ĐỐI KHÔNG sửa, tạo hay xóa file nào; không chạy lệnh làm thay đổi hệ thống.
Trả lời bằng tiếng Việt, kết thúc bằng báo cáo markdown: Phát hiện / Mức độ / File:dòng / Đề xuất.

$Prompt
"@
    [IO.File]::WriteAllText($promptFile, $full, $Utf8)
    $log = Join-Path $r.Logs "ask-$stamp.json"
    $argLine = "-p `"Doc file $promptFile (UTF-8) va lam dung theo do.`" --output-format json --print-timeout 0 --sandbox --dangerously-skip-permissions --add-dir `"$($r.Logs)`""
    $m = if ($Model) { $Model } else { $cfg.agy.model }
    if ($m) { $argLine += " --model `"$m`"" }
    $before = git -C $r.Path status --porcelain
    Start-Process $cfg.agy.exe -ArgumentList $argLine -WorkingDirectory $r.Path -NoNewWindow -Wait `
        -RedirectStandardInput (Get-EmptyInput) -RedirectStandardOutput $log -RedirectStandardError "$log.err" | Out-Null
    $after = git -C $r.Path status --porcelain
    if ("$before" -ne "$after") { Say "CẢNH BÁO: việc chỉ đọc đã làm thay đổi file trong $($r.Path)! Kiểm tra git status." Red }
    $resp = [regex]::Match("$(Get-Content $log -Raw -Encoding UTF8)", '"response":"((?:[^"\\]|\\.)*)"').Groups[1].Value
    if ($resp) { ('"' + $resp + '"' | ConvertFrom-Json) } else { Get-Content $log, "$log.err" -ErrorAction SilentlyContinue }
    Say "Log: $log" DarkGray
}

function Invoke-Models {
    $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try {
        $output = @(& (Get-Config).agy.exe models 2>&1)
        $code = $LASTEXITCODE
        $output | Where-Object { "$_" -notmatch '^Fetching available models\.\.\.$' } | ForEach-Object { Say "$_" }
        if ($code -ne 0) { throw "Không lấy được danh sách model (mã $code)." }
    } finally { $ErrorActionPreference = $eap }
}

# ===== Quy trình v4 (R-006, chủ dự án chọn 25/10): Claude brief → Codex kế hoạch (CHỈ ĐỌC) → Claude duyệt →
# Codex chuẩn bị từng task (đặc tả + khung + test nghiệm thu) → Antigravity code → cổng KT + Codex QC → Claude kiểm cuối.
# /// <summary>Đọc khai báo **Giao diện:** Có/Không của từng task; thiếu/chưa rõ:
# /// "Chưa xác định loại task; cần bổ sung Giao diện: Có/Không.".
# /// Trả PSCustomObject { HasUi: bool; Html: string }; lõi Html rỗng.
# /// UI cần đúng một khối html trong đúng một mục ## Mockup, 1..120 dòng nội dung.
# /// Chỉ HTML/CSS tĩnh, CSS Hub hoặc nội tuyến; chỉ href="#" và liên kết CSS Hub.
# /// Cấm mã chạy, handler on*, URL ngoài, CSS url/@import, iframe/object/embed/form,
# /// base/meta refresh, SVG/MathML; lỗi: "Mockup không hợp lệ.". Không ghi/chạy file.</summary>
function Read-PlanMockup([string]$PlanText) {
    $invalid = 'Mockup không hợp lệ.'
    $unknown = 'Chưa xác định loại task; cần bổ sung Giao diện: Có/Không.'
    $plain = New-Object 'Collections.Generic.List[string]'
    $blocks = New-Object 'Collections.Generic.List[object]'
    $content = New-Object 'Collections.Generic.List[string]'
    $fence = ''; $section = ''; $isHtml = $false; $mockups = 0
    foreach ($line in ($PlanText -split '\r\n|\n|\r')) {
        if ($fence) {
            if ($line -match ('^ {0,3}' + [regex]::Escape($fence[0]) + '{' + $fence.Length + ',}\s*$')) {
                if ($isHtml) { $blocks.Add(@{ Section = $section; Lines = @($content.ToArray()) }) }
                $fence = ''; $content.Clear()
            } else { $content.Add($line) }
            continue
        }
        if ($line -match '^ {0,3}(`{3,}|~{3,})([^\r\n]*)$') {
            $fence = $Matches[1]; $isHtml = $Matches[2].Trim() -ieq 'html'
            continue
        }
        $plain.Add($line)
        if ($line -match '^##[ \t]+(.+?)\s*$') {
            $section = $Matches[1]
            if ($section -ceq 'Mockup') { $mockups++ }
        }
    }
    if ($fence) { throw $invalid }
    $text = [regex]::Replace(($plain -join "`n"), '(?s)<!--.*?(?:-->|\z)', '')
    $tasks = [regex]::Matches($text, '(?ms)^###[ \t]+[A-Za-z0-9]+-\d+[a-z]?[ \t]*[·:-][^\n]*\n(?<body>.*?)(?=^#{2,3}[ \t]|\z)')
    if ($tasks.Count -eq 0) { throw $unknown }
    $hasUi = $false
    foreach ($taskSection in $tasks) {
        $types = [regex]::Matches($taskSection.Groups['body'].Value, '(?m)^-[ \t]+\*\*Giao diện:\*\*[ \t]*(?<type>[^\n]*)$')
        if ($types.Count -ne 1 -or $types[0].Groups['type'].Value.Trim() -cnotmatch '^(Có|Không)$') { throw $unknown }
        if ($types[0].Groups['type'].Value.Trim() -ceq 'Có') { $hasUi = $true }
    }
    # Có Mockup thì vẫn kiểm UI để không lách guard bằng khai báo Không.
    if (-not $hasUi -and $mockups -eq 0) { return [pscustomobject]@{ HasUi = $false; Html = '' } }
    if ($mockups -ne 1 -or $blocks.Count -ne 1 -or $blocks[0].Section -cne 'Mockup') { throw $invalid }
    $lines = $blocks[0].Lines
    if ($lines.Count -lt 1 -or $lines.Count -gt 120) { throw $invalid }
    $html = $lines -join "`r`n"
    if ([string]::IsNullOrWhiteSpace($html)) { throw $invalid }
    # Whitelist cú pháp nhỏ, không làm sạch rồi xuất lại nội dung nguy hiểm.
    $checkCss = {
        param([string]$css)
        $css = [Net.WebUtility]::HtmlDecode($css)
        $css = [regex]::Replace($css, '(?s)/\*.*?\*/', '')
        if ($css -match '[\\@\x00-\x08\x0b\x0c\x0e-\x1f<>]' -or $css -match '(?i)(url|expression)\s*\(|(?:javascript|data|https?|file)\s*:|//') { throw $invalid }
    }
    # Comment sai cú pháp có thể kết thúc sớm trong trình duyệt và lộ mã chủ động.
    foreach ($comment in [regex]::Matches($html, '(?s)<!--(?<body>.*?)-->')) {
        if ($comment.Groups['body'].Value -match '[<>]|--') { throw $invalid }
    }
    $scan = [regex]::Replace($html, '(?s)<!--.*?-->', '')
    foreach ($style in [regex]::Matches($scan, '(?is)<style\s*>(?<css>.*?)</style\s*>')) { & $checkCss $style.Groups['css'].Value }
    $scan = [regex]::Replace($scan, '(?is)<style\s*>.*?</style\s*>', '<style></style>')
    $tagPattern = '(?is)<!doctype\s+html\s*>|</?[a-z][a-z0-9]*(?:\s+[^<>]*?)?\s*/?>'
    if ([regex]::Replace($scan, $tagPattern, '') -match '[<>]') { throw $invalid }
    foreach ($token in [regex]::Matches($scan, $tagPattern)) {
        if ($token.Value -match '(?i)^<!doctype\s+html\s*>$') { continue }
        $tag = [regex]::Match($token.Value, '(?is)^<(?<close>/)?(?<name>[a-z][a-z0-9]*)(?<attrs>.*?)\s*/?>$')
        $name = $tag.Groups['name'].Value.ToLowerInvariant()
        if ($name -notmatch '^(html|head|body|title|meta|link|style|main|nav|header|footer|section|article|aside|div|span|p|a|b|strong|em|i|small|h[1-6]|ol|ul|li|br|hr|table|thead|tbody|tfoot|tr|th|td|caption|button|label|pre|code|dl|dt|dd)$') { throw $invalid }
        $attrs = $tag.Groups['attrs'].Value
        if ($tag.Groups['close'].Success) {
            if ($attrs.Trim()) { throw $invalid }
            continue
        }
        $attrPattern = '\s+(?<name>[a-zA-Z][a-zA-Z0-9-]*)\s*=\s*(?:"(?<value>[^"<>]*)"|''(?<value>[^''<>]*)'')'
        if ([regex]::Replace($attrs, $attrPattern, '').Trim()) { throw $invalid }
        $seen = @{}
        foreach ($attr in [regex]::Matches($attrs, $attrPattern)) {
            $key = $attr.Groups['name'].Value.ToLowerInvariant()
            $value = [Net.WebUtility]::HtmlDecode($attr.Groups['value'].Value)
            if ($seen.ContainsKey($key)) { throw $invalid }; $seen[$key] = $value
            if ($key -notmatch '^(class|id|title|lang|style|role|aria-(current|label|hidden|selected)|charset|rel|href|type|colspan|rowspan)$') { throw $invalid }
            if ($value -match '[\x00-\x1f\x7f]' -or $value -match '(?i)(javascript|data)\s*:') { throw $invalid }
            switch ($key) {
                'style' { & $checkCss $value }
                'href' {
                    if (-not (($name -eq 'a' -and $value -ceq '#') -or ($name -eq 'link' -and $value -ceq '../../hub/wwwroot/css/hub.css'))) { throw $invalid }
                    if ($name -eq 'link' -and $attr.Groups['value'].Value -cne $value) { throw $invalid }
                }
                'rel' { if ($name -ne 'link' -or $value -cne 'stylesheet') { throw $invalid } }
                'charset' { if ($name -ne 'meta' -or $value -ine 'utf-8') { throw $invalid } }
                'type' { if ($name -ne 'button' -or $value -cne 'button') { throw $invalid } }
            }
        }
        if ($name -eq 'link' -and ($seen['rel'] -cne 'stylesheet' -or $seen['href'] -cne '../../hub/wwwroot/css/hub.css')) { throw $invalid }
        if ($name -eq 'meta' -and ($seen.Count -ne 1 -or $seen['charset'] -ine 'utf-8')) { throw $invalid }
    }
    return [pscustomobject]@{ HasUi = $true; Html = $html }
}

# /// <summary>Kiểm Request ^R-\d{3}$; sai: "Đường dẫn mockup không hợp lệ.".
# /// Gọi Read-PlanMockup, lõi trả null và không tạo file; UI ghi UTF-8 BOM CRLF
# /// docs/design/REQUEST-mockup.html dưới Root, thay href CSS Hub bằng URI file tuyệt đối
# /// của Root/hub/wwwroot/css/hub.css. Không thực thi/mở HTML. Chặn reparse point
# /// ở file và mọi cha trước ghi; lỗi đường dẫn như trên. Trả { Path; Fingerprint }
# /// với Fingerprint SHA-256 hex thường của toàn bộ byte file. Không tự ghi duyệt.</summary>
function Write-PlanMockup([string]$RequestId, [string]$PlanText) {
    $pathError = 'Đường dẫn mockup không hợp lệ.'
    if ($RequestId -cnotmatch '^R-[0-9]{3}$') { throw $pathError }
    $mockup = Read-PlanMockup $PlanText
    if (-not $mockup.HasUi) { return $null }
    try {
        $rootPath = [IO.Path]::GetFullPath($Root)
        $path = [IO.Path]::GetFullPath((Join-Path $rootPath "docs/design/$RequestId-mockup.html"))
        $cssPath = [IO.Path]::GetFullPath((Join-Path $rootPath 'hub/wwwroot/css/hub.css'))
        if (-not $path.StartsWith($rootPath.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw $pathError }
        foreach ($candidate in @($path, $cssPath)) {
            for ($current = $candidate; $current; $current = [IO.Path]::GetDirectoryName($current)) {
                try { $attributes = [IO.File]::GetAttributes($current) }
                catch [IO.FileNotFoundException] { continue }
                catch [IO.DirectoryNotFoundException] { continue }
                if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw $pathError }
            }
        }
        $uri = [Net.WebUtility]::HtmlEncode(([uri]$cssPath).AbsoluteUri)
        $html = [regex]::Replace($mockup.Html, '(?i)(\bhref\s*=\s*["''])\.\./\.\./hub/wwwroot/css/hub\.css(["''])', ('${1}' + $uri.Replace('$', '$$') + '${2}'))
        $encoding = New-Object Text.UTF8Encoding $true
        [byte[]]$bytes = $encoding.GetPreamble() + $encoding.GetBytes($html + "`r`n")
        $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))
        [IO.File]::WriteAllBytes($path, $bytes)
    } catch { throw $pathError }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    $filePath = [IO.Path]::Combine($rootPath, "docs/design/$RequestId-mockup.html")
    return [pscustomobject]@{ Path = $filePath; Fingerprint = $hash }
}

# /// <summary>Chỉ đọc, không fetch/worktree/provider/ghi file. Loại comment HTML và
# /// fenced code trước tìm đúng mục ## Quyết định của quản lý (cho phép hậu tố).
# /// Chỉ dòng '- ĐÃ DUYỆT.' trong mục đó hợp lệ; thiếu: "Kế hoạch chưa được duyệt.".
# /// Plan cũ không khai báo UI và không Mockup vẫn nhận duyệt hiện có.
# /// UI cần mockup hợp lệ, file hiện tại khớp HTML được xuất và dòng chính thức
# /// '- Mockup SHA256: &lt;64 hex&gt;' khớp byte hiện tại; thiếu/sai/đổi/reparse point:
# /// "Mockup chưa được duyệt hoặc đã thay đổi.". Không tin dấu duyệt trong comment/code.
# /// Lõi mới cần khai báo Không; chưa rõ báo lỗi loại task như Read-PlanMockup.</summary>
function Assert-PrepPlanApproval([string]$RequestId, [string]$PlanText) {
    # Bỏ ví dụ trong comment và hàng rào Markdown trước khi tìm quyết định.
    $clean = [regex]::Replace($PlanText, '(?s)<!--.*?(?:-->|\z)', '')
    $plain = New-Object 'Collections.Generic.List[string]'
    $fence = ''
    foreach ($line in ($clean -split '\r\n|\n|\r')) {
        if ($fence) {
            if ($line -match ('^ {0,3}' + [regex]::Escape($fence[0]) + '{' + $fence.Length + ',}\s*$')) { $fence = '' }
            continue
        }
        if ($line -match '^ {0,3}(`{3,}|~{3,})') { $fence = $Matches[1]; continue }
        $plain.Add($line)
    }
    $clean = $plain -join "`n"
    $decisions = [regex]::Matches($clean, '(?ms)^##[ \t]+Quyết định của quản lý(?:[ \t]+[^\n]*)?\n(?<body>.*?)(?=^##[ \t]|\z)')
    if ($decisions.Count -ne 1 -or $decisions[0].Groups['body'].Value -cnotmatch '(?m)^- ĐÃ DUYỆT\.[^\n]*$') { throw 'Kế hoạch chưa được duyệt.' }
    $decision = $decisions[0].Groups['body'].Value
    $hasType = $clean -match '(?m)^-[ \t]+\*\*Giao diện:\*\*'
    $hasMockup = $clean -match '(?m)^##[ \t]+Mockup[ \t]*$'
    if (-not $hasType -and -not $hasMockup) { return }
    $currentPlan = $PlanText
    if (-not $hasType) {
        # Plan UI cũ vẫn kiểm nội dung/hash, chỉ bổ sung phân loại trong bộ nhớ.
        $currentPlan = [regex]::Replace($PlanText, '(?m)^(### [^\r\n]+)\r?$', ('$1' + "`r`n- **Giao diện:** Có"))
    }
    try { $mockup = Read-PlanMockup $currentPlan }
    catch {
        if ($_.Exception.Message -eq 'Chưa xác định loại task; cần bổ sung Giao diện: Có/Không.') { throw }
        throw 'Mockup chưa được duyệt hoặc đã thay đổi.'
    }
    if (-not $mockup.HasUi) { return }
    try {
        if ($RequestId -cnotmatch '^R-[0-9]{3}$') { throw 'Sai mã yêu cầu.' }
        $hashes = [regex]::Matches($decision, '(?m)^- Mockup SHA256: ([a-fA-F0-9]{64})[ \t]*$')
        if ($hashes.Count -ne 1) { throw 'Thiếu hash.' }
        $rootPath = [IO.Path]::GetFullPath($Root)
        $path = [IO.Path]::GetFullPath((Join-Path $rootPath "docs/design/$RequestId-mockup.html"))
        $cssPath = [IO.Path]::GetFullPath((Join-Path $rootPath 'hub/wwwroot/css/hub.css'))
        if (-not $path.StartsWith($rootPath.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Sai đường dẫn.' }
        foreach ($candidate in @($path, $cssPath)) {
            for ($current = $candidate; $current; $current = [IO.Path]::GetDirectoryName($current)) {
                try { $attributes = [IO.File]::GetAttributes($current) }
                catch [IO.FileNotFoundException] { continue }
                catch [IO.DirectoryNotFoundException] { continue }
                if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse point.' }
            }
        }
        $uri = [Net.WebUtility]::HtmlEncode(([uri]$cssPath).AbsoluteUri)
        $html = [regex]::Replace($mockup.Html, '(?i)(\bhref\s*=\s*["''])\.\./\.\./hub/wwwroot/css/hub\.css(["''])', ('${1}' + $uri.Replace('$', '$$') + '${2}'))
        $encoding = New-Object Text.UTF8Encoding $true
        [byte[]]$expected = $encoding.GetPreamble() + $encoding.GetBytes($html + "`r`n")
        [byte[]]$actual = [IO.File]::ReadAllBytes($path)
        if ([Convert]::ToBase64String($actual) -cne [Convert]::ToBase64String($expected)) { throw 'Nội dung đã đổi.' }
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($actual)).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose() }
        if ($hash -ine $hashes[0].Groups[1].Value) { throw 'Hash đã đổi.' }
    } catch { throw 'Mockup chưa được duyệt hoặc đã thay đổi.' }
}

function Get-PlanDir([string]$req) {
    if ($req -notmatch '^R-\d{3}$') { throw "Thiếu / sai -Request (vd. R-006)" }
    Join-Path $Root "plans\$req"
}

# /// <summary>pipeline -Request R-xxx [-Project X]: nối Read-WorkflowPipelinePlan và
# /// Invoke-WorkflowPipeline với repo đã Resolve-Project (mặc định AITeam), bảng origin/main
# /// sau fetch thành công; lỗi fetch phải dừng, không dùng bảng local. Worker là powershell.exe
# /// riêng mỗi task; prep -NoLaunch rồi assign -Dev agy; khóa Codex chỉ giữ lúc prep/QC.
# /// Dùng Invoke-WorkflowPipelineTask để cô lập bộ đếm, tiếp tục prep dở theo Invoke-Prep.
# /// Mỗi task đúng một dòng "MÃ: trạng thái"; log dài ghi file riêng. Không merge.
# /// Lỗi chưa làm được catch ngoài cùng in "LỖI: chưa làm", mã 1.</summary>
function Invoke-Pipeline {
    $pipeRepo = Resolve-Project $(if ($Project) { $Project } else { "AITeam" })
    if ($Task) {
        # Worker (tiến trình riêng do lõi khởi chạy): bộ đếm agent / KT / QC nằm trong Invoke-WorkflowPipelineTask của riêng task này.
        [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
        $pipeHasFile = { @(Get-ChildItem (Join-Path $pipeRepo.Path "tasks") -Filter "$Task-*.md" -ErrorAction SilentlyContinue).Count -gt 0 }
        $script:Project = $pipeRepo.Info.name
        $pipeResult = Invoke-WorkflowPipelineTask -TaskId $Task -Prep {
            $script:NoLaunch = $true
            $prepResult = @(Invoke-Prep) | Select-Object -Last 1   # tiếp tục worktree prep dở
            @{ Ok = [bool]($prepResult -and $prepResult.Ok -and (& $pipeHasFile)) }   # prep lỗi chỉ return ⇒ xác minh bằng file task thật trên repo chính
        } -Agent {
            param($resumeRun, $feedbackText)
            $script:Resume = [bool]$resumeRun; $script:Feedback = [string]$feedbackText
            $script:Dev = "agy"; $script:NoVerify = $true   # không fallback Codex, không tự giao lại trong assign
            $agentResult = @(Invoke-Assign) | Select-Object -Last 1
            @{ Ok = [bool]($agentResult -and $agentResult.Ok); Quota = [bool]($agentResult -and $agentResult.Quota) }
        } -Verify {
            $v = @(Invoke-Verify) | Select-Object -Last 1
            @{ Ok = [bool]$v.Ok; Feedback = "Cổng kiểm tra tự động KHÔNG ĐẠT. Sửa hết các mục sau, chạy lại build + test, cập nhật báo cáo, commit và push:`n- " + (@($v.Fails) -join "`n- ") }
        } -Qc {
            $ctx = Get-TaskContext
            $rv = @(Invoke-AutoReview $ctx.R $ctx.TaskFile.Name $ctx.Work "agy") | Select-Object -Last 1
            $verdict = if ($rv) { @{ 0 = 'PASS'; 1 = 'CHANGES_REQUESTED'; 3 = 'BLOCKED' }[[int]$rv.Code] } else { 'BLOCKED' }
            $fb = if ($rv -and $rv.Json -and $rv.Json.feedback) { "Reviewer AI (provider khác) yêu cầu sửa. Sửa đúng các mục Blocker / Required dưới đây, không làm thêm; chạy lại build + test, cập nhật báo cáo, commit và push:`n" + $rv.Json.feedback } else { "" }
            @{ Verdict = $verdict; Feedback = $fb }
        }
        $pipeBoard = @{ PASS = "🔵 Chờ review · cổng KT ĐẠT · AI review PASS"; VERIFY_FAILED = "🔴 Cần sửa · cổng KT KHÔNG ĐẠT"
            QC_FAILED = "🔵 Chờ review · cổng KT ĐẠT · AI review CẦN SỬA (Claude xem)"; BLOCKED = "🔵 Chờ review · AI review BLOCKED (Claude review)"; ERROR = "🔴 Lỗi pipeline" }[$pipeResult.Status]
        if ($pipeBoard -and (& $pipeHasFile)) { Set-BoardStatus $pipeRepo $Task $pipeBoard }
        Say "$($Task): $($pipeResult.Status)"
        if ($pipeResult.Status -eq 'ERROR') { exit 1 }
        return
    }
    $pipePlan = Join-Path (Get-PlanDir $Request) "plan.md"
    $pipeLogs = $pipeRepo.Logs
    New-Item -ItemType Directory -Force $pipeLogs | Out-Null
    $rows = @(Invoke-WorkflowPipeline -PlanPath $pipePlan -ProjectKey $pipeRepo.Path -RequestId $Request -ReadBoard {
        # Luôn fetch rồi đọc bảng việc origin/main; lỗi thì dừng, không dùng bảng local.
        $global:LASTEXITCODE = 0
        git -C $pipeRepo.Path fetch -q origin
        if ($LASTEXITCODE -ne 0) { throw "Không fetch được origin ⇒ dừng pipeline (không dùng bảng việc local)." }
        $enc = [Console]::OutputEncoding
        try { [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false; $boardLines = @(git -C $pipeRepo.Path show "origin/main:tasks/README.md") }
        finally { [Console]::OutputEncoding = $enc }
        if ($LASTEXITCODE -ne 0) { throw "Không đọc được origin/main:tasks/README.md ⇒ dừng pipeline." }
        $boardLines -join "`n"
    } -TaskExists {
        param($id)
        @(Get-ChildItem (Join-Path $pipeRepo.Path "tasks") -Filter "$id-*.md" -ErrorAction SilentlyContinue).Count -gt 0
    } -StartTask {
        param($item)
        $base = Join-Path $pipeLogs "$($item.Id).pipeline"
        $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $Root 'team.ps1') + '"'), 'pipeline',
            '-Project', ('"' + $pipeRepo.Info.name + '"'), '-Request', $Request, '-Task', $item.Id)
        Start-Process powershell.exe -ArgumentList $argList -WindowStyle Hidden -PassThru -RedirectStandardOutput "$base.out.txt" -RedirectStandardError "$base.err.txt"
    })
    if ($rows.Count -eq 0) { Say "Pipeline $Request của $($pipeRepo.Info.name) đang chạy ở tiến trình khác ⇒ bỏ qua." Yellow; return }
    foreach ($row in $rows) {
        $status = $row.Status
        $outFile = Join-Path $pipeLogs "$($row.Id).pipeline.out.txt"
        if ($status -in @('DONE', 'ERROR') -and (Test-Path $outFile)) {
            # Worker in đúng một dòng "MÃ: trạng thái" ở cuối; log dài nằm trong file riêng.
            $line = Get-Content $outFile -Encoding UTF8 | Where-Object { $_ -match ('^' + [regex]::Escape($row.Id) + ': [A-Z_]+$') } | Select-Object -Last 1
            if ($line) { $status = ($line -split ': ', 2)[1] }
        }
        Say "$($row.Id): $status"
    }
}

function ConvertTo-AsciiSlug([string]$text) {
    $t = $text.ToLower().Replace([string][char]0x0111, "d").Normalize([Text.NormalizationForm]::FormD)
    $t = -join ($t.ToCharArray() | Where-Object { [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne [Globalization.UnicodeCategory]::NonSpacingMark })
    $s = ($t -replace '[^a-z0-9]+', '-').Trim('-')
    if ($s.Length -gt 40) { $s = $s.Substring(0, 40).Trim('-') }
    if (-not $s) { "task" } else { $s }
}

# Bước 6 chạy riêng (vd. cổng KT trượt vì test chập chờn ⇒ assign không gọi QC): Codex review nhánh task, ghi bảng việc theo kết luận.
function Invoke-Qc {
    $ctx = Get-TaskContext
    if (-not (Test-Path $ctx.Work)) { throw "Chưa có thư mục làm việc $($ctx.Work)" }
    $devProvider = if ($ctx.AgentId -ceq 'C') { 'codex' } else { 'agy' }
    $lastRun = Get-ChildItem $ctx.R.Logs -Filter "$Task-*.json" -ErrorAction SilentlyContinue |
        Where-Object { $_.BaseName -match ('^' + [regex]::Escape($Task) + '-\d+$') } |
        Sort-Object LastWriteTime | Select-Object -Last 1
    if ($lastRun) {
        $run = Read-Json $lastRun.FullName
        if ($run.provider -in @('codex','agy')) { $devProvider = $run.provider }
    }
    $rv = Invoke-AutoReview $ctx.R $ctx.TaskFile.Name $ctx.Work $devProvider
    if (-not $rv) { Say "Review AI bỏ qua (task không bật Review AI?)." Yellow; return }
    $text = @{ 0 = "🔵 Chờ review · cổng KT ĐẠT · AI review PASS"; 1 = "🔴 Cần sửa · AI review CẦN SỬA"; 3 = "🔵 Chờ review · AI review BLOCKED (Claude review)" }[[int]$rv.Code]
    Say "QC: mã $($rv.Code) · $($rv.Out)" Cyan
    $qcRes = switch ($rv.Code) {
        0 { 'PASS' }
        1 { 'CẦN SỬA' }
        3 { 'BLOCKED' }
        default { 'LỖI' }
    }
    Invoke-RepoTransaction -RepoPath $ctx.R.Path -Action {
        $qcRun = Get-NextJournalRun $ctx.R.Path $ctx.TaskFile.Name $Task 'Qc'
        if ($text) {
            Set-BoardStatus $ctx.R $Task $text -Journal @{ TaskFile = $ctx.TaskFile.Name; Event = 'Qc'; Run = $qcRun; Result = $qcRes }
        } else {
            Update-WorkflowJournal -RepoPath $ctx.R.Path -TaskFile $ctx.TaskFile.Name -TaskId $Task -Event 'Qc' -Run $qcRun -Result $qcRes
            Set-BoardStatus $ctx.R $Task '🔴 Lỗi QC'
        }
    }
}

# Bước 2: plans/R-xxx/brief.md (Claude viết, ≤ 20 dòng) ⇒ Codex đọc repo CHỈ ĐỌC ⇒ plans/R-xxx/plan.md. -Name = tiền tố mã task (vd. V6).
# /// <summary>Đọc quyết định qua aiteam workflow-role --root $Root --role $Role; JSON camelCase.
# /// CLI lỗi hoặc JSON không hợp lệ phải throw, không trả mặc định; không ghi team.json.</summary>
function Resolve-WorkflowAssignment([string]$Role) {
    $exe = Get-AITeamExe
    if (-not $exe) { throw 'Không build được aiteam.exe' }
    $json = & $exe workflow-role --root $Root --role $Role
    if ($LASTEXITCODE -ne 0) { throw 'Không đọc được cấu hình phân vai.' }
    try { $assignment = ($json -join "`n") | ConvertFrom-Json } catch { throw 'Phân vai không hợp lệ.' }
    if (-not $assignment -or $assignment.role -cne $Role -or
        $assignment.actor -cnotin @('claude','codex','minh','lan','gate','owner') -or
        $assignment.isExplicit -isnot [bool] -or $assignment.readOnly -isnot [bool] -or $assignment.manual -isnot [bool] -or
        $assignment.readOnly -ne ($Role -ne 'prep')) { throw 'Phân vai không hợp lệ.' }
    $provider = switch -CaseSensitive ($assignment.actor) { 'codex' {'codex'} 'minh' {'agy'} 'lan' {'agy'} default {$null} }
    if (-not $assignment.isExplicit -and $Role -eq 'qc') { $provider = $null }
    if ($assignment.providerId -cne $provider -or $assignment.manual -ne ($assignment.actor -cin @('claude','gate','owner'))) { throw 'Phân vai không hợp lệ.' }
    if ($assignment.manual) { throw 'Tác nhân được gán cần thực hiện thủ công.' }
    return $assignment
}

# /// <summary>Chạy đúng Assignment bằng $Exe với mảng Arguments; không Invoke-Expression.
# /// Manual => throw "Tác nhân được gán cần thực hiện thủ công." trước gọi exe.
# /// run: thêm --provider đúng người; plan thêm --read-only, prep không thêm; review: thêm
# /// --reviewer-provider khi IsExplicit, QC cũ không thêm. Giữ mã LASTEXITCODE.
# /// Chỉ bọc Invoke-WorkflowCodexLock khi ProviderId=codex; không khóa agy/thủ công.
# /// Không fallback; không đổi --out/--root/timeout hay cờ khác trong Arguments.</summary>
function Invoke-WorkflowRoleRun($Assignment, [string]$Exe, [string[]]$Arguments) {
    if ($Assignment.Manual) { throw 'Tác nhân được gán cần thực hiện thủ công.' }
    $callArgs = @($Arguments)
    if ($callArgs[0] -ceq 'run') {
        $callArgs += @('--provider', $Assignment.ProviderId)
        if ($Assignment.Role -ceq 'plan' -and '--read-only' -notin $callArgs) { $callArgs += '--read-only' }
    } elseif ($callArgs[0] -ceq 'review' -and $Assignment.IsExplicit) {
        $callArgs += @('--reviewer-provider', $Assignment.ProviderId)
    }
    if ($Assignment.ProviderId -ceq 'codex') {
        Invoke-WorkflowCodexLock -Action { & $Exe @callArgs }
    } else { & $Exe @callArgs }
}
function Invoke-Plan {
    $assignment = Resolve-WorkflowAssignment 'plan'
    $r = Resolve-Project $(if ($Project) { $Project } else { "AITeam" })
    $dir = Get-PlanDir $Request
    $brief = Join-Path $dir "brief.md"
    if (-not (Test-Path $brief)) { throw "Chưa có $brief — Claude viết brief (≤ 20 dòng) trước." }
    $prefix = if ($Name) { $Name } else { "T" }
    $exe = Get-AITeamExe
    if (-not $exe) { throw "Không build được aiteam.exe" }
    $tpl = [IO.File]::ReadAllText((Join-Path $Root "templates\codex-plan.md"))
    $promptText = $tpl.Replace("{REQUEST}", $Request).Replace("{PREFIX}", $prefix) + "`n" + [IO.File]::ReadAllText($brief)
    New-Item -ItemType Directory -Force $r.Logs | Out-Null
    $promptText = Add-SkillPrompt -Prompt $promptText -Root $Root -Stage plan -Stack $r.Info.stack
    $pf = Join-Path $r.Logs "$Request.plan.prompt.md"
    [IO.File]::WriteAllText($pf, $promptText, $Utf8)
    $out = Join-Path $r.Logs "$Request.plan.json"
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    $before = git -C $r.Path status --porcelain
    Say "[$(Get-Date -Format HH:mm:ss)] $Request → Codex lập kế hoạch (chỉ đọc) trong $($r.Path)" Cyan
    Invoke-WorkflowRoleRun $assignment $exe @("run", "--workdir", $r.Path, "--prompt-file", $pf, "--logdir", (Join-Path $r.Logs "$Request.plan"), "--timeout-min", "20", "--out", $out, "--root", $Root) 2>&1 | ForEach-Object { Say "  $_" }
    $code = $LASTEXITCODE; $global:LASTEXITCODE = 0
    $after = git -C $r.Path status --porcelain
    $ErrorActionPreference = $eap
    if ("$before" -ne "$after") { Say "CẢNH BÁO: bước chỉ đọc đã làm thay đổi file trong $($r.Path) — kiểm tra git status!" Red }
    $j = $null; if (Test-Path $out) { try { $j = Read-Json $out } catch { } }
    if ($code -ne 0 -or -not $j -or -not $j.text) {
        Say "Codex không lập được kế hoạch (mã $code$(if ($j) { ': ' + $j.summary })) ⇒ Claude tự viết $dir\plan.md (quy trình v3)." Yellow
        return
    }
    $plan = Join-Path $dir "plan.md"
    $mockup = Write-PlanMockup -RequestId $Request -PlanText ([string]$j.text)
    [IO.File]::WriteAllText($plan, ([string]$j.text).Trim() + "`n", $Utf8)
    Say "Đã ghi $plan ($($j.summary))." Green
    Say "Bước 3: Claude đọc + sửa plan.md nếu cần, rồi với từng task: .\team.ps1 prep -Request $Request -Task <MÃ> [-NoLaunch]"
}

# Bước 4–6: Codex chuẩn bị 1 task từ mục "### <MÃ> · …" của plan.md (worktree nhánh mới từ origin/main):
# file task + khung + test nghiệm thu ⇒ build phải 0 lỗi ⇒ commit + push nhánh, file task + dòng bảng việc lên main
# ⇒ giao Antigravity (assign; task có "Review AI: có" ⇒ Codex QC sau cổng KT). -NoLaunch: chỉ chuẩn bị, không giao.
function Invoke-Prep {
    $r = Resolve-Project $(if ($Project) { $Project } else { "AITeam" })
    $dir = Get-PlanDir $Request
    $planFile = Join-Path $dir "plan.md"
    if (-not (Test-Path $planFile)) { throw "Chưa có $planFile (chạy .\team.ps1 plan -Request $Request trước)" }
    if ($Task -notmatch '^[A-Za-z0-9]+-\d+[a-z]?$') { throw "Thiếu / sai -Task (mã trong plan.md, vd. V6-01)" }
    $plan = [IO.File]::ReadAllText($planFile)
    Assert-PrepPlanApproval -RequestId $Request -PlanText $plan
    $assignment = Resolve-WorkflowAssignment 'prep'
    $m = [regex]::Match($plan, "(?ms)^###\s+" + [regex]::Escape($Task) + "\s*[·:-]\s*(?<title>[^\r\n]+)\r?\n(?<body>.*?)(?=^###\s|^##\s|\z)")
    if (-not $m.Success) { throw "Không thấy mục '### $Task · …' trong $planFile" }
    $ttl = $m.Groups['title'].Value.Trim()
    $section = "### $Task · $ttl`n" + $m.Groups['body'].Value
    $agentId = [regex]::Match($section, '\*\*Agent:\*\*\s*([ABab])').Groups[1].Value.ToUpper()
    if (-not $agentId) { $agentId = "A" }
    $agentId = Select-FreeAgent $r $agentId   # chia đều Minh / Lan theo ai đang rảnh (kế hoạch chỉ là gợi ý)
    $budget = [regex]::Match($section, '\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
    $existing = @(Get-ChildItem (Join-Path $r.Path "tasks") -Filter "$Task-*.md" -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) { throw "Đã có tasks\$($existing[0].Name) — task này đã được chuẩn bị." }
    $slug = ConvertTo-AsciiSlug $ttl
    $fileName = "$Task-$slug.md"
    $branch = "agent-$($agentId.ToLower())/$Task-$slug"
    $work = Join-Path $r.Work "prep-$($Task.ToLower())"
    New-Item -ItemType Directory -Force $r.Logs | Out-Null

    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $r.Path fetch -q origin 2>$null
    # Lần trước Codex đã viết xong nhưng dừng ở build / commit (vd. khóa file) ⇒ đi tiếp từ build, KHÔNG chạy lại Codex (~500k token).
    $resumePrep = (Test-Path (Join-Path $work "tasks\$fileName"))
    if ($resumePrep) { Say "Đã có worktree chuẩn bị $work ⇒ bỏ qua bước Codex, đi tiếp từ build." Yellow }
    else {
    git -C $r.Path worktree add -q -b $branch $work origin/main 2>&1 | ForEach-Object { Say "  $_" }
    if (-not (Test-Path $work)) { $ErrorActionPreference = $eap; throw "Không tạo được worktree $work" }
    $taskPath = Join-Path $work "tasks\$fileName"
    Copy-Template "task.md" $taskPath @{ TASK = $Task; TITLE = $ttl; AGENT = $agentId; BRANCH = $branch; BUILD = $r.Info.build; TEST = $r.Info.test } | Out-Null
    $tt = [IO.File]::ReadAllText($taskPath).Replace("- **Yêu cầu:** R-xxx", "- **Yêu cầu:** $Request")
    $tt = [regex]::Replace($tt, '(?m)^<!-- Tùy chọn: - \*\*Review AI:\*\*.*-->\r?$', '- **Review AI:** có')
    if ($budget) { $tt = [regex]::Replace($tt, '\*\*Ngân sách:\*\*\s*~\d+ dòng', "**Ngân sách:** ~$budget dòng") }
    [IO.File]::WriteAllText($taskPath, $tt, $Utf8)

    $tpl = [IO.File]::ReadAllText((Join-Path $Root "templates\codex-prep.md"))
    $promptText = $tpl.Replace("{TASKFILE}", "tasks/$fileName").Replace("{BUILD}", [string]$r.Info.build).Replace("{TASK}", $Task).Replace("{REQUEST}", $Request) + "`n" + $section
    $promptText = Add-SkillPrompt -Prompt $promptText -Root $Root -Stage prep -Stack $r.Info.stack
    $pf = Join-Path $r.Logs "$Task.prep.prompt.md"
    [IO.File]::WriteAllText($pf, $promptText, $Utf8)
    $out = Join-Path $r.Logs "$Task.prep.json"
    $exe = Get-AITeamExe
    Say "[$(Get-Date -Format HH:mm:ss)] $Task → Codex chuẩn bị task (đặc tả + khung + test nghiệm thu) trong $work" Cyan
    Invoke-WorkflowRoleRun $assignment $exe @("run", "--workdir", $work, "--prompt-file", $pf, "--logdir", (Join-Path $r.Logs "$Task.prep"), "--timeout-min", "20", "--out", $out, "--root", $Root) 2>&1 | ForEach-Object { Say "  $_" }
    $code = $LASTEXITCODE; $global:LASTEXITCODE = 0
    $j = $null; if (Test-Path $out) { try { $j = Read-Json $out } catch { } }
    if ($code -ne 0) {
        Say "Codex không chuẩn bị được $Task (mã $code$(if ($j) { ': ' + $j.summary })). Worktree giữ ở $work, nhánh $branch — Claude làm tiếp (quy trình v3)." Yellow
        $ErrorActionPreference = $eap; return
    }
    }
    # Codex hay ghi UTF-8 không BOM ⇒ trả BOM cho file mã (quy ước dự án, PowerShell 5.1).
    foreach ($f in @(git -C $work status --porcelain -uall 2>$null | ForEach-Object { $_.Substring(3).Trim('"') } | Where-Object { $_ -match '\.(cs|cshtml|ps1)$' })) {
        $p = Join-Path $work $f
        if (Test-Path $p -PathType Leaf) { $b = [IO.File]::ReadAllBytes($p); if (-not (Test-HasBom $b)) { [IO.File]::WriteAllBytes($p, [byte[]](@(0xEF, 0xBB, 0xBF) + $b)) } }
    }
    $bCheck = Invoke-WorkflowCheck -Stage 'Build' -Run {
        param($attempt)
        if ($attempt -gt 1) {
            Say "Build lỗi do khóa file (lỗi máy) ⇒ tắt build server, build lại 1 lần." Yellow
            dotnet build-server shutdown 2>&1 | Out-Null
            Start-Sleep -Seconds 20
        }
        Invoke-ShellCapture $r.Info.build $work
    }
    $bc = $bCheck.Code; $bo = $bCheck.Output
    if ($bCheck.Attempts.Count -gt 1) {
        $prepLog = Join-Path $r.Logs "$Task.prep.build.log"
        $logContent = "=== Lần 1 (mã $($bCheck.Attempts[0].Code)) ===`r`n$($bCheck.Attempts[0].Output)`r`n=== Lần 2 (mã $($bCheck.Attempts[1].Code)) ===`r`n$($bCheck.Attempts[1].Output)"
        [IO.File]::WriteAllText($prepLog, $logContent, $Utf8)
    }
    if ($bc -ne 0) {
        Invoke-RepoTransaction -RepoPath $r.Path -Action {
            $pendingTask = Join-Path $r.Path "tasks/$fileName"
            $copiedTask = -not (Test-Path -LiteralPath $pendingTask)
            if ($copiedTask) { Copy-Item (Join-Path $work "tasks/$fileName") $pendingTask }
            try {
            $prepRun = Get-NextJournalRun $r.Path $fileName $Task 'Prep'
            Update-WorkflowJournal -RepoPath $r.Path -TaskFile $fileName -TaskId $Task -Event 'Prep' -Run $prepRun -Result 'KHÔNG ĐẠT'
            } finally { if ($copiedTask) { Remove-Item -LiteralPath $pendingTask } }
        }
        Say "Build khung $Task LỖI ⇒ KHÔNG giao. Worktree giữ ở $work." Red
        $bo | Select-String -Pattern ' error ' | Select-Object -First 10 | ForEach-Object { Say "  $_" }
        $ErrorActionPreference = $eap; return
    }
    git -C $work add -A 2>$null
    git -C $work commit -q -m "Test nghiệm thu $Task + khung (Codex, quy trình v4)" 2>&1 | Select-Object -Last 8 | ForEach-Object { Say "  $_" Yellow }
    if (@(git -C $work status --porcelain 2>$null | Where-Object { $_ }).Count -gt 0) {
        Say "Commit $Task bị chặn (hook quét bí mật?) ⇒ KHÔNG giao. Worktree giữ ở $work." Red
        $ErrorActionPreference = $eap; return
    }
    git -C $work push -q -u origin $branch 2>&1 | Select-Object -Last 2 | ForEach-Object { Say "  $_" }
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }

    # File task + dòng bảng việc lên main (nội dung giống hệt trên nhánh ⇒ merge không xung đột).
    Invoke-RepoTransaction -RepoPath $r.Path -Action {
        git -C $r.Path pull -q --ff-only 2>$null
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }
        Copy-Item (Join-Path $work "tasks\$fileName") (Join-Path $r.Path "tasks\$fileName") -Force
        $prepRun = Get-NextJournalRun $r.Path $fileName $Task 'Prep'
        Update-WorkflowJournal -RepoPath $r.Path -TaskFile $fileName -TaskId $Task -Event 'Prep' -Run $prepRun -Result 'ĐẠT'
        $board = Join-Path $r.Path "tasks\README.md"
        if (Test-Path $board) {
            $bt = [IO.File]::ReadAllText($board)
            $lines = [Collections.ArrayList]@($bt -split "`r?`n")
            $last = -1; for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\| \[') { $last = $i } }
            $row = "| [$Task $ttl]($fileName) | $agentId | ``$branch`` | ⬜ Chưa giao |"
            if ($last -ge 0) { $lines.Insert($last + 1, $row) } else { [void]$lines.Add($row) }
            $nl = if ($bt -match "`r`n") { "`r`n" } else { "`n" }
            [IO.File]::WriteAllText($board, ($lines -join $nl), (New-Object Text.UTF8Encoding (Test-HasBom ([IO.File]::ReadAllBytes($board)))))
        }
        $commitFiles = @("tasks/$fileName", "tasks/README.md")
        $reqMatch = [regex]::Match($tt, '\*\*Yêu cầu:\*\*\s*(R-\d+)')
        if ($reqMatch.Success -and (Test-Path (Join-Path $r.Path "requests\$($reqMatch.Groups[1].Value).md"))) {
            $commitFiles += "requests/$($reqMatch.Groups[1].Value).md"
        }
        git -C $r.Path add -- $commitFiles 2>$null
        git -C $r.Path commit -q -m "Task $Task (Codex chuẩn bị, $Request)" -- $commitFiles 2>&1 | Out-Null
        git -C $r.Path push -q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }
    }
    # Gộp main (đã có file task giống hệt) vào nhánh ngay ⇒ tổ tiên chung có file task ⇒ lần sau agent sửa báo cáo không bị xung đột add/add (H6-07).
    git -C $work fetch -q origin 2>$null
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }
    git -C $work merge -q --no-edit origin/main 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }
    git -C $work push -q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $ErrorActionPreference = $eap; throw "Chuẩn bị $Task thất bại ở giao dịch Git." }
    git -C $r.Path worktree remove --force $work 2>&1 | Out-Null
    $ErrorActionPreference = $eap
    Say "Đã chuẩn bị ${Task}: tasks/$fileName + nhánh $branch$(if ($j) { ' · ' + $j.summary })." Green
    if ($NoLaunch) { Say "Giao: .\team.ps1 assign -Project $($r.Info.name) -Task $Task"; return @{ Ok = $true } }
    Say "Giao Antigravity (Agent $agentId)…" Cyan
    $script:Project = $r.Info.name
    Invoke-Assign
}

# ───────────────────────── Bảo mật: hook + kho bí mật cục bộ ─────────────────────────

function Install-Hook([string]$repoPath) {
    $common = git -C $repoPath rev-parse --git-common-dir
    if (-not [IO.Path]::IsPathRooted($common)) { $common = Join-Path $repoPath $common }
    $hooks = Join-Path $common "hooks"
    New-Item -ItemType Directory -Force $hooks | Out-Null
    $scan = (Join-Path $Root "tools\secret-scan.ps1") -replace '\\', '/'
    $hook = "#!/bin/sh`n# AITeam: chặn commit có bí mật`npowershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scan`" -Repo `"`$(git rev-parse --show-toplevel)`"`n"
    [IO.File]::WriteAllText((Join-Path $hooks "pre-commit"), $hook.Replace("`r", ""), $Utf8)
    # B6 (R-004): worktree của agent (git-dir chứa /worktrees/) KHÔNG được push lên main/master — chỉ team.ps1 merge ở repo chính.
    $push = "#!/bin/sh`n# AITeam: agent khong duoc push len main/master (chi Claude merge bang team.ps1 o repo chinh)`n" +
        "gd=`$(git rev-parse --git-dir)`ncase `"`$gd`" in`n  */worktrees/*)`n    while read lref lsha rref rsha; do`n" +
        "      case `"`$rref`" in`n        refs/heads/main|refs/heads/master) echo `"AITeam: worktree agent KHONG duoc push len `$rref`" >&2; exit 1 ;;`n      esac`n" +
        "    done ;;`nesac`nexit 0`n"
    [IO.File]::WriteAllText((Join-Path $hooks "pre-push"), $push, (New-Object Text.UTF8Encoding $false))
    Say "  [OK] Hook chặn bí mật + chặn agent push main: $repoPath" Green
}

function Invoke-InstallHooks {
    foreach ($p in Get-Projects | Where-Object { -not $Project -or $_.name -eq $Project }) {
        $r = Resolve-Project $p.name
        if (Test-Path (Join-Path $r.Path ".git")) { Install-Hook $r.Path }
    }
    if (Test-Path (Join-Path $Root ".git")) { Install-Hook $Root }
}

$VaultFile = Join-Path $env:USERPROFILE ".aiteam\vault.json"
$VaultEntropy = [Text.Encoding]::UTF8.GetBytes("AITeam.vault.v1")

function Read-Vault {
    if (Test-Path $VaultFile) { Get-Content $VaultFile -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }
}

function Write-Vault($vault) {
    New-Item -ItemType Directory -Force (Split-Path $VaultFile) | Out-Null
    [IO.File]::WriteAllText($VaultFile, ($vault | ConvertTo-Json), $Utf8)
}

function Protect-Text([string]$plain) {
    Add-Type -AssemblyName System.Security
    $bytes = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($plain), $VaultEntropy, "CurrentUser")
    [Convert]::ToBase64String($bytes)
}

function Unprotect-Text([string]$cipher) {
    Add-Type -AssemblyName System.Security
    $bytes = [Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($cipher), $VaultEntropy, "CurrentUser")
    [Text.Encoding]::UTF8.GetString($bytes)
}

function Invoke-SecretSet {
    if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw "Tên bí mật dạng CHU_HOA_GACH_DUOI, ví dụ DB_PASSWORD" }
    $secure = Read-Host "Nhập giá trị cho $Name (không hiện trên màn hình)" -AsSecureString
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    if (-not $plain) { throw "Giá trị rỗng" }
    $vault = Read-Vault
    $vault | Add-Member -NotePropertyName $Name -NotePropertyValue (Protect-Text $plain) -Force
    Write-Vault $vault
    Say "Đã lưu $Name vào kho cục bộ $VaultFile (mã hóa DPAPI, không nằm trong repo nào)." Green
}

function Invoke-SecretList {
    $names = (Read-Vault).PSObject.Properties.Name
    if (-not $names) { Say "Kho bí mật trống." } else { Say "Bí mật đã lưu (chỉ hiện tên): $($names -join ', ')" }
}

function Invoke-SecretRemove {
    $vault = Read-Vault
    $vault.PSObject.Properties.Remove($Name)
    Write-Vault $vault
    Say "Đã xóa $Name" Green
}

function Invoke-SecretEnv {
    $r = Resolve-Project $Project
    $target = Join-Path $r.Path ".env.local"
    git -C $r.Path check-ignore -q ".env.local"
    if ($LASTEXITCODE -ne 0) { throw ".env.local CHƯA bị .gitignore trong $($r.Path) — thêm dòng '.env.*' vào .gitignore trước." }
    $vault = Read-Vault
    $lines = foreach ($n in $Names) {
        $c = $vault.$n
        if (-not $c) { throw "Không có bí mật $n (team.ps1 secret-list)" }
        "$n=$(Unprotect-Text $c)"
    }
    [IO.File]::WriteAllText($target, ($lines -join "`r`n") + "`r`n", $Utf8)
    Say "Đã ghi $($Names.Count) bí mật vào $target (bị .gitignore, chỉ dùng trên máy này)." Green
}

function Install-Skill {
    $dir = Join-Path $env:USERPROFILE ".claude\skills\aiteam"
    New-Item -ItemType Directory -Force $dir | Out-Null
    $text = (Get-Content (Join-Path $Root "skill\SKILL.md") -Raw -Encoding UTF8).Replace("{{AITEAM}}", $Root)
    [IO.File]::WriteAllText((Join-Path $dir "SKILL.md"), $text, $Utf8)
    Say "  [OK] Skill 'aiteam' cho Claude: $dir" Green
}


function Test-PortOpen([string]$hostName, [int]$port) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $iar = $tcp.BeginConnect($hostName, $port, $null, $null)
        $wait = $iar.AsyncWaitHandle.WaitOne(400, $false)
        if (-not $wait) { $tcp.Close(); return $false }
        $tcp.EndConnect($iar)
        $tcp.Close()
        return $true
    } catch {
        return $false
    }
}

function Invoke-Hub {
    param([switch]$Restart)
    if ($Restart) {
        $bin = [IO.Path]::GetFullPath((Join-Path $Root 'hub\bin')) + '\'
        $dllPattern = '(?i)(?:^|[\s"])' + [regex]::Escape($bin) + '[^"\r\n]*?AITeam\.Hub\.dll(?=["\s]|$)'
        Get-CimInstance Win32_Process | Where-Object {
            ($_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath).StartsWith($bin, [StringComparison]::OrdinalIgnoreCase)) -or
            ($_.CommandLine -and $_.CommandLine -match $dllPattern)
        } | ForEach-Object {
            $stopped = Stop-Process -Id $_.ProcessId -Force -PassThru -ErrorAction Stop
            if ($stopped -and -not $stopped.WaitForExit(10000)) { throw "Hub chưa dừng sau 10 giây." }
        }
        if (Test-PortOpen "127.0.0.1" 5057) { throw "Cổng 5057 vẫn đang được sử dụng; không thể khởi động lại Hub." }
    }
    $hubUrl = "http://127.0.0.1:5057"
    if (Test-PortOpen "127.0.0.1" 5057) {
        Say "Hub đang chạy tại $hubUrl — đang mở trình duyệt." Green
        Start-Process $hubUrl
        return
    }

    $hubDir = Join-Path $Root "hub"
    $hubProj = Join-Path $hubDir "AITeam.Hub.csproj"
    $hubDll = Join-Path $hubDir "bin\Debug\net10.0-windows\AITeam.Hub.dll"

    $needBuild = [bool]$Restart
    if (-not (Test-Path $hubDll)) {
        $needBuild = $true
    } else {
        $dllTime = (Get-Item $hubDll).LastWriteTimeUtc
        $srcFiles = Get-ChildItem -Path $hubDir -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
        foreach ($f in $srcFiles) {
            if ($f.LastWriteTimeUtc -gt $dllTime) {
                $needBuild = $true
                break
            }
        }
    }

    if ($needBuild) {
        Say "Đang build AITeam Hub..." Cyan
        dotnet build $hubProj --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "Build Hub thất bại." }
    }

    Say "Đang khởi động AITeam Hub ở nền..." Cyan
    Start-Process dotnet -ArgumentList "`"$hubDll`"" -WorkingDirectory $hubDir -WindowStyle Hidden

    $timeoutSeconds = 90
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $opened = $false
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSeconds) {
        if (Test-PortOpen "127.0.0.1" 5057) {
            $opened = $true
            break
        }
        Start-Sleep -Milliseconds 500
    }

    if ($opened) {
        Say "AITeam Hub đã mở tại $hubUrl" Green
        Start-Process $hubUrl
    } else {
        throw "Hub không mở cổng 5057 sau $timeoutSeconds giây."
    }
}

# ───────────────────────── Cổng kiểm tra tự động (verify) ─────────────────────────
# Chạy ngay sau khi agent xong, TRƯỚC khi Claude review: bắt hết lỗi cơ học để Claude chỉ còn review nội dung.

function Resolve-TaskWork($r, [string]$branch, [string]$defaultWork) {
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $listing = @(git -C $r.Path worktree list --porcelain 2>$null)
        if ($LASTEXITCODE -ne 0) { return $defaultWork }
        $pattern = '(?m)^worktree ([^\r\n]+)\r?\nHEAD [0-9a-f]+\r?\nbranch refs/heads/' + [regex]::Escape($branch) + '\r?$'
        $match = [regex]::Match(($listing -join "`n"), $pattern)
        if ($match.Success) { return $match.Groups[1].Value.Replace('/', '\') }
        return $defaultWork
    } catch { return $defaultWork }
    finally { $ErrorActionPreference = $savedPreference }
}

function Test-BranchPrepared($r, [string]$branch) {
    if (Resolve-TaskWork $r $branch '') { return $true }
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $remote = @(git -C $r.Path ls-remote --heads origin $branch 2>$null)
        return ($LASTEXITCODE -eq 0 -and $remote.Count -gt 0)
    } catch { return $false }
    finally { $ErrorActionPreference = $savedPreference }
}

function Get-AcceptRef([string]$Work, [string]$Base, [string]$Path, [string]$ManagerEmail) {
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $history = @(git -C $Work log '--format=%H%x09%ae%x09%s' "$Base..HEAD" -- $Path 2>$null)
        if ($LASTEXITCODE -ne 0) { return $null }
        foreach ($line in $history) {
            $parts = "$line" -split "`t", 3
            if ($parts.Count -eq 3 -and $parts[1] -ieq $ManagerEmail -and
                ($parts[2] -match '\(Claude\)' -or $parts[2] -match '^Test nghiệm thu')) {
                return $parts[0]
            }
        }
        git -C $Work cat-file -e "${Base}:$Path" 2>$null
        if ($LASTEXITCODE -eq 0) { return $Base }
        $added = @(git -C $Work log --reverse --diff-filter=A --format=%H "$Base..HEAD" -- $Path 2>$null)
        if ($LASTEXITCODE -eq 0 -and $added.Count -gt 0) { return $added[0] }
        return $null
    } catch { return $null }
    finally { $ErrorActionPreference = $savedPreference }
}

function Get-TaskContext {
    $r = Resolve-Project $Project
    if (-not $Task) { throw "Thiếu -Task" }
    $taskFile = Find-TaskFile $r $Task
    $text = Get-Content $taskFile.FullName -Raw -Encoding UTF8
    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $agentId = [regex]::Match($text, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
    [pscustomobject]@{
        R = $r; TaskFile = $taskFile; Text = $text; Branch = $branch; AgentId = $agentId
        Work = Resolve-TaskWork $r $branch (Join-Path $r.Work ("$agentId-$Task".ToLower()))
    }
}

# Nội dung dưới tiêu đề "## <tên>" (khớp phần đầu tên) cho tới tiêu đề "## " kế tiếp.
function Get-Section([string]$text, [string]$heading) {
    $m = [regex]::Match($text, "(?ms)^##\s+" + [regex]::Escape($heading) + "[^\r\n]*\r?\n(.*?)(?=^##\s|\z)")
    if ($m.Success) { return $m.Groups[1].Value } else { return "" }
}

# Các đường dẫn viết trong `…` (có / hoặc có đuôi file).
function Get-BacktickPaths([string]$section) {
    $section = [regex]::Replace($section, '(?s)<!--.*?-->', '')
    @([regex]::Matches($section, '`([^`\s]+)`') | ForEach-Object { $_.Groups[1].Value } |
        Where-Object { $_ -match '[/\\]' -or $_ -match '\.[A-Za-z0-9]{1,8}$' } |
        ForEach-Object { ($_ -replace '\\', '/') -replace '^\./', '' } | Select-Object -Unique)
}

function Test-PathInScope([string]$file, [string[]]$scope) {
    foreach ($s in $scope) {
        $p = $s.TrimEnd('/')
        if ($p -match '[\*\?]') {
            $rx = '^' + [regex]::Escape($p).Replace('\*\*', '.*').Replace('\*', '[^/]*').Replace('\?', '.') + '(/|$)'
            if ($file -match $rx) { return $true }
        } elseif ($file -eq $p -or $file.StartsWith("$p/")) { return $true }
    }
    return $false
}

function Get-PsExe { (Get-Process -Id $PID).Path }

# Chạy lệnh build/test của dự án trong thư mục cho trước, lấy mã thoát + toàn bộ output.
function Invoke-ShellCapture([string]$command, [string]$dir) {
    $buildMutex = $null; $buildOwned = $false; $locationPushed = $false
    try {
        if ($command -match '(?i)\bdotnet\s+build\b') {
            # Dùng chung giữa worktree/tiến trình trên máy, không để hai build tranh tệp/worker.
            $buildMutex = New-Object Threading.Mutex($false, 'Global\AITeam.Build')
            try { $buildOwned = $buildMutex.WaitOne([TimeSpan]::FromMinutes(15)) }
            catch [Threading.AbandonedMutexException] { $buildOwned = $true }
            if (-not $buildOwned) { throw 'Hết thời gian chờ build khác; chưa chạy build.' }
            $command = [regex]::Replace($command, '(?i)\bdotnet\s+build\b', '$0 -nodeReuse:false')
            $command = [regex]::Replace($command, '(?i)(?:-|/)(?:nr|nodeReuse):\s*true\b', '-nodeReuse:false')
        }
        Push-Location $dir; $locationPushed = $true
        if ($env:OS -eq 'Windows_NT') { $out = cmd /c "$command 2>&1" | Out-String }
        else { $out = sh -c "$command 2>&1" | Out-String }
        return [pscustomobject]@{ Code = $LASTEXITCODE; Output = $out }
    } finally {
        if ($locationPushed) { Pop-Location }
        if ($buildOwned) { $buildMutex.ReleaseMutex() }
        if ($buildMutex) { $buildMutex.Dispose() }
    }
}

# 3 byte đầu của file trong một commit (Start-Process ghi stdout nguyên byte, không bị đổi mã như pipe của PowerShell).
function Get-GitBlobHead([string]$repo, [string]$rev, [string]$file) {
    $tmp = [IO.Path]::GetTempFileName()
    try {
        $p = Start-Process git -ArgumentList "-C `"$repo`" show `"${rev}:$file`"" -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $tmp -RedirectStandardError "$tmp.err"
        if ($p.ExitCode -ne 0) { return $null }
        $b = [IO.File]::ReadAllBytes($tmp)
        return ,($b | Select-Object -First 3)
    } finally { Remove-Item $tmp, "$tmp.err" -ErrorAction SilentlyContinue }
}

function Test-HasBom([byte[]]$bytes) { $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF }

# Cảnh báo (không chặn) khi file TEST mới thêm tự dựng ProcessStartInfo thay vì helper chung.
# Chỉ xét file trạng thái A trong origin/main...HEAD; miễn helper Support của core.tests/hub.tests.
# Tên file có dấu/khoảng trắng: dùng git diff --name-status -z, không cắt theo khoảng trắng.
function Get-NewTestProcessStartInfoWarnings {
    param([string]$Work, [string]$NameStatus)
    $warnings = @()
    if (-not $PSBoundParameters.ContainsKey('NameStatus')) {
        $NameStatus = (@(git -C $Work diff --name-status -z --diff-filter=A origin/main...HEAD 2>$null) -join '')
        if ($LASTEXITCODE -ne 0) { return $warnings }
    }
    if (-not $NameStatus) { return $warnings }
    $parts = @($NameStatus -split ([string][char]0))
    for ($i = 0; $i + 1 -lt $parts.Count; $i += 2) {
        $status = $parts[$i]; $path = $parts[$i + 1]
        if ($status -ne 'A' -or -not $path) { continue }
        if ($path -match '(?i)^(core\.tests|hub\.tests)/Support/') { continue }
        if ($path -notmatch '(?i)(^|/)tests?/|tests?\.cs$') { continue }
        $full = Join-Path $Work ($path -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { continue }
        $text = [IO.File]::ReadAllText($full)
        if ($text -match 'new\s+(System\.Diagnostics\.|Diagnostics\.)?ProcessStartInfo\b') {
            $warnings += "Test mới tự dựng ProcessStartInfo thay vì helper TestShell/TeamScript: $path"
        }
    }
    return $warnings
}

function Invoke-Verify {
    $cfg = Get-Config
    $ctx = Get-TaskContext
    $r = $ctx.R; $work = $ctx.Work; $branch = $ctx.Branch
    if (-not (Test-Path $work)) { throw "Chưa có thư mục làm việc $work (task chưa được giao trên máy này)." }
    $ErrorActionPreference = "Continue"   # git / dotnet in ra stderr
    $fails = New-Object Collections.ArrayList
    $warns = New-Object Collections.ArrayList
    $notes = New-Object Collections.ArrayList
    Say "== Cổng kiểm tra $($r.Info.name) $Task ($work) ==" Cyan

    git -C $work fetch origin --quiet 2>$null
    $base = (git -C $work merge-base origin/main HEAD 2>$null | Select-Object -First 1)
    if (-not $base) { $base = "origin/main" }
    $changed = @(git -C $work diff --name-only --diff-filter=ACMR $base HEAD 2>$null | Where-Object { $_ })
    [void]$notes.Add("File thay đổi so với main: $($changed.Count)")
    if ($changed.Count -eq 0) { [void]$fails.Add("Nhánh không có thay đổi nào so với main (agent chưa làm hoặc chưa commit).") }

    # 0. Cổng KT (V10-03 / R-014): diff chạm file nhạy cảm ⇒ tạo mục duyệt bảo mật cho (task, commit HEAD).
    $sensitiveFiles = @(Get-SensitiveChanges $changed $cfg)
    if ($sensitiveFiles.Count -gt 0) {
        $gateHead = (git -C $work rev-parse HEAD 2>$null | Select-Object -First 1)
        $existingGate = @()
        try { $existingGate = @(Get-Approvals | Where-Object { ([string]$_.kind).ToLowerInvariant() -eq 'security' -and $_.task -eq $Task -and $_.commit -eq $gateHead }) } catch { }
        if ($existingGate.Count -gt 0) {
            [void]$notes.Add("Cổng KT: đã có mục duyệt bảo mật $($existingGate[0].id) cho $Task")
        } else {
            try {
                $gateRisk = if (@($sensitiveFiles | Where-Object { $_ -match '(?i)(^|/)\.env|(^|/)vault|secret' }).Count -gt 0) { 'high' } else { 'medium' }
                $gateDetail = "Cổng KT phát hiện file nhạy cảm trong diff:`r`n" + (($sensitiveFiles | ForEach-Object { "- $_" }) -join "`r`n")
                $gateId = New-ApprovalItem -Kind 'security' -Title "Cổng KT: $Task chạm file nhạy cảm" -Description $gateDetail -ProjectValue $r.Info.name -TaskValue $Task -CommitValue $gateHead -RiskValue $gateRisk -FilesValue $sensitiveFiles
                [void]$notes.Add("Cổng KT: tạo mục duyệt bảo mật $gateId")
            } catch {
                [void]$fails.Add("Cổng KT không tạo được mục duyệt bảo mật: $($_.Exception.Message)")
            }
        }
    }

    foreach ($deleted in @(git -C $work diff --name-only --diff-filter=D origin/main...HEAD 2>$null)) {
        if ($deleted -match '(?i)(^|/)[^/]*tests/.*\.cs$') { [void]$fails.Add("Xóa file test: $deleted") }
    }
    # 1. Agent đã commit hết chưa
    $dirty = @(git -C $work status --porcelain 2>$null | Where-Object { $_ })
    if ($dirty.Count -gt 0) {
        [void]$fails.Add("Agent CHƯA COMMIT $($dirty.Count) file: " + (($dirty | Select-Object -First 8 | ForEach-Object { $_.Trim() }) -join ', '))
    }

    # 2. BOM: file vốn có BOM phải giữ; .ps1 có chữ ngoài ASCII phải có BOM (PowerShell 5.1). Tự sửa + commit.
    $bomFixed = @()
    # File khung Claude tạo trên nhánh (chưa có trên main) cũng phải giữ BOM như bản khung (H3-01 lọt lỗi này).
    $seedCommit = git -C $work log --format=%H --grep="^Test nghiệm thu" "$base..HEAD" 2>$null | Select-Object -Last 1
    foreach ($f in $changed) {
        $full = Join-Path $work $f
        if (-not (Test-Path $full -PathType Leaf)) { continue }
        $bytes = [IO.File]::ReadAllBytes($full)
        if (Test-HasBom $bytes) { continue }
        $need = $false
        $old = Get-GitBlobHead $work $base $f
        if (-not $old -and $seedCommit) { $old = Get-GitBlobHead $work $seedCommit $f }
        if ($old -and (Test-HasBom ([byte[]]$old))) { $need = $true }
        elseif ($f -match '\.ps1$' -and ($bytes | Where-Object { $_ -gt 127 } | Select-Object -First 1)) { $need = $true }
        if ($need) {
            [IO.File]::WriteAllBytes($full, [byte[]](0xEF, 0xBB, 0xBF) + $bytes)
            $bomFixed += $f
        }
    }
    if ($bomFixed.Count -gt 0) {
        if ($dirty.Count -eq 0) {
            git -C $work add -- $bomFixed
            git -C $work commit -q -m "verify: trả lại BOM UTF-8 ($($bomFixed.Count) file)" 2>$null
            [void]$notes.Add("ĐÃ TỰ SỬA: trả lại BOM UTF-8 cho " + ($bomFixed -join ', '))
        } else {
            [void]$fails.Add("Mất BOM UTF-8 (đã ghi lại BOM nhưng chưa commit vì còn file agent chưa commit): " + ($bomFixed -join ', '))
        }
    }

    # Giữ SHA trước các phép kiểm tra; không gắn bằng chứng cho commit đổi giữa lượt.
    $verifyStartHead=git -C $work rev-parse HEAD
    $script:AcceptVerify=$null

    # 3. Test nghiệm thu do Claude viết trước: phải còn nguyên
    $accept = Get-BacktickPaths (Get-Section $ctx.Text "Test nghiệm thu")
    $allowedHarness = @()
    foreach ($permission in [regex]::Matches($ctx.Text, '(?m)^-\s+\*\*Cho phép sửa harness:\*\*\s*([^\r\n]+)')) {
        $declared = @(Get-BacktickPaths $permission.Groups[1].Value)
        if ($declared.Count -eq 0) { $declared = @($permission.Groups[1].Value.Trim()) }
        foreach ($path in $declared) {
            if ($accept -contains $path -and $path -notmatch '(^|[\\/])\.\.([\\/]|$)|^[\\/]|:') { $allowedHarness += $path }
        }
    }
    foreach ($a in $accept) {
        if (-not (Test-Path (Join-Path $work $a))) { [void]$fails.Add("Thiếu file test nghiệm thu: $a"); continue }
        # Ưu tiên bản sửa được quản lý đánh dấu, rồi mới dùng bản trên main hoặc lần thêm đầu.
        $ref = Get-AcceptRef -Work $work -Base $base -Path $a -ManagerEmail "$($cfg.github.owner)@users.noreply.github.com"
        if (-not $ref) { [void]$fails.Add("Không tìm thấy commit thêm test nghiệm thu: $a"); continue }
        git -C $work diff --quiet $ref HEAD -- $a 2>$null
        if ($LASTEXITCODE -ne 0) {
            $harnessOk = $false
            if ($allowedHarness -contains $a) {
                $policyExe = Get-AITeamExe
                if ($policyExe) {
                    $diffFile = [IO.Path]::GetTempFileName()
                    try {
                        $diffLines = @(git -C $work diff --no-ext-diff --no-textconv --unified=0 $ref HEAD -- $a 2>$null)
                        if ($LASTEXITCODE -eq 0) {
                            [IO.File]::WriteAllText($diffFile, ($diffLines -join "`n"), (New-Object Text.UTF8Encoding $false))
                            $policyMessage = @(& $policyExe check-harness-change --allow --diff-file $diffFile)
                            $harnessOk = $LASTEXITCODE -eq 0
                        }
                    } finally { Remove-Item -LiteralPath $diffFile -ErrorAction SilentlyContinue }
                }
            }
            if ($harnessOk) { [void]$notes.Add("Đã cho phép sửa harness khai báo: $a; không đổi assert/thuộc tính test, Claude review phần dựng môi trường.") }
            else { [void]$fails.Add("Agent đã SỬA test nghiệm thu (không được phép hoặc đụng assert): $a") }
        }
    }
    if ($accept.Count -gt 0 -and -not ($fails | Where-Object { $_ -match 'test nghiệm thu' })) { [void]$notes.Add("Test nghiệm thu còn nguyên: $($accept.Count) file") }

    # 4. Phạm vi
    $scope = @(Get-BacktickPaths (Get-Section $ctx.Text "Phạm vi"))
    if ($scope.Count -gt 0) {
        $always = @("tasks/$($ctx.TaskFile.Name)", "docs/screenshots/$Task", "CHANGELOG.md") + $accept
        $out = @($changed | Where-Object { -not (Test-PathInScope $_ ($scope + $always)) })
        # File khung do Claude đưa lên trong commit "Test nghiệm thu … (Claude)": agent KHÔNG đổi ⇒ không tính ngoài phạm vi.
        # Agent sửa file khung (bị cấm) ⇒ vẫn bị bắt (V4-06: Lan phải gộp JobModels.cs để qua cổng).
        $claudeCommit = git -C $work log --format=%H --grep="^Test nghiệm thu" "$base..HEAD" 2>$null | Select-Object -Last 1
        if ($claudeCommit -and $out.Count -gt 0) {
            $out = @($out | Where-Object {
                $inClaude = @(git -C $work show --name-only --format= $claudeCommit -- $_ 2>$null | Where-Object { $_ })
                if (-not $inClaude) { return $true }
                git -C $work diff --quiet $claudeCommit HEAD -- $_ 2>$null
                return ($LASTEXITCODE -ne 0)
            })
        }
        if ($out.Count -gt 0) { [void]$fails.Add("Sửa NGOÀI PHẠM VI task ($($out.Count) file) — hoàn tác các file này hoặc ghi lý do bắt buộc vào báo cáo để quản lý quyết: " + (($out | Select-Object -First 10) -join ', ')) }
    } else { [void]$notes.Add("Task không ghi đường dẫn trong mục Phạm vi → bỏ qua kiểm tra phạm vi.") }
    # 4b. Ngân sách code: dòng "- **Ngân sách:** ~400 dòng" trong file task (không tính file task và test nghiệm thu).
    $budgetText = [regex]::Match($ctx.Text, '\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
    if ($budgetText) {
        $limit = [int]$budgetText
        $countFiles = @($changed | Where-Object { $_ -ne "tasks/$($ctx.TaskFile.Name)" -and $accept -notcontains $_ })
        $added = 0
        if ($countFiles.Count -gt 0) {
            # Khung code của Claude (commit "Test nghiệm thu…") không tính vào ngân sách của agent (H3-01 báo vượt nhầm 508/320).
            $from = if ($seedCommit) { $seedCommit } else { $base }
            foreach ($line in (git -C $work diff --numstat $from HEAD -- $countFiles 2>$null)) {
                $p = $line -split "`t"; if ($p[0] -match '^\d+$') { $added += [int]$p[0] }
            }
        }
        [void]$notes.Add("Code thêm mới: $added dòng / ngân sách $limit")
        # Vượt ngân sách KHÔNG tự trả agent: ngân sách có thể do Claude ước sai (C-011, 21/10). Claude xem và quyết khi review.
        if ($added -gt [math]::Ceiling($limit * 1.5)) { [void]$warns.Add("VƯỢT NGÂN SÁCH quá 1,5 lần: thêm $added dòng / $limit — Claude xem: code dư hay ngân sách ước thấp?") }
        elseif ($added -gt $limit) { [void]$warns.Add("Code thêm $added dòng, vượt ngân sách $limit (chưa quá x1.5).") }
    }

    # 5. Test có nhánh return sớm / assert luôn đúng (chỉ xét dòng mới thêm trong file test)
    $testFiles = @($changed | Where-Object { $_ -match '(?i)(^|/)tests?/|tests?\.cs$' })
    if ($testFiles.Count -gt 0) {
        $cur = ""; $sus = @()
        foreach ($line in (git -C $work diff -U0 $base HEAD -- $testFiles 2>$null)) {
            if ($line -match '^\+\+\+ b/(.+)$') { $cur = $Matches[1]; continue }
            if ($line -notmatch '^\+') { continue }
            if ($line -match '^\+\s*if\s*\(.+\)\s*\{?\s*return\s*;' -or $line -match 'Assert\.True\(\s*true\s*\)') { $sus += "$cur → $($line.Substring(1).Trim())" }
        }
        if ($sus.Count -gt 0) { [void]$warns.Add("Test có thể luôn pass (return sớm / Assert.True(true)): " + (($sus | Select-Object -First 5) -join ' | ')) }
    }

    # 5b. Cảnh báo (không chặn) test MỚI tự dựng ProcessStartInfo; helper Support được miễn.
    foreach ($processWarning in @(Get-NewTestProcessStartInfoWarnings -Work $work)) { [void]$warns.Add($processWarning) }

    # 6. Báo cáo của agent
    $report = [regex]::Replace((Get-Section $ctx.Text "Báo cáo của agent"), '(?s)<!--.*?-->', '').Trim()
    $reportNow = ""
    $taskInWork = Join-Path (Join-Path $work "tasks") $ctx.TaskFile.Name
    if (Test-Path $taskInWork) { $reportNow = [regex]::Replace((Get-Section (Get-Content $taskInWork -Raw -Encoding UTF8) "Báo cáo của agent"), '(?s)<!--.*?-->', '').Trim() }
    if ($reportNow.Length -lt 40 -and $report.Length -lt 40) { [void]$fails.Add("Agent chưa điền mục 'Báo cáo của agent' trong file task.") }

    # 7. Quét bí mật trên phần thay đổi (chạy tiến trình riêng: script dùng exit)
    $scan = Join-Path (Join-Path $Root "tools") "secret-scan.ps1"
    if (Test-Path $scan) {
        $sc = Invoke-ShellCapture "`"$(Get-PsExe)`" -NoProfile -ExecutionPolicy Bypass -File `"$scan`" -Repo `"$work`" -Range $base..HEAD" $work
        if ($sc.Code -ne 0) { [void]$fails.Add("Quét bí mật: CHẶN`n" + $sc.Output.Trim()) } else { [void]$notes.Add("Quét bí mật: sạch") }
    }

    # 8. Build (0 warning) + test
    $buildAttempts = @()
    if ($r.Info.build) {
        $bCheck = Invoke-WorkflowCheck -Stage 'Build' -Run {
            param($attempt)
            if ($attempt -gt 1) { Start-Sleep -Seconds 20 }
            Invoke-ShellCapture $r.Info.build $work
        }
        $b = [pscustomobject]@{ Code = $bCheck.Code; Output = $bCheck.Output }
        $buildAttempts = $bCheck.Attempts

        $w = @([regex]::Matches($b.Output, '(?m)^.*: warning [A-Za-z]+\d+:.*$') | ForEach-Object { ($_.Value -replace '\s*\[[^\]]+\]\s*$', '').Trim() } | Select-Object -Unique)
        # NU1900 = NuGet không tải được dữ liệu lỗ hổng (lỗi MẠNG, không phải lỗi code) ⇒ ghi chú, không đánh trượt (R-004 / V4-11).
        $netWarn = @($w | Where-Object { $_ -match ': warning NU1900:' })
        if ($netWarn) { $w = @($w | Where-Object { $_ -notmatch ': warning NU1900:' }); [void]$notes.Add("Bỏ qua $($netWarn.Count) cảnh báo NU1900 (lỗi mạng khi tải dữ liệu lỗ hổng NuGet)") }
        if ($b.Code -ne 0) {
            $errs = @([regex]::Matches($b.Output, '(?m)^.*: error [A-Za-z]+\d+:.*$') | ForEach-Object { $_.Value.Trim() } | Select-Object -Unique -First 10)
            [void]$fails.Add("Build LỖI (mã $($b.Code)):`n  " + ($errs -join "`n  "))
        } elseif ($w.Count -gt 0) {
            [void]$fails.Add("Build có $($w.Count) warning (yêu cầu 0):`n  " + (($w | Select-Object -First 10) -join "`n  "))
        } else {
            $bSum = @($b.Output -split "`r?`n" | Where-Object { $_ -match '(?i)(Passed!|Build succeeded|0 Warning|OK)' } | ForEach-Object { $_.Trim() } | Select-Object -First 1)
            [void]$notes.Add("Build: OK, 0 warning" + $(if ($bSum) { " ($bSum)" } else { "" }))
        }
    }
    $testAttempts = @()
    if ($r.Info.test -and -not ($fails | Where-Object { $_ -like 'Build LỖI*' })) {
        # Chốt chặn treo: test giao diện bật hộp thoại modal từng làm treo cả agent lẫn cổng kiểm tra (21/10/2026, 47 phút / task).
        $testCmd = $r.Info.test
        if ($testCmd -match '^\s*dotnet\s+test\b' -and $testCmd -notmatch 'blame-hang') { $testCmd += " --blame-hang-timeout 3m --blame-hang-dump-type none" }
        $tCheck = Invoke-WorkflowCheck -Stage 'Test' -Run {
            param($attempt)
            Invoke-ShellCapture $testCmd $work
        }
        $t = Invoke-FailedTestRetry -Code $tCheck.Code -Output $tCheck.Output -TestCommand $testCmd -Run {
            param($command)
            Invoke-ShellCapture $command $work
        }
        foreach ($warning in $t.Warnings) { [void]$warns.Add($warning) }
        $testAttempts = @($tCheck.Attempts) + @($t.Attempts)

        if ($t.Output -match '(?i)blame-hang|hang timeout|The active test run was aborted') { [void]$fails.Add("Test bị TREO (quá 3 phút, thường do test giao diện mở hộp thoại / form / MessageBox thật). Xem luật 'Test giao diện' trong lời giao việc.") }
        $summary = @($t.Output -split "`r?`n" | Where-Object { $_ -match '(?i)(Passed!|Failed!|Total tests|Tổng số|Đạt!|Không đạt!)' } | ForEach-Object { $_.Trim() })
        if ($t.Code -ne 0) {
            $failed = @($t.Output -split "`r?`n" | Where-Object { $_ -match '^\s*(Failed|Không đạt)\s+\S' } | Select-Object -First 10 | ForEach-Object { $_.Trim() })
            [void]$fails.Add("Test KHÔNG ĐẠT (mã $($t.Code)):`n  " + (($failed + $summary) -join "`n  "))
        } else { [void]$notes.Add("Test: OK " + ($summary -join ' ; ')) }
    }

    # 9. Đã push bản mới nhất chưa (sau khi có thể đã tự commit BOM)
    if ($dirty.Count -eq 0) {
        $local = git -C $work rev-parse HEAD
        $remote = (git -C $work ls-remote origin "refs/heads/$branch" 2>$null | Select-Object -First 1) -replace '\s.*$', ''
        if ($bomFixed.Count -gt 0 -or ($remote -and $remote -ne $local)) {
            $ahead = if ($remote) { git -C $work rev-list --count "$remote..HEAD" 2>$null } else { "?" }
            if (-not $remote -or $bomFixed.Count -gt 0) { git -C $work push -q -u origin $branch 2>$null }
            $remote = (git -C $work ls-remote origin "refs/heads/$branch" 2>$null | Select-Object -First 1) -replace '\s.*$', ''
            if ($remote -ne $local) { [void]$fails.Add("Nhánh $branch trên GitHub chưa có commit mới nhất (local đi trước $ahead commit).") }
            elseif ($bomFixed.Count -gt 0) { [void]$notes.Add("Đã push commit sửa BOM lên $branch") }
        } elseif (-not $remote) { [void]$fails.Add("Nhánh $branch CHƯA được push lên GitHub.") }
    }

    # Kết quả
    $ok = ($fails.Count -eq 0)
    New-Item -ItemType Directory -Force $r.Logs | Out-Null
    $reportPath = $null

    $reportPath = Invoke-RepoTransaction -RepoPath $r.Path -Action {
        $n = 1
        while (Test-Path (Join-Path $r.Logs "$Task-verify-$n.md")) { $n++ }
        $reportPath = Join-Path $r.Logs "$Task-verify-$n.md"
        $md = @("# Cổng kiểm tra $Task — $(if ($ok) { 'ĐẠT' } else { 'KHÔNG ĐẠT' })", "", "- Máy: $env:COMPUTERNAME · $(Get-Date -Format 'HH:mm dd/MM/yyyy')", "- Nhánh: ````$branch````", "")
        if ($fails.Count) { $md += "## Phải sửa"; $fails | ForEach-Object { $md += "- $_" }; $md += "" }
        if ($warns.Count) { $md += "## Cảnh báo (Claude xem khi review)"; $warns | ForEach-Object { $md += "- $_" }; $md += "" }
        $md += "## Đã kiểm tra"; $notes | ForEach-Object { $md += "- $_" }
        if ($buildAttempts.Count -gt 1) {
            $md += ""; $md += "## Chi tiết Build thử lại"
            foreach ($att in $buildAttempts) {
                $md += "### Lần $($att.Number) (mã $($att.Code))`r`n" + ([string]::new([char]96, 3)) + "`r`n$($att.Output)`r`n" + ([string]::new([char]96, 3))
            }
        }
        if ($testAttempts.Count -gt 1) {
            $md += ""; $md += "## Chi tiết Test thử lại"
            foreach ($att in $testAttempts) {
                $md += "### Lần $($att.Number) (mã $($att.Code))`r`n" + ([string]::new([char]96, 3)) + "`r`n$($att.Output)`r`n" + ([string]::new([char]96, 3))
            }
        }
        [IO.File]::WriteAllText($reportPath, ($md -join "`r`n"), $Utf8)
        try {
            $null = Update-WorkflowJournal -RepoPath $r.Path -TaskFile $ctx.TaskFile.Name -TaskId $Task -Event 'Verify' -Run $n -Result $(if ($ok) { 'ĐẠT' } else { 'KHÔNG ĐẠT' })
        } catch {
            $ok = $false
            [void]$fails.Add("Lỗi ghi nhật ký phiếu: $($_.Exception.Message)")
            $mdFail = @("# Cổng kiểm tra $Task — KHÔNG ĐẠT", "", "- Máy: $env:COMPUTERNAME · $(Get-Date -Format 'HH:mm dd/MM/yyyy')", "- Nhánh: ````$branch````", "", "## Phải sửa")
            $fails | ForEach-Object { $mdFail += "- $_" }
            $mdFail += ""
            if ($warns.Count) { $mdFail += "## Cảnh báo (Claude xem khi review)"; $warns | ForEach-Object { $mdFail += "- $_" }; $mdFail += "" }
            $mdFail += "## Đã kiểm tra"; $notes | ForEach-Object { $mdFail += "- $_" }
            [IO.File]::WriteAllText($reportPath, ($mdFail -join "`r`n"), $Utf8)
            throw
        }
        return $reportPath
    }

    foreach ($f in $fails) { Say "  [PHẢI SỬA] $f" Red }
    foreach ($w in $warns) { Say "  [CẢNH BÁO] $w" Yellow }
    foreach ($x in $notes) { Say "  [OK] $x" Green }
    Say ("Cổng kiểm tra: " + $(if ($ok) { "ĐẠT → Claude review nội dung" } else { "KHÔNG ĐẠT" }) + "  ($reportPath)") $(if ($ok) { "Green" } else { "Red" })
    $verifiedHead=git -C $work rev-parse HEAD
    if ($LASTEXITCODE -eq 0 -and $verifiedHead -match '^[0-9a-fA-F]{40}$' -and $verifiedHead -ceq $verifyStartHead) {
        $acceptRun=[regex]::Match([IO.Path]::GetFileName($reportPath),'-(\d+)\.md$').Groups[1].Value
        $agentRun=[regex]::Match([IO.Path]::GetFileName([string]$log),('^'+[regex]::Escape($Task)+'-(\d+)\.json$')).Groups[1].Value
        if ($agentRun) { $acceptRun=$agentRun }
        if ([int]$acceptRun -gt 0) {
            $metricSource=$null
            $metricDir=Join-Path $Root 'metrics/runs'
            $matchesMetric=@(foreach ($metricFile in Get-ChildItem -LiteralPath $metricDir -File -ErrorAction SilentlyContinue) {
                try {
                    for ($node=$metricFile.FullName; $node; $node=[IO.Path]::GetDirectoryName($node)) {
                        if ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) { throw 'Nguồn không an toàn.' }
                    }
                    $metricValue=Read-Json $metricFile.FullName
                    if ($metricValue.project -ceq $r.Info.name -and $metricValue.task -cmatch ('^'+[regex]::Escape($Task)+'(?:-.+)?$') -and
                        [string]$metricValue.run -eq [string]$acceptRun -and $metricValue.head -ceq $verifiedHead) { $metricFile.Name }
                } catch { continue } # Metrics hỏng không được dùng làm nguồn.
            })
            if ($matchesMetric.Count -eq 1) { $metricSource=@{source=$matchesMetric[0]} }
            $script:AcceptVerify=@{task=$Task;project=$r.Info.name;head=[string]$verifiedHead;run=[int]$acceptRun;
                verify=@{ok=[bool]$ok;source=[IO.Path]::GetFileName($reportPath)};qc=$null;metrics=$metricSource}
            Invoke-RepoTransaction -RepoPath $r.Path -Action {
                $index=Join-Path $r.Logs "$Task.accept-$acceptRun.json"
                for ($node=$index; $node; $node=[IO.Path]::GetDirectoryName($node)) {
                    if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Nguồn không an toàn.' }
                }
                $temp="$index.$([guid]::NewGuid().ToString('N')).tmp"
                [IO.File]::WriteAllText($temp,($script:AcceptVerify | ConvertTo-Json -Depth 16),$Utf8)
                Move-Item -LiteralPath $temp -Destination $index -Force
            }
        }
    }
    return [pscustomobject]@{ Ok = $ok; Fails = @($fails); Warns = @($warns); Report = $reportPath }
}

# ───────────────────────── Bảng việc: giữ chỗ theo máy ─────────────────────────
# Hai máy (nhà / công ty) dùng chung git → ghi "đang chạy ở máy nào" lên bảng việc trên GitHub để không giao trùng.

$ClaimPattern = 'Đang chạy · máy (?<m>[^·|]+?) · (?<t>\d{2}:\d{2} \d{2}/\d{2}/\d{4})'

function Get-BoardRow([string]$board, [string]$taskId) {
    ($board -split "`r?`n") | Where-Object { $_ -match '^\|' -and $_ -match ("(\[|\|\s*)" + [regex]::Escape($taskId) + "\b") } | Select-Object -First 1
}

function Get-BoardClaim($r, [string]$taskId) {
    $board = git -C $r.Path show origin/main:tasks/README.md 2>$null | Out-String
    $row = Get-BoardRow $board $taskId
    if (-not $row) { return $null }
    $m = [regex]::Match($row, $ClaimPattern)
    if (-not $m.Success) { return [pscustomobject]@{ Row = $row; Machine = $null; Since = $null } }
    $since = [datetime]::ParseExact($m.Groups['t'].Value, 'HH:mm dd/MM/yyyy', $null)
    [pscustomobject]@{ Row = $row; Machine = $m.Groups['m'].Value.Trim(); Since = $since }
}

# Đổi ô cuối (trạng thái) của dòng task trên bảng việc, commit riêng file bảng việc và push lên main.
# Khi có $Journal: cấp lượt và ghi nhật ký trước; chỉ commit bảng việc + phiếu của task trong cùng khóa repo.
function Set-BoardStatus($r, [string]$taskId, [string]$status, [hashtable]$Journal = $null) {
    Invoke-RepoTransaction -RepoPath $r.Path -Action {
        $ErrorActionPreference = "Stop"
        $journalTask = if ($Journal) { $Journal.TaskFile } else { (Find-TaskFile $r $taskId).Name }
        $reqText = [IO.File]::ReadAllText((Join-Path $r.Path "tasks/$journalTask"), (New-Object Text.UTF8Encoding($false, $true)))
        $reqMatch = [regex]::Match($reqText, '\*\*Yêu cầu:\*\*\s*(R-\d+)')
        $reqRel = if ($reqMatch.Success) { "requests/$($reqMatch.Groups[1].Value).md" } else { $null }
        if ($reqRel -and -not (Test-Path -LiteralPath (Join-Path $r.Path $reqRel))) {
            $requestFiles=@(Get-ChildItem -LiteralPath (Join-Path $r.Path 'requests') -File | Where-Object { $_.Name -match ('^'+[regex]::Escape($reqMatch.Groups[1].Value)+'-.+\.md$') })
            if ($requestFiles.Count -eq 1) { $reqRel='requests/'+$requestFiles[0].Name }
        }
        if ($Journal) {
            $jRun = if ($Journal.ContainsKey('Run')) { [int]$Journal.Run } else { Get-NextJournalRun $r.Path $Journal.TaskFile $taskId $Journal.Event }
            Update-WorkflowJournal -RepoPath $r.Path -TaskFile $Journal.TaskFile -TaskId $taskId -Event $Journal.Event -Run $jRun -Result $Journal.Result
        }
        $ErrorActionPreference = "Continue"
        $boardRel = "tasks/README.md"
        $board = Join-Path $r.Path $boardRel
        if (-not (Test-Path $board)) { return }
        foreach ($try in 1..2) {
            git -C $r.Path fetch origin --quiet 2>$null
            $branchNow = git -C $r.Path rev-parse --abbrev-ref HEAD 2>$null
            $dirtyBoard = git -C $r.Path status --porcelain -- $boardRel 2>$null
            if ($branchNow -ne "main" -or $dirtyBoard) { Say "  (Bỏ qua cập nhật bảng việc: repo chính không ở main hoặc bảng việc đang sửa dở.)" Yellow; return }
            git -C $r.Path merge --ff-only origin/main --quiet 2>$null
            $text = [IO.File]::ReadAllText($board)
            $lines = $text -split "`r?`n"
            $idx = -1
            for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\|' -and $lines[$i] -match ("(\[|\(|\|\s*)" + [regex]::Escape($taskId) + "\b")) { $idx = $i; break } }
            $added = $idx -lt 0
            if ($added) {
                $taskTitle = [regex]::Match($reqText, '(?m)^#\s+(.+?)\r?$').Groups[1].Value
                $taskTitle = ($taskTitle -replace ('^' + [regex]::Escape($taskId) + '\b\s*'), '').Trim()
                $agentId = [regex]::Match($reqText, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
                $taskBranch = [regex]::Match($reqText, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
                $idx = 0
                for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\|') { $idx = $i + 1 } }
                $list = New-Object 'System.Collections.Generic.List[string]'
                $list.AddRange([string[]]$lines)
                $list.Insert($idx, ('| [{0} {1}]({2}) | {3} | `{4}` | {5} |' -f $taskId, $taskTitle, $journalTask, $agentId, $taskBranch, $status))
                $lines = $list.ToArray()
            }
            $cells = $lines[$idx].TrimEnd().TrimEnd('|') -split '\|'
            $cells[$cells.Count - 1] = " $status "
            $newLine = ($cells -join '|') + '|'
            $boardChanged = $added -or ($newLine -ne $lines[$idx])
            if ($boardChanged) {
                $lines[$idx] = $newLine
                $nl = if ($text -match "`r`n") { "`r`n" } else { "`n" }
                $bom = Test-HasBom ([IO.File]::ReadAllBytes($board))
                [IO.File]::WriteAllText($board, ($lines -join $nl), (New-Object Text.UTF8Encoding $bom))
            }
            $reqModified = if ($reqRel -and (git -C $r.Path status --porcelain -- $reqRel 2>$null)) { @($reqRel) } else { @() }
            $commitFiles = @()
            if ($boardChanged -or (git -C $r.Path status --porcelain -- $boardRel 2>$null)) { $commitFiles += $boardRel }
            if ($reqModified) { $commitFiles += $reqModified }

            if ($commitFiles.Count -eq 0) { return }

            $boardGitErrorActionPreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            git -C $r.Path add -- $commitFiles 2>$null
            if ($LASTEXITCODE -ne 0) { throw 'Không cập nhật được bảng việc.' }
            git -C $r.Path commit -q -m "Bảng việc và nhật ký: $taskId $status" -- $commitFiles 2>$null
            $ErrorActionPreference = $boardGitErrorActionPreference
            if ($LASTEXITCODE -ne 0) { throw 'Không cập nhật được bảng việc.' }
            git -C $r.Path push -q origin main 2>$null
            if ($LASTEXITCODE -eq 0) { return }

            # Bị máy khác push trước → pull --rebase để tích hợp, tuyệt đối KHÔNG reset --hard làm mất thay đổi
            Say "  Push bảng việc gặp xung đột → thử pull --rebase origin main..." Yellow
            git -C $r.Path pull --rebase -q origin main 2>$null
            if ($LASTEXITCODE -eq 0) {
                git -C $r.Path push -q origin main 2>$null
                if ($LASTEXITCODE -eq 0) { return }
            } else {
                git -C $r.Path rebase --abort 2>$null
            }
        }
        throw 'Không push được bảng việc.'
    }
}

function Get-ClaimText { "🟡 Đang chạy · máy $env:COMPUTERNAME · $(Get-Date -Format 'HH:mm dd/MM/yyyy')" }

# Trả lỗi nếu task đang chạy ở MÁY KHÁC trong vòng 3 giờ (trừ khi -Force).
function Assert-NotClaimedElsewhere($r, [string]$taskId) {
    git -C $r.Path fetch origin --quiet 2>$null
    $c = Get-BoardClaim $r $taskId
    if (-not $c -or -not $c.Machine) { return }
    if ($c.Machine -eq $env:COMPUTERNAME) { return }
    $age = (Get-Date) - $c.Since
    if ($age.TotalHours -lt 3 -and -not $Force) {
        throw "Task $taskId đang chạy trên máy $($c.Machine) từ $($c.Since.ToString('HH:mm dd/MM')) ($([int]$age.TotalMinutes) phút trước). Nếu chắc máy kia đã dừng: thêm -Force."
    }
}

# ───────────────────────── Số liệu hiệu quả ─────────────────────────

# ===== Số đo vận hành: mỗi lượt chạy agent ghi 1 file JSON vào metrics/runs (lên git ⇒ cộng dồn mọi máy) =====
function Get-RunRecord($r, [string]$TaskName, [string]$logFile, [string]$agentId, [string]$model, [string]$work, $verify, [bool]$autoRetry) {
    $j = $null
    try { $j = Get-Content $logFile -Raw -Encoding UTF8 | ConvertFrom-Json } catch { }
    $added = 0; $removed = 0; $files = 0
    if ($work -and (Test-Path $work)) {
        $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
        $base = git -C $work merge-base origin/main HEAD 2>$null | Select-Object -First 1
        if ($base) {
            # Chỉ đếm dòng agent viết: bỏ khung + test nghiệm thu của Claude (commit "Test nghiệm thu…") — trước đây V4-07 báo 512/230, thật 176.
            $seed = git -C $work log --format=%H --grep="^Test nghiệm thu" "$base..HEAD" 2>$null | Select-Object -Last 1
            $from = if ($seed) { $seed } else { $base }
            foreach ($line in (git -C $work diff --numstat $from HEAD 2>$null)) {
                $p = $line -split "`t"
                if ($p.Count -ge 3 -and $p[2] -notmatch '^tasks/' -and $p[0] -match '^\d+$') { $added += [int]$p[0]; $removed += [int]$p[1]; $files++ }
            }
        }
        $ErrorActionPreference = $eap
    }
    $u = if ($j) { $j.usage } else { $null }
    $tokensKnown = $u -and $null -ne $u.input_tokens -and $null -ne $u.output_tokens -and $u.input_tokens -ge 0 -and $u.output_tokens -ge 0 -and $j.tokensKnown -ne $false
    $taskText = try { Get-Content (Find-TaskFile $r $TaskName).FullName -Raw -Encoding UTF8 } catch { "" }
    $maxText = [regex]::Match($taskText, '\*\*Thời gian tối đa:\*\*\s*(\d+)').Groups[1].Value
    $failureLog = if ($verify -and $verify.Report -and (Test-Path -LiteralPath $verify.Report) -and (Get-Item -LiteralPath $verify.Report).Length -le 4194304) { [IO.File]::ReadAllText($verify.Report) } else { "" }
    [ordered]@{
        at = (Get-Item $logFile).LastWriteTime.ToString("s")
        head = $(if ($work -and (Test-Path $work)) { git -C $work rev-parse HEAD 2>$null } else { $null })
        machine = $env:COMPUTERNAME
        project = $r.Info.name
        task = $TaskName
        run = [regex]::Match([IO.Path]::GetFileNameWithoutExtension($logFile), '-(\d+)$').Groups[1].Value
        agent = $agentId
        model = $(if ($model) { $model } else { "mặc định" })
        findings = @(Get-ReviewFindings (Get-Section $taskText "Review (Claude)"))
        status = $(if ($j) { $j.status } else { "KHÔNG CÓ KẾT QUẢ" })
        durationSec = $(if ($j) { [int]$j.duration_seconds } else { $null })
        tokensKnown = [bool]$tokensKnown
        inputTokens = $(if ($tokensKnown) { [long]$u.input_tokens } else { $null })
        outputTokens = $(if ($tokensKnown) { [long]$u.output_tokens } else { $null })
        thinkingTokens = $(if ($tokensKnown -and $null -ne $u.thinking_tokens) { [long]$u.thinking_tokens } else { $null })
        totalTokens = $(if ($tokensKnown) { if ($null -ne $u.total_tokens -and $u.total_tokens -ge 0) { [long]$u.total_tokens } else { [long]$u.input_tokens + [long]$u.output_tokens } } else { $null })
        maxMinutes = $(if ($maxText -and [int]$maxText -gt 0) { [int]$maxText } else { 30 })
        difficulty = [regex]::Match($taskText, '\*\*Độ khó:\*\*\s*([SML])\b').Groups[1].Value
        hasOutput = [bool]($j -and -not [string]::IsNullOrWhiteSpace([string]$j.response))
        diagnosticError = $(if ($j) { [string]$j.error + " " + [string]$j.status } else { "" })
        failureLog = $failureLog
        linesAdded = $added; linesRemoved = $removed; filesChanged = $files
        verifyOk = $(if ($verify) { [bool]$verify.Ok } else { $null })
        verifyFails = $(if ($verify) { @($verify.Fails).Count } else { $null })
        autoRetry = $autoRetry
        hungBackgroundTask = $(try { [bool]((Get-Content ($logFile -replace '\.json$', '.stderr.txt') -Raw -ErrorAction Stop) -match 'waiting up to \d+m\d+s for \d+ background task') } catch { $false })
        log = [IO.Path]::GetFileName($logFile)
        conversationId = $(if ($j) { [string]$j.conversation_id } else { "" })
        request = $(try { [regex]::Match((Get-Content (Find-TaskFile $r $TaskName).FullName -Raw -Encoding UTF8), '\*\*Yêu cầu:\*\*\s*(R-\d+)').Groups[1].Value } catch { "" })
    }
}

# V9-13: phân tích bằng máy, không gọi AI; dữ liệu lỗi thô chỉ tồn tại trong file tạm.
function Add-RunFlags($record, [string]$history) {
    $temp = [IO.Path]::GetTempFileName()
    $savedExit = $global:LASTEXITCODE
    try {
        [IO.File]::WriteAllText($temp, ($record | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
        $exe = Get-AITeamExe
        if (-not $exe) { throw 'Chưa có CLI phân tích lượt.' }
        $text = & $exe diagnose-run --input $temp --history $history
        if ($LASTEXITCODE -ne 0) { throw 'Phân tích lượt thất bại.' }
        $result = ($text -join "`n") | ConvertFrom-Json
        $record.flags = @($result.flags)
        $record.pause = [bool]$result.pause
        $record.failureFingerprint = $result.failureFingerprint
        $record.failureFingerprints = @($result.failureFingerprints)
    } finally {
        $record.Remove('diagnosticError'); $record.Remove('failureLog')
        Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
        $global:LASTEXITCODE = $savedExit
    }
}

function Add-RunFlagsJournal($record) {
    if (-not $record.flags -or $record.request -notmatch '\AR-[0-9]{3,4}\z') { return }
    $folder = Join-Path $Root 'requests'
    $files = @(Get-ChildItem -LiteralPath $folder -Filter "$($record.request)*.md" -ErrorAction SilentlyContinue | Where-Object { $_.BaseName -eq $record.request -or $_.BaseName.StartsWith("$($record.request)-",[StringComparison]::Ordinal) })
    if ($files.Count -ne 1) { return }
    $file = $files[0].FullName
    for ($node=$file; $node; $node=[IO.Path]::GetDirectoryName($node)) {
        if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Nguồn nhật ký không an toàn.' }
    }
    $body = [IO.File]::ReadAllText($file)
    $stamp = "Cờ tự động $($record.project)/$($record.task) lượt $($record.run)"
    if ($body.Contains($stamp)) { return }
    $heading = [regex]::Match($body, '(?m)^##\s+3[.)]?\s+Nhật ký[^\r\n]*\r?\n')
    if (-not $heading.Success) { return }
    $start = $heading.Index + $heading.Length
    $next = [regex]::Match($body.Substring($start), '(?m)^##\s+')
    $at = if ($next.Success) { $start + $next.Index } else { $body.Length }
    $nl = if ($body.Contains("`r`n")) { "`r`n" } else { "`n" }
    $line = "| $(Get-Date -Format 'HH:mm dd/MM/yyyy') | $stamp | $(@($record.flags) -join ', ')$(if ($record.pause) { ' — có thể test nghiệm thu sai; Tạm dừng' }) |$nl"
    [IO.File]::WriteAllText($file, $body.Insert($at,$line), (New-Object Text.UTF8Encoding (Test-HasBom ([IO.File]::ReadAllBytes($file)))))
}

function Save-RunMetrics($record) {
    Invoke-RepoTransaction -RepoPath $Root -Action {
        $dir = Join-Path $Root "metrics\runs"
        New-Item -ItemType Directory -Force $dir | Out-Null
        $name = "{0}-{1}-{2}-{3}.json" -f ($record.at -replace '[^0-9]', ''), $record.machine, $record.project, ($record.log -replace '\.json$', '')
        $file = Join-Path $dir $name
        Add-RunFlags $record $dir
        Add-RunFlagsJournal $record
        [IO.File]::WriteAllText($file, ($record | ConvertTo-Json -Depth 8), $Utf8)
        if ($script:AcceptVerify -and $record.task -ceq $script:AcceptVerify.task -and $record.head -ceq $script:AcceptVerify.head -and [string]$record.run -eq [string]$script:AcceptVerify.run) {
            $metricRepo=Resolve-Project $record.project
            Invoke-RepoTransaction -RepoPath $metricRepo.Path -Action {
                $index=Join-Path $metricRepo.Logs "$($record.task).accept-$($record.run).json"
                for ($node=$index; $node; $node=[IO.Path]::GetDirectoryName($node)) {
                    if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Nguồn không an toàn.' }
                }
                if (Test-Path -LiteralPath $index) {
                    $envelope=Read-Json $index
                    if ($envelope.head -ceq $record.head) {
                        $envelope.metrics=@{source=$name}
                        $temp="$index.$([guid]::NewGuid().ToString('N')).tmp"
                        [IO.File]::WriteAllText($temp,($envelope | ConvertTo-Json -Depth 16),$Utf8)
                        Move-Item -LiteralPath $temp -Destination $index -Force
                    }
                }
            }
        }
        $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
        for ($i = 0; $i -lt 3; $i++) {
            git -C $Root add -- "metrics/runs/$name" 2>$null
            git -C $Root commit -q -m "Số đo: $($record.project) $($record.task) lượt $($record.run) ($($record.machine))" -- "metrics/runs/$name" 2>$null
            git -C $Root pull -q --rebase --autostash 2>$null
            git -C $Root push -q origin HEAD 2>$null
            if ($LASTEXITCODE -eq 0) { break }
            Start-Sleep -Seconds (2 + $i * 3)
        }
        $ErrorActionPreference = $eap
    }
}

# V9-17: ghi số đo cho lượt chạy trực tiếp (OpenCode/Codex) — CLI đọc log, ở đây chỉ ghép bản ghi.
function Invoke-LogRun {
    if (-not $Project) { throw "Thiếu -Project" }
    if (-not $Task) { throw "Thiếu -Task" }
    if ($Dev -notin @('opencode', 'codex')) { throw "Thiếu hoặc sai -Dev (chỉ opencode | codex)." }
    if (-not $Log) { throw "Thiếu -Log <file log>." }
    if (-not (Test-Path -LiteralPath $Log -PathType Leaf)) { throw "Không thấy file log: $Log" }
    $logFull = [IO.Path]::GetFullPath($Log)
    for ($node = $logFull; $node; $node = [IO.Path]::GetDirectoryName($node)) {
        if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw "File log không an toàn: $Log" }
    }

    $taskCode = [regex]::Match($Task, '^[A-Z]\d+-\d+').Value
    if (-not $taskCode) { $taskCode = $Task }
    $r = Resolve-Project $Project
    $exe = Get-AITeamExe
    if (-not $exe) { throw 'Chưa có CLI đọc log lượt chạy.' }
    $lines = @(& $exe parse-run-log --dev $Dev --log $logFull)
    if ($LASTEXITCODE -ne 0) { throw "Không đọc được log lượt chạy: $Log" }
    $parsed = ($lines -join "`n") | ConvertFrom-Json

    # Khóa chống ghi trùng: cùng dự án + task + máy + đúng file log (tên và kích thước).
    $source = '{0}:{1}' -f [IO.Path]::GetFileName($logFull), (Get-Item -LiteralPath $logFull).Length
    $runsDir = Join-Path $Root 'metrics\runs'
    $maxRun = 0
    foreach ($f in Get-ChildItem -LiteralPath $runsDir -Filter *.json -File -ErrorAction SilentlyContinue) {
        try { $x = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
        if ($x.project -ne $r.Info.name -or $x.task -ne $taskCode) { continue }
        if ($x.machine -eq $env:COMPUTERNAME -and [string]$x.sourceLog -eq $source) { Say "Đã ghi lượt này" Yellow; return }
        $n = 0
        if ([int]::TryParse([string]$x.run, [ref]$n) -and $n -gt $maxRun) { $maxRun = $n }
    }
    $run = $maxRun + 1

    $taskText = try { Get-Content (Find-TaskFile $r $taskCode).FullName -Raw -Encoding UTF8 } catch { "" }
    $maxText = [regex]::Match($taskText, '\*\*Thời gian tối đa:\*\*\s*(\d+)').Groups[1].Value
    $added = 0; $removed = 0; $files = 0
    if ($Path -and (Test-Path -LiteralPath $Path)) {
        $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
        $base = git -C $Path merge-base origin/main HEAD 2>$null | Select-Object -First 1
        if ($base) {
            $seed = git -C $Path log --format=%H --grep="^Test nghiệm thu" "$base..HEAD" 2>$null | Select-Object -Last 1
            $from = if ($seed) { $seed } else { $base }
            foreach ($line in (git -C $Path diff --numstat $from HEAD 2>$null)) {
                $p = $line -split "`t"
                if ($p.Count -ge 3 -and $p[2] -notmatch '^tasks/' -and $p[0] -match '^\d+$') { $added += [int]$p[0]; $removed += [int]$p[1]; $files++ }
            }
        }
        $ErrorActionPreference = $eap
    }

    $record = [ordered]@{
        at = [string]$parsed.at
        head = $(if ($Path -and (Test-Path -LiteralPath $Path)) { git -C $Path rev-parse HEAD 2>$null } else { $null })
        machine = $env:COMPUTERNAME
        project = $r.Info.name
        task = $taskCode
        run = [string]$run
        agent = $(if ($Dev -eq 'opencode') { 'O' } else { 'C' })
        model = $(if ($Dev -eq 'opencode') { 'opencode-deepseek' } else { 'codex' })
        findings = @(Get-ReviewFindings (Get-Section $taskText "Review (Claude)"))
        status = [string]$parsed.status
        durationSec = [int]$parsed.durationSec
        tokensKnown = [bool]$parsed.tokensKnown
        inputTokens = $(if ($null -eq $parsed.inputTokens) { $null } else { [long]$parsed.inputTokens })
        outputTokens = $(if ($null -eq $parsed.outputTokens) { $null } else { [long]$parsed.outputTokens })
        thinkingTokens = $null
        totalTokens = $(if ($null -eq $parsed.totalTokens) { $null } else { [long]$parsed.totalTokens })
        maxMinutes = $(if ($maxText -and [int]$maxText -gt 0) { [int]$maxText } else { 30 })
        difficulty = [regex]::Match($taskText, '\*\*Độ khó:\*\*\s*([SML])\b').Groups[1].Value
        hasOutput = [bool]$parsed.hasOutput
        linesAdded = $added
        linesRemoved = $removed
        filesChanged = $files
        verifyOk = $null
        verifyFails = $null
        autoRetry = $false
        hungBackgroundTask = $false
        log = "$taskCode-$run.json"
        conversationId = ""
        request = [regex]::Match($taskText, '\*\*Yêu cầu:\*\*\s*(R-\d+)').Groups[1].Value
        sourceLog = $source
    }
    Save-RunMetrics $record
    $minutes = [math]::Round(([int]$parsed.durationSec) / 60, 1)
    $tokenText = if ($parsed.tokensKnown) { "$($parsed.totalTokens) token" } else { "không rõ token" }
    Say ("Đã ghi số đo {0}/{1} lượt {2}: {3} phút · {4}" -f $r.Info.name, $taskCode, $run, $minutes, $tokenText) Green
}

# Nhập các log agent cũ trên máy này (chưa có số đo) — chạy 1 lần mỗi máy.
function Invoke-MetricsImport {
    $have = @(Get-ChildItem (Join-Path $Root "metrics\runs") -Filter *.json -ErrorAction SilentlyContinue | ForEach-Object {
        try { $x = Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json; "$($x.machine)|$($x.project)|$($x.log)" } catch { } })
    $n = 0
    foreach ($p in Get-Projects) {
        $r = Resolve-Project $p.name
        foreach ($lf in Get-ChildItem $r.Logs -Filter *.json -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'prompt' -and $_.Length -gt 0 }) {
            if ($have -contains "$env:COMPUTERNAME|$($p.name)|$($lf.Name)") { continue }
            $taskName = $lf.BaseName -replace '-\d+$', ''
            $rec = Get-RunRecord $r $taskName $lf.FullName "" "" "" $null $false
            Save-RunMetrics $rec; $n++
        }
    }
    Say "Đã nhập $n lượt chạy cũ của máy $env:COMPUTERNAME vào metrics/runs." Green
}

function Show-RunMetrics {
    $runs = @(Get-ChildItem (Join-Path $Root "metrics\runs") -Filter *.json -ErrorAction SilentlyContinue | ForEach-Object {
        try { Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { } } |
        Where-Object { (-not $Project -or $_.project -eq $Project) -and $_.durationSec })
    if (-not $runs) { Say "Chưa có số đo lượt chạy (metrics/runs). Chạy .\team.ps1 metrics-import trên mỗi máy." Yellow; return }
    $med = { param($xs) $s = @($xs | Sort-Object); if (-not $s) { return 0 }; $s[[int][math]::Floor(($s.Count - 1) / 2)] }
    $groups = $runs | Group-Object { ([datetime]$_.at).ToString("yyyy-MM-dd") } | Sort-Object Name
    Say "== Hiệu suất lượt chạy agent ($($runs.Count) lượt, mọi máy) ==" Cyan
    Say ("  {0,-10} {1,5} {2,10} {3,12} {4,10} {5,12} {6,10}" -f "Ngày", "Lượt", "TB phút", "Trung vị ph", "TB token", "Dòng/lượt", "Dòng/phút")
    foreach ($g in $groups) {
        $d = @($g.Group | ForEach-Object { $_.durationSec / 60 })
        $lines = @($g.Group | Where-Object { $_.linesAdded -gt 0 } | ForEach-Object { $_.linesAdded })
        $lpm = if ($lines) { ($lines | Measure-Object -Sum).Sum / (@($g.Group | Where-Object { $_.linesAdded -gt 0 } | ForEach-Object { $_.durationSec / 60 }) | Measure-Object -Sum).Sum } else { 0 }
        Say ("  {0,-10} {1,5} {2,10:N1} {3,12:N1} {4,10:N0} {5,12} {6,10}" -f $g.Name, $g.Count, ($d | Measure-Object -Average).Average, (& $med $d),
            ($g.Group | Measure-Object totalTokens -Average).Average, $(if ($lines) { "{0:N0}" -f ($lines | Measure-Object -Average).Average } else { "-" }), $(if ($lpm) { "{0:N0}" -f $lpm } else { "-" }))
    }
    $byModel = $runs | Group-Object model
    if ($byModel.Count -gt 1) {
        Say "  Theo model:" Cyan
        foreach ($m in $byModel) { Say ("    {0,-28} {1,4} lượt · TB {2:N1} phút" -f $m.Name, $m.Count, ($m.Group | ForEach-Object { $_.durationSec / 60 } | Measure-Object -Average).Average) }
    }
    $withVerify = @($runs | Where-Object { $null -ne $_.verifyOk })
    if ($withVerify) {
        Say ("  Cổng kiểm tra đạt ngay: {0:P0} ({1} lượt có kiểm tra) · tự giao lại: {2}" -f (@($withVerify | Where-Object { $_.verifyOk }).Count / $withVerify.Count), $withVerify.Count, @($runs | Where-Object { $_.autoRetry }).Count)
    }
    $perTask = $runs | Group-Object { "$($_.project)/$($_.task)" }
    Say ("  Thời gian agent cho 1 task (cộng mọi lượt): TB {0:N0} phút · lượt / task TB {1:N2}" -f (($perTask | ForEach-Object { ($_.Group | Measure-Object durationSec -Sum).Sum / 60 }) | Measure-Object -Average).Average, (($perTask | ForEach-Object { $_.Count }) | Measure-Object -Average).Average)
}
# Các lệnh assign đang chạy của 1 dự án trên máy này (V9-15 dùng để đếm tải và dừng).
function Get-AssignProcesses($r) {
    $name = [regex]::Escape([string]$r.Info.name)
    @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $cl = $_.CommandLine
        $cl -and $_.Name -match '^powershell|^pwsh' -and $cl -match 'team\.ps1' -and $cl -match '\bassign\b' -and
        $cl -match ('-Project\s+"?' + $name + '\b')
    })
}

function Stop-TaskProcesses([string]$taskId) {
    $procs = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $cl = $_.CommandLine
        $cl -and (
            ($_.Name -match '^powershell|^pwsh' -and $cl -match 'team\.ps1' -and $cl -match '\bassign\b' -and $cl -match ("-Task\s+" + [regex]::Escape($taskId) + "\b")) -or
            ($_.Name -match '^agy' -and $cl -match ([regex]::Escape($taskId) + '-\d+\.prompt\.md'))
        ) })
    if (-not $procs) { Say "Không thấy tiến trình nào của task $taskId trên máy này." Yellow; return }
    foreach ($p in $procs | Sort-Object { $_.Name -notmatch '^powershell|^pwsh' }) {   # dừng lệnh assign trước để nó không tự giao lại
        Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
        Say ("Đã dừng {0} (PID {1})" -f $p.Name, $p.ProcessId) Yellow
    }
}

# Dừng hẳn 1 task (mặc định) hoặc dừng vòng auto và mọi assign nó sinh ra (-All).
function Invoke-Stop {
    if ($All) {
        if (-not $Project) { throw 'Thiếu -Project khi dùng stop -All.' }
        $r = Resolve-Project $Project
        $stateDir = Join-Path $Root '.state'
        New-Item -ItemType Directory -Force $stateDir | Out-Null
        $stopFile = Join-Path $stateDir 'auto.stop'
        [IO.File]::WriteAllText($stopFile, (Get-Date).ToUniversalTime().ToString('o'), (New-Object Text.UTF8Encoding $false))
        Say "Đã đặt cờ dừng hàng đợi auto: $stopFile" Yellow
        $pidFile = Join-Path $stateDir 'auto.pid'
        if (Test-Path -LiteralPath $pidFile) {
            $autoPid = 0
            if ([int]::TryParse(([IO.File]::ReadAllText($pidFile)).Trim(), [ref]$autoPid) -and $autoPid -gt 0) {
                Stop-Process -Id $autoPid -Force -ErrorAction SilentlyContinue
                Say "Đã dừng vòng auto (PID $autoPid)" Yellow
            }
        }
        foreach ($p in @(Get-AssignProcesses $r)) {
            $taskId = [regex]::Match([string]$p.CommandLine, '-Task\s+([A-Za-z0-9][A-Za-z0-9-]{0,63})').Groups[1].Value
            if ($taskId) { Stop-TaskProcesses $taskId }
        }
        return
    }
    if (-not $Task) { throw "Thiếu -Task" }
    Stop-TaskProcesses $Task
}

# V9-15: giao các task "Chờ làm" đủ phụ thuộc, tối đa -Max chạy song song.
function Invoke-AutoQueue {
    if (-not $Project) { throw 'Thiếu -Project cho hàng đợi auto.' }
    if ($Max -lt 1) { throw '-Max phải từ 1 trở lên.' }
    $r = Resolve-Project $Project
    $exe = Get-AITeamExe
    if (-not $exe) { throw 'Không build được CLI (AITeam).' }
    $stateDir = Join-Path $Root '.state'
    New-Item -ItemType Directory -Force $stateDir | Out-Null
    $stopFile = Join-Path $stateDir 'auto.stop'
    Remove-Item -LiteralPath $stopFile -Force -ErrorAction SilentlyContinue
    $pidFile = Join-Path $stateDir 'auto.pid'
    [IO.File]::WriteAllText($pidFile, "$PID", (New-Object Text.UTF8Encoding $false))
    $lastAttempt = @{}
    $teamScript = Join-Path $Root 'team.ps1'
    Say "Hàng đợi auto: tối đa $Max task song song. Dừng: .\team.ps1 stop -All -Project $($r.Info.name)" Cyan
    try {
        while (-not (Test-Path -LiteralPath $stopFile)) {
            # Danh sách chờ giao do lõi C# (TaskQueue) quyết định — không viết lại logic bảng/phụ thuộc bằng PowerShell.
            $queueJson = & $exe queue-ready --board (Join-Path $r.Path 'tasks/README.md') --tasks (Join-Path $r.Path 'tasks')
            $queueCode = $LASTEXITCODE
            if ($queueCode -ne 0) { throw 'Không đọc được hàng đợi task (queue-ready).' }
            $ready = @((($queueJson -join "`n") | ConvertFrom-Json) | Where-Object { $_.ready })
            $running = @(Get-AssignProcesses $r)
            if (-not $ready -and $running.Count -eq 0) {
                Say 'Hàng đợi: không còn task chờ làm đủ phụ thuộc → kết thúc.' Green
                return
            }
            $now = Get-Date
            foreach ($item in $ready) {
                if ($running.Count -ge $Max) { break }
                $key = $item.Id.ToUpperInvariant()
                if ($lastAttempt.ContainsKey($key) -and ($now - $lastAttempt[$key]).TotalSeconds -lt 60) { continue }
                if ($running | Where-Object { $_.CommandLine -match ("-Task\s+" + [regex]::Escape($item.Id) + "\b") }) { continue }
                $lastAttempt[$key] = $now
                Start-Process -FilePath 'powershell.exe' -WorkingDirectory $Root -WindowStyle Hidden -ArgumentList @(
                    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $teamScript, 'assign', '-Project', $r.Info.name, '-Task', $item.Id) | Out-Null
                Say "Hàng đợi: giao $($item.Id) ($($item.FileName))" Cyan
                $running += [pscustomobject]@{ CommandLine = "-Task $($item.Id)" }
            }
            Start-Sleep -Seconds 5
        }
        Say 'Hàng đợi: đã nhận lệnh dừng.' Yellow
    } finally {
        Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
    }
}
# ===== Review / merge gọn cho Claude (tiết kiệm lượt gọi công cụ và token) =====
# /// <summary>Adapter chỉ đọc: Resolve-Project/Get-PlanDir, plan đã duyệt và bảng việc,
# /// gom tất cả task của Request đang chờ nghiệm thu; gọi Get-WorkflowAcceptEvidence/Pack.
# /// Đếm Added theo cùng seedCommit và bộ loại file của Invoke-Verify; không dùng metrics.linesAdded cũ.
# /// Không fetch, commit, ghi heartbeat hoặc log khi Command=accept-pack. Request sai báo
# /// 'Thiếu / sai -Request (vd. R-006)'. Không có task in 'Không có task chờ nghiệm thu.'.</summary>
function Invoke-AcceptPack {
    if ($Request -notmatch '^R-[0-9]{3}$') { throw 'Thiếu / sai -Request (vd. R-006)' }
    $r=Resolve-Project $(if ($Project) {$Project} else {'AITeam'})
    $plan=Read-WorkflowPipelinePlan (Join-Path (Get-PlanDir $Request) 'plan.md')
    $board=(git -C $r.Path show 'origin/main:tasks/README.md') -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Không đọc được bảng việc.' }
    $items=@()
    foreach ($planned in $plan) {
        $row=Get-BoardRow $board $planned.Id
        if (-not $row -or $row -notmatch 'Chờ (review|nghiệm thu)' -or $row -match 'Đang chạy|Đã merge') { continue }
        $taskCandidates=@(Get-ChildItem -LiteralPath (Join-Path $r.Path 'tasks') -File | Where-Object { $_.Name -match ('^'+[regex]::Escape($planned.Id)+'(?:-.+)?\.md$') })
        if ($taskCandidates.Count -ne 1) { continue }
        $tf=$taskCandidates[0]
        if (-not $tf) { continue }
        $unsafeTask=$false
        for ($node=$tf.FullName; $node; $node=[IO.Path]::GetDirectoryName($node)) {
            if ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) { $unsafeTask=$true; break }
        }
        if ($unsafeTask) { continue }
        $branch=[regex]::Match([IO.File]::ReadAllText($tf.FullName),'\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
        if (-not $branch -or $branch -match '[\s~^:?*\[\\]' -or $branch.StartsWith('-')) { continue }
        $ref="origin/$branch"
        $head=git -C $r.Path rev-parse --verify $ref 2>$null
        if ($LASTEXITCODE -ne 0) { continue }
        $text=(git -C $r.Path show "${ref}:tasks/$($tf.Name)" 2>$null) -join "`n"
        if ($LASTEXITCODE -ne 0 -or [regex]::Match($text,'\*\*Yêu cầu:\*\*\s*(R-\d+)').Groups[1].Value -cne $Request) { continue }
        $report=(Get-Section $text 'Báo cáo của agent') -replace '(?s)<!--.*?-->',''
        if ([string]::IsNullOrWhiteSpace($report)) { continue }
        git -C $r.Path merge-base --is-ancestor $ref origin/main 2>$null
        if ($LASTEXITCODE -eq 0) { continue }
        $base=git -C $r.Path merge-base origin/main $ref
        $seed=git -C $r.Path log --format=%H --grep='^Test nghiệm thu' "$base..$ref" | Select-Object -Last 1
        $from=if ($seed) {$seed} else {$base}
        $added=$null; $changes=@()
        if ($from) {
            $stat=@(git -C $r.Path diff --numstat $from $ref)
            if ($LASTEXITCODE -eq 0) { $added=Get-WorkflowAcceptLineCount $stat "tasks/$($tf.Name)" (Get-BacktickPaths (Get-Section $text 'Test nghiệm thu')) }
            $changes=@(git -C $r.Path diff --name-only $base $ref)
        }
        $budgetValue=[regex]::Match($text,'\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
        $items+=@{Id=$planned.Id;Evidence=(Get-WorkflowAcceptEvidence $planned.Id $head $r.Logs (Join-Path $Root 'metrics/runs'));Added=$added;
            Budget=$(if ($budgetValue) {[int]$budgetValue} else {$null});Changes=$changes;DependsOn=$planned.DependsOn}
    }
    Get-WorkflowAcceptPack $items
}

# /// <summary>Adapter dựng Context từ đúng nhánh/task, phát hiện đã merge từ bảng và ancestry;
# /// truyền các callback thật cho Invoke-WorkflowAccept; gọi Codex theo template codex-accept.md.
# /// Ghi envelope Task.accept-N.json liên kết source KT/QC/metrics với task/run/SHA khi tạo bằng chứng,
# /// không gắn SHA hiện tại cho log cũ. Dùng khóa chung Codex, không giữ khóa repo khi gọi AI.
# /// Chỉ commit/push ba mục tài liệu rồi gọi Invoke-Merge hiện có, giữ Assert-MergeApproval
# /// với SHA mới; không tái sử dụng approval SHA trước commit tài liệu. Reject dùng assign -Resume -Feedback.
# /// Kiểm mọi exit code git, không ghi Merge ĐẠT trước push thành công; lỗi trả qua catch CLI.</summary>
function Invoke-Accept {
    if ($Task -notmatch '^[A-Za-z0-9]+-\d+[a-z]?$') { throw 'Thiếu / sai -Task.' }
    $ctx=Get-TaskContext; $r=$ctx.R; $work=$ctx.Work; $taskRel="tasks/$($ctx.TaskFile.Name)"
    function AcceptGit {
        $output=@(& git -C $work @args)
        if ($LASTEXITCODE -ne 0) { throw 'Không hoàn tất nghiệm thu.' }
        $output
    }
    function Snapshot {
        # Kiểm cả index và worktree để phát hiện đổi mode hoặc stage ngoài tài liệu.
        $changedPaths=@(AcceptGit diff --name-only --no-renames)
        $changedPaths+=@(AcceptGit diff --cached --name-only --no-renames)
        foreach ($changedPath in $changedPaths) {
            if ($changedPath -and $changedPath -cne $taskRel) { throw 'Codex sửa ngoài mục nghiệm thu cho phép.' }
        }
        $snapshot=@{}
        foreach ($relative in (AcceptGit ls-files --cached --others --exclude-standard)) {
            if (@($relative -split '[\\/]') -contains '..' -or [IO.Path]::IsPathRooted($relative)) { throw 'Đường dẫn không an toàn.' }
            $full=Join-Path $work $relative
            for ($node=$full; $node; $node=[IO.Path]::GetDirectoryName($node)) {
                if ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint) { throw 'Đường dẫn không an toàn.' }
            }
            $bytes=[IO.File]::ReadAllBytes($full)
            $snapshot[$relative]=if ($relative -ceq $taskRel) {(New-Object Text.UTF8Encoding($false,$true)).GetString($bytes)} else {[Convert]::ToBase64String($bytes)}
        }
        return $snapshot
    }
    $board=(git -C $r.Path show 'origin/main:tasks/README.md') -join "`n"
    if ((Get-BoardRow $board $Task) -match '✅ Đã merge') { return @{Status='ALREADY_MERGED'} }
    $head=AcceptGit rev-parse HEAD
    if ((AcceptGit branch --show-current) -cne $ctx.Branch) { throw 'Sai nhánh task.' }
    $board=(git -C $r.Path show 'origin/main:tasks/README.md') -join "`n"
    $merged=(Get-BoardRow $board $Task) -match '✅ Đã merge'
    git -C $work merge-base --is-ancestor $head origin/main 2>$null
    $merged=$merged -or $LASTEXITCODE -eq 0
    if ($merged) { return @{Status='ALREADY_MERGED'} }
    if (@(AcceptGit status --porcelain).Count) { throw 'Nhánh có thay đổi chưa lưu.' }
    $files=Snapshot
    $context=[pscustomobject]@{TaskId=$Task;Head=[string]$head;TaskFile=$taskRel;Files=$files;Merged=$false}
    $pendingPath=Join-Path $r.Logs "$Task.accept-pending.json"
    for ($node=$pendingPath; $node; $node=[IO.Path]::GetDirectoryName($node)) {
        if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) { throw 'Đường dẫn không an toàn.' }
    }
    $pending=$null
    if (Test-Path -LiteralPath $pendingPath) {
        $pending=Read-Json $pendingPath
        if ($pending.head -cne $head -or $pending.optional -cne $Optional -or $pending.note -cne $Note -or $pending.reject -cne $Reject) { throw 'Không hoàn tất nghiệm thu.' }
        # Tài liệu đã commit: tiếp tục publish/approval, không thêm vòng hay gán SHA mới cho bằng chứng cũ.
        try {
            $null=AcceptGit push -u origin $ctx.Branch
            if ($Reject) {
                $script:Resume=$true; $script:Feedback=$Reject; $null=Invoke-Assign
                $status='REJECTED'
            } else {
                $approval=Assert-MergeApproval (Get-Config) $Task $head
                if (-not $approval -or $approval.commit -cne $head -or $approval.status -ne 'approved') { throw 'Chưa có phê duyệt.' }
                $null=Invoke-Merge; $status='MERGED'
            }
            Remove-Item -LiteralPath $pendingPath
            return @{Status=$status;Head=$head}
        } catch { throw 'Không hoàn tất nghiệm thu.' }
    }
    $result=Invoke-WorkflowAccept -Context $context -Optional $Optional -Note $Note -Reject $Reject -ReadEvidence {
        param($id,$sha)
        Get-WorkflowAcceptEvidence $id $sha $r.Logs (Join-Path $Root 'metrics/runs')
    } -WriteReview {
        param($decision)
        $exe=Get-AITeamExe
        if (-not $exe) { throw 'Không gọi được Codex.' }
        $prefix=Join-Path $r.Logs ("$Task.accept-input-"+[guid]::NewGuid().ToString('N'))
        $dataFile="$prefix.json"; $promptFile="$prefix.md"
        [IO.File]::WriteAllText($dataFile,($decision | ConvertTo-Json -Depth 16),$Utf8)
        $template=[IO.File]::ReadAllText((Join-Path $Root 'templates/codex-accept.md'))
        [IO.File]::WriteAllText($promptFile,($template+"`r`nTask: $taskRel`r`nDữ liệu: $dataFile"),$Utf8)
        foreach ($argumentPath in @($work,$promptFile,$prefix,$Root)) {
            if ($argumentPath -match '["\r\n]') { throw 'Đường dẫn không an toàn.' }
        }
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=$exe; $psi.WorkingDirectory=$work; $psi.UseShellExecute=$false; $psi.CreateNoWindow=$true
        $psi.RedirectStandardOutput=$true; $psi.RedirectStandardError=$true
        $psi.Arguments='run --provider codex --workdir "'+$work+'" --prompt-file "'+$promptFile+'" --logdir "'+$prefix+'.logs" --timeout-min 25 --out "'+$prefix+'.result.json" --root "'+$Root+'"'
        $process=[Diagnostics.Process]::Start($psi)
        try {
            $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(1500000)) { & taskkill.exe /PID $process.Id /T /F | Out-Null; throw 'Codex quá thời hạn.' }
            $null=$stdout.GetAwaiter().GetResult(); $err=$stderr.GetAwaiter().GetResult()
            if ($process.ExitCode -ne 0) { throw 'Codex không hoàn tất nghiệm thu.' }
        } finally { $process.Dispose() }
        return (Snapshot)
    } -Publish {
        param($updated)
        Invoke-RepoTransaction -RepoPath $r.Path -Action {
            if ((AcceptGit rev-parse HEAD) -cne $head) { throw 'Chưa đủ bằng chứng nghiệm thu.' }
            $current=Snapshot
            $null=Test-WorkflowAcceptDocuments $files $current $taskRel
            if ($current[$taskRel] -cne $updated[$taskRel]) { throw 'Codex sửa ngoài mục nghiệm thu cho phép.' }
            if (-not $pending) {
                $null=AcceptGit add -- $taskRel
                $null=AcceptGit commit -m "Nghiệm thu $Task" -- $taskRel
                $newHead=AcceptGit rev-parse HEAD
                $state=@{head=[string]$newHead;optional=$Optional;note=$Note;reject=$Reject}
                [IO.File]::WriteAllText($pendingPath,($state | ConvertTo-Json -Depth 16),$Utf8)
            } else { $newHead=$head }
            $null=AcceptGit push -u origin $ctx.Branch
            return @{Ok=$true;Head=[string]$newHead}
        }
    } -CheckApproval {
        param($sha)
        try { $approval=Assert-MergeApproval (Get-Config) $Task $sha; return [bool]($approval -and $approval.commit -ceq $sha -and $approval.status -eq 'approved') } catch { return $false }
    } -Merge {
        param($sha)
        if ((AcceptGit rev-parse HEAD) -cne $sha) { return @{Ok=$false} }
        $null=Invoke-Merge
        if (Test-Path -LiteralPath $pendingPath) { Remove-Item -LiteralPath $pendingPath }
        return @{Ok=$true}
    } -ResumeTask {
        param($resumeRun,$reason)
        $script:Resume=$resumeRun; $script:Feedback=$reason
        Invoke-Assign
        if (Test-Path -LiteralPath $pendingPath) { Remove-Item -LiteralPath $pendingPath }
    }
    $result
}

function Invoke-ReviewPack {
    $r = Resolve-Project $Project
    $tf = Find-TaskFile $r $Task
    $text = Get-Content $tf.FullName -Raw -Encoding UTF8
    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $r.Path fetch -q origin 2>$null
    Say "== $($tf.BaseName) · $branch ==" Cyan
    $v = Get-ChildItem $r.Logs -Filter "$Task*-verify-*.md" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if ($v) { Say "-- Cổng kiểm tra ($($v.Name)):" Cyan; Get-Content $v.FullName -Encoding UTF8 | Where-Object { $_ -match '^\s*-|^#' } | Select-Object -First 14 | ForEach-Object { Say $_ } }
    Say "-- Thay đổi:" Cyan
    git -C $r.Path diff --stat "origin/main...origin/$branch" 2>$null | ForEach-Object { Say $_ }
    Say "-- Báo cáo agent (rút gọn):" Cyan
    (Get-Section ((git -C $r.Path show "origin/${branch}:tasks/$($tf.Name)" 2>$null) -join "`n") "Báo cáo của agent") -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 15 | ForEach-Object { Say $_ }
    $shots = @(git -C $r.Path diff --name-only "origin/main...origin/$branch" 2>$null | Where-Object { $_ -match '\.png$' })
    if ($shots) { Say ("-- Ảnh: " + ($shots -join ', ')) Cyan }
    # S1-07: nghiệm thu tự kèm skill 10-nghiem-thu + skill stack (helper S1-04), quản lý không tự ghép đường dẫn.
    $skillLines = Add-SkillPrompt -Prompt '' -Root $Root -Stage accept -Stack $r.Info.stack
    if ($skillLines) { Say "-- Skill nghiệm thu:" Cyan; $skillLines -split "`n" | ForEach-Object { Say $_ } }
    $ErrorActionPreference = $eap
}

# ===== Duyệt trên Hub (R-003): approvals/A-xxx.json. Hub chỉ ghi status / decidedAt / note; team.ps1 làm việc thật =====
function Get-TaskId($tf) { if ($tf.BaseName -match '^([A-Za-z0-9]+-\d+)') { $Matches[1] } else { $tf.BaseName } }

function Get-Approvals {
    $dir = Join-Path $Root "approvals"
    if (-not (Test-Path $dir)) { return @() }
    @(Get-ChildItem $dir -Filter 'A-*.json' | Where-Object { $_.BaseName -match '^A-\d{3}$' } | ForEach-Object {
        try { $a = Read-Json $_.FullName; if ($a.id -eq $_.BaseName) { $a } } catch { Say "Bỏ qua $($_.Name): JSON hỏng" Yellow } })
}

# V10-03: file nhạy cảm trong diff ⇒ cổng KT sinh mục duyệt bảo mật (R-014 mục 4.2).
# Mẫu mặc định; ghi đè bằng team.json "approvals": { "sensitivePatterns": [ "...regex..." ] }.
function Get-SensitiveChanges([string[]]$Files, $cfg) {
    $patterns = @(
        '(?i)(^|/)(auth|secret|vault|credential|password|crypto|token)[^/]*\.(cs|ps1|cshtml|json)$',
        '(?i)(^|/)\.env',
        '(?i)(^|/)Migrations/',
        '(?i)SecretRedactor\.cs$',
        '(?i)tools/secret-scan\.ps1$',
        '(?i)(^|/)vault'
    )
    if ($cfg) {
        try { $custom = $cfg.approvals.sensitivePatterns } catch { $custom = $null }
        if ($custom) { $patterns = @($custom) }
    }
    $result = New-Object Collections.ArrayList
    foreach ($file in @($Files)) {
        if (-not $file) { continue }
        $name = ([string]$file).Replace('\', '/')
        foreach ($pattern in $patterns) {
            try {
                if ($name -match $pattern) { [void]$result.Add([string]$file); break }
            } catch { }
        }
    }
    return @($result)
}

function ConvertTo-ApprovalKindJson([string]$value) {
    switch (([string]$value).ToLowerInvariant()) {
        'sensitivedata' { 'sensitive-data' }
        default { ([string]$value).ToLowerInvariant() }
    }
}

function Save-ApprovalFile($obj) {
    $dir = Join-Path $Root "approvals"
    New-Item -ItemType Directory -Force $dir | Out-Null
    for ($node = $dir; $node; $node = [IO.Path]::GetDirectoryName($node)) {
        if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Đường dẫn duyệt không an toàn.'
        }
        if ([IO.Path]::GetPathRoot($node) -eq $node) { break }
    }
    [IO.File]::WriteAllText((Join-Path $dir "$($obj.id).json"), ($obj | ConvertTo-Json -Depth 4), $Utf8)
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $Root add -- "approvals/$($obj.id).json" 2>$null
    git -C $Root commit -q -m "Xin duyệt $($obj.id): $($obj.title)" 2>$null
    git -C $Root push -q 2>$null
    $ErrorActionPreference = $eap
    return $obj.id
}

function New-ApprovalItem {
    param(
        [string]$Kind, [string]$Title, [string]$Description = "",
        [string]$ProjectValue = "", [string]$TaskValue = "", [string]$CommitValue = "",
        [string]$RiskValue = "medium", [string]$SecretNameValue = "",
        $FilesValue = @(), $LinksValue = @()
    )
    $dir = Join-Path $Root "approvals"
    $max = 0
    if (Test-Path $dir) {
        foreach ($f in @(Get-ChildItem $dir -Filter 'A-*.json')) {
            if ($f.BaseName -match '^A-(\d{3})$') { $n = [int]$Matches[1]; if ($n -gt $max) { $max = $n } }
        }
    }
    $id = 'A-{0:000}' -f ($max + 1)
    $obj = [ordered]@{
        id = $id; kind = (ConvertTo-ApprovalKindJson $Kind); title = $Title; detail = $Description
        project = $ProjectValue
        task = $(if ($TaskValue) { $TaskValue } else { $null })
        commit = $(if ($CommitValue) { $CommitValue } else { $null })
        links = @($LinksValue); requestedBy = "Claude"; requestedAt = (Get-Date -Format s)
        status = "pending"; decidedAt = $null; note = $null; risk = $RiskValue
    }
    if ($SecretNameValue) { $obj.secretName = $SecretNameValue }
    if (@($FilesValue).Count -gt 0) { $obj.files = @($FilesValue) }
    return (Save-ApprovalFile $obj)
}

# team.ps1 approval-request -Kind merge -Project X -Task T [-Title "…"] [-Prompt "chi tiết markdown"]
# team.ps1 approval-request -Kind decision|improvement|secret|security|sensitive-data|external -Title "…" [-Prompt "…"] [-Risk low|medium|high] [-Name KHÓA]
# Kind secret bắt buộc -Name là TÊN khóa trong két (KHÔNG BAO GIỜ nhận / ghi giá trị khóa).
function Invoke-ApprovalRequest {
    $validKinds = @('merge', 'decision', 'improvement', 'secret', 'security', 'sensitive-data', 'external')
    if ($Kind -notin $validKinds) { throw "-Kind phải là merge / decision / improvement / secret / security / sensitive-data / external." }
    $riskValue = if ([string]::IsNullOrWhiteSpace($Risk)) { 'medium' } else { ([string]$Risk).ToLowerInvariant() }
    if ($riskValue -notin @('low', 'medium', 'high')) { throw "-Risk phải là low / medium / high." }
    $secretName = $null
    if ($Kind -eq 'secret') {
        if ($Name -notmatch '^[A-Z][A-Z0-9_]{1,63}$') { throw "-Kind secret bắt buộc -Name <TÊN_KHÓA> (chữ HOA, số, dấu _, 2–64 ký tự; KHÔNG nhận giá trị khóa)." }
        $secretName = $Name
    }
    $taskId = $null; $commit = $null; $links = @(); $title = $Title
    if ($Kind -eq 'merge') {
        $r = Resolve-Project $Project; $tf = Find-TaskFile $r $Task
        $text = Get-Content $tf.FullName -Raw -Encoding UTF8
        $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
        $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
        git -C $r.Path fetch -q origin 2>$null
        $commit = (git -C $r.Path rev-parse "origin/$branch" 2>$null)
        $ErrorActionPreference = $eap
        if (-not $commit) { throw "Không thấy nhánh origin/$branch." }
        $taskId = Get-TaskId $tf; $links = @("tasks/$($tf.Name)")
        if (-not $title) { $title = "Merge " + ([regex]::Match($text, '^#\s*(.+)$', 'Multiline').Groups[1].Value.Trim()) }
    } elseif (-not $title) { throw "Thiếu -Title." }
    $id = New-ApprovalItem -Kind $Kind -Title $title -Description $Prompt -ProjectValue $Project -TaskValue $taskId -CommitValue $commit -RiskValue $riskValue -SecretNameValue $secretName -LinksValue $links
    Say "Đã tạo $id ($Kind) — chủ dự án duyệt tại http://127.0.0.1:5057/Approvals" Green
}

# Nhật ký "Claude tự quyết" (R-014): approvals/claude-decisions.jsonl, CHỈ THÊM DÒNG.
function Add-ClaudeDecision {
    param([string]$Kind = "", [string]$Title = "", [string]$Task = "", [string]$Request = "", [string]$Detail = "")
    if (-not $Title) { throw "Thiếu -Title." }
    $id = 'D-' + (Get-Date -Format 'yyyyMMddHHmmss') + '-' + ([guid]::NewGuid().ToString('N').Substring(0, 4))
    $obj = [ordered]@{
        type = "decision"; id = $id; at = (Get-Date).ToString('o'); kind = ([string]$Kind).ToLowerInvariant(); title = $Title
        project = $(if ($Project) { $Project } else { $null })
        task = $(if ($Task) { $Task } else { $null })
        request = $(if ($Request) { $Request } else { $null })
        detail = $(if ($Detail) { $Detail } else { $null })
    }
    $dir = Join-Path $Root "approvals"
    New-Item -ItemType Directory -Force $dir | Out-Null
    $file = Join-Path $dir "claude-decisions.jsonl"
    for ($node = $file; $node; $node = [IO.Path]::GetDirectoryName($node)) {
        if ((Test-Path -LiteralPath $node) -and ([IO.File]::GetAttributes($node) -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Đường dẫn nhật ký không an toàn.'
        }
        if ([IO.Path]::GetPathRoot($node) -eq $node) { break }
    }
    $line = ($obj | ConvertTo-Json -Compress -Depth 4) + "`n"
    $stream = New-Object IO.FileStream($file, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    try {
        $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($line)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
    } finally { $stream.Dispose() }
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $Root add -- "approvals/claude-decisions.jsonl" 2>$null
    git -C $Root commit -q -m "Claude tự quyết: $Title" 2>$null
    git -C $Root push -q 2>$null
    $ErrorActionPreference = $eap
    return $id
}

# Trước khi merge: CHẶN nếu task còn mục secret|security|sensitive-data|external đang pending / rejected.
# Mục merge cũ KHÔNG còn bắt buộc (bỏ qua requireMerge); mục approved cho security phải đúng commit HEAD.
function Assert-MergeApproval($cfg, $taskId, $head) {
    $sensitiveKinds = @('secret', 'security', 'sensitive-data', 'external')
    $items = @(Get-Approvals | Where-Object { $_.task -eq $taskId })
    $sensitive = @($items | Where-Object {
        $sensitiveKinds -contains ([string]$_.kind).ToLowerInvariant() -and
        (-not $_.commit -or $_.commit -eq $head)
    })
    $blocked = @($sensitive | Where-Object { $_.status -ne 'approved' })
    if ($blocked.Count -gt 0) {
        $b = $blocked | Sort-Object { [datetime]$_.requestedAt }, id | Select-Object -Last 1
        if ($b.status -eq 'pending') { throw "$($b.id) ($($b.kind)) đang chờ chủ dự án duyệt trên Hub ⇒ chưa merge." }
        if ($b.status -eq 'rejected') { throw "Chủ dự án đã TỪ CHỐI $($b.id)$(if ($b.note) { ': ' + $b.note }) ⇒ không merge." }
        throw "$($b.id) có trạng thái lạ '$($b.status)'."
    }
    foreach ($s in $sensitive) {
        if ($s.commit -and $s.commit -ne $head) { throw "$($s.id) duyệt commit $($s.commit.Substring(0, 7)) nhưng nhánh giờ là $("$head".Substring(0, [Math]::Min(7, "$head".Length))) ⇒ xin duyệt lại." }
    }
    # Mục merge cũ chỉ còn để không lỗi / giữ luồng nghiệm thu khi chủ dự án đã duyệt; pending / rejected KHÔNG chặn.
    $mergeItems = @($items | Where-Object { ([string]$_.kind).ToLowerInvariant() -eq 'merge' })
    if ($mergeItems.Count -eq 0) { return $null }
    $a = $mergeItems | Sort-Object { [datetime]$_.requestedAt }, id | Select-Object -Last 1
    if ($a.status -eq 'approved') {
        if ($a.commit -and $a.commit -ne $head) { throw "$($a.id) duyệt commit $($a.commit.Substring(0, 7)) nhưng nhánh giờ là $("$head".Substring(0, [Math]::Min(7, "$head".Length))) ⇒ xin duyệt lại." }
        Say "Chủ dự án đã duyệt $($a.id)$(if ($a.note) { ' — ' + $a.note }) ✓" Green
        return $a
    }
    return $null
}

function Invoke-Merge {
    $mutexName = $env:AITEAM_MERGE_MUTEX
    if ([string]::IsNullOrWhiteSpace($mutexName)) { $mutexName = 'Global\AITeam_Merge' }
    $mutex = New-Object Threading.Mutex($false, $mutexName)
    $locked = $false; $clone = $null
    $savedPreference = $ErrorActionPreference
    try {
    try { $locked = $mutex.WaitOne([TimeSpan]::FromMinutes(30)) } catch [Threading.AbandonedMutexException] { $locked = $true }
    if (-not $locked) { throw 'Đang có merge khác chạy' }
    $cfg = Get-Config
    $r = Resolve-Project $Project
    $tf = Find-TaskFile $r $Task
    $text = Get-Content $tf.FullName -Raw -Encoding UTF8
    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $agentId = [regex]::Match($text, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    git -C $r.Path fetch -q origin 2>$null
    $review = (git -C $r.Path show "origin/${branch}:tasks/$($tf.Name)" 2>$null) -join "`n"
    if ($review -notmatch 'Vòng\s+\d+\s*[—–-]+\s*ĐẠT') { $ErrorActionPreference = $eap; throw "Nhánh chưa có dòng '### Vòng N — ĐẠT' trong Review (Claude) ⇒ chưa merge." }
    $round = ([regex]::Matches($review, 'Vòng\s+(\d+)') | ForEach-Object { [int]$_.Groups[1].Value } | Measure-Object -Maximum).Maximum
    $head = (git -C $r.Path rev-parse "origin/$branch" 2>$null)
    $ErrorActionPreference = $eap
    $approval = Assert-MergeApproval $cfg (Get-TaskId $tf) $head
    $ErrorActionPreference = "Continue"
    Invoke-RepoTransaction -RepoPath $r.Path -Action {
        $dirty = @(git -C $r.Path -c core.quotepath=false status --porcelain --untracked-files=all)
        if ($LASTEXITCODE -ne 0) { throw 'Không đọc được trạng thái main.' }
        $other = @($dirty | Where-Object { $_ -cnotmatch '^.. (requests/R-[^/]+\.md|tasks/README\.md|metrics/runs/.+)$' })
        if ((git -C $r.Path branch --show-current) -eq 'main' -and $dirty.Count -gt 0 -and $other.Count -eq 0) {
            $files = @($dirty | ForEach-Object { $_.Substring(3) })
            git -C $r.Path add -- $files
            if ($LASTEXITCODE -ne 0) { throw 'Không thêm được nhật ký / bảng / số đo.' }
            git -C $r.Path commit -q -m 'Nhật ký / bảng / số đo (tự commit trước merge)' -- $files
            if ($LASTEXITCODE -ne 0) { throw 'Không tự commit được nhật ký / bảng / số đo.' }
            git -C $r.Path push -q origin main 2>$null
            if ($LASTEXITCODE -ne 0) { throw 'Không push được nhật ký / bảng / số đo.' }
        }
    }
    $dirty = @(git -C $r.Path status --porcelain)
    if ((git -C $r.Path branch --show-current) -ne 'main' -or $dirty.Count -ne 0) { throw 'Main local chưa sạch; không merge.' }
    git -C $r.Path pull -q --ff-only 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Không hoàn tất nghiệm thu.' }
    $before = git -C $r.Path rev-parse HEAD
    $ahead = @(git -C $r.Path rev-list origin/main..HEAD)
    if ((git -C $r.Path branch --show-current) -ne 'main' -or $ahead.Count -ne 0) { throw 'Main local chưa đồng bộ; không merge.' }
    git -C $r.Path merge --no-ff -q "origin/$branch" -m "Merge $($tf.BaseName) ($(Get-StaffName $agentId), review đạt vòng $round)`n`nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" 2>&1 | Select-Object -Last 2 | ForEach-Object { Say $_ Yellow }
    if ($LASTEXITCODE -ne 0) {
        # File task thường được thêm cả trên main (Claude) lẫn trên nhánh (khung + báo cáo + review) ⇒ add/add.
        # Bản trên nhánh là bản đầy đủ ⇒ lấy bản nhánh. Xung đột ở file khác ⇒ dừng, xử lý tay.
        $conflicts = @(git -C $r.Path diff --name-only --diff-filter=U 2>$null)
        $onlyTask = $conflicts.Count -gt 0 -and -not ($conflicts | Where-Object { $_ -ne "tasks/$($tf.Name)" })
        if ($onlyTask) {
            git -C $r.Path checkout --theirs -- "tasks/$($tf.Name)" 2>$null
            git -C $r.Path add -- "tasks/$($tf.Name)" 2>$null
            git -C $r.Path commit -q --no-edit 2>$null
            if ($LASTEXITCODE -ne 0) { throw 'Không hoàn tất nghiệm thu.' }
            Say "Xung đột chỉ ở file task ⇒ đã lấy bản trên nhánh (có báo cáo + review)." Yellow
        } else {
            $ErrorActionPreference = $eap
            throw "Merge xung đột ở: $($conflicts -join ', ') — xử lý tay (git merge --abort để hủy)."
        }
    }
    $mergeHead = git -C $r.Path rev-parse HEAD
    if ($mergeHead -eq $before) { throw 'Không tạo được commit merge mới.' }
    $clone = Join-Path $env:TEMP ('merge-check-' + [guid]::NewGuid().ToString('N'))
    try {
        git clone --no-hardlinks -q -- $r.Path $clone 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Không clone được main local.' }
        $built = Invoke-ShellCapture $r.Info.build $clone
        if ($built.Code -ne 0 -or $built.Output -match ': warning (?!NU1900)[A-Za-z]+\d+:') { throw "Build clone sạch lỗi: $($built.Output)" }
        $testCmd = $r.Info.test
        if ($testCmd -match '^\s*dotnet\s+test\b' -and $testCmd -notmatch 'blame-hang') { $testCmd += ' --blame-hang-timeout 5m --blame-hang-dump-type none' }
        $savedMutexName = $env:AITEAM_MERGE_MUTEX
        try {
            $env:AITEAM_MERGE_MUTEX = 'Global\AITeam_Merge_test_' + [guid]::NewGuid().ToString('N')
            $res = Invoke-ShellCapture $testCmd $clone
            $res = Invoke-FailedTestRetry -Code $res.Code -Output $res.Output -TestCommand $testCmd -Run {
                param($command)
                Invoke-ShellCapture $command $clone
            }
        } finally { $env:AITEAM_MERGE_MUTEX = $savedMutexName }
        foreach ($warning in $res.Warnings) { Say $warning Yellow }
        if ($res.Code -ne 0 -or $res.Output -notmatch 'Passed!\s+.*Passed:\s*[1-9]') { throw "Test clone sạch lỗi: $($res.Output)" }
    } catch {
        $failure = $_
        git -C $r.Path fetch -q origin 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Không xác minh được remote; không reset main.' }
        # Chỉ xét chuỗi cha thứ nhất của main; các commit nhánh thuộc chính merge vừa tạo.
        $pending = @(git -C $r.Path rev-list --first-parent origin/main..main)
        $current = git -C $r.Path rev-parse main
        $original = git -C $r.Path rev-parse ORIG_HEAD
        if ($LASTEXITCODE -ne 0 -or $pending.Count -ne 1 -or $pending[0] -ne $mergeHead -or $current -ne $mergeHead -or $original -ne $before) { throw 'Không an toàn để hủy merge; không reset main.' }
        git -C $r.Path reset --hard ORIG_HEAD 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Không hủy được merge local.' }
        Say "$failure" Red
        throw 'Clone sạch KHÔNG ĐẠT — đã hủy merge, main không đổi'
    }
    git -C $r.Path push -q origin main 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Không hoàn tất nghiệm thu.' }
    $mergeTaskId = Get-TaskId $tf
    Invoke-RepoTransaction -RepoPath $r.Path -Action {
        Update-WorkflowJournal -RepoPath $r.Path -TaskFile $tf.Name -TaskId $mergeTaskId -Event 'Merge' -Run $round -Result 'ĐẠT'
        Set-BoardStatus $r $mergeTaskId "✅ Đã merge (vòng $round)"
    }
    Say "Đã merge + push main." Green
    # V10-03: merge do Claude tự quyết ⇒ ghi nhật ký (không còn chờ chủ dự án).
    try {
        $decisionRequest = [regex]::Match($text, '\*\*Yêu cầu:\*\*\s*(R-\d+)').Groups[1].Value
        $decisionId = Add-ClaudeDecision -Kind 'merge' -Title "Merge $($tf.BaseName)" -Task $mergeTaskId -Request $decisionRequest -Detail "Nhánh $branch · review đạt vòng $round"
        Say "Nhật ký Claude tự quyết: $decisionId (merge)" Green
    } catch { Say "Không ghi được nhật ký Claude tự quyết: $($_.Exception.Message)" Yellow }
    if ($approval) {
        # Quyết định chủ dự án bấm trên Hub chỉ nằm trên máy này ⇒ đưa lên git để máy kia cũng thấy.
        git -C $Root add -- "approvals/$($approval.id).json" 2>$null
        git -C $Root commit -q -m "Duyệt $($approval.id): $($approval.title)" 2>$null
        git -C $Root push -q 2>$null
    }
    # Dọn worktree của task
    foreach ($wt in ((git -C $r.Path worktree list --porcelain) -join "`n" | Select-String -Pattern "worktree ([^\n]+)\nHEAD [0-9a-f]+\nbranch refs/heads/$([regex]::Escape($branch))" -AllMatches).Matches) {
        git -C $r.Path worktree remove --force $wt.Groups[1].Value 2>$null; Say "Đã dọn $($wt.Groups[1].Value)"
    }
    git -C $r.Path worktree prune 2>$null
    $ErrorActionPreference = $eap
    # Bản chạy thử cho chủ dự án
    $own = Join-Path $r.Path "tools\ban-chay-thu.ps1"
    if (Test-Path $own) { & powershell -NoProfile -ExecutionPolicy Bypass -File $own -NoLaunch -NoPull | Select-Object -Last 1 | ForEach-Object { Say $_ } }
    } finally {
        if ($clone -and (Test-Path -LiteralPath $clone)) { Remove-Item -LiteralPath $clone -Recurse -Force -ErrorAction SilentlyContinue }
        $ErrorActionPreference = $savedPreference
        if ($locked) { $mutex.ReleaseMutex() }; $mutex.Dispose()
    }
}
# ===== Chạy thử app cho chủ dự án: bản ĐÃ MERGE (main), build ra thư mục riêng, không đụng việc agent đang làm =====
function Invoke-Try {
    $cfg = Get-Config
    $r = Resolve-Project $Project
    # Dự án có script riêng (tools\ban-chay-thu.ps1, bấm CHAY-THU.cmd) ⇒ dùng nó: bản chạy thử nằm trong <dự án>\ban-chay-thu\
    $own = Join-Path $r.Path "tools\ban-chay-thu.ps1"
    if (Test-Path $own) { & powershell -NoProfile -ExecutionPolicy Bypass -File $own $(if ($NoLaunch) { "-NoLaunch" }); return }
    $eap = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    $dirty = @(git -C $r.Path status --porcelain 2>$null | Where-Object { $_ })
    if ($dirty.Count -eq 0) { git -C $r.Path pull -q 2>$null } else { Say "Thư mục $($r.Path) có thay đổi chưa commit → không pull, build bản hiện có." Yellow }
    $commit = (git -C $r.Path log -1 --format="%h %ad %s" --date=format:"%d/%m %H:%M" 2>$null)
    $ErrorActionPreference = $eap

    $app = if ($r.Info.app) { Join-Path $r.Path $r.Info.app } else {
        Get-ChildItem (Join-Path $r.Path "src") -Recurse -Filter "*.App.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
    if (-not $app -or -not (Test-Path $app)) { throw "Không tìm thấy project app (*.App.csproj) của $($r.Info.name). Thêm trường ""app"" vào projects.json." }

    $out = Join-Path (Split-Path $cfg.workRoot -Parent) ("AITeam-try\" + $r.Info.name)
    $running = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($out, [StringComparison]::OrdinalIgnoreCase) })
    if ($running) { throw "Bản chạy thử đang mở ($($running[0].ProcessName)). Đóng app rồi chạy lại lệnh." }

    Say "Build bản đã merge: $commit" Cyan
    New-Item -ItemType Directory -Force $out | Out-Null
    $log = Join-Path $out "build.log"
    $p = Start-Process dotnet -ArgumentList "build `"$app`" -c Release -o `"$out`" -nologo -v q" -NoNewWindow -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError "$log.err"
    if ($p.ExitCode -ne 0) { Get-Content $log -Tail 15 | ForEach-Object { Say $_ Red }; throw "Build lỗi — xem $log" }
    [IO.File]::WriteAllText((Join-Path $out "PHIEN-BAN.txt"), "Bản chạy thử $($r.Info.name)`r`nCommit: $commit`r`nBuild lúc: $(Get-Date -Format 'HH:mm dd/MM/yyyy')`r`n", $Utf8)
    $exe = Get-ChildItem $out -Filter "*.exe" | Where-Object { $_.BaseName -eq [IO.Path]::GetFileNameWithoutExtension($app).Replace(".App", "") -or $_.BaseName -eq [IO.Path]::GetFileNameWithoutExtension($app) } | Select-Object -First 1
    if (-not $exe) { $exe = Get-ChildItem $out -Filter "*.exe" | Where-Object { $_.BaseName -notmatch 'Updater|createdump' } | Select-Object -First 1 }
    Say "Xong: $($exe.FullName)" Green
    if (-not $NoLaunch) { Start-Process $exe.FullName -WorkingDirectory $out }
}
# ===== Nhân viên: tên chỉ dùng trong log / đánh giá (staff/<mã>-<tên>.md) =====
function Get-StaffName([string]$id) {
    $sf = Get-ChildItem (Join-Path $Root "staff") -Filter "$id-*.md" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $sf) { return "Agent $id" }
    $n = [regex]::Match((Get-Content $sf.FullName -Raw -Encoding UTF8), '\*\*Tên:\*\*\s*(.+)').Groups[1].Value.Trim()
    if ($n) { "$n ($id)" } else { "Agent $id" }
}

# Lỗi trong mục Review ghi dạng: "- [AG] (bảo mật) ..." — AG: agent làm sai · TK: task / thiết kế của Claude thiếu · CK: cổng kiểm tra đáng lẽ phải bắt.
function Get-ReviewFindings([string]$review) {
    if ([string]::IsNullOrWhiteSpace($review)) { return @() }
    $findingExe = Get-AITeamExe
    if (-not $findingExe) { throw 'Không build được CLI để phân loại lỗi.' }
    $findingFile = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($findingFile, $review, (New-Object Text.UTF8Encoding $false))
        $findingJson = @(& $findingExe review-findings --file $findingFile)
        if ($LASTEXITCODE -ne 0) { throw 'Không đọc được lỗi có cấu trúc.' }
        @((($findingJson -join "`n") | ConvertFrom-Json))
    } finally { Remove-Item -LiteralPath $findingFile -ErrorAction SilentlyContinue }
}

# agy báo thời gian / token CỘNG DỒN cả hội thoại khi giao lại bằng -Resume ⇒ đổi về số của riêng từng lượt.
function Get-RunDeltas($runs) {
    $out = @()
    foreach ($g in ($runs | Group-Object { if ($_.conversationId) { $_.conversationId } else { [guid]::NewGuid().ToString() } })) {
        $prevD = 0; $prevT = 0
        foreach ($x in ($g.Group | Sort-Object at)) {
            $d = [math]::Max(0, [int]$x.durationSec - $prevD); $tk = [math]::Max(0, [long]$x.totalTokens - $prevT)
            if ($x.conversationId) { $prevD = [int]$x.durationSec; $prevT = [long]$x.totalTokens }
            $c = $x.PSObject.Copy(); $c.durationSec = $d; $c.totalTokens = $tk; $out += $c
        }
    }
    $out
}
function Invoke-StaffReport {
    $runs = @(Get-ChildItem (Join-Path $Root "metrics\runs") -Filter *.json -ErrorAction SilentlyContinue | ForEach-Object {
        try { Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { } })
    $runs = @(Get-RunDeltas $runs)
    $tasks = @()
    foreach ($p in Get-Projects) {
        $r = Resolve-Project $p.name
        $dir = Join-Path $r.Path "tasks"
        if (-not (Test-Path $dir)) { continue }
        foreach ($tf in Get-ChildItem $dir -Filter "*.md" | Where-Object { $_.Name -ne "README.md" }) {
            $tx = Get-Content $tf.FullName -Raw -Encoding UTF8
            $req = [regex]::Match($tx, '\*\*Yêu cầu:\*\*\s*(R-\d+)').Groups[1].Value
            if (-not $req) { continue }   # chỉ tính task theo quy trình mới (có phiếu yêu cầu)
            $review = Get-Section $tx "Review (Claude)"
            $rounds = @([regex]::Matches($review, 'Vòng\s+(\d+)') | ForEach-Object { [int]$_.Groups[1].Value })
            $mine = @($runs | Where-Object { $_.project -eq $p.name -and $tf.BaseName.StartsWith($_.task) } | Sort-Object at)
            $budget = [regex]::Match($tx, '\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
            $tasks += [pscustomobject]@{
                Agent = [regex]::Match($tx, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
                Task = "$($p.name)/$($tf.BaseName)"; Request = $req
                Runs = $mine.Count; Min = (($mine | Measure-Object durationSec -Sum).Sum) / 60; Tokens = ($mine | Measure-Object totalTokens -Sum).Sum
                VerifyFirst = $(if ($mine) { $mine[0].verifyOk } else { $null })
                Lines = $(if ($mine) { $mine[-1].linesAdded } else { $null }); Budget = $(if ($budget) { [int]$budget } else { $null })
                Rounds = $(if ($rounds) { ($rounds | Measure-Object -Maximum).Maximum } else { 0 })
                Passed = [bool]($review -match 'Vòng\s+\d+\s*[—–-]+\s*ĐẠT')
                ClaudeFix = ([regex]::Matches($review, '(?im)^\s*[-*]?\s*(Claude|Quản lý) sửa')).Count
                Findings = Get-ReviewFindings $review
            }
        }
    }
    if (-not $tasks) { Say "Chưa có task nào theo quy trình mới (dòng **Yêu cầu:** R-xxx)." Yellow; return }
    $pct = { param($a, $b) if ($b) { "{0:P0}" -f ($a / $b) } else { "-" } }
    Say "== Bảng điểm nhân viên (chỉ task có phiếu yêu cầu) ==" Cyan
    foreach ($g in $tasks | Group-Object Agent | Sort-Object Name) {
        $x = $g.Group; $done = @($x | Where-Object { $_.Passed })
        $runsN = ($x | Measure-Object Runs -Sum).Sum
        $vf = @($x | Where-Object { $null -ne $_.VerifyFirst })
        $bud = @($x | Where-Object { $_.Budget -and $_.Lines })
        $ag = @($x | ForEach-Object { $_.Findings } | Where-Object { $_.Cause -eq 'AG' })
        Say ("  {0}: {1} task ({2} xong) · {3} lượt · TB {4:N1} phút/lượt · TB {5:N0}k token/lượt" -f (Get-StaffName $g.Name), $x.Count, $done.Count, $runsN,
            $(if ($runsN) { ($x | Measure-Object Min -Sum).Sum / $runsN } else { 0 }), $(if ($runsN) { ($x | Measure-Object Tokens -Sum).Sum / $runsN / 1000 } else { 0 }))
        Say ("     cổng kiểm tra đạt lần đầu: {0} · vòng review TB {1:N2} · đạt vòng 1: {2} · trong ngân sách: {3} · Claude tự sửa: {4} lần" -f
            (& $pct @($vf | Where-Object { $_.VerifyFirst }).Count $vf.Count), (($done | Measure-Object Rounds -Average).Average),
            (& $pct @($done | Where-Object { $_.Rounds -le 1 }).Count $done.Count), (& $pct @($bud | Where-Object { $_.Lines -le $_.Budget }).Count $bud.Count),
            ($x | Measure-Object ClaudeFix -Sum).Sum)
        if ($ag) { Say ("     lỗi do nhân viên (AG): " + (($ag | Group-Object Kind | Sort-Object Count -Descending | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ' · ')) Yellow }
    }
    $all = @($tasks | ForEach-Object { $_.Findings })
    $tk = @($all | Where-Object { $_.Cause -eq 'TK' }); $ck = @($all | Where-Object { $_.Cause -eq 'CK' })
    Say "  Quản lý (Claude) + công cụ:" Cyan
    Say ("     lỗi do task / thiết kế (TK): {0}{1}" -f $tk.Count, $(if ($tk) { " — " + (($tk | Group-Object Kind | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ' · ') } else { "" }))
    Say ("     lỗi cổng kiểm tra để lọt (CK): {0}{1}" -f $ck.Count, $(if ($ck) { " — " + (($ck | Group-Object Kind | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ' · ') } else { "" }))
}
# ===== Yêu cầu của chủ dự án (R-xxx): đơn vị theo dõi + đánh giá hiệu suất =====
function Invoke-NewRequest {
    $dir = Join-Path $Root "requests"
    New-Item -ItemType Directory -Force $dir | Out-Null
    $last = @(Get-ChildItem $dir -Filter "R-*.md" | ForEach-Object { [int]([regex]::Match($_.Name, '^R-(\d+)').Groups[1].Value) } | Sort-Object)[-1]
    $id = "R-{0:D3}" -f ($last + 1)
    $plain = (($Title.Normalize([Text.NormalizationForm]::FormD).ToCharArray() | Where-Object { [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne "NonSpacingMark" }) -join "").Replace("đ", "d").Replace("Đ", "D")
    $slug = (($plain -replace '[^A-Za-z0-9 ]', '').Trim() -replace '\s+', '-').ToLower()
    if ($slug.Length -gt 40) { $slug = $slug.Substring(0, 40).Trim('-') }
    $file = Join-Path $dir "$id-$slug.md"
    $tpl = [IO.File]::ReadAllText((Join-Path $Root "templates\request.md"))
    $tpl = $tpl.Replace("{{ID}}", $id).Replace("{{TITLE}}", $Title).Replace("{{NOW}}", (Get-Date -Format "HH:mm dd/MM/yyyy")).Replace("{{MACHINE}}", $env:COMPUTERNAME)
    [IO.File]::WriteAllText($file, $tpl, $Utf8)
    Say "Đã tạo ${id}: $file" Green
    $id
}

function Invoke-RequestReport {
    if (-not $Request) { throw "Thiếu -Request R-xxx" }
    $runs = @(Get-ChildItem (Join-Path $Root "metrics\runs") -Filter *.json -ErrorAction SilentlyContinue | ForEach-Object {
        try { Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { } })
    $runs = @(Get-RunDeltas $runs)
    $rows = @()
    foreach ($p in Get-Projects) {
        $r = Resolve-Project $p.name
        $dir = Join-Path $r.Path "tasks"
        if (-not (Test-Path $dir)) { continue }
        foreach ($tf in Get-ChildItem $dir -Filter "*.md" | Where-Object { $_.Name -ne "README.md" }) {
            $tx = Get-Content $tf.FullName -Raw -Encoding UTF8
            if ($tx -notmatch "\*\*Yêu cầu:\*\*\s*$([regex]::Escape($Request))\b") { continue }
            $review = Get-Section $tx "Review (Claude)"
            $rounds = @([regex]::Matches($review, 'Vòng\s+(\d+)') | ForEach-Object { [int]$_.Groups[1].Value })
            $mine = @($runs | Where-Object { $_.project -eq $p.name -and $tf.BaseName.StartsWith($_.task) })
            $budget = [regex]::Match($tx, '\*\*Ngân sách:\*\*\s*~?(\d+)').Groups[1].Value
            $rows += [pscustomobject]@{
                Project = $p.name; Task = $tf.BaseName
                Staff = Get-StaffName ([regex]::Match($tx, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value)
                Runs = $mine.Count
                AgentMin = [math]::Round((($mine | Measure-Object durationSec -Sum).Sum) / 60, 1)
                TokensK = [math]::Round((($mine | Measure-Object totalTokens -Sum).Sum) / 1000)
                Lines = ($mine | Sort-Object at | Select-Object -Last 1).linesAdded
                Budget = $budget
                VerifyFirst = $(if ($mine) { ($mine | Sort-Object at | Select-Object -First 1).verifyOk } else { $null })
                Rounds = $(if ($rounds) { ($rounds | Measure-Object -Maximum).Maximum } else { 0 })
                Passed = [bool]($review -match 'Vòng\s+\d+\s*[—–-]+\s*ĐẠT')
                ClaudeFix = ([regex]::Matches($review, '(?im)^\s*[-*]?\s*(Claude|Quản lý) sửa')).Count
            }
        }
    }
    if (-not $rows) { Say "Không có task nào ghi **Yêu cầu:** $Request" Yellow; return }
    Say "== Báo cáo $Request ==" Cyan
    Say ("  {0,-22} {1,-34} {9,-10} {2,4} {3,8} {4,8} {5,11} {6,7} {7,5} {8,6}" -f "Dự án", "Task", "Lượt", "Phút AG", "Token k", "Dòng/ngân", "Cổng v1", "Vòng", "ĐẠT", "Nhân viên")
    foreach ($x in $rows) {
        $ln = if ($x.Lines) { "$($x.Lines)/$(if ($x.Budget) { $x.Budget } else { '-' })" } else { "-" }
        $vf = if ($null -eq $x.VerifyFirst) { "-" } elseif ($x.VerifyFirst) { "đạt" } else { "trượt" }
        Say ("  {0,-22} {1,-34} {9,-10} {2,4} {3,8} {4,8} {5,11} {6,7} {7,5} {8,6}" -f $x.Project, $x.Task, $x.Runs, $x.AgentMin, $x.TokensK, $ln, $vf, $x.Rounds, $(if ($x.Passed) { "có" } else { "chưa" }), $x.Staff)
    }
    Say ("  Tổng: {0} task · {1} lượt agent · {2} phút agent · {3}k token · vòng TB {4:N2} · đạt vòng 1: {5}/{0}" -f $rows.Count,
        ($rows | Measure-Object Runs -Sum).Sum, ($rows | Measure-Object AgentMin -Sum).Sum, ($rows | Measure-Object TokensK -Sum).Sum,
        ($rows | Measure-Object Rounds -Average).Average, @($rows | Where-Object { $_.Rounds -le 1 -and $_.Passed }).Count)
}
function Invoke-Metrics {
    $rows = @()
    foreach ($p in Get-Projects | Where-Object { -not $Project -or $_.name -eq $Project }) {
        $r = Resolve-Project $p.name
        $dir = Join-Path $r.Path "tasks"
        if (-not (Test-Path $dir)) { continue }
        foreach ($f in Get-ChildItem $dir -Filter "*.md" | Where-Object { $_.Name -ne "README.md" }) {
            $t = Get-Content $f.FullName -Raw -Encoding UTF8
            $review = Get-Section $t "Review (Claude)"
            $rounds = @([regex]::Matches($review, 'Vòng\s+(\d+)') | ForEach-Object { [int]$_.Groups[1].Value })
            $max = if ($rounds) { ($rounds | Measure-Object -Maximum).Maximum } else { 0 }
            $passed = $review -match 'Vòng\s+\d+\s*[—–-]+\s*ĐẠT'
            $selfFix = $review -match '(?i)(Claude|quản lý)\s+(tự\s+)?(sửa|hoàn tất)|Lỗi quản lý'
            $sec = $review -match '(?i)lỗi bảo mật|zip slip|traversal|chèn lệnh|bị lộ|ghi bí mật ra'
            $ui = $review -match '(?i)bố cục|chồng (nút|nhau|lên)|(nút|nhãn|chữ)[^.\r\n]{0,40}(bị|đè|che)[^.\r\n]{0,20}(đè|che)|che chữ|xuống dòng giữa'
            $bom = $review -match 'BOM'
            $rows += [pscustomobject]@{ Project = $p.name; Task = ($f.BaseName -split '-')[0..1] -join '-'; Rounds = $max; Passed = $passed; SelfFix = $selfFix; Security = $sec; UI = $ui; Bom = $bom }
        }
    }
    $done = @($rows | Where-Object { $_.Passed })
    Say "== Số liệu review (task đã ĐẠT: $($done.Count), đang làm: $(@($rows | Where-Object { -not $_.Passed }).Count)) ==" Cyan
    $yn = { param($b) if ($b) { "có" } else { "-" } }
    Say ("  {0,-22} {1,-7} {2,4}  {3,-7} {4,-7} {5,-9} {6}" -f "Dự án", "Task", "Vòng", "Tự sửa", "B.mật", "Giao diện", "BOM")
    foreach ($x in $done | Sort-Object Project, Task) {
        Say ("  {0,-22} {1,-7} {2,4}  {3,-7} {4,-7} {5,-9} {6}" -f $x.Project, $x.Task, $x.Rounds, (& $yn $x.SelfFix), (& $yn $x.Security), (& $yn $x.UI), (& $yn $x.Bom))
    }
    if ($done.Count) {
        $pct = { param($n) "{0:P0}" -f ($n / $done.Count) }
        Say ("Số vòng trung bình: {0:N2}" -f (($done | Measure-Object Rounds -Average).Average))
        Say ("Đạt ngay vòng 1: " + (& $pct @($done | Where-Object { $_.Rounds -le 1 }).Count))
        Say ("Claude phải tự sửa: " + (& $pct @($done | Where-Object { $_.SelfFix }).Count))
        Say ("Review có lỗi bảo mật: " + (& $pct @($done | Where-Object { $_.Security }).Count) + " · giao diện: " + (& $pct @($done | Where-Object { $_.UI }).Count) + " · mất BOM: " + (& $pct @($done | Where-Object { $_.Bom }).Count))
    }
}

# ===== Quản lý phụ tự động (chủ dự án 25/10): Claude vắng (không chạy team.ps1 > 20 phút) ⇒ Codex điều phối 1 vòng theo QUAN-LY-PHU.md.
# Chốt cuối (nghiệm thu ĐẠT + merge + push main) LUÔN là Claude — Codex chỉ ghi "Đề xuất nghiệm thu".
$DeputyIdleMinutes = 20
function Update-ManagerHeartbeat {
    if ($env:AITEAM_ACTOR -eq "deputy" -or $Command -in @("deputy", "help")) { return }
    try {
        $f = Join-Path $Root ".state\claude-heartbeat"
        New-Item -ItemType Directory -Force (Split-Path $f) | Out-Null
        [IO.File]::WriteAllText($f, (Get-Date -Format o))
    } catch { }
}

function Invoke-Deputy {
    param([ValidateRange(1, 2700)][int]$TimeoutSeconds = 2700)
    $hb = Join-Path $Root ".state\claude-heartbeat"
    $lock = Join-Path $Root ".state\deputy.lock"
    $idle = if (Test-Path $hb) { ((Get-Date) - (Get-Item $hb).LastWriteTime).TotalMinutes } else { 999 }
    if ($idle -lt $DeputyIdleMinutes -and -not $Force) { Say "Claude vừa hoạt động $([int]$idle) phút trước ⇒ quản lý phụ nghỉ."; return }
    if ((Test-Path $lock) -and ((Get-Date) - (Get-Item $lock).LastWriteTime).TotalMinutes -lt 90) { Say "Quản lý phụ đang chạy (khóa $lock)."; return }
    New-Item -ItemType Directory -Force (Split-Path $lock) | Out-Null
    [IO.File]::WriteAllText($lock, (Get-Date -Format o))
    try {
        $codexCmd = (Get-Command codex.cmd -ErrorAction SilentlyContinue).Source
        if (-not $codexCmd) { Say "Không thấy codex.cmd ⇒ bỏ qua." Yellow; return }
        $stamp = Get-Date -Format "yyyyMMdd-HHmm"
        $pf = Join-Path $Root ".state\deputy-$stamp.prompt.md"
        $log = Join-Path $Root ".state\deputy-$stamp.log"
        $deputyText = @"
Bạn là Codex QUẢN LÝ PHỤ của team AITeam (Claude đang vắng). Thư mục: $Root. Đọc QUAN-LY-PHU.md rồi làm MỘT vòng điều phối:
- kiểm tra việc đang dở (bảng việc tasks/README.md, logs ở $((Resolve-Project 'AITeam').Logs)), chạy các lệnh .\team.ps1 cần thiết (plan / prep / assign / verify / qc) theo đúng thứ tự ưu tiên trong sổ tay;
- task nào cổng KT ĐẠT + QC PASS ⇒ viết "Đề xuất nghiệm thu" lên nhánh và thêm vào mục "Chờ Claude chốt".
TUYỆT ĐỐI KHÔNG: chạy .\team.ps1 merge; push commit code lên main; force-push; xóa nhánh / dữ liệu; tải phần mềm; đưa bí mật vào file.
Bạn tự commit + push lên main CHỈ các file: plans/**, requests/**, tasks/README.md, QUAN-LY-PHU.md. Commit do CHÍNH team.ps1 tự làm khi chạy plan / prep / assign / verify / qc (bảng việc, metrics/runs/**, nhật ký phiếu, file task mới) là ĐƯỢC PHÉP — cứ chạy các lệnh đó.
Không có việc cần làm ⇒ dừng ngay, không đọc thêm. Lệnh chạy lâu (assign / prep) chạy nền bằng Start-Process rồi kết thúc vòng — vòng sau (30 phút) xem tiếp.
Cuối vòng: cập nhật mục "Việc đang dở" + thêm 1 dòng có giờ vào mục "Nhật ký quản lý phụ" cuối QUAN-LY-PHU.md (tạo mục nếu chưa có), commit + push. Trả lời ≤ 10 dòng.
"@
        [IO.File]::WriteAllText($pf, $deputyText, $Utf8)
        Say "[$(Get-Date -Format HH:mm)] Claude vắng $([int]$idle) phút ⇒ Codex quản lý phụ chạy 1 vòng (log $log)." Cyan
        $env:AITEAM_ACTOR = "deputy"
        # Model ghi đè theo lần gọi (KHÔNG sửa ~/.codex/config.toml): model mặc định trong config của máy không dùng được với tài khoản ChatGPT.
        $proc = Start-Process $codexCmd -ArgumentList "exec", "-m", "gpt-6.1-sol", "--dangerously-bypass-approvals-and-sandbox", "--skip-git-repo-check", "-C", "`"$Root`"", "-" `
            -WorkingDirectory $Root -WindowStyle Hidden -PassThru -RedirectStandardInput $pf -RedirectStandardOutput $log -RedirectStandardError "$log.err"
        try {
            if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
                & taskkill.exe /PID $proc.Id /T /F | Out-Null
                if ($LASTEXITCODE -ne 0 -and -not $proc.HasExited) { throw "Không dừng được cây tiến trình quản lý phụ." }
                $journal = Join-Path $Root 'QUAN-LY-PHU.md'
                $text = if (Test-Path $journal) { [IO.File]::ReadAllText($journal) } else { '' }
                $entry = "`r`n- $(Get-Date -Format 'yyyy-MM-dd HH:mm'): Lượt quản lý quá 45 phút — đã dừng`r`n"
                $heading = [regex]::Match($text, '(?m)^## Nhật ký quản lý phụ[^\r\n]*(?:\r?\n|$)')
                if ($heading.Success) { $text = $text.Insert($heading.Index + $heading.Length, $entry) }
                else { $text += "`r`n## Nhật ký quản lý phụ`r`n" + $entry }
                $bytes = if (Test-Path $journal) { [IO.File]::ReadAllBytes($journal) } else { @() }
                $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
                [IO.File]::WriteAllText($journal, $text, (New-Object Text.UTF8Encoding $bom))
                Say "Lượt quản lý quá 45 phút — đã dừng" Yellow
            }
        } finally { $proc.Dispose() }
        Get-Content $log -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { Say "  $_" }
    }
    finally { Remove-Item $lock -Force -ErrorAction SilentlyContinue; $env:AITEAM_ACTOR = $null }
}

# Bật / tắt lịch Windows chạy deputy mỗi 30 phút (chỉ khi chủ dự án đang đăng nhập — cần đăng nhập Antigravity / Codex của user).
function Invoke-DeputySchedule([bool]$on) {
    $taskName = "AITeam-QuanLyPhu"
    if ($on) {
        # Không lồng dấu " (PowerShell 5.1 làm hỏng tham số có ngoặc kép khi gọi exe) — đường dẫn AITeam không có khoảng trắng.
        $act = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File $(Join-Path $Root 'team.ps1') deputy"
        schtasks /Create /F /SC MINUTE /MO 30 /TN $taskName /TR $act /IT | Out-Null
        if ($LASTEXITCODE -eq 0) { Say "Đã bật lịch $taskName (30 phút / lần, khi đang đăng nhập). Tắt: .\team.ps1 deputy-off" Green } else { Say "Không tạo được lịch $taskName (mã $LASTEXITCODE)." Red }
    } else {
        schtasks /Delete /F /TN $taskName 2>&1 | Out-Null
        Say "Đã tắt lịch $taskName." Yellow
    }
    $global:LASTEXITCODE = 0
}

try {
if ($Command -ne 'accept-pack') { Update-ManagerHeartbeat }
if ($Command -ne "skill-note") {
    . (Join-Path $Root "tools/workflow-common.ps1")
    . (Join-Path $Root "tools/workflow-pipeline.ps1")
    . (Join-Path $Root "tools/workflow-accept.ps1")
}
switch ($Command) {
    "skill-note" {
        $savedExit = $global:LASTEXITCODE
        Invoke-SkillNote -Root $Root -Skill $Skill -Text $Text -Source $Source
        $noteExit = $global:LASTEXITCODE
        try {
            $projPath = Join-Path $Root "core\AITeam.Cli\AITeam.Cli.csproj"
            if (Test-Path -LiteralPath $projPath) {
                $noteExe = Get-AITeamExe
                if ($noteExe -and (Test-Path -LiteralPath $noteExe -PathType Leaf)) {
                    $titleText = if ($Text.Length -gt 80) { $Text.Substring(0, 80) } else { $Text }
                    $noteArgs = @('lesson-add', '--root', $Root, '--title', $titleText, '--body', $Text, '--kind', 'meo')
                    if ($Source) { $noteArgs += @('--task', $Source) }
                    $lessonOut = (& $noteExe @noteArgs 2>$null) -join "`n"
                    if ($LASTEXITCODE -eq 0 -and (-not [string]::IsNullOrWhiteSpace($lessonOut))) {
                        Write-Output $lessonOut.Trim()
                    } elseif ($LASTEXITCODE -ne 0) {
                        Write-Warning "Không thể tạo bài học nháp qua CLI (mã $LASTEXITCODE)."
                    }
                } else {
                    Write-Warning "Chưa có aiteam CLI để tạo bài học nháp."
                }
            }
        } catch {
            Write-Warning "Lỗi khi tạo bài học nháp: $_"
        } finally {
            $global:LASTEXITCODE = $noteExit
        }
    }
    "setup" { Invoke-Setup; Install-Skill; Invoke-InstallHooks }
    "ask" { Invoke-Ask }
    "plan" { Invoke-Plan }
    "prep" { Invoke-Prep }
    "pipeline" { Invoke-Pipeline }
    "qc" { Invoke-Qc }
    "accept-pack" { Invoke-AcceptPack }
    "accept" { Invoke-Accept }
    "deputy" { Invoke-Deputy }
    "deputy-on" { Invoke-DeputySchedule $true }
    "deputy-off" { Invoke-DeputySchedule $false }
    "models" { Invoke-Models }
    "install-hooks" { Invoke-InstallHooks }
    "secret-set" { Invoke-SecretSet }
    "secret-list" { Invoke-SecretList }
    "secret-remove" { Invoke-SecretRemove }
    "secret-env" { Invoke-SecretEnv }
    "doctor" { Invoke-Doctor }
    "add-project" { Invoke-AddProject }
    "new-task" { Invoke-NewTask }
    "assign" { Invoke-Assign }
    "auto" { Invoke-AutoQueue }
    "lint-task" { Invoke-LintTask }
    "status" { Invoke-Status }
    "verify" { $null = Invoke-Verify }
    "metrics" { Invoke-Metrics; Show-RunMetrics }
    "metrics-import" { Invoke-MetricsImport }
    "new-request" { $null = Invoke-NewRequest }
    "request-report" { Invoke-RequestReport }
    "staff-report" { Invoke-StaffReport }
    "log-run" { Invoke-LogRun }
    "try" { Invoke-Try }
    "stop" { Invoke-Stop }
    "review" { Invoke-ReviewPack }
    "merge" { Invoke-Merge }
    "approval-request" { Invoke-ApprovalRequest }
    "claude-decision" { $null = Add-ClaudeDecision -Kind $Kind -Title $Title -Task $Task -Request $Request -Detail $Detail }
    "cleanup" { Invoke-Cleanup }
    "switch-account" { Invoke-SwitchAccount }
    "hub" { Invoke-Hub -Restart:$Restart }
    default { Get-Help $PSCommandPath -Detailed | Out-String | Write-Host }
}
} catch {
    $message = if ($_.Exception -is [System.NotImplementedException]) { 'chưa làm' } else { $_.Exception.Message }
    [Console]::Error.WriteLine("LỖI: $message")
    exit 1
}
