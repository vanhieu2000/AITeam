using System.Text;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public class AcceptanceV806AgyModelTests
{
    private static string Functions()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "team.ps1")))
            root = Directory.GetParent(root)?.FullName ?? throw new IOException("Không tìm thấy team.ps1.");
        var path = Path.Combine(root, "team.ps1").Replace("'", "''");
        return $$"""
            $ast = [Management.Automation.Language.Parser]::ParseFile('{{path}}', [ref]$null, [ref]$null)
            $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in @('Read-Json','Get-Config','Resolve-AgyModel','ConvertFrom-AgyModels','Get-AgyModels')}, $false) | ForEach-Object { Invoke-Expression $_.Extent.Text }
            $Root = (Get-Location).Path
            """;
    }

    [Theory]
    [InlineData("claude-sonnet-4-6", "claude-sonnet-4-6,claude-opus-4-6-thinking", "claude-sonnet-4-6")]
    [InlineData("old-model", "gemini-4-argon-high,claude-opus-5-5-medium", "claude-opus-5-5-medium")]
    [InlineData("", "claude-opus-4-6-thinking", "")]
    [InlineData("old-model", "", "")]
    [InlineData("old-model", "unknown-model", "")]
    public async Task Resolves_requested_or_first_available_fallback(string wanted, string available, string expected)
    {
        using var repo = new TempRepo(git: false);
        var r = await TestShell.PowerShell(repo.Root, Functions() + $"\n$result = Resolve-AgyModel '{wanted}' ('{available}' -split ','); Write-Output ('KẾT QUẢ:' + $result)");
        Assert.Equal(0, r.Code);
        Assert.Equal("KẾT QUẢ:" + expected, r.Output.Trim());
    }

    [Fact]
    public async Task Parses_two_account_lists_and_uses_configured_priority()
    {
        using var repo = new TempRepo(git: false);
        repo.Write("team.json", """{"agy":{"modelFallback":["gemini-4-argon-high","claude-opus-5-5-medium"]}}""");
        var r = await TestShell.PowerShell(repo.Root, Functions() + """

            $one = @(ConvertFrom-AgyModels "Fetching available models...`nclaude-opus-4-6-thinking`tClaude Opus 4.6`nclaude-sonnet-4-6`tClaude Sonnet 4.6")
            $two = @(ConvertFrom-AgyModels "Fetching available models...`r`nclaude-opus-5-5-medium`tClaude Opus 5.5`r`ngemini-4-argon-high`tGemini 4 Argon")
            Write-Output ($one -join ',')
            Write-Output ($two -join ',')
            Write-Output (Resolve-AgyModel 'old-model' $two (Get-Config).agy.modelFallback)
            """);
        Assert.Equal(0, r.Code);
        Assert.Equal(new[] { "claude-opus-4-6-thinking,claude-sonnet-4-6", "claude-opus-5-5-medium,gemini-4-argon-high", "gemini-4-argon-high" }, r.Output.Trim().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    public async Task Refresh_handles_success_exit_error_and_timeout(int exitCode, bool timeout)
    {
        using var repo = new TempRepo(git: false);
        var r = await TestShell.PowerShell(repo.Root, Functions() + $"\n$code = {exitCode}; $timeout = ${timeout.ToString().ToLowerInvariant()}" + """

            $script:killed = $false
            function Start-Process {
                param($FilePath, $ArgumentList, [switch]$PassThru, [switch]$NoNewWindow, $RedirectStandardOutput, $RedirectStandardError)
                if ($ArgumentList -ne 'models') { throw 'Sai đối số.' }
                "gemini-4-argon-high`tGemini 4 Argon" | Set-Content $RedirectStandardOutput -Encoding UTF8
                $p = [pscustomobject]@{ ExitCode = $code }
                $p | Add-Member ScriptMethod WaitForExit { param($ms) if ($ms -ne $null) { if ($ms -ne 60000) { throw 'Sai timeout.' }; return (-not $timeout) } }
                $p | Add-Member ScriptMethod Kill { $script:killed = $true }
                $p | Add-Member ScriptMethod Dispose { }
                return $p
            }
            $result = Get-AgyModels @{ agy = @{ exe = 'giả-lập.exe' } } 3>$null
            if ($code -eq 0 -and -not $timeout) {
                if ($result.models -ne 'gemini-4-argon-high' -or -not (Test-Path .state/agy-models.json)) { throw 'Thiếu model hoặc cache.' }
            } elseif ($null -ne $result -or (Test-Path .state/agy-models.json)) { throw 'Không được cache kết quả lỗi.' }
            if ($script:killed -ne $timeout) { throw 'Chưa dừng tiến trình quá hạn.' }
            if (@(Get-ChildItem .state -Filter 'models-*').Count -ne 0) { throw 'Chưa dọn file tạm.' }
            Write-Output 'ĐẠT'
            """);
        Assert.Equal(0, r.Code);
        Assert.Equal("ĐẠT", r.Output.Trim());
    }

    [Fact]
    public async Task Uses_fresh_cache_and_preserves_model_when_refresh_fails()
    {
        using var repo = new TempRepo(git: false);
        var r = await TestShell.PowerShell(repo.Root, Functions() + """

            $null = New-Item -ItemType Directory .state
            @{ fetchedAt = [DateTime]::UtcNow.ToString('o'); models = @('cached-model') } | ConvertTo-Json | Set-Content .state/agy-models.json -Encoding UTF8
            function Start-Process { throw 'Không được gọi tiến trình thật.' }
            $cfg = @{ agy = @{ exe = 'không-tồn-tại.exe' } }
            Write-Output ((Get-AgyModels $cfg).models -join ',')
            @{ fetchedAt = [DateTime]::UtcNow.AddHours(-2).ToString('o'); models = @('stale-model') } | ConvertTo-Json | Set-Content .state/agy-models.json -Encoding UTF8
            $models = Get-AgyModels $cfg 3>$null
            if ($null -ne $models) { throw 'Không được dùng cache cũ khi agy lỗi.' }
            Write-Output 'GIỮ MODEL CŨ'
            """);
        Assert.Equal(0, r.Code);
        Assert.Equal("cached-model\r\nGIỮ MODEL CŨ", r.Output.Trim());
    }
}
