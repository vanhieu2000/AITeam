using System.Text.RegularExpressions;

namespace AITeam.Core.Lessons;

// F1-05 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-05.

/// <summary>Một bài học đọc từ file markdown (CLAUDE.md / AGENTS.md).</summary>
public sealed record ImportedLesson(string Title, string Body, string Source);

/// <summary>Kết quả nhập: số bài thêm mới, số bài bỏ qua vì đã có.</summary>
public sealed record LessonImportResult(int Added, int Skipped, IReadOnlyList<string> Warnings);

public static class LessonImporter
{
    private static readonly Regex SeparatorRegex = new(@"(:|\.)(?=\s|$)|→|⇒| — ", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Mỗi gạch đầu dòng cấp 1 trong mục "## Bài học…" / "## Lỗi hay gặp…" là 1 bài học.</summary>
    public static IReadOnlyList<ImportedLesson> Extract(string markdown, string source)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var items = new List<ImportedLesson>();
        bool inSection = false;
        string? currentFirstLine = null;
        var currentBodyLines = new List<string>();

        void FlushCurrent()
        {
            if (currentFirstLine is not null)
            {
                var title = ExtractTitle(currentFirstLine);
                var body = string.Join("\n", currentBodyLines);
                items.Add(new ImportedLesson(title, body, source));
                currentFirstLine = null;
                currentBodyLines.Clear();
            }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine;
            if (line.StartsWith('#'))
            {
                FlushCurrent();
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    var headingTitle = line[3..].Trim();
                    inSection = headingTitle.StartsWith("Bài học", StringComparison.OrdinalIgnoreCase)
                             || headingTitle.StartsWith("Lỗi hay gặp", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    inSection = false;
                }
                continue;
            }

            if (!inSection)
                continue;

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushCurrent();
                var firstLineContent = line[2..];
                currentFirstLine = firstLineContent;
                currentBodyLines.Add(firstLineContent);
            }
            else if (currentFirstLine is not null && (line.StartsWith(' ') || line.StartsWith('\t')))
            {
                currentBodyLines.Add(line.Trim());
            }
        }

        FlushCurrent();
        return items;
    }

    private static string ExtractTitle(string firstLine)
    {
        var text = firstLine.Replace("**", "").Trim();
        if (text.Length == 0) return "";

        var match = SeparatorRegex.Match(text);
        if (match.Success)
        {
            text = text[..match.Index];
        }

        text = text.Trim();
        if (text.Length > 120)
        {
            text = text[..120];
        }
        return text;
    }

    /// <summary>Thêm bản nháp cho bài chưa có (so tiêu đề đã chuẩn hóa với mọi bài học hiện có).</summary>
    public static LessonImportResult Import(ILessonService lessons, IEnumerable<ImportedLesson> items)
    {
        var existingTitles = new HashSet<string>(
            lessons.Lessons().Select(l => NormalizeTitle(l.Title)),
            StringComparer.OrdinalIgnoreCase
        );

        int added = 0;
        int skipped = 0;
        var warnings = new List<string>();

        foreach (var item in items)
        {
            var normTitle = NormalizeTitle(item.Title);
            if (existingTitles.Contains(normTitle))
            {
                skipped++;
                continue;
            }

            var fullBody = string.IsNullOrEmpty(item.Source) ? item.Body : $"{item.Body}\n\nNguồn: {item.Source}";
            if (fullBody.Length > 4000)
            {
                fullBody = fullBody[..4000];
            }

            lessons.AddDraft(item.Title, fullBody, "meo", null, null, null, null);
            existingTitles.Add(normTitle);
            added++;
        }

        return new LessonImportResult(added, skipped, warnings);
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        return WhitespaceRegex.Replace(title.Trim(), " ");
    }
}