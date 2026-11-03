using System.Text;
using System.Text.Json;
using AITeam.Core.Metrics;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class V917ExternalRunLogTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "run-logs", name);

    [Fact]
    public void OpenCode_sums_three_step_finish_without_cache_read()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-three-steps.jsonl"));
        Assert.True(run.TokensKnown);
        Assert.Equal(600L, run.InputTokens!.Value);
        Assert.Equal(81L, run.OutputTokens!.Value);
        Assert.Equal(681L, run.TotalTokens!.Value);
    }

    [Fact]
    public void OpenCode_without_step_finish_reports_unknown_tokens()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-no-tokens.jsonl"));
        Assert.False(run.TokensKnown);
        Assert.Null(run.InputTokens);
        Assert.Null(run.OutputTokens);
        Assert.Null(run.TotalTokens);
    }

    [Fact]
    public void OpenCode_skips_broken_line_and_keeps_reading()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-broken-line.jsonl"));
        Assert.True(run.TokensKnown);
        Assert.Equal(11L, run.InputTokens!.Value);
        Assert.Equal(5L, run.OutputTokens!.Value);
        Assert.Equal(16L, run.TotalTokens!.Value);
        Assert.Equal("SUCCESS", run.Status);
    }

    [Fact]
    public void OpenCode_without_text_is_not_success()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-no-text.jsonl"));
        Assert.Equal("KHÔNG CÓ KẾT QUẢ", run.Status);
        Assert.False(run.HasOutput);
        Assert.True(run.TokensKnown);
        Assert.Equal(6L, run.TotalTokens!.Value);
    }

    [Fact]
    public void OpenCode_duration_is_max_minus_min_timestamp()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-three-steps.jsonl"));
        Assert.Equal(30, run.DurationSec);
        Assert.Equal("SUCCESS", run.Status);
        Assert.True(run.HasOutput);
    }

    [Fact]
    public void Codex_reads_grouped_total_tokens()
    {
        var run = ExternalRunLog.Parse("codex", Fixture("codex-tokens.log"));
        Assert.True(run.TokensKnown);
        Assert.Equal(37701L, run.TotalTokens!.Value);
        Assert.Null(run.InputTokens);
        Assert.Null(run.OutputTokens);
        Assert.Equal("SUCCESS", run.Status);
        Assert.True(run.HasOutput);
    }

    [Fact]
    public void Codex_without_tokens_line_is_unknown()
    {
        var run = ExternalRunLog.Parse("codex", Fixture("codex-no-tokens.log"));
        Assert.False(run.TokensKnown);
        Assert.Null(run.TotalTokens);
        Assert.Equal("SUCCESS", run.Status);
    }

    [Fact]
    public void Unknown_dev_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() => ExternalRunLog.Parse("agy", Fixture("opencode-three-steps.jsonl")));
    }

    [Fact]
    public void Missing_log_throws_file_not_found()
    {
        Assert.Throws<FileNotFoundException>(() =>
            ExternalRunLog.Parse("opencode", Path.Combine(Path.GetTempPath(), "khong-co-" + Guid.NewGuid().ToString("N") + ".jsonl")));
    }

    [Fact]
    public void Record_json_does_not_copy_part_text()
    {
        var run = ExternalRunLog.Parse("opencode", Fixture("opencode-three-steps.jsonl"));
        var json = JsonSerializer.Serialize(run, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("MARKER_KHONG_DUOC_CHEP", json);
        Assert.DoesNotContain("text", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cli_parse_run_log_outputs_record_and_rejects_bad_arguments()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        var ok = await TestShell.Run(Path.GetTempPath(), "dotnet", 60, dll,
            "parse-run-log", "--dev", "codex", "--log", Fixture("codex-tokens.log"));
        Assert.Equal(0, ok.Code);
        using (var document = JsonDocument.Parse(ok.Output))
        {
            Assert.Equal(37701L, document.RootElement.GetProperty("totalTokens").GetInt64());
            Assert.True(document.RootElement.GetProperty("tokensKnown").GetBoolean());
        }

        var missing = await TestShell.Run(Path.GetTempPath(), "dotnet", 60, dll, "parse-run-log", "--dev", "codex");
        Assert.Equal(2, missing.Code);

        var badDev = await TestShell.Run(Path.GetTempPath(), "dotnet", 60, dll,
            "parse-run-log", "--dev", "agy", "--log", Fixture("codex-tokens.log"));
        Assert.Equal(2, badDev.Code);
    }

    [Fact]
    public async Task PowerShell_log_run_skips_duplicate_and_continues_run_numbers()
    {
        using var sandbox = new TeamScript();
        var root = sandbox.Root;
        Directory.CreateDirectory(Path.Combine(root, "metrics", "runs"));
        Directory.CreateDirectory(Path.Combine(root, "tasks"));
        File.WriteAllText(Path.Combine(root, "tasks", "H8-12.md"),
            "**Yêu cầu:** R-012\r\n**Thời gian tối đa:** 30\r\n**Độ khó:** M\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "metrics", "runs", "cu.json"),
            "{\"project\":\"AITeam\",\"task\":\"H8-12\",\"run\":\"3\",\"machine\":\"MAY-KHAC\",\"sourceLog\":\"cu.json:1\"}",
            new UTF8Encoding(false));
        var logPath = Path.Combine(root, "oc-H8-12.jsonl");
        File.WriteAllText(logPath,
            "{\"type\":\"text\",\"timestamp\":1,\"part\":{\"type\":\"text\",\"text\":\"x\"}}\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "cli.cmd"), """
            @echo off
            echo {"durationSec":660,"tokensKnown":true,"inputTokens":78000,"outputTokens":2000,"totalTokens":80000,"status":"SUCCESS","at":"2026-11-03T04:25:00","hasOutput":true}
            exit /b 0
            """.ReplaceLineEndings("\r\n"), new UTF8Encoding(false));

        var script = $$"""
            $Project = 'AITeam'
            $Task = 'H8-12'
            $Dev = 'opencode'
            $Log = '{{logPath}}'
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root } }
            function Get-AITeamExe { Join-Path $Root 'cli.cmd' }
            function Find-TaskFile($r, $id) { Get-Item (Join-Path $Root "tasks/$id.md") }
            function Get-Section($text, $name) { '' }
            function Get-ReviewFindings($review) { @() }
            function Say($text, $color) { $script:said += [string]$text }
            function Save-RunMetrics($record) {
                [IO.File]::WriteAllText((Join-Path $Root ('metrics/runs/' + $record.log)), ($record | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
            }
            $script:said = @()
            Invoke-LogRun
            Invoke-LogRun
            $script:said -join '|'
            """;

        var result = await sandbox.RunAsync("team.ps1", ["Invoke-LogRun"], script);
        Assert.Equal(0, result.Process.Code);
        Assert.Contains("Đã ghi lượt này", result.Process.Output);
        var files = Directory.GetFiles(Path.Combine(root, "metrics", "runs"), "H8-12-*.json");
        using var saved = JsonDocument.Parse(File.ReadAllText(Assert.Single(files)));
        var row = saved.RootElement;
        Assert.Equal("4", row.GetProperty("run").GetString());
        Assert.Equal("O", row.GetProperty("agent").GetString());
        Assert.Equal("", row.GetProperty("conversationId").GetString());
        Assert.Equal("R-012", row.GetProperty("request").GetString());
        Assert.Equal(78000L, row.GetProperty("inputTokens").GetInt64());
    }

    [Fact]
    public async Task PowerShell_log_run_keeps_null_tokens_for_codex()
    {
        using var sandbox = new TeamScript();
        var root = sandbox.Root;
        Directory.CreateDirectory(Path.Combine(root, "metrics", "runs"));
        Directory.CreateDirectory(Path.Combine(root, "tasks"));
        File.WriteAllText(Path.Combine(root, "tasks", "H8-12.md"),
            "**Yêu cầu:** R-012\r\n**Thời gian tối đa:** 30\r\n**Độ khó:** M\r\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, "cli.cmd"), """
            @echo off
            echo {"durationSec":5,"tokensKnown":true,"inputTokens":null,"outputTokens":null,"totalTokens":37701,"status":"SUCCESS","at":"2026-11-03T04:25:00","hasOutput":true}
            exit /b 0
            """.ReplaceLineEndings("\r\n"), new UTF8Encoding(false));

        var logPath = Fixture("codex-tokens.log");
        var script = $$"""
            $Project = 'AITeam'
            $Task = 'H8-12'
            $Dev = 'codex'
            $Log = '{{logPath}}'
            function Resolve-Project($name) { [pscustomobject]@{ Path = $Root; Info = [pscustomobject]@{ name = $name }; Logs = $Root } }
            function Get-AITeamExe { Join-Path $Root 'cli.cmd' }
            function Find-TaskFile($r, $id) { Get-Item (Join-Path $Root "tasks/$id.md") }
            function Get-Section($text, $name) { '' }
            function Get-ReviewFindings($review) { @() }
            function Say($text, $color) { $script:said += [string]$text }
            function Save-RunMetrics($record) {
                [IO.File]::WriteAllText((Join-Path $Root ('metrics/runs/' + $record.log)), ($record | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
            }
            $script:said = @()
            Invoke-LogRun
            """;

        var result = await sandbox.RunAsync("team.ps1", ["Invoke-LogRun"], script);
        Assert.Equal(0, result.Process.Code);
        var files = Directory.GetFiles(Path.Combine(root, "metrics", "runs"), "H8-12-*.json");
        using var saved = JsonDocument.Parse(File.ReadAllText(Assert.Single(files)));
        var row = saved.RootElement;
        Assert.Equal(JsonValueKind.Null, row.GetProperty("inputTokens").ValueKind);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("outputTokens").ValueKind);
        Assert.Equal(37701L, row.GetProperty("totalTokens").GetInt64());
        Assert.Equal("C", row.GetProperty("agent").GetString());
    }
}
