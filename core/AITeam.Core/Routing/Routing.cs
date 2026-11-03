using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AITeam.Core.Providers;

namespace AITeam.Core.Routing;

/// <summary>Đọc giờ provider cho dùng lại từ câu báo lỗi THẬT (hàm thuần, không ném lỗi).</summary>
public static class QuotaResetParser
{
    private static readonly TimeSpan RxTimeout = TimeSpan.FromSeconds(1);
    private const string UnitPat = @"hours?|hrs?|h|minutes?|mins?|m|seconds?|secs?|s";

    private static readonly Regex AbsDateRx = new(
        @"try\s+again\s+at\s+(?<mon>Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+(?<day>\d{1,2})(?:st|nd|rd|th)?,?\s+(?<year>\d{4}),?\s+(?<hr>\d{1,2}):(?<min>\d{2})\s*(?<ampm>AM|PM)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, RxTimeout);

    private static readonly Regex TimeOnlyRx = new(
        @"try\s+again\s+at\s+(?<hr>\d{1,2}):(?<min>\d{2})\s*(?<ampm>AM|PM)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, RxTimeout);

    private static readonly Regex RelativeRx = new(
        $"(?:try\\s+again|resets?|retry)\\s+in\\s+(?<n1>\\d+)\\s*(?<u1>{UnitPat})\\b(?:(?:\\s*,\\s*|\\s+and\\s+|\\s+)(?<n2>\\d+)\\s*(?<u2>{UnitPat})\\b)?(?:(?:\\s*,\\s*|\\s+and\\s+|\\s+)(?<n3>\\d+)\\s*(?<u3>{UnitPat})\\b)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, RxTimeout);

    private static readonly Regex RetryAfterRx = new(
        @"retry[- ]after:?\s*(?<sec>\d+)(?:\s*(?:seconds?|secs?|s)\b)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, RxTimeout);

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
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            var candidates = new List<(int Index, DateTime Time)>();
            if (AbsDateRx.Match(text) is { Success: true } m1 && TryBuildAbsDate(m1, out var dt1)) candidates.Add((m1.Index, dt1));
            if (TimeOnlyRx.Match(text) is { Success: true } m2 && TryBuildTimeOnly(m2, now, out var dt2)) candidates.Add((m2.Index, dt2));
            if (RelativeRx.Match(text) is { Success: true } m3 && TryBuildRelative(m3, now, out var dt3)) candidates.Add((m3.Index, dt3));
            if (RetryAfterRx.Match(text) is { Success: true } m4 && long.TryParse(m4.Groups["sec"].Value, out var sec) && sec > 0)
                candidates.Add((m4.Index, now.AddSeconds(sec)));

            if (candidates.Count == 0) return null;
            var best = candidates.OrderBy(c => c.Index).First().Time;
            return best > now ? best : null;
        }
        catch { return null; }
    }

    private static bool TryBuildAbsDate(Match m, out DateTime dt)
    {
        dt = default;
        var month = m.Groups["mon"].Value[..3].ToLowerInvariant() switch
        {
            "jan" => 1, "feb" => 2, "mar" => 3, "apr" => 4, "may" => 5, "jun" => 6,
            "jul" => 7, "aug" => 8, "sep" => 9, "oct" => 10, "nov" => 11, "dec" => 12, _ => 0,
        };
        if (month == 0 || !int.TryParse(m.Groups["day"].Value, out var day) || !int.TryParse(m.Groups["year"].Value, out var year) || !TryParse12Hour(m, out var hour, out var min))
            return false;
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        dt = new DateTime(year, month, day, hour, min, 0);
        return true;
    }

    private static bool TryBuildTimeOnly(Match m, DateTime now, out DateTime dt)
    {
        dt = default;
        if (!TryParse12Hour(m, out var hour, out var min)) return false;
        dt = now.Date.AddHours(hour).AddMinutes(min);
        if (dt <= now) dt = dt.AddDays(1);
        return true;
    }

    private static bool TryParse12Hour(Match m, out int hour24, out int min)
    {
        hour24 = min = 0;
        if (!int.TryParse(m.Groups["hr"].Value, out var h) || !int.TryParse(m.Groups["min"].Value, out min) || h is < 1 or > 12 || min is < 0 or > 59)
            return false;
        hour24 = (h % 12) + (m.Groups["ampm"].Value.Equals("PM", StringComparison.OrdinalIgnoreCase) ? 12 : 0);
        return true;
    }

    private static bool TryBuildRelative(Match m, DateTime now, out DateTime dt)
    {
        dt = default;
        var sec = AddUnit(m.Groups["n1"], m.Groups["u1"]) + AddUnit(m.Groups["n2"], m.Groups["u2"]) + AddUnit(m.Groups["n3"], m.Groups["u3"]);
        if (sec <= 0) return false;
        dt = now.AddSeconds(sec);
        return true;
    }

    private static long AddUnit(Group numG, Group unitG)
        => numG.Success && long.TryParse(numG.Value, out var n)
            ? char.ToLowerInvariant(unitG.Value[0]) switch { 'h' => n * 3600, 'm' => n * 60, 's' => n, _ => 0 }
            : 0;
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
    public static readonly TimeSpan FailureRest = TimeSpan.FromMinutes(15);

    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public ProviderStateStore(string aiteamRoot) => FilePath = Path.Combine(aiteamRoot, ".state", "providers.json");

    public string FilePath { get; }

    /// <summary>Mọi bản ghi trong file (kể cả đã hết hạn).</summary>
    public IReadOnlyList<ProviderRest> All()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<List<ProviderRest>>(File.ReadAllText(FilePath, Encoding.UTF8), JsonOpts) ?? []
                : [];
        }
        catch { return []; }
    }

