using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AITeam.Core.Metrics;
using AITeam.Core.Tasks;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Bộ lọc chung của màn Báo cáo. Person: mã Agent (A/B) hoặc provider (codex/opencode/agy).</summary>
public sealed record ReportFilter(DateOnly From, DateOnly To, string? Project = null, string? Person = null, string? Difficulty = null);

/// <summary>Một ô số của tab Tổng quan; Value null = chưa đo (không thay bằng 0).</summary>
public sealed record ReportKpi(string Key, string Label, double? Value, double? Previous, string Unit);
public sealed record WeekPoint(string Week, DateOnly Start, int Done, double? FirstPassPct, int ErrorsTk, int ErrorsAg, int ErrorsCk);
public sealed record NamedValue(string Name, double Value);
public sealed record ScoreRow(string Person, string Display, int Score, int Tasks, double? FirstPassPct, double? InBudgetPct, double? OnTimePct, int AgErrors, double? AvgMinutes);
public sealed record ErrorCell(string Cause, string Category, int Count, IReadOnlyList<string> Tasks);
public sealed record ReportData(
    ReportFilter Filter,
    IReadOnlyList<ReportKpi> Kpis,
    IReadOnlyList<WeekPoint> Weeks,
    IReadOnlyList<NamedValue> MinutesByProject,
    IReadOnlyList<NamedValue> DifficultyCounts,
    IReadOnlyList<NamedValue> TasksByPerson,
    IReadOnlyList<ScoreRow> Scores,
    IReadOnlyList<ErrorCell> ErrorMatrix,
    IReadOnlyList<string> Projects, IReadOnlyList<string> People,
    IReadOnlyList<string> Warnings);

public sealed record TimelineStep(int Order, string Name, string? Actor, string? Model, DateTimeOffset? Start, DateTimeOffset? End,
    double? Minutes, long? Tokens, int Runs, int Rounds, IReadOnlyList<string> Findings, double? WaitMinutesBefore, IReadOnlyList<string> Tasks);
public sealed record RequestTimeline(string RequestId, string Title, IReadOnlyList<TimelineStep> Steps, double? TotalHours, IReadOnlyList<string> Warnings);

public interface IReportService
{
    ReportData Query(ReportFilter filter, DateTimeOffset? now = null);
    RequestTimeline? Timeline(string requestId);
    string ExportCsv(ReportFilter filter, string table);
}

/// <summary>
/// Số liệu màn Báo cáo &amp; thống kê (R-014 mục 4.4). Chỉ đọc: bảng việc, file task, metrics/runs, phiếu yêu cầu.
/// Điểm hiệu suất = round(40·đạt vòng 1 + 25·trong ngân sách + 20·đúng giờ + 15·(1 − tỉ lệ task có lỗi AG));
/// thành phần chưa đo thì chia lại trọng số cho các thành phần có số.
/// </summary>
public sealed class ReportService : IReportService
{
    private static readonly string[] CauseOrder = ["TK", "AG", "CK", "HT", "khác"];
    private static readonly string[] DifficultyOrder = ["S", "M", "L", "chưa rõ"];
    private static readonly Regex DifficultyRx = new(@"(?im)^\s*[-*]?\s*\*\*Độ khó:\*\*\s*([SML])\b", RegexOptions.Compiled);
    private static readonly Regex DifficultyRx2 = new(@"(?im)Độ khó\s*[:：]\s*([SML])\b", RegexOptions.Compiled);
    private static readonly Regex EventTimeRx = new(@"(\d{1,2}):(\d{2})", RegexOptions.Compiled);
    private static readonly Regex ReceivedDateRx = new(@"(\d{1,2})/(\d{1,2})/(\d{4})", RegexOptions.Compiled);
    private static readonly Regex TaskCodeRx = new(@"\b[A-Za-z][A-Za-z0-9]*-\d+\b", RegexOptions.Compiled);

    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _boards;
    private readonly IGitHistory? _git;

