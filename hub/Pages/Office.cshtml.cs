using System.Text;
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
    private readonly IRequestService? _requests;
    private readonly IOfficeChatService? _chat;
    private readonly ITeamConfigService? _config;
    private readonly LocalChatStore? _localChat;
    public ChatConversation? LatestAiConversation { get; private set; }
    public string? AiConversationNotice { get; private set; }
    private readonly IResearchService? _research;
    public ResearchModel? ResearchPane { get; private set; }

    public OfficeModel(IOfficeService office, ICommandService? commands = null, IRequestService? requests = null, IOfficeChatService? chat = null, ITeamConfigService? config = null, LocalChatStore? localChat = null, IResearchService? research = null)
    {
        _office = office;
        _commands = commands;
        _requests = requests;
        _chat = chat;
        _config = config;
        _localChat = localChat;
        _research = research;
    }

    [BindProperty(SupportsGet = true)] public string? RequestId { get; set; }
    public IReadOnlyList<RequestThread> Threads { get; private set; } = Array.Empty<RequestThread>();
    public RequestThread? SelectedThread { get; private set; }
    public IReadOnlyList<ProjectInfo> ChatProjects { get; private set; } = Array.Empty<ProjectInfo>();
    public string? ChatNotice { get; private set; }
    public string? ChatError { get; private set; }

    /// <summary>
    /// Đọc GetAll/GetProjects. RequestId null/rỗng chọn tạo mới; khác phải khớp toàn bộ
    /// R-[0-9]{3} và tồn tại theo Ordinal, sai/mất: "Phiếu không hợp lệ."; không tự chọn phiếu khác.
    /// Lỗi đọc: "Không đọc được hội thoại: " + ex.Message. Thiếu dịch vụ: danh sách rỗng,
    /// "Chat đang ở chế độ chỉ xem.". Không ghi dữ liệu hoặc chạy tiến trình.
    /// </summary>
    public void LoadChat()
    {
        Threads = Array.Empty<RequestThread>();
        ChatProjects = Array.Empty<ProjectInfo>();
        SelectedThread = null;
        if (_requests == null || _config == null)
        {
            ChatError ??= "Chat đang ở chế độ chỉ xem.";
            return;
        }
        try
        {
            Threads = _requests.GetAll();
            ChatProjects = _config.GetProjects();
            if (!string.IsNullOrEmpty(RequestId))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(RequestId, @"\AR-[0-9]{3}\z"))
                    SelectedThread = Threads.FirstOrDefault(t => string.Equals(t.Id, RequestId, StringComparison.Ordinal));
                if (SelectedThread == null && ChatNotice == null) ChatError ??= "Phiếu không hợp lệ.";
            }
        }
        catch (Exception ex) { ChatError ??= "Không đọc được hội thoại: " + ex.Message; }
    }

    /// <summary>
    /// GET Chat: LoadChat rồi trả _OfficeChat, Model=this, sao chép ViewData và ModelState.
    /// Partial chỉ có hội thoại/thông báo, root office-chat-thread, không chứa form soạn.
    /// </summary>
    public IActionResult OnGetChat()
    {
        LoadChat();
        return Self("_OfficeChat");
    }

    /// <summary>
    /// POST Chat: gọi IOfficeChatService.Send(project,text,requestId) đúng một lần, không trim thay dịch vụ.
    /// Thành công: "Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.".
    /// ArgumentException: ChatError=ex.Message; lỗi khác: "Không gửi được yêu cầu: " + ex.Message.
    /// Thiếu chat: "Chat đang ở chế độ chỉ xem.". Giữ RequestId, nạp hội thoại, trả _OfficeChat,
    /// giữ ViewData/ModelState. Không redirect. Form ngoài swap, lỗi giữ bản nháp, chỉ thành công mới xóa.
    /// </summary>
    public IActionResult OnPostChat(string project, string? text, string? requestId)
    {
        RequestId = requestId;
        if (_chat == null) ChatError = "Chat đang ở chế độ chỉ xem.";
        else
        {
            try
            {
                _chat.Send(project, text, requestId);
                if (PageContext.HttpContext is { } context)
                    context.Response.Headers["HX-Trigger"] = "office-chat-sent";
                ChatNotice = "Đã gửi. Claude nhận yêu cầu khi bắt đầu phiên làm việc và trả lời trong phiếu R-xxx.";
            }
            catch (ArgumentException ex) { ChatError = ex.Message; }
            catch (Exception ex) { ChatError = "Không gửi được yêu cầu: " + ex.Message; }
        }
        LoadChat();
        return Self("_OfficeChat");
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
        try { LatestAiConversation = _localChat?.Conversations().FirstOrDefault(); }
        catch (Exception) { AiConversationNotice = "Chưa đọc được hội thoại. Mở Trò chuyện AI để kiểm tra."; }
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
        LoadChat();
        LoadResearch();
    }

    private void LoadResearch()
    {
        if (_research == null || _requests == null) return;
        ResearchPane = new ResearchModel(_research, _requests) { Embedded = true };
        ResearchPane.OnGet(SelectedThread?.Id);
    }

    public IActionResult OnGetResearch()
    {
        LoadChat(); LoadResearch();
        return Self("_OfficeResearch");
    }

    public IActionResult OnPostResearchAsk(string? r, string? question, string[]? ai, bool includeRequestText)
    {
        RequestId = r; OnGet();
        if (ResearchPane?.Current != null) ResearchPane.OnPostAsk(r, question, ai, includeRequestText);
        return Page();
    }

    public IActionResult OnPostResearchSaveManual(string? r, string? questionId, string? ai, string? question, string? answer, string? sources)
    {
        RequestId = r; OnGet();
        if (ResearchPane?.Current != null) ResearchPane.OnPostSaveManual(r, questionId, ai, question, answer, sources);
        return Page();
    }

    /// <summary>
    /// _commands null ⇒ không làm gì. Ngược lại nạp Assignable, Resumable, Jobs (tối đa 8 job đầu).
    /// Lỗi đọc (vd. bảng việc hỏng) ⇒ các danh sách rỗng, CommandError = "Không đọc được danh sách task: " + ex.Message (nếu CommandError đang null).
    /// </summary>
    public void LoadControl()
    {
        if (_commands == null) return;
        try
        {
            Assignable = _commands.Assignable();
            Resumable = _commands.Resumable();
            Jobs = _commands.Jobs().Take(8).ToList();
        }
        catch (Exception ex)
        {
            Assignable = Array.Empty<TaskCard>();
            Resumable = Array.Empty<TaskCard>();
            Jobs = Array.Empty<CommandJob>();
            CommandError ??= "Không đọc được danh sách task: " + ex.Message;
        }
    }

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
    {
        if (_commands == null)
        {
            CommandError = "Hub đang ở chế độ chỉ xem.";
            return Self("_OfficeControl");
        }
        if (string.IsNullOrWhiteSpace(command) ||
            !Enum.GetNames<HubCommand>().Any(n => string.Equals(n, command, StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse<HubCommand>(command, ignoreCase: true, out var cmd))
        {
            CommandError = "Lệnh không hợp lệ.";
            LoadControl();
            return Self("_OfficeControl");
        }

        var slash = target?.IndexOf('/') ?? -1;
        var project = slash >= 0 ? target![..slash] : "";
        var task = slash >= 0 ? target![(slash + 1)..] : "";

        try
        {
            var job = _commands.Start(new CommandRequest(cmd, project, task, feedback));
            Notice = $"Đã bắt đầu: {CommandService.Label(cmd)} {job.Request.Task}.";
        }
        catch (InvalidOperationException ex) { CommandError = ex.Message; }
        catch (Exception ex) { CommandError = "Không chạy được lệnh: " + ex.Message; }

        LoadControl();
        return Self("_OfficeControl");
    }

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
    {
        if (_commands == null || string.IsNullOrEmpty(id)) return NotFound();
        var job = _commands.Jobs().FirstOrDefault(j => string.Equals(j.Id, id, StringComparison.Ordinal));
        if (job == null) return NotFound();

        string content = "(chưa có output)";
        if (System.IO.File.Exists(job.LogFile))
        {
            using var fs = new FileStream(job.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            var text = sr.ReadToEnd();
            content = text.Length > 8000 ? text[^8000..] : text;
        }
        return new ContentResult { Content = content, ContentType = "text/plain; charset=utf-8" };
    }

    private PartialViewResult Self(string name)
    {
        var data = new ViewDataDictionary<OfficeModel>(new EmptyModelMetadataProvider(), ModelState);
        if (ViewData != null)
            foreach (var item in ViewData) data[item.Key] = item.Value;
        data.Model = this;
        return new PartialViewResult { ViewName = name, ViewData = data };
    }
}
