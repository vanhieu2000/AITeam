<#
.SYNOPSIS
  Chặn bí mật lọt lên git. Dùng làm git pre-commit hook (team.ps1 cài cho mọi dự án) và khi Claude review trước khi merge.

.EXAMPLE
  powershell -File secret-scan.ps1 -Repo D:\HDVTool              # quét file đang staged (pre-commit)
  powershell -File secret-scan.ps1 -Repo D:\HDVTool -Range origin/main..HEAD   # quét các commit của một nhánh
#>
param(
    [string] $Repo = ".",
    [string] $Range = ""
)
$ErrorActionPreference = "Stop"

# Tên file không bao giờ được commit
$badNames = '(^|/)(\.env(\.[^/]*)?|secrets?\.[^/]+|[^/]*\.(pem|pfx|p12|key|lic|kdbx)|id_(rsa|ed25519)[^/]*|signing-key\.dat|vault\.dat)$'
# Chỉ miễn mẫu cấu hình (.env.example, templates/*.env*). KHÔNG BAO GIỜ miễn file khóa / chứng chỉ, kể cả trong templates/ (Codex review 23/10).
$allowNames = '(^|/)\.env\.example$|(^|/)templates/[^/]*\.env[^/]*$'

# Nội dung giống bí mật (khóa API, token, private key, mật khẩu ghi thẳng)
$patterns = [ordered]@{
    "Private key"            = '-----BEGIN [A-Z ]*PRIVATE KEY-----'
    "GitHub token"           = '\b(gh[opusr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b'
    "Google API key"         = '\bAIza[0-9A-Za-z_\-]{35}\b'
    "OpenAI / Anthropic key" = '\bsk-(ant-)?[A-Za-z0-9_\-]{30,}\b'
    "Groq key"               = '\bgsk_[A-Za-z0-9]{30,}\b'
    "AWS key"                = '\bAKIA[0-9A-Z]{16}\b'
    "Slack token"            = '\bxox[baprs]-[A-Za-z0-9-]{10,}\b'
    "Mật khẩu ghi thẳng"     = '(?i)\b(password|passwd|pwd|mat_?khau|secret|api_?key|token)\b\s*[:=]\s*["''][^"''\s{}$]{6,}["'']'
    "Chuỗi kết nối có mật khẩu" = '(?i)(password|pwd)\s*=\s*[^;"''\s{}$]{4,}\s*;'
}

Push-Location $Repo
try {
    if ($Range) {
        $files = git diff --name-only --diff-filter=ACMR $Range
        $diff = git diff --diff-filter=ACMR -U0 $Range
    } else {
        $files = git diff --cached --name-only --diff-filter=ACMR
        $diff = git diff --cached --diff-filter=ACMR -U0
    }
    $problems = @()
    foreach ($f in $files) {
        if ($f -match $badNames -and $f -notmatch $allowNames) { $problems += "File bí mật: $f" }
    }
    $current = ""
    foreach ($line in $diff) {
        if ($line -match '^\+\+\+ b/(.+)$') { $current = $Matches[1]; continue }
        if (-not $line.StartsWith('+') -or $line.StartsWith('+++')) { continue }
        $fakeOk = $current -match '(^|/)(tests?|templates)/'
        foreach ($name in $patterns.Keys) {
            if ($line -match $patterns[$name]) {
                # Trong tests/ hoặc templates/ chỉ bỏ qua khi CHÍNH chuỗi khớp là giá trị giả, không bỏ qua cả dòng (Codex review 23/10).
                if ($fakeOk -and $Matches[0] -match '(?i)(test|fake|example|matkhau\d|xxxx)') { continue }
                $shown = $line.Substring(1).Trim()
                if ($shown.Length -gt 80) { $shown = $shown.Substring(0, 80) + "…" }
                $problems += "${name}: $current → $shown"
            }
        }
    }
    if ($problems.Count -gt 0) {
        Write-Host "CHẶN COMMIT: phát hiện thứ giống bí mật" -ForegroundColor Red
        $problems | Select-Object -Unique | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        Write-Host "Bí mật để trong kho cục bộ: D:\AITeam\team.ps1 secret-set -Name <tên>  (hoặc file .env.local đã bị .gitignore)." -ForegroundColor Yellow
        Write-Host "Nếu chắc chắn là dữ liệu giả để test, đổi giá trị cho rõ (ví dụ chứa chữ 'test' / 'fake')." -ForegroundColor Yellow
        exit 1
    }
    exit 0
} finally { Pop-Location }
