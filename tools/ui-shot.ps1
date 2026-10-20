param([Parameter(Mandatory)] [string] $Out, [string] $Title = "")
# Chụp cửa sổ có tiêu đề chứa $Title bằng PrintWindow (không cần đưa lên trước) → PNG. Dùng khi Claude review giao diện desktop.
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices; using System.Text;
public static class UiShotW {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr p);
  public struct RECT { public int L, T, R, B; }
  public static IntPtr Find(string part) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, p) => { if (!IsWindowVisible(h)) return true; var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
      if (sb.ToString().Contains(part)) { found = h; return false; } return true; }, IntPtr.Zero);
    return found;
  }
}
"@
[UiShotW]::SetProcessDPIAware() | Out-Null
$h = [UiShotW]::Find($Title)
if ($h -eq [IntPtr]::Zero) { Write-Output "NOWINDOW"; exit 1 }
$r = New-Object UiShotW+RECT
[UiShotW]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.R - $r.L; $ht = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[UiShotW]::PrintWindow($h, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "${w}x${ht}"
