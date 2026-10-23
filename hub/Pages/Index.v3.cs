using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AITeam.Hub.Pages;

// Tổng quan v3 (R-003 / H3-02): chỉ số, biểu đồ thời gian, danh sách nhiệm vụ. Bố cục: tasks/H3-02-hub-overview-v3.md.
public partial class IndexModel
{
    public static readonly IReadOnlyList<string> Ranges = new[] { "fit", "24h", "7d", "30d" };

    /// <summary>?range=fit|24h|7d|30d. Giá trị khác (kể cả rỗng) ⇒ đặt lại "fit".</summary>
    [BindProperty(SupportsGet = true)] public string Range { get; set; } = "fit";

    public TeamInsights? Insights { get; private set; }
    public GanttChart? Chart { get; private set; }
    /// <summary>Tối đa 20 task đầu của Insights.Tasks (đã sắp mới nhất trước).</summary>
    public IReadOnlyList<TaskInsight> TaskRows { get; private set; } = Array.Empty<TaskInsight>();
    public int PendingApprovals { get; private set; }
    /// <summary>Lỗi đọc dữ liệu v3 (Insights.Get() ném lỗi) ⇒ thông báo, trang vẫn hiện phần cũ.</summary>
    public string? V3Error { get; private set; }

    /// <summary>Giờ hiện tại — test thay được.</summary>
    internal Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <summary>
    /// Gọi cuối OnGet (sau LoadData). Quy tắc:
    /// - Range không thuộc Ranges ⇒ Range = "fit".
    /// - PendingApprovals = số mục Pending của IApprovalService (null ⇒ 0; ném lỗi ⇒ 0).
    /// - _insights null ⇒ Insights / Chart null, TaskRows rỗng, KHÔNG lỗi.
    /// - Insights.Get() ném lỗi ⇒ V3Error = "Không đọc được dữ liệu đánh giá: " + ex.Message; Insights / Chart null.
    /// - Khung giờ: "24h" ⇒ (now − 24 giờ, now); "7d" ⇒ (now − 7 ngày, now); "30d" ⇒ (now − 30 ngày, now);
    ///   "fit" ⇒ GanttLayout.FitWindow(Insights.Tasks, now). Chart = GanttLayout.Build(Insights.Tasks, from, to) (width, maxRows mặc định).
    /// </summary>
    private void LoadV3()
    {
        if (!Ranges.Contains(Range))
            Range = "fit";

        try
        {
            PendingApprovals = _approvals?.GetAll().Count(a => a.Status == ApprovalStatus.Pending) ?? 0;
        }
        catch
        {
            PendingApprovals = 0;
        }

        if (_insights is null)
        {
            Insights = null;
            Chart = null;
            TaskRows = Array.Empty<TaskInsight>();
            V3Error = null;
            return;
        }

        try
        {
            Insights = _insights.Get();
        }
        catch (Exception ex)
        {
            V3Error = "Không đọc được dữ liệu đánh giá: " + ex.Message;
            Insights = null;
            Chart = null;
            TaskRows = Array.Empty<TaskInsight>();
            return;
        }

        V3Error = null;
        TaskRows = Insights.Tasks.Take(20).ToList();
        var now = Now();
        var (from, to) = Range switch
        {
            "24h" => (now.AddHours(-24), now),
            "7d" => (now.AddDays(-7), now),
            "30d" => (now.AddDays(-30), now),
            _ => GanttLayout.FitWindow(Insights.Tasks, now),
        };
        Chart = GanttLayout.Build(Insights.Tasks, from, to);
    }


    /// <summary>S ⇒ ("pill ok","S"); M ⇒ ("pill run","M"); L ⇒ ("pill err","L"); Unknown ⇒ ("pill n","—").</summary>
    public static (string Css, string Text) DifficultyPill(Difficulty d) => d switch
    {
        Difficulty.S => ("pill ok", "S"),
        Difficulty.M => ("pill run", "M"),
        Difficulty.L => ("pill err", "L"),
        _ => ("pill n", "—"),
    };


    /// <summary>Assigned ⇒ ("pill run","Đang làm"); Review ⇒ ("pill rev","Chờ review"); NeedsFix ⇒ ("pill fix","Cần sửa");
    /// Merged ⇒ ("pill ok","Đã merge"); Unknown ⇒ ("pill n","?").</summary>
    public static (string Css, string Text) StatePill(TaskState s) => s switch
    {
        TaskState.Assigned => ("pill run", "Đang làm"),
        TaskState.Review => ("pill rev", "Chờ review"),
        TaskState.NeedsFix => ("pill fix", "Cần sửa"),
        TaskState.Merged => ("pill ok", "Đã merge"),
        _ => ("pill n", "?"),
    };


    /// <summary>0.5 ⇒ "50%"; 0.666 ⇒ "67%" (làm tròn); null ⇒ "—".</summary>
    public static string Percent(double? rate)
        => rate is null ? "—" : $"{Math.Round(rate.Value * 100):0}%";

}
