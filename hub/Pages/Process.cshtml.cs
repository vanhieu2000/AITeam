using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Quy trình & cải tiến" (/Process) — R-003 / H3-04. Bố cục: tasks/H3-04-hub-team-process.md.</summary>
public class ProcessModel : PageModel
{
    private readonly ITeamInsightsService _insights;

    public ProcessModel(ITeamInsightsService insights) => _insights = insights;

    /// <summary>?cause=TK|AG|CK|NV|HT để lọc bảng vấn đề (không phân biệt hoa thường, đổi về chữ hoa); rỗng = tất cả; giá trị khác ⇒ đặt lại "".</summary>
    [BindProperty(SupportsGet = true)] public string Cause { get; set; } = "";

    public IReadOnlyList<StageAverage> StageAverages { get; private set; } = Array.Empty<StageAverage>();

    /// <summary>Insights.Issues đã lọc theo Cause (so khớp không phân biệt hoa thường), giữ thứ tự.</summary>
    public IReadOnlyList<TeamIssue> Issues { get; private set; } = Array.Empty<TeamIssue>();

    /// <summary>Đếm Insights.Issues (CHƯA lọc) theo Cause; đủ 5 khóa TeamModel.Causes (0 nếu không có); mã lạ bỏ qua.</summary>
    public IReadOnlyDictionary<string, int> IssueCounts { get; private set; } = new Dictionary<string, int>();

    /// <summary>Tổng số vấn đề chưa lọc.</summary>
    public int IssueTotal { get; private set; }

    public IReadOnlyList<Improvement> Improvements { get; private set; } = Array.Empty<Improvement>();

    /// <summary>Get() ném lỗi ⇒ "Không đọc được dữ liệu đánh giá: " + ex.Message; danh sách rỗng, IssueCounts đủ 5 khóa = 0.</summary>
    public string? Error { get; private set; }

    public void OnGet()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Độ dài thanh: avg / 5 · max, làm tròn 1 số lẻ, kẹp [0, max]; null ⇒ 0.</summary>
    public static double BarWidth(double? avg, double max = 300)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>≥ 4.5 ⇒ "b-ok"; ≥ 3.5 ⇒ "b-mid"; còn lại ⇒ "b-bad"; null ⇒ "b-none".</summary>
    public static string BarCss(double? avg)
        => throw new NotImplementedException(); // TODO(agent)
}
