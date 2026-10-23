using System.Globalization;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// Biểu đồ thời gian trang Tổng quan (R-003 / H3-02). Hàm thuần: TaskInsight ⇒ toạ độ SVG. Razor chỉ vẽ.

/// <summary>Một thanh. Css: "coding" / "fix" / "review" / "merge" (đúng tên SegmentKind viết thường).</summary>
public sealed record GanttBar(string Css, double X, double Width, string Tooltip);

/// <summary>Một hàng = một task. Label "V4-06 · Lan".</summary>
public sealed record GanttRow(string Project, string Id, string Label, Difficulty Difficulty, double Y, IReadOnlyList<GanttBar> Bars);

public sealed record GanttTick(double X, string Label);

public sealed record GanttChart(DateTime From, DateTime To, double Width, double Height, IReadOnlyList<GanttRow> Rows, IReadOnlyList<GanttTick> Ticks);

public static class GanttLayout
{
    public const double LabelWidth = 150;   // cột nhãn bên trái
    public const double RightPad = 10;
    public const double Top = 10;
    public const double RowHeight = 34;
    public const double AxisHeight = 22;
    public const double MinBarWidth = 3;    // đoạn rất ngắn / điểm merge vẫn nhìn thấy

    /// <summary>
    /// Khung giờ "Gần nhất": lấy tối đa maxRows task có Timeline (theo End lớn nhất của task, mới nhất trước);
    /// from = Start nhỏ nhất, to = End lớn nhất của các task đó. Khoảng &lt; 30 phút ⇒ nới thành 30 phút quanh tâm.
    /// Sau đó đệm 3% mỗi bên. Không task nào có Timeline ⇒ (now − 24 giờ, now).
    /// </summary>
    public static (DateTime From, DateTime To) FitWindow(IReadOnlyList<TaskInsight> tasks, DateTime now, int maxRows = 12)
    {
        var selected = tasks
            .Where(t => t.Timeline.Count > 0)
            .OrderByDescending(t => t.Timeline.Max(s => s.End))
            .Take(maxRows)
            .ToList();

        if (selected.Count == 0)
            return (now.AddHours(-24), now);

        var segments = selected.SelectMany(t => t.Timeline).ToList();
        var from = segments.Min(s => s.Start);
        var to = segments.Max(s => s.End);
        var span = to - from;

        if (span < TimeSpan.FromMinutes(30))
        {
            var center = new DateTime((from.Ticks + to.Ticks) / 2, from.Kind);
            from = center.AddMinutes(-15);
            to = center.AddMinutes(15);
            span = TimeSpan.FromMinutes(30);
        }

        var pad = TimeSpan.FromTicks((long)Math.Round(span.Ticks * 0.03));
        return (from - pad, to + pad);
    }


    /// <summary>
    /// from &gt;= to ⇒ ArgumentException.
    /// Hàng: task có ÍT NHẤT một đoạn giao [from, to] (đoạn Merge là điểm: from ≤ Start ≤ to); lấy tối đa maxRows task
    /// có End lớn nhất, rồi sắp theo Start nhỏ nhất của task TĂNG dần (task cũ ở trên). Hàng i: Y = Top + i·RowHeight.
    /// Thanh: mọi đoạn của task (kể cả đoạn nằm ngoài khung — bị kẹp vào khung):
    ///   X(t) = LabelWidth + (t − from) / (to − from) · (width − LabelWidth − RightPad), kẹp trong [LabelWidth, width − RightPad];
    ///   Width = max(X(End) − X(Start), MinBarWidth). Làm tròn X, Width 1 số lẻ.
    /// Tooltip: Coding "Code 23:43–23:48 (5′)", Fix "Sửa lại 23:47–23:51 (4′)", Review "Review 23:51–23:59 (8′)", Merge "Merge 23:59"
    ///   (phút = làm tròn TotalMinutes; giờ "HH:mm"; khác ngày giữa Start và End thì thêm " dd/MM" sau giờ End).
    /// Height = Top + số hàng·RowHeight + AxisHeight.
    /// Mốc: bước nhỏ nhất trong {10′, 20′, 30′, 1h, 2h, 3h, 6h, 12h, 1 ngày, 2 ngày, 7 ngày} sao cho số mốc trong [from, to] ≤ 8;
    ///   mốc đầu = bội số đầu tiên của bước ≥ from (tính từ 00:00 của ngày from; bước 7 ngày tính từ 00:00 ngày from);
    ///   nhãn "HH:mm" nếu bước &lt; 1 ngày, ngược lại "dd/MM". X tính như trên, làm tròn 1 số lẻ.
    /// </summary>
    private static readonly TimeSpan[] Steps =
    {
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(20),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(3),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(12),
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(2),
        TimeSpan.FromDays(7),
    };

