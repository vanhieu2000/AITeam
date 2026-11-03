using System.Text.RegularExpressions;
namespace AITeam.Core.Metrics;
public sealed record StructuredFinding(string Cause, string Kind, string Text, int? Round);
/// <summary>Định dạng chung cho Hub, staff-report và metrics; không bỏ nội dung của mã lạ.</summary>
public static class FindingParser
{
    public static readonly IReadOnlyList<string> Causes = Array.AsReadOnly(new[] { "TK", "TN", "AG", "CK", "HT", "HM", "CH", "TO", "DP" });
    public static string Normalize(string value) => Causes.Contains(value.ToUpperInvariant()) ? value.ToUpperInvariant() : "khác";
    public static IReadOnlyList<StructuredFinding> Parse(string? review)
    {
        var result = new List<StructuredFinding>(); int? round = null; bool fenced = false;
        var text = Regex.Replace(review ?? "", @"<!--[\s\S]*?-->", "");
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = !fenced; continue; }
            if (fenced) continue;
            var heading = Regex.Match(line, @"^\s*###\s+Vòng\s+(\d+)", RegexOptions.IgnoreCase);
            if (heading.Success && int.TryParse(heading.Groups[1].Value, out var n)) round = n;
            var match = Regex.Match(line, @"^\s*-\s*\[([\p{L}]{2,16})\]\s*(?:\(([^)]*)\)\s*)?(.+?)\s*$");
            if (!match.Success) continue;
            result.Add(new(Normalize(match.Groups[1].Value), match.Groups[2].Value.Trim(), match.Groups[3].Value, round));
        }
        return result;
    }
}
