# /// <summary>
# /// Nối chỉ dẫn skill vào Prompt, giữ nguyên toàn bộ nội dung cũ và không đọc nội dung skill.
# /// Root là gốc AITeam tuyệt đối, không lấy từ thư mục dự án hoặc thư mục hiện hành.
# /// Stage: plan → 03-research, 04-ke-hoach; prep → 06-chuan-bi-task, code-test;
# /// code → 07-code, code-test. Sau đó thêm skill stack theo thứ tự winforms, web, app.
# /// Stack không phân biệt hoa thường: WinForms; ASP.NET hoặc Blazor; MAUI, console hoặc CLI.
# /// Stack null/rỗng/lạ không thêm skill stack; hỗn hợp thêm đủ, không lặp code-app.
# /// Chỉ phát đường dẫn tuyệt đối tới file skills/<id>/SKILL.md tồn tại, không reparse point
# /// ở skills, thư mục id hoặc file. Thiếu file bỏ qua; không có file hợp lệ trả nguyên Prompt.
# /// Mỗi file một dòng: Đọc skill: `đường dẫn tuyệt đối`. Không lặp đường dẫn kể cả gọi lại.
# /// Khi có skill, thêm đúng một câu: Được phép đọc các skill được liệt kê dù danh sách file cũ bị giới hạn.
# /// Không thay đổi quyền sandbox, không thực thi Stack, không nhúng Stack vào prompt.
# /// Stage sai ném ArgumentException("Bước skill không hợp lệ."); Root không tuyệt đối
# /// ném ArgumentException("Gốc AITeam phải là đường dẫn tuyệt đối.").
# /// </summary>

function Test-SkillReparsePoint {
    param([string]$Path)
    if ([string]::IsNullOrEmpty($Path)) { return $false }
    try {
        if ([System.IO.File]::Exists($Path) -or [System.IO.Directory]::Exists($Path)) {
            $attr = [System.IO.File]::GetAttributes($Path)
            return $attr.HasFlag([System.IO.FileAttributes]::ReparsePoint)
        }
    } catch {
        return $true
    }
    return $false
}

function Add-SkillPrompt {
    param(
        [AllowEmptyString()][string] $Prompt,
        [string] $Root,
        [string] $Stage,
        [AllowNull()][AllowEmptyString()][string] $Stack
    )

    if ($Stage -notin @('plan', 'prep', 'code')) {
        throw [System.ArgumentException]::new("Bước skill không hợp lệ.")
    }

    if ([string]::IsNullOrWhiteSpace($Root) -or -not [System.IO.Path]::IsPathRooted($Root) -or ($Root -notmatch '^([a-zA-Z]:[\\/]|\\\\)')) {
        throw [System.ArgumentException]::new("Gốc AITeam phải là đường dẫn tuyệt đối.")
    }

    $stageSkills = switch ($Stage) {
        'plan' { @('03-research', '04-ke-hoach') }
        'prep' { @('06-chuan-bi-task', 'code-test') }
        'code' { @('07-code', 'code-test') }
    }

    $stackSkills = @()
    if (-not [string]::IsNullOrWhiteSpace($Stack)) {
        if ($Stack -match '(?i)\bwinforms\b') {
            $stackSkills += 'code-winforms'
        }
        if ($Stack -match '(?i)(asp\.net|\bblazor\b)') {
            $stackSkills += 'code-web'
        }
        if ($Stack -match '(?i)\b(maui|console|cli)\b') {
            $stackSkills += 'code-app'
        }
    }

    $candidateIds = @()
    foreach ($id in ($stageSkills + $stackSkills)) {
        if ($candidateIds -notcontains $id) {
            $candidateIds += $id
        }
    }

    $skillsDir = [System.IO.Path]::Combine($Root, "skills")
    if (-not [System.IO.Directory]::Exists($skillsDir) -or (Test-SkillReparsePoint $skillsDir)) {
        return $Prompt
    }

    $validFiles = @()
    foreach ($id in $candidateIds) {
        $idDir = [System.IO.Path]::Combine($skillsDir, $id)
        if (-not [System.IO.Directory]::Exists($idDir) -or (Test-SkillReparsePoint $idDir)) {
            continue
        }
        $skillFile = [System.IO.Path]::Combine($idDir, "SKILL.md")
        if (-not [System.IO.File]::Exists($skillFile) -or (Test-SkillReparsePoint $skillFile)) {
            continue
        }
        try {
            $attr = [System.IO.File]::GetAttributes($skillFile)
            if ($attr.HasFlag([System.IO.FileAttributes]::Directory)) {
                continue
            }
        } catch {
            continue
        }
        $validFiles += $skillFile
    }

    if ($validFiles.Count -eq 0) {
        return $Prompt
    }

    $linesToAdd = @()
    foreach ($f in $validFiles) {
        if (-not $Prompt.Contains($f)) {
            $linesToAdd += "Đọc skill: " + [char]96 + $f + [char]96
        }
    }

    $permission = "Được phép đọc các skill được liệt kê dù danh sách file cũ bị giới hạn."
    if (-not $Prompt.Contains($permission)) {
        $linesToAdd += $permission
    }

    if ($linesToAdd.Count -eq 0) {
        return $Prompt
    }

    $extra = $linesToAdd -join "`n"
    if ([string]::IsNullOrEmpty($Prompt)) {
        return $extra
    } elseif ($Prompt.EndsWith("`n")) {
        return $Prompt + $extra
    } else {
        return $Prompt + "`n" + $extra
    }
}