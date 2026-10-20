using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class InboxService : IInboxService
{
    private readonly HubPaths _paths;
    private static readonly Regex ItemRegex = new(
        @"^\s*-\s*\[(?<check>[ xX])\]\s*(?:(?<time>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2})\s*·\s*)?(?:(?<proj>[^:\r\n]+):\s*)?(?<content>.*)$",
        RegexOptions.Compiled);

    public InboxService(HubPaths paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<InboxItem> GetItems()
    {
        var items = new List<InboxItem>();
        if (!File.Exists(_paths.InboxFile))
        {
            return items;
        }

        string[] lines = File.ReadAllLines(_paths.InboxFile, Encoding.UTF8);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var match = ItemRegex.Match(line);
            if (match.Success)
            {
                bool done = match.Groups["check"].Value.Equals("x", StringComparison.OrdinalIgnoreCase);

                DateTime? time = null;
                string timeStr = match.Groups["time"].Value;
                if (!string.IsNullOrWhiteSpace(timeStr) &&
                    DateTime.TryParseExact(timeStr, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    time = dt;
                }

                string project = match.Groups["proj"].Value.Trim();
                string text = match.Groups["content"].Value.Trim();

                items.Add(new InboxItem(i + 1, time, project, text, done));
            }
        }

        return items;
    }

    public void Add(string project, string text)
    {
        // Gộp nội dung nhiều dòng thành một dòng
        string singleLine = Regex.Replace(text ?? "", @"[\r\n]+", " ").Trim();
        singleLine = Regex.Replace(singleLine, @"\s{2,}", " ");

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        string newLine = $"- [ ] {timestamp} · {project.Trim()}: {singleLine}";

        string? dir = Path.GetDirectoryName(_paths.InboxFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(_paths.InboxFile))
        {
            string initialContent = "# Hộp thư yêu cầu cho team\r\n\r\n" + newLine + "\r\n";
            File.WriteAllText(_paths.InboxFile, initialContent, new UTF8Encoding(false));
        }
        else
        {
            string existing = File.ReadAllText(_paths.InboxFile, Encoding.UTF8);
            var sb = new StringBuilder();
            sb.Append(existing);
            if (!existing.EndsWith("\n") && !existing.EndsWith("\r"))
            {
                sb.AppendLine();
            }
            sb.AppendLine(newLine);
            File.WriteAllText(_paths.InboxFile, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
