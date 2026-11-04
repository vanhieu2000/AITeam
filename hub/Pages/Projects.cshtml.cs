using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>R-015 (Claude): quản lý dự án — danh sách | chi tiết, thêm / sửa / tạm dừng / lưu trữ (lõi F1-01).
/// Mockup docs/design/R-015-mockup.html màn "Dự án". Không có xóa: lưu trữ = ẩn khỏi giao việc, repo còn nguyên.</summary>
[ValidateAntiForgeryToken]
public sealed class ProjectsModel(ITeamConfigService config, IProjectAdminService? admin = null,
    WorkOverviewService? work = null, IRequestProgressService? requests = null) : PageModel
{
    public static readonly IReadOnlyList<(string Key, string Label)> Filters = [("active", "Đang làm"), ("paused", "Tạm dừng"), ("archived", "Lưu trữ"), ("all", "Tất cả")];
    public static readonly IReadOnlyList<(string Key, string Label)> AiChoices = [("", "Theo định tuyến"), ("codex", "Codex"), ("antigravity", "Antigravity"), ("opencode", "OpenCode"), ("claude", "Claude")];

    /// <summary>Mọi dự án theo thứ tự projects.json (không lọc).</summary>
    public IReadOnlyList<ProjectProfile> All { get; private set; } = [];
    public IReadOnlyList<ProjectProfile> Visible { get; private set; } = [];
    public ProjectProfile? Selected { get; private set; }
    public bool CanEdit => admin != null;

    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? F { get; set; }
    [BindProperty(SupportsGet = true)] public bool Edit { get; set; }

    public string Filter => Filters.Any(x => x.Key == F) ? F! : "active";

    /// <summary>Việc đang mở của dự án đang chọn (null = không có dịch vụ).</summary>
    public IReadOnlyList<OfficeWorkCard>? Tasks { get; private set; }
    public IReadOnlyList<RequestProgress>? OpenRequests { get; private set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public static string StatusKey(ProjectStatus s) => s switch { ProjectStatus.Paused => "paused", ProjectStatus.Archived => "archived", _ => "active" };
    public static (string Css, string Text) StatusPill(ProjectStatus s) => s switch
    {
        ProjectStatus.Paused => ("fix", "Tạm dừng"),
        ProjectStatus.Archived => ("n", "Lưu trữ"),
        _ => ("ok", "Đang làm"),
    };

    public int CountIn(string f) => f == "all" ? All.Count : All.Count(p => StatusKey(p.Status) == f);

    public string Link(string? name = null, string? f = null, bool edit = false)
    {
        var q = new List<string>();
        if (name != null) q.Add("name=" + Uri.EscapeDataString(name));
        var ff = f ?? Filter;
        if (ff != "active") q.Add("f=" + ff);
        if (edit) q.Add("edit=true");
        return "/Projects" + (q.Count == 0 ? "" : "?" + string.Join("&", q));
    }

    /// <summary>Không có dịch vụ quản lý (test cũ) ⇒ dựng hồ sơ từ GetProjects(). Lỗi cấu hình không bị che bằng danh sách rỗng.</summary>
    public void OnGet()
    {
        All = admin != null ? admin.List() : config.GetProjects().Select(FromInfo).ToList();
        Visible = Filter == "all" ? All : All.Where(p => StatusKey(p.Status) == Filter).ToList();
        Selected = (Name == null ? null : All.FirstOrDefault(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase)))
            ?? Visible.FirstOrDefault();
        if (Selected == null) return;
        try
        {
            if (work != null)
                Tasks = work.Read().Tasks.Where(t => string.Equals(t.Task.Project, Selected.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (requests != null)
                OpenRequests = requests.GetAll().Where(r => r.State == RequestState.Open
                    && r.Tasks.Any(t => string.Equals(t.Project, Selected.Name, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ErrorMessage ??= "Không đọc được việc của dự án: " + ex.Message;
        }
    }

    private static ProjectProfile FromInfo(ProjectInfo p) => new(p.Name, "", ProjectStatus.Active, "main", p.Repo, p.Path, p.ExistsLocally,
        p.Stack, p.Build, p.Test, "", "", p.Areas);

    public IActionResult OnPostSave(string name, string? description, string? branch, string? stack, string? build, string? test,
        string? tryCmd, string? defaultAi, string? localPath, string? areaA, string? areaB)
    {
        if (admin == null) return NotFound();
        var areas = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(areaA)) areas["A"] = areaA.Trim();
        if (!string.IsNullOrWhiteSpace(areaB)) areas["B"] = areaB.Trim();
        var r = admin.Update(name, new ProjectEdit((description ?? "").Trim(), (branch ?? "main").Trim(), (stack ?? "").Trim(), (build ?? "").Trim(),
            (test ?? "").Trim(), (tryCmd ?? "").Trim(), defaultAi ?? "", areas, (localPath ?? "").Trim()));
        if (r.Ok) { Message = $"Đã lưu thông tin {name}."; return Redirect(Link(name)); }
        ErrorMessage = r.Error;
        return Redirect(Link(name, edit: true));
    }

    public IActionResult OnPostStatus(string name, string status)
    {
        if (admin == null) return NotFound();
        ProjectStatus? s = status switch { "active" => ProjectStatus.Active, "paused" => ProjectStatus.Paused, "archived" => ProjectStatus.Archived, _ => null };
        if (s == null) { ErrorMessage = "Trạng thái không hợp lệ."; return Redirect(Link(name)); }
        var r = admin.SetStatus(name, s.Value);
        if (r.Ok) Message = $"{name}: {StatusPill(s.Value).Text}.";
        else ErrorMessage = r.Error;
        return Redirect(Link(name, "all"));
    }

    public IActionResult OnPostAdd(string? newName, string? mode, string? repo, string? path)
    {
        if (admin == null) return NotFound();
        ProjectAddMode? m = mode switch { "new" => ProjectAddMode.New, "github" => ProjectAddMode.GitHub, "folder" => ProjectAddMode.Folder, _ => null };
        if (m == null || string.IsNullOrWhiteSpace(newName)) { ErrorMessage = "Nhập tên dự án và chọn cách thêm."; return Redirect(Link()); }
        var name = newName.Trim();
        var r = admin.Add(new ProjectAdd(name, m.Value,
            m == ProjectAddMode.GitHub ? repo?.Trim() : null,
            m == ProjectAddMode.Folder ? path?.Trim() : null));
        if (r.Ok) { Message = $"Đã thêm dự án {name}. Điền lệnh build / test ở thẻ Thông tin."; return Redirect(Link(name, "all", edit: true)); }
        ErrorMessage = r.Error;
        return Redirect(Link());
    }

    public static bool SafeName(string? s) => s != null && Regex.IsMatch(s, @"^[A-Za-z][A-Za-z0-9._-]{0,63}$");
}
