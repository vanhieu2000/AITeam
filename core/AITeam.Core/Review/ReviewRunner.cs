using System.Text.Json;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Core.Skills;

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
    TimeSpan Timeout,
    /// <summary>Gốc AITeam tùy chọn; null giữ lời gọi cũ, không dò thư mục máy thật.</summary>
    string? Root = null,
    /// <summary>Hồ sơ tùy chọn; không tự đọc USERPROFILE.</summary>
    string? Profile = null);

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
    public async Task<ReviewRunOutcome> RunAsync(ReviewRunInput input, CancellationToken ct)
    {
        // Lỗi bất ngờ (thư mục log hỏng, provider ném lỗi…) ⇒ BLOCKED có lý do, vẫn ghi --out được — không bao giờ làm sập lệnh (Codex review V4-08).
        try { return await RunCoreAsync(input, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ReviewRunOutcome(ExitBlocked, $"BLOCKED: lỗi khi review — {ex.GetType().Name}: {ex.Message}", [], null);
        }
    }

    private async Task<ReviewRunOutcome> RunCoreAsync(ReviewRunInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.TaskFile) || !File.Exists(input.TaskFile))
            return new ReviewRunOutcome(ExitConfig, $"LỖI: không thấy file {input.TaskFile}", [], null);
        if (string.IsNullOrWhiteSpace(input.DiffFile) || !File.Exists(input.DiffFile))
            return new ReviewRunOutcome(ExitConfig, $"LỖI: không thấy file {input.DiffFile}", [], null);

        var taskText = await File.ReadAllTextAsync(input.TaskFile, ct);
        var diffText = await File.ReadAllTextAsync(input.DiffFile, ct);
        if (string.IsNullOrWhiteSpace(diffText))
            return new ReviewRunOutcome(ExitBlocked, "BLOCKED: diff rỗng — không có gì để review", [], null);

        var verifyText = !string.IsNullOrWhiteSpace(input.VerifyFile) && File.Exists(input.VerifyFile)
            ? await File.ReadAllTextAsync(input.VerifyFile, ct)
            : null;

        var service = new ReviewService();
        var pack = BuildReviewPack(input, taskText, diffText, verifyText);
        var notes = new List<string>();
        ReviewResult? result = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var d = _router.Pick(AgentRole.Reviewer, input.DeveloperProviderId, _now());
            foreach (var s in d.Skipped)
            {
                if (!notes.Contains(s)) notes.Add(s);
            }

            if (!d.Ok)
            {
                return new ReviewRunOutcome(
                    ExitBlocked,
                    "BLOCKED: không có reviewer khả dụng — " + string.Join("; ", d.Skipped),
                    notes,
                    result);
            }

            var cfg = _settings.Providers.First(p => p.Id.Equals(d.ProviderId, StringComparison.OrdinalIgnoreCase));
            var wrapped = new StateRecordingProvider(_factory(cfg), _state, _now);
            result = await service.ReviewAsync(pack, wrapped, input.DeveloperProviderId, input.WorkDir, input.LogDir, input.Timeout, ct);

            if (result.Verdict != ReviewVerdict.Blocked)
                break;

            if (_state.RestOf(d.ProviderId!, _now()) != null && attempt < MaxAttempts)
            {
                notes.Add($"{d.ProviderId}: {result.BlockedReason} ⇒ thử reviewer khác");
                continue;
            }
            break;
        }

        var exit = result!.Verdict switch
        {
            ReviewVerdict.Pass => ExitPass,
            ReviewVerdict.ChangesRequested => ExitChanges,
            _ => ExitBlocked,
        };
        return new ReviewRunOutcome(exit, Summarize(result), notes, result);
    }


    /// <summary>
    /// Pass / ChangesRequested: "{PASS|CHANGES_REQUESTED} · {ProviderId} · {n} phát hiện ({b} blocker, {r} required, {o} optional) · {giây làm tròn}s".
    /// Blocked: "BLOCKED · {ProviderId}: {BlockedReason}".
    /// </summary>
    public static string Summarize(ReviewResult r)
    {
        if (r.Verdict == ReviewVerdict.Blocked)
            return $"BLOCKED · {r.ProviderId}: {r.BlockedReason}";

        var tag = r.Verdict == ReviewVerdict.Pass ? "PASS" : "CHANGES_REQUESTED";
        var b = r.Findings.Count(f => f.Severity == FindingSeverity.Blocker);
        var req = r.Findings.Count(f => f.Severity == FindingSeverity.Required);
        var opt = r.Findings.Count(f => f.Severity == FindingSeverity.Optional);
        var secs = (int)Math.Round(r.Duration.TotalSeconds);
        return $"{tag} · {r.ProviderId} · {r.Findings.Count} phát hiện ({b} blocker, {req} required, {opt} optional) · {secs}s";
    }


    /// <summary>
    /// Lời giao lại cho developer: chỉ finding Blocker / Required, theo thứ tự gốc. Không có ⇒ "".
    /// Dòng đầu "Reviewer {ProviderId} yêu cầu sửa:", mỗi finding 1 dòng "- [{blocker|required}] {File}:{Line} — {Evidence} ⇒ {Fix}", nối bằng "\n".
    /// </summary>
    public static string Feedback(ReviewResult r)
    {
        var items = r.Findings.Where(f => f.Severity is FindingSeverity.Blocker or FindingSeverity.Required).ToList();
        if (items.Count == 0) return "";

        var lines = new List<string>(items.Count + 1) { $"Reviewer {r.ProviderId} yêu cầu sửa:" };
        foreach (var f in items)
        {
            var sev = f.Severity == FindingSeverity.Blocker ? "blocker" : "required";
            lines.Add($"- [{sev}] {f.File}:{f.Line} — {f.Evidence} ⇒ {f.Fix}");
        }
        return string.Join("\n", lines);
    }


    /// <summary>
    /// JSON cho team.ps1 (UTF-8, camelCase, thụt lề): { "exitCode", "verdict" ("PASS"|"CHANGES_REQUESTED"|"BLOCKED"), "summary", "notes": [],
    /// "feedback", "provider", "diffHash" }. Result null ⇒ verdict theo ExitCode (0 PASS, 1 CHANGES_REQUESTED, còn lại BLOCKED), provider / diffHash null, feedback "".
    /// </summary>
    public static string OutcomeJson(ReviewRunOutcome o)
    {
        var verdict = o.Result?.Verdict switch
        {
            ReviewVerdict.Pass => "PASS",
            ReviewVerdict.ChangesRequested => "CHANGES_REQUESTED",
            ReviewVerdict.Blocked => "BLOCKED",
            null => o.ExitCode switch { ExitPass => "PASS", ExitChanges => "CHANGES_REQUESTED", _ => "BLOCKED" },
            _ => "BLOCKED",
        };
        var payload = new
        {
            exitCode = o.ExitCode,
            verdict,
            summary = o.Summary,
            notes = o.Notes,
            feedback = o.Result != null ? Feedback(o.Result) : "",
            provider = o.Result?.ProviderId,
            diffHash = o.Result?.DiffHash,
        };
        return JsonSerializer.Serialize(payload, JsonOpts);
    }

    /// <summary>
    /// Dựng pack bằng ReviewService rồi nối SkillPromptBuilder.BuildReviewInstructions khi Root có giá trị.
    /// Root null/rỗng: pack giống hệt lời gọi cũ. Chỉ đổi Prompt; giữ DiffHash và DiffTruncated.
    /// Thiếu skill/dự án không lỗi, không suy stack từ diff. JSON hỏng không bị nuốt;
    /// RunAsync trả "BLOCKED: lỗi khi review — {loại lỗi}: {thông báo}" như hiện tại.
    /// </summary>
    public static ReviewPack BuildReviewPack(ReviewRunInput input, string taskText, string diffText, string? verifyText)
    {
        var service = new ReviewService();
        var basePack = service.BuildPack(new ReviewPackInput(taskText, diffText, verifyText, null));

        if (string.IsNullOrWhiteSpace(input.Root))
            return basePack;

        var instructions = SkillPromptBuilder.BuildReviewInstructions(input.Root, input.WorkDir, input.TaskFile, input.Profile);
        if (string.IsNullOrWhiteSpace(instructions))
            return basePack;

        var prompt = basePack.Prompt.TrimEnd() + "\n\n" + instructions.Trim() + "\n";
        return new ReviewPack(prompt, basePack.DiffHash, basePack.DiffTruncated);
    }
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private sealed class StateRecordingProvider(IAgentProvider inner, ProviderStateStore state, Func<DateTime> now) : IAgentProvider
    {
        public string Id => inner.Id;
        public ProviderKind Kind => inner.Kind;
        public bool HasReadOnlySandbox => inner.HasReadOnlySandbox;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => inner.CheckAsync(ct);
        public async Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
        {
            var res = await inner.RunAsync(request, ct);
            state.Record(res, now());
            return res;
        }
    }

}
