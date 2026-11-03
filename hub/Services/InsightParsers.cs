using AITeam.Core.Metrics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>
/// Hàm thuần đọc dữ liệu Hub v3 từ chuỗi (không đụng đĩa) — R-003 / H3-01. Thiết kế: docs/design/HUB-V3.md mục 3.
/// Mọi hàm KHÔNG ném lỗi với dữ liệu hỏng / thiếu: trả giá trị mặc định (Unknown, null, danh sách rỗng).
/// Tiêu đề mục so khớp sau Trim, không phân biệt hoa thường; dòng có thể kết thúc bằng \r\n hoặc \n.
/// </summary>
public static class InsightParsers
{
    private static readonly Regex DifficultyRx = new(@"(?m)^[ \t]*-\s*\*\*Độ khó:\*\*\s*([SsMmLl])\b", RegexOptions.Compiled);
    private static readonly Regex RequestRx = new(@"(?m)^[ \t]*-\s*\*\*Yêu cầu:\*\*\s*(R-\d{3})\b", RegexOptions.Compiled);
    private static readonly Regex StageIndexRx = new(@"^(\d+)", RegexOptions.Compiled);
    private static readonly Regex ReviewIssueRx = new(@"^\s*-\s*\[(AG|TK|CK|NV|HT)\]\s*(?:\(([^)]*)\)\s*)?(.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex ReviewRoundRx = new(@"(?m)^[ \t]*###\s+Vòng\s+(\d+)\s*[—–-]\s*(\S+)", RegexOptions.Compiled);
    private static readonly Regex ImpIdRx = new(@"^(IMP-\d{3})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HtmlCommentRx = new(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex StatusLineRx = new(@"(?m)^[ \t]*-\s*\*\*Trạng thái:\*\*\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex TasksLineRx = new(@"(?m)^[ \t]*-\s*\*\*Task trong chu kỳ:\*\*\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex StaffNameRx = new(@"(?m)^[ \t]*-\s*\*\*Tên:\*\*\s*(.+)$", RegexOptions.Compiled);

    /// <summary>Tên 7 giai đoạn chuẩn (Index 1..7) — dùng khi một giai đoạn không có dòng nào.</summary>
    public static readonly IReadOnlyList<string> StageNames = new[]
    {
        "1. Tiếp nhận & quản lý", "2. Ý tưởng", "3. Plan / thiết kế", "4. Giao việc",
        "5. Thực hiện code", "6. Review / QC", "7. Đánh giá & bàn giao",
    };

    /// <summary>Dòng "- **Độ khó:** M" (chấp nhận "M (vừa)", chữ thường "m"). Không có / sai ⇒ Unknown.</summary>
    public static Difficulty ParseDifficulty(string taskMarkdown)
    {
        var m = DifficultyRx.Match(taskMarkdown ?? "");
        return !m.Success ? Difficulty.Unknown : char.ToUpperInvariant(m.Groups[1].Value[0]) switch
        {
            'S' => Difficulty.S, 'M' => Difficulty.M, 'L' => Difficulty.L, _ => Difficulty.Unknown,
        };
    }

    /// <summary>Dòng "- **Yêu cầu:** R-002" ⇒ "R-002" (khớp R-\d{3}). Không có ⇒ null.</summary>
    public static string? ParseRequest(string taskMarkdown)
        => RequestRx.Match(taskMarkdown ?? "") is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>
    /// Bảng trong mục "## Đánh giá quy trình" (tới mục "## " kế tiếp). Bỏ dòng tiêu đề và dòng "|---|".
    /// Mỗi dòng 7 ô: Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất. Thiếu ô ⇒ ô rỗng.
    /// Ô chỉ có "—" / "-" ⇒ "". Bỏ dấu ` quanh nội dung KHÔNG cần (giữ nguyên văn). Không có mục ⇒ rỗng.
    /// </summary>
    public static IReadOnlyList<StageScore> ParseProcessReview(string taskMarkdown)
    {
        var result = new List<StageScore>();
        foreach (var raw in GetSectionLines(taskMarkdown, "Đánh giá quy trình"))
        {
            var line = raw.Trim();
            if (!line.StartsWith('|')) continue;
            var inner = line.EndsWith('|') ? line[1..^1] : line[1..];
            var cells = inner.Split('|');
            if (cells.All(c => c.Trim().Trim('-', ':').Length == 0)) continue;
            var stage = CleanCell(cells, 0);
            if (string.IsNullOrEmpty(stage) || stage.Equals("Giai đoạn", StringComparison.OrdinalIgnoreCase)) continue;
            var idx = StageIndexRx.Match(stage) is { Success: true } im && int.TryParse(im.Groups[1].Value, out var n) ? n : 0;
            int? score = int.TryParse(CleanCell(cells, 2), out var sc) && sc is >= 1 and <= 5 ? sc : null;
            result.Add(new StageScore(idx, stage, CleanCell(cells, 1), score, CleanCell(cells, 3), CleanCell(cells, 4), CleanCell(cells, 5), CleanCell(cells, 6)));
        }
        return result;
    }

    /// <summary>
    /// Trong mục "## Review (Claude)" (tới "## " kế tiếp; "### Vòng N" vẫn thuộc mục): mọi dòng (kể cả thụt lề)
    /// dạng "- [AG] (giao diện) mô tả" ⇒ Cause "AG", Kind "giao diện", Text "mô tả". Không có "(loại)" ⇒ Kind "".
    /// Chỉ nhận 5 mã AG, TK, CK, NV, HT (dòng "- [AG|TK|CK] …" của mẫu KHÔNG tính). Dấu ";" / "." cuối Text bỏ đi.
    /// </summary>
    public static IReadOnlyList<ReviewIssue> ParseReviewIssues(string taskMarkdown, string project, string task)
    {
        return FindingParser.Parse(string.Join("\n", GetSectionLines(taskMarkdown, "Review (Claude)")))
            .Select(f => new ReviewIssue(project ?? "", task ?? "", f.Cause, f.Kind,
                f.Text.TrimEnd(';', '.').TrimEnd())).ToList();
    }

    /// <summary>
    /// "### Vòng N — ĐẠT" / "### Vòng N — CẦN SỬA" (gạch "—" hoặc "-"; có thể kèm "(ngày, ai)").
    /// Rounds = N lớn nhất (0 nếu không có); PassedRound1 = có "Vòng 1" với chữ "ĐẠT" ngay sau gạch.
    /// </summary>
    public static (int Rounds, bool PassedRound1) ParseReviewRounds(string taskMarkdown)
    {
        int maxRound = 0;
        bool passed1 = false;
        foreach (Match m in ReviewRoundRx.Matches(taskMarkdown ?? ""))
        {
            if (!int.TryParse(m.Groups[1].Value, out var n)) continue;
            if (n > maxRound) maxRound = n;
            if (n == 1 && m.Groups[2].Value.Equals("ĐẠT", StringComparison.OrdinalIgnoreCase)) passed1 = true;
        }
        return (maxRound, passed1);
    }

    /// <summary>
    /// Một file metrics/runs/*.json (mẫu thật trong test nghiệm thu). "run" có thể là chuỗi "3" hoặc số.
    /// "at" là giờ địa phương không múi ("2026-10-23T23:56:27"). Thiếu project / task / at hoặc JSON hỏng ⇒ null.
    /// Trường số thiếu ⇒ 0; bool thiếu ⇒ false; "request" thiếu / rỗng ⇒ null.
    /// </summary>
    public static RunRecord? ParseRun(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var atStr = GetStr(root, "at");
            var project = GetStr(root, "project");
            var task = GetStr(root, "task");
            if (string.IsNullOrWhiteSpace(atStr) || string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(task)) return null;
            if (!DateTime.TryParse(atStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)) return null;
            var req = GetStr(root, "request");
            return new RunRecord(at, GetStr(root, "machine") ?? "", project, task, (int)GetNum(root, "run"),
                GetStr(root, "agent") ?? "", GetStr(root, "status") ?? "", (int)GetNum(root, "durationSec"),
                GetNum(root, "totalTokens"), (int)GetNum(root, "linesAdded"),
                root.TryGetProperty("verifyOk", out var vo) && vo.ValueKind == JsonValueKind.True,
                root.TryGetProperty("autoRetry", out var ar) && ar.ValueKind == JsonValueKind.True,
                string.IsNullOrWhiteSpace(req) ? null : req,
                GetStr(root, "conversationId") is { Length: > 0 } conv ? conv : null);
        }
        catch { return null; }
    }

    /// <summary>
    /// improvements/IMP-001-ten.md. Id = "IMP-001" lấy từ TÊN FILE (khớp ^IMP-\d{3}); không khớp ⇒ null.
    /// Title = dòng "# " đầu tiên bỏ "# " (không có ⇒ Id). Status = giá trị "- **Trạng thái:**" đã bỏ chú thích &lt;!-- --&gt; và Trim.
    /// Tasks = "- **Task trong chu kỳ:**" tách theo dấu phẩy, Trim, bỏ rỗng, bỏ chú thích.
    /// </summary>
    public static Improvement? ParseImprovement(string fileName, string markdown)
    {
        var nameOnly = Path.GetFileName(fileName ?? "");
        var idMatch = ImpIdRx.Match(nameOnly);
        if (!idMatch.Success) return null;
        var id = idMatch.Groups[1].Value.ToUpperInvariant();
        var md = markdown ?? "";
        var titleLine = md.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("# "));
        var title = titleLine != null && StripComments(titleLine[2..]) is { Length: > 0 } t ? t : id;
        var status = StatusLineRx.Match(md) is { Success: true } sm ? StripComments(sm.Groups[1].Value) : "";
        var tasks = TasksLineRx.Match(md) is { Success: true } tm
            ? StripComments(tm.Groups[1].Value).Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0 && s is not "—" and not "-").ToList()
            : new List<string>();
        return new Improvement(id, title, status, tasks, nameOnly, md);
    }

