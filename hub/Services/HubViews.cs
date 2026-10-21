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

public sealed record RunPage(IReadOnlyList<AgentRun> Items, int Total, int Page, int PageCount);

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
            return new RunPage(Array.Empty<AgentRun>(), 0, 1, 1);
        }

        int pageCount = (total + pageSize - 1) / pageSize;
        int clampedPage = Math.Clamp(page, 1, pageCount);
        var items = filtered.Skip((clampedPage - 1) * pageSize).Take(pageSize).ToList();

        return new RunPage(items, total, clampedPage, pageCount);
    }
}
