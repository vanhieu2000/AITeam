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
  .\team.ps1 assign -Project Ten -Task T-003 [-Resume] [-Feedback "..."] [-NoVerify] [-Force]
                                                    Giao task (chờ tới khi agent xong). Ghi "đang chạy · máy X" lên bảng việc;
                                                    xong thì tự chạy cổng kiểm tra, không đạt thì tự giao lại 1 lần.
  .\team.ps1 verify -Project Ten -Task T-003        Cổng kiểm tra: commit/push, BOM (tự sửa), phạm vi, test nghiệm thu, build 0 warning, test
  .\team.ps1 metrics [-Project Ten]                 Số liệu review: số vòng, đạt vòng 1, Claude tự sửa, lỗi bảo mật / giao diện / BOM
  .\team.ps1 status [-Project Ten]                  Agent đang chạy, nhánh, kết quả lần chạy gần nhất
  .\team.ps1 cleanup -Project Ten -Task T-003       Xóa worktree sau khi đã merge
  .\team.ps1 switch-account                         Mở agy để /logout và đăng nhập tài khoản Antigravity khác
  .\team.ps1 ask -Project Ten -Prompt "..."          Việc CHỈ ĐỌC (review, phân tích, ý kiến thứ hai) — agy chạy --sandbox, không sửa file
  .\team.ps1 models                                 Danh sách model Antigravity dùng được
  .\team.ps1 install-hooks [-Project Ten]           Cài git hook chặn commit có bí mật (mặc định: mọi dự án)
  .\team.ps1 secret-set -Name DB_PASSWORD           Lưu bí mật vào kho CỤC BỘ (DPAPI, chỉ user Windows này giải mã được)
  .\team.ps1 secret-list | secret-remove -Name X    Xem tên (không xem giá trị) / xóa bí mật
  .\team.ps1 secret-env -Project Ten -Names A,B     Ghi .env.local (đã bị .gitignore) cho dự án chạy thử trên máy
  .\team.ps1 hub                                    Mở bảng điều khiển AITeam Hub (chạy ngầm)
#>
param(
    [Parameter(Position = 0)] [string] $Command = "help",
    [string] $Project,
    [string] $Task,
    [string] $Feedback = "",
    [switch] $Resume,
    [string] $Repo,
    [string] $Path,
    [switch] $New,
    [string] $Build = "",
    [string] $Test = "",
    [string] $Stack = "",
    [string] $Title = "",
    [string] $Agent = "A",
    [string] $Name = "",
    [string[]] $Names = @(),
    [string] $Prompt = "",
    [string] $Model = "",
    [switch] $NoVerify,
    [switch] $Force
)
$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Utf8 = New-Object Text.UTF8Encoding $false

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

