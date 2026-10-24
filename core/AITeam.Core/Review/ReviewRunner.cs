using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Review;

// Lệnh `aiteam review` (R-002 P0-3/P0-7) — Claude thiết kế khung; agent điền TODO(agent), KHÔNG đổi chữ ký public.
// team.ps1 gom sẵn file (task, diff, báo cáo cổng KT) rồi gọi aiteam review; đọc mã thoát + file --out.

public sealed record ReviewRunInput(
    string TaskFile,
    string DiffFile,
    string? VerifyFile,
    string WorkDir,
    string LogDir,
    /// <summary>Provider đã viết code (vd. "agy") — reviewer phải khác (cấm tự duyệt).</summary>
    string DeveloperProviderId,
    TimeSpan Timeout);

/// <summary>ExitCode: 0 PASS · 1 CHANGES_REQUESTED · 2 lỗi đầu vào / cấu hình · 3 BLOCKED. Notes: lý do bỏ qua reviewer, lần thử lại.</summary>
public sealed record ReviewRunOutcome(int ExitCode, string Summary, IReadOnlyList<string> Notes, ReviewResult? Result);

public sealed class ReviewRunner
{
    public const int ExitPass = 0, ExitChanges = 1, ExitConfig = 2, ExitBlocked = 3;
    /// <summary>Tối đa số reviewer thử lần lượt (reviewer đầu hết hạn mức / chưa đăng nhập ⇒ thử reviewer kế).</summary>
    public const int MaxAttempts = 2;

    private readonly ProviderSettings _settings;
    private readonly IProviderRouter _router;
    private readonly ProviderStateStore _state;
    private readonly Func<ProviderConfig, IAgentProvider> _factory;
    private readonly Func<DateTime> _now;

    public ReviewRunner(ProviderSettings settings, IProviderRouter router, ProviderStateStore state,
        Func<ProviderConfig, IAgentProvider> factory, Func<DateTime> now)
    {
        _settings = settings;
        _router = router;
        _state = state;
        _factory = factory;
        _now = now;
    }

    /// <summary>
    /// 1. TaskFile hoặc DiffFile không tồn tại ⇒ (ExitConfig, "LỖI: không thấy file &lt;đường dẫn&gt;", [], null) — không gọi provider.
    ///    Diff rỗng / toàn khoảng trắng ⇒ (ExitBlocked, "BLOCKED: diff rỗng — không có gì để review", [], null).
    ///    VerifyFile null hoặc không tồn tại ⇒ bỏ qua (null).
    /// 2. pack = new ReviewService().BuildPack(new ReviewPackInput(task, diff, verify, null)).
    /// 3. Lặp tối đa MaxAttempts lần:
    ///    - d = router.Pick(AgentRole.Reviewer, DeveloperProviderId, now()); thêm mọi d.Skipped chưa có vào Notes (giữ thứ tự, không trùng);
    ///    - !d.Ok ⇒ trả (ExitBlocked, "BLOCKED: không có reviewer khả dụng — " + string.Join("; ", d.Skipped), Notes, kết quả lần trước hoặc null);
    ///    - provider = factory(cấu hình của d.ProviderId) BỌC trong lớp ghi trạng thái: sau MỖI RunAsync gọi state.Record(kết quả, now());
    ///    - result = await new ReviewService().ReviewAsync(pack, provider bọc, DeveloperProviderId, WorkDir, LogDir, Timeout, ct);
    ///    - result không Blocked ⇒ dừng lặp;
    ///    - result Blocked VÀ state.RestOf(id, now()) != null (provider vừa bị đánh dấu nghỉ) VÀ còn lượt ⇒
    ///      Notes thêm "{id}: {result.BlockedReason} ⇒ thử reviewer khác", lặp tiếp; ngược lại dừng.
    /// 4. ExitCode theo Verdict (Pass 0, ChangesRequested 1, Blocked 3); Summary = Summarize(result).
    /// </summary>
    public Task<ReviewRunOutcome> RunAsync(ReviewRunInput input, CancellationToken ct)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Pass / ChangesRequested: "{PASS|CHANGES_REQUESTED} · {ProviderId} · {n} phát hiện ({b} blocker, {r} required, {o} optional) · {giây làm tròn}s".
    /// Blocked: "BLOCKED · {ProviderId}: {BlockedReason}".
    /// </summary>
    public static string Summarize(ReviewResult r)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Lời giao lại cho developer: chỉ finding Blocker / Required, theo thứ tự gốc. Không có ⇒ "".
    /// Dòng đầu "Reviewer {ProviderId} yêu cầu sửa:", mỗi finding 1 dòng "- [{blocker|required}] {File}:{Line} — {Evidence} ⇒ {Fix}", nối bằng "\n".
    /// </summary>
    public static string Feedback(ReviewResult r)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// JSON cho team.ps1 (UTF-8, camelCase, thụt lề): { "exitCode", "verdict" ("PASS"|"CHANGES_REQUESTED"|"BLOCKED"), "summary", "notes": [],
    /// "feedback", "provider", "diffHash" }. Result null ⇒ verdict theo ExitCode (0 PASS, 1 CHANGES_REQUESTED, còn lại BLOCKED), provider / diffHash null, feedback "".
    /// </summary>
    public static string OutcomeJson(ReviewRunOutcome o)
        => throw new NotImplementedException(); // TODO(agent)
}
