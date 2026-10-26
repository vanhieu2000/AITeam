using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceV809RetryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Agy_models_respects_exit_code(int code)
    {
        using var sandbox = new TeamScript();
        var r = await sandbox.RunAsync("team.ps1", ["Get-AgyModels", "ConvertFrom-AgyModels", "Read-Json"], """
            $cfg = [pscustomobject]@{ agy = @{ exe = (Join-Path $env:PATH 'agy.exe') } }
            $result = Get-AgyModels $cfg
            if ($result) { Write-Output ('MODELS=' + ($result.models -join ',')) }
            else { Write-Output 'GIỮ MODEL CŨ' }
            """, [new("agy", code, "model-a\tModel A\r\nmodel-b\tModel B\r\n")]);
        Assert.Equal(0, r.Process.Code);
        Assert.Single(r.Calls);
        if (code == 0) Assert.Contains("MODELS=model-a,model-b", r.Process.Output);
        else { Assert.Contains("GIỮ MODEL CŨ", r.Process.Output); Assert.Contains("trả mã 1", r.Process.Output); }
    }

    [Theory]
    [InlineData("at capacity", false, 2, 0)]
    [InlineData("overloaded", true, 3, 0)]
    [InlineData("HTTP 429", false, 2, 0)]
    [InlineData("429 usage limit", true, 1, 1)]
    [InlineData("overloaded", false, 2, 1)]
    public async Task Codex_retries_only_overload_and_preserves_work(string error, bool fallback, int calls, int expectedCode)
    {
        using var sandbox = new TeamScript();
        var script = """
            function Get-Config { return [pscustomobject]@{ codex = @{ modelFallback = @(FALLBACK) } } }
            function Say($text, $color) { Write-Output $text }
            function Start-Sleep { param($Seconds) if ($Seconds -ne 60) { throw 'Sai thời gian chờ' }; $script:sleeps++ }
            $script:sleeps = 0; $script:count = 0
            $work = Join-Path $Root 'dở.txt'; [IO.File]::WriteAllText($work, 'phần dở')
            function Invoke-FakeRunner {
                $script:count++
                if ($script:count -eq SUCCESS) { $script:codexCode = 0; $script:codexOut = 'OK' }
                codex @args
                [IO.File]::WriteAllText((Join-Path $Root 'result.json'), ('{"summary":"' + $codexOut + '"}'))
            }
            Invoke-CodexRetry 'Invoke-FakeRunner' @('run', '--provider', 'codex', '--workdir', $Root) (Join-Path $Root 'result.json')
            Write-Output "CODE=$LASTEXITCODE;SLEEPS=$script:sleeps"
            if ([IO.File]::ReadAllText($work) -ne 'phần dở') { throw 'Mất phần dở' }
            """.Replace("FALLBACK", fallback ? "'backup-model', 'unused-model'" : "")
            .Replace("SUCCESS", expectedCode == 0 ? calls.ToString() : "99");
        var r = await sandbox.RunAsync("team.ps1", ["Invoke-CodexRetry"], script, [new("codex", 1, error)]);
        Assert.Equal(0, r.Process.Code);
        Assert.Equal(calls, r.Calls.Count);
        Assert.Contains($"CODE={expectedCode};SLEEPS={(calls > 1 ? 1 : 0)}", r.Process.Output);
        Assert.Equal(calls == 3 ? 2 : calls - 1, r.Process.Output.Split("Codex quá tải ⇒ thử lại").Length - 1);
        Assert.All(r.Calls, c => Assert.Contains(sandbox.Root, c.Arguments));
        if (calls == 3) Assert.Equal(["--model", "backup-model"], r.Calls[2].Arguments.TakeLast(2));
        else Assert.All(r.Calls, c => Assert.DoesNotContain("--model", c.Arguments));
    }
}