function Invoke-Assign {
    $cfg = Get-Config
    $r = Resolve-Project $Project
    if (-not $Task) { throw "Thiếu -Task" }
    $taskFile = Get-ChildItem (Join-Path $r.Path "tasks") -Filter "$Task-*.md" | Select-Object -First 1
    if (-not $taskFile) { throw "Không thấy tasks\$Task-*.md trong $($r.Path)" }
    $text = Get-Content $taskFile.FullName -Raw -Encoding UTF8
    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $agentId = [regex]::Match($text, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
    if (-not $branch) { throw 'File task thiếu dòng **Nhánh:** `tên-nhánh`' }

    Assert-NotClaimedElsewhere $r $Task
    Set-BoardStatus $r $Task (Get-ClaimText)

    New-Item -ItemType Directory -Force $r.Logs | Out-Null
    $work = Join-Path $r.Work ("$agentId-$Task".ToLower())
    git -C $r.Path fetch origin --quiet
    if (-not (Test-Path $work)) {
        $remoteHas = git -C $r.Path ls-remote --heads origin $branch
        if ($remoteHas) { git -C $r.Path worktree add $work -B $branch "origin/$branch" --quiet }
        else { git -C $r.Path worktree add $work -B $branch origin/main --quiet }
    } else {
        git -C $work merge origin/main --no-edit --quiet 2>$null
    }

    $dev = $cfg.developers | Where-Object { $_.id -eq $agentId } | Select-Object -First 1
    $null = Set-ManagerIdentity $r.Path $cfg.github.owner
    Set-AgentIdentity $r.Path $work $dev $r.Info.name
    $prompt = @"
Bạn là $($dev.name) (chuyên: $($dev.focus)) trong team AI của dự án $($r.Info.name). Quản lý là Claude.
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
"@
    if ($Feedback) { $prompt += "`nYÊU CẦU CỦA QUẢN LÝ: $Feedback`nĐọc kỹ mục 'Review (Claude)' trong file task nếu có." }

    $n = @(Get-ChildItem $r.Logs -Filter "$Task-*.json" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'prompt' }).Count + 1
    $log = Join-Path $r.Logs "$Task-$n.json"
    # PowerShell 5.1 truyền sai tham số có dấu ngoặc kép / xuống dòng → lời giao việc nằm trong file, chỉ truyền 1 câu ngắn.
    $promptFile = Join-Path $r.Logs "$Task-$n.prompt.md"
    [IO.File]::WriteAllText($promptFile, $prompt, $Utf8)
    $short = "Doc file $promptFile (UTF-8, tieng Viet) va lam dung theo huong dan trong do."
    $argLine = "-p `"$short`" --output-format json --print-timeout 0 --dangerously-skip-permissions --add-dir `"$work`" --add-dir `"$($r.Logs)`""
    if ($cfg.agy.model) { $argLine += " --model `"$($cfg.agy.model)`"" }
    if ($Resume) {
        $last = Get-ChildItem $r.Logs -Filter "$Task-*.json" | Where-Object { $_.Length -gt 0 -and $_.Name -notmatch 'prompt' } |
            Sort-Object LastWriteTime | Select-Object -Last 1
        $id = if ($last) { [regex]::Match((Get-Content $last.FullName -Raw), '"conversation_id":"([^"]+)"').Groups[1].Value }
        if ($id) { $argLine += " --conversation $id"; Say "Tiếp tục hội thoại $id" }
    }

    Say "[$(Get-Date -Format HH:mm:ss)] $($r.Info.name) $Task → $($dev.name), nhánh $branch, thư mục $work" Cyan
    # Start-Process: agy in trạng thái ra stderr; gọi trực tiếp thì PowerShell 5.1 coi là lỗi và dừng script.
    $err = $log -replace '\.json$', '.stderr.txt'
    $proc = Start-Process -FilePath $cfg.agy.exe -ArgumentList $argLine -WorkingDirectory $work -NoNewWindow -Wait -PassThru -RedirectStandardInput (Get-EmptyInput) `
        -RedirectStandardOutput $log -RedirectStandardError $err
    $logText = Get-Content $log -Raw -ErrorAction SilentlyContinue
    $errText = Get-Content $err -Raw -ErrorAction SilentlyContinue
    Say "[$(Get-Date -Format HH:mm:ss)] $Task kết thúc, mã $($proc.ExitCode), log: $log" Cyan
    if (Test-QuotaExhausted $logText $errText) {
        Say "HẾT HẠN MỨC tài khoản Antigravity hiện tại → chạy .\team.ps1 switch-account rồi giao lại với -Resume" Yellow
    }
    if (Set-ManagerIdentity $r.Path $cfg.github.owner) {
        Say "Agent đã ghi đè tên commit chung của repo → đã trả lại HieuDV (agent chỉ dùng tên riêng của worktree)." Yellow
    }
    $pushed = git -C $r.Path ls-remote --heads origin $branch
    Say ("Nhánh trên GitHub: " + $(if ($pushed) { "đã push" } else { "CHƯA push" }))

    if (Test-QuotaExhausted $logText $errText) { Set-BoardStatus $r $Task "🟡 Dừng · hết hạn mức Antigravity"; return }
    if ($NoVerify) { Set-BoardStatus $r $Task "🔵 Chờ review"; return }
    $v = Invoke-Verify
    if (-not $v.Ok -and -not $script:VerifyRetried) {
        # Lỗi cơ học (chưa commit / build / test / BOM...) → trả agent sửa luôn 1 lần, không tốn lượt review của Claude.
        $script:VerifyRetried = $true
        Say "Cổng kiểm tra KHÔNG ĐẠT → tự giao lại cho $($dev.name) 1 lần kèm danh sách lỗi." Yellow
        $script:Resume = $true
        $script:Feedback = "Cổng kiểm tra tự động KHÔNG ĐẠT. Sửa hết các mục sau, chạy lại build + test, cập nhật báo cáo, commit và push:`n- " + ($v.Fails -join "`n- ")
        Invoke-Assign
        return
    }
    Set-BoardStatus $r $Task ("🔵 Chờ review · cổng kiểm tra " + $(if ($v.Ok) { "ĐẠT" } else { "KHÔNG ĐẠT" }))
}

# Agent hết hạn mức? Lần chạy báo SUCCESS thì không. Còn lại dò từ khóa, "429"/"quota" phải đứng riêng
# (trước đây "429" khớp nhầm vào số token như "thinking_tokens":42953).
function Test-QuotaExhausted([string]$logText, [string]$errText) {
    if ($logText -match '"status"\s*:\s*"SUCCESS"') { return $false }
    return ("$logText`n$errText") -match '(?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits'
}

# Repo chính luôn commit bằng tên chủ dự án. Đặt lại khi chưa có hoặc bị agent ghi đè (tên "Agent …").
# Trả về $true nếu đã phải sửa.
function Set-ManagerIdentity([string]$repoPath, [string]$owner) {
    $name = git -C $repoPath config --local user.name 2>$null
    if ($name -and $name -notmatch '^Agent\b') { return $false }
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

function Invoke-SwitchAccount {
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

function Invoke-Models { & (Get-Config).agy.exe models }

# ───────────────────────── Bảo mật: hook + kho bí mật cục bộ ─────────────────────────

function Install-Hook([string]$repoPath) {
    $common = git -C $repoPath rev-parse --git-common-dir
    if (-not [IO.Path]::IsPathRooted($common)) { $common = Join-Path $repoPath $common }
    $hooks = Join-Path $common "hooks"
    New-Item -ItemType Directory -Force $hooks | Out-Null
    $scan = (Join-Path $Root "tools\secret-scan.ps1") -replace '\\', '/'
    $hook = "#!/bin/sh`n# AITeam: chặn commit có bí mật`npowershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scan`" -Repo `"`$(git rev-parse --show-toplevel)`"`n"
    [IO.File]::WriteAllText((Join-Path $hooks "pre-commit"), $hook.Replace("`r", ""), $Utf8)
    Say "  [OK] Hook chặn bí mật: $repoPath" Green
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
    $hubUrl = "http://127.0.0.1:5057"
    if (Test-PortOpen "127.0.0.1" 5057) {
        Say "Hub đang chạy tại $hubUrl — đang mở trình duyệt." Green
        Start-Process $hubUrl
        return
    }

    $hubDir = Join-Path $Root "hub"
    $hubProj = Join-Path $hubDir "AITeam.Hub.csproj"
    $hubDll = Join-Path $hubDir "bin\Debug\net10.0-windows\AITeam.Hub.dll"

    $needBuild = $false
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

    $timeoutSeconds = 30
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

function Get-TaskContext {
    $r = Resolve-Project $Project
    if (-not $Task) { throw "Thiếu -Task" }
    $taskFile = Get-ChildItem (Join-Path $r.Path "tasks") -Filter "$Task-*.md" | Select-Object -First 1
    if (-not $taskFile) { throw "Không thấy tasks\$Task-*.md trong $($r.Path)" }
    $text = Get-Content $taskFile.FullName -Raw -Encoding UTF8
    $branch = [regex]::Match($text, '\*\*Nhánh:\*\*\s*`([^`]+)`').Groups[1].Value
    $agentId = [regex]::Match($text, '\*\*Agent:\*\*\s*(\S+)').Groups[1].Value
    [pscustomobject]@{
        R = $r; TaskFile = $taskFile; Text = $text; Branch = $branch; AgentId = $agentId
        Work = (Join-Path $r.Work ("$agentId-$Task".ToLower()))
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
    Push-Location $dir
    try {
        if ($env:OS -eq 'Windows_NT') { $out = cmd /c "$command 2>&1" | Out-String }
        else { $out = sh -c "$command 2>&1" | Out-String }
        return [pscustomobject]@{ Code = $LASTEXITCODE; Output = $out }
    } finally { Pop-Location }
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

    # 1. Agent đã commit hết chưa
    $dirty = @(git -C $work status --porcelain 2>$null | Where-Object { $_ })
    if ($dirty.Count -gt 0) {
        [void]$fails.Add("Agent CHƯA COMMIT $($dirty.Count) file: " + (($dirty | Select-Object -First 8 | ForEach-Object { $_.Trim() }) -join ', '))
    }

    # 2. BOM: file vốn có BOM phải giữ; .ps1 có chữ ngoài ASCII phải có BOM (PowerShell 5.1). Tự sửa + commit.
    $bomFixed = @()
    foreach ($f in $changed) {
        $full = Join-Path $work $f
        if (-not (Test-Path $full -PathType Leaf)) { continue }
        $bytes = [IO.File]::ReadAllBytes($full)
        if (Test-HasBom $bytes) { continue }
        $need = $false
        $old = Get-GitBlobHead $work $base $f
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

    # 3. Test nghiệm thu do Claude viết trước: phải còn nguyên
    $accept = Get-BacktickPaths (Get-Section $ctx.Text "Test nghiệm thu")
    foreach ($a in $accept) {
        if (-not (Test-Path (Join-Path $work $a))) { [void]$fails.Add("Thiếu file test nghiệm thu: $a"); continue }
        # Test nghiệm thu nằm sẵn trên main, hoặc là commit đầu tiên Claude đẩy lên nhánh task (vì tham chiếu lớp chưa có).
        $ref = $base
        git -C $work cat-file -e "${base}:$a" 2>$null
        if ($LASTEXITCODE -ne 0) { $ref = git -C $work log --reverse --diff-filter=A --format=%H "$base..HEAD" -- $a 2>$null | Select-Object -First 1 }
        if (-not $ref) { [void]$fails.Add("Không tìm thấy commit thêm test nghiệm thu: $a"); continue }
        git -C $work diff --quiet $ref HEAD -- $a 2>$null
        if ($LASTEXITCODE -ne 0) { [void]$fails.Add("Agent đã SỬA test nghiệm thu (không được phép): $a") }
    }
    if ($accept.Count -gt 0 -and -not ($fails | Where-Object { $_ -match 'test nghiệm thu' })) { [void]$notes.Add("Test nghiệm thu còn nguyên: $($accept.Count) file") }

    # 4. Phạm vi
    $scope = Get-BacktickPaths (Get-Section $ctx.Text "Phạm vi")
    if ($scope.Count -gt 0) {
        $always = @("tasks/$($ctx.TaskFile.Name)", "docs/screenshots/$Task", "CHANGELOG.md") + $accept
        $out = @($changed | Where-Object { -not (Test-PathInScope $_ ($scope + $always)) })
        if ($out.Count -gt 0) { [void]$warns.Add("Sửa ngoài phạm vi task ($($out.Count) file): " + (($out | Select-Object -First 10) -join ', ')) }
    } else { [void]$notes.Add("Task không ghi đường dẫn trong mục Phạm vi → bỏ qua kiểm tra phạm vi.") }

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
    if ($r.Info.build) {
        $b = Invoke-ShellCapture $r.Info.build $work
        $w = @([regex]::Matches($b.Output, '(?m)^.*: warning [A-Za-z]+\d+:.*$') | ForEach-Object { ($_.Value -replace '\s*\[[^\]]+\]\s*$', '').Trim() } | Select-Object -Unique)
        if ($b.Code -ne 0) {
            $errs = @([regex]::Matches($b.Output, '(?m)^.*: error [A-Za-z]+\d+:.*$') | ForEach-Object { $_.Value.Trim() } | Select-Object -Unique -First 10)
            [void]$fails.Add("Build LỖI (mã $($b.Code)):`n  " + ($errs -join "`n  "))
        } elseif ($w.Count -gt 0) {
            [void]$fails.Add("Build có $($w.Count) warning (yêu cầu 0):`n  " + (($w | Select-Object -First 10) -join "`n  "))
        } else { [void]$notes.Add("Build: OK, 0 warning") }
    }
    if ($r.Info.test -and -not ($fails | Where-Object { $_ -like 'Build LỖI*' })) {
        $t = Invoke-ShellCapture $r.Info.test $work
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
    $n = @(Get-ChildItem $r.Logs -Filter "$Task-verify-*.md" -ErrorAction SilentlyContinue).Count + 1
    $reportPath = Join-Path $r.Logs "$Task-verify-$n.md"
    $md = @("# Cổng kiểm tra $Task — $(if ($ok) { 'ĐẠT' } else { 'KHÔNG ĐẠT' })", "", "- Máy: $env:COMPUTERNAME · $(Get-Date -Format 'HH:mm dd/MM/yyyy')", "- Nhánh: ``$branch``", "")
    if ($fails.Count) { $md += "## Phải sửa"; $fails | ForEach-Object { $md += "- $_" }; $md += "" }
    if ($warns.Count) { $md += "## Cảnh báo (Claude xem khi review)"; $warns | ForEach-Object { $md += "- $_" }; $md += "" }
    $md += "## Đã kiểm tra"; $notes | ForEach-Object { $md += "- $_" }
    [IO.File]::WriteAllText($reportPath, ($md -join "`r`n"), $Utf8)

    foreach ($f in $fails) { Say "  [PHẢI SỬA] $f" Red }
    foreach ($w in $warns) { Say "  [CẢNH BÁO] $w" Yellow }
    foreach ($x in $notes) { Say "  [OK] $x" Green }
    Say ("Cổng kiểm tra: " + $(if ($ok) { "ĐẠT → Claude review nội dung" } else { "KHÔNG ĐẠT" }) + "  ($reportPath)") $(if ($ok) { "Green" } else { "Red" })
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
function Set-BoardStatus($r, [string]$taskId, [string]$status) {
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
        for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^\|' -and $lines[$i] -match ("(\[|\|\s*)" + [regex]::Escape($taskId) + "\b")) { $idx = $i; break } }
        if ($idx -lt 0) { Say "  (Bảng việc chưa có dòng $taskId — không cập nhật.)" Yellow; return }
        $cells = $lines[$idx].TrimEnd().TrimEnd('|') -split '\|'
        $cells[$cells.Count - 1] = " $status "
        $newLine = ($cells -join '|') + '|'
        if ($newLine -eq $lines[$idx]) { return }
        $lines[$idx] = $newLine
        $nl = if ($text -match "`r`n") { "`r`n" } else { "`n" }
        $bom = Test-HasBom ([IO.File]::ReadAllBytes($board))
        [IO.File]::WriteAllText($board, ($lines -join $nl), (New-Object Text.UTF8Encoding $bom))
        git -C $r.Path add -- $boardRel
        git -C $r.Path commit -q -m "Bảng việc: $taskId $status" -- $boardRel 2>$null
        git -C $r.Path push -q origin main 2>$null
        if ($LASTEXITCODE -eq 0) { return }
        git -C $r.Path reset -q --hard origin/main 2>$null   # bị máy khác push trước → làm lại trên bản mới
    }
    Say "  Không push được bảng việc (máy khác vừa đổi?). Cập nhật tay sau." Yellow
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

switch ($Command) {
    "setup" { Invoke-Setup; Install-Skill; Invoke-InstallHooks }
    "ask" { Invoke-Ask }
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
    "status" { Invoke-Status }
    "verify" { $null = Invoke-Verify }
    "metrics" { Invoke-Metrics }
    "cleanup" { Invoke-Cleanup }
    "switch-account" { Invoke-SwitchAccount }
    "hub" { Invoke-Hub }
    default { Get-Help $PSCommandPath -Detailed | Out-String | Write-Host }
}
