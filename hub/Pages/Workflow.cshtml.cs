using AITeam.Core.AiServices;
using AITeam.Core.Lessons;
using AITeam.Core.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Một dòng bước gửi từ form /Workflow (thứ tự = thứ tự trong danh sách).</summary>
public sealed class WorkflowStepInput
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Owner { get; set; }
    public string? Fallback { get; set; }
    public string? Skills { get; set; }
    public string? Rules { get; set; }
    public int? MaxMinutes { get; set; }
    public int? MaxRounds { get; set; }
    public bool OwnerApproval { get; set; }
}

/// <summary>
/// Quy trình &amp; vai trò (/Workflow) — R-014 UI-07. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.5.
/// Đọc / ghi config/workflow.json qua IWorkflowConfigService (kiểm hợp lệ + chống ghi đè bằng Revision).
/// Luật chuẩn lấy từ ILessonService; người phụ trách gồm claude / owner / gate + dịch vụ AI trong danh bạ.
/// </summary>
[ValidateAntiForgeryToken]
public sealed class WorkflowModel(IWorkflowConfigService workflow, ILessonService lessons, IAiServiceRegistry registry) : PageModel
{
    private static readonly (string Id, string Name)[] FixedActors =
        [("claude", "Claude"), ("owner", "Chủ dự án"), ("gate", "Cổng kiểm tra")];

    [BindProperty(SupportsGet = true, Name = "type")] public string? TypeId { get; set; }

    public WorkflowConfig? Config { get; private set; }
    public TaskType? Current { get; private set; }
    public IReadOnlyList<(string Id, string Name)> Actors { get; private set; } = [];
    public IReadOnlyList<string> Skills { get; private set; } = [];
    public IReadOnlyList<Rule> ActiveRules { get; private set; } = [];
    public string? Error { get; private set; }
    public IReadOnlyList<string> SaveErrors { get; private set; } = [];

    [TempData] public string? Message { get; set; }

    public void OnGet() => Load();

    private void Load()
    {
        try
        {
            Config = workflow.Load();
            Current = Config.TaskTypes.FirstOrDefault(t => t.Id == TypeId) ?? Config.TaskTypes.FirstOrDefault();
            TypeId = Current?.Id;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Error = "Không đọc được config/workflow.json: " + ex.Message;
        }
        try { Skills = workflow.KnownSkills(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Skills = []; }
        try { ActiveRules = lessons.Rules(activeOnly: true); } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { ActiveRules = []; }
        IReadOnlyList<AiService> services;
        try { services = registry.List(includeDisabled: false); } catch (Exception ex) when (ex is IOException or InvalidOperationException) { services = []; }
        var used = Config?.TaskTypes.SelectMany(t => t.Steps).SelectMany(s => new[] { s.Owner, s.Fallback }).OfType<string>() ?? [];
        Actors = FixedActors.Concat(services.Select(s => (s.Id, s.Name)))
            .Concat(used.Select(u => (u, u)))
            .DistinctBy(a => a.Item1, StringComparer.Ordinal).ToList();
    }

    public IActionResult OnPostSave(string type, string revision, string? name, string? prefixes, List<WorkflowStepInput>? steps)
    {
        TypeId = type;
        Load();
        if (Config == null || Current == null || Current.Id != type)
        {
            SaveErrors = [Error ?? "Không tìm thấy loại việc " + type + "."];
            return Page();
        }
        var newSteps = (steps ?? []).Select(ToStep).ToList();
        var newType = Current with
        {
            Name = (name ?? "").Trim(),
            CodePrefixes = Split(prefixes),
            Steps = newSteps,
        };
        var newConfig = Config with { TaskTypes = Config.TaskTypes.Select(t => t.Id == type ? newType : t).ToList() };
        try
        {
            workflow.Save(newConfig, revision ?? "");
            Message = $"Đã lưu quy trình \"{newType.Name}\" ({newSteps.Count} bước). Nhớ commit config/workflow.json để máy kia nhận.";
            return RedirectToPage(new { type });
        }
        catch (ArgumentException)
        {
            SaveErrors = workflow.Validate(newConfig).Errors;
            if (SaveErrors.Count == 0) SaveErrors = ["Cấu hình không hợp lệ."];
        }
        catch (InvalidOperationException)
        {
            SaveErrors = ["File quy trình vừa bị thay đổi ở nơi khác (máy kia / git pull). Trang đã tải bản mới — anh sửa lại rồi lưu."];
            return Page();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveErrors = ["Không ghi được file: " + ex.Message];
        }
        // Giữ bản anh đang sửa để không mất công.
        Current = newType;
        return Page();
    }

    private static WorkflowStep ToStep(WorkflowStepInput s) => new(
        (s.Code ?? "").Trim(),
        (s.Name ?? "").Trim(),
        (s.Owner ?? "").Trim(),
        string.IsNullOrWhiteSpace(s.Fallback) ? null : s.Fallback.Trim(),
        Split(s.Skills),
        Split(s.Rules),
        s.MaxMinutes,
        s.MaxRounds,
        s.OwnerApproval);

    public static IReadOnlyList<string> Split(string? s) =>
        (s ?? "").Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Nhóm màu cho sơ đồ: claude | owner | gate | ai.</summary>
    public static string ActorKind(string actor) => actor switch
    {
        "claude" => "claude",
        "owner" => "owner",
        "gate" => "gate",
        _ => "ai",
    };

    public string ActorName(string? id) =>
        id == null ? "—" : Actors.FirstOrDefault(a => a.Id == id).Name ?? id;
}
