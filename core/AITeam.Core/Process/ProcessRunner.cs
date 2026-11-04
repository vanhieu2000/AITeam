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
    IReadOnlyDictionary<string, string>? Environment = null,
    // K1-02 (khung Claude): không có dòng stdout/stderr nào quá khoảng này ⇒ dừng cả cây tiến trình, Stalled = true. null = tắt.
    TimeSpan? IdleTimeout = null);

/// <param name="Stalled">K1-02: bị dừng vì đứng im quá ProcessSpec.IdleTimeout (khác TimedOut = quá giờ cứng).</param>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, TimeSpan Duration, bool TimedOut, bool Stalled = false);

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
        long lastActivity = System.Environment.TickCount64;
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Interlocked.Exchange(ref lastActivity, System.Environment.TickCount64);
            lock (stdout) stdout.AppendLine(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Interlocked.Exchange(ref lastActivity, System.Environment.TickCount64);
            lock (stderr) stderr.AppendLine(e.Data);
        };
        p.Start();
        Interlocked.Exchange(ref lastActivity, System.Environment.TickCount64);
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
        var timeoutStarted = System.Environment.TickCount64;
        timeoutCts.CancelAfter(spec.Timeout);
        using var idleCts = new CancellationTokenSource();
        using var monitorCts = new CancellationTokenSource();
        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, idleCts.Token);
        async Task MonitorIdleAsync()
        {
            if (spec.IdleTimeout is not { } idle) return;
            var interval = TimeSpan.FromMilliseconds(Math.Max(100, Math.Min(idle.TotalMilliseconds / 4, 5000)));
            try
            {
                while (true)
                {
                    await Task.Delay(interval, monitorCts.Token).ConfigureAwait(false);
                    if (p.HasExited) return;
                    if (System.Environment.TickCount64 - Interlocked.Read(ref lastActivity) < idle.TotalMilliseconds) continue;
                    idleCts.Cancel();
                    return;
                }
            }
            catch (OperationCanceledException) when (monitorCts.IsCancellationRequested) { /* Đã dừng bộ canh. */ }
        }
        var monitor = MonitorIdleAsync();
        bool timedOut = false, stalled = false;
        try
        {
            await p.WaitForExitAsync(waitCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = !ct.IsCancellationRequested && (timeoutCts.IsCancellationRequested
                || System.Environment.TickCount64 - timeoutStarted >= spec.Timeout.TotalMilliseconds);
            stalled = !timedOut && !ct.IsCancellationRequested && idleCts.IsCancellationRequested;
            try { p.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* đã thoát */ }
            await p.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
        }
        finally
        {
            monitorCts.Cancel();
            await monitor.ConfigureAwait(false);
        }
        ct.ThrowIfCancellationRequested();
        p.WaitForExit(); // xả nốt output bất đồng bộ
        return new ProcessResult(p.ExitCode, stdout.ToString(), stderr.ToString(), sw.Elapsed, timedOut, stalled);
    }
}
