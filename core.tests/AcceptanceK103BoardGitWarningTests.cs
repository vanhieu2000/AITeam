using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

// K1-03 (Claude): Set-BoardStatus chạy với ErrorActionPreference = Stop. Lệnh git ghi cảnh báo ra stderr
// (vd. "LF will be replaced by CRLF") mà không chuyển hướng ⇒ PowerShell 5.1 coi là lỗi, cổng kiểm tra báo
// "Không ghi được nhật ký phiếu" (K1-01, K1-02 ngày 04/11). Mọi git add / commit trong hàm phải chuyển hướng stderr.
public sealed class AcceptanceK103BoardGitWarningTests
{
    private static string FunctionText(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var text = File.ReadAllText(Path.Combine(dir!.FullName, "team.ps1"));
        var m = Regex.Match(text, @"(?ms)^function " + Regex.Escape(name) + @"\b.*?(?=^function )");
        Assert.True(m.Success, $"Không tìm thấy hàm {name}");
        return m.Value;
    }

    [Fact]
    public void Set_BoardStatus_git_add_and_commit_redirect_stderr()
    {
        var lines = FunctionText("Set-BoardStatus").Split('\n')
            .Where(l => Regex.IsMatch(l, @"^\s*git\s+-C\s+\S+\s+(add|commit)\b"))
            .ToList();
        Assert.True(lines.Count >= 2, "Phải còn lệnh git add và git commit trong Set-BoardStatus.");
        foreach (var l in lines)
            Assert.True(Regex.IsMatch(l, @"\s2>(\$null|&1)"),
                $"Lệnh git không chuyển hướng stderr (cảnh báo git sẽ thành lỗi dưới Stop): {l.Trim()}");
    }

    [Fact]
    public void Set_BoardStatus_still_checks_git_exit_codes()
    {
        var body = FunctionText("Set-BoardStatus");
        // Chuyển hướng stderr không được làm mất kiểm tra lỗi thật.
        Assert.Contains("Không cập nhật được bảng việc.", body);
        Assert.True(Regex.Matches(body, @"\$LASTEXITCODE -ne 0").Count >= 2);
    }
}
