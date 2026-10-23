using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Đội ngũ" (/Team) — R-003 / H3-04. Bố cục: tasks/H3-04-hub-team-process.md.</summary>
public class TeamModel : PageModel
{
    private readonly ITeamInsightsService _insights;

    public TeamModel(ITeamInsightsService insights) => _insights = insights;

    /// <summary>Mã nguyên nhân theo thứ tự hiển thị.</summary>
    public static readonly IReadOnlyList<string> Causes = new[] { "TK", "AG", "CK", "NV", "HT" };

    public IReadOnlyList<StaffScore> Staff { get; private set; } = Array.Empty<StaffScore>();

    /// <summary>Task có ít nhất 1 dòng Đánh giá quy trình, giữ thứ tự của Insights.Tasks, tối đa 15.</summary>
    public IReadOnlyList<TaskInsight> HeatRows { get; private set; } = Array.Empty<TaskInsight>();

    /// <summary>Đếm ReviewIssue theo Cause trên mọi task có Request != null. Luôn đủ 5 khóa của Causes (0 nếu không có); mã lạ bỏ qua.</summary>
    public IReadOnlyDictionary<string, int> CauseCounts { get; private set; } = new Dictionary<string, int>();

    /// <summary>Get() ném lỗi ⇒ "Không đọc được dữ liệu đánh giá: " + ex.Message; các danh sách rỗng, CauseCounts đủ 5 khóa = 0.</summary>
    public string? Error { get; private set; }

    public void OnGet()
    {
        var counts = Causes.ToDictionary(c => c, _ => 0);
        try
        {
            var data = _insights.Get();
            Staff = data.Staff;
            HeatRows = data.Tasks.Where(t => t.Stages.Count > 0).Take(15).ToList();
            foreach (var task in data.Tasks.Where(t => t.Request != null))
            {
                foreach (var issue in task.Issues)
                {
                    if (counts.ContainsKey(issue.Cause))
                        counts[issue.Cause]++;
                }
            }
            CauseCounts = counts;
            Error = null;
        }
        catch (Exception ex)
        {
            Error = "Không đọc được dữ liệu đánh giá: " + ex.Message;
            Staff = Array.Empty<StaffScore>();
            HeatRows = Array.Empty<TaskInsight>();
            CauseCounts = counts;
        }
    }

    /// <summary>5 ⇒ "h5" … 1 ⇒ "h1"; null hoặc ngoài 1–5 ⇒ "h0".</summary>
    public static string HeatCss(int? score)
        => score is >= 1 and <= 5 ? $"h{score.Value}" : "h0";

    /// <summary>{"giao diện":2,"test":1} ⇒ "giao diện 2 · test 1" (số lớn trước, bằng nhau theo tên tăng dần; Kind rỗng ⇒ "khác"). Rỗng ⇒ "—".</summary>
    public static string AgErrorsText(IReadOnlyDictionary<string, int> byKind)
    {
        if (byKind == null || byKind.Count == 0) return "—";
        return string.Join(" · ", byKind
            .Select(kv => (Kind: string.IsNullOrWhiteSpace(kv.Key) ? "khác" : kv.Key, Count: kv.Value))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .Select(x => $"{x.Kind} {x.Count}"));
    }

    /// <summary>TK "Task / thiết kế (Claude)" · AG "Nhân viên (agent)" · CK "Công cụ / cổng kiểm tra" · NV "Nghiệp vụ / chủ dự án" · HT "Hạ tầng / provider"; khác ⇒ chính mã đó.</summary>
    public static string CauseLabel(string code) => code switch
    {
        "TK" => "Task / thiết kế (Claude)",
        "AG" => "Nhân viên (agent)",
        "CK" => "Công cụ / cổng kiểm tra",
        "NV" => "Nghiệp vụ / chủ dự án",
        "HT" => "Hạ tầng / provider",
        _ => code
    };

    /// <summary>0.5 ⇒ "50%"; 0.666 ⇒ "67%" (làm tròn); null ⇒ "—".</summary>
    public static string Pct(double? rate)
        => rate.HasValue ? $"{Math.Round(rate.Value * 100.0, MidpointRounding.AwayFromZero):0}%" : "—";

    /// <summary>Điểm của giai đoạn index (1..7) trong task; không có dòng ⇒ null.</summary>
    public static int? StageScoreOf(TaskInsight task, int index)
        => task.Stages.FirstOrDefault(s => s.Index == index)?.Score;
}
