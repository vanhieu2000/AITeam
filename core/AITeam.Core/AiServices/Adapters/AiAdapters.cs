using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Research;

namespace AITeam.Core.AiServices.Adapters;

// V10-02 (R-014): chạy AI theo danh bạ: 3 adapter chung + factory đọc registry.
// Thiết kế: tasks/V10-02-adapter-ai-tu-danh-ba.md, docs/design/R-014-van-phong-ai.md mục 4.6, 5, 8.

/// <summary>Tra giá trị khóa trong két theo TÊN. Core không biết két nằm đâu; Hub cắm vào (VaultSecretLookup).</summary>
public interface ISecretLookup
{
    string? Get(string name);
}

public interface IAiProviderFactory
{
    /// <summary>
    /// Id "codex" ⇒ CodexProvider; "opencode" ⇒ OpenCodeProvider; "agy" hoặc "antigravity" ⇒ AgyProvider (lớp riêng có sẵn).
    /// Còn lại theo Adapter: CliJson ⇒ CliJsonProvider, CliText ⇒ CliTextProvider, OpenAiCompatibleHttp ⇒ OpenAiHttpProvider.
    /// Dịch vụ tắt ⇒ InvalidOperationException("Dịch vụ AI &lt;id&gt; đang tắt.").
    /// </summary>
    IAgentProvider Create(AiService service);

    /// <summary>registry.List(includeDisabled: false) lọc theo vai trò, giữ thứ tự registry.</summary>
    IReadOnlyList<AiService> ForRole(AiRole role);
}

public sealed class AiProviderFactory(IAiServiceRegistry registry, IProcessRunner runner, ISecretLookup secrets, HttpMessageHandler? http = null) : IAiProviderFactory
{
    private readonly IAiServiceRegistry _registry = registry;
    private readonly IProcessRunner _runner = runner;
    private readonly ISecretLookup _secrets = secrets;
    private readonly HttpMessageHandler? _http = http;

    public IAgentProvider Create(AiService service)
    {
        if (!service.Enabled)
            throw new InvalidOperationException($"Dịch vụ AI {service.Id} đang tắt.");

        var id = service.Id.Trim();
        if (id.Equals("codex", StringComparison.OrdinalIgnoreCase))
        {
            return new CodexProvider(new ProviderConfig
            {
                Id = service.Id, Kind = ProviderKind.Codex, Exe = service.Command ?? "codex",
                Model = service.Models.FirstOrDefault(), MaxConcurrent = service.MaxJobs, Paid = service.Paid, Enabled = service.Enabled
            }, _runner);
        }

        if (id.Equals("opencode", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenCodeProvider(new ProviderConfig
            {
                Id = service.Id, Kind = ProviderKind.OpenCode, Exe = service.Command ?? "opencode",
                Model = service.Models.FirstOrDefault(), MaxConcurrent = service.MaxJobs, Paid = service.Paid, Enabled = service.Enabled
            }, _runner);
        }

        if (id.Equals("agy", StringComparison.OrdinalIgnoreCase) || id.Equals("antigravity", StringComparison.OrdinalIgnoreCase))
        {
            return new AgyProvider(new ProviderConfig
            {
                Id = service.Id, Kind = ProviderKind.Antigravity, Exe = service.Command ?? "agy",
                Model = service.Models.FirstOrDefault(), MaxConcurrent = service.MaxJobs, Paid = service.Paid, Enabled = service.Enabled
            }, _runner);
        }

        return service.Adapter switch
        {
            AiAdapter.CliJson => new CliJsonProvider(service, _runner),
            AiAdapter.CliText => new CliTextProvider(service, _runner),
            AiAdapter.OpenAiCompatibleHttp => new OpenAiHttpProvider(service, _secrets, _http),
            _ => new CliTextProvider(service, _runner)
        };
    }

    public IReadOnlyList<AiService> ForRole(AiRole role)
    {
        return _registry.List(includeDisabled: false)
            .Where(s => s.Roles.Contains(role))
            .ToList();
    }
}

/// <summary>CLI in chữ thường ra stdout. Args thay chỗ giữ {prompt_file} {workdir} {model} TRONG từng phần tử, không qua shell.</summary>
public sealed class CliTextProvider(AiService service, IProcessRunner runner) : IAgentProvider
{
    private readonly IProcessRunner _runner = runner;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => false;
    public Task<ProviderHealth> CheckAsync(CancellationToken ct) => AiAdapterSupport.CheckCliAsync(service, _runner, ct);
    public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => AiAdapterSupport.RunCliTextAsync(service, _runner, request, ct);
}

/// <summary>CLI in JSON lines: Text = nối "text" của dòng type=text (hoặc part.text); token từ tokens.input / tokens.output.</summary>
public sealed class CliJsonProvider(AiService service, IProcessRunner runner) : IAgentProvider
{
    private readonly IProcessRunner _runner = runner;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => false;
    public Task<ProviderHealth> CheckAsync(CancellationToken ct) => AiAdapterSupport.CheckCliAsync(service, _runner, ct);
    public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => AiAdapterSupport.RunCliJsonAsync(service, _runner, request, ct);
}

/// <summary>POST {BaseUrl}/chat/completions; chỉ RunMode.ReadOnly. Giá trị khóa không bao giờ ra Text / Detail / ngoại lệ.</summary>
public sealed class OpenAiHttpProvider(AiService service, ISecretLookup secrets, HttpMessageHandler? http = null) : IAgentProvider
{
    private readonly ISecretLookup _secrets = secrets;
    private readonly HttpMessageHandler? _http = http;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => true;

