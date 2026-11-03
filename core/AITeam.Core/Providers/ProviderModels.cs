namespace AITeam.Core.Providers;

/// <summary>Vai trò trong team — tách khỏi provider: một provider làm được nhiều vai trò.</summary>
public enum AgentRole { Planner, Architect, Developer, Reviewer, QA, Manager }

/// <summary>Loại runtime. Chỉ dùng CLI đăng nhập chính thức của từng hãng.</summary>
public enum ProviderKind { Antigravity, Codex, ClaudeCli, OpenCode }

/// <summary>Chế độ chạy: viết code trong worktree, hoặc chỉ đọc (review / phân tích).</summary>
public enum RunMode { Develop, ReadOnly }

/// <summary>Kết cục một lần gọi. Lỗi được phân loại để chọn provider dự phòng — không retry mù.</summary>
public enum RunStatus
{
    Succeeded,
    Failed,
    TimedOut,
    /// <summary>Hết hạn mức / credits / 429 — chuyển provider khác, đánh dấu provider "nghỉ".</summary>
    QuotaExhausted,
    /// <summary>Chưa đăng nhập / phiên hết hạn — cần chủ dự án.</summary>
    NotLoggedIn,
    /// <summary>Provider từ chối cấu hình (model không hỗ trợ, tham số sai) — lỗi cấu hình, không retry.</summary>
    ConfigError,
    /// <summary>Không chạy được tiến trình (chưa cài, sai đường dẫn).</summary>
    Unavailable,
}

public sealed record TokenUsage(long Input, long CachedInput, long Output)
{
    public long? ReportedTotal { get; init; }
    public static readonly TokenUsage Unknown = new(-1, -1, -1);
    public bool IsKnown => Input >= 0;
    public long Total => IsKnown ? ReportedTotal ?? Input + Output : -1;
}

public sealed record AgentRunRequest(
    string WorkDir,
    string PromptFile,
    RunMode Mode,
    TimeSpan Timeout,
    string? Model = null,
    /// <summary>Lược đồ JSON bắt buộc cho câu trả lời (chỉ provider hỗ trợ, ví dụ Codex <c>--output-schema</c>).</summary>
    string? OutputSchemaFile = null,
    /// <summary>Thư mục provider được phép ghi log / kết quả ngoài WorkDir.</summary>
    string? LogDir = null)
{
    public string? Title { get; init; }
}

public sealed record AgentRunResult(
    string ProviderId,
    RunStatus Status,
    string Text,
    TokenUsage Tokens,
    TimeSpan Duration,
    string? Model,
    string? ConversationId,
    string Detail)
{
    public bool Ok => Status == RunStatus.Succeeded;
}

public sealed record ProviderHealth(string ProviderId, bool Installed, bool? LoggedIn, string Detail);

/// <summary>Một runtime AI chạy được từ dòng lệnh (agy, codex, claude…).</summary>
public interface IAgentProvider
{
    string Id { get; }
    ProviderKind Kind { get; }
    /// <summary>Provider có chế độ chỉ đọc được thực thi bằng sandbox của chính CLI (không chỉ bằng lời nhắn).</summary>
    bool HasReadOnlySandbox { get; }
    Task<ProviderHealth> CheckAsync(CancellationToken ct);
    Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct);
}
