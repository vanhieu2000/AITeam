using System.Globalization;
using System.Text.Json;
using AITeam.Core.Routing;

namespace AITeam.Core.Providers;

// Lệnh `aiteam run` (R-004): chạy 1 provider ở chế độ VIẾT CODE trong worktree của task (vd. Codex dự phòng khi agy hết hạn mức).
// Claude thiết kế khung; agent điền TODO(agent), KHÔNG đổi chữ ký public. team.ps1 tự commit + push sau đó (Codex không ghi được .git — thử thật 24/10).

public sealed record DevRunInput(
    string ProviderId,
    string WorkDir,
    string PromptFile,
    string LogDir,
    TimeSpan Timeout,
    string? Model = null,
    RunMode Mode = RunMode.Develop);   // R-006: ReadOnly cho bước lập kế hoạch (Codex chỉ đọc repo)

/// <summary>ExitCode: 0 xong · 1 lỗi khác · 2 đầu vào / cấu hình sai · 5 provider đang nghỉ (không gọi) · 6 hết hạn mức · 7 chưa đăng nhập · 8 quá giờ.</summary>
public sealed record DevRunOutcome(int ExitCode, string Status, string Summary, AgentRunResult? Result);

public sealed class DevRunner
{
    public const int ExitOk = 0, ExitFailed = 1, ExitConfig = 2, ExitResting = 5, ExitQuota = 6, ExitLogin = 7, ExitTimeout = 8;

    private readonly ProviderSettings _settings;
    private readonly ProviderStateStore _state;
    private readonly Func<ProviderConfig, IAgentProvider> _factory;
    private readonly Func<DateTime> _now;

    public DevRunner(ProviderSettings settings, ProviderStateStore state, Func<ProviderConfig, IAgentProvider> factory, Func<DateTime> now)
    {
        _settings = settings;
        _state = state;
        _factory = factory;
        _now = now;
    }