    private HttpClient CreateClient() => _http != null
        ? new HttpClient(_http, disposeHandler: false)
        : new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false });

    public async Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        if (!AiAdapterSupport.IsValidBaseUrl(service.BaseUrl))
            return new ProviderHealth(Id, false, null, "BaseUrl không hợp lệ.");

        try
        {
            using var client = CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var url = service.BaseUrl!.TrimEnd('/') + "/models";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            var key = string.IsNullOrWhiteSpace(service.SecretKeyName) ? null : _secrets.Get(service.SecretKeyName);
            if (!string.IsNullOrWhiteSpace(key))
            {
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            }
            using var res = await client.SendAsync(req, cts.Token).ConfigureAwait(false);
            bool ok = res.IsSuccessStatusCode;
            bool? loggedIn = (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) ? false : (ok ? true : null);
            return new ProviderHealth(Id, ok, loggedIn, $"HTTP {(int)res.StatusCode}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ProviderHealth(Id, false, null, "Quá thời gian kiểm tra.");
        }
        catch (Exception)
        {
            return new ProviderHealth(Id, false, null, "Lỗi kết nối.");
        }
    }

    public async Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
    {
        var model = request.Model ?? service.Models.FirstOrDefault();
        if (!AiAdapterSupport.IsValidBaseUrl(service.BaseUrl))
        {
            return new AgentRunResult(Id, RunStatus.ConfigError, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, "BaseUrl không hợp lệ.");
        }

        if (request.Mode == RunMode.Develop)
        {
            return new AgentRunResult(Id, RunStatus.ConfigError, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, "Adapter HTTP không viết code được.");
        }

        var key = string.IsNullOrWhiteSpace(service.SecretKeyName) ? null : _secrets.Get(service.SecretKeyName);
        string promptContent = "";
        if (File.Exists(request.PromptFile))
        {
            promptContent = await File.ReadAllTextAsync(request.PromptFile, ct).ConfigureAwait(false);
        }

        var payload = new
        {
            model = model,
            messages = new[] { new { role = "user", content = promptContent } }
        };

        var json = JsonSerializer.Serialize(payload, AiAdapterSupport.HttpJsonOptions);
        var url = service.BaseUrl!.TrimEnd('/') + "/chat/completions";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(key))
        {
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        }

        using var client = CreateClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timeout = request.Timeout <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : request.Timeout;
        cts.CancelAfter(timeout);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage resp;
        try
        {
            resp = await client.SendAsync(req, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AgentRunResult(Id, RunStatus.TimedOut, "", TokenUsage.Unknown, sw.Elapsed, model, null, "Yêu cầu HTTP quá thời gian.");
        }
        catch (HttpRequestException ex)
        {
            return new AgentRunResult(Id, RunStatus.Unavailable, "", TokenUsage.Unknown, sw.Elapsed, model, null, AiAdapterSupport.CleanOutput(ex.Message, key));
        }

        var (body, truncated) = await AiAdapterSupport.ReadHttpBodyAsync(resp, cts.Token).ConfigureAwait(false);

        RunStatus status;
        if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            status = RunStatus.QuotaExhausted;
        else if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            status = RunStatus.NotLoggedIn;
        else if ((int)resp.StatusCode >= 300 && (int)resp.StatusCode < 400)
            status = RunStatus.Failed;
        else if (!resp.IsSuccessStatusCode)
        {
            if (AiAdapterSupport.ContainsAny(body, "rate limit", "quota", "usage limit", "insufficient balance", "free usage limit"))
                status = RunStatus.QuotaExhausted;
            else if (AiAdapterSupport.ContainsAny(body, "not logged in", "unauthorized", "unauthenticated"))
                status = RunStatus.NotLoggedIn;
            else
                status = RunStatus.Failed;
        }
        else
            status = RunStatus.Succeeded;

        string detail = truncated ? "đã cắt" : (!resp.IsSuccessStatusCode ? (string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body) : "OK");

        string text = "";
        long inTokens = -1, outTokens = -1;
        if (status == RunStatus.Succeeded)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var choice0 = choices[0];
                    if (choice0.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                    {
                        text = content.GetString() ?? "";
                    }
                }
                if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt64(out var pv)) inTokens = pv;
                    if (usage.TryGetProperty("completion_tokens", out var ctProp) && ctProp.TryGetInt64(out var cv)) outTokens = cv;
                }
            }
            catch (JsonException)
            {
                status = RunStatus.Failed;
                detail = "Phản hồi không phải JSON hợp lệ.";
            }
        }

        var tokens = (inTokens >= 0 && outTokens >= 0) ? new TokenUsage(inTokens, 0, outTokens) : TokenUsage.Unknown;

        return new AgentRunResult(
            Id, status, AiAdapterSupport.CleanOutput(text, key), tokens, sw.Elapsed, model, null, AiAdapterSupport.CleanOutput(detail, key));
    }
}

