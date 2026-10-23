using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Dữ liệu Hub v3: Tổng quan, Đội ngũ, Quy trình & cải tiến (R-003). Tính lại mỗi lần gọi (dữ liệu nhỏ).</summary>
public interface ITeamInsightsService
{
    TeamInsights Get();
    TaskInsight? GetTask(string project, string taskId);
}

/// <summary>Giờ merge task vào main. Tách riêng để test không cần git.</summary>
public interface IGitHistory
{
    /// <summary>Giờ commit merge mới nhất trên main có tiêu đề "Merge &lt;taskId&gt;" theo sau là "-", ":", " " hoặc hết dòng. Không có / lỗi git ⇒ null.</summary>
    DateTime? MergedAt(string repoPath, string taskId);
}

public sealed class GitHistory : IGitHistory
{
    private static readonly Regex SafeTaskIdRx = new(@"^[A-Za-z0-9][A-Za-z0-9-]*$", RegexOptions.Compiled);
    private readonly ConcurrentDictionary<(string Repo, string Id), (DateTime? Value, DateTime Expires)> _cache = new();

    /// <summary>
    /// Chạy: git -C &lt;repo&gt; log main --merges -E --grep="^Merge &lt;id&gt;([-: ]|$)" -1 --format=%cI
    /// (ArgumentList, không qua shell; taskId chỉ nhận [A-Za-z0-9-], sai ⇒ null; chờ tối đa 10 giây rồi kill).
    /// Kết quả đổi về giờ địa phương (DateTimeOffset.Parse(...).LocalDateTime). Cache theo (repo, id) trong 60 giây.
    /// </summary>
    public DateTime? MergedAt(string repoPath, string taskId)
    {
        if (string.IsNullOrWhiteSpace(repoPath) || string.IsNullOrWhiteSpace(taskId) || !SafeTaskIdRx.IsMatch(taskId) || !Directory.Exists(repoPath))
            return null;

        var key = (Path.GetFullPath(repoPath), taskId.ToUpperInvariant());
        if (_cache.TryGetValue(key, out var cached) && cached.Expires > DateTime.UtcNow)
            return cached.Value;

        DateTime? result = null;
        try
        {
            var psi = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var a in new[] { "-C", repoPath, "log", "main", "--merges", "-E", $"--grep=^Merge {taskId}([-: ]|$)", "-1", "--format=%cI" })
                psi.ArgumentList.Add(a);
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var stdout = proc.StandardOutput.ReadToEnd();
                if (!proc.WaitForExit(10_000)) { try { proc.Kill(entireProcessTree: true); } catch { } return null; }
                if (proc.ExitCode == 0 && DateTimeOffset.TryParse(stdout.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
                    result = dto.LocalDateTime;
            }
        }
        catch { return null; }

        _cache[key] = (result, DateTime.UtcNow.AddSeconds(60));
        return result;
    }
}

