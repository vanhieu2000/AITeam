using System.Text.RegularExpressions;

namespace AITeam.Core.Providers;

/// <summary>
/// Phân loại lỗi provider từ output thật. Bài học team.ps1: "429" / "quota" phải đứng riêng
/// (từng khớp nhầm số token "42953"); thành công thì không bao giờ là lỗi hạn mức.
/// </summary>
public static class ErrorClassifier
{
    private static readonly Regex Quota = new(
        @"(?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits|usage limit|exceeded your (current )?(quota|usage)",
        RegexOptions.Compiled);
    private static readonly Regex Login = new(
        @"(?i)not logged in|please (log|sign) ?in|login required|unauthenticated|\b401\b|session (has )?expired|ErrNeedsLogin",
        RegexOptions.Compiled);
    private static readonly Regex Config = new(
        @"(?i)model .{0,40}not supported|unknown model|invalid_request_error|unrecognized (argument|option)|unexpected argument",
        RegexOptions.Compiled);

    public static RunStatus Classify(string text, bool succeeded)
    {
        if (succeeded) return RunStatus.Succeeded;
        if (string.IsNullOrWhiteSpace(text)) return RunStatus.Failed;
        if (Quota.IsMatch(text)) return RunStatus.QuotaExhausted;
        if (Login.IsMatch(text)) return RunStatus.NotLoggedIn;
        if (Config.IsMatch(text)) return RunStatus.ConfigError;
        return RunStatus.Failed;
    }
}
