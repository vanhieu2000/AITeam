<#
  Tạo / cập nhật bản chạy thử: publish bản đang có trên máy (thường là main đã merge) ra thư mục ban-chay-thu\ rồi mở app.
  Chủ dự án: bấm đúp CHAY-THU.cmd ở thư mục gốc dự án. Claude: chạy với -NoLaunch sau mỗi lần merge.
  Không đụng việc agent đang làm (agent làm ở thư mục riêng).
#>
param([switch] $NoLaunch, [switch] $NoPull)
$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
$app = Get-ChildItem (Join-Path $root "src") -Recurse -Filter "*.App.csproj" | Select-Object -First 1
if (-not $app) { Write-Host "Không tìm thấy project *.App.csproj trong src\" -ForegroundColor Red; exit 1 }
$out = Join-Path $root "ban-chay-thu"
$exeName = [IO.Path]::GetFileNameWithoutExtension($app.Name) -replace '\.App$', ''

# App đang mở từ thư mục này thì không ghi đè được
$running = @(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($out, [StringComparison]::OrdinalIgnoreCase) })
if ($running) {
    Write-Host "Bản chạy thử đang mở ($($running[0].ProcessName)). Đóng app rồi bấm lại CHAY-THU.cmd." -ForegroundColor Yellow
    if (-not $NoLaunch) { Read-Host "Nhấn Enter để thoát" | Out-Null }
    exit 2
}

if (-not $NoPull) {
    $dirty = @(git -C $root status --porcelain 2>$null | Where-Object { $_ })
    if ($dirty.Count -eq 0) { git -C $root pull -q 2>$null } else { Write-Host "Có thay đổi chưa commit → không lấy bản mới từ GitHub, build bản hiện có." -ForegroundColor Yellow }
}
$commit = git -C $root log -1 --format="%h · %ad · %s" --date=format:"%d/%m/%Y %H:%M" 2>$null
Write-Host "Đang tạo bản chạy thử: $commit" -ForegroundColor Cyan

$tmp = Join-Path $root "ban-chay-thu.tmp"
if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
dotnet publish $app.FullName -c Release -o $tmp -nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Host "Build LỖI — bản chạy thử cũ vẫn giữ nguyên." -ForegroundColor Red; if (-not $NoLaunch) { Read-Host "Nhấn Enter để thoát" | Out-Null }; exit 1 }
# Giữ dữ liệu tạm của bản chạy thử (Data\, portable.flag) nếu có
foreach ($keep in "Data", "portable.flag") { if (Test-Path (Join-Path $out $keep)) { Move-Item (Join-Path $out $keep) (Join-Path $tmp $keep) -Force } }
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
Move-Item $tmp $out
Get-ChildItem $out -Filter *.pdb -Recurse | Remove-Item -Force
[IO.File]::WriteAllText((Join-Path $out "PHIEN-BAN.txt"), "Bản chạy thử`r`nCommit: $commit`r`nTạo lúc: $(Get-Date -Format 'HH:mm dd/MM/yyyy') trên máy $env:COMPUTERNAME`r`n", (New-Object Text.UTF8Encoding $true))
$exe = Join-Path $out "$exeName.exe"
Write-Host "Xong: $exe" -ForegroundColor Green
if (-not $NoLaunch) { Start-Process $exe -WorkingDirectory $out }