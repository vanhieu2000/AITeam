using System.Diagnostics;
using System.Text;

namespace AITeam.Core.Process;

/// <summary>Một lần chạy tiến trình: đường dẫn, tham số (từng phần tử, KHÔNG ghép chuỗi lệnh), thư mục, stdin từ file.</summary>
public sealed record ProcessSpec(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    string? StdinFile,
    TimeSpan Timeout,
    IReadOnlyDictionary<string, string>? Environment = null);

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, TimeSpan Duration, bool TimedOut);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct);
}

/// <summary>
/// Chạy tiến trình con an toàn: tham số qua ArgumentList (không qua shell ⇒ không chèn lệnh),
/// stdin luôn có (file hoặc rỗng — agy / codex treo nếu stdin là pipe mở), quá giờ hoặc bị hủy thì giết cả cây tiến trình.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = spec.FileName,
            WorkingDirectory = spec.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in spec.Arguments) psi.ArgumentList.Add(a);
        if (spec.Environment != null)
            foreach (var kv in spec.Environment) psi.Environment[kv.Key] = kv.Value;

        var sw = Stopwatch.StartNew();
        using var p = new System.Diagnostics.Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data); };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        // stdin: nội dung file (lời nhắn) rồi đóng — không bao giờ để mở.
        if (spec.StdinFile != null)
        {
            var bytes = await File.ReadAllBytesAsync(spec.StdinFile, ct).ConfigureAwait(false);
            await p.StandardInput.BaseStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        }
        p.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(spec.Timeout);
        bool timedOut = false;
        try
        {
            await p.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = !ct.IsCancellationRequested;
            try { p.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* đã thoát */ }
            await p.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            if (!timedOut) throw;
        }
        p.WaitForExit(); // xả nốt output bất đồng bộ
        return new ProcessResult(p.ExitCode, stdout.ToString(), stderr.ToString(), sw.Elapsed, timedOut);
    }
}
