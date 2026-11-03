using System.Runtime.CompilerServices;
using AITeam.Core.Tests.Support;
namespace AITeam.Core.Tests;
public sealed class V901ModelCacheTests
{
    private static string Source([CallerFilePath] string source = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "team.ps1"));
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Invalid_model_and_account_switch_remove_only_model_cache(bool switchAccount)
    {
        var root = Path.Combine(Path.GetTempPath(), "v901-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            File.Copy(Source(), Path.Combine(root, "team-source.ps1"));
            var result = await TestShell.PowerShell(root, $$$"""
                $Root = $PWD.Path
                $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'team-source.ps1'),[ref]$null,[ref]$null)
                foreach ($f in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true)) {
                    if ($f.Name -in @('Clear-AgyModelCache','Invoke-SwitchAccount')) { . ([scriptblock]::Create($f.Extent.Text)) }
                }
                function Say { }
                function Get-Config { @{agy=@{exe='fake';accounts=@()}} }
                function Start-Process { }
                function Set-BoardStatus { }
                New-Item -ItemType Directory (Join-Path $Root '.state') | Out-Null
                $cache = Join-Path $Root '.state/agy-models.json'
                $other = Join-Path $Root '.state/other.json'
                [IO.File]::WriteAllText($cache,'{}'); [IO.File]::WriteAllText($other,'giữ lại')
                if (${{{switchAccount.ToString().ToLowerInvariant()}}}) { Invoke-SwitchAccount } else {
                    $logText='invalid model selection'; $errText=''; $taskFile=@{Name='test.md'}; $NoVerify=$true
                    $branch = $ast.Find({param($n) $n -is [Management.Automation.Language.IfStatementAst] -and $n.Extent.Text.StartsWith('if ("$logText`n$errText" -match')},$true)
                    & ([scriptblock]::Create($branch.Extent.Text)) | Out-Null
                }
                if (Test-Path $cache) { throw 'Cache model chưa bị xóa' }
                if ([IO.File]::ReadAllText($other) -ne 'giữ lại') { throw 'Đã đụng file khác' }
                Clear-AgyModelCache
                Write-Output 'CACHE_OK'
                """);
            Assert.Equal(0, result.Code); Assert.Contains("CACHE_OK", result.Output);
        }
        finally { Directory.Delete(root, true); }
    }
}
