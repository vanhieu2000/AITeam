using System.Globalization;
using System.Text.RegularExpressions;

namespace AITeam.Hub.Services;

/// <summary>Chín bước theo brief R-011, theo thứ tự hiển thị.</summary>
public enum WorkStep { Received, Plan, PlanApproval, Prep, Code, Verify, Qc, Acceptance, Merge }

/// <summary>Waiting: chưa có bằng chứng; Current: đang chờ làm; Done: có bằng chứng đạt; Failed: cần sửa.</summary>
public enum WorkStepState { Waiting, Current, Done, Failed }

/// <summary>Loại thao tác có trong dòng workflow; bảng cũ không phân loại được dùng Unknown.</summary>
public enum WorkAction { Unknown, Prep, Assign, Verify, Qc, Merge }

/// <summary>Kết quả đã chuẩn hóa; BLOCKED khác lỗi thông thường nhưng đều giữ bước cần sửa.</summary>
public enum WorkOutcome { Unknown, Passed, Failed, Blocked }

/// <summary>Sự kiện riêng của parser, không thay hợp đồng Hub. Time giữ offset; thiếu giờ/người dùng null.
/// Round=0 và TaskId="" chỉ dành cho bảng cũ chưa xác định được. Text giữ nội dung, bỏ marker workflow.</summary>
public sealed record WorkProgressEvent(
    DateTimeOffset? Time, string TaskId, int Round, WorkAction Action,
    WorkOutcome Outcome, string? Actor, string Text);

/// <summary>Một bước hiển thị. Thiếu giờ hoặc người: từng trường tương ứng là "Chưa có dữ liệu".
/// TimeText có giờ dùng "HH:mm dd/MM/yyyy" tại UTC+07:00, không phụ thuộc múi giờ máy.</summary>
public sealed record WorkProgressStep(
    WorkStep Step, WorkStepState State, string TimeText, string ActorText);

/// <summary>Kết quả thuần: đúng chín bước; LatestError là nội dung lỗi đang hiệu lực hoặc null.
/// IsMerged chỉ true khi có Merge đạt hoặc boardStatus chứa ✅; không suy diễn Acceptance Done.</summary>
public sealed record WorkProgress(
    WorkStep CurrentStep, IReadOnlyList<WorkProgressStep> Steps, string? LatestError, bool IsMerged);

