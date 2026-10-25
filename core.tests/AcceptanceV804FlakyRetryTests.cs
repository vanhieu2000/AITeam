using System.Text.Json;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public class AcceptanceV804FlakyRetryTests
{
    private static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("Không tìm thấy mã nguồn kiểm thử.");
        }
    }

    private static async Task<TestShell.Result> Run(string script)
    {
        using var temp = new TempRepo(git: false);
        var common = temp.Write("workflow-common.ps1", File.ReadAllText(Path.Combine(Root, "tools/workflow-common.ps1")));
        return await TestShell.PowerShell(temp.Root, $". '{common.Replace("'", "''")}'\n" + script);
    }

    [Theory]
    [InlineData("  Failed AITeam.Core.Tests.AcceptanceV801WorkflowTests.KhoaChanTienTrinhKhacVaTimeout [12 s]", "AITeam.Core.Tests.AcceptanceV801WorkflowTests.KhoaChanTienTrinhKhacVaTimeout")]
    [InlineData("  Failed AITeam.Core.Tests.AcceptanceV601ReviewRoundsTests.Invalid_task_id_returns_exact_error(id: \"../V6-01\") [28 s]", "AITeam.Core.Tests.AcceptanceV601ReviewRoundsTests.Invalid_task_id_returns_exact_error")]
    [InlineData("[xUnit.net 00:00:12.34] A.B.C(x: 1) [FAIL]\n  Failed A.B.C(x: 2) [1 s]\n  Failed A.B.D [2 ms]", "A.B.C,A.B.D")]
    [InlineData("  Failed A.B.C|Other [1 s]\nBad&Name [FAIL]", "")]
    [InlineData("x.cs(1): error CS1002: ; expected\nBuild FAILED.\nFailed! Failed: 1", "")]
    [InlineData("", "")]
    public async Task Parse_ten_test_tu_mau_that(string output, string expected)
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(output));
        var r = await Run($"$text = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{encoded}'))\n@(Get-FailedTestNames $text) -join ','");
        Assert.Equal(0, r.Code);
        Assert.Equal(expected, r.Output.Trim());
    }

    [Theory]
    [InlineData("pass", 0, 2)]
    [InlineData("fail", 1, 2)]
    [InlineData("many", 1, 0)]
    [InlineData("build", 1, 0)]
    [InlineData("unsafe", 1, 1)]
    [InlineData("empty", 1, 2)]
    [InlineData("ten", 0, 10)]
    public async Task Thu_lai_dung_mot_lan_va_giu_loi(string mode, int code, int calls)
    {
        var r = await Run("$mode = '" + mode + "'\n" + """
            $script:commands = @()
            $output = "  Failed A.B.C(x: 1) [1 s]`n  Failed A.B.C(x: 2) [1 s]`n[xUnit.net 00:01] A.B.D [FAIL]"
            if ($mode -eq 'many' -or $mode -eq 'ten') {
                $count = if ($mode -eq 'many') { 11 } else { 10 }
                $output = (1..$count | ForEach-Object { "  Failed A.B.Test$_ [1 s]" }) -join "`n"
            }
            if ($mode -eq 'build') { $output += "`nx.cs(1): error CS1002: ; expected" }
            if ($mode -eq 'unsafe') { $output = "  Failed A.B.C [1 s]`n  Failed A.B.Bad|Other [1 s]" }
            $r = Invoke-FailedTestRetry -Code 1 -Output $output -TestCommand 'dotnet test "My Project.csproj" --logger console' -Run {
                param($command)
                $script:commands += $command
                if ($mode -eq 'fail') { return @{ Code = 1; Output = 'Failed A.B.C [1 s]' } }
                if ($mode -eq 'empty') { return @{ Code = 0; Output = 'No test matches the given testcase filter.' } }
                return @{ Code = 0; Output = 'Passed! Failed: 0, Passed: 1, Skipped: 0, Total: 1' }
            }
            @{ Code = $r.Code; Warnings = @($r.Warnings); Commands = @($script:commands); Output = $r.Output } | ConvertTo-Json -Compress
            """);
        Assert.Equal(0, r.Code);
        using var json = JsonDocument.Parse(r.Output);
        var result = json.RootElement;
        Assert.Equal(code, result.GetProperty("Code").GetInt32());
        var commands = result.GetProperty("Commands").EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal(calls, commands.Length);
        Assert.Equal(calls, commands.Distinct().Count());
        Assert.All(commands, command =>
        {
            Assert.Contains("dotnet test \"My Project.csproj\" --no-build --filter \"FullyQualifiedName~A.B.", command);
            Assert.DoesNotContain("|", command);
            Assert.DoesNotContain("(x:", command);
        });
        if (code == 0) Assert.All(result.GetProperty("Warnings").EnumerateArray(), x => Assert.Contains("Test chập chờn: A.B.", x.GetString()));
        if (mode == "pass") Assert.Equal(2, result.GetProperty("Warnings").GetArrayLength());
        if (mode == "fail") Assert.Contains("Test vẫn đỏ: A.B.C, A.B.D", result.GetProperty("Output").GetString());
    }

    [Fact]
    public async Task Verify_them_canh_bao_vao_bao_cao_va_merge_dung_cung_co_che()
    {
        var source = File.ReadAllText(Path.Combine(Root, "team.ps1"));
        var start = source.IndexOf("    # 8. Build (0 warning) + test", StringComparison.Ordinal);
        var end = source.IndexOf("    # 9.", start, StringComparison.Ordinal);
        var reportLine = source.Split('\n').Single(x => x.Contains("if ($warns.Count) { $md +="));
        var script = """
            $fails = New-Object Collections.ArrayList
            $warns = New-Object Collections.ArrayList
            $notes = New-Object Collections.ArrayList
            $r = @{ Info = @{ test = 'dotnet test AITeam.sln'; build = $null } }
            $work = $pwd
            $script:count = 0
            function Invoke-ShellCapture {
                param($command, $dir)
                $script:count++
                if ($script:count -eq 1) { return @{ Code = 1; Output = '  Failed A.B.C [1 s]' } }
                return @{ Code = 0; Output = 'Passed! Failed: 0, Passed: 1, Total: 1' }
            }
            """ + "\n" + source[start..end] + "\n$md = @()\n" + reportLine + "\n@{ Fails = @($fails); Report = $md -join \"`n\"; Calls = $script:count } | ConvertTo-Json -Compress";
        var r = await Run(script);
        Assert.Equal(0, r.Code);
        using var json = JsonDocument.Parse(r.Output);
        Assert.Equal(0, json.RootElement.GetProperty("Fails").GetArrayLength());
        Assert.Equal(2, json.RootElement.GetProperty("Calls").GetInt32());
        Assert.Contains("Test chập chờn: A.B.C", json.RootElement.GetProperty("Report").GetString());
        Assert.Contains("Invoke-FailedTestRetry -Code $res.Code -Output $res.Output -TestCommand $testCmd", source);
    }
}