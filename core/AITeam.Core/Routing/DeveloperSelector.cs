namespace AITeam.Core.Routing;

/// <summary>Ảnh chụp khả năng người làm; dữ liệu không rõ không được coi là sẵn sàng.</summary>
public sealed record DeveloperCandidate(string Id, bool Available, bool PrivacyGuaranteed, string? Model,
    int ActiveTasks, DateTimeOffset? RestUntil = null, string? UnavailableReason = null);
public sealed record DeveloperTask(string? Dev, bool PrivateRepository, string Difficulty, int? PlannedLines,
    IReadOnlyList<string> Scope, bool ScopeLocked = false);
public sealed record DeveloperSelection(string? ProviderId, string Status, string Reason, DateTimeOffset? RetryAt);

/// <summary>Quyết định thuần theo 6 luật R-012; không gọi CLI/model hoặc tự sửa cấu hình.</summary>
public static class DeveloperSelector
{
    public static DeveloperSelection Select(DeveloperTask task, IReadOnlyList<DeveloperCandidate> candidates,
        DateTimeOffset now, string requiredAgyModel = "argon-4")
    {
        if (candidates.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != candidates.Count)
            throw new InvalidDataException("Trùng người làm trong dữ liệu định tuyến.");
        if (task.ScopeLocked) return Wait("Phạm vi đang bị khóa bởi task khác; không giao đồng thời.", null);
        string? Reject(DeveloperCandidate c)
        {
            if (c.Id.Equals("opencode", StringComparison.OrdinalIgnoreCase) &&
                !(c.Model?.Contains("deepseek", StringComparison.OrdinalIgnoreCase) ?? false)) return "OpenCode chỉ được dùng DeepSeek";
            if (c.Id.Equals("agy", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(requiredAgyModel) || !(c.Model?.Contains(requiredAgyModel, StringComparison.OrdinalIgnoreCase) ?? false)))
                return "Antigravity không có model bắt buộc " + requiredAgyModel;
            if (c.Id is not ("codex" or "opencode" or "agy")) return "không phải người code được phép";
            if (task.PrivateRepository && !c.PrivacyGuaranteed) return "repo private/không rõ riêng tư, model không cam kết riêng tư";
            if (!c.Available || c.RestUntil > now) return c.UnavailableReason ?? "đang nghỉ hoặc chưa đăng nhập";
            if (c.ActiveTasks < 0) return "số việc đang chạy không rõ";
            return null;
        }
        if (!string.IsNullOrWhiteSpace(task.Dev))
        {
            var pinned = candidates.SingleOrDefault(c => c.Id.Equals(task.Dev, StringComparison.OrdinalIgnoreCase));
            var reason = pinned == null ? "người được chỉ định chưa cấu hình" : Reject(pinned);
            return reason == null ? Chosen(pinned!, "Dev được chỉ định trong task")
                : Wait("Không đổi Dev chỉ định: " + reason, pinned?.RestUntil);
        }
        var eligible = candidates.Where(c => Reject(c) == null).ToList();
        if (eligible.Count == 0)
        {
            var resets = candidates.Where(c => c.RestUntil > now).Select(c => c.RestUntil).ToList();
            return Wait(string.Join("; ", candidates.Select(c => c.Id + ": " + Reject(c))), resets.Count > 0 ? resets.Min() : null);
        }
        var hard = task.Difficulty.Equals("L", StringComparison.OrdinalIgnoreCase) || task.PlannedLines > 300 ||
            task.Scope.Any(p => p.Replace('\\', '/').Split('/').Last().Equals("team.ps1", StringComparison.OrdinalIgnoreCase));
        var preferred = hard ? "codex" : "opencode";
        // Độ khó ưu tiên trước; cân tải chỉ trong nhóm ứng viên còn lại có cùng mức phù hợp.
        var chosen = eligible.OrderBy(c => c.Id.Equals(preferred, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(c => c.ActiveTasks).ThenBy(c => c.Id, StringComparer.OrdinalIgnoreCase).First();
        return Chosen(chosen, $"Độ khó {(hard ? "L/PowerShell/>300 dòng" : "S–M")}; ưu tiên {preferred}; đang chạy {chosen.ActiveTasks} việc");
    }
    private static DeveloperSelection Chosen(DeveloperCandidate c, string reason) => new(c.Id, "Sẵn sàng giao", reason, null);
    private static DeveloperSelection Wait(string reason, DateTimeOffset? retry) => new(null, "Chờ người làm", reason, retry);
}
