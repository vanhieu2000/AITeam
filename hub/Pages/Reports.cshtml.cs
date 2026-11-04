using System.Globalization;
using System.Text;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>
/// Báo cáo &amp; thống kê (/Reports) — R-014 UI-05. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.4.
/// Chỉ đọc qua IReportService. Bộ lọc nằm trên URL để chia sẻ được: range (7 | 30 | 90 | custom), from, to,
/// project, person, difficulty, tab (overview | people | projects | errors), sort, dir.
/// </summary>
public sealed class ReportsModel(IReportService reports) : PageModel
{
    public static readonly string[] Tabs = ["overview", "people", "projects", "errors"];
    public static readonly string[] SortKeys = ["score", "tasks", "pass", "budget", "ontime", "ag", "minutes", "name"];

    [BindProperty(SupportsGet = true)] public string? Range { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }
    [BindProperty(SupportsGet = true)] public string? Project { get; set; }
    [BindProperty(SupportsGet = true)] public string? Person { get; set; }
    [BindProperty(SupportsGet = true)] public string? Difficulty { get; set; }
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public string? Dir { get; set; }

    /// <summary>Đồng hồ (test thay được; không nhận từ URL).</summary>
    public Func<DateTimeOffset> Clock { get; init; } = () => DateTimeOffset.Now;

    public ReportData? Data { get; private set; }
    public string? Error { get; private set; }

    /// <summary>(mã, tên hiển thị) cho ô chọn người / AI — lấy từ kỳ không lọc người để không mất lựa chọn.</summary>
    public IReadOnlyList<(string Key, string Display)> PeopleOptions { get; private set; } = [];

    public IReadOnlyList<ScoreRow> SortedScores { get; private set; } = [];

    public void OnGet()
    {
        Normalize(Clock());
        try
        {
            Data = reports.Query(BuildFilter());
            var optionSource = string.IsNullOrWhiteSpace(Person) ? Data : reports.Query(BuildFilter() with { Person = null });
            PeopleOptions = optionSource.Scores.Select(s => (s.Person, s.Display))
                .DistinctBy(p => p.Person, StringComparer.OrdinalIgnoreCase).ToList();
            SortedScores = SortScores(Data.Scores, Sort!, Dir == "asc");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            Error = "Không đọc được số liệu báo cáo: " + ex.Message;
        }
    }

    /// <summary>Xuất CSV: table = scores | weeks | errors | tasks. Lỗi ⇒ 400 có chữ, không ném.</summary>
    public IActionResult OnGetCsv(string table)
    {
        Normalize(Clock());
        try
        {
            var csv = reports.ExportCsv(BuildFilter(), table);
            var name = $"bao-cao-{table}-{From:yyyyMMdd}-{To:yyyyMMdd}.csv";
            return File(new UTF8Encoding(false).GetBytes(csv), "text/csv; charset=utf-8", name);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return StatusCode(500, "Không xuất được CSV: " + ex.Message);
        }
    }

    private void Normalize(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        Range = Range is "7" or "30" or "90" or "custom" ? Range : "30";
        if (Range != "custom" || From is null || To is null)
        {
            if (Range == "custom") Range = "30";
            To = today;
            From = today.AddDays(-(int.Parse(Range, CultureInfo.InvariantCulture) - 1));
        }
        if (From > To) (From, To) = (To, From);
        if (To!.Value.DayNumber - From!.Value.DayNumber > 366) From = To.Value.AddDays(-366);
        Project = Clean(Project);
        Person = Clean(Person);
        Difficulty = Difficulty is "S" or "M" or "L" or "chưa rõ" ? Difficulty : null;
        Tab = Tabs.Contains(Tab) ? Tab : "overview";
        Sort = SortKeys.Contains(Sort) ? Sort : "score";
        Dir = Dir == "asc" ? "asc" : "desc";
    }

    private static string? Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length > 80 || t.Any(char.IsControl) ? null : t;
    }

    private ReportFilter BuildFilter() => new(From!.Value, To!.Value, Project, Person, Difficulty);

    public static IReadOnlyList<ScoreRow> SortScores(IReadOnlyList<ScoreRow> rows, string sort, bool asc)
    {
        Func<ScoreRow, double> key = sort switch
        {
            "tasks" => r => r.Tasks,
            "pass" => r => r.FirstPassPct ?? -1,
            "budget" => r => r.InBudgetPct ?? -1,
            "ontime" => r => r.OnTimePct ?? -1,
            "ag" => r => r.AgErrors,
            "minutes" => r => r.AvgMinutes ?? -1,
            _ => r => r.Score,
        };
        if (sort == "name")
            return (asc ? rows.OrderBy(r => r.Display, StringComparer.OrdinalIgnoreCase) : rows.OrderByDescending(r => r.Display, StringComparer.OrdinalIgnoreCase)).ToList();
        var ordered = asc ? rows.OrderBy(key) : rows.OrderByDescending(key);
        return ordered.ThenBy(r => r.Display, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>URL giữ nguyên bộ lọc hiện tại, thay các khóa được truyền (null = bỏ).</summary>
    public string Link(params (string Key, string? Value)[] changes)
    {
        var q = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["range"] = Range,
            ["from"] = Range == "custom" ? From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            ["to"] = Range == "custom" ? To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            ["project"] = Project,
            ["person"] = Person,
            ["difficulty"] = Difficulty,
            ["tab"] = Tab == "overview" ? null : Tab,
            ["sort"] = Sort == "score" ? null : Sort,
            ["dir"] = Dir == "desc" ? null : Dir,
        };
        foreach (var (k, v) in changes) q[k] = v;
        var parts = q.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key + "=" + Uri.EscapeDataString(p.Value!));
        var s = string.Join("&", parts);
        return "/Reports" + (s.Length == 0 ? "" : "?" + s);
    }

    public string CsvUrl(string table) => Link(("handler", "Csv"), ("table", table), ("tab", null), ("sort", null), ("dir", null));

    public static string Num(double? v, string unit = "")
    {
        if (v is not { } x) return "chưa đo";
        var s = x.ToString(Math.Abs(x) >= 100 ? "0" : "0.#", CultureInfo.InvariantCulture);
        return unit.Length == 0 ? s : unit == "%" ? s + "%" : s + " " + unit;
    }

    /// <summary>Chênh lệch so với kỳ trước: (chữ, lớp css). Lớp "up" / "down" kèm chữ ▲ ▼ (không chỉ dùng màu).</summary>
    public static (string Text, string Css) Delta(ReportKpi k)
    {
        if (k.Value is not { } v || k.Previous is not { } p) return ("chưa có kỳ trước", "flat");
        var d = v - p;
        if (Math.Abs(d) < 0.05) return ("bằng kỳ trước", "flat");
        var lowerIsBetter = k.Key == "errors_tk";
        var good = lowerIsBetter ? d < 0 : d > 0;
        var arrow = d > 0 ? "▲" : "▼";
        var unit = k.Unit == "%" ? " điểm %" : " " + k.Unit;
        return ($"{arrow} {Math.Abs(d).ToString("0.#", CultureInfo.InvariantCulture)}{unit} so với kỳ trước", good ? "up" : "down");
    }
}
