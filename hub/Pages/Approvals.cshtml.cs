using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Pages;

/// <summary>
/// Trang "Cần anh duyệt" (/Approvals) — R-014 UI-04. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.2.
/// Tab "Chờ anh": chỉ việc nhạy cảm (ApprovalPolicy.RequiresOwner). Tab "Claude tự quyết": nhật ký
/// claude-decisions.jsonl + đơn merge / cải tiến cũ còn treo. Tab "Đã quyết": đơn đã có quyết định.
/// </summary>
[ValidateAntiForgeryToken]
public class ApprovalsModel : PageModel
{
    private readonly IApprovalService _approvals;
    private readonly IClaudeDecisionLog? _decisions;

    public ApprovalsModel(IApprovalService approvals) => _approvals = approvals;

    [ActivatorUtilitiesConstructor]
    public ApprovalsModel(IApprovalService approvals, IClaudeDecisionLog decisions)
    {
        _approvals = approvals;
        _decisions = decisions;
    }

    /// <summary>Status == Pending, giữ thứ tự của GetAll().</summary>
    public IReadOnlyList<Approval> Pending { get; private set; } = Array.Empty<Approval>();

    /// <summary>Status != Pending, giữ thứ tự của GetAll().</summary>
    public IReadOnlyList<Approval> Decided { get; private set; } = Array.Empty<Approval>();

    /// <summary>Đơn chờ chủ dự án (bí mật, bảo mật, dữ liệu nhạy cảm, ra ngoài, quyết định); rủi ro cao trước.</summary>
    public IReadOnlyList<Approval> OwnerPending { get; private set; } = Array.Empty<Approval>();

    /// <summary>Đơn cũ loại merge / cải tiến còn treo: Claude tự xử lý, anh có thể đóng hộ.</summary>
    public IReadOnlyList<Approval> ClaudePending { get; private set; } = Array.Empty<Approval>();

    public IReadOnlyList<ClaudeDecision> Decisions { get; private set; } = Array.Empty<ClaudeDecision>();

    /// <summary>Tab đang mở: owner | claude | decided.</summary>
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet() => LoadAll();

    private void LoadAll()
    {
        var all = _approvals.GetAll();
        Pending = all.Where(a => a.Status == ApprovalStatus.Pending).ToList();
        Decided = all.Where(a => a.Status != ApprovalStatus.Pending).ToList();
        OwnerPending = Pending.Where(a => ApprovalPolicy.RequiresOwner(a.Kind))
            .OrderByDescending(a => a.Risk).ThenBy(a => a.RequestedAt).ToList();
        ClaudePending = Pending.Where(a => !ApprovalPolicy.RequiresOwner(a.Kind)).ToList();
        Decisions = _decisions?.List(200) ?? Array.Empty<ClaudeDecision>();
        Tab = Tab is "owner" or "claude" or "decided" ? Tab : "owner";
    }

    /// <summary>
    /// decision "approve" ⇒ Decide(id, true, note); "reject" ⇒ Decide(id, false, note); khác ⇒ ErrorMessage, không gọi Decide.
    /// Bắt ArgumentException / KeyNotFoundException / InvalidOperationException ⇒ ErrorMessage = ex.Message (không ném ra ngoài).
    /// Thành công ⇒ Message = "Đã duyệt A-001: &lt;Title&gt;" hoặc "Đã từ chối A-001: &lt;Title&gt;".
    /// Luôn trả RedirectToPage() (PRG).
    /// </summary>
    public IActionResult OnPostDecide(string id, string decision, string? note)
    {
        if (decision is not ("approve" or "reject"))
        {
            ErrorMessage = "Quyết định không hợp lệ.";
            return RedirectToPage();
        }
        try
        {
            var approve = decision == "approve";
            var a = _approvals.Decide(id, approve, note);
            Message = $"{(approve ? "Đã duyệt" : "Đã từ chối")} {a.Id}: {a.Title}";
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage();
    }

    /// <summary>"Không đồng ý — mở lại" một quyết định của Claude. Bắt buộc ghi lý do.</summary>
    public IActionResult OnPostReopen(string id, string? note)
    {
        if (_decisions == null)
        {
            ErrorMessage = "Chưa bật nhật ký Claude tự quyết.";
            return RedirectToPage(new { tab = "claude" });
        }
        try
        {
            var d = _decisions.Reopen(id, note ?? "");
            Message = $"Đã mở lại {d.Id}: {d.Title}. Claude sẽ xem lại ở phiên tới.";
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException or IOException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage(new { tab = "claude" });
    }

    public static (string Label, string Css) KindLabel(ApprovalKind kind) => kind switch
    {
        ApprovalKind.Secret => ("Bí mật", "err"),
        ApprovalKind.Security => ("Bảo mật", "fix"),
        ApprovalKind.SensitiveData => ("Dữ liệu nhạy cảm", "fix"),
        ApprovalKind.External => ("Ra ngoài", "fix"),
        ApprovalKind.Decision => ("Quyết định sản phẩm", "run"),
        ApprovalKind.Merge => ("Merge", "rev"),
        _ => ("Cải tiến quy trình", "n"),
    };

    public static (string Label, string Css) RiskLabel(ApprovalRisk risk) => risk switch
    {
        ApprovalRisk.High => ("Rủi ro cao", "err"),
        ApprovalRisk.Low => ("Rủi ro thấp", "ok"),
        _ => ("Rủi ro trung bình", "warn"),
    };

    /// <summary>Link nội bộ tới task nếu đủ dữ liệu; null nếu không.</summary>
    public static string? TaskHref(string? project, string? task) =>
        string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(task)
            ? null
            : $"/p/{Uri.EscapeDataString(project)}/t/{Uri.EscapeDataString(task)}";
}
