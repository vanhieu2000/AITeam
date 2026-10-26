using System.Diagnostics;
using System.Text;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceV810HubRestartTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Restart_selects_only_local_hub_and_builds_even_with_existing_dll(bool running)
    {
        using var box = new TeamScript();
        var r = await box.RunAsync("team.ps1", ["Invoke-Hub"], $$"""
            function Say { param($Text, $Color) }
            $script:stopped = @(); $script:started = @(); $script:ports = 0
            function Get-CimInstance { param($Class)
                if (${{running.ToString().ToLowerInvariant()}} -eq $false) { return }
                [pscustomobject]@{ProcessId=101; ExecutablePath="$Root\hub\bin\Debug\AITeam.Hub.exe"; CommandLine=''}
                [pscustomobject]@{ProcessId=102; ExecutablePath='C:\dotnet.exe'; CommandLine="dotnet `"$Root\hub\bin\Debug\AITeam.Hub.dll`""}
                [pscustomobject]@{ProcessId=103; ExecutablePath="$Root-other\hub\bin\AITeam.Hub.exe"; CommandLine="dotnet `"$Root-other\hub\bin\AITeam.Hub.dll`""}
                [pscustomobject]@{ProcessId=104; ExecutablePath="$Root\hub\binary\AITeam.Hub.exe"; CommandLine="dotnet `"$Root\hub\bin\AITeam.Hub.dll.extra`""}
            }
            function Stop-Process { param($Id, [switch]$Force, $ErrorAction) $script:stopped += $Id }
            function Test-PortOpen { param($HostName, $Port) $script:ports++; return $script:ports -ge 3 }
            function Start-Process { param($FilePath, $ArgumentList, $WorkingDirectory, $WindowStyle) $script:started += $FilePath }
            function dotnet { $global:LASTEXITCODE=0; Write-Output "BUILD:$args" }
            $dll = Join-Path $Root 'hub\bin\Debug\net10.0-windows\AITeam.Hub.dll'
            $null = New-Item -ItemType Directory -Force (Split-Path $dll)
            [IO.File]::WriteAllText($dll, '')
            Invoke-Hub -Restart
            Write-Output "STOP:$($script:stopped -join ',')"
            Write-Output "START:$($script:started -join ',')"
            """);
        Assert.True(r.Process.Code == 0, r.Process.Error + r.Process.Output);
        Assert.Contains(running ? "STOP:101,102" : "STOP:\r\n", r.Process.Output);
        Assert.Contains("BUILD:build", r.Process.Output);
        Assert.Contains("START:dotnet,http://127.0.0.1:5057", r.Process.Output);
    }

    [Fact]
    public async Task Deputy_timeout_kills_fake_process_tree_and_writes_journal()
    {
        using var box = new TeamScript();
        var journal = Path.Combine(box.Root, "QUAN-LY-PHU.md");
        File.WriteAllText(journal, "# Sổ tay\r\n## Nhật ký quản lý phụ\r\n- Cũ\r\n## Khác\r\nGiữ nguyên\r\n", new UTF8Encoding(true));
        var r = await box.RunAsync("team.ps1", ["Invoke-Deputy"], """
            $Utf8 = New-Object Text.UTF8Encoding $false
            $Force = $true
            function Say { param($Text, $Color) Write-Output $Text }
            function Resolve-Project { param($Name) [pscustomobject]@{Logs=$Root} }
            function Get-Command { param($Name, $ErrorAction) [pscustomobject]@{Source='codex.cmd'} }
            function Start-Process {
                param($FilePath, $ArgumentList, $WorkingDirectory, $WindowStyle, [switch]$PassThru, $RedirectStandardInput, $RedirectStandardOutput, $RedirectStandardError)
                $exe = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
                $body = '$c = Start-Process "' + $exe + '" -ArgumentList "-NoProfile -Command Start-Sleep 120" -WindowStyle Hidden -PassThru; [IO.File]::WriteAllText("' + $Root + '\child.pid", [string]$c.Id); Start-Sleep 120'
                $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($body))
                $p = Microsoft.PowerShell.Management\Start-Process $exe -ArgumentList "-NoProfile -EncodedCommand $encoded" -WindowStyle Hidden -PassThru
                [IO.File]::WriteAllText((Join-Path $Root 'parent.pid'), [string]$p.Id)
                $ready = [Diagnostics.Stopwatch]::StartNew()
                while (-not (Test-Path (Join-Path $Root 'child.pid'))) {
                    if ($ready.Elapsed.TotalSeconds -gt 10) { throw 'Tiến trình giả không sẵn sàng.' }
                    Start-Sleep -Milliseconds 50
                }
                return $p
            }
            function taskkill.exe {
                if (($args -join ',') -ne "/PID,$([IO.File]::ReadAllText((Join-Path $Root 'parent.pid'))),/T,/F") { throw 'Sai lệnh dừng cây tiến trình.' }
                Microsoft.PowerShell.Management\Stop-Process -Id ([int][IO.File]::ReadAllText((Join-Path $Root 'child.pid'))) -Force
                Microsoft.PowerShell.Management\Stop-Process -Id ([int]$args[1]) -Force
                $global:LASTEXITCODE = 0
            }
            Invoke-Deputy -TimeoutSeconds 1
            """);
        try
        {
            Assert.True(r.Process.Code == 0, r.Process.Error + r.Process.Output);
            Assert.Contains("Lượt quản lý quá 45 phút — đã dừng", r.Process.Output);
            var text = File.ReadAllText(journal);
            Assert.Contains("Lượt quản lý quá 45 phút — đã dừng", text);
            Assert.Contains("- Cũ", text);
            Assert.True(text.IndexOf("Lượt quản lý", StringComparison.Ordinal) < text.IndexOf("## Khác", StringComparison.Ordinal));
            Assert.Equal(new byte[] { 239, 187, 191 }, File.ReadAllBytes(journal)[..3]);
            Assert.False(File.Exists(Path.Combine(box.Root, ".state", "deputy.lock")));
            foreach (var name in new[] { "parent", "child" })
                Assert.DoesNotContain(System.Diagnostics.Process.GetProcesses(), p => p.Id == int.Parse(File.ReadAllText(Path.Combine(box.Root, name + ".pid"))));
        }
        finally
        {
            foreach (var name in new[] { "parent", "child" })
            {
                var path = Path.Combine(box.Root, name + ".pid");
                if (File.Exists(path))
                    foreach (var p in System.Diagnostics.Process.GetProcesses().Where(p => p.Id == int.Parse(File.ReadAllText(path))))
                    { using (p) { p.Kill(true); p.WaitForExit(5000); } }
            }
        }
    }
}
