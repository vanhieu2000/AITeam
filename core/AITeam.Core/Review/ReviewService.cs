using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AITeam.Core.Providers;
using AITeam.Core.Tasks;

namespace AITeam.Core.Review;

/// <summary>
/// Review độc lập bằng provider khác provider đã viết code (thiết kế: docs/design/V4-MULTI-AI.md mục B5).
/// KHUNG do Claude viết — agent chỉ điền phần thân (TODO(agent)), giữ nguyên chữ ký public.
/// </summary>
public sealed class ReviewService
{
    private static readonly JsonSerializerOptions SaveJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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
    private static readonly UTF8Encoding NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public ReviewPack BuildPack(ReviewPackInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var task = TaskFile.Parse("task.md", input.TaskFileText ?? "");
        var fullDiff = input.DiffText ?? "";
        var diffHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullDiff))).ToLowerInvariant();

        var maxChars = Math.Max(0, input.MaxDiffChars);
        var truncated = fullDiff.Length > maxChars;
        var diffBody = truncated
            ? $"{fullDiff[..maxChars]}\n[ĐÃ CẮT {fullDiff.Length - maxChars} ký tự]"
            : fullDiff;

        var sb = new StringBuilder();
        sb.AppendLine("# Yêu cầu Review Độc Lập (CHỈ ĐỌC)");
        sb.AppendLine("Bạn đang ở vai trò Reviewer CHỈ ĐỌC (ReadOnly). Tuyệt đối không sửa file. Hãy đối chiếu diff với tiêu chí task và trả lời đúng lược đồ JSON yêu cầu.");
        sb.AppendLine();
        sb.AppendLine($"## Task: {task.Id} · {task.Title}");

        AppendSection(sb, "Tóm tắt", task.Section("Tóm tắt"));
        AppendSection(sb, "Mục tiêu", task.Section("Mục tiêu"));
        AppendSection(sb, "Phạm vi", task.Section("Phạm vi"));
        AppendSection(sb, "Tiêu chí hoàn thành", task.Section("Tiêu chí"));
        AppendSection(sb, "Test nghiệm thu", task.Section("Test nghiệm thu"));
        AppendSection(sb, "Báo cáo của agent", task.Section("Báo cáo của agent"));

        if (!string.IsNullOrWhiteSpace(input.DesignExcerpt))
            AppendSection(sb, "Thiết kế liên quan", input.DesignExcerpt);

        if (!string.IsNullOrWhiteSpace(input.VerifyReport))
            AppendSection(sb, "Báo cáo cổng kiểm tra", input.VerifyReport);

        sb.AppendLine("## Diff");
        sb.AppendLine("```diff");
        sb.AppendLine(diffBody);
        sb.AppendLine("```");

        return new ReviewPack(sb.ToString(), diffHash, truncated);
    }

    private static void AppendSection(StringBuilder sb, string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        sb.AppendLine($"### {title}");
        sb.AppendLine(content.Trim());
        sb.AppendLine();
    }

    /// <summary>
    /// Đọc JSON reviewer trả về. JSON hỏng / thiếu verdict / verdict lạ / severity lạ ⇒ null (người gọi coi là Blocked).
    /// Không phân biệt hoa thường cho giá trị enum. Bỏ qua ký tự rác quanh JSON (ví dụ ```json … ```).
    /// </summary>
    public static (ReviewVerdict Verdict, IReadOnlyList<ReviewFinding> Findings, IReadOnlyList<string> Unverified)? ParseOutput(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var doc = JsonDocument.Parse(text.Substring(start, end - start + 1));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            if (!TryGetPropertyIgnoreCase(root, "verdict", out var verdictEl) || verdictEl.ValueKind != JsonValueKind.String)
                return null;

            var verdictStr = verdictEl.GetString()?.Trim().ToUpperInvariant();
            ReviewVerdict verdict = verdictStr switch
            {
                "PASS" => ReviewVerdict.Pass,
                "CHANGES_REQUESTED" or "CHANGESREQUESTED" => ReviewVerdict.ChangesRequested,
                "BLOCKED" => ReviewVerdict.Blocked,
                _ => (ReviewVerdict)(-1)
            };
            if ((int)verdict < 0) return null;

            var findings = new List<ReviewFinding>();
            if (TryGetPropertyIgnoreCase(root, "findings", out var findingsEl))
            {
                if (findingsEl.ValueKind != JsonValueKind.Array) return null;
                foreach (var item in findingsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) return null;
                    if (!TryGetPropertyIgnoreCase(item, "severity", out var sevEl) || sevEl.ValueKind != JsonValueKind.String)
                        return null;

                    var sevStr = sevEl.GetString()?.Trim().ToUpperInvariant();
                    FindingSeverity severity = sevStr switch
                    {
                        "BLOCKER" => FindingSeverity.Blocker,
                        "REQUIRED" => FindingSeverity.Required,
                        "OPTIONAL" => FindingSeverity.Optional,
                        _ => (FindingSeverity)(-1)
                    };
                    if ((int)severity < 0) return null;

                    var file = TryGetPropertyIgnoreCase(item, "file", out var fileEl) && fileEl.ValueKind == JsonValueKind.String
                        ? fileEl.GetString() ?? ""
                        : "";
                    var line = TryGetPropertyIgnoreCase(item, "line", out var lineEl) && lineEl.ValueKind == JsonValueKind.Number && lineEl.TryGetInt32(out var l)
                        ? l
                        : 0;
                    var evidence = TryGetPropertyIgnoreCase(item, "evidence", out var evEl) && evEl.ValueKind == JsonValueKind.String
                        ? evEl.GetString() ?? ""
                        : "";
                    var fix = TryGetPropertyIgnoreCase(item, "fix", out var fixEl) && fixEl.ValueKind == JsonValueKind.String
                        ? fixEl.GetString() ?? ""
                        : "";

                    findings.Add(new ReviewFinding(severity, file, line, evidence, fix));
                }
            }

            var unverified = new List<string>();
            if (TryGetPropertyIgnoreCase(root, "unverified", out var unvEl))
            {
                if (unvEl.ValueKind != JsonValueKind.Array) return null;
                foreach (var item in unvEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
                        unverified.Add(s);
                }
            }

            return (verdict, findings, unverified);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.TryGetProperty(name, out value)) return true;
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
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
        Directory.CreateDirectory(logDir);

        if (string.Equals(reviewer.Id, developerProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return SaveResult(logDir, new ReviewResult(
                ReviewVerdict.Blocked, [], [], reviewer.Id, null, TimeSpan.Zero, TokenUsage.Unknown, pack.DiffHash,
                $"Chặn tự duyệt: reviewer '{reviewer.Id}' trùng developer '{developerProviderId}'."));
        }

        if (!reviewer.HasReadOnlySandbox)
        {
            return SaveResult(logDir, new ReviewResult(
                ReviewVerdict.Blocked, [], [], reviewer.Id, null, TimeSpan.Zero, TokenUsage.Unknown, pack.DiffHash,
                $"Provider '{reviewer.Id}' không hỗ trợ sandbox chỉ đọc (HasReadOnlySandbox = false)."));
        }

        var promptFile = Path.Combine(logDir, "prompt-review.md");
        var schemaFile = Path.Combine(logDir, "schema-review.json");
        // UTF-8 KHÔNG BOM: Codex từ chối lược đồ có BOM ("not valid JSON: expected value at line 1 column 1") — phát hiện khi chạy thật 23/10.
        await File.WriteAllTextAsync(promptFile, pack.Prompt, NoBom, ct);
        await File.WriteAllTextAsync(schemaFile, OutputSchemaJson, NoBom, ct);

        var request = new AgentRunRequest(
            WorkDir: workDir,
            PromptFile: promptFile,
            Mode: RunMode.ReadOnly,
            Timeout: timeout,
            Model: null,
            OutputSchemaFile: schemaFile,
            LogDir: logDir);

        var runResult = await reviewer.RunAsync(request, ct);
        if (!runResult.Ok)
        {
            return SaveResult(logDir, new ReviewResult(
                ReviewVerdict.Blocked, [], [], reviewer.Id, runResult.Model, runResult.Duration, runResult.Tokens, pack.DiffHash,
                $"Provider lỗi ({runResult.Status}): {runResult.Detail}"));
        }

        var parsed = ParseOutput(runResult.Text);
        if (parsed is null)
        {
            return SaveResult(logDir, new ReviewResult(
                ReviewVerdict.Blocked, [], [], reviewer.Id, runResult.Model, runResult.Duration, runResult.Tokens, pack.DiffHash,
                "Không đọc được kết luận review từ phản hồi của provider."));
        }

        var (verdict, findings, unverified) = parsed.Value;
        if (verdict == ReviewVerdict.Pass && findings.Any(f => f.Severity is FindingSeverity.Blocker or FindingSeverity.Required))
        {
            verdict = ReviewVerdict.ChangesRequested;
        }

        var blockedReason = verdict == ReviewVerdict.Blocked ? "Reviewer trả kết luận BLOCKED." : "";
        return SaveResult(logDir, new ReviewResult(
            verdict, findings, unverified, reviewer.Id, runResult.Model, runResult.Duration, runResult.Tokens, pack.DiffHash, blockedReason));
    }

    private static ReviewResult SaveResult(string logDir, ReviewResult result)
    {
        var path = Path.Combine(logDir, $"review-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(result, SaveJsonOptions), Encoding.UTF8);
        return result;
    }
}
