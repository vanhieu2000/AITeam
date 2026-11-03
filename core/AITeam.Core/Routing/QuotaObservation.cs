using System.Text.Json;

namespace AITeam.Core.Routing;

/// <summary>Số đo quota phải ghi nguồn; null là chưa biết, không phải 0.</summary>
public sealed record QuotaObservation(decimal? UsedPercent, int? WindowMinutes, DateTime? ResetsAt, string Source, DateTime? ExhaustsAt)
{
    public static QuotaObservation Unknown => new(null, null, null, "unknown", null);
    public void Validate()
    {
        if (Source is not ("provider" or "estimate" or "unknown") || UsedPercent is < 0 or > 100 || WindowMinutes is <= 0)
            throw new ArgumentException("Số đo hạn mức không hợp lệ.");
        if (Source == "unknown" && (UsedPercent != null || ResetsAt != null || ExhaustsAt != null))
            throw new ArgumentException("Hạn mức chưa biết không được có số liệu suy đoán.");
    }
}

public sealed record QuotaPolicy(decimal LimitUsd, int WindowMinutes)
{
    /// <summary>team.json: quotas.provider = [{limitUsd, windowMinutes}, ...]. Không có khai báo thì không tự đoán gói.</summary>
    public static IReadOnlyList<QuotaPolicy> Load(string root, string provider)
    {
        var file = Path.Combine(root, "team.json");
        if (!File.Exists(file)) return [];
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        if (!doc.RootElement.TryGetProperty("quotas", out var quotas) || !quotas.TryGetProperty(provider, out var windows)) return [];
        if (windows.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Cấu hình quotas phải là danh sách cửa sổ.");
        var result = new List<QuotaPolicy>();
        foreach (var w in windows.EnumerateArray())
        {
            if (!w.TryGetProperty("limitUsd", out var amount) || !amount.TryGetDecimal(out var usd) || usd <= 0
                || !w.TryGetProperty("windowMinutes", out var minutes) || !minutes.TryGetInt32(out var span) || span <= 0)
                throw new InvalidDataException("Cửa sổ quotas cần limitUsd và windowMinutes dương.");
            result.Add(new(usd, span));
        }
        return result;
    }
}

public static class QuotaEstimator
{
    /// <summary>Cùng đơn vị USD; chi tiêu thiếu phải unknown. Chỉ dự báo hết trước khi cửa sổ hồi, không giả số dư provider.</summary>
    public static QuotaObservation Estimate(decimal limitUsd, decimal? spentUsd, int windowMinutes, DateTime resetsAt, decimal? spentLast24HoursUsd, DateTime now)
    {
        if (limitUsd <= 0 || spentUsd is null or < 0 || windowMinutes <= 0 || resetsAt <= now) return QuotaObservation.Unknown;
        var percent = Math.Min(100m, spentUsd.Value / limitUsd * 100m);
        DateTime? exhausts = null;
        if (spentLast24HoursUsd is > 0)
        {
            var hours = Math.Max(0m, limitUsd - spentUsd.Value) / spentLast24HoursUsd.Value * 24m;
            if (hours <= (decimal)(DateTime.MaxValue - now).TotalHours)
            {
                var forecast = now.AddHours((double)hours);
                if (forecast <= resetsAt) exhausts = forecast;
            }
        }
        return new(percent, windowMinutes, resetsAt, "estimate", exhausts);
    }
}
