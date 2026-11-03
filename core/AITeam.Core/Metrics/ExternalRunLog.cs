using System.Globalization;
using System.Text.Json;

namespace AITeam.Core.Metrics;

/// <summary>Số đo tối thiểu của một lượt chạy trực tiếp (ngoài assign); không chứa prompt hay nội dung file.</summary>
public sealed record ExternalRunRecord(
    int DurationSec,
    bool TokensKnown,
    long? InputTokens,
    long? OutputTokens,
    long? TotalTokens,
    string Status,
    string At,
    bool HasOutput);

/// <summary>Đọc log OpenCode (JSON Lines) và Codex (văn bản) để lấy số đo lượt chạy.</summary>
public static class ExternalRunLog
{
    /// <summary>Dòng dài hơn 1 MB bị bỏ qua (log thật có thể chứa cả nội dung file).</summary>
    public const int MaxLineLength = 1024 * 1024;

    public static ExternalRunRecord Parse(string dev, string logPath)
    {
        if (string.IsNullOrWhiteSpace(dev) ||
            (!dev.Equals("opencode", StringComparison.OrdinalIgnoreCase) && !dev.Equals("codex", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Chỉ đọc log của opencode hoặc codex.", nameof(dev));
        if (!File.Exists(logPath)) throw new FileNotFoundException("Không thấy file log.", logPath);
        return dev.Equals("opencode", StringComparison.OrdinalIgnoreCase) ? ParseOpenCode(logPath) : ParseCodex(logPath);
    }

    private static ExternalRunRecord ParseOpenCode(string path)
    {
        long? first = null;
        long? last = null;
        long input = 0;
        long output = 0;
        var sawStepFinish = false;
        var hasText = false;

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length > MaxLineLength) continue;
            JsonDocument document;
            try { document = JsonDocument.Parse(line); }
            catch (JsonException) { continue; }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;

                if (root.TryGetProperty("timestamp", out var stamp) &&
                    stamp.ValueKind == JsonValueKind.Number && stamp.TryGetInt64(out var milliseconds))
                {
                    if (first is null || milliseconds < first) first = milliseconds;
                    if (last is null || milliseconds > last) last = milliseconds;
                }

                if (!root.TryGetProperty("type", out var typeValue) || typeValue.ValueKind != JsonValueKind.String) continue;
                var type = typeValue.GetString();
                if (!root.TryGetProperty("part", out var part) || part.ValueKind != JsonValueKind.Object) continue;

                if (type == "step_finish" &&
                    part.TryGetProperty("tokens", out var tokens) && tokens.ValueKind == JsonValueKind.Object)
                {
                    sawStepFinish = true;
                    input += TokenValue(tokens, "input");
                    output += TokenValue(tokens, "output") + TokenValue(tokens, "reasoning");
                }
                else if (type == "text" &&
                         part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String &&
                         !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    hasText = true;
                }
            }
        }

        var duration = first.HasValue && last.HasValue ? (int)Math.Max(0, (last.Value - first.Value) / 1000) : 0;
        var at = last.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(last.Value).ToLocalTime().ToString("s", CultureInfo.InvariantCulture)
            : File.GetLastWriteTime(path).ToString("s", CultureInfo.InvariantCulture);
        return new ExternalRunRecord(
            duration,
            sawStepFinish,
            sawStepFinish ? input : null,
            sawStepFinish ? output : null,
            sawStepFinish ? input + output : null,
            hasText ? "SUCCESS" : "KHÔNG CÓ KẾT QUẢ",
            at,
            hasText);
    }

    private static ExternalRunRecord ParseCodex(string path)
    {
        var hasCodexLine = false;
        var hasOutput = false;
        var tokensKnown = false;
        long? total = null;
        var previousWasTokensUsed = false;

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length > MaxLineLength) { previousWasTokensUsed = false; continue; }
            var text = line.Trim();
            if (text.Length == 0) continue;

            if (previousWasTokensUsed)
            {
                if (TryParseGroupedNumber(text, out var value)) { total = value; tokensKnown = true; }
                previousWasTokensUsed = false;
                continue;
            }

            if (text == "tokens used") { previousWasTokensUsed = true; continue; }
            if (text == "codex") { hasCodexLine = true; continue; }
            if (hasCodexLine) hasOutput = true;
        }

        // Log `codex exec` không có mốc bắt đầu, nên lấy chênh lệch thời gian tạo/ghi cuối của file làm thời lượng.
        var info = new FileInfo(path);
        var duration = (int)Math.Max(0, (info.LastWriteTime - info.CreationTime).TotalSeconds);
        return new ExternalRunRecord(
            duration,
            tokensKnown,
            null,
            null,
            tokensKnown ? total : null,
            hasOutput ? "SUCCESS" : "KHÔNG CÓ KẾT QUẢ",
            info.LastWriteTime.ToString("s", CultureInfo.InvariantCulture),
            hasOutput);
    }

    private static long TokenValue(JsonElement tokens, string name)
    {
        if (!tokens.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number) return 0;
        return value.TryGetInt64(out var number) && number > 0 ? number : 0;
    }

    private static bool TryParseGroupedNumber(string text, out long value)
    {
        value = 0;
        var digits = text.Replace(",", "", StringComparison.Ordinal);
        if (digits.Length == 0) return false;
        foreach (var character in digits)
            if (character < '0' || character > '9') return false;
        return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
