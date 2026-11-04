using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

// K1-03 (Claude): Set-BoardStatus chạy với ErrorActionPreference = Stop. git add in cảnh báo ra stderr
// ("LF will be replaced by CRLF") ⇒ PowerShell 5.1 ném NativeCommandError, cổng kiểm tra báo
// "Không ghi được nhật ký phiếu" (K1-01, K1-02 ngày 04/11).
// Vòng 2 (Claude sửa test 04/11 14:00): đã thử thật trên PowerShell 5.1 — CHỈ thêm "2>$null" dưới Stop vẫn ném
// (còn tệ hơn: ném cả khi gọi thẳng). Cách đúng: tạm đặt $ErrorActionPreference = 'Continue' quanh git add / commit,
// trả lại giá trị cũ ngay sau, lỗi thật bắt bằng $LASTEXITCODE.
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

    private static readonly Regex GitAddOrCommit = new(@"^\s*git\s+-C\s+\S+\s+(add|commit)\b");
    private static readonly Regex SetContinue = new(@"\$ErrorActionPreference\s*=\s*['""]Continue['""]");
    private static readonly Regex Restore = new(@"\$ErrorActionPreference\s*=\s*\$\w+");

    [Fact]
    public void Set_BoardStatus_runs_git_add_and_commit_under_Continue()
    {
        var lines = FunctionText("Set-BoardStatus").Split('\n');
        var idx = Enumerable.Range(0, lines.Length).Where(i => GitAddOrCommit.IsMatch(lines[i])).ToList();
        Assert.True(idx.Count >= 2, "Phải còn lệnh git add và git commit trong Set-BoardStatus.");
        foreach (var i in idx)
        {
            // Gần nhất phía trên (tối đa 6 dòng, hoặc cùng dòng) phải là đặt Continue, chưa bị trả lại giữa chừng.
            string? last = null;
            for (var j = i; j >= Math.Max(0, i - 6); j--)
            {
                var head = j == i ? lines[j][..GitAddOrCommit.Match(lines[j]).Index] : lines[j];
                if (Restore.IsMatch(head)) { last ??= "restore"; }
                if (SetContinue.IsMatch(head)) { last ??= "continue"; }
                if (last != null) break;
            }
            Assert.True(last == "continue", $"git chưa chạy dưới ErrorActionPreference = 'Continue': {lines[i].Trim()}");
        }
        // Sau lệnh git cuối cùng (tối đa 8 dòng) phải trả lại ErrorActionPreference cũ.
        var after = string.Join('\n', lines.Skip(idx.Max()).Take(8));
        Assert.Matches(Restore, after);
    }

    [Fact]
    public void Set_BoardStatus_still_checks_git_exit_codes()
    {
        var body = FunctionText("Set-BoardStatus");
        Assert.Contains("Không cập nhật được bảng việc.", body);
        Assert.True(Regex.Matches(body, @"\$LASTEXITCODE -ne 0").Count >= 2);
    }
}
