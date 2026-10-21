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
        var hasH3 = lines.Any(l => l.TrimStart().StartsWith("### ", StringComparison.Ordinal));

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
            if (trimmedStart.StartsWith("### ", StringComparison.Ordinal))
            {
                if (currentTitle != null)
                {
                    rounds.Add(CreateRound(currentTitle, currentLines));
                    currentLines.Clear();
                }
                currentTitle = trimmedStart[4..].Trim();
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
