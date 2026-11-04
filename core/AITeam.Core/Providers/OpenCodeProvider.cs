using System.Text;
using System.Text.Json;
using AITeam.Core.Process;
using AITeam.Core.Research;

namespace AITeam.Core.Providers;

public sealed class OpenCodeProvider(ProviderConfig config, IProcessRunner runner) : IAgentProvider
{
    public const string DefaultModel = "opencode/deepseek-v4.1-flash-free";
    /// <summary>K1-02 (khung Claude): OpenCode không in dòng nào quá khoảng này ⇒ dừng (hết lượt free hay đứng im).</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(20);
    public string Id => config.Id;
    public ProviderKind Kind => ProviderKind.OpenCode;
    public bool HasReadOnlySandbox => false;

    public static ProviderConfig Configuration(string root, bool publicRepository, ProviderConfig? existing = null)
    {
        if (existing != null && existing.Kind != ProviderKind.OpenCode) throw new InvalidDataException("Provider opencode có Kind không đúng.");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "team.json")));
        var options = document.RootElement.TryGetProperty("opencode", out var o) ? o : default;
        var localFile = Path.Combine(root, "team.local.json");
        using var local = File.Exists(localFile) ? JsonDocument.Parse(File.ReadAllText(localFile)) : null;
        var localOptions = local?.RootElement.TryGetProperty("opencode", out var lo) == true ? lo : default;
        JsonElement Value(string key) => localOptions.ValueKind == JsonValueKind.Object && localOptions.TryGetProperty(key, out var lv) ? lv
            : options.ValueKind == JsonValueKind.Object && options.TryGetProperty(key, out var value) ? value : default;
        string? Read(string key) => Value(key).ValueKind == JsonValueKind.Undefined ? null : Value(key).GetString();
        return new ProviderConfig
        {
            Id = "opencode", Kind = ProviderKind.OpenCode, Exe = Read("exe") ?? "opencode", Model = Read("model") ?? DefaultModel,
            Roles = existing?.Roles ?? [AgentRole.Developer], Enabled = existing?.Enabled ?? true, PrivateRepository = !publicRepository,
            Paid = Value("paid").ValueKind == JsonValueKind.True,
            PermissionsFile = Path.Combine(root, "templates", "opencode-quyen-han.json")
        };
    }

    private (string Exe, List<string> Prefix) Launch()
    {
        var exe = string.IsNullOrWhiteSpace(config.Exe) ? "opencode" : config.Exe;
        if (OperatingSystem.IsWindows() && exe == "opencode")
        {
            var native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "opencode-ai", "bin", "opencode.exe");
            if (File.Exists(native)) return (native, []);
            var wrapper = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "opencode-ai", "bin", "opencode");
            if (File.Exists(wrapper)) return (config.NodeExe ?? "node", [wrapper]);
        }
        return exe.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? (config.NodeExe ?? "node", [exe]) : (exe, []);
    }

    public async Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
    {
        var model = request.Model ?? config.Model ?? DefaultModel;
        AgentRunResult Reject(string detail) => new(Id, RunStatus.ConfigError, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, detail);
        if (!model.Contains("deepseek", StringComparison.OrdinalIgnoreCase)) return Reject("OpenCode chỉ được dùng model DeepSeek.");
        if (config.PrivateRepository && !config.Paid) return Reject("Repo private/không rõ riêng tư không được chạy OpenCode free. Cần opencode.paid=true hoặc khai báo repo public.");
        if (request.Mode == RunMode.ReadOnly) return Reject("OpenCode hiện chỉ hỗ trợ developer; chưa có sandbox review chỉ đọc được xác minh.");
        if (config.PermissionsFile == null || !File.Exists(config.PermissionsFile)) return Reject("Thiếu templates/opencode-quyen-han.json.");
        var permissions = Path.Combine(Path.GetTempPath(), "aiteam-opencode-" + Guid.NewGuid().ToString("N") + ".json");
        File.Copy(config.PermissionsFile, permissions);
        try
        {
            var (exe, args) = Launch();
            args.AddRange(["run", "Lam dung theo file dinh kem.", "-f", request.PromptFile, "--dir", request.WorkDir,
                "--format", "json", "--title", request.Title ?? Path.GetFileNameWithoutExtension(request.PromptFile), "-m", model]);
            var result = await runner.RunAsync(new ProcessSpec(exe, args, request.WorkDir, null, request.Timeout,
                new Dictionary<string, string> { ["OPENCODE_CONFIG"] = permissions }, IdleTimeout), ct);
            if (request.LogDir != null)
            {
                Directory.CreateDirectory(request.LogDir);
                await File.WriteAllTextAsync(Path.Combine(request.LogDir, $"opencode-{Guid.NewGuid():N}.jsonl"), SecretRedactor.Redact(result.StdOut).Text, new UTF8Encoding(false), ct);
            }
            return Parse(Id, result, model);
        }
        catch (System.ComponentModel.Win32Exception) { return new(Id, RunStatus.Unavailable, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, "Chưa chạy được OpenCode CLI."); }
        finally { File.Delete(permissions); }
    }

    internal static AgentRunResult Parse(string id, ProcessResult result, string model)
    {
        if (result.TimedOut) return new(id, RunStatus.TimedOut, "", TokenUsage.Unknown, result.Duration, model, null, "OpenCode quá thời gian.");
        string text = "", error = ""; string? session = null;
        long input = 0, output = 0, total = 0; int steps = 0; bool known = true;
        foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line); var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) { known = false; continue; }
                var type = root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                if (root.TryGetProperty("sessionID", out var sid) && sid.ValueKind == JsonValueKind.String) session = sid.GetString();
                var part = root.TryGetProperty("part", out var p) && p.ValueKind == JsonValueKind.Object ? p : root;
                if (type == "text" && part.TryGetProperty("text", out var message) && message.ValueKind == JsonValueKind.String) text = message.GetString() ?? "";
                if (type == "step_finish")
                {
                    steps++;
                    if (!part.TryGetProperty("tokens", out var tokens) || Read(tokens, "input") is not { } i
                        || Read(tokens, "output") is not { } o || Read(tokens, "total") is not { } sum) { known = false; continue; }
                    if (i > long.MaxValue - o || sum < i + o) { known = false; continue; }
                    checked { input += i; output += o; total += sum; }
                }
                if (type == "error") error = line;
                if (part.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.Object && state.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String && status.GetString() == "error")
                {
                    var command = state.TryGetProperty("input", out var data) && data.ValueKind == JsonValueKind.Object && data.TryGetProperty("command", out var cmd) && cmd.ValueKind == JsonValueKind.String ? cmd.GetString() : "công cụ";
                    error = "Lệnh bị từ chối/lỗi công cụ: " + command + " · " + state.ToString();
                }
            }
            catch (JsonException) { known = false; }
            catch (OverflowException) { known = false; }
        }
        if (result.Stalled) return new(id, RunStatus.TimedOut, "", TokenUsage.Unknown, result.Duration, model, session,
            $"OpenCode đứng im {(int)IdleTimeout.TotalMinutes} phút không in gì (treo hoặc hết lượt miễn phí) — đã dừng.");
        var ok = result.ExitCode == 0 && error.Length == 0 && !string.IsNullOrWhiteSpace(text);
        var statusCode = ErrorClassifier.Classify(error + "\n" + result.StdErr, ok);
        var usage = known && steps > 0 ? new TokenUsage(input, 0, output) { ReportedTotal = total } : TokenUsage.Unknown;
        return new(id, statusCode, ok ? SecretRedactor.Redact(text).Text : "", usage, result.Duration, model, session,
            ok ? "" : AgyProvider.Clip(SecretRedactor.Redact(error.Length > 0 ? error : result.StdErr).Text));
    }
    private static long? Read(JsonElement tokens, string name) => tokens.ValueKind == JsonValueKind.Object && tokens.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var n) && n >= 0 ? n : null;

    public async Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        if (!(config.Model ?? DefaultModel).Contains("deepseek", StringComparison.OrdinalIgnoreCase)) return new(Id, false, null, "Model OpenCode phải là DeepSeek.");
        var (exe, prefix) = Launch();
        try
        {
            var result = await runner.RunAsync(new ProcessSpec(exe, [.. prefix, "--version"], Path.GetTempPath(), null, TimeSpan.FromSeconds(15)), ct);
            return new(Id, result.ExitCode == 0 && !result.TimedOut, null, AgyProvider.Clip(SecretRedactor.Redact(result.StdOut + result.StdErr).Text));
        }
        catch (System.ComponentModel.Win32Exception) { return new(Id, false, null, "Chưa cài OpenCode CLI."); }
    }
}
