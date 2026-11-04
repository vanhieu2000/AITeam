using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

// R-015 (Claude sửa: chặn dây chuyền, 04/11): Set-BoardStatus chèn dòng task mới sau dòng "|" CUỐI file
// ⇒ 5 task F1-0x rơi vào bảng O-02 ở mục "Kế hoạch", Hub không thấy. Get-BoardInsertIndex trả chỗ ngay sau bảng việc.
public sealed class BoardInsertIndexTests
{
    private static string Function(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var text = File.ReadAllText(Path.Combine(dir!.FullName, "team.ps1"));
        var m = Regex.Match(text, @"(?ms)^function " + Regex.Escape(name) + @"\b.*?(?=^function |^# )");
        Assert.True(m.Success, $"Không tìm thấy hàm {name}");
        return m.Value;
    }

    private static int Run(string board)
    {
        var dir = Path.Combine(Path.GetTempPath(), "aiteam-bii-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var boardFile = Path.Combine(dir, "README.md");
            File.WriteAllText(boardFile, board, new UTF8Encoding(true));
            var script = Path.Combine(dir, "t.ps1");
            File.WriteAllText(script, Function("Get-BoardInsertIndex") +
                "\n$lines = [IO.File]::ReadAllText('" + boardFile.Replace("'", "''") + "') -split \"`r?`n\"\nWrite-Output (Get-BoardInsertIndex $lines)\n",
                new UTF8Encoding(true));
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            using var p = System.Diagnostics.Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(30000);
            Assert.True(p.ExitCode == 0, err);
            return int.Parse(output.Trim());
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Inserts_after_task_table_not_after_last_pipe_line_of_file()
    {
        // Bảng việc THẬT rút gọn (04/11): bảng việc, dòng trống, chú thích, mục Kế hoạch có bảng O-02.
        var board = string.Join("\n",
            "# Bảng công việc",                                   // 0
            "",                                                    // 1
            "| Task | Agent | Nhánh | Trạng thái |",               // 2
            "|---|---|---|---|",                                   // 3
            "| [K1-04 Codex](K1-04.md) | C | `b` | ✅ |",          // 4
            "",                                                    // 5
            "| [F1-06 Bảng](F1-06.md) | C | `b` | ✅ |",           // 6
            "",                                                    // 7
            "Trạng thái: 🟡 Đã giao · ✅ Đã merge",                 // 8
            "",                                                    // 9
            "## Kế hoạch / hàng chờ",                              // 10
            "| O-02 | OpenCode | agent-c/O-02 | Chờ |");          // 11
        Assert.Equal(7, Run(board));
    }

    [Fact]
    public void Board_without_task_table_appends_at_end()
        => Assert.Equal(3, Run("# Bảng\n\nchưa có bảng"));

    [Fact]
    public void Empty_task_table_inserts_after_divider()
        => Assert.Equal(3, Run("# B\n| Task | Trạng thái |\n|---|---|\n\n## Kế hoạch\n| X | Y |"));

    [Fact]
    public void Set_BoardStatus_uses_insert_index()
        => Assert.Contains("Get-BoardInsertIndex $lines", Function("Set-BoardStatus"));
}