/// <summary>
/// Ghép dữ liệu:
/// 1. Dự án: config.GetProjects() có ExistsLocally. Task: board.GetBoard(p).Tasks (Id, Title, Agent, State, FileName).
///    Nội dung task: &lt;Project.Path&gt;/tasks/&lt;FileName&gt; (FileName rỗng / file không có ⇒ chuỗi rỗng, vẫn ra TaskInsight).
/// 2. Lượt: mọi metrics/runs/*.json trong AITeamRoot (InsightParsers.ParseRun, bỏ null), khớp Project + Task (không phân biệt hoa thường).
/// 3. Nhân viên: staff/*.md (ParseStaff). StaffName = tên theo Agent; Agent rỗng ⇒ "Claude"; không có hồ sơ ⇒ mã Agent.
/// 4. MergedAt: chỉ gọi git.MergedAt khi State == Merged.
/// 5. Staff: nhóm task có Request != null và Agent != "" theo Agent (StaffScore — xem chú thích model).
/// 6. StageAverages: đúng 7 phần tử Index 1..7 (tên lấy từ StageNames), TB Score khác null của MỌI task, 1 số lẻ.
/// 7. Issues: mọi StageScore có Cause khác rỗng của mọi task.
/// 8. Improvements: improvements/IMP-*.md (ParseImprovement, bỏ null), theo Id tăng dần. Thư mục không có ⇒ rỗng.
/// 9. Kpis: xem OverviewKpis. Thư mục / file lỗi đọc ⇒ bỏ qua, không ném lỗi.
/// </summary>
public sealed class TeamInsightsService : ITeamInsightsService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _board;
    private readonly IGitHistory _git;

    public TeamInsightsService(HubPaths paths, ITeamConfigService config, ITaskBoardService board, IGitHistory git)
    {
        _paths = paths;
        _config = config;
        _board = board;
        _git = git;
    }

    public TeamInsights Get()
    {
        var runs = SafeFiles(Path.Combine(_paths.AITeamRoot, "metrics", "runs"), "*.json")
            .Select(f => InsightParsers.ParseRun(SafeRead(f))).Where(r => r != null).Select(r => r!).ToList();
        var staffMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in SafeFiles(Path.Combine(_paths.AITeamRoot, "staff"), "*.md"))
            if (InsightParsers.ParseStaff(Path.GetFileName(f), SafeRead(f)) is { } s) staffMap[s.Agent] = s.Name;

        var entries = new List<(TaskInsight Insight, DateTime? SortTime)>();
        IReadOnlyList<ProjectInfo> projects;
        try { projects = _config.GetProjects(); } catch { projects = Array.Empty<ProjectInfo>(); }

        foreach (var proj in projects.Where(p => p.ExistsLocally))
        {
            IReadOnlyList<TaskCard> cards;
            try { cards = _board.GetBoard(proj.Name).Tasks; } catch { continue; }
            foreach (var card in cards)
            {
                var safeName = string.IsNullOrWhiteSpace(card.FileName) ? "" : Path.GetFileName(card.FileName);
                var md = string.IsNullOrWhiteSpace(safeName) ? "" : SafeRead(Path.Combine(proj.Path, "tasks", safeName));
                var taskRuns = runs.Where(r => string.Equals(r.Project, proj.Name, StringComparison.OrdinalIgnoreCase)
                                            && string.Equals(r.Task, card.Id, StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(r => r.At).ThenBy(r => r.Run).ToList();
                var agent = (card.Agent ?? "").Trim();
                var staffName = string.IsNullOrEmpty(agent) ? "Claude" : (staffMap.TryGetValue(agent, out var sn) ? sn : agent);
                var agentMinutes = (int)Math.Round(taskRuns.Sum(r => r.DurationSec) / 60.0, MidpointRounding.AwayFromZero);
                var (rounds, passed1) = InsightParsers.ParseReviewRounds(md);
                var stages = InsightParsers.ParseProcessReview(md);
                var scored = stages.Where(s => s.Score.HasValue).Select(s => s.Score!.Value).ToList();
                double? procScore = scored.Count > 0 ? Math.Round(scored.Average(), 1, MidpointRounding.AwayFromZero) : null;
                DateTime? mergedAt = card.State == TaskState.Merged ? _git.MergedAt(proj.Path, card.Id) : null;

                var insight = new TaskInsight(proj.Name, card.Id, card.Title, agent, staffName,
                    InsightParsers.ParseDifficulty(md), InsightParsers.ParseRequest(md), card.State,
                    agentMinutes, taskRuns.Count, rounds, passed1, taskRuns.Count > 0 ? taskRuns[0].VerifyOk : null,
                    procScore, mergedAt, stages, InsightParsers.ParseReviewIssues(md, proj.Name, card.Id),
                    InsightParsers.BuildTimeline(taskRuns, mergedAt));
                entries.Add((insight, mergedAt ?? (taskRuns.Count > 0 ? taskRuns[^1].At : null)));
            }
        }

        var ordered = entries.OrderByDescending(x => x.SortTime.HasValue).ThenByDescending(x => x.SortTime).Select(x => x.Insight).ToList();
        var staffScores = BuildStaffScores(ordered);
        var stageAvgs = Enumerable.Range(1, 7).Select(idx =>
        {
            var sc = ordered.SelectMany(t => t.Stages).Where(s => s.Index == idx && s.Score.HasValue).Select(s => s.Score!.Value).ToList();
            return new StageAverage(idx, InsightParsers.StageNames[idx - 1], sc.Count > 0 ? Math.Round(sc.Average(), 1, MidpointRounding.AwayFromZero) : null, sc.Count);
        }).ToList();
        var issues = ordered.SelectMany(t => t.Stages.Where(s => !string.IsNullOrWhiteSpace(s.Cause))
            .Select(s => new TeamIssue(t.Project, t.Id, s.Index, s.Stage, s.Cause, s.Issue, s.Proposal))).ToList();
        var imps = SafeFiles(Path.Combine(_paths.AITeamRoot, "improvements"), "IMP-*.md")
            .Select(f => InsightParsers.ParseImprovement(Path.GetFileName(f), SafeRead(f)))
            .Where(i => i != null).Select(i => i!).OrderBy(i => i.Id, StringComparer.OrdinalIgnoreCase).ToList();

        var recent = ordered.Where(t => t.Request != null).Take(6).ToList();
        var rev = recent.Where(t => t.ReviewRounds > 0).ToList();
        var wr = recent.Where(t => t.Runs > 0).ToList();
        var kpis = new OverviewKpis(
            rev.Count > 0 ? rev.Count(t => t.PassedRound1) / (double)rev.Count : null,
            wr.Count > 0 ? Math.Round(wr.Average(t => t.AgentMinutes), 1, MidpointRounding.AwayFromZero) : null,
            recent.Count);

        return new TeamInsights(ordered, staffScores, stageAvgs, issues, imps, kpis);
    }

    public TaskInsight? GetTask(string project, string taskId)
        => Get().Tasks.FirstOrDefault(t => string.Equals(t.Project, project, StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(t.Id, taskId, StringComparison.OrdinalIgnoreCase));

    private static List<StaffScore> BuildStaffScores(IReadOnlyList<TaskInsight> tasks)
        => tasks.Where(t => t.Request != null && !string.IsNullOrEmpty(t.Agent))
            .GroupBy(t => t.Agent, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var list = g.ToList();
                var totalRuns = list.Sum(t => t.Runs);
                var avgMin = totalRuns > 0 ? Math.Round(list.Sum(t => t.AgentMinutes) / (double)totalRuns, 1, MidpointRounding.AwayFromZero) : 0.0;
                var withRuns = list.Where(t => t.FirstRunVerifyOk.HasValue).ToList();
                var reviewed = list.Where(t => t.ReviewRounds > 0).ToList();
                var agErrors = list.SelectMany(t => t.Issues)
                    .Where(i => string.Equals(i.Cause, "AG", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(i => i.Kind).ToDictionary(k => k.Key, k => k.Count());
                return new StaffScore(list[0].Agent, list[0].StaffName, list.Count, totalRuns, avgMin,
                    withRuns.Count > 0 ? withRuns.Count(t => t.FirstRunVerifyOk == true) / (double)withRuns.Count : null,
                    reviewed.Count > 0 ? reviewed.Count(t => t.PassedRound1) / (double)reviewed.Count : null,
                    agErrors);
            }).ToList();

    private static IEnumerable<string> SafeFiles(string dir, string pattern)
    {
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        try { return Directory.EnumerateFiles(dir, pattern).ToList(); } catch { return Array.Empty<string>(); }
    }

    private static string SafeRead(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : ""; } catch { return ""; }
    }
}
