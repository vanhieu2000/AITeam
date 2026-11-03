using System.Text.RegularExpressions;
namespace AITeam.Core.Tasks;
/// <summary>Cho phép thay harness có khai báo; vẫn chặn assert và thuộc tính chọn/bỏ test.</summary>
public static class AcceptanceHarnessPolicy
{
    public static bool Allows(string diff, bool declared)
    {
        if (!declared || string.IsNullOrWhiteSpace(diff)) return false;
        var changed = diff.Split('\n').Where(l => l.Length > 0 && l[0] is '+' or '-' && !l.StartsWith("+++", StringComparison.Ordinal) && !l.StartsWith("---", StringComparison.Ordinal)).Select(l => l[1..]).ToList();
        if (changed.Count == 0) return false;
        var text = string.Join('\n', changed);
        return !Regex.IsMatch(text, @"\bAssert\s*\.|\[(?:Fact|Theory|InlineData|MemberData|ClassData)\b|\bSkip\s*=|\b(?:Need|Expect-Error)\s", RegexOptions.CultureInvariant);
    }
}
