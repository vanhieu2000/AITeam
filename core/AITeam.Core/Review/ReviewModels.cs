using AITeam.Core.Providers;

namespace AITeam.Core.Review;

/// <summary>Kết luận review. Lỗi / quá giờ / không đọc được ⇒ Blocked — KHÔNG BAO GIỜ là Pass.</summary>
public enum ReviewVerdict { Pass, ChangesRequested, Blocked }

public enum FindingSeverity { Blocker, Required, Optional }

public sealed record ReviewFinding(FindingSeverity Severity, string File, int Line, string Evidence, string Fix);

/// <summary>Đầu vào gói review — người gọi (team.ps1 / aiteam review) gom từ git + file task + log.</summary>
public sealed record ReviewPackInput(
    string TaskFileText,
    string DiffText,
    string? VerifyReport,
    string? DesignExcerpt,
    /// <summary>Giới hạn ký tự của diff đưa cho reviewer; dài hơn thì cắt và ghi rõ đã cắt.</summary>
    int MaxDiffChars = 120_000);

/// <summary>Gói review đã dựng: lời nhắn cho reviewer + hash của đúng diff được review (để biết code đổi sau review).</summary>
public sealed record ReviewPack(string Prompt, string DiffHash, bool DiffTruncated);

public sealed record ReviewResult(
    ReviewVerdict Verdict,
    IReadOnlyList<ReviewFinding> Findings,
    IReadOnlyList<string> Unverified,
    string ProviderId,
    string? Model,
    TimeSpan Duration,
    TokenUsage Tokens,
    string DiffHash,
    /// <summary>Lý do khi Blocked (timeout, hết hạn mức, JSON hỏng, tự duyệt…); rỗng khi không Blocked.</summary>
    string BlockedReason);
