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
    string? Model = null);

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
    /// 2. Directory.CreateDirectory(LogDir); r = await factory(cfg).RunAsync(new AgentRunRequest(WorkDir, PromptFile, RunMode.Develop, Timeout, Model, null, LogDir), ct);
    ///    state.Record(r, now()) (ghi nghỉ / xóa nghỉ).
    /// 3. ExitCode theo r.Status: Succeeded 0, QuotaExhausted 6, NotLoggedIn 7, TimedOut 8, còn lại 1. Status = r.Status.ToString().
    ///    Summary = "{Status} · {id} · {giây làm tròn}s · token {Input}/{Output}" (token không biết ⇒ "token ?");
    ///    nếu không Succeeded thêm " — {Detail}" (Detail cắt còn 200 ký tự).
    /// 4. Lỗi bất ngờ (trừ OperationCanceledException) ⇒ (ExitFailed, "Failed", "LỖI: {Kiểu}: {Message}", null).
    /// </summary>
    public Task<DevRunOutcome> RunAsync(DevRunInput input, CancellationToken ct)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// JSON cho team.ps1 (camelCase, thụt lề): { "exitCode", "status", "summary", "provider", "model", "durationSec" (số nguyên, 0 nếu không có),
    /// "inputTokens", "outputTokens" (-1 nếu không biết), "conversationId", "text" (câu trả lời cuối), "detail" }.
    /// Result null ⇒ provider / model / conversationId null, text / detail "", durationSec 0, token -1.
    /// </summary>
    public static string OutcomeJson(DevRunOutcome o)
        => throw new NotImplementedException(); // TODO(agent)
}
