using AITeam.Core.Providers;

namespace AITeam.Core.Review;

/// <summary>
/// Review độc lập bằng provider khác provider đã viết code (thiết kế: docs/design/V4-MULTI-AI.md mục B5).
/// KHUNG do Claude viết — agent chỉ điền phần thân (TODO(agent)), giữ nguyên chữ ký public.
/// </summary>
public sealed class ReviewService
{
    /// <summary>Lược đồ JSON bắt buộc reviewer trả về (Codex: --output-schema). Đã thử thật 23/10 với gpt-6.1-sol.</summary>
    public const string OutputSchemaJson = """
        {"type":"object","additionalProperties":false,"required":["verdict","findings","unverified"],
         "properties":{
          "verdict":{"type":"string","enum":["PASS","CHANGES_REQUESTED","BLOCKED"]},
          "findings":{"type":"array","items":{"type":"object","additionalProperties":false,
            "required":["severity","file","line","evidence","fix"],
            "properties":{"severity":{"type":"string","enum":["blocker","required","optional"]},
              "file":{"type":"string"},"line":{"type":"integer"},"evidence":{"type":"string"},"fix":{"type":"string"}}}},
          "unverified":{"type":"array","items":{"type":"string"}}}}
        """;

    /// <summary>
    /// Dựng lời nhắn cho reviewer. Bắt buộc có: vai trò CHỈ ĐỌC + yêu cầu trả đúng lược đồ; các mục task
    /// "Tóm tắt", "Mục tiêu", "Phạm vi", "Tiêu chí", "Test nghiệm thu", "Báo cáo của agent";
    /// đoạn thiết kế (nếu có); báo cáo cổng kiểm tra (nếu có); diff (cắt nếu quá MaxDiffChars, ghi rõ "[ĐÃ CẮT …]").
    /// KHÔNG đưa mục "Review (Claude)" cũ (tránh reviewer chép kết luận cũ).
    /// DiffHash = SHA-256 hex chữ thường của DiffText ĐẦY ĐỦ (trước khi cắt).
    /// </summary>
    public ReviewPack BuildPack(ReviewPackInput input)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>
    /// Đọc JSON reviewer trả về. JSON hỏng / thiếu verdict / verdict lạ / severity lạ ⇒ null (người gọi coi là Blocked).
    /// Không phân biệt hoa thường cho giá trị enum. Bỏ qua ký tự rác quanh JSON (ví dụ ```json … ```).
    /// </summary>
    public static (ReviewVerdict Verdict, IReadOnlyList<ReviewFinding> Findings, IReadOnlyList<string> Unverified)? ParseOutput(string text)
    {
        throw new NotImplementedException("TODO(agent)");
    }

    /// <summary>
    /// Chạy review bằng <paramref name="reviewer"/> ở chế độ ReadOnly với lược đồ (ghi OutputSchemaJson ra file tạm trong logDir).
    /// Luật (fail-safe):
    /// - reviewer.Id trùng <paramref name="developerProviderId"/> (không phân biệt hoa thường) ⇒ Blocked "tự duyệt", KHÔNG gọi provider;
    /// - reviewer không có sandbox chỉ đọc (HasReadOnlySandbox = false) ⇒ Blocked, KHÔNG gọi provider;
    /// - provider trả !Ok (TimedOut, QuotaExhausted, NotLoggedIn, ConfigError, Unavailable, Failed) ⇒ Blocked, lý do chứa tên trạng thái;
    /// - ParseOutput trả null ⇒ Blocked "không đọc được kết luận";
    /// - verdict PASS mà có finding Blocker hoặc Required ⇒ đổi thành ChangesRequested (kết luận phải khớp nội dung);
    /// - ghi kết quả (JSON, có DiffHash, provider, model, token, thời gian) ra logDir/review-{yyyyMMdd-HHmmss}.json.
    /// </summary>
    public async Task<ReviewResult> ReviewAsync(ReviewPack pack, IAgentProvider reviewer, string developerProviderId,
        string workDir, string logDir, TimeSpan timeout, CancellationToken ct)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("TODO(agent)");
    }
}
