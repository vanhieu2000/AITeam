using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public enum RunVerdict { Running, Success, CreditsExhausted, Error }

public static partial class RunVerdicts
{
    [GeneratedRegex(@"(?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits")]
    private static partial Regex QuotaExhaustedRegex();

    // Đang chạy ⇒ Running; Status "SUCCESS" ⇒ Success; còn lại: Error/Response/Status khớp
    // (?i)RESOURCE_EXHAUSTED|\bquota\b|rate.?limit|\b429\b|credits? balance|out of credits ⇒ CreditsExhausted; còn lại Error.
    public static RunVerdict Classify(AgentRun run)
    {
        if (run.State == RunState.Running)
        {
            return RunVerdict.Running;
        }

        if (run.State == RunState.Success || string.Equals(run.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return RunVerdict.Success;
        }

        var regex = QuotaExhaustedRegex();
        if ((run.Error != null && regex.IsMatch(run.Error)) ||
            (run.Response != null && regex.IsMatch(run.Response)) ||
            (run.Status != null && regex.IsMatch(run.Status)))
        {
            return RunVerdict.CreditsExhausted;
        }

        return RunVerdict.Error;
    }

    public static string Label(RunVerdict v) => v switch
    {
        RunVerdict.Running => "Đang chạy",
        RunVerdict.Success => "Thành công",
        RunVerdict.CreditsExhausted => "Hết credits",
        RunVerdict.Error => "Lỗi",
        _ => "Lỗi"
    };
}

public sealed record AntigravityStatus(bool CreditsExhausted, DateTime? At, string? Project, string? Task);

public static class AntigravityStatuses
{
    // Lần chạy ĐÃ KẾT THÚC mới nhất (theo Time): CreditsExhausted ⇒ (true, Time, Project, Task); còn lại / không có ⇒ (false, null, null, null).
    public static AntigravityStatus From(IEnumerable<AgentRun> runs)
    {
        var latestFinished = runs
            .Where(r => r.State != RunState.Running)
            .OrderByDescending(r => r.Time)
            .FirstOrDefault();

        if (latestFinished != null && RunVerdicts.Classify(latestFinished) == RunVerdict.CreditsExhausted)
        {
            return new AntigravityStatus(true, latestFinished.Time, latestFinished.Project, latestFinished.Task);
        }

        return new AntigravityStatus(false, null, null, null);
    }
}

public sealed record ActivityItem(DateTime Time, RunVerdict Verdict, string Project, string Task, int Attempt);

public static class ActivityFeed
{
    // Từ các lần chạy: mới nhất trước, tối đa `take`, bỏ lần đang chạy.
    public static IReadOnlyList<ActivityItem> Build(IEnumerable<AgentRun> runs, int take = 6)
    {
        return runs
            .Where(r => r.State != RunState.Running)
            .OrderByDescending(r => r.Time)
            .Take(take)
            .Select(r => new ActivityItem(r.Time, RunVerdicts.Classify(r), r.Project, r.Task, r.Attempt))
            .ToList();
    }
}

public enum RunFilter { All, Errors, Success }

public sealed record RunPage(IReadOnlyList<AgentRun> Items, int Total, int Page, int PageCount)
{
    public int PageSize { get; init; } = 8;
}

public static class RunHistory
{
    // Lọc: Errors = Error + CreditsExhausted; Success = Success; All = mọi lần đã kết thúc (bỏ Running).
    // project null/rỗng ⇒ mọi dự án. Sắp Time giảm dần. page bắt đầu từ 1; page ngoài khoảng ⇒ kẹp về [1, PageCount];
    // không có dòng ⇒ PageCount = 1, Items rỗng.
    public static RunPage Page(IEnumerable<AgentRun> runs, RunFilter filter, string? project, int page, int pageSize = 8)
    {
        if (pageSize <= 0) pageSize = 8;

        var query = runs.Where(r =>
        {
            var verdict = RunVerdicts.Classify(r);
            if (verdict == RunVerdict.Running || r.State == RunState.Running)
            {
                return false;
            }

            return filter switch
            {
                RunFilter.Errors => verdict == RunVerdict.Error || verdict == RunVerdict.CreditsExhausted,
                RunFilter.Success => verdict == RunVerdict.Success,
                _ => true
            };
        });

        if (!string.IsNullOrWhiteSpace(project))
        {
            string p = project.Trim();
            query = query.Where(r => string.Equals(r.Project, p, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.OrderByDescending(r => r.Time).ToList();
        int total = filtered.Count;

        if (total == 0)
        {
            return new RunPage(Array.Empty<AgentRun>(), 0, 1, 1) { PageSize = pageSize };
        }

        int pageCount = (total + pageSize - 1) / pageSize;
        int clampedPage = Math.Clamp(page, 1, pageCount);
        var items = filtered.Skip((clampedPage - 1) * pageSize).Take(pageSize).ToList();

        return new RunPage(items, total, clampedPage, pageCount) { PageSize = pageSize };
    }
}

public enum ReviewVerdict { Pass, NeedsFix, Other }
public enum ReviewSeverity { Blocker, Required, Optional, None }
public sealed record ReviewItem(ReviewSeverity Severity, string Text);
public sealed record ReviewRound(string Title, ReviewVerdict Verdict, string Markdown, IReadOnlyList<ReviewItem> Items);

public static partial class ReviewParser
{
    [GeneratedRegex(@"^\*\*(?<sev>Blocker|Required|Optional)\*\*(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SeverityHeaderRegex();

    [GeneratedRegex(@"^\s*(?:(?:\d+[\.\)])|[-*+])\s+(?<text>.+)$")]
    private static partial Regex ListItemRegex();

    public static IReadOnlyList<ReviewRound> Parse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown) || markdown.Trim() == "<!-- Claude điền -->")
        {
            return Array.Empty<ReviewRound>();
        }

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var hasH3 = lines.Any(l => IsRoundHeading(l));

        if (!hasH3)
        {
            var roundItems = ParseItems(lines);
            return new[] { new ReviewRound(string.Empty, ReviewVerdict.Other, markdown.Trim(), roundItems) };
        }

        var rounds = new List<ReviewRound>();
        string? currentTitle = null;
        var currentLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedStart = line.TrimStart();
            if (IsRoundHeading(trimmedStart))
            {
                if (currentTitle != null)
                {
                    rounds.Add(CreateRound(currentTitle, currentLines));
                    currentLines.Clear();
                }
                currentTitle = trimmedStart.StartsWith("### ", StringComparison.Ordinal) ? trimmedStart[4..].Trim() : trimmedStart.Trim().Trim('*');
            }
            else
            {
                if (currentTitle != null)
                {
                    currentLines.Add(line);
                }
            }
        }

        if (currentTitle != null)
        {
            rounds.Add(CreateRound(currentTitle, currentLines));
        }

        return rounds;
    }

    private static bool IsRoundHeading(string line)
    {
        var text = line.Trim();
        return text.StartsWith("### ", StringComparison.Ordinal) ||
            Regex.IsMatch(text, @"^(?:\*\*)?Vòng\s+\d+\s*(?:[—–:-]|$)", RegexOptions.IgnoreCase);
    }

    private static ReviewRound CreateRound(string title, List<string> lines)
    {
        var roundMd = string.Join("\n", lines).Trim();
        var verdict = DetermineVerdict(title);
        var items = ParseItems(lines);
        return new ReviewRound(title, verdict, roundMd, items);
    }

    public static ReviewVerdict DetermineVerdict(string title)
    {
        if (title.Contains("CẦN SỬA", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("CẦN LÀM NỐT", StringComparison.OrdinalIgnoreCase))
        {
            return ReviewVerdict.NeedsFix;
        }

        if (title.Contains("ĐẠT", StringComparison.OrdinalIgnoreCase) &&
            !title.Contains("CẦN", StringComparison.OrdinalIgnoreCase))
        {
            return ReviewVerdict.Pass;
        }

        return ReviewVerdict.Other;
    }

    private static IReadOnlyList<ReviewItem> ParseItems(IEnumerable<string> lines)
    {
        var items = new List<ReviewItem>();
        var currentSeverity = ReviewSeverity.None;
        var sevRegex = SeverityHeaderRegex();
        var listRegex = ListItemRegex();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var sevMatch = sevRegex.Match(line);
            if (sevMatch.Success)
            {
                var sevStr = sevMatch.Groups["sev"].Value;
                currentSeverity = ParseSeverity(sevStr);

                var rest = sevMatch.Groups["rest"].Value.Trim();
                var colonIdx = rest.IndexOf(':');
                if (colonIdx >= 0)
                {
                    var inlineText = rest[(colonIdx + 1)..].Trim();
                    inlineText = CleanListItemPrefix(inlineText);
                    if (!string.IsNullOrWhiteSpace(inlineText))
                    {
                        items.Add(new ReviewItem(currentSeverity, inlineText));
                    }
                }
                continue;
            }

            var listMatch = listRegex.Match(line);
            if (listMatch.Success)
            {
                var text = listMatch.Groups["text"].Value.Trim();
                items.Add(new ReviewItem(currentSeverity, text));
                continue;
            }
        }

        return items;
    }

    private static ReviewSeverity ParseSeverity(string str)
    {
        if (str.Equals("Blocker", StringComparison.OrdinalIgnoreCase)) return ReviewSeverity.Blocker;
        if (str.Equals("Required", StringComparison.OrdinalIgnoreCase)) return ReviewSeverity.Required;
        if (str.Equals("Optional", StringComparison.OrdinalIgnoreCase)) return ReviewSeverity.Optional;
        return ReviewSeverity.None;
    }

    private static string CleanListItemPrefix(string text)
    {
        var listMatch = ListItemRegex().Match(text);
        return listMatch.Success ? listMatch.Groups["text"].Value.Trim() : text;
    }
}

public static class BoardViews
{
    private static int ExtractNumber(string id)
    {
        var match = Regex.Match(id, @"\d+");
        return match.Success && int.TryParse(match.Value, out var n) ? n : -1;
    }

    public static (IReadOnlyList<TaskCard> Items, int Remaining) MergedPreview(IEnumerable<TaskCard> cards, int take = 7)
    {
        var merged = cards
            .Where(c => c.State == TaskState.Merged)
            .OrderByDescending(c => ExtractNumber(c.Id))
            .ThenByDescending(c => c.Id)
            .ToList();

        var items = merged.Take(take).ToList();
        var remaining = Math.Max(0, merged.Count - items.Count);
        return (items, remaining);
    }
}
