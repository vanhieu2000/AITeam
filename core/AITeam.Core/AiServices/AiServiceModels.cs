namespace AITeam.Core.AiServices;

// Danh bạ dịch vụ AI cục bộ (R-014 / V10-01). Thiết kế: docs/design/R-014-van-phong-ai.md mục 4.6, 5, 7, 8.
// Hợp đồng do Claude chốt — KHÔNG đổi tên / kiểu / thứ tự tham số.

public enum AiAdapter { CliJson, CliText, OpenAiCompatibleHttp }

public enum AiRole { Code, Qc, Research, Chat }

public sealed record AiService(
    string Id, string Name, AiAdapter Adapter,
    string? Command, IReadOnlyList<string> Args, string? BaseUrl,
    IReadOnlyList<string> Models, IReadOnlyList<AiRole> Roles,
    int MaxJobs, bool Paid, string? SecretKeyName, string? ManageUrl,
    bool Enabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record AiAccount(string Id, string ServiceId, string Alias, string Mode, string? LoginState, DateTimeOffset? CheckedAt);

public sealed record AiQuota(string ServiceId, string? AccountId, double? RemainingPct, DateTimeOffset? RestUntil, string Source, DateTimeOffset ObservedAt);

public sealed record AiServiceValidation(bool Ok, IReadOnlyList<string> Errors);

public sealed record IniImportResult(int Added, int Updated, IReadOnlyList<string> Skipped);

public interface IAiServiceRegistry
{
    string Location { get; }

    IReadOnlyList<AiService> List(bool includeDisabled = true);

    AiService? Get(string id);

    AiServiceValidation Validate(AiService service);

    AiService Upsert(AiService service);

    void SetEnabled(string id, bool enabled);

    IReadOnlyList<AiAccount> Accounts(string? serviceId = null);

    AiAccount UpsertAccount(AiAccount account);

    IReadOnlyList<AiQuota> LatestQuotas();

    void RecordQuota(AiQuota quota);

    string? GetSetting(string key);

    void SetSetting(string key, string? value);

    string ExportIni();

    IniImportResult ImportIni(string text);
}

/// <summary>Chuyển enum ↔ chuỗi lưu trong SQLite / .ini.</summary>
internal static class AiServiceText
{
    public static string ToStorage(this AiAdapter adapter) => adapter switch
    {
        AiAdapter.CliJson => "cli-json",
        AiAdapter.CliText => "cli-text",
        _ => "openai-compatible-http",
    };

    public static string ToStorage(this AiRole role) => role switch
    {
        AiRole.Code => "code",
        AiRole.Qc => "qc",
        AiRole.Research => "research",
        _ => "chat",
    };

    public static bool TryParseAdapter(string? raw, out AiAdapter adapter)
    {
        adapter = raw?.Trim().ToLowerInvariant() switch
        {
            "cli-json" => AiAdapter.CliJson,
            "cli-text" => AiAdapter.CliText,
            "openai-compatible-http" => AiAdapter.OpenAiCompatibleHttp,
            _ => (AiAdapter)(-1),
        };
        return (int)adapter >= 0;
    }

    public static bool TryParseRole(string? raw, out AiRole role)
    {
        var value = raw?.Trim().ToLowerInvariant();
        foreach (var candidate in Enum.GetValues<AiRole>())
        {
            if (string.Equals(value, candidate.ToStorage(), StringComparison.Ordinal))
            {
                role = candidate;
                return true;
            }
        }
        role = default;
        return false;
    }
}
