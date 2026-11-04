namespace AITeam.Hub.Services;

// F1-06 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-06.

/// <summary>Một dòng task trong bảng việc tasks/README.md.</summary>
public sealed record BoardRow(string Id, string Title, string File, string Agent, string Branch, string Status, int Line);

/// <summary>Bộ đọc bảng việc dùng chung: chịu dòng trống giữa bảng, dòng thiếu "|" cuối, cột theo tên tiêu đề.</summary>
public static class BoardTable
{
    public static IReadOnlyList<BoardRow> Parse(string? markdown)
    {
        var rows = new List<BoardRow>();
        if (string.IsNullOrEmpty(markdown)) return rows;
        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int start = -1, task = -1, agent = -1, branch = -1, status = -1;
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('|')) continue;
            var headers = Cells(line);
            int index = Array.FindIndex(headers, h => h.Equals("Task", StringComparison.OrdinalIgnoreCase));
            var divider = lines[i + 1].Trim();
            if (index < 0 || !divider.StartsWith('|') || !divider.Contains('-')
                || divider.Any(c => c != '-' && c != ':' && c != '|' && !char.IsWhiteSpace(c))) continue;
            task = index;
            agent = Array.FindIndex(headers, h => h.Equals("Agent", StringComparison.OrdinalIgnoreCase));
            branch = Array.FindIndex(headers, h => h.Contains("Nhánh", StringComparison.OrdinalIgnoreCase)
                || h.Contains("Branch", StringComparison.OrdinalIgnoreCase));
            status = Array.FindIndex(headers, h => h.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase)
                || h.Contains("Status", StringComparison.OrdinalIgnoreCase));
            start = i + 2;
            break;
        }
        if (start < 0) return rows;
        for (int i = start; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;
            if (!line.StartsWith('|')) break;
            var cells = Cells(line);
            string Get(int column) => column >= 0 && column < cells.Length ? cells[column] : "";
            var text = Get(task);
            string file = "";
            var link = System.Text.RegularExpressions.Regex.Match(text, @"\[([^\]]+)\]\(([^)]+)\)",
                System.Text.RegularExpressions.RegexOptions.NonBacktracking);
            if (link.Success)
            {
                text = link.Groups[1].Value.Trim();
                var target = link.Groups[2].Value.Trim();
                // Tên file hỏng không được làm gián đoạn các dòng còn lại.
                if (!target.Contains('\0')) file = Path.GetFileName(target);
            }
            var id = System.Text.RegularExpressions.Regex.Match(text, @"^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9]*",
                System.Text.RegularExpressions.RegexOptions.NonBacktracking);
            if (!id.Success || !ids.Add(id.Value)) continue;
            int titleStart = id.Length;
            while (titleStart < text.Length && (char.IsWhiteSpace(text[titleStart])
                || "·-:–—".Contains(text[titleStart]))) titleStart++;
            var title = text[titleStart..].Trim();
            rows.Add(new BoardRow(id.Value, title, file, Get(agent).Trim('`', ' '),
                Get(branch).Replace("`", "").Trim(), Get(status), i + 1));
        }
        return rows;

        static string[] Cells(string line)
        {
            var content = line[1..];
            if (content.EndsWith('|')) content = content[..^1];
            return content.Split('|').Select(c => c.Trim()).ToArray();
        }
    }
}
