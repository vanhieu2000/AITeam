using AITeam.Core.Providers;

namespace AITeam.Core.Routing;

/// <summary>Đọc giờ provider cho dùng lại từ câu báo lỗi THẬT (hàm thuần, không ném lỗi).</summary>
public static class QuotaResetParser
{
    /// <summary>
    /// Trả giờ địa phương provider cho dùng lại, hoặc null nếu không đọc được / giờ đó không ở sau <paramref name="now"/>.
    /// Mẫu nhận (không phân biệt hoa thường):
    /// 1. Codex thật: "try again at Oct 24th, 2026 4:19 AM" (tháng tiếng Anh viết tắt hoặc đầy đủ; hậu tố st/nd/rd/th tùy chọn; dấu phẩy tùy chọn).
    /// 2. Không có ngày: "try again at 4:19 AM" ⇒ hôm nay giờ đó; nếu ≤ now ⇒ ngày mai.
    /// 3. Khoảng: "try again in 2 hours 5 minutes", "try again in 37 minutes", "resets in 1h 30m", "retry in 45 seconds"
    ///    (đơn vị: hour/hours/hr/hrs/h, minute/minutes/min/mins/m, second/seconds/sec/secs/s; có thể có "and", dấu phẩy) ⇒ now + tổng.
    /// 4. "Retry-After: 120" hoặc "retry after 120 seconds" ⇒ now + 120 giây.
    /// Có nhiều mẫu ⇒ lấy mẫu xuất hiện ĐẦU TIÊN trong text.
    /// </summary>
    public static DateTime? Parse(string? text, DateTime now)
        => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>
/// Trạng thái "nghỉ" của provider trên máy này: &lt;AITeamRoot&gt;/.state/providers.json (thư mục /.state/ đã gitignore).
/// Định dạng: mảng JSON các ProviderRest (tên trường camelCase, Reason ghi chữ "QuotaExhausted"). UTF-8 không BOM.
/// File không có / hỏng ⇒ coi như rỗng (KHÔNG ném lỗi); lần ghi sau ghi đè bằng nội dung đúng.
/// Ghi: file tạm cùng thư mục rồi File.Move(overwrite: true).
/// </summary>
public sealed class ProviderStateStore
{
    public static readonly TimeSpan DefaultQuotaRest = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan LoginRest = TimeSpan.FromHours(24);
    public const int MaxDetail = 300;

    public ProviderStateStore(string aiteamRoot) => FilePath = Path.Combine(aiteamRoot, ".state", "providers.json");

    public string FilePath { get; }

    /// <summary>Mọi bản ghi trong file (kể cả đã hết hạn).</summary>
    public IReadOnlyList<ProviderRest> All()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Bản ghi của provider (so khớp không phân biệt hoa thường) nếu Until &gt; now; ngược lại null.</summary>
    public ProviderRest? RestOf(string providerId, DateTime now)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Cập nhật sau 1 lần chạy provider:
    /// - Succeeded ⇒ xóa bản ghi của provider đó (đã dùng lại được);
    /// - QuotaExhausted ⇒ Until = QuotaResetParser.Parse(Detail + "\n" + Text, now) ?? now + DefaultQuotaRest;
    /// - NotLoggedIn ⇒ Until = now + LoginRest (chờ chủ dự án đăng nhập; lần chạy thành công sau đó sẽ xóa);
    /// - trạng thái khác ⇒ không đổi gì (không ghi file).
    /// Mỗi provider chỉ 1 bản ghi (ghi đè). Detail = r.Detail cắt còn MaxDetail ký tự. At = now.
    /// </summary>
    public void Record(AgentRunResult r, DateTime now)
        => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>Chọn provider theo thứ tự routing trong team.json, bỏ qua provider tắt / sai vai trò / bị loại trừ / đang nghỉ.</summary>
public sealed class ProviderRouter : IProviderRouter
{
    private readonly ProviderSettings _settings;
    private readonly ProviderStateStore _state;

    public ProviderRouter(ProviderSettings settings, ProviderStateStore state)
    {
        _settings = settings;
        _state = state;
    }

    /// <summary>
    /// Duyệt settings.Routing[role] theo thứ tự; provider đầu tiên qua hết các điều kiện được chọn. Mỗi provider bị bỏ qua thêm 1 dòng Skipped:
    /// - không có trong settings.Providers ⇒ "{id}: không có cấu hình";
    /// - Enabled = false ⇒ "{id}: đang tắt";
    /// - Roles không chứa role ⇒ "{id}: không có vai trò {role}";
    /// - trùng excludeProviderId (không phân biệt hoa thường): role Reviewer ⇒ "{id}: là developer của task — cấm tự duyệt", vai trò khác ⇒ "{id}: bị loại trừ";
    /// - đang nghỉ (state.RestOf) ⇒ "{id}: nghỉ tới {Until:HH:mm dd/MM} ({Reason})".
    /// Không chọn được ⇒ ProviderId null; nếu Routing không có role hoặc danh sách rỗng ⇒ Skipped = ["không có provider nào cho vai trò {role}"].
    /// </summary>
    public RouteDecision Pick(AgentRole role, string? excludeProviderId, DateTime now)
        => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>Quyết định review AI tự động cho 1 task (đọc nguyên văn file task).</summary>
public static class ReviewPolicy
{
    /// <summary>
    /// Thứ tự ưu tiên:
    /// 1. Dòng "- **Review AI:** có" ⇒ (true, "Task yêu cầu Review AI"); "- **Review AI:** không" ⇒ (false, "Task tắt Review AI")
    ///    (chấp nhận chữ thường / hoa, "co" / "khong" không dấu; giá trị khác ⇒ bỏ qua dòng này).
    /// 2. Dòng "- **Độ khó:** L" (chữ đầu của giá trị, hoa / thường) ⇒ (true, "Độ khó L").
    /// 3. Còn lại ⇒ (false, "Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)").
    /// </summary>
    public static ReviewDecision Decide(string taskText)
        => throw new NotImplementedException(); // TODO(agent)
}
