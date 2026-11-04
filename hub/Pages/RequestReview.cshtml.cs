using System.Text.RegularExpressions;
using AITeam.Core.Lessons;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>
/// Xem lại từng bước một yêu cầu (/r/R-xxx/review) — R-014 UI-06. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.4.
/// Mỗi bước có "Ghi mẹo" / "Đề xuất cải tiến" ⇒ bản nháp ở Mẹo &amp; bài học, gắn nguồn yêu cầu / task / bước.
/// </summary>
[ValidateAntiForgeryToken]
public sealed partial class RequestReviewModel(IReportService reports, ILessonService lessons) : PageModel
{
    [GeneratedRegex(@"\AR-\d{3,4}\z")]
    private static partial Regex RequestIdRx();

    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9-]*\z")]
    private static partial Regex TaskIdRx();

    public string Id { get; private set; } = "";
    public RequestTimeline? Timeline { get; private set; }
    public string? Error { get; private set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string id)
    {
        if (!RequestIdRx().IsMatch(id ?? "")) return NotFound();
        Id = id!;
        Error = null;
        Timeline = null;
        try
        {
            Timeline = reports.Timeline(Id);
            if (Timeline == null) Error = "Không tìm thấy phiếu " + Id + " trên máy này.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            Error = "Không đọc được dòng thời gian: " + ex.Message;
        }
        return Page();
    }

    /// <summary>Tạo bản nháp bài học từ một bước. kind: meo | cai-tien.</summary>
    public IActionResult OnPostLesson(string id, string kind, string? title, string? body, string? step, string? task, string? ai)
    {
        if (!RequestIdRx().IsMatch(id ?? "")) return NotFound();
        try
        {
            var lesson = lessons.AddDraft(title ?? "", body ?? "", kind, id, SafeTask(task), SafeLabel(step), SafeLabel(ai));
            Message = $"Đã ghi {(kind == "meo" ? "mẹo" : "đề xuất cải tiến")} {lesson.Id} (bản nháp). Xem ở Mẹo & bài học.";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage(new { id });
    }

    /// <summary>Nhãn bước / AI: bỏ ký tự đường dẫn, cắt 60 ký tự (dịch vụ chặn '/', '\\', '..').</summary>
    public static string? SafeLabel(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = new string(s.Where(c => !char.IsControl(c) && c is not ('/' or '\\')).ToArray()).Trim();
        while (t.Contains("..", StringComparison.Ordinal)) t = t.Replace("..", ".", StringComparison.Ordinal);
        if (t.Length > 60) t = t[..60].TrimEnd();
        return t.Length == 0 ? null : t;
    }

    public static string? SafeTask(string? s) => s != null && TaskIdRx().IsMatch(s) && s.Length <= 40 ? s : null;

    public static string Minutes(double? m) => m is { } v ? (v >= 90 ? $"{v / 60:0.#} giờ" : $"{v:0} phút") : "—";

    public static string Tokens(long? t) => t is { } v ? (v >= 1_000_000 ? $"{v / 1_000_000d:0.#}M" : v >= 1000 ? $"{v / 1000d:0.#}k" : v.ToString()) : "—";
}
