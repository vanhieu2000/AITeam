using AITeam.Core.Research;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Phòng nghiên cứu" (/Research?r=R-005) — R-005 / V5-07b.</summary>
public class ResearchModel : PageModel
{
    public bool Embedded { get; set; }
    private readonly IResearchService _research;
    private readonly IRequestService _requests;

    public ResearchModel(IResearchService research, IRequestService requests)
    {
        _research = research;
        _requests = requests;
    }

    /// <summary>Phiếu đang xem: query "r" nếu là Id có trong Requests; ngược lại phiếu đầu tiên của Requests; không có phiếu nào ⇒ null.</summary>
    public RequestThread? Current { get; private set; }

    public IReadOnlyList<RequestThread> Requests { get; private set; } = Array.Empty<RequestThread>();
    public IReadOnlyList<ResearchAiOption> Options { get; private set; } = Array.Empty<ResearchAiOption>();
    public IReadOnlyList<ResearchJob> Jobs { get; private set; } = Array.Empty<ResearchJob>();
    public IReadOnlyList<ResearchEntry> Entries { get; private set; } = Array.Empty<ResearchEntry>();

    /// <summary>Sau bấm "Hỏi": kết quả (lời nhắn để copy + kết cục từng AI). null khi chưa hỏi.</summary>
    public ResearchAskResult? Asked { get; private set; }

    /// <summary>Câu hỏi vừa gửi (để form dán kết quả thủ công gửi kèm).</summary>
    public string? AskedQuestion { get; private set; }

    public string? Notice { get; private set; }

    /// <summary>Lỗi đọc dữ liệu ⇒ "Không đọc được dữ liệu phòng nghiên cứu: " + ex.Message; lỗi dữ liệu nhập (ArgumentException) ⇒ ex.Message.</summary>
    public string? Error { get; private set; }

    /// <summary>Nạp Requests (GetAll), Current theo r, Options, Jobs / Entries của Current. Lỗi ⇒ Error (danh sách rỗng).</summary>
    public void OnGet(string? r)
    {
        try
        {
            Requests = _requests.GetAll();
            Current = (!string.IsNullOrEmpty(r) ? Requests.FirstOrDefault(x => string.Equals(x.Id, r, StringComparison.OrdinalIgnoreCase)) : null)
                ?? (Embedded ? null : Requests.FirstOrDefault());
            if (Embedded && Current == null)
            {
                Options = []; Jobs = []; Entries = [];
                return;
            }
            Options = _research.Options();
            Jobs = Current != null ? _research.Jobs(Current.Id) : Array.Empty<ResearchJob>();
            Entries = Current != null ? _research.Entries(Current.Id) : Array.Empty<ResearchEntry>();
        }
        catch (Exception ex)
        {
            Current = null;
            Requests = Array.Empty<RequestThread>();
            Options = Array.Empty<ResearchAiOption>();
            Jobs = Array.Empty<ResearchJob>();
            Entries = Array.Empty<ResearchEntry>();
            Error ??= "Không đọc được dữ liệu phòng nghiên cứu: " + ex.Message;
        }
    }

    /// <summary>
    /// POST ?handler=Ask (antiforgery): _research.Ask(r, question, ai, includeRequestText) ⇒ Asked, AskedQuestion = question;
    /// ArgumentException ⇒ Error = ex.Message. Luôn nạp lại như OnGet(r) (KHÔNG ghi đè Error đã có) rồi trả Page().
    /// </summary>
    public IActionResult OnPostAsk(string? r, string? question, string[]? ai, bool includeRequestText)
    {
        try
        {
            Asked = _research.Ask(r, question, ai, includeRequestText);
            AskedQuestion = question;
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Error = "Không đọc được dữ liệu phòng nghiên cứu: " + ex.Message;
        }
        OnGet(r);
        return Page();
    }

    /// <summary>
    /// POST ?handler=SaveManual (antiforgery): _research.SaveManual(r, questionId, ai, question, answer, sources) ⇒ Notice "Đã lưu câu trả lời {AiName}.";
    /// ArgumentException ⇒ Error = ex.Message. Luôn nạp lại như OnGet(r) rồi trả Page().
    /// </summary>
    public IActionResult OnPostSaveManual(string? r, string? questionId, string? ai, string? question, string? answer, string? sources)
    {
        try
        {
            var entry = _research.SaveManual(r, questionId, ai, question, answer, sources);
            var aiName = AITeam.Core.Research.ResearchAis.NameOf(entry.Ai);
            Notice = $"Đã lưu câu trả lời {aiName}.";
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Error = "Không đọc được dữ liệu phòng nghiên cứu: " + ex.Message;
        }
        OnGet(r);
        return Page();
    }

    /// <summary>HTMX GET ?handler=Results&amp;r=… mỗi 5 giây: nạp như OnGet(r) rồi PartialViewResult "_ResearchResults" (model = trang này, dựng tay ViewDataDictionary như OfficeModel).</summary>
    public IActionResult OnGetResults(string? r)
    {
        OnGet(r);
        return new PartialViewResult
        {
            ViewName = "_ResearchResults",
            ViewData = new ViewDataDictionary<ResearchModel>(new EmptyModelMetadataProvider(), ModelState) { Model = this },
        };
    }
}
