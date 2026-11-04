using System.Text;
using AITeam.Core.AiServices;
using AITeam.Core.AiServices.Adapters;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Form thêm / sửa dịch vụ AI. KHÔNG có ô giá trị khóa: chỉ tên khóa trong két.</summary>
public sealed class AiServiceInput
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Adapter { get; set; }
    public string? Command { get; set; }
    public string? Args { get; set; }
    public string? BaseUrl { get; set; }
    public string? Models { get; set; }
    public List<string> Roles { get; set; } = [];
    public int MaxJobs { get; set; } = 1;
    public bool Paid { get; set; }
    public string? SecretKeyName { get; set; }
    public string? ManageUrl { get; set; }
}

/// <summary>
/// Tài khoản AI (/AiServices) — R-014 UI-08. Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.6 + 4.5b (Cài đặt trợ lý chat).
/// Danh bạ dịch vụ AI trong SQLite cục bộ (IAiServiceRegistry). Thêm AI = thêm dòng, không sửa code.
/// Xóa = tắt (giữ lịch sử số đo). Xuất / nhập .ini để chép sang máy kia (không chứa bí mật).
/// </summary>
[ValidateAntiForgeryToken]
public sealed class AiServicesModel(IAiServiceRegistry registry, IAiProviderFactory factory, IChatAssistant assistant) : PageModel
{
    public const int MaxIniBytes = 256 * 1024;

    [BindProperty(SupportsGet = true)] public string? Edit { get; set; }