internal static class AiAdapterSupport
{
    public static readonly JsonSerializerOptions HttpJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string CleanOutput(string? text, string? secretValue)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var res = text;
        if (!string.IsNullOrEmpty(secretValue)) res = res.Replace(secretValue, "***");
        return SecretRedactor.Redact(res).Text;
    }

    public static bool IsValidCommand(string? cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return false;
        const string invalidChars = "&|;<>^\"\r\n";
        return cmd.IndexOfAny(invalidChars.ToCharArray()) < 0;
    }

    public static bool IsValidBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme == Uri.UriSchemeHttps) return true;
        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            return uri.Host is "127.0.0.1" or "localhost" or "[::1]";
        }
        return false;
    }

    public static bool ContainsAny(string text, params string[] phrases)
    {
        foreach (var p in phrases)
        {
            if (text.Contains(p, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static RunStatus ClassifyCli(int exitCode, string text)
    {
        if (exitCode == 429 || ContainsAny(text, "rate limit", "quota", "usage limit", "insufficient balance", "free usage limit"))
        {
            return RunStatus.QuotaExhausted;
        }
        if (exitCode is 401 or 403 || ContainsAny(text, "not logged in", "please log in", "please sign in", "login required", "unauthenticated"))
        {
            return RunStatus.NotLoggedIn;
        }
        if (exitCode != 0) return RunStatus.Failed;
        return RunStatus.Succeeded;
    }

    public static (IReadOnlyList<string> Args, string? StdinFile) SubstituteArgs(
        IReadOnlyList<string> templateArgs, AgentRunRequest request, string? model)
    {
        var modelStr = model ?? "";
        var list = new List<string>(templateArgs.Count);
        bool hasPromptFile = false;
        foreach (var arg in templateArgs)
        {
            if (arg.Contains("{prompt_file}", StringComparison.OrdinalIgnoreCase)) hasPromptFile = true;
            list.Add(arg
                .Replace("{prompt_file}", request.PromptFile, StringComparison.OrdinalIgnoreCase)
                .Replace("{workdir}", request.WorkDir, StringComparison.OrdinalIgnoreCase)
                .Replace("{model}", modelStr, StringComparison.OrdinalIgnoreCase));
        }
        string? stdinFile = hasPromptFile ? null : request.PromptFile;
        return (list, stdinFile);
    }

    public static (string Text, bool Truncated) LimitOutput(string? output)
    {
        const int maxChars = 2 * 1024 * 1024;
        var text = output ?? "";
        return text.Length > maxChars ? (text[..maxChars], true) : (text, false);
    }

    public static async Task<ProviderHealth> CheckCliAsync(AiService service, IProcessRunner runner, CancellationToken ct)
    {
        if (!IsValidCommand(service.Command)) return new ProviderHealth(service.Id, false, null, "Lệnh không hợp lệ.");
        try
        {
            var spec = new ProcessSpec(service.Command!, ["--version"], Directory.GetCurrentDirectory(), null, TimeSpan.FromSeconds(15));
            var res = await runner.RunAsync(spec, ct).ConfigureAwait(false);
            bool installed = !res.TimedOut && res.ExitCode == 0;
            var detail = res.TimedOut ? "Quá thời gian kiểm tra." : res.StdOut.Trim();
            return new ProviderHealth(service.Id, installed, null, CleanOutput(detail, null));
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new ProviderHealth(service.Id, false, null, "Không tìm thấy chương trình.");
        }
        catch (Exception ex)
        {
            return new ProviderHealth(service.Id, false, null, CleanOutput(ex.Message, null));
        }
    }

    private static async Task<(AgentRunResult? EarlyResult, string StdOut, string Detail, string? Model, TimeSpan Duration, RunStatus Status)> ExecuteCliAsync(
        AiService service, IProcessRunner runner, AgentRunRequest request, CancellationToken ct)
    {
        var model = request.Model ?? service.Models.FirstOrDefault();
        if (!IsValidCommand(service.Command))
        {
            return (new AgentRunResult(service.Id, RunStatus.ConfigError, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, "Lệnh không hợp lệ."), "", "", model, TimeSpan.Zero, RunStatus.ConfigError);
        }

        var (args, stdinFile) = SubstituteArgs(service.Args, request, model);
        var timeout = request.Timeout <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : request.Timeout;
        var spec = new ProcessSpec(service.Command!, args, request.WorkDir, stdinFile, timeout);

        ProcessResult pr;
        try
        {
            pr = await runner.RunAsync(spec, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return (new AgentRunResult(service.Id, RunStatus.Unavailable, "", TokenUsage.Unknown, TimeSpan.Zero, model, null, "Không tìm thấy chương trình."), "", "", model, TimeSpan.Zero, RunStatus.Unavailable);
        }

        if (pr.TimedOut)
        {
            return (new AgentRunResult(service.Id, RunStatus.TimedOut, "", TokenUsage.Unknown, pr.Duration, model, null, "Tiến trình quá thời gian."), "", "", model, pr.Duration, RunStatus.TimedOut);
        }

        var (stdout, truncated) = LimitOutput(pr.StdOut);
        var combined = (pr.StdErr ?? "") + "\n" + stdout;
        var status = ClassifyCli(pr.ExitCode, combined);

        string detail = pr.StdErr ?? "";
        if (truncated) detail = string.IsNullOrWhiteSpace(detail) ? "đã cắt" : (detail + " (đã cắt)");
        if (string.IsNullOrWhiteSpace(detail) && status == RunStatus.Succeeded) detail = "OK";

        return (null, stdout, detail, model, pr.Duration, status);
    }

    public static async Task<AgentRunResult> RunCliTextAsync(AiService service, IProcessRunner runner, AgentRunRequest request, CancellationToken ct)
    {
        var (early, stdout, detail, model, duration, status) = await ExecuteCliAsync(service, runner, request, ct).ConfigureAwait(false);
        if (early != null) return early;
        return new AgentRunResult(service.Id, status, CleanOutput(stdout, null), TokenUsage.Unknown, duration, model, null, CleanOutput(detail, null));
    }

    public static async Task<AgentRunResult> RunCliJsonAsync(AiService service, IProcessRunner runner, AgentRunRequest request, CancellationToken ct)
    {
        var (early, stdout, detail, model, duration, status) = await ExecuteCliAsync(service, runner, request, ct).ConfigureAwait(false);
        if (early != null) return early;
        var (text, tokens) = ParseJsonLines(stdout);
        return new AgentRunResult(service.Id, status, CleanOutput(text, null), tokens, duration, model, null, CleanOutput(detail, null));
    }

    public static async Task<(string Body, bool Truncated)> ReadHttpBodyAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        const int maxBytes = 2 * 1024 * 1024;
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var ms = new MemoryStream();
        byte[] buffer = new byte[81920];
        int total = 0;
        bool truncated = false;
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, maxBytes - total), ct).ConfigureAwait(false)) > 0)
        {
            ms.Write(buffer, 0, read);
            total += read;
            if (total >= maxBytes)
            {
                int extra = await stream.ReadAsync(buffer, 0, 1, ct).ConfigureAwait(false);
                if (extra > 0) truncated = true;
                break;
            }
        }
        return (Encoding.UTF8.GetString(ms.ToArray()), truncated);
    }

    public static (string Text, TokenUsage Tokens) ParseJsonLines(string output)
    {
        var sb = new StringBuilder();
        long inTokens = -1, outTokens = -1;
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;

                string? type = null;
                if (root.TryGetProperty("type", out var tProp) && tProp.ValueKind == JsonValueKind.String)
                {
                    type = tProp.GetString();
                }

                if (string.Equals(type, "text", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(txt.GetString());
                    }
                    else if (root.TryGetProperty("part", out var part) && part.ValueKind == JsonValueKind.Object
                        && part.TryGetProperty("text", out var partTxt) && partTxt.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(partTxt.GetString());
                    }
                }

                JsonElement tokensElem = default;
                if (root.TryGetProperty("tokens", out var tok) && tok.ValueKind == JsonValueKind.Object)
                    tokensElem = tok;
                else if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object
                    && usage.TryGetProperty("tokens", out var utok) && utok.ValueKind == JsonValueKind.Object)
                    tokensElem = utok;

                if (tokensElem.ValueKind == JsonValueKind.Object)
                {
                    if (tokensElem.TryGetProperty("input", out var inp) && inp.TryGetInt64(out var iv)) inTokens = iv;
                    if (tokensElem.TryGetProperty("output", out var outp) && outp.TryGetInt64(out var ov)) outTokens = ov;
                }
            }
            catch (JsonException) { }
        }
        var tokens = (inTokens >= 0 && outTokens >= 0) ? new TokenUsage(inTokens, 0, outTokens) : TokenUsage.Unknown;
        return (sb.ToString(), tokens);
    }
}
