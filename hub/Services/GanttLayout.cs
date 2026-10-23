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
        => throw new NotImplementedException(); // TODO(agent)

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
    public static GanttChart Build(IReadOnlyList<TaskInsight> tasks, DateTime from, DateTime to, double width = 900, int maxRows = 12)
        => throw new NotImplementedException(); // TODO(agent)
}