    /// <summary>staff/A-minh.md: mã = phần trước "-" đầu tiên của tên file ("A"); tên = giá trị "- **Tên:**". Thiếu ⇒ null.</summary>
    public static (string Agent, string Name)? ParseStaff(string fileName, string markdown)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName ?? "");
        var dash = baseName.IndexOf('-');
        var agent = (dash > 0 ? baseName[..dash] : baseName).Trim();
        if (string.IsNullOrEmpty(agent) || StaffNameRx.Match(markdown ?? "") is not { Success: true } m) return null;
        var name = StripComments(m.Groups[1].Value);
        return string.IsNullOrEmpty(name) ? null : (agent, name);
    }

    /// <summary>
    /// Biểu đồ thời gian từ các lượt (đã lọc đúng task), sắp theo At tăng dần:
    /// - lượt đầu ⇒ Coding [At − DurationSec, At]; các lượt sau ⇒ Fix [At − DurationSec, At];
    /// - mergedAt có và &gt; At lượt cuối ⇒ thêm Review [At lượt cuối, mergedAt] rồi Merge [mergedAt, mergedAt];
    /// - mergedAt có nhưng không có lượt nào ⇒ chỉ Merge; không mergedAt ⇒ không có Review / Merge.
    /// </summary>
    public static IReadOnlyList<TimelineSegment> BuildTimeline(IReadOnlyList<RunRecord> runs, DateTime? mergedAt)
    {
        var list = new List<TimelineSegment>();
        var sorted = (runs ?? Array.Empty<RunRecord>()).OrderBy(r => r.At).ToList();
        for (int i = 0; i < sorted.Count; i++)
            list.Add(new TimelineSegment(i == 0 ? SegmentKind.Coding : SegmentKind.Fix, sorted[i].At.AddSeconds(-Math.Max(0, sorted[i].DurationSec)), sorted[i].At));
        if (mergedAt is { } m)
        {
            if (sorted.Count == 0) list.Add(new TimelineSegment(SegmentKind.Merge, m, m));
            else if (m > sorted[^1].At)
            {
                list.Add(new TimelineSegment(SegmentKind.Review, sorted[^1].At, m));
                list.Add(new TimelineSegment(SegmentKind.Merge, m, m));
            }
        }
        return list;
    }

    /// <summary>
    /// Đổi số đo cộng dồn thành số của từng lượt (giống Get-RunDeltas trong team.ps1): trong cùng ConversationId, sắp theo At,
    /// DurationSec / TotalTokens = max(0, giá trị − giá trị lượt trước). ConversationId null ⇒ giữ nguyên. Giữ thứ tự At tăng dần.
    /// </summary>
    public static IReadOnlyList<RunRecord> ToDeltas(IEnumerable<RunRecord> runs)
    {
        var result = new List<RunRecord>();
        foreach (var g in runs.GroupBy(r => r.ConversationId ?? Guid.NewGuid().ToString()))
        {
            int prevD = 0; long prevT = 0;
            foreach (var r in g.OrderBy(r => r.At))
            {
                result.Add(r with { DurationSec = Math.Max(0, r.DurationSec - prevD), TotalTokens = Math.Max(0, r.TotalTokens - prevT) });
                if (r.ConversationId != null) { prevD = r.DurationSec; prevT = r.TotalTokens; }
            }
        }
        return result.OrderBy(r => r.At).ToList();
    }

    private static IEnumerable<string> GetSectionLines(string markdown, string sectionTitle)
    {
        if (string.IsNullOrEmpty(markdown)) yield break;
        var target = "## " + sectionTitle;
        bool inside = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.Trim();
            if (!inside) { if (trimmed.Equals(target, StringComparison.OrdinalIgnoreCase)) inside = true; }
            else if (trimmed.StartsWith("## ", StringComparison.Ordinal)) yield break;
            else yield return line;
        }
    }

    private static string CleanCell(string[] cells, int i)
        => i < cells.Length && cells[i].Trim() is var v && v is not ("—" or "-" or "–") ? v : "";

    private static string StripComments(string text) => HtmlCommentRx.Replace(text, "").Trim();

    private static string? GetStr(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static long GetNum(JsonElement obj, string prop)
        => !obj.TryGetProperty(prop, out var el) ? 0L
         : el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var n) ? n
         : el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 0L;
}
