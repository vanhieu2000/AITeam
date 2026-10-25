using System.Diagnostics;
using System.Text;

namespace AITeam.Core.Tests.Support;

/// <summary>
/// Bộ chạy tiến trình DÙNG CHUNG cho mọi test nghiệm thu (Claude, 25/10).
/// BẮT BUỘC dùng thay vì tự viết harness: đã gom các lỗi S1-04 / S1-06 / V8-02
/// (UTF-8 stdout, CLIXML của -EncodedCommand, xóa repo git chỉ-đọc, biến GIT_* rò từ máy, treo không timeout).
/// </summary>
public static class TestShell
{
    public sealed record Result(int Code, string Output, string Error);

    /// <summary>Chạy đoạn PowerShell 5.1 bằng -File (script tạm, UTF-8 BOM), stdout/stderr UTF-8, ErrorActionPreference=Stop.</summary>
    public static async Task<Result> PowerShell(string workDir, string script, int timeoutSeconds = 60)
    {
        var file = Path.Combine(Path.GetTempPath(), "aiteam-test-" + Guid.NewGuid().ToString("N") + ".ps1");
        var header = "[Console]::OutputEncoding = New-Object Text.UTF8Encoding $false\r\n$ErrorActionPreference = 'Stop'\r\n";
        await File.WriteAllTextAsync(file, header + script, new UTF8Encoding(true));
        try
        {
            return await Run(workDir, "powershell.exe", timeoutSeconds,
                "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", file);
        }
        finally { File.Delete(file); }
    }

    /// <summary>Chạy exe với đối số rời (không qua shell), xóa biến GIT_*, git không đọc cấu hình máy, có timeout.</summary>
    public static async Task<Result> Run(string workDir, string exe, int timeoutSeconds, params string[] args)
    {
        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workDir, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false), StandardErrorEncoding = new UTF8Encoding(false)
        };
        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
            info.Environment.Remove(key);
        info.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        info.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(Path.GetTempPath(), "aiteam-test-empty.gitconfig");
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var a in args) info.ArgumentList.Add(a);
        using var process = System.Diagnostics.Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try { await process.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException($"{exe} quá {timeoutSeconds} giây."); }
        return new(process.ExitCode, await stdout, await stderr);
    }
}

/// <summary>Thư mục tạm (tùy chọn là repo git) tự dọn được cả object git chỉ-đọc.</summary>
public sealed class TempRepo : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "aiteam-repo-" + Guid.NewGuid().ToString("N"));

    public TempRepo(bool git = true)
    {
        Directory.CreateDirectory(Root);
        if (!git) return;
        Git("init", "-q");
        Git("config", "user.name", "Kiểm thử");
        Git("config", "user.email", "test@example.invalid");
        Git("config", "core.autocrlf", "false");
    }

    /// <summary>Ghi file (giữ đúng byte: BOM theo tham số, xuống dòng giữ nguyên chuỗi truyền vào).</summary>
    public string Write(string relative, string text, bool bom = true)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(bom));
        return path;
    }

    /// <summary>Chạy git, lỗi ⇒ ném kèm stderr. Commit dùng --allow-empty để biến thể trùng dữ liệu không làm hỏng test.</summary>
    public string Git(params string[] args)
    {
        if (args.Length > 0 && args[0] == "commit" && !args.Contains("--allow-empty"))
            args = ["commit", "--allow-empty", .. args[1..]];
        var r = TestShell.Run(Root, "git", 30, args).GetAwaiter().GetResult();
        if (r.Code != 0) throw new InvalidOperationException($"git {string.Join(' ', args)}: {r.Error}{r.Output}");
        return r.Output;
    }

    public void Dispose()
    {
        if (!Directory.Exists(Root)) return;
        foreach (var f in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            File.SetAttributes(f, FileAttributes.Normal);
        Directory.Delete(Root, true);
    }
}
