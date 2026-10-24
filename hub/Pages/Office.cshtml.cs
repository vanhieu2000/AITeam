using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Pages;

/// <summary>Trang "Văn phòng" (trang chủ "/") — R-005 / V5-04 (bàn làm việc) + V5-05 (điều khiển).</summary>
public class OfficeModel : PageModel
{
    private readonly IOfficeService _office;
    private readonly ICommandService? _commands;

    public OfficeModel(IOfficeService office, ICommandService? commands = null)
    {
        _office = office;
        _commands = commands;
    }

    public OfficeView? View { get; private set; }

    /// <summary>Lỗi khi đọc dữ liệu ⇒ "Không đọc được dữ liệu văn phòng: " + ex.Message; View null.</summary>
    public string? Error { get; private set; }

    // ───── V5-05: điều khiển ─────

    /// <summary>true khi có ICommandService (Razor chỉ hiện nút / bảng điều khiển khi true).</summary>
    public bool ControlsEnabled => _commands != null;

    /// <summary>Sau lệnh thành công: "Đã bắt đầu: {Label} {Task}." (Label = CommandService.Label).</summary>
    public string? Notice { get; private set; }

    /// <summary>Lệnh bị từ chối: câu lỗi của dịch vụ (InvalidOperationException.Message); lỗi khác ⇒ "Không chạy được lệnh: " + ex.Message.</summary>
    public string? CommandError { get; private set; }

    public IReadOnlyList<TaskCard> Assignable { get; private set; } = Array.Empty<TaskCard>();
    public IReadOnlyList<TaskCard> Resumable { get; private set; } = Array.Empty<TaskCard>();
    public IReadOnlyList<CommandJob> Jobs { get; private set; } = Array.Empty<CommandJob>();

    /// <summary>Nạp View (bắt lỗi ⇒ Error) rồi LoadControl().</summary>
    public void OnGet()
    {
        try
        {
            View = _office.Get();
        }
        catch (Exception ex)
        {
            View = null;
            Error = "Không đọc được dữ liệu văn phòng: " + ex.Message;
        }
        LoadControl();
    }

    /// <summary>
    /// _commands null ⇒ không làm gì. Ngược lại nạp Assignable, Resumable, Jobs (tối đa 8 job đầu).
    /// Lỗi đọc (vd. bảng việc hỏng) ⇒ các danh sách rỗng, CommandError = "Không đọc được danh sách task: " + ex.Message (nếu CommandError đang null).
    /// </summary>
    public void LoadControl()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// HTMX tự làm mới (GET ?handler=Desks mỗi 10 giây): nạp như OnGet rồi trả PartialViewResult "_OfficeDesks"
    /// với model OfficeModel (chính trang này) — dựng tay ViewDataDictionary&lt;OfficeModel&gt;(new EmptyModelMetadataProvider(), ModelState) { Model = this }.
    /// </summary>
    public IActionResult OnGetDesks()
    {
        OnGet();
        return Self("_OfficeDesks");
    }

    /// <summary>
    /// POST ?handler=Command (form có antiforgery). target = "{Project}/{Task}" (tách ở dấu "/" ĐẦU TIÊN; không có "/" ⇒ Project "" và Task "").
    /// command phải là ĐÚNG tên một HubCommand (không phân biệt hoa thường, KHÔNG nhận số như "0"); khác / null ⇒ CommandError "Lệnh không hợp lệ.",
    /// không gọi dịch vụ. _commands null ⇒ CommandError "Hub đang ở chế độ chỉ xem." .
    /// Hợp lệ ⇒ _commands.Start(new CommandRequest(cmd, project, task, feedback)) ⇒ Notice; lỗi ⇒ CommandError (xem trên).
    /// Luôn: LoadControl() rồi trả Self("_OfficeControl") (model = trang này).
    /// </summary>
    public IActionResult OnPostCommand(string? command, string? target, string? feedback)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>GET ?handler=Jobs (bảng "Lệnh gần đây" tự làm mới): LoadControl() rồi Partial "_OfficeJobs".</summary>
    public IActionResult OnGetJobs()
    {
        LoadControl();
        return Self("_OfficeJobs");
    }

    /// <summary>
    /// GET ?handler=JobLog&amp;id=…: CHỈ id có trong _commands.Jobs() (so khớp chính xác) — đường dẫn file lấy từ job, không bao giờ ghép từ id.
    /// Không có / _commands null ⇒ NotFound(). Có ⇒ ContentResult text/plain; charset=utf-8 với 8000 ký tự CUỐI của log
    /// (file chưa có ⇒ "(chưa có output)"; đọc với FileShare.ReadWrite vì tiến trình còn đang ghi).
    /// </summary>
    public IActionResult OnGetJobLog(string? id)
        => throw new NotImplementedException(); // TODO(agent)

    private PartialViewResult Self(string name) => new()
    {
        ViewName = name,
        ViewData = new ViewDataDictionary<OfficeModel>(new EmptyModelMetadataProvider(), ModelState) { Model = this },
    };
}
