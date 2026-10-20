using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace AITeam.Hub.Pages.Shared;

/// <summary>
/// Bộ chuyển đổi Markdown sang HTML gọn nhẹ, an toàn XSS (Mã hóa HTML ký tự đặc biệt trước khi phân tích cú pháp).
/// Hỗ trợ: Heading, In đậm, In nghiêng, Code inline/block, Danh sách, Checkbox, Link, Bảng, Trích dẫn.
/// Không sử dụng thư viện ngoài.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new(@"\*([^\*]+?)\*", RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+?)`", RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Mã hóa các ký tự điều khiển HTML nguy hiểm (&, &lt;, &gt;, &quot;, &#39;) chống tấn công XSS,
    /// đồng thời giữ nguyên ký tự tiếng Việt có dấu.
    /// </summary>
    public static string EscapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    public static HtmlString Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return HtmlString.Empty;
        }

        // Bước 1: HTML Escape toàn bộ văn bản để chống tuyệt đối tấn công XSS
        var encoded = EscapeHtml(markdown);

        // Chuẩn hóa ký tự xuống dòng
        var lines = encoded.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var sb = new StringBuilder();

        var inCodeBlock = false;
        var codeBlockLang = string.Empty;
        var codeBlockContent = new StringBuilder();

        var inList = false;
        var isOrderedList = false;

        var inTable = false;
        var tableHeaders = new List<string>();
        var tableRows = new List<List<string>>();

        void CloseList()
        {
            if (inList)
            {
                sb.AppendLine(isOrderedList ? "</ol>" : "</ul>");
                inList = false;
                isOrderedList = false;
            }
        }

        void CloseTable()
        {
            if (inTable)
            {
                sb.AppendLine("<div class=\"table-container\"><table class=\"markdown-table\">");
                if (tableHeaders.Count > 0)
                {
                    sb.AppendLine("<thead><tr>");
                    foreach (var h in tableHeaders)
                    {
                        sb.Append("<th>").Append(RenderInline(h.Trim())).AppendLine("</th>");
                    }
                    sb.AppendLine("</tr></thead>");
                }
                if (tableRows.Count > 0)
                {
                    sb.AppendLine("<tbody>");
                    foreach (var row in tableRows)
                    {
                        sb.AppendLine("<tr>");
                        foreach (var cell in row)
                        {
                            sb.Append("<td>").Append(RenderInline(cell.Trim())).AppendLine("</td>");
                        }
                        sb.AppendLine("</tr>");
                    }
                    sb.AppendLine("</tbody>");
                }
                sb.AppendLine("</table></div>");

                inTable = false;
                tableHeaders.Clear();
                tableRows.Clear();
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Xử lý Code Block: ``` hoặc ```lang
            if (trimmed.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    CloseList();
                    CloseTable();
                    inCodeBlock = true;
                    codeBlockLang = trimmed.Length > 3 ? trimmed[3..].Trim() : string.Empty;
                    codeBlockContent.Clear();
                }
                else
                {
                    inCodeBlock = false;
                    sb.Append("<pre class=\"code-block\"><code");
                    if (!string.IsNullOrEmpty(codeBlockLang))
                    {
                        sb.Append(" class=\"language-").Append(codeBlockLang).Append('\"');
                    }
                    sb.Append('>');
                    sb.Append(codeBlockContent.ToString().TrimEnd());
                    sb.AppendLine("</code></pre>");
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockContent.AppendLine(line);
                continue;
            }

            // Dòng trống: đóng danh sách, bảng, hoặc ngắt đoạn
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                CloseList();
                CloseTable();
                continue;
            }

            // Xử lý Bảng Markdown (| col1 | col2 |)
            if (trimmed.StartsWith('|') && trimmed.EndsWith('|') && trimmed.Length > 1)
            {
                CloseList();

                var isSeparator = IsTableSeparator(trimmed);
                if (!inTable)
                {
                    // Dòng đầu tiên của bảng là header
                    inTable = true;
                    tableHeaders = SplitTableRow(trimmed);
                }
                else if (isSeparator)
                {
                    // Dòng phân cách |---|---|
                    continue;
                }
                else
                {
                    var cells = SplitTableRow(trimmed);
                    tableRows.Add(cells);
                }
                continue;
            }
            else if (inTable)
            {
                CloseTable();
            }

            // Xử lý Heading (#, ##, ###, ...)
            if (trimmed.StartsWith('#'))
            {
                CloseList();
                CloseTable();

                var level = 0;
                while (level < trimmed.Length && trimmed[level] == '#')
                {
                    level++;
                }

                if (level <= 6 && level < trimmed.Length && trimmed[level] == ' ')
                {
                    var title = trimmed[(level + 1)..].Trim();
                    sb.Append("<h").Append(level).Append('>')
                      .Append(RenderInline(title))
                      .Append("</h").Append(level).AppendLine(">");
                    continue;
                }
            }

            // Xử lý Blockquote: > hoặc &gt; (do đã EscapeHtml)
            if (trimmed.StartsWith("&gt;"))
            {
                CloseList();
                CloseTable();
                var quoteText = trimmed[4..].TrimStart();
                sb.Append("<blockquote>").Append(RenderInline(quoteText)).AppendLine("</blockquote>");
                continue;
            }

            // Xử lý Danh sách không thứ tự & Checkbox (- hoặc *)
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                CloseTable();
                if (!inList || isOrderedList)
                {
                    CloseList();
                    inList = true;
                    isOrderedList = false;
                    sb.AppendLine("<ul>");
                }

                var itemContent = trimmed[2..].Trim();
                if (itemContent.StartsWith("[ ] ") || itemContent.StartsWith("[x] ") || itemContent.StartsWith("[X] "))
                {
                    var isChecked = itemContent.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase);
                    var text = itemContent[4..];
                    sb.Append("<li class=\"task-item\">")
                      .Append(isChecked ? "<input type=\"checkbox\" checked disabled /> " : "<input type=\"checkbox\" disabled /> ")
                      .Append(RenderInline(text))
                      .AppendLine("</li>");
                }
                else
                {
                    sb.Append("<li>").Append(RenderInline(itemContent)).AppendLine("</li>");
                }
                continue;
            }

            // Xử lý Danh sách có thứ tự (1. , 2. )
            var matchOrdered = Regex.Match(trimmed, @"^(\d+)\.\s+(.+)$");
            if (matchOrdered.Success)
            {
                CloseTable();
                if (!inList || !isOrderedList)
                {
                    CloseList();
                    inList = true;
                    isOrderedList = true;
                    sb.AppendLine("<ol>");
                }

                var itemContent = matchOrdered.Groups[2].Value;
                sb.Append("<li>").Append(RenderInline(itemContent)).AppendLine("</li>");
                continue;
            }

            // Dòng văn bản bình thường (Paragraph)
            CloseList();
            CloseTable();
            sb.Append("<p>").Append(RenderInline(trimmed)).AppendLine("</p>");
        }

        CloseList();
        CloseTable();

        return new HtmlString(sb.ToString().TrimEnd());
    }

    private static bool IsTableSeparator(string line)
    {
        var cells = SplitTableRow(line);
        if (cells.Count == 0) return false;
        foreach (var c in cells)
        {
            var cleaned = c.Trim().Replace(":", string.Empty).Replace("-", string.Empty);
            if (cleaned.Length > 0) return false;
        }
        return true;
    }

    private static List<string> SplitTableRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];

        var parts = trimmed.Split('|');
        var list = new List<string>(parts.Length);
        foreach (var p in parts)
        {
            list.Add(p);
        }
        return list;
    }

    /// <summary>
    /// Xử lý định dạng nội dòng (inline): Bold, Italic, Code, Link.
    /// Lưu ý chuỗi đầu vào đã qua EscapeHtml.
    /// </summary>
    public static string RenderInline(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Code inline: `code`
        text = InlineCodeRegex.Replace(text, m =>
        {
            return $"<code class=\"inline-code\">{m.Groups[1].Value}</code>";
        });

        // Bold: **text**
        text = BoldRegex.Replace(text, m =>
        {
            return $"<strong>{m.Groups[1].Value}</strong>";
        });

        // Italic: *text*
        text = ItalicRegex.Replace(text, m =>
        {
            return $"<em>{m.Groups[1].Value}</em>";
        });

        // Link: [text](url)
        text = LinkRegex.Replace(text, m =>
        {
            var linkText = m.Groups[1].Value;
            var url = m.Groups[2].Value.Trim();

            // Kiểm tra an toàn URL chống Javascript injection
            var lowerUrl = url.ToLowerInvariant();
            if (lowerUrl.StartsWith("http://") ||
                lowerUrl.StartsWith("https://") ||
                lowerUrl.StartsWith("/") ||
                lowerUrl.StartsWith("#") ||
                lowerUrl.StartsWith("mailto:"))
            {
                return $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{linkText}</a>";
            }

            // Nếu url không an toàn hoặc không hợp lệ, chỉ hiển thị text thường
            return linkText;
        });

        return text;
    }
}