    public ReportService(HubPaths paths, ITeamConfigService config, ITaskBoardService boards)
        : this(paths, config, boards, null) { }

    public ReportService(HubPaths paths, ITeamConfigService config, ITaskBoardService boards, IGitHistory? git)
    {
        _paths = paths;
        _config = config;
        _boards = boards;
        _git = git;
    }

    public ReportData Query(ReportFilter filter, DateTimeOffset? now = null)
    {
        var built = Build(filter);
        return built.Data;
    }

    public string ExportCsv(ReportFilter filter, string table)
    {
        if (table is not ("scores" or "weeks" or "errors" or "tasks"))
            throw new ArgumentException("Bảng cần xuất không hợp lệ: " + table, nameof(table));

        var built = Build(filter);
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        switch (table)
        {
            case "scores":
                sb.AppendLine("Person,Display,Score,Tasks,FirstPassPct,InBudgetPct,OnTimePct,AgErrors,AvgMinutes");
                foreach (var row in built.Data.Scores)
                    sb.AppendLine(string.Join(',', new object?[] { row.Person, row.Display, row.Score, row.Tasks, row.FirstPassPct, row.InBudgetPct, row.OnTimePct, row.AgErrors, row.AvgMinutes }.Select(CsvCell)));
                break;
            case "weeks":
                sb.AppendLine("Week,Start,Done,FirstPassPct,ErrorsTk,ErrorsAg,ErrorsCk");
                foreach (var row in built.Data.Weeks)
                    sb.AppendLine(string.Join(',', new object?[] { row.Week, row.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), row.Done, row.FirstPassPct, row.ErrorsTk, row.ErrorsAg, row.ErrorsCk }.Select(CsvCell)));
                break;
            case "errors":
                sb.AppendLine("Cause,Category,Count,Tasks");
                foreach (var row in built.Data.ErrorMatrix)
                    sb.AppendLine(string.Join(',', new object?[] { row.Cause, row.Category, row.Count, string.Join(';', row.Tasks) }.Select(CsvCell)));
                break;
            default:
                sb.AppendLine("Project,Code,Title,Person,Difficulty,Status,DoneAt,Minutes,Tokens,Request");
                foreach (var task in built.Tasks)
                    sb.AppendLine(string.Join(',', new object?[]
                    {
                        task.Project, task.Code, task.Title, task.Display, task.Difficulty,
                        task.State == TaskState.Merged ? "Đã merge" : task.State.ToString(),
                        task.DoneAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        task.Minutes, task.Tokens, task.Request
                    }.Select(CsvCell)));
                break;
        }
        return sb.ToString();
    }

    public RequestTimeline? Timeline(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || !Regex.IsMatch(requestId, @"\AR-\d{3,4}\z"))
            throw new ArgumentException("Mã yêu cầu không hợp lệ: " + requestId, nameof(requestId));

        var folder = Path.Combine(_paths.AITeamRoot, "requests");
        if (!Directory.Exists(folder)) return null;
        try
        {
            for (string? p = folder; p != null; p = Directory.GetParent(p)?.FullName)
                if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Không đọc phiếu qua symlink/reparse point.");
            var candidates = Directory.EnumerateFiles(folder, requestId + "*.md").OrderBy(f => f, StringComparer.Ordinal).Take(2).ToArray();
            if (candidates.Length == 0) return null;

            var fileName = Path.GetFileName(candidates[0]);
            var text = HubMetricReader.ReadText(_paths.AITeamRoot, "requests/" + fileName);
            if (text == null) return null;
            var request = RequestParser.Parse(fileName, text);
            if (request == null) return null;

            var warnings = new List<string>();
            var snapshot = HubMetricReader.Read(_paths);
            warnings.AddRange(snapshot.Warnings);
            var tasks = ReadTasks(snapshot.Runs, warnings);
            var linked = tasks.Where(t => string.Equals(t.Request, request.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (request.Tasks.Any(t => t.Code.Length > 0 && !linked.Any(v => v.Code.Equals(HubMetricReader.TaskCode(t.Code), StringComparison.OrdinalIgnoreCase))))
                warnings.Add("Một số task trong phiếu chưa có dữ liệu trên máy này.");

            var baseDate = ReceivedDate(request.ReceivedAt) ?? DateOnly.FromDateTime(File.GetLastWriteTime(candidates[0]));
            var steps = new List<TimelineStep>();
            var previousAt = (DateTimeOffset?)null;
            var raw = new List<(DateTimeOffset At, RequestEvent Event)>();
            var dayShift = 0;
            var last = AtStart(baseDate);
            foreach (var e in request.Events)
            {
                var at = last;
                var m = EventTimeRx.Match(e.Time ?? "");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var h) && int.TryParse(m.Groups[2].Value, out var min) && h < 24 && min < 60)
                {
                    at = AtStart(baseDate).AddDays(dayShift).AddHours(h).AddMinutes(min);
                    if (previousAt.HasValue && at < previousAt.Value) { dayShift++; at = at.AddDays(1); }
                }
                previousAt = at;
                last = at;
                raw.Add((at, e));
            }

            var ordered = raw.OrderBy(x => x.At).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var (at, e) = ordered[i];
                var codes = TaskCodeRx.Matches((e.Text ?? "") + " " + (e.Note ?? ""))
                    .Select(m => HubMetricReader.TaskCode(m.Value))
                    .Where(c => linked.Any(t => t.Code.Equals(c, StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var stepTasks = linked.Where(t => codes.Contains(t.Code, StringComparer.OrdinalIgnoreCase)).ToList();
                var stepRuns = stepTasks.SelectMany(t => t.Runs).ToList();
                var latest = stepRuns.Count == 0 ? null : stepRuns.OrderByDescending(r => r.At).ThenByDescending(r => r.Run).First();
                var minutes = stepRuns.Any(r => r.Seconds.HasValue) ? stepRuns.Where(r => r.Seconds.HasValue).Sum(r => r.Seconds!.Value) / 60 : (double?)null;
                var tokens = stepRuns.Any(r => r.Tokens.HasValue) ? stepRuns.Where(r => r.Tokens.HasValue).Sum(r => r.Tokens!.Value) : (long?)null;
                double? wait = null;
                if (i > 0) wait = (at - ordered[i - 1].At).TotalMinutes;
                if (wait is <= 30) wait = null;
                var start = stepRuns.Count == 0 ? at : stepRuns.Min(r => r.At);
                var end = stepRuns.Count == 0 ? null : (DateTimeOffset?)stepRuns.Max(r => r.At.AddSeconds(r.Seconds ?? 0));
                steps.Add(new TimelineStep(
                    i + 1, string.IsNullOrWhiteSpace(e.Text) ? "Bước " + (i + 1) : e.Text.Trim(), Actor(e.Text, latest), latest?.Model ?? latest?.Provider,
                    start, end, minutes, tokens, stepRuns.Count, stepTasks.Sum(t => t.Rounds.Count),
                    stepTasks.SelectMany(t => t.Findings).Select(f => "[" + f.Cause + "] " + f.Text).ToList(),
                    wait, codes));
            }

            double? totalHours = steps.Count == 0 ? null : (steps[^1].End ?? steps[^1].Start)?.Subtract(steps[0].Start!.Value).TotalHours;
            return new RequestTimeline(request.Id, request.Title, steps, totalHours, warnings.Distinct().ToList());
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            if (ex is ArgumentException) throw;
            return null;
        }
    }

    private (ReportData Data, IReadOnlyList<TaskRow> Tasks) Build(ReportFilter filter)
    {
        var from = filter.From <= filter.To ? filter.From : filter.To;
        var to = filter.From <= filter.To ? filter.To : filter.From;
        var warnings = new List<string>();
        var snapshot = HubMetricReader.Read(_paths);
        warnings.AddRange(snapshot.Warnings);
        var all = ReadTasks(snapshot.Runs, warnings);
        var view = all.Where(t => Matches(t, filter) && InRange(t, from, to)).ToList();

        var length = to.DayNumber - from.DayNumber + 1;
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(length - 1));
        var previous = all.Where(t => Matches(t, filter) && InRange(t, previousFrom, previousTo)).ToList();

        var kpis = BuildKpis(view, from, to, previous);
        var weeks = BuildWeeks(view, from, to);
        var minutesByProject = view.GroupBy(t => t.Project)
            .Select(g => new NamedValue(g.Key, SumMinutes(g.ToList(), from, to) ?? 0))
            .Where(v => v.Value > 0)
            .OrderByDescending(v => v.Value).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var difficultyCounts = DifficultyOrder
            .Select(d => new NamedValue(d, view.Count(t => string.Equals(t.Difficulty, d, StringComparison.OrdinalIgnoreCase))))
            .ToList();
        var tasksByPerson = view.GroupBy(t => t.PersonKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NamedValue(g.First().Display, g.Count()))
            .OrderByDescending(v => v.Value).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var scores = BuildScores(view);
        var errors = BuildErrorMatrix(view);
        var projects = _config.GetProjects().Select(p => p.Name).Concat(snapshot.Runs.Select(r => r.Project))
            .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        var people = all.Select(t => t.Display).Concat(snapshot.Runs.Select(r => PerformanceService.ProviderLabel(r.Provider)))
            .Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

        var data = new ReportData(new ReportFilter(from, to, filter.Project, filter.Person, filter.Difficulty),
            kpis, weeks, minutesByProject, difficultyCounts, tasksByPerson, scores, errors, projects, people,
            warnings.Distinct().ToList());
        return (data, view.OrderBy(t => t.Project, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Code, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static double? SumMinutes(List<TaskRow> tasks, DateOnly from, DateOnly to)
    {
        var start = AtStart(from);
        var end = AtStart(to.AddDays(1));
        var measured = tasks.SelectMany(t => t.Runs).Where(r => r.At >= start && r.At < end && r.Seconds.HasValue).ToList();
        return measured.Count == 0 ? null : measured.Sum(r => r.Seconds!.Value) / 60;
    }

    private static IReadOnlyList<ReportKpi> BuildKpis(List<TaskRow> view, DateOnly from, DateOnly to, List<TaskRow> previous)
    {
        var done = view.Count(t => t.State == TaskState.Merged && InDoneRange(t, from, to));
        var length = to.DayNumber - from.DayNumber + 1;
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(length - 1));
        var prevDone = previous.Count(t => t.State == TaskState.Merged && InDoneRange(t, previousFrom, previousTo));
        var pass = FirstPass(view);
        var prevPass = FirstPass(previous);
        var minutes = SumMinutes(view, from, to);
        var prevMinutes = SumMinutes(previous, previousFrom, previousTo);
        var tk = view.Sum(t => t.Findings.Count(f => f.Cause == "TK"));
        var prevTk = previous.Sum(t => t.Findings.Count(f => f.Cause == "TK"));
        return
        [
            new("done", "Task xong", done, prevDone, "task"),
            new("first_pass_pct", "Đạt vòng 1", pass, prevPass, "%"),
            new("ai_minutes", "Phút AI", minutes, prevMinutes, "phút"),
            new("errors_tk", "Lỗi TK", view.Count == 0 ? null : tk, previous.Count == 0 ? null : prevTk, "lỗi")
        ];
    }

    private static bool InDoneRange(TaskRow task, DateOnly from, DateOnly to)
    {
        if (!task.DoneAt.HasValue) return false;
        var start = AtStart(from);
        var end = AtStart(to.AddDays(1));
        return task.DoneAt.Value >= start && task.DoneAt.Value < end;
    }

    private static double? FirstPass(List<TaskRow> tasks)
    {
        var reviewed = tasks.Where(t => t.Rounds.Count > 0).ToList();
        return reviewed.Count == 0 ? null : reviewed.Count(t => t.Rounds[0].Verdict == ReviewVerdict.Pass) * 100.0 / reviewed.Count;
    }

    private static IReadOnlyList<WeekPoint> BuildWeeks(List<TaskRow> view, DateOnly from, DateOnly to)
    {
        var weeks = new List<WeekPoint>();
        for (var start = StartOfWeek(from); start <= to; start = start.AddDays(7))
        {
            var end = start.AddDays(7);
            var bucket = view.Where(t => t.DoneAt.HasValue && t.DoneAt.Value >= AtStart(start) && t.DoneAt.Value < AtStart(end)).ToList();
            var reviewed = bucket.Where(t => t.Rounds.Count > 0).ToList();
            weeks.Add(new WeekPoint(
                "T" + ISOWeek.GetWeekOfYear(start.ToDateTime(TimeOnly.MinValue)),
                start,
                bucket.Count(t => t.State == TaskState.Merged),
                reviewed.Count == 0 ? null : reviewed.Count(t => t.Rounds[0].Verdict == ReviewVerdict.Pass) * 100.0 / reviewed.Count,
                bucket.Sum(t => t.Findings.Count(f => f.Cause == "TK")),
                bucket.Sum(t => t.Findings.Count(f => f.Cause == "AG")),
                bucket.Sum(t => t.Findings.Count(f => f.Cause == "CK"))));
        }
        return weeks;
    }

    private static IReadOnlyList<ScoreRow> BuildScores(List<TaskRow> view)
    {
        var rows = new List<ScoreRow>();
        foreach (var group in view.GroupBy(t => t.PersonKey, StringComparer.OrdinalIgnoreCase))
        {
            var tasks = group.ToList();
            var reviewed = tasks.Where(t => t.Rounds.Count > 0).ToList();
            double? firstPass = reviewed.Count == 0 ? null : reviewed.Count(t => t.Rounds[0].Verdict == ReviewVerdict.Pass) * 100.0 / reviewed.Count;
            var budgeted = tasks.Where(t => t.BudgetLines.HasValue && t.LinesAdded.HasValue).ToList();
            double? inBudget = budgeted.Count == 0 ? null : budgeted.Count(t => t.LinesAdded <= t.BudgetLines) * 100.0 / budgeted.Count;
            var timed = tasks.Where(t => t.Minutes.HasValue).ToList();
            double? onTime = timed.Count == 0 ? null : timed.Count(t => t.Minutes <= t.MaxMinutes) * 100.0 / timed.Count;
            var agErrors = tasks.Sum(t => t.Findings.Count(f => f.Cause == "AG"));
            var agTaskRatio = tasks.Count == 0 ? 0 : (double)tasks.Count(t => t.Findings.Any(f => f.Cause == "AG")) / tasks.Count;
            var parts = new List<(double Weight, double Value)>();
            if (firstPass.HasValue) parts.Add((40, firstPass.Value / 100));
            if (inBudget.HasValue) parts.Add((25, inBudget.Value / 100));
            if (onTime.HasValue) parts.Add((20, onTime.Value / 100));
            if (tasks.Count > 0) parts.Add((15, 1 - agTaskRatio));
            var totalWeight = parts.Sum(p => p.Weight);
            var score = totalWeight == 0 ? 0 : (int)Math.Round(parts.Sum(p => p.Weight * p.Value) / totalWeight * 100, MidpointRounding.AwayFromZero);
            var minutes = tasks.Any(t => t.Minutes.HasValue) ? tasks.Where(t => t.Minutes.HasValue).Average(t => t.Minutes!.Value) : (double?)null;
            rows.Add(new ScoreRow(group.First().PersonKey, group.First().Display, Math.Clamp(score, 0, 100), tasks.Count,
                firstPass, inBudget, onTime, agErrors, minutes));
        }
        return rows.OrderByDescending(r => r.Score).ThenByDescending(r => r.Tasks).ThenBy(r => r.Person, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<ErrorCell> BuildErrorMatrix(List<TaskRow> view)
    {
        var cells = new List<ErrorCell>();
        foreach (var group in view
            .SelectMany(t => t.Findings.Select(f => (Task: t.Code, Cause: NormalizeCause(f.Cause), Category: string.IsNullOrWhiteSpace(f.Kind) ? "khác" : f.Kind)))
            .GroupBy(x => (x.Cause, x.Category)))
        {
            cells.Add(new ErrorCell(group.Key.Cause, group.Key.Category, group.Count(),
                group.Select(x => x.Task).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()));
        }
        return cells.OrderBy(c => Array.IndexOf(CauseOrder, c.Cause)).ThenByDescending(c => c.Count)
            .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string NormalizeCause(string cause)
        => cause is "TK" or "AG" or "CK" or "HT" ? cause : "khác";

    private List<TaskRow> ReadTasks(IReadOnlyList<HubMetric> runs, List<string> warnings)
    {
        var rows = new List<TaskRow>();
        var byKey = new Dictionary<(string Project, string Code), TaskRow>();
        foreach (var project in _config.GetProjects())
        {
            ProjectBoard board;
            try { board = _boards.GetBoard(project.Name); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            { warnings.Add("Không đọc được bảng việc của " + project.Name + "."); continue; }

            foreach (var card in board.Tasks)
            {
                var code = HubMetricReader.TaskCode(card.Id);
                if (code.Length == 0) code = card.Id;
                var row = new TaskRow(project.Name, project.Path, code, card.Title ?? "", card.Agent ?? "", card.State);
                try
                {
                    if (!string.IsNullOrWhiteSpace(card.FileName) && !card.FileName.Contains("..", StringComparison.Ordinal))
                    {
                        var text = HubMetricReader.ReadText(project.Path, "tasks/" + card.FileName);
                        if (text != null) ParseTaskFile(row, text);
                    }
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or DecoderFallbackException)
                { warnings.Add("Không đọc được file task " + card.Id + " của " + project.Name + "."); }
                rows.Add(row);
                byKey[(project.Name.ToLowerInvariant(), code)] = row;
            }
        }

        foreach (var run in runs)
        {
            var key = (run.Project.ToLowerInvariant(), run.Task);
            if (!byKey.TryGetValue(key, out var row))
            {
                row = new TaskRow(run.Project, "", run.Task, run.Task, "", TaskState.Unknown);
                byKey[key] = row;
                rows.Add(row);
            }
            row.Runs.Add(run);
        }

        foreach (var row in rows)
        {
            row.DoneAt = row.Runs.Count == 0 ? null : row.Runs.Max(r => r.At);
            if (row.State == TaskState.Merged && row.DoneAt == null && _git != null && !string.IsNullOrWhiteSpace(row.ProjectPath))
            {
                var merged = _git.MergedAt(row.ProjectPath, row.Code);
                if (merged.HasValue) row.DoneAt = new DateTimeOffset(DateTime.SpecifyKind(merged.Value, DateTimeKind.Local));
            }
        }
        return rows;
    }

    private static void ParseTaskFile(TaskRow row, string text)
    {
        var file = TaskFile.Parse("", text);
        row.Request = file.Request;
        row.BudgetLines = file.BudgetLines;
        row.MaxMinutes = file.MaxMinutes ?? 30;
        var difficulty = DifficultyRx.Match(text);
        if (!difficulty.Success) difficulty = DifficultyRx2.Match(text);
        if (difficulty.Success) row.Difficulty = difficulty.Groups[1].Value.ToUpperInvariant();
        var review = file.Sections.FirstOrDefault(kv => kv.Key.StartsWith("Review", StringComparison.OrdinalIgnoreCase)
            || kv.Key.StartsWith("Đánh giá", StringComparison.OrdinalIgnoreCase)).Value;
        row.Rounds = ReviewParser.Parse(review);
        row.Findings = FindingParser.Parse(review);
    }

    private static bool Matches(TaskRow task, ReportFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Project) && !task.Project.Equals(filter.Project.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            var person = filter.Person.Trim();
            if (!task.PersonKey.Equals(person, StringComparison.OrdinalIgnoreCase)
                && !task.Agent.Equals(person, StringComparison.OrdinalIgnoreCase)) return false;
        }
        if (!string.IsNullOrWhiteSpace(filter.Difficulty) && !task.Difficulty.Equals(filter.Difficulty.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private static bool InRange(TaskRow task, DateOnly from, DateOnly to)
    {
        var start = AtStart(from);
        var end = AtStart(to.AddDays(1));
        if (task.Runs.Any(r => r.At >= start && r.At < end)) return true;
        return task.DoneAt.HasValue && task.DoneAt.Value >= start && task.DoneAt.Value < end;
    }

    private static DateTimeOffset AtStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue));

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static DateOnly? ReceivedDate(string received)
    {
        var match = ReceivedDateRx.Match(received ?? "");
        if (!match.Success) return null;
        if (!int.TryParse(match.Groups[1].Value, out var day) || !int.TryParse(match.Groups[2].Value, out var month)
            || !int.TryParse(match.Groups[3].Value, out var year)) return null;
        if (day < 1 || day > 31 || month < 1 || month > 12) return null;
        try { return new DateOnly(year, month, day); } catch (ArgumentOutOfRangeException) { return null; }
    }

    private static string? Actor(string? text, HubMetric? run)
    {
        if (text != null && text.Contains("Claude", StringComparison.OrdinalIgnoreCase)) return "Claude";
        if (run != null) return PerformanceService.ProviderLabel(run.Provider);
        return null;
    }

    private static string CsvCell(object? value)
    {
        var text = value switch
        {
            null => "",
            double d => d.ToString("0.#", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@') text = "'" + text;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }

    private sealed class TaskRow(string project, string projectPath, string code, string title, string agent, TaskState state)
    {
        public string Project { get; } = project;
        public string ProjectPath { get; } = projectPath;
        public string Code { get; } = code;
        public string Title { get; } = title;
        public string Agent { get; } = agent;
        public TaskState State { get; } = state;
        public string? Request { get; set; }
        public string Difficulty { get; set; } = "chưa rõ";
        public int? BudgetLines { get; set; }
        public int MaxMinutes { get; set; } = 30;
        public List<HubMetric> Runs { get; } = [];
        public IReadOnlyList<ReviewRound> Rounds { get; set; } = [];
        public IReadOnlyList<StructuredFinding> Findings { get; set; } = [];
        public DateTimeOffset? DoneAt { get; set; }

        public HubMetric? LatestRun => Runs.Count == 0 ? null : Runs.OrderByDescending(r => r.At).ThenByDescending(r => r.Run).First();
        public string? Provider => LatestRun?.Provider;
        public string PersonKey => !string.IsNullOrWhiteSpace(Provider) ? Provider!.ToLowerInvariant() : (!string.IsNullOrWhiteSpace(Agent) ? Agent : "không rõ");
        public string Display => string.IsNullOrWhiteSpace(Provider) ? PersonKey : PerformanceService.ProviderLabel(Provider!);
        public double? Minutes => Runs.Any(r => r.Seconds.HasValue) ? Runs.Where(r => r.Seconds.HasValue).Sum(r => r.Seconds!.Value) / 60 : null;
        public long? Tokens => Runs.Any(r => r.Tokens.HasValue) ? Runs.Where(r => r.Tokens.HasValue).Sum(r => r.Tokens!.Value) : null;
        public int? LinesAdded => Runs.Any(r => r.Added.HasValue) ? Runs.Where(r => r.Added.HasValue).Sum(r => r.Added!.Value) : null;

        public double? MinutesIn(DateTimeOffset start, DateTimeOffset end)
        {
            var measured = Runs.Where(r => r.At >= start && r.At < end && r.Seconds.HasValue).ToList();
            return measured.Count == 0 ? null : measured.Sum(r => r.Seconds!.Value) / 60;
        }
    }
}
