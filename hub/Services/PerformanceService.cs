using System.Globalization;
using System.Text.RegularExpressions;
using AITeam.Core.Metrics;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public enum PerformanceMetricKind { Count, Percent, Minutes, Tokens }

/// <summary>Một chỉ số mục 2 của mô tả hệ thống, so sánh tuần này với tuần trước.</summary>
public sealed record PerformanceMetric(
    string Key, string Label, string Target, PerformanceMetricKind Kind,
    double? Current, double? Previous, int CurrentSample, int PreviousSample, bool Unavailable = false)
{
    public string Display(bool current)
    {
        if (Unavailable) return "chưa đo";
        var sample = current ? CurrentSample : PreviousSample;
        var value = current ? Current : Previous;
        if (sample < 3) return "Chưa đủ dữ liệu";
        if (value is null) return "chưa đo";
        return Kind switch
        {
            PerformanceMetricKind.Percent => value.Value.ToString("0.#", CultureInfo.InvariantCulture) + " %",
            PerformanceMetricKind.Minutes => value.Value.ToString("0.#", CultureInfo.InvariantCulture) + " phút",
            PerformanceMetricKind.Tokens => FormatTokens(value.Value),
            _ => value.Value.ToString("0", CultureInfo.InvariantCulture)
        };
    }

    public static string FormatTokens(double value) => value >= 1_000_000
        ? (value / 1_000_000).ToString("0.#", CultureInfo.InvariantCulture) + " triệu"
        : value >= 1000
            ? (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + " nghìn"
            : value.ToString("0", CultureInfo.InvariantCulture);
}

public sealed record PerformanceCauseCount(string Cause, int Current, int Previous);

public sealed record PerformanceProviderScore(
    string Provider, string? Model, int TaskCount, double? Round1Rate, double? AverageRounds,
    double? MinutesS, double? MinutesM, double? MinutesL, double? AverageTokens,
    IReadOnlyDictionary<string, int> AgByGroup, int QuotaExhausted, IReadOnlyList<string> WorkTypes,
    bool EnoughData, IReadOnlyList<string> TaskCodes)
{
    public string DisplayName => PerformanceService.ProviderLabel(Provider) + (Model == null ? "" : " / " + Model);
    public string Suggested => EnoughData && WorkTypes.Count > 0 ? WorkTypes[0] : "";
}

public sealed record PerformanceTaskRow(
    string Project, string Code, string Title, string? Provider, string? Model, string Difficulty,
    int Rounds, bool? PassedFirstRound, double? Minutes, long? Tokens,
    IReadOnlyList<string> Causes, IReadOnlyList<string> WorkTypes, OfficeTaskStatus Status)
{
    public string ProviderDisplay => Provider == null
        ? "—"
        : PerformanceService.ProviderLabel(Provider) + (Model == null ? "" : " / " + Model);
}

public sealed record PerformanceSnapshot(
    DateTimeOffset WeekStart, DateTimeOffset PreviousWeekStart,
    IReadOnlyList<PerformanceMetric> Metrics, IReadOnlyList<PerformanceCauseCount> Causes,
    IReadOnlyList<PerformanceProviderScore> Scores, IReadOnlyList<PerformanceTaskRow> Tasks,
    IReadOnlyList<string> Warnings);

/// <summary>Hiệu suất + bảng điểm AI (F7, F8). Chỉ đọc metrics/runs, bảng việc và mục Review; thiếu số đo thì ghi "chưa đo".</summary>
public sealed class PerformanceService(HubPaths paths, ITeamConfigService config, ITaskBoardService boards)
{
    private static readonly string[] CauseOrder = ["TK", "TN", "AG", "CK", "HT", "HM", "CH", "TO", "DP", "khác"];
    private static readonly string[] WorkTypeOrder = ["giao diện", "PowerShell", "C# lõi", "test"];

    public PerformanceSnapshot Read(DateTimeOffset? nowValue = null)
    {
        var now = nowValue ?? DateTimeOffset.Now;
        var weekStart = StartOfWeek(now);
        var previousStart = weekStart.AddDays(-7);
        var warnings = new List<string>();
        var metrics = HubMetricReader.Read(paths);
        warnings.AddRange(metrics.Warnings);
        var runs = metrics.Runs;
        var tasks = ReadTasks(warnings, runs);
        var currentTasks = tasks.Where(t => t.Runs.Any(r => InWindow(r.At, weekStart, weekStart.AddDays(7)))).ToList();
        var previousTasks = tasks.Where(t => t.Runs.Any(r => InWindow(r.At, previousStart, weekStart))).ToList();
        var currentRuns = runs.Where(r => InWindow(r.At, weekStart, weekStart.AddDays(7))).ToList();
        var previousRuns = runs.Where(r => InWindow(r.At, previousStart, weekStart)).ToList();
        return new(
            weekStart, previousStart,
            BuildMetrics(currentTasks, previousTasks, currentRuns, previousRuns, now, weekStart),
            BuildCauses(currentTasks, previousTasks),
            BuildScores(tasks, runs),
            tasks.OrderBy(t => t.Project).ThenBy(t => t.Code).Select(ToRow).ToList(),
            warnings.Distinct().ToList());
    }

    public static string ProviderLabel(string provider) => provider switch
    {
        "codex" => "Codex",
        "opencode" => "OpenCode",
        "agy" => "Antigravity",
        _ => provider
    };

    private static DateTimeOffset StartOfWeek(DateTimeOffset value)
    {
        var offset = ((int)value.DayOfWeek + 6) % 7;
        return new DateTimeOffset(value.Date.AddDays(-offset), value.Offset);
    }

    private static bool InWindow(DateTimeOffset at, DateTimeOffset start, DateTimeOffset end) => at >= start && at < end;

    private List<TaskMeta> ReadTasks(List<string> warnings, IReadOnlyList<HubMetric> runs)
    {
        var result = new List<TaskMeta>();
        foreach (var project in config.GetProjects())
        {
            try
            {
                var board = boards.GetBoard(project.Name);
                var statuses = ReadStatuses(project, warnings);
                foreach (var card in board.Tasks)
                {
                    var code = HubMetricReader.TaskCode(card.Id);
                    if (code.Length == 0) code = card.Id;
                    var meta = new TaskMeta(project.Name, code, card.Title ?? "");
                    if (statuses.TryGetValue(card.Id, out var status)) meta.Status = status;
                    if (!string.IsNullOrWhiteSpace(card.FileName))
                    {
                        try
                        {
                            var text = HubMetricReader.ReadText(project.Path, "tasks/" + card.FileName);
                            if (text != null) ParseTask(meta, text);
                        }
                        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
                        {
                            warnings.Add("Không đọc được file task " + card.Id + " của " + project.Name + ".");
                        }
                    }
                    result.Add(meta);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            {
                warnings.Add("Không đọc được bảng việc của " + project.Name + ".");
            }
        }
        AttachRuns(result, runs);
        return result;
    }

    private static Dictionary<string, OfficeTaskStatus> ReadStatuses(ProjectInfo project, List<string> warnings)
    {
        var map = new Dictionary<string, OfficeTaskStatus>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = HubMetricReader.ReadText(project.Path, "tasks/README.md");
            if (raw == null) return map;
            foreach (var line in raw.Split('\n').Where(l => l.StartsWith('|')))
            {
                var cells = line.Split('|');
                if (cells.Length < 5) continue;
                var id = Regex.Match(cells[1], @"[A-Za-z][A-Za-z0-9]*-\d+").Value;
                if (id.Length == 0) continue;
                map[id] = WorkOverviewService.ParseStatus(cells[^2].Trim(), TaskState.Unknown);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            warnings.Add("Không đọc được trạng thái task của " + project.Name + ".");
        }
        return map;
    }

    private static void AttachRuns(List<TaskMeta> tasks, IReadOnlyList<HubMetric> runs)
    {
        var map = tasks.GroupBy(t => (t.Project.ToLowerInvariant(), t.Code))
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var run in runs)
        {
            var key = (run.Project.ToLowerInvariant(), run.Task);
            if (map.TryGetValue(key, out var list))
            {
                foreach (var task in list) task.Runs.Add(run);
            }
            else
            {
                var meta = new TaskMeta(run.Project, run.Task, run.Task);
                meta.Runs.Add(run);
                tasks.Add(meta);
                map[key] = [meta];
            }
        }
        foreach (var task in tasks) task.UpdatedAt = task.LatestRun?.At;
    }

    private static void ParseTask(TaskMeta meta, string text)
    {
        var sections = SplitSections(text);
        var difficulty = Regex.Match(text, @"(?im)^\s*[-*]?\s*\*\*Độ khó:\*\*\s*([SML])\b");
        if (!difficulty.Success) difficulty = Regex.Match(text, @"(?im)Độ khó\s*[:：]\s*([SML])\b");
        if (difficulty.Success) meta.Difficulty = difficulty.Groups[1].Value.ToUpperInvariant();
        foreach (var section in sections.Where(s => s.Title.StartsWith("Phạm vi", StringComparison.OrdinalIgnoreCase)))
            foreach (var token in ExtractScopeTokens(section.Markdown))
            {
                var kind = ClassifyScope(token);
                if (kind != null) meta.WorkTypes.Add(kind);
            }
        var review = string.Join("\n", sections
            .Where(s => s.Title.StartsWith("Review", StringComparison.OrdinalIgnoreCase) || s.Title.StartsWith("Đánh giá", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Markdown));
        meta.ReviewRounds = ReviewParser.Parse(review);
        meta.Findings = FindingParser.Parse(review);
    }

    private static IReadOnlyList<TaskSection> SplitSections(string content)
    {
        var sections = new List<TaskSection>();
        string? title = null;
        var lines = new List<string>();
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.TrimStart().StartsWith("## ", StringComparison.Ordinal))
            {
                if (title != null) sections.Add(new TaskSection(title, string.Join("\n", lines).Trim()));
                lines.Clear();
                title = line.TrimStart()[3..].Trim();
            }
            else if (title != null)
            {
                lines.Add(line);
            }
        }
        if (title != null) sections.Add(new TaskSection(title, string.Join("\n", lines).Trim()));
        return sections;
    }

    private static IEnumerable<string> ExtractScopeTokens(string markdown)
    {
        foreach (Match match in Regex.Matches(markdown, "`([^`]+)`"))
            if (match.Groups[1].Value.Length <= 256) yield return match.Groups[1].Value;
        foreach (Match match in Regex.Matches(markdown, @"[A-Za-z0-9_./\\-]+\.(?:cs|cshtml|css|js|ts|razor|ps1|psm1|psd1|html|xaml|resx|svg|sln|csproj)"))
            yield return match.Value;
    }

    private static string? ClassifyScope(string raw)
    {
        var path = raw.Trim().Replace('\\', '/').ToLowerInvariant();
        if (path.Length == 0) return null;
        if (path.Contains("/tests/") || path.Contains(".tests/") || path.StartsWith("tests/") ||
            path.Contains("/test/") || path.EndsWith("tests.cs")) return "test";
        if (path.EndsWith(".ps1") || path.EndsWith(".psm1") || path.EndsWith(".psd1")) return "PowerShell";
        if (path.EndsWith(".cshtml") || path.EndsWith(".razor") || path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".ts") || path.EndsWith(".html") || path.EndsWith(".xaml") || path.EndsWith(".resx") ||
            path.EndsWith(".svg")) return "giao diện";
        if (path.EndsWith(".cs") || path.EndsWith(".csproj") || path.EndsWith(".sln") ||
            path.EndsWith(".props") || path.EndsWith(".targets")) return "C# lõi";
        return null;
    }

    private static IReadOnlyList<PerformanceMetric> BuildMetrics(
        List<TaskMeta> currentTasks, List<TaskMeta> previousTasks,
        List<HubMetric> currentRuns, List<HubMetric> previousRuns, DateTimeOffset now, DateTimeOffset weekStart)
    {
        var (currentPass, _) = PassRate(currentTasks);
        var (previousPass, _) = PassRate(previousTasks);
        var (currentMinutes, _) = AverageTaskMinutes(currentTasks, weekStart, weekStart.AddDays(7));
        var (previousMinutes, _) = AverageTaskMinutes(previousTasks, PreviousWeekStart(weekStart), weekStart);
        var (currentTokens, _) = AverageTaskTokens(currentTasks, weekStart, weekStart.AddDays(7));
        var (previousTokens, _) = AverageTaskTokens(previousTasks, PreviousWeekStart(weekStart), weekStart);
        var currentPaused = PausedOverdue(currentTasks, now);
        var previousPaused = PausedOverdue(previousTasks, weekStart);
        var currentRepeated = RepeatedCauses(currentTasks);
        var previousRepeated = RepeatedCauses(previousTasks);
        var currentCoverage = MeasurementCoverage(currentTasks, currentRuns);
        var previousCoverage = MeasurementCoverage(previousTasks, previousRuns);
        return
        [
            new("M1", "Phút chủ dự án bỏ ra", "≤ 10 phút / yêu cầu", PerformanceMetricKind.Minutes,
                null, null, 0, 0, true),
            new("M2", "Đạt ngay vòng đầu", "≥ 80 %", PerformanceMetricKind.Percent,
                currentPass, previousPass, currentTasks.Count, previousTasks.Count),
            new("M3", "Phút mỗi task", "≤ 30 phút", PerformanceMetricKind.Minutes,
                currentMinutes, previousMinutes, currentTasks.Count, previousTasks.Count),
            new("M4", "Token mỗi task", "giảm 30 % mỗi quý", PerformanceMetricKind.Tokens,
                currentTokens, previousTokens, currentTasks.Count, previousTasks.Count),
            new("M5", "Việc kẹt quá hạn", "0", PerformanceMetricKind.Count,
                currentPaused, previousPaused, currentTasks.Count, previousTasks.Count),
            new("M6", "Lỗi lặp lại", "0", PerformanceMetricKind.Count,
                currentRepeated, previousRepeated, currentTasks.Count, previousTasks.Count),
            new("M7", "Dữ liệu hiện trên Hub", "100 %", PerformanceMetricKind.Percent,
                currentCoverage, previousCoverage, currentTasks.Count, previousTasks.Count)
        ];
    }

    private static DateTimeOffset PreviousWeekStart(DateTimeOffset weekStart) => weekStart.AddDays(-7);

    private static (double? Value, int Sample) PassRate(List<TaskMeta> tasks)
    {
        var reviewed = tasks.Where(t => t.FirstRoundVerdict != null).ToList();
        return (reviewed.Count == 0 ? null : reviewed.Count(t => t.FirstRoundVerdict == ReviewVerdict.Pass) * 100.0 / reviewed.Count, tasks.Count);
    }

    private static (double? Value, int Sample) AverageTaskMinutes(List<TaskMeta> tasks, DateTimeOffset start, DateTimeOffset end)
    {
        var measured = tasks.Select(t => t.MinutesIn(start, end)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return (measured.Count == 0 ? null : measured.Average(), tasks.Count);
    }

    private static (double? Value, int Sample) AverageTaskTokens(List<TaskMeta> tasks, DateTimeOffset start, DateTimeOffset end)
    {
        var measured = tasks.Select(t => t.TokensIn(start, end)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return (measured.Count == 0 ? null : measured.Average(), tasks.Count);
    }

    private static double? PausedOverdue(List<TaskMeta> tasks, DateTimeOffset end)
    {
        if (tasks.Count == 0) return null;
        return tasks.Count(t => t.Status == OfficeTaskStatus.Paused && t.UpdatedAt.HasValue && t.UpdatedAt.Value < end.AddHours(-24));
    }

    private static double? RepeatedCauses(List<TaskMeta> tasks)
    {
        var reviewed = tasks.Where(t => t.ReviewRounds.Count > 0 || t.Findings.Count > 0).ToList();
        if (reviewed.Count == 0) return null;
        return reviewed.SelectMany(t => t.Findings.Select(f => (t.Code, f.Cause)))
            .GroupBy(x => x.Cause)
            .Count(g => g.Select(x => x.Code).Distinct().Count() >= 2);
    }

    private static double? MeasurementCoverage(List<TaskMeta> tasks, List<HubMetric> runs)
    {
        if (runs.Count == 0) return null;
        var measured = runs.Count(r => r.Seconds.HasValue || r.Tokens.HasValue);
        return measured * 100.0 / runs.Count;
    }

    private static IReadOnlyList<PerformanceCauseCount> BuildCauses(List<TaskMeta> currentTasks, List<TaskMeta> previousTasks)
    {
        var current = currentTasks.SelectMany(t => t.Findings).GroupBy(f => f.Cause).ToDictionary(g => g.Key, g => g.Count());
        var previous = previousTasks.SelectMany(t => t.Findings).GroupBy(f => f.Cause).ToDictionary(g => g.Key, g => g.Count());
        return CauseOrder.Select(cause => new PerformanceCauseCount(cause, current.GetValueOrDefault(cause), previous.GetValueOrDefault(cause))).ToList();
    }

    private static IReadOnlyList<PerformanceProviderScore> BuildScores(List<TaskMeta> tasks, IReadOnlyList<HubMetric> runs)
    {
        var attributed = tasks.Where(t => t.LatestRun != null)
            .GroupBy(t => (Provider: NormalizeProvider(t.LatestRun!.Provider), t.LatestRun.Model))
            .ToDictionary(g => g.Key, g => g.ToList());
        var scores = new List<PerformanceProviderScore>();
        foreach (var group in runs.GroupBy(r => (Provider: NormalizeProvider(r.Provider), r.Model)))
        {
            var groupTasks = attributed.GetValueOrDefault(group.Key) ?? [];
            var reviewed = groupTasks.Where(t => t.FirstRoundVerdict != null).ToList();
            var round1 = reviewed.Count == 0 ? (double?)null : reviewed.Count(t => t.FirstRoundVerdict == ReviewVerdict.Pass) * 100.0 / reviewed.Count;
            var averageRounds = reviewed.Count == 0 ? (double?)null : reviewed.Average(t => t.ReviewRounds.Count);
            var minutesS = Average(groupTasks.Where(t => t.Difficulty == "S").Select(t => t.TotalMinutes));
            var minutesM = Average(groupTasks.Where(t => t.Difficulty == "M").Select(t => t.TotalMinutes));
            var minutesL = Average(groupTasks.Where(t => t.Difficulty == "L").Select(t => t.TotalMinutes));
            var tokens = Average(groupTasks.Select(t => t.TotalTokens is long value ? (double?)value : null));
            var ag = groupTasks.SelectMany(t => t.Findings).Where(f => f.Cause == "AG")
                .GroupBy(f => string.IsNullOrWhiteSpace(f.Kind) ? "khác" : f.Kind)
                .ToDictionary(g => g.Key, g => g.Count());
            var workTypes = groupTasks.SelectMany(t => t.WorkTypes).GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => Array.IndexOf(WorkTypeOrder, g.Key))
                .Select(g => g.Key).ToList();
            scores.Add(new PerformanceProviderScore(
                group.Key.Provider, group.Key.Model, groupTasks.Count, round1, averageRounds,
                minutesS, minutesM, minutesL, tokens, ag,
                group.Count(IsQuota), workTypes, groupTasks.Count >= 3,
                groupTasks.Select(t => t.Code).OrderBy(c => c).ToList()));
        }
        return scores.OrderByDescending(s => s.TaskCount)
            .ThenBy(s => s.Provider, StringComparer.Ordinal)
            .ThenBy(s => s.Model ?? "", StringComparer.Ordinal)
            .ToList();
    }

    private static double? Average(IEnumerable<double?> values)
    {
        var measured = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return measured.Count == 0 ? null : measured.Average();
    }

    private static string NormalizeProvider(string? provider) => string.IsNullOrWhiteSpace(provider) ? "không rõ" : provider.ToLowerInvariant();

    private static bool IsQuota(HubMetric run) =>
        run.Flags.Any(f => f.Equals("HM", StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(run.Status) &&
         (run.Status.Contains("QUOTA", StringComparison.OrdinalIgnoreCase) ||
          run.Status.Contains("429", StringComparison.OrdinalIgnoreCase) ||
          run.Status.Contains("EXHAUSTED", StringComparison.OrdinalIgnoreCase)));

    private static PerformanceTaskRow ToRow(TaskMeta task) => new(
        task.Project, task.Code, task.Title, task.LatestRun?.Provider, task.LatestRun?.Model, task.Difficulty,
        task.ReviewRounds.Count, task.FirstRoundVerdict switch { ReviewVerdict.Pass => true, ReviewVerdict.NeedsFix => false, _ => null },
        task.TotalMinutes, task.TotalTokens,
        task.Findings.Select(f => f.Cause).Distinct().ToList(),
        task.WorkTypes.OrderBy(t => Array.IndexOf(WorkTypeOrder, t)).ToList(), task.Status);

    private sealed class TaskMeta(string project, string code, string title)
    {
        public string Project { get; } = project;
        public string Code { get; } = code;
        public string Title { get; } = title;
        public string Difficulty { get; set; } = "chưa rõ";
        public OfficeTaskStatus Status { get; set; } = OfficeTaskStatus.Pending;
        public DateTimeOffset? UpdatedAt { get; set; }
        public List<HubMetric> Runs { get; } = [];
        public IReadOnlyList<ReviewRound> ReviewRounds { get; set; } = [];
        public IReadOnlyList<StructuredFinding> Findings { get; set; } = [];
        public HashSet<string> WorkTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ReviewVerdict? FirstRoundVerdict => ReviewRounds.Count > 0 ? ReviewRounds[0].Verdict : null;
        public HubMetric? LatestRun => Runs.Count == 0 ? null : Runs.OrderByDescending(r => r.At).ThenByDescending(r => r.Run).First();
        public double? TotalMinutes => Runs.Any(r => r.Seconds.HasValue) ? Runs.Where(r => r.Seconds.HasValue).Sum(r => r.Seconds!.Value) / 60 : null;
        public long? TotalTokens => Runs.Any(r => r.Tokens.HasValue) ? Runs.Where(r => r.Tokens.HasValue).Sum(r => r.Tokens!.Value) : null;

        public double? MinutesIn(DateTimeOffset start, DateTimeOffset end)
        {
            var values = Runs.Where(r => InWindow(r.At, start, end) && r.Seconds.HasValue).ToList();
            return values.Count == 0 ? null : values.Sum(r => r.Seconds!.Value) / 60;
        }

        public long? TokensIn(DateTimeOffset start, DateTimeOffset end)
        {
            var values = Runs.Where(r => InWindow(r.At, start, end) && r.Tokens.HasValue).ToList();
            return values.Count == 0 ? null : values.Sum(r => r.Tokens!.Value);
        }
    }
}
