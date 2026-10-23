using System.Text.Json;
using AITeam.Core.Process;

namespace AITeam.Core.Providers;

/// <summary>
/// Google Antigravity CLI (<c>agy</c>). Đăng nhập nằm trong Windows Credential Manager (1 tài khoản / user Windows).
/// Lời nhắn đầy đủ nằm trong file; tham số <c>-p</c> chỉ trỏ tới file (bài học PowerShell 5.1 / dấu ngoặc).
/// </summary>
public sealed class AgyProvider(ProviderConfig config, IProcessRunner runner) : IAgentProvider
{
    public string Id => config.Id;
    public ProviderKind Kind => ProviderKind.Antigravity;
    public bool HasReadOnlySandbox => true; // --sandbox

    internal IReadOnlyList<string> BuildArguments(AgentRunRequest r)
    {
        var args = new List<string>
        {
            "-p", $"Doc file {r.PromptFile} (UTF-8, tieng Viet) va lam dung theo huong dan trong do.",
            "--output-format", "json",
            "--print-timeout", $"{Math.Max(1, (int)Math.Ceiling(r.Timeout.TotalMinutes))}m",
        };
        if (r.Mode == RunMode.ReadOnly) args.Add("--sandbox");
        args.Add("--dangerously-skip-permissions"); // chạy ngầm không ai bấm duyệt; ReadOnly vẫn bị --sandbox chặn ghi
        args.Add("--add-dir"); args.Add(r.WorkDir);
        if (r.LogDir != null) { args.Add("--add-dir"); args.Add(r.LogDir); }
        var model = r.Model ?? config.Model;
        if (!string.IsNullOrWhiteSpace(model)) { args.Add("--model"); args.Add(model); }
        return args;
    }

    public async Task<AgentRunResult> RunAsync(AgentRunRequest r, CancellationToken ct)
    {
        if (!File.Exists(config.Exe))
            return new AgentRunResult(Id, RunStatus.Unavailable, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, $"Không thấy {config.Exe}");
        // Dư 2 phút cho agy tự dừng theo --print-timeout trước khi bị giết.
        var res = await runner.RunAsync(new ProcessSpec(config.Exe, BuildArguments(r), r.WorkDir, null, r.Timeout + TimeSpan.FromMinutes(2)), ct).ConfigureAwait(false);
        return Parse(Id, res, r.Model ?? config.Model);
    }

    /// <summary>Đọc JSON kết quả của <c>agy --output-format json</c> (mẫu thật: logs/*-n.json).</summary>
    internal static AgentRunResult Parse(string id, ProcessResult res, string? model)
    {
        if (res.TimedOut)
            return new AgentRunResult(id, RunStatus.TimedOut, "", TokenUsage.Unknown, res.Duration, model, null, "Quá thời gian");
        string? status = null, text = "", conv = null;
        var tokens = TokenUsage.Unknown;
        double? seconds = null;
        try
        {
            using var doc = JsonDocument.Parse(res.StdOut);
            var root = doc.RootElement;
            status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            text = root.TryGetProperty("response", out var t) ? t.GetString() ?? "" : "";
            conv = root.TryGetProperty("conversation_id", out var c) ? c.GetString() : null;
            if (root.TryGetProperty("duration_seconds", out var d) && d.ValueKind == JsonValueKind.Number) seconds = d.GetDouble();
            if (root.TryGetProperty("usage", out var u))
                tokens = new TokenUsage(
                    u.TryGetProperty("input_tokens", out var i) ? i.GetInt64() : 0,
                    u.TryGetProperty("cache_read_tokens", out var cr) ? cr.GetInt64() : 0,
                    u.TryGetProperty("output_tokens", out var o) ? o.GetInt64() : 0);
        }
        catch (JsonException)
        {
            // Không phải JSON: rơi xuống phân loại theo văn bản thô, KHÔNG coi là thành công.
        }
        bool ok = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
        var kind = ErrorClassifier.Classify(res.StdOut + "\n" + res.StdErr, ok);
        var duration = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : res.Duration;
        return new AgentRunResult(id, kind, text ?? "", tokens, duration, model, conv, ok ? "" : Clip(res.StdErr + res.StdOut));
    }

    public async Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        if (!File.Exists(config.Exe)) return new ProviderHealth(Id, false, null, $"Chưa cài: {config.Exe}");
        var empty = Path.GetTempFileName();
        try
        {
            var res = await runner.RunAsync(new ProcessSpec(config.Exe, ["models"], Path.GetTempPath(), empty, TimeSpan.FromSeconds(90)), ct).ConfigureAwait(false);
            bool ok = res.ExitCode == 0 && !res.TimedOut;
            var kind = ErrorClassifier.Classify(res.StdOut + res.StdErr, ok);
            return new ProviderHealth(Id, true, ok ? true : kind == RunStatus.NotLoggedIn ? false : null,
                ok ? "Đã đăng nhập (agy models chạy được)" : Clip(res.StdErr + res.StdOut));
        }
        finally { File.Delete(empty); }
    }

    internal static string Clip(string s) => s.Length <= 600 ? s.Trim() : s[..600].Trim() + "…";
}