    /// <summary>Bản ghi của provider (so khớp không phân biệt hoa thường) nếu Until &gt; now; ngược lại null.</summary>
    public ProviderRest? RestOf(string providerId, DateTime now)
        => All().FirstOrDefault(x => x.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase) && x.Until > now);

    /// <summary>
    /// Cập nhật sau 1 lần chạy provider:
    /// - Succeeded ⇒ xóa bản ghi của provider đó (đã dùng lại được);
    /// - QuotaExhausted ⇒ Until = QuotaResetParser.Parse(Detail + "\n" + Text, now) ?? now + DefaultQuotaRest;
    /// - NotLoggedIn ⇒ Until = now + LoginRest (chờ chủ dự án đăng nhập; lần chạy thành công sau đó sẽ xóa);
    /// - trạng thái khác ⇒ không đổi gì (không ghi file).
    /// Mỗi provider chỉ 1 bản ghi (ghi đè). Detail = r.Detail cắt còn MaxDetail ký tự. At = now.
    /// </summary>
    public void Record(AgentRunResult r, DateTime now)
    {
        var root = Path.GetDirectoryName(Path.GetDirectoryName(FilePath))!;
        using var gate = FailureStreakStore.Lock(root);
        if (r.Status == RunStatus.Succeeded) FailureStreakStore.Update(root, r.ProviderId, false);
        if (r.Status is not (RunStatus.Succeeded or RunStatus.QuotaExhausted or RunStatus.NotLoggedIn))
        {
            var count = FailureStreakStore.Update(root, r.ProviderId, true);
            if (count < 3) return;
            // Luồng mới không được ghi đè state hỏng dù reader cũ có cơ chế coi rỗng.
            if (File.Exists(FilePath)) _ = JsonSerializer.Deserialize<List<ProviderRest>>(File.ReadAllText(FilePath), JsonOpts) ?? throw new InvalidDataException("State provider rỗng.");
            var rests = All().ToList();
            var prior = rests.FirstOrDefault(p => p.ProviderId.Equals(r.ProviderId, StringComparison.OrdinalIgnoreCase));
            if (prior != null && prior.Until > now + FailureRest) return;
            rests.RemoveAll(p => p.ProviderId.Equals(r.ProviderId, StringComparison.OrdinalIgnoreCase));
            rests.Add(new ProviderRest(r.ProviderId, now + FailureRest, r.Status, $"{count} lỗi liên tiếp; tạm nghỉ 15 phút để quản lý kiểm tra.", now));
            Save(rests);
            return;
        }

        var list = All().Where(x => !x.ProviderId.Equals(r.ProviderId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (r.Status != RunStatus.Succeeded)
        {
            var until = r.Status == RunStatus.QuotaExhausted
                ? QuotaResetParser.Parse($"{r.Detail}\n{r.Text}", now) ?? (now + DefaultQuotaRest)
                : now + LoginRest;
            var detail = r.Detail ?? "";
            if (detail.Length > MaxDetail) detail = detail[..MaxDetail];
            list.Add(new ProviderRest(r.ProviderId, until, r.Status, detail, now));
        }

        Save(list);
    }

    /// <summary>Xóa trạng thái nghỉ của provider (chủ dự án biết hạn mức đã có lại / vừa đổi tài khoản). Trả false nếu không có bản ghi.</summary>
    public bool Clear(string providerId)
    {
        var root = Path.GetDirectoryName(Path.GetDirectoryName(FilePath))!;
        using var gate = FailureStreakStore.Lock(root);
        FailureStreakStore.Update(root, providerId, false);
        var all = All();
        var list = all.Where(x => !x.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (list.Count == all.Count) return false;
        Save(list);
        return true;
    }

    private void Save(List<ProviderRest> list)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"providers.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tmp, JsonSerializer.Serialize(list, JsonOpts), Utf8NoBom);
        File.Move(tmp, FilePath, overwrite: true);
    }
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
    {
        if (!_settings.Routing.TryGetValue(role, out var candidates) || candidates.Count == 0)
            return new RouteDecision(null, [$"không có provider nào cho vai trò {role}"]);

        var skipped = new List<string>();
        foreach (var id in candidates)
        {
            var cfg = _settings.Providers.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (cfg == null) { skipped.Add($"{id}: không có cấu hình"); continue; }
            if (!cfg.Enabled) { skipped.Add($"{id}: đang tắt"); continue; }
            if (!cfg.Roles.Contains(role)) { skipped.Add($"{id}: không có vai trò {role}"); continue; }
            if (!string.IsNullOrEmpty(excludeProviderId) && id.Equals(excludeProviderId, StringComparison.OrdinalIgnoreCase))
            {
                skipped.Add(role == AgentRole.Reviewer ? $"{id}: là developer của task — cấm tự duyệt" : $"{id}: bị loại trừ");
                continue;
            }
            if (_state.RestOf(id, now) is { } rest)
            {
                skipped.Add($"{id}: nghỉ tới {rest.Until.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture)} ({rest.Reason})");
                continue;
            }
            return new RouteDecision(cfg.Id, skipped);
        }
        return new RouteDecision(null, skipped);
    }
}

/// <summary>Quyết định review AI tự động cho 1 task (đọc nguyên văn file task).</summary>
public static class ReviewPolicy
{
    private static readonly Regex ReviewAiRx = new(@"^\-\s*\*\*Review AI:\*\*\s*(?<val>\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DifficultyRx = new(@"^\-\s*\*\*Độ khó:\*\*\s*(?<val>\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Thứ tự ưu tiên:
    /// 1. Dòng "- **Review AI:** có" ⇒ (true, "Task yêu cầu Review AI"); "- **Review AI:** không" ⇒ (false, "Task tắt Review AI")
    ///    (chấp nhận chữ thường / hoa, "co" / "khong" không dấu; giá trị khác ⇒ bỏ qua dòng này).
    /// 2. Dòng "- **Độ khó:** L" (chữ đầu của giá trị, hoa / thường) ⇒ (true, "Độ khó L").
    /// 3. Còn lại ⇒ (false, "Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)").
    /// </summary>
    public static ReviewDecision Decide(string taskText)
    {
        var isLarge = false;
        foreach (var rawLine in (taskText ?? "").Split('\n'))
        {
            var line = rawLine.Trim();
            if (ReviewAiRx.Match(line) is { Success: true } mRev)
            {
                var val = mRev.Groups["val"].Value;
                if (val.Equals("có", StringComparison.OrdinalIgnoreCase) || val.Equals("co", StringComparison.OrdinalIgnoreCase))
                    return new ReviewDecision(true, "Task yêu cầu Review AI");
                if (val.Equals("không", StringComparison.OrdinalIgnoreCase) || val.Equals("khong", StringComparison.OrdinalIgnoreCase))
                    return new ReviewDecision(false, "Task tắt Review AI");
            }
            if (DifficultyRx.Match(line) is { Success: true } mDiff && mDiff.Groups["val"].Value.Equals("L", StringComparison.OrdinalIgnoreCase))
                isLarge = true;
        }
        return isLarge ? new ReviewDecision(true, "Độ khó L") : new ReviewDecision(false, "Độ khó S/M — Claude tự review (Codex hạn mức nhỏ)");
    }
}