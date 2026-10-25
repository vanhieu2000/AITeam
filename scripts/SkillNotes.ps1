# /// <summary>
# /// Ghi bài học vào skills/Skill/SKILL.md dưới Root tuyệt đối của repo AITeam.
# /// Skill chỉ gồm chữ ASCII, số, gạch ngang/gạch dưới; cấm tên DOS (cả phần mở rộng,
# /// khác hoa thường), dấu chấm, dấu phân cách, đường dẫn tuyệt đối và reparse point
# /// tại Root/skills/thư mục skill/file. Sai: "Skill không hợp lệ.".
# /// Thiếu file/mục: "Không tìm thấy skill hoặc mục Lỗi hay gặp.".
# /// Text một dòng không trắng, tối đa 1000 ký tự trước chuẩn hóa; Source tùy chọn
# /// tối đa 100 ký tự; cấm CR/LF cả hai. Sai: "Nội dung ghi chú không hợp lệ.".
# /// Text trim/gộp whitespace thành dấu cách; Source trim, trắng coi như thiếu.
# /// Bullet: "- Text" hoặc "- Text (Nguồn: Source)". So trùng Text ordinal có phân
# /// biệt hoa thường trong mục ## Lỗi hay gặp (cho phép hậu tố tiêu đề), bỏ nguồn
# /// bullet cũ. Trùng khác Source không ghi/commit, trả "Bài học đã tồn tại.".
# /// Chèn cuối mục trước tiêu đề cấp 1/2 kế tiếp; mục cuối chèn cuối file. Giữ BOM,
# /// CRLF/LF, byte ngoài vị trí chèn và trạng thái có/không newline cuối file.
# /// Kiểm git worktree/index đúng file trước mọi ghi, kể cả trùng; file bẩn:
# /// "File skill đang có thay đổi chưa commit.". Chỉ stage/commit đúng file với
# /// pathspec, giữ nguyên file khác đã stage/chưa stage. Không push/reset/clean/stash.
# /// Git timeout hữu hạn; kiểm exit code từng lệnh. Git lỗi ném thông điệp
# /// "Không thể ghi hoặc commit bài học."; không báo thành công/xóa dữ liệu người dùng.
# /// Thành công trả "Đã ghi và commit bài học.". Exception không có tiền tố;
# /// team.ps1 bắt mọi lỗi, in stderr "LỖI: …", exit 1 (khung: "LỖI: chưa làm").
# /// </summary>
function Invoke-SkillNote {
    param(
        [string] $Root,
        [AllowNull()][AllowEmptyString()][string] $Skill,
        [AllowNull()][AllowEmptyString()][string] $Text,
        [AllowNull()][AllowEmptyString()][string] $Source = ""
    )

    # ── 1. Kiểm ID skill ──
    if ([string]::IsNullOrWhiteSpace($Skill)) {
        throw "Skill không hợp lệ."
    }
    # Chỉ ASCII chữ/số/gạch ngang/gạch dưới
    if ($Skill -notmatch '^[a-zA-Z0-9_-]+$') {
        throw "Skill không hợp lệ."
    }
    # Chặn tên thiết bị DOS (CON, PRN, AUX, NUL, COM1-9, LPT1-9) — cả phần mở rộng và khác hoa thường
    $baseName = $Skill
    if ($baseName -match '\.') { $baseName = $baseName.Substring(0, $baseName.IndexOf('.')) }
    $dosNames = @('CON','PRN','AUX','NUL',
                  'COM1','COM2','COM3','COM4','COM5','COM6','COM7','COM8','COM9',
                  'LPT1','LPT2','LPT3','LPT4','LPT5','LPT6','LPT7','LPT8','LPT9')
    if ($dosNames -contains $baseName.ToUpperInvariant()) {
        throw "Skill không hợp lệ."
    }

    # Chuẩn hóa gốc và kiểm mọi thư mục cha trước khi truy cập skill.
    try {
        if (-not [IO.Path]::IsPathRooted($Root)) { throw 'Gốc không tuyệt đối.' }
        $Root = [IO.Path]::GetFullPath($Root)
        $parent = $Root
        while (-not [string]::IsNullOrEmpty($parent)) {
            if (Test-SkillReparsePoint $parent) { throw 'Liên kết không hợp lệ.' }
            $parent = [IO.Path]::GetDirectoryName($parent)
        }
    } catch { throw "Skill không hợp lệ." }

    # Kiểm reparse point (dùng hàm từ SkillPrompts.ps1 đã dot-source)
    $skillsDir = [System.IO.Path]::Combine($Root, "skills")
    if (Test-SkillReparsePoint $skillsDir) {
        throw "Skill không hợp lệ."
    }
    $skillDir = [System.IO.Path]::Combine($skillsDir, $Skill)
    if (Test-SkillReparsePoint $skillDir) {
        throw "Skill không hợp lệ."
    }
    $skillFile = [System.IO.Path]::Combine($skillDir, "SKILL.md")
    if (Test-SkillReparsePoint $skillFile) {
        throw "Skill không hợp lệ."
    }

    # ── 2. Kiểm nội dung và nguồn ──
    if ([string]::IsNullOrWhiteSpace($Text)) {
        throw "Nội dung ghi chú không hợp lệ."
    }
    if ($Text.Contains("`r") -or $Text.Contains("`n")) {
        throw "Nội dung ghi chú không hợp lệ."
    }
    if ($Text.Length -gt 1000) {
        throw "Nội dung ghi chú không hợp lệ."
    }
    if (-not [string]::IsNullOrEmpty($Source)) {
        if ($Source.Contains("`r") -or $Source.Contains("`n")) {
            throw "Nội dung ghi chú không hợp lệ."
        }
        if ($Source.Length -gt 100) {
            throw "Nội dung ghi chú không hợp lệ."
        }
    }

    # ── 3. Chuẩn hóa ──
    $Text = ($Text.Trim() -replace '\s+', ' ')
    $Source = if ([string]::IsNullOrWhiteSpace($Source)) { "" } else { $Source.Trim() }

    # ── 4. Đọc file skill ──
    if (-not [System.IO.File]::Exists($skillFile)) {
        throw "Không tìm thấy skill hoặc mục Lỗi hay gặp."
    }
    try { $rawBytes = [System.IO.File]::ReadAllBytes($skillFile) }
    catch { throw "Không thể ghi hoặc commit bài học." }
    $hasBom = ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF)
    $content = if ($hasBom) {
        [System.Text.Encoding]::UTF8.GetString($rawBytes, 3, $rawBytes.Length - 3)
    } else {
        [System.Text.Encoding]::UTF8.GetString($rawBytes)
    }

    # Phát hiện CRLF/LF
    $nl = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

    # ── 5. Tìm mục "Lỗi hay gặp" ──
    # Tiêu đề cấp 2 bắt đầu bằng "Lỗi hay gặp", cho phép hậu tố
    $lines = $content.Split("`n")
    $sectionStart = -1
    $sectionEnd = -1
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i].TrimEnd("`r")
        if ($sectionStart -lt 0 -and $line -cmatch '^## Lỗi hay gặp') {
            $sectionStart = $i
        } elseif ($sectionStart -ge 0 -and $sectionEnd -lt 0 -and $line -match '^#{1,2}\s') {
            $sectionEnd = $i
            break
        }
    }
    if ($sectionStart -lt 0) {
        throw "Không tìm thấy skill hoặc mục Lỗi hay gặp."
    }
    # Mục cuối: chèn cuối file
    if ($sectionEnd -lt 0) { $sectionEnd = $lines.Length }

    # ── 7. Kiểm git clean (worktree + index) cho đúng file ──
    $relPath = "skills/$Skill/SKILL.md"
    $gitTimeout = 30
    # Kiểm worktree
    # Hàm chạy git với đối số riêng (không qua shell), timeout hữu hạn, kiểm exit code
    function Invoke-GitCmd {
        param([string[]]$GitArgs, [string]$ErrorMessage)
        $info = New-Object System.Diagnostics.ProcessStartInfo
        $info.FileName = "git"
        $info.WorkingDirectory = $Root
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        # .NET Framework của PowerShell 5.1 chưa có ArgumentList.
        # Mã hóa từng đối số theo quy tắc argv Windows, không thông qua shell.
        $quoted = foreach ($argument in $GitArgs) {
            '"' + (($argument -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
        }
        $info.Arguments = $quoted -join ' '
        $info.StandardOutputEncoding = [Text.Encoding]::UTF8
        $info.StandardErrorEncoding = [Text.Encoding]::UTF8
        $info.EnvironmentVariables['GIT_TERMINAL_PROMPT'] = '0'
        $proc = New-Object System.Diagnostics.Process
        $proc.StartInfo = $info
        try {
            $null = $proc.Start()
            $stdout = $proc.StandardOutput.ReadToEndAsync()
            $stderr = $proc.StandardError.ReadToEndAsync()
            if (-not $proc.WaitForExit($gitTimeout * 1000)) {
                $proc.Kill()
                throw $ErrorMessage
            }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 1000)) {
                throw $ErrorMessage
            }
            if ($proc.ExitCode -ne 0) { throw $ErrorMessage }
            return [pscustomobject]@{ Code = $proc.ExitCode; Output = $stdout.Result; Error = $stderr.Result }
        } catch { throw $ErrorMessage }
        finally { $proc.Dispose() }
    }
    # Kiểm git status đúng file (worktree + index)
    $statusResult = Invoke-GitCmd -GitArgs @("status", "--porcelain", "--", $relPath) -ErrorMessage "Không thể ghi hoặc commit bài học."
    if ($statusResult.Code -ne 0) {
        throw "Không thể ghi hoặc commit bài học."
    }
    if ($statusResult.Output.Trim().Length -gt 0) {
        throw "File skill đang có thay đổi chưa commit."
    }


    # ── 6. Chống trùng (ordinal, phân biệt hoa thường, chỉ trong mục) ──
    for ($i = $sectionStart + 1; $i -lt $sectionEnd; $i++) {
        $line = $lines[$i].TrimEnd("`r")
        if ($line.StartsWith("- ", [System.StringComparison]::Ordinal)) {
            $bulletText = $line.Substring(2)
            # Bỏ hậu tố nguồn " (Nguồn: …)" nếu có
            $sourceIdx = $bulletText.LastIndexOf(" (Nguồn: ", [StringComparison]::Ordinal)
            if ($sourceIdx -ge 0) {
                $closeParen = $bulletText.LastIndexOf(")", [StringComparison]::Ordinal)
                if ($closeParen -eq $bulletText.Length - 1) {
                    $bulletText = $bulletText.Substring(0, $sourceIdx)
                }
            }
            $bulletText = ($bulletText.Trim() -replace '\s+', ' ')
            if ([string]::Equals($bulletText, $Text, [System.StringComparison]::Ordinal)) {
                Write-Output "Bài học đã tồn tại."
                return
            }
        }
    }

    # ── 8. Tạo bullet và chèn ──
    $bullet = "- $Text"
    if ($Source.Length -gt 0) {
        $bullet = "- $Text (Nguồn: $Source)"
    }

    # Chèn byte mới, giữ nguyên tất cả byte cũ và newline cuối tài liệu.
    $pos = $content.Length
    $insertion = $bullet + $nl
    if ($sectionEnd -lt $lines.Length) {
        $pos = 0
        for ($i = 0; $i -lt $sectionEnd; $i++) { $pos += $lines[$i].Length + 1 }
    } elseif (-not $content.EndsWith("`n", [StringComparison]::Ordinal)) {
        $insertion = $nl + $bullet
    }
    $offset = [Text.Encoding]::UTF8.GetByteCount($content.Substring(0, $pos))
    if ($hasBom) { $offset += 3 }
    $insertBytes = [Text.Encoding]::UTF8.GetBytes($insertion)
    $updated = New-Object byte[] ($rawBytes.Length + $insertBytes.Length)
    [Array]::Copy($rawBytes, 0, $updated, 0, $offset)
    [Array]::Copy($insertBytes, 0, $updated, $offset, $insertBytes.Length)
    [Array]::Copy($rawBytes, $offset, $updated, $offset + $insertBytes.Length, $rawBytes.Length - $offset)
    try { [IO.File]::WriteAllBytes($skillFile, $updated) }
    catch { throw "Không thể ghi hoặc commit bài học." }
    # ── 9. Git: stage chỉ file skill, commit ──
    $addResult = Invoke-GitCmd -GitArgs @("add", "--", $relPath) -ErrorMessage "Không thể ghi hoặc commit bài học."
    if ($addResult.Code -ne 0) {
        throw "Không thể ghi hoặc commit bài học."
    }

    $commitResult = Invoke-GitCmd -GitArgs @("commit", "--only", "-m", "skill-note: $Skill", "--", $relPath) -ErrorMessage "Không thể ghi hoặc commit bài học."
    if ($commitResult.Code -ne 0) {
        throw "Không thể ghi hoặc commit bài học."
    }

    Write-Output "Đã ghi và commit bài học."
}
