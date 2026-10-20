param([Parameter(Mandatory)] [string] $Title, [string[]] $Texts = @(), [string] $Click = "", [string[]] $Toggle = @(), [string] $First = "")
# Điều khiển cửa sổ desktop bằng UI Automation khi Claude review: bấm nút theo tên, tick ô, điền các ô Edit theo thứ tự.
# CHỈ dùng với app đang phát triển chạy bằng dữ liệu tạm.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
$win = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond) | Where-Object { $_.Current.Name -like "*$Title*" } | Select-Object -First 1
if (-not $win) { Write-Output "NOWINDOW"; exit 1 }
function Find-Named($name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $e = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
    if (-not $e) { Write-Output "NOTFOUND $name"; exit 1 }
    return $e
}
if ($First) { (Find-Named $First).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 500 }
foreach ($t in $Toggle) { (Find-Named $t).GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
$editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$edits = @($win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond))
for ($i = 0; $i -lt $Texts.Count -and $i -lt $edits.Count; $i++) {
    if ($Texts[$i] -eq "-") { continue }
    $edits[$i].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Texts[$i])
}
if ($Click) { (Find-Named $Click).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
Write-Output "OK edits=$($edits.Count)"