    /// <summary>
    /// 1. Kiểm tra (không gọi provider nếu sai), Status tương ứng trong ngoặc:
    ///    - không có cấu hình ProviderId (không phân biệt hoa thường) ⇒ (ExitConfig, "Config", "LỖI: không có provider {id}");
    ///    - Enabled = false ⇒ (ExitConfig, "Config", "LỖI: provider {id} đang tắt");
    ///    - Roles không có Developer ⇒ (ExitConfig, "Config", "LỖI: provider {id} không có vai trò Developer");
    ///    - PromptFile không tồn tại ⇒ (ExitConfig, "Config", "LỖI: không thấy file {PromptFile}");
    ///    - WorkDir không tồn tại ⇒ (ExitConfig, "Config", "LỖI: không thấy thư mục {WorkDir}");
    ///    - state.RestOf(id, now()) có ⇒ (ExitResting, "Resting", "NGHỈ: {id} nghỉ tới {Until:HH:mm dd/MM} ({Reason})").
    /// 2. Directory.CreateDirectory(LogDir); r = await factory(cfg).RunAsync(new AgentRunRequest(WorkDir, PromptFile, Mode, Timeout, Model, null, LogDir), ct);
    ///    state.Record(r, now()) (ghi nghỉ / xóa nghỉ).
    /// 3. ExitCode theo r.Status: Succeeded 0, QuotaExhausted 6, NotLoggedIn 7, TimedOut 8, còn lại 1. Status = r.Status.ToString().
    ///    Summary = "{Status} · {id} · {giây làm tròn}s · token {Input}/{Output}" (token không biết ⇒ "token ?");
    ///    nếu không Succeeded thêm " — {Detail}" (Detail cắt còn 200 ký tự).
    /// 4. Lỗi bất ngờ (trừ OperationCanceledException) ⇒ (ExitFailed, "Failed", "LỖI: {Kiểu}: {Message}", null).
    /// </summary>
    public async Task<DevRunOutcome> RunAsync(DevRunInput input, CancellationToken ct)
    {
        try
        {
            var cfg = _settings.Providers.FirstOrDefault(p => p.Id.Equals(input.ProviderId, StringComparison.OrdinalIgnoreCase));
            if (cfg is null)
                return new(ExitConfig, "Config", $"LỖI: không có provider {input.ProviderId}", null);
            if (!cfg.Enabled)
                return new(ExitConfig, "Config", $"LỖI: provider {cfg.Id} đang tắt", null);
            if (!cfg.Roles.Contains(AgentRole.Developer))
                return new(ExitConfig, "Config", $"LỖI: provider {cfg.Id} không có vai trò Developer", null);
            if (string.IsNullOrWhiteSpace(input.PromptFile) || !File.Exists(input.PromptFile))
                return new(ExitConfig, "Config", $"LỖI: không thấy file {input.PromptFile}", null);
            if (string.IsNullOrWhiteSpace(input.WorkDir) || !Directory.Exists(input.WorkDir))
                return new(ExitConfig, "Config", $"LỖI: không thấy thư mục {input.WorkDir}", null);
            if (_state.RestOf(cfg.Id, _now()) is { } rest)
                return new(ExitResting, "Resting", $"NGHỈ: {cfg.Id} nghỉ tới {rest.Until.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture)} ({rest.Reason})", null);

            Directory.CreateDirectory(input.LogDir);
            var req = new AgentRunRequest(input.WorkDir, input.PromptFile, input.Mode, input.Timeout, input.Model, null, input.LogDir);
            var r = await _factory(cfg).RunAsync(req, ct);
            _state.Record(r, _now());

            var exit = r.Status switch
            {
                RunStatus.Succeeded => ExitOk,
                RunStatus.QuotaExhausted => ExitQuota,
                RunStatus.NotLoggedIn => ExitLogin,
                RunStatus.TimedOut => ExitTimeout,
                _ => ExitFailed,
            };
            var sec = (int)Math.Round(r.Duration.TotalSeconds);
            var tok = r.Tokens.IsKnown ? $"token {r.Tokens.Input}/{r.Tokens.Output}" : "token ?";
            var summary = $"{r.Status} · {cfg.Id} · {sec}s · {tok}";
            if (r.Status != RunStatus.Succeeded)
            {
                var d = r.Detail ?? "";
                if (d.Length > 200) d = d[..200];
                summary += $" — {d}";
            }
            if (r.Status is not (RunStatus.Succeeded or RunStatus.QuotaExhausted or RunStatus.NotLoggedIn) && _state.RestOf(cfg.Id, _now()) is { } failureRest)
                summary += $" — CẢNH BÁO: {failureRest.Detail} Nghỉ tới {failureRest.Until:HH:mm dd/MM}.";
            return new(exit, r.Status.ToString(), summary, r);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(ExitFailed, "Failed", $"LỖI: {ex.GetType().Name}: {ex.Message}", null);
        }
    }

    /// <summary>
    /// JSON cho team.ps1 (camelCase, thụt lề): { "exitCode", "status", "summary", "provider", "model", "durationSec" (số nguyên, 0 nếu không có),
    /// "inputTokens", "outputTokens" (-1 nếu không biết), "conversationId", "text" (câu trả lời cuối), "detail" }.
    /// Result null ⇒ provider / model / conversationId null, text / detail "", durationSec 0, token -1.
    /// </summary>
    public static string OutcomeJson(DevRunOutcome o)
    {
        var r = o.Result;
        return JsonSerializer.Serialize(new
        {
            exitCode = o.ExitCode,
            status = o.Status,
            summary = o.Summary,
            provider = r?.ProviderId,
            model = r?.Model,
            durationSec = r is null ? 0 : (int)Math.Round(r.Duration.TotalSeconds),
            inputTokens = r is { Tokens.IsKnown: true } ? r.Tokens.Input : -1L,
            outputTokens = r is { Tokens.IsKnown: true } ? r.Tokens.Output : -1L,
            conversationId = r?.ConversationId,
            text = r?.Text ?? "",
            detail = r?.Detail ?? "",
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