    public static GanttChart Build(IReadOnlyList<TaskInsight> tasks, DateTime from, DateTime to, double width = 900, int maxRows = 12)
    {
        if (from >= to)
            throw new ArgumentException("from must be less than to.", nameof(from));

        double totalTicks = (to - from).Ticks;
        double plotWidth = width - LabelWidth - RightPad;

        double MapX(DateTime t)
        {
            double raw = LabelWidth + ((t - from).Ticks / totalTicks) * plotWidth;
            return Math.Clamp(raw, LabelWidth, width - RightPad);
        }

        var visibleTasks = tasks
            .Where(t => t.Timeline.Any(s => s.End >= from && s.Start <= to))
            .OrderByDescending(t => t.Timeline.Max(s => s.End))
            .Take(maxRows)
            .OrderBy(t => t.Timeline.Min(s => s.Start))
            .ToList();

        var rows = new List<GanttRow>(visibleTasks.Count);
        for (int i = 0; i < visibleTasks.Count; i++)
        {
            var task = visibleTasks[i];
            var y = Top + i * RowHeight;
            var bars = task.Timeline.Select(s =>
            {
                var x1 = MapX(s.Start);
                var x2 = MapX(s.End);
                var x = Math.Round(x1, 1);
                var w = Math.Round(Math.Max(x2 - x1, MinBarWidth), 1);
                return new GanttBar(s.Kind.ToString().ToLowerInvariant(), x, w, FormatTooltip(s));
            }).ToList();

            rows.Add(new GanttRow(task.Project, task.Id, $"{task.Id} · {task.StaffName}", task.Difficulty, y, bars));
        }

        IReadOnlyList<GanttTick> ticks = Array.Empty<GanttTick>();
        foreach (var step in Steps)
        {
            var candidate = BuildTicks(from, to, step, MapX);
            ticks = candidate;
            if (candidate.Count <= 8)
                break;
        }

        var height = Top + rows.Count * RowHeight + AxisHeight;
        return new GanttChart(from, to, width, height, rows, ticks);
    }

    private static string FormatTooltip(TimelineSegment s)
    {
        var startStr = s.Start.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (s.Kind == SegmentKind.Merge)
            return $"Merge {startStr}";

        var kindName = s.Kind switch
        {
            SegmentKind.Coding => "Code",
            SegmentKind.Fix => "Sửa lại",
            SegmentKind.Review => "Review",
            _ => s.Kind.ToString(),
        };
        var endStr = s.Start.Date == s.End.Date
            ? s.End.ToString("HH:mm", CultureInfo.InvariantCulture)
            : s.End.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture);
        var mins = (int)Math.Round((s.End - s.Start).TotalMinutes);
        return $"{kindName} {startStr}–{endStr} ({mins}′)";
    }

    private static List<GanttTick> BuildTicks(DateTime from, DateTime to, TimeSpan step, Func<DateTime, double> mapX)
    {
        var anchor = from.Date;
        var offsetTicks = (from - anchor).Ticks;
        var k = offsetTicks <= 0 ? 0 : (offsetTicks + step.Ticks - 1) / step.Ticks;
        var cur = anchor.AddTicks(k * step.Ticks);
        var fmt = step < TimeSpan.FromDays(1) ? "HH:mm" : "dd/MM";

        var list = new List<GanttTick>();
        while (cur <= to)
        {
            list.Add(new GanttTick(Math.Round(mapX(cur), 1), cur.ToString(fmt, CultureInfo.InvariantCulture)));
            cur = cur.Add(step);
        }
        return list;
    }

}
