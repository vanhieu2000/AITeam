using System.Text.Json;
using AITeam.Core.Process;

namespace AITeam.Core.Providers;

/// <summary>
/// OpenAI Codex CLI (<c>codex exec</c>), đăng nhập ChatGPT chính thức.
/// - ReadOnly: <c>--sandbox read-only</c> — CLI tự chặn ghi, không chỉ dựa vào lời nhắn.
/// - Lời nhắn qua stdin (<c>-</c>), không qua tham số (PowerShell 5.1 / tiếng Việt / dấu ngoặc).
/// - KHÔNG sửa <c>~/.codex/config.toml</c> (cấu hình app Codex desktop của chủ dự án): chỉ ghi đè theo lần gọi
///   (<c>-m</c>, tắt notify / MCP cho nhẹ). Đã thử 23/10: model mặc định cũ không dùng được với tài khoản ChatGPT.
/// </summary>
public sealed class CodexProvider(ProviderConfig config, IProcessRunner runner) : IAgentProvider
{
    public string Id => config.Id;
    public ProviderKind Kind => ProviderKind.Codex;
    public bool HasReadOnlySandbox => true;

    /// <summary>K1-04 (khung Claude): không có hoạt động thật quá khoảng này ⇒ dừng (mất kết nối / treo). Lệnh build + test dài nhất ~8 phút.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

    /// <summary>K1-04 (khung Claude): false = dòng thử lại / mất kết nối (sự kiện `error`, log `ERROR codex_…`), không tính là đang làm việc.</summary>
    internal static bool IsActivityLine(string line)
    {
        if (!line.TrimStart().StartsWith('{'))
            return !line.Contains(" ERROR codex_", StringComparison.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return true;
            if (type.ValueEquals("error")) return false;
            return !((type.ValueEquals("item.completed") || type.ValueEquals("item.started"))
                && root.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("type", out var itemType) && itemType.ValueKind == JsonValueKind.String && itemType.ValueEquals("error"));
        }
        catch (JsonException) { return true; }
    }

    /// <summary>(exe, tiền tố tham số): <c>codex.js</c> chạy bằng node; còn lại chạy thẳng.</summary>
    internal (string Exe, List<string> Prefix) ResolveLaunch()
    {
        var exe = config.Exe;
        if (OperatingSystem.IsWindows() && (string.IsNullOrWhiteSpace(exe) || exe.Equals("codex", StringComparison.OrdinalIgnoreCase)))
            exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "@openai", "codex", "bin", "codex.js");
        if (string.IsNullOrWhiteSpace(exe)) exe = "codex";
        if (exe.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            return (string.IsNullOrWhiteSpace(config.NodeExe) ? "node" : config.NodeExe!, [exe]);
        return (exe, []);
    }

    internal IReadOnlyList<string> BuildArguments(AgentRunRequest r, string lastMessageFile)
    {
        var (_, args) = ResolveLaunch();
        args.AddRange(["exec", "--json", "--ephemeral", "--skip-git-repo-check",
            "-C", r.WorkDir,
            "--sandbox", r.Mode == RunMode.ReadOnly ? "read-only" : "workspace-write",
            "-c", "notify=[]", "-c", "mcp_servers={}"]);
        args.AddRange(["-c", "windows.sandbox=\"unelevated\""]);
        if (!string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException("codex.baseUrl phải là URL HTTP(S) không chứa thông tin đăng nhập.");
            args.AddRange(["-c", "openai_base_url=" + JsonSerializer.Serialize(config.BaseUrl)]);
        }
        var model = r.Model ?? config.Model;
        if (!string.IsNullOrWhiteSpace(model)) args.AddRange(["-m", model]);
        if (r.OutputSchemaFile != null) args.AddRange(["--output-schema", r.OutputSchemaFile]);
        args.AddRange(["-o", lastMessageFile, "-"]);
        return args;
    }