    public IReadOnlyList<AiService> Services { get; private set; } = [];
    public IReadOnlyDictionary<string, AiQuota> Quotas { get; private set; } = new Dictionary<string, AiQuota>();
    public IReadOnlyDictionary<string, IReadOnlyList<AiAccount>> AccountsByService { get; private set; } = new Dictionary<string, IReadOnlyList<AiAccount>>();
    public AssistantSettings? Assistant { get; private set; }
    public IReadOnlyList<AssistantOption> AssistantOptions { get; private set; } = [];
    public AiServiceInput Form { get; private set; } = new();
    public bool IsEdit { get; private set; }
    public IReadOnlyList<string> FormErrors { get; private set; } = [];
    public string? Error { get; private set; }
    public string Location => SafeLocation();

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        Load();
        var editing = Edit == null ? null : Services.FirstOrDefault(s => s.Id == Edit);
        if (editing != null)
        {
            Form = ToInput(editing);
            IsEdit = true;
        }
    }

    private void Load()
    {
        try
        {
            Services = registry.List(includeDisabled: true).OrderByDescending(s => s.Enabled).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
            Quotas = registry.LatestQuotas().GroupBy(q => q.ServiceId).ToDictionary(g => g.Key, g => g.OrderByDescending(q => q.ObservedAt).First());
            AccountsByService = registry.Accounts().GroupBy(a => a.ServiceId).ToDictionary(g => g.Key, g => (IReadOnlyList<AiAccount>)g.ToList());
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            Error = "Không đọc được danh bạ AI: " + ex.Message;
        }
        try
        {
            Assistant = assistant.Settings();
            AssistantOptions = assistant.Options();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or ArgumentException)
        {
            Assistant = null;
        }
    }

    public IActionResult OnPostSave(AiServiceInput input, bool isEdit)
    {
        var existing = string.IsNullOrWhiteSpace(input.Id) ? null : registry.Get(input.Id.Trim());
        if (!isEdit && existing != null)
            return Invalid(input, isEdit, [$"Mã \"{input.Id}\" đã có. Bấm Sửa ở thẻ đó để đổi."]);
        if (isEdit && existing == null)
            return Invalid(input, isEdit, ["Không tìm thấy dịch vụ cần sửa."]);
        if (!AiServiceText2.TryParseAdapter(input.Adapter, out var adapter))
            return Invalid(input, isEdit, ["Kiểu kết nối không hợp lệ."]);

        var roles = new List<AiRole>();
        foreach (var r in input.Roles)
        {
            if (!AiServiceText2.TryParseRole(r, out var role)) return Invalid(input, isEdit, ["Vai trò không hợp lệ: " + r]);
            roles.Add(role);
        }
        var now = DateTimeOffset.Now;
        var service = new AiService(
            (input.Id ?? "").Trim(), (input.Name ?? "").Trim(), adapter,
            Blank(input.Command), Lines(input.Args), Blank(input.BaseUrl),
            WorkflowModel.Split(input.Models), roles.Distinct().ToList(),
            input.MaxJobs, input.Paid, Blank(input.SecretKeyName), Blank(input.ManageUrl),
            existing?.Enabled ?? true, existing?.CreatedAt ?? now, now);

        var validation = registry.Validate(service);
        if (!validation.Ok) return Invalid(input, isEdit, validation.Errors);
        try
        {
            registry.Upsert(service);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or InvalidOperationException)
        {
            return Invalid(input, isEdit, [ex.Message]);
        }
        Message = $"Đã {(isEdit ? "lưu" : "thêm")} dịch vụ AI \"{service.Name}\".";
        return RedirectToPage();
    }

    private PageResult Invalid(AiServiceInput input, bool isEdit, IReadOnlyList<string> errors)
    {
        Load();
        Form = input;
        IsEdit = isEdit;
        FormErrors = errors;
        return Page();
    }

    public IActionResult OnPostToggle(string id, bool enabled)
    {
        try
        {
            registry.SetEnabled(id, enabled);
            Message = enabled ? $"Đã bật {id}." : $"Đã tắt {id}. Lịch sử số đo vẫn giữ.";
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or IOException or InvalidDataException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage();
    }

    /// <summary>"Kiểm tra kết nối": chỉ gọi CheckAsync của adapter (không gửi prompt, không tốn lượt).</summary>
    public async Task<IActionResult> OnPostCheckAsync(string id)
    {
        var service = registry.Get(id ?? "");
        if (service == null)
        {
            ErrorMessage = "Không tìm thấy dịch vụ.";
            return RedirectToPage();
        }
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext?.RequestAborted ?? CancellationToken.None);
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            var h = await factory.Create(service).CheckAsync(cts.Token);
            var login = h.LoggedIn switch { true => "đã đăng nhập", false => "chưa đăng nhập", null => "không rõ đăng nhập" };
            var text = $"{service.Name}: {(h.Installed ? "có cài" : "chưa cài")}, {login}.{(string.IsNullOrWhiteSpace(h.Detail) ? "" : " " + h.Detail)}";
            if (h.Installed && h.LoggedIn != false) Message = text; else ErrorMessage = text;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = $"{service.Name}: kiểm tra quá 20 giây, đã dừng.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or System.ComponentModel.Win32Exception or HttpRequestException)
        {
            ErrorMessage = $"{service.Name}: {ex.Message}";
        }
        return RedirectToPage();
    }

    public IActionResult OnGetExport()
    {
        var text = registry.ExportIni();
        return File(new UTF8Encoding(true).GetBytes(text), "text/plain; charset=utf-8", $"ai-services-{DateTime.Now:yyyyMMdd}.ini");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ErrorMessage = "Chưa chọn tệp .ini.";
            return RedirectToPage();
        }
        if (file.Length > MaxIniBytes)
        {
            ErrorMessage = "Tệp .ini quá lớn (tối đa 256 KB).";
            return RedirectToPage();
        }
        try
        {
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buffer = new char[MaxIniBytes + 1];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            var r = registry.ImportIni(new string(buffer, 0, read));
            Message = $"Đã nhập: thêm {r.Added}, cập nhật {r.Updated}" + (r.Skipped.Count > 0 ? $", bỏ qua {r.Skipped.Count} ({string.Join("; ", r.Skipped.Take(5))})." : ".");
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or DecoderFallbackException)
        {
            ErrorMessage = "Không nhập được: " + ex.Message;
        }
        return RedirectToPage();
    }

    public IActionResult OnPostAssistant(string defaultAi, string defaultMode, List<string>? researchAis, bool keepHistory)
    {
        try
        {
            if (!Enum.TryParse<ChatMode>(defaultMode, true, out var mode) || !Enum.IsDefined(mode))
                throw new ArgumentException("Chế độ mặc định không hợp lệ.");
            assistant.SaveSettings(new AssistantSettings(defaultAi ?? "", mode, researchAis ?? [], keepHistory));
            Message = "Đã lưu cài đặt trợ lý chat.";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
        return RedirectToPage(null, null, "tro-ly");
    }

    public static AiServiceInput ToInput(AiService s) => new()
    {
        Id = s.Id, Name = s.Name, Adapter = AdapterText(s.Adapter), Command = s.Command,
        Args = string.Join("\n", s.Args), BaseUrl = s.BaseUrl, Models = string.Join(", ", s.Models),
        Roles = s.Roles.Select(RoleText).ToList(), MaxJobs = s.MaxJobs, Paid = s.Paid,
        SecretKeyName = s.SecretKeyName, ManageUrl = s.ManageUrl,
    };

    public static string AdapterText(AiAdapter a) => a switch
    {
        AiAdapter.CliJson => "cli-json",
        AiAdapter.CliText => "cli-text",
        _ => "openai-compatible-http",
    };

    public static string RoleText(AiRole r) => r switch
    {
        AiRole.Code => "code",
        AiRole.Qc => "qc",
        AiRole.Research => "research",
        _ => "chat",
    };

    public static string RoleLabel(AiRole r) => r switch
    {
        AiRole.Code => "Code",
        AiRole.Qc => "QC",
        AiRole.Research => "Nghiên cứu",
        _ => "Chat",
    };

    public static string ModeLabel(ChatMode m) => m switch
    {
        ChatMode.Task => "Giao việc",
        ChatMode.Research => "Nghiên cứu (nhiều AI)",
        _ => "Hỏi đáp",
    };

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static IReadOnlyList<string> Lines(string? s) =>
        (s ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>Chỉ hiện tên tệp CSDL, không lộ đường dẫn thư mục người dùng đầy đủ.</summary>
    private string SafeLocation()
    {
        try { return Path.GetFileName(registry.Location) is { Length: > 0 } f ? f : "(bộ nhớ)"; }
        catch (ArgumentException) { return "(không rõ)"; }
    }

    /// <summary>Bản sao công khai của bộ chuyển chuỗi (lớp gốc trong Core là internal).</summary>
    private static class AiServiceText2
    {
        public static bool TryParseAdapter(string? raw, out AiAdapter adapter)
        {
            switch (raw?.Trim().ToLowerInvariant())
            {
                case "cli-json": adapter = AiAdapter.CliJson; return true;
                case "cli-text": adapter = AiAdapter.CliText; return true;
                case "openai-compatible-http": adapter = AiAdapter.OpenAiCompatibleHttp; return true;
                default: adapter = default; return false;
            }
        }

        public static bool TryParseRole(string? raw, out AiRole role)
        {
            foreach (var r in Enum.GetValues<AiRole>())
                if (string.Equals(raw?.Trim(), RoleText(r), StringComparison.OrdinalIgnoreCase)) { role = r; return true; }
            role = default;
            return false;
        }
    }
}
