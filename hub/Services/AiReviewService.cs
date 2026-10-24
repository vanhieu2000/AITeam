using System.Text.Json;
using System.Text.RegularExpressions;

namespace AITeam.Hub.Services;

// R-002 P0-8: Hub hiện kết luận review AI do `team.ps1 assign` (aiteam review) ghi ở <WorkDir>/logs/<Task>.review-<n>.json.

/// <summary>Một lần review AI. Verdict: PASS / CHANGES_REQUESTED / BLOCKED (file hỏng ⇒ không hiện).</summary>
public sealed record AiReview(int Attempt, string Verdict, string Summary, string? Provider, IReadOnlyList<string> Notes, string Feedback, DateTime At);

public interface IAiReviewService
{
    /// <summary>Các lần review AI của task, mới nhất trước. taskId sai mẫu / dự án không có / thư mục không có ⇒ rỗng.</summary>
    IReadOnlyList<AiReview> GetReviews(string project, string taskId);
}

public sealed class AiReviewService : IAiReviewService
{
    private static readonly Regex SafeId = new(@"^[A-Za-z0-9][A-Za-z0-9-]*$", RegexOptions.Compiled);
    private readonly ITeamConfigService _config;

    public AiReviewService(ITeamConfigService config) => _config = config;

    public IReadOnlyList<AiReview> GetReviews(string project, string taskId)
    {
        if (string.IsNullOrEmpty(taskId) || !SafeId.IsMatch(taskId)) return Array.Empty<AiReview>();
        var p = _config.FindProject(project);
        var logs = p == null || string.IsNullOrEmpty(p.WorkDir) ? null : Path.Combine(p.WorkDir, "logs");
        if (logs == null || !Directory.Exists(logs)) return Array.Empty<AiReview>();

        var fileRx = new Regex("^" + Regex.Escape(taskId) + @"\.review-(\d+)\.json$", RegexOptions.IgnoreCase);
        var list = new List<AiReview>();
        foreach (var f in Directory.EnumerateFiles(logs, taskId + ".review-*.json"))
        {
            var m = fileRx.Match(Path.GetFileName(f));
            if (!m.Success) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(f));
                var r = doc.RootElement;
                string S(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                var verdict = S("verdict");
                if (verdict is not ("PASS" or "CHANGES_REQUESTED" or "BLOCKED")) continue;
                var notes = r.TryGetProperty("notes", out var n) && n.ValueKind == JsonValueKind.Array
                    ? n.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
                    : new List<string>();
                list.Add(new AiReview(int.Parse(m.Groups[1].Value), verdict, S("summary"), S("provider") is { Length: > 0 } pv ? pv : null,
                    notes, S("feedback"), File.GetLastWriteTime(f)));
            }
            catch { /* file hỏng / đang ghi ⇒ bỏ qua */ }
        }
        return list.OrderByDescending(x => x.Attempt).ToList();
    }
}
