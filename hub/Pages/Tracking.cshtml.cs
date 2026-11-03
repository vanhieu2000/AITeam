using System.Text.RegularExpressions;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Một task trên trang Theo dõi việc: thẻ bảng việc + thông tin đọc từ file task (độ khó, yêu cầu).</summary>
public sealed record TrackItem(OfficeWorkCard Card, string Code, string Person, string PersonName, string Difficulty, string? Request, string Url);

/// <summary>Một nhóm trong view "Theo danh sách": theo yêu cầu R-xxx, không có ⇒ theo dự án.</summary>
public sealed record TrackGroup(string Key, string Title, string? Url, IReadOnlyList<TrackItem> Items);

/// <summary>
/// R-014 · UI-03 (Claude): Theo dõi việc — cùng dữ liệu bảng việc, 3 cách xem (theo người / theo danh sách / Kanban).
/// Bộ lọc nằm trên địa chỉ trang (gửi link được). Chỉ đọc, không ghi dữ liệu.
/// </summary>
public class TrackingModel(WorkOverviewService work) : PageModel
{
    public static readonly string[] Views = ["person", "list", "kanban"];
    public static readonly string[] States = ["all", "open", "running", "review", "fix", "done"];
    private static readonly Regex DifficultyLine = new(@"\*\*Độ khó:\*\*\s*([SML])\b", RegexOptions.CultureInvariant);
    private static readonly Regex RequestLine = new(@"\*\*Yêu cầu:\*\*\s*(R-\d{3,4})\b", RegexOptions.CultureInvariant);
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "Minh (Antigravity)", ["B"] = "Lan (Antigravity)", ["C"] = "Codex", ["O"] = "OpenCode", ["claude"] = "Claude"
    };

    [BindProperty(SupportsGet = true)] public string? View { get; set; }
    [BindProperty(SupportsGet = true)] public string? Project { get; set; }
    [BindProperty(SupportsGet = true)] public string? State { get; set; }
    [BindProperty(SupportsGet = true)] public string? Difficulty { get; set; }
    [BindProperty(SupportsGet = true)] public string? Person { get; set; }
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }

    public IReadOnlyList<TrackItem> Items { get; private set; } = [];
    public IReadOnlyList<string> Projects { get; private set; } = [];
    public IReadOnlyList<(string Code, string Name)> People { get; private set; } = [];
    public IReadOnlyList<string> Warnings { get; private set; } = [];
    public int TotalBeforeFilter { get; private set; }
    public DateTime DoneSince { get; private set; }

    public void OnGet()
    {
        View = Views.Contains(View) ? View : "person";
        State = States.Contains(State) ? State : "all";
        Difficulty = Difficulty is "S" or "M" or "L" ? Difficulty : null;
        Query = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim()[..Math.Min(Query.Trim().Length, 60)];
        DoneSince = DateTime.Today.AddDays(State == "done" ? -30 : -7);

        var snapshot = work.Read();
        Warnings = snapshot.Warnings;
        Projects = snapshot.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        if (Project != null && !Projects.Contains(Project, StringComparer.OrdinalIgnoreCase)) Project = null;

        var all = new List<TrackItem>();
        foreach (var card in snapshot.Tasks.Where(c => c.Status != OfficeTaskStatus.Cancelled))
        {
            var info = snapshot.Projects.FirstOrDefault(p => p.Name.Equals(card.Task.Project, StringComparison.OrdinalIgnoreCase));
            string? text = null;
            if (info != null && !string.IsNullOrEmpty(card.Task.FileName))
            {
                try { text = HubMetricReader.ReadText(info.Path, "tasks/" + card.Task.FileName, 256 * 1024); }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { }
            }
            var person = string.IsNullOrWhiteSpace(card.Task.Agent) ? "?" : card.Task.Agent.Trim();
            all.Add(new TrackItem(card, HubMetricReader.TaskCode(card.Task.Id), person,
                Names.GetValueOrDefault(person) ?? "Agent " + person,
                text == null ? "?" : DifficultyLine.Match(text) is { Success: true } d ? d.Groups[1].Value : "?",
                text == null ? null : RequestLine.Match(text) is { Success: true } r ? r.Groups[1].Value : null,
                $"/p/{Uri.EscapeDataString(card.Task.Project)}/t/{Uri.EscapeDataString(card.Task.Id)}"));
        }
        TotalBeforeFilter = all.Count;
        People = all.Select(i => (i.Person, i.PersonName)).Distinct().OrderBy(p => p.PersonName, StringComparer.OrdinalIgnoreCase).ToList();
        if (Person != null && !People.Any(p => p.Code.Equals(Person, StringComparison.OrdinalIgnoreCase))) Person = null;

        Items = all.Where(Matches).OrderByDescending(i => i.Card.UpdatedAt ?? DateTimeOffset.MinValue).ToList();
    }

    private bool Matches(TrackItem i)
    {
        if (Project != null && !i.Card.Task.Project.Equals(Project, StringComparison.OrdinalIgnoreCase)) return false;
        if (Person != null && !i.Person.Equals(Person, StringComparison.OrdinalIgnoreCase)) return false;
        if (Difficulty != null && i.Difficulty != Difficulty) return false;
        if (Query != null && !(i.Code.Contains(Query, StringComparison.OrdinalIgnoreCase) || i.Card.Task.Title.Contains(Query, StringComparison.OrdinalIgnoreCase))) return false;
        // Việc đã xong chỉ giữ trong cửa sổ gần đây để màn hình không bị task cũ lấn át.
        if (i.Card.Status == OfficeTaskStatus.Merged && i.Card.MergedAt is { } at && at < DoneSince) return false;
        return State switch
        {
            "open" => i.Card.Status != OfficeTaskStatus.Merged,
            "running" => Column(i.Card.Status) == "running",
            "review" => Column(i.Card.Status) == "review",
            "fix" => Column(i.Card.Status) == "fix",
            "done" => Column(i.Card.Status) == "done",
            _ => true
        };
    }

    /// <summary>Gộp 10 trạng thái thành 5 cột Kanban.</summary>
    public static string Column(OfficeTaskStatus s) => s switch
    {
        OfficeTaskStatus.Running or OfficeTaskStatus.Gate => "running",
        OfficeTaskStatus.Review => "review",
        OfficeTaskStatus.NeedsFix or OfficeTaskStatus.Blocked or OfficeTaskStatus.Paused => "fix",
        OfficeTaskStatus.Merged => "done",
        _ => "todo"
    };

    public static readonly (string Key, string Label)[] Columns =
        [("todo", "Chờ làm"), ("running", "Đang làm"), ("review", "Chờ review"), ("fix", "Cần sửa / chặn"), ("done", "Xong")];

    public static string PillClass(OfficeTaskStatus s) => Column(s) switch
    {
        "running" => "run", "review" => "rev", "fix" => "fix", "done" => "ok", _ => "n"
    };

    public IReadOnlyList<TrackGroup> Groups() => Items
        .GroupBy(i => i.Request ?? "~" + i.Card.Task.Project)
        .Select(g => g.Key.StartsWith('~')
            ? new TrackGroup(g.Key, "Dự án " + g.Key[1..] + " · không gắn yêu cầu", $"/p/{Uri.EscapeDataString(g.Key[1..])}", g.ToList())
            : new TrackGroup(g.Key, g.Key, Url.Page("/Request", new { id = g.Key }), g.ToList()))
        .OrderBy(g => g.Key.StartsWith('~')).ThenByDescending(g => g.Key, StringComparer.Ordinal)
        .ToList();

    /// <summary>Địa chỉ trang với một tham số đổi đi, giữ các bộ lọc còn lại.</summary>
    public string With(string key, string? value)
    {
        var q = new Dictionary<string, string?>
        {
            ["view"] = View, ["project"] = Project, ["state"] = State == "all" ? null : State,
            ["difficulty"] = Difficulty, ["person"] = Person, ["q"] = Query
        };
        q[key] = value;
        var parts = q.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key + "=" + Uri.EscapeDataString(p.Value!));
        return "/Tracking" + (parts.Any() ? "?" + string.Join("&", parts) : "");
    }
}