/// <summary>Parser thuần, không I/O, không chạy lệnh, không trạng thái dùng chung; không đăng ký DI.</summary>
public static class WorkProgressParser
{
    private const string MissingMeta = "Chưa có dữ liệu";
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);
    private static readonly Regex TaskIdRegex = new(@"^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9-]*$", RegexOptions.Compiled);
    private static readonly Regex RoundRegex = new(@"^lượt\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MarkerRegex = new(@"\s*<!--\s*workflow:[\s\S]*?-->\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IsoTimeRegex = new(@"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}(:\d{2}(\.\d+)?)?([Zz]|[+-]\d{2}:?\d{2})$", RegexOptions.Compiled);
    private static readonly string[] VnDateFormats = { "HH:mm dd/MM/yyyy", "H:mm dd/MM/yyyy", "HH:mm d/M/yyyy", "H:mm d/M/yyyy" };
    private static readonly string[] HourFormats = { "HH:mm", "H:mm" };

    /// <summary>
    /// null/rỗng trả danh sách rỗng. Chấp nhận BOM đầu chuỗi, CRLF/LF/CR.
    /// Chỉ đọc mục heading cấp 2 có "Nhật ký" (kể cả "## 3. Nhật ký (…)"), dừng ở heading cấp 2 kế tiếp.
    /// Bảng cũ | Giờ | Sự kiện | Ghi chú |: bỏ header, separator, ô sự kiện rỗng;
    /// dòng không xác định task/thao tác vẫn giữ Text, Unknown, Round=0, không đoán kết quả từ văn xuôi.
    /// Workflow: "- giờ | task | Prep/Assign/Verify/Qc/Merge | lượt N | kết quả &lt;!-- workflow:… --&gt;".
    /// Task phải khớp ^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9-]*$, lượt là số nguyên dương.
    /// Sai số cột, task, lượt hoặc thao tác: bỏ dòng; sai/thiếu giờ: vẫn giữ sự kiện với Time=null.
    /// Giờ ISO có offset giữ nguyên; "HH:mm dd/MM/yyyy" dùng +07:00; chỉ HH:mm dùng ngày receivedAt
    /// tại +07:00, không có receivedAt thì null. Actor=null nếu nguồn không chỉ rõ người.
    /// ĐẠT/PASS/PASSED (nguyên từ đầu kết quả) → Passed; CẦN SỬA/FAIL/FAILED/LỖI → Failed;
    /// BLOCKED → Blocked; còn lại Unknown. Không coi "KHÔNG ĐẠT" là Passed.
    /// Loại dòng trùng hoàn toàn sau Trim; giữ thứ tự nguồn. Không đọc marker như lệnh hay HTML.
    /// </summary>
    public static IReadOnlyList<WorkProgressEvent> Parse(string? markdown, DateTimeOffset? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return Array.Empty<WorkProgressEvent>();

        var lines = markdown.TrimStart('\uFEFF').Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var events = new List<WorkProgressEvent>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var inJournal = false;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("## ", StringComparison.Ordinal) && !trimmed.StartsWith("###", StringComparison.Ordinal))
            {
                if (inJournal) break;
                if (trimmed.Contains("Nhật ký", StringComparison.OrdinalIgnoreCase)) inJournal = true;
                continue;
            }
            if (!inJournal || string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed)) continue;

            if (trimmed.StartsWith('|'))
            {
                var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cells.Length < 2 || cells.All(c => c.All(ch => ch is '-' or ':' or ' '))) continue;
                if (cells[0].Equals("Giờ", StringComparison.OrdinalIgnoreCase) &&
                    cells[1].Equals("Sự kiện", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(cells[1])) continue;

                var note = cells.Length > 2 ? cells[2] : "";
                var text = string.IsNullOrEmpty(note) ? cells[1] : $"{cells[1]} ({note})";
                events.Add(new WorkProgressEvent(
                    ParseTime(cells[0], receivedAt), "", 0, WorkAction.Unknown, WorkOutcome.Unknown, null, text));
            }
            else if (trimmed.StartsWith('-'))
            {
                var body = MarkerRegex.Replace(trimmed[1..].TrimStart(), "");
                var parts = body.Split('|').Select(p => p.Trim()).ToArray();
                if (parts.Length != 5 || !TaskIdRegex.IsMatch(parts[1])) continue;
                var action = ParseAction(parts[2]);
                if (action == WorkAction.Unknown) continue;
                var roundMatch = RoundRegex.Match(parts[3]);
                if (!roundMatch.Success || !int.TryParse(roundMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var round) || round <= 0)
                    continue;
                if (string.IsNullOrEmpty(parts[4])) continue;

                events.Add(new WorkProgressEvent(
                    ParseTime(parts[0], receivedAt), parts[1], round, action, ParseOutcome(parts[4]), null, parts[4]));
            }
        }
        return events;
    }

    /// <summary>
    /// Trả đúng 9 bước theo enum; mặc định Received=Current, còn lại Waiting. planExists chuyển sang PlanApproval (Received/Plan Done);
    /// planApproved chuyển sang Prep (Received/Plan/PlanApproval Done); chỉ đánh dấu Done những bước có bằng chứng, không điền người/giờ giả.
    /// Lọc đúng TaskId (OrdinalIgnoreCase), bỏ Unknown/round&lt;1; chỉ dùng lượt mới nhất theo số.
    /// Trong lượt so theo giờ UTC; hòa giữ thứ tự nguồn, thiếu giờ xếp sau.
    /// Prep/Verify/QC/Merge đạt hoàn thành chính bước đó; Assign đạt chỉ chuyển Code Current.
    /// Lỗi/BLOCKED giữ bước Failed, chặn bước sau đến khi đạt lại; LatestError là lỗi đang hiệu lực.
    /// Merge đạt mới hơn hoặc boardStatus chứa ✅ hoàn thành Merge, không suy diễn nghiệm thu; xóa lỗi cũ.
    /// 🟡/🔵/🔴 không tự chứng minh gate đạt; không suy diễn Done từ kết quả bước khác.
    /// Chuỗi hiển thị thiếu giờ/người là "Chưa có dữ liệu"; giờ rõ đổi +07:00 định dạng HH:mm dd/MM/yyyy.
    /// null events coi rỗng. Không đổi đầu vào, không I/O, không suy luận từ đồng hồ hoặc máy hiện tại.
    /// </summary>
    public static WorkProgress Calculate(
        string taskId, IReadOnlyList<WorkProgressEvent>? events,
        bool planExists = false, bool planApproved = false, string? boardStatus = null)
    {
        var allSteps = Enum.GetValues<WorkStep>();
        var done = allSteps.ToDictionary(s => s, _ => false);
        var errors = allSteps.ToDictionary(s => s, _ => (string?)null);
        var timeText = allSteps.ToDictionary(s => s, _ => MissingMeta);
        var actorText = allSteps.ToDictionary(s => s, _ => MissingMeta);

        if (planExists || planApproved) { done[WorkStep.Received] = true; done[WorkStep.Plan] = true; }
        if (planApproved) done[WorkStep.PlanApproval] = true;

        var currentStep = planApproved ? WorkStep.Prep
            : planExists ? WorkStep.PlanApproval : WorkStep.Received;
        var isMerged = false;
        var workflow = (events ?? Array.Empty<WorkProgressEvent>())
            .Select((ev, index) => (Event: ev, Index: index))
            .Where(x => x.Event != null
                && !string.IsNullOrWhiteSpace(taskId)
                && string.Equals(x.Event.TaskId, taskId.Trim(), StringComparison.OrdinalIgnoreCase)
                && x.Event.Round >= 1
                && x.Event.Action != WorkAction.Unknown
                && x.Event.Outcome != WorkOutcome.Unknown)
            .ToArray();
        var latestRound = workflow.Length == 0 ? 0 : workflow.Max(x => x.Event.Round);

        // Chỉ lượt mới nhất có hiệu lực; trong lượt so giờ UTC, thiếu giờ giữ cuối.
        foreach (var item in workflow.Where(x => x.Event.Round == latestRound)
            .OrderBy(x => x.Event.Time.HasValue ? 0 : 1)
            .ThenBy(x => x.Event.Time?.UtcDateTime)
            .ThenBy(x => x.Index))
        {
            var ev = item.Event;
            var step = MapStep(ev.Action);
            SetMeta(step, ev, timeText, actorText);
            if (ev.Outcome is WorkOutcome.Failed or WorkOutcome.Blocked)
            {
                done[step] = false;
                errors[step] = ev.Text;
                isMerged = false;
                continue;
            }

            errors[step] = null;
            // Assign chỉ chứng minh đã giao việc, không chứng minh code hoàn thành.
            done[step] = ev.Action != WorkAction.Assign;
            var nextStep = ev.Action switch
            {
                WorkAction.Prep or WorkAction.Assign => WorkStep.Code,
                WorkAction.Verify => WorkStep.Qc,
                WorkAction.Qc => WorkStep.Acceptance,
                _ => WorkStep.Merge
            };
            // Bằng chứng bước sau xác định tiến độ, không đánh Done các bước bị thiếu.
            if (nextStep > currentStep) currentStep = nextStep;

            if (ev.Action == WorkAction.Merge)
            {
                isMerged = true;
                foreach (var key in allSteps) errors[key] = null;
            }
        }

        isMerged |= boardStatus?.Contains("✅", StringComparison.Ordinal) ?? false;
        if (isMerged)
        {
            done[WorkStep.Merge] = true;
            currentStep = WorkStep.Merge;
            foreach (var step in allSteps) errors[step] = null;
        }
        else
        {
            // Lỗi chưa được chính bước đó đạt lại luôn chặn tiến độ, kể cả sau Assign.
            var failedStep = allSteps.Where(step => errors[step] != null).Cast<WorkStep?>().FirstOrDefault();
            if (failedStep.HasValue) currentStep = failedStep.Value;
        }
        var latestError = allSteps.Select(step => errors[step]).FirstOrDefault(error => error != null);
        var stepList = allSteps.Select(s =>
        {
            var state = errors[s] != null ? WorkStepState.Failed
                : done[s] ? WorkStepState.Done
                : currentStep == s ? WorkStepState.Current
                : WorkStepState.Waiting;
            return new WorkProgressStep(s, state, timeText[s], actorText[s]);
        }).ToArray();

        return new WorkProgress(currentStep, stepList, latestError, isMerged);
    }

    private static void SetMeta(
        WorkStep step, WorkProgressEvent ev,
        Dictionary<WorkStep, string> timeText, Dictionary<WorkStep, string> actorText)
    {
        timeText[step] = ev.Time.HasValue
            ? ev.Time.Value.ToOffset(VnOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture)
            : MissingMeta;
        actorText[step] = string.IsNullOrWhiteSpace(ev.Actor) ? MissingMeta : ev.Actor.Trim();
    }

    private static WorkStep MapStep(WorkAction action) => action switch
    {
        WorkAction.Prep => WorkStep.Prep,
        WorkAction.Assign => WorkStep.Code,
        WorkAction.Verify => WorkStep.Verify,
        WorkAction.Qc => WorkStep.Qc,
        WorkAction.Merge => WorkStep.Merge,
        _ => WorkStep.Received
    };

    private static WorkAction ParseAction(string value) => value.ToLowerInvariant() switch
    {
        "prep" => WorkAction.Prep,
        "assign" => WorkAction.Assign,
        "verify" => WorkAction.Verify,
        "qc" => WorkAction.Qc,
        "merge" => WorkAction.Merge,
        _ => WorkAction.Unknown
    };

    private static WorkOutcome ParseOutcome(string text)
    {
        if (HasPrefix(text, "PASSED") || HasPrefix(text, "PASS") || HasPrefix(text, "ĐẠT")) return WorkOutcome.Passed;
        if (HasPrefix(text, "CẦN SỬA") || HasPrefix(text, "FAILED") || HasPrefix(text, "FAIL") || HasPrefix(text, "LỖI")) return WorkOutcome.Failed;
        return HasPrefix(text, "BLOCKED") ? WorkOutcome.Blocked : WorkOutcome.Unknown;
    }

    private static bool HasPrefix(string text, string prefix) =>
        text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        && (text.Length == prefix.Length || !char.IsLetterOrDigit(text[prefix.Length]));

    private static DateTimeOffset? ParseTime(string raw, DateTimeOffset? receivedAt)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTime.TryParseExact(raw, VnDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var vnDt))
            return new DateTimeOffset(vnDt.Year, vnDt.Month, vnDt.Day, vnDt.Hour, vnDt.Minute, 0, VnOffset);
        if (TimeOnly.TryParseExact(raw, HourFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hm))
        {
            if (!receivedAt.HasValue) return null;
            var d = receivedAt.Value.ToOffset(VnOffset);
            return new DateTimeOffset(d.Year, d.Month, d.Day, hm.Hour, hm.Minute, 0, VnOffset);
        }
        return IsoTimeRegex.IsMatch(raw) && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso)
            ? iso : null;
    }
}
