using System.Text.RegularExpressions;

namespace AITeam.Core.Tasks;

/// <summary>File task Markdown (templates/task.md): các dòng "- **Khóa:** giá trị" và các mục "## Tiêu đề".</summary>
public sealed record TaskFile(
    string Id,
    string Title,
    string? Agent,
    string? Branch,
    string? Request,
    int? BudgetLines,
    int? MaxMinutes,
    string? Model,
    IReadOnlyDictionary<string, string> Sections)
{
    private static readonly Regex Header = new(@"^#\s+(?<id>[A-Z]+-\d+[\w-]*)\s*·\s*(?<title>.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    public static TaskFile Parse(string fileName, string text)
    {
        var h = Header.Match(text);
        var id = h.Success ? h.Groups["id"].Value : Path.GetFileNameWithoutExtension(fileName);
        return new TaskFile(
            id,
            h.Success ? h.Groups["title"].Value.Trim() : id,
            Field(text, "Agent"),
            Backticked(Field(text, "Nhánh")),
            Field(text, "Yêu cầu") is { } r && Regex.IsMatch(r, @"^R-\d+") ? Regex.Match(r, @"^R-\d+").Value : null,
            Number(Field(text, "Ngân sách")),
            Number(Field(text, "Thời gian tối đa")),
            Backticked(Field(text, "Model")),
            SplitSections(text));
    }

    public string? Section(string startsWith) =>
        Sections.FirstOrDefault(kv => kv.Key.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase)).Value;

    private static string? Field(string text, string key)
    {
        var m = Regex.Match(text, @"^\s*-\s*\*\*" + Regex.Escape(key) + @":\*\*\s*(?<v>.+?)\s*$", RegexOptions.Multiline);
        return m.Success ? m.Groups["v"].Value : null;
    }

    private static string? Backticked(string? v)
    {
        if (v == null) return null;
        var m = Regex.Match(v, "`([^`]+)`");
        return m.Success ? m.Groups[1].Value : v;
    }

    private static int? Number(string? v)
    {
        if (v == null) return null;
        var m = Regex.Match(v, @"\d+");
        return m.Success ? int.Parse(m.Value) : null;
    }

    private static Dictionary<string, string> SplitSections(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? title = null;
        var buf = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("## "))
            {
                if (title != null) result[title] = string.Join("\n", buf).Trim();
                title = line[3..].Trim();
                buf.Clear();
            }
            else if (title != null) buf.Add(line);
        }
        if (title != null) result[title] = string.Join("\n", buf).Trim();
        return result;
    }
}
