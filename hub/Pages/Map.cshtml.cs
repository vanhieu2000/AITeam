using System.ComponentModel.DataAnnotations;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Dòng nhập Map; Code/Actor phân biệt hoa thường, Note giữ nguyên văn, tối đa 500 đơn vị UTF-16.</summary>
public sealed class MapRoleInput
{
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Code { get; set; } = "";
    public string? Actor { get; set; } = "";
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Note { get; set; } = "";
}

/// <summary>Trang /Map dùng dịch vụ H7-01 đã đăng ký DI; không ghi file trực tiếp hay cấp quyền thực thi/duyệt.</summary>
public sealed class MapModel(IWorkflowRolesService service) : PageModel
{
    private static readonly (string Code, string Label)[] Steps =
    [
        ("receive", "Tiếp nhận yêu cầu"), ("idea", "Lên ý tưởng"), ("research", "Research"),
        ("plan", "Lên kế hoạch"), ("approve_plan", "Duyệt kế hoạch"), ("prep", "Chia việc / chuẩn bị task"),
        ("code", "Code"), ("gate", "Kiểm thử tự động (cổng KT)"), ("qc", "Test / QC"),
        ("accept", "Duyệt cuối (nghiệm thu)"), ("publish", "Publish git (merge + push)")
    ];
    private static readonly (string Val, string Text)[] StaticActors =
    [
        ("claude", "Claude"), ("codex", "Codex"), ("minh", "Minh"),
        ("lan", "Lan"), ("gate", "Cổng KT"), ("owner", "Chủ dự án")
    ];

    private static readonly HashSet<string> StandardCodesSet = new(Steps.Select(s => s.Code), StringComparer.Ordinal);
    private static readonly HashSet<string> ValidActorsSet = new(StaticActors.Select(a => a.Val), StringComparer.Ordinal);
    private static readonly Dictionary<string, string> CodeLabels = Steps.ToDictionary(s => s.Code, s => s.Label, StringComparer.Ordinal);

    public (string Val, string Text)[] ActorChoices => StaticActors;
    public string GetLabel(string? code) => !string.IsNullOrEmpty(code) && CodeLabels.TryGetValue(code, out var label) ? label : (code ?? "");

    [BindProperty] public List<MapRoleInput> Roles { get; set; } = [];
    [BindProperty] public string Revision { get; set; } = "";
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    /// <summary>
    /// Gọi GetRoles một lần, chép đủ 11 dòng và Revision, kể cả mặc định v4 khi cấu hình cũ thiếu roles.
    /// InvalidDataException: “Phân vai không hợp lệ.”; IOException: “Không đọc được cấu hình phân vai.”.
    /// Khi lỗi không dựng form có thể lưu; không ghi file, không hiện exception.Message hay dữ liệu gốc.
    /// </summary>
    public void OnGet()
    {
        try
        {
            var snapshot = service.GetRoles();
            Roles = snapshot.Roles.Select(r => new MapRoleInput { Code = r.Code, Actor = r.Actor, Note = r.Note }).ToList();
            Revision = snapshot.Revision;
        }
        catch (InvalidDataException) { Error = "Phân vai không hợp lệ."; Roles = []; Revision = ""; }
        catch (IOException) { Error = "Không đọc được cấu hình phân vai."; Roles = []; Revision = ""; }
    }

    /// <summary>
    /// POST /Map dùng antiforgery mặc định Razor. Yêu cầu đủ 11 mã duy nhất, sáu Actor của H7-01,
    /// Note tối đa 500 đơn vị UTF-16 và Revision không rỗng; Note bỏ trống chuyển thành chuỗi rỗng,
    /// không trim. ModelState sai hoặc dữ liệu sai: “Phân vai không hợp lệ.”, không gọi SaveRoles.
    /// Gọi SaveRoles một lần với revision từ POST, không đọc lại để thay revision. Thành công trả Page,
    /// hiện “Đã lưu phân vai.” và dùng snapshot mới. IOException: “Không lưu được cấu hình phân vai.”;
    /// InvalidDataException: “Phân vai không hợp lệ.”; InvalidOperationException:
    /// “Cấu hình đã thay đổi. Vui lòng tải lại.”. Lỗi giữ form/revision cũ, không báo thành công,
    /// không hiện exception.Message; mọi giá trị được Razor mã hóa.
    /// </summary>
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(Revision) || Roles == null || Roles.Count != 11)
        {
            Error = "Phân vai không hợp lệ.";
            return Page();
        }

        if (Request.HasFormContentType)
        {
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("Roles[", StringComparison.Ordinal))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(key, @"^Roles\[(\d+)\]\.(Code|Actor|Note)$");
                    if (!match.Success || int.Parse(match.Groups[1].Value) >= 11)
                    {
                        Error = "Phân vai không hợp lệ.";
                        return Page();
                    }
                }
                else if (key.StartsWith("Roles", StringComparison.Ordinal))
                {
                    Error = "Phân vai không hợp lệ.";
                    return Page();
                }
            }
        }

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        var roleList = new List<WorkflowRole>(11);
        foreach (var r in Roles)
        {
            if (r == null || string.IsNullOrEmpty(r.Code) || string.IsNullOrEmpty(r.Actor)) { Error = "Phân vai không hợp lệ."; return Page(); }
            r.Note ??= "";
            if (!StandardCodesSet.Contains(r.Code) || !seenCodes.Add(r.Code) || !ValidActorsSet.Contains(r.Actor) || r.Note.Length > 500)
            {
                Error = "Phân vai không hợp lệ.";
                return Page();
            }
            roleList.Add(new WorkflowRole(r.Code, r.Actor, r.Note));
        }

        try
        {
            var snapshot = service.SaveRoles(roleList, Revision);
            Message = "Đã lưu phân vai.";
            Revision = snapshot.Revision;
            Roles = snapshot.Roles.Select(r => new MapRoleInput { Code = r.Code, Actor = r.Actor, Note = r.Note }).ToList();
            return Page();
        }
        catch (InvalidOperationException) { Error = "Cấu hình đã thay đổi. Vui lòng tải lại."; return Page(); }
        catch (InvalidDataException) { Error = "Phân vai không hợp lệ."; return Page(); }
        catch (IOException) { Error = "Không lưu được cấu hình phân vai."; return Page(); }
    }
}
