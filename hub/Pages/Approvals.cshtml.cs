using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Cần anh duyệt" (/Approvals) — R-003 / H3-03. Bố cục: tasks/H3-03-hub-approvals.md mục "Bố cục giao diện".</summary>
[ValidateAntiForgeryToken]
public class ApprovalsModel : PageModel
{
    private readonly IApprovalService _approvals;

    public ApprovalsModel(IApprovalService approvals) => _approvals = approvals;

    /// <summary>Status == Pending, giữ thứ tự của GetAll().</summary>
    public IReadOnlyList<Approval> Pending { get; private set; } = Array.Empty<Approval>();

    /// <summary>Status != Pending, giữ thứ tự của GetAll().</summary>
    public IReadOnlyList<Approval> Decided { get; private set; } = Array.Empty<Approval>();

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet() => LoadAll();

    private void LoadAll()
    {
        var all = _approvals.GetAll();
        Pending = all.Where(a => a.Status == ApprovalStatus.Pending).ToList();
        Decided = all.Where(a => a.Status != ApprovalStatus.Pending).ToList();
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



}
