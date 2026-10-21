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