    public async Task<AgentRunResult> RunAsync(AgentRunRequest r, CancellationToken ct)
    {
        var (exe, _) = ResolveLaunch();
        var last = Path.Combine(r.LogDir ?? Path.GetTempPath(), $"codex-last-{Guid.NewGuid():N}.txt");
        ProcessResult res;
        try
        {
            res = await runner.RunAsync(new ProcessSpec(exe, BuildArguments(r, last), r.WorkDir, r.PromptFile, r.Timeout, IdleTimeout: IdleTimeout, IsActivity: IsActivityLine), ct).ConfigureAwait(false);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new AgentRunResult(Id, RunStatus.Unavailable, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, ex.Message);
        }
        var text = File.Exists(last) ? await File.ReadAllTextAsync(last, ct).ConfigureAwait(false) : "";
        if (File.Exists(last)) File.Delete(last);
        return Parse(Id, res, text, r.Model ?? config.Model);
    }

    /// <summary>Đọc luồng sự kiện <c>--json</c> (mẫu thật 23/10: thread.started, turn.completed{usage}, turn.failed{error}).</summary>
    internal static AgentRunResult Parse(string id, ProcessResult res, string lastMessage, string? model)
    {
        if (res.TimedOut)
            return new AgentRunResult(id, RunStatus.TimedOut, "", TokenUsage.Unknown, res.Duration, model, null, "Quá thời gian");
        string? thread = null, error = null;
        bool completed = false;
        var tokens = TokenUsage.Unknown;
        foreach (var line in res.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith('{')) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var e = doc.RootElement;
                var type = e.TryGetProperty("type", out var t) ? t.GetString() : null;
                switch (type)
                {
                    case "thread.started":
                        thread = e.TryGetProperty("thread_id", out var th) ? th.GetString() : null; break;
                    case "turn.completed":
                        completed = true;
                        tokens = TokenUsage.Unknown;
                        if (e.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object
                            && ReadToken(u, "input_tokens") is { } input && ReadToken(u, "output_tokens") is { } output
                            && input <= long.MaxValue - output)
                            tokens = new TokenUsage(input, ReadToken(u, "cached_input_tokens") ?? 0, output)
                            { Thinking = ReadToken(u, "reasoning_output_tokens") };
                        break;
                    case "turn.failed":
                        error = e.TryGetProperty("error", out var er) && er.TryGetProperty("message", out var m) ? m.GetString() : line; break;
                    case "error":
                        error ??= e.TryGetProperty("message", out var msg) ? msg.GetString() : line; break;
                }
            }
            catch (JsonException) { /* dòng hỏng: bỏ qua, kết luận dựa trên turn.completed */ }
        }
        if (res.Stalled)
        {
            var network = (res.StdOut + "\n" + res.StdErr)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(line => !IsActivityLine(line));
            var minutes = (int)IdleTimeout.TotalMinutes;
            var detail = network
                ? $"Codex mất kết nối tới máy chủ AI suốt {minutes} phút (Reconnecting… / waiting for network) — đã dừng. Kiểm tra mạng hoặc openai_base_url trong ~/.codex/config.toml."
                : $"Codex đứng im {minutes} phút không có hoạt động — đã dừng.";
            return new AgentRunResult(id, RunStatus.TimedOut, "", tokens, res.Duration, model, thread, detail);
        }
        bool ok = completed && error == null && res.ExitCode == 0;
        var kind = ErrorClassifier.Classify((error ?? "") + "\n" + res.StdErr, ok);
        return new AgentRunResult(id, kind, ok ? lastMessage : "", tokens, res.Duration, model, thread,
            ok ? "" : AgyProvider.Clip(error ?? res.StdErr));
    }

    private static long? ReadToken(JsonElement usage, string name) => usage.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) && number >= 0 ? number : null;

    public async Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        var (exe, prefix) = ResolveLaunch();
        if (prefix.Count > 0 && !File.Exists(prefix[0])) return new ProviderHealth(Id, false, null, $"Chưa cài: {prefix[0]}");
        var empty = Path.GetTempFileName();
        try
        {
            var res = await runner.RunAsync(new ProcessSpec(exe, [.. prefix, "login", "status"], Path.GetTempPath(), empty, TimeSpan.FromSeconds(60)), ct).ConfigureAwait(false);
            var all = res.StdOut + res.StdErr;
            bool logged = res.ExitCode == 0 && all.Contains("Logged in", StringComparison.OrdinalIgnoreCase);
            return new ProviderHealth(Id, true, logged, AgyProvider.Clip(all));
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new ProviderHealth(Id, false, null, ex.Message);
        }
        finally { File.Delete(empty); }
    }
}
