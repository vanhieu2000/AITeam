using AITeam.Core.Lessons;
using AITeam.Core.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>
/// Mẹo &amp; bài học (/Reports/Lessons) — R-014 UI-06. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.4 + 4.5 (luật chuẩn).
/// Nháp ⇒ "Nâng thành luật chuẩn" (Claude quyết) hoặc "Bỏ". Luật đang hiệu lực tự chèn vào prompt khi giao task đúng loại / bước.
/// Hub chỉ ghi file lessons/ và rules/; commit lên git do Claude làm.
/// </summary>
[ValidateAntiForgeryToken]
public sealed class LessonsModel(ILessonService lessons, IWorkflowConfigService workflow) : PageModel
{
    public static readonly string[] Views = ["draft", "promoted", "dropped", "rules"];

    [BindProperty(SupportsGet = true)] public string? View { get; set; }
    [BindProperty(SupportsGet = true)] public string? Kind { get; set; }
    [BindProperty(SupportsGet = true, Name = "request")] public string? RequestFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? Ai { get; set; }
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    /// <summary>R-015: mục đang xem bên phải (LS-xxx / L-xxx); không có / lạ ⇒ mục đầu danh sách.</summary>
    [BindProperty(SupportsGet = true)] public string? Sel { get; set; }

    public string SelLink(string id) => Link() + (Link().Contains('?') ? "&" : "?") + "sel=" + Uri.EscapeDataString(id);

    public IReadOnlyList<Lesson> Items { get; private set; } = [];
    public IReadOnlyList<Rule> RuleItems { get; private set; } = [];
    public IReadOnlyDictionary<string, int> Counts { get; private set; } = new Dictionary<string, int>();
    public IReadOnlyList<(string Id, string Name)> TaskTypes { get; private set; } = [];
    public IReadOnlyList<(string Code, string Name)> Steps { get; private set; } = [];
    public IReadOnlyList<string> AiOptions { get; private set; } = [];
    public string? Error { get; private set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        View = Views.Contains(View) ? View : "draft";
        Kind = Kind is "meo" or "cai-tien" ? Kind : null;
        RequestFilter = Clean(RequestFilter);
        Ai = Clean(Ai);
        Query = Clean(Query);
        try
        {
            var all = lessons.Lessons();
            var rules = lessons.Rules();
            Counts = new Dictionary<string, int>
            {
                ["draft"] = all.Count(l => l.Status == LessonStatus.Draft),
                ["promoted"] = all.Count(l => l.Status == LessonStatus.Promoted),
                ["dropped"] = all.Count(l => l.Status == LessonStatus.Dropped),
                ["rules"] = rules.Count(r => r.Status == RuleStatus.Active),
            };
            AiOptions = all.Select(l => l.Ai).OfType<string>().Concat(rules.SelectMany(r => r.Ais))
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();

            var status = View switch { "promoted" => LessonStatus.Promoted, "dropped" => LessonStatus.Dropped, _ => LessonStatus.Draft };
            Items = all.Where(l => l.Status == status)
                .Where(l => Kind == null || l.Kind == Kind)
                .Where(l => RequestFilter == null || string.Equals(l.RequestId, RequestFilter, StringComparison.OrdinalIgnoreCase))
                .Where(l => Ai == null || string.Equals(l.Ai, Ai, StringComparison.OrdinalIgnoreCase))
                .Where(l => Query == null || l.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) || l.Body.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.CreatedAt).ToList();
            RuleItems = rules
                .Where(r => Ai == null || r.Ais.Count == 0 || r.Ais.Contains(Ai, StringComparer.OrdinalIgnoreCase))
                .Where(r => Query == null || r.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) || r.Body.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAt).ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Error = "Không đọc được bài học / luật: " + ex.Message;
        }
        LoadWorkflow();
    }

    private void LoadWorkflow()
    {
        try
        {
            var cfg = workflow.Load();
            TaskTypes = cfg.TaskTypes.Select(t => (t.Id, t.Name)).ToList();
            Steps = cfg.TaskTypes.SelectMany(t => t.Steps).GroupBy(s => s.Code, StringComparer.Ordinal)
                .Select(g => (g.Key, g.First().Name)).ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            TaskTypes = [];
            Steps = [];
        }
    }

    public IActionResult OnPostAdd(string? title, string? body, string kind)
    {
        return Run(() =>
        {
            var l = lessons.AddDraft(title ?? "", body ?? "", kind, null, null, null, null);
            return $"Đã ghi bản nháp {l.Id}.";
        }, "draft");
    }

    public IActionResult OnPostPromote(string id, string? title, string? body, string[]? types, string[]? steps, string? ais)
    {
        return Run(() =>
        {
            var aiList = (ais ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
            var r = lessons.Promote(id, title ?? "", body ?? "", types ?? [], steps ?? [], aiList);
            return $"Đã nâng {id} thành luật {r.Id}. Luật tự chèn vào prompt khi giao việc đúng phạm vi.";
        }, "rules");
    }

    public IActionResult OnPostDrop(string id) => Run(() => { lessons.Drop(id); return $"Đã bỏ {id}."; }, "draft");

    public IActionResult OnPostRetire(string id) => Run(() => { lessons.Retire(id); return $"Đã ngừng luật {id}."; }, "rules");

    private IActionResult Run(Func<string> action, string view)
    {
        try
        {
            Message = action();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage(new { view });
    }

    private static string? Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length > 80 || t.Any(char.IsControl) ? null : t;
    }

    public string Link(string? view = null, string? kind = null, bool keep = true)
    {
        var q = new List<string>();
        var v = view ?? View;
        if (!string.IsNullOrEmpty(v) && v != "draft") q.Add("view=" + v);
        var k = kind ?? (keep ? Kind : null);
        if (k != null && k != "*") q.Add("kind=" + k);
        if (keep && RequestFilter != null) q.Add("request=" + Uri.EscapeDataString(RequestFilter));
        if (keep && Ai != null) q.Add("ai=" + Uri.EscapeDataString(Ai));
        if (keep && Query != null) q.Add("q=" + Uri.EscapeDataString(Query));
        return "/Reports/Lessons" + (q.Count == 0 ? "" : "?" + string.Join("&", q));
    }

    public static string KindText(string kind) => kind == "cai-tien" ? "Đề xuất cải tiến" : "Mẹo";
}
