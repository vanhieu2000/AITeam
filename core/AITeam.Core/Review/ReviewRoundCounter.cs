using System.Text.Json;

namespace AITeam.Core.Review;

/// <summary>Đếm vòng review từ nội dung đã nạp; không đọc file, cấu hình hay gọi provider.</summary>
public static class ReviewRoundCounter
{
    /// <summary>
    /// Trả số tiêu đề cấp 3 "### Vòng N" (N là dãy chữ số ASCII, có thể có hậu tố)
    /// trong mục "## Review (Claude)" đầu tiên đến tiêu đề cấp 2 tiếp theo, cộng số JSON
    /// có thuộc tính gốc verdict dạng chuỗi đúng "CHANGES_REQUESTED" (phân biệt hoa thường).
    /// Đếm từng tiêu đề, kể cả N trùng/không liên tiếp và mọi kết luận; không lấy max N.
    /// Bỏ văn bản thường, tiêu đề cấp khác, dòng thụt >= 4 dấu cách/tab và khối mã
    /// rào bằng >= 3 dấu backtick hoặc dấu ngã (rào đóng cùng loại, dài >= rào mở).
    /// Chấp nhận UTF-8 BOM đầu nội dung, LF/CRLF; null/rỗng ở mỗi nguồn đóng góp 0.
    /// JSON hỏng, gốc không phải object, thiếu/sai kiểu verdict hoặc verdict khác đóng góp 0
    /// riêng từng log, không làm mất log tốt. Không phát thông báo lỗi, không sửa đầu vào.
    /// </summary>
    public static int Count(string? taskContent, IEnumerable<string?>? reviewLogs)
    {
        int rounds = 0;

        if (!string.IsNullOrEmpty(taskContent))
        {
            rounds += CountMarkdownRounds(taskContent);
        }

        if (reviewLogs != null)
        {
            rounds += CountLogRounds(reviewLogs);
        }

        return rounds;
    }

    private static int CountMarkdownRounds(string content)
    {
        if (content.StartsWith('\uFEFF'))
        {
            content = content.Substring(1);
        }

        using var reader = new StringReader(content);
        string? line;
        bool inCodeBlock = false;
        char fenceChar = '\0';
        int fenceLength = 0;
        int reviewSectionState = 0; // 0: chưa vào, 1: đang trong review, 2: đã xong
        int rounds = 0;

        while ((line = reader.ReadLine()) != null)
        {
            // Kiểm tra rào đóng khi đang ở trong khối mã
            if (inCodeBlock)
            {
                // Rào đóng không được thụt >= 4 space hoặc tab
                if (line.StartsWith('\t') || line.StartsWith("    "))
                {
                    continue;
                }

                int indent = 0;
                while (indent < line.Length && line[indent] == ' ') indent++;
                if (indent < line.Length && line[indent] == fenceChar)
                {
                    int count = 0;
                    while (indent + count < line.Length && line[indent + count] == fenceChar) count++;
                    if (count >= fenceLength)
                    {
                        // Sau rào đóng chỉ được chứa khoảng trắng
                        bool onlyWhitespace = true;
                        for (int i = indent + count; i < line.Length; i++)
                        {
                            if (line[i] != ' ' && line[i] != '\t')
                            {
                                onlyWhitespace = false;
                                break;
                            }
                        }
                        if (onlyWhitespace)
                        {
                            inCodeBlock = false;
                            fenceChar = '\0';
                            fenceLength = 0;
                        }
                    }
                }
                continue;
            }

            // Khi không ở trong khối mã:
            // Dòng thụt >= 4 space hoặc tab bị bỏ qua
            if (line.StartsWith('\t') || line.StartsWith("    "))
            {
                continue;
            }

            int lineIndent = 0;
            while (lineIndent < line.Length && line[lineIndent] == ' ') lineIndent++;

            // Kiểm tra rào mở khối mã (>= 3 backticks hoặc tildes)
            if (lineIndent < line.Length && (line[lineIndent] == '`' || line[lineIndent] == '~'))
            {
                char c = line[lineIndent];
                int count = 0;
                while (lineIndent + count < line.Length && line[lineIndent + count] == c) count++;
                if (count >= 3)
                {
                    inCodeBlock = true;
                    fenceChar = c;
                    fenceLength = count;
                    continue;
                }
            }

            string trimmed = line.Substring(lineIndent);

            // Kiểm tra tiêu đề cấp 2 ngoài khối mã: bắt đầu bằng "##" theo sau là khoảng trắng hoặc kết thúc chuỗi
            if (trimmed.StartsWith("## ", StringComparison.Ordinal) || trimmed.StartsWith("##\t", StringComparison.Ordinal) || trimmed == "##")
            {
                string title = trimmed.Length > 2 ? trimmed.Substring(2).Trim() : "";
                while (title.EndsWith('#'))
                {
                    title = title.Substring(0, title.Length - 1).TrimEnd();
                }

                if (reviewSectionState == 0)
                {
                    if (title == "Review (Claude)")
                    {
                        reviewSectionState = 1;
                    }
                    continue;
                }
                else if (reviewSectionState == 1)
                {
                    // Tiêu đề cấp 2 tiếp theo ngoài mã: kết thúc mục Review (Claude) đầu tiên
                    reviewSectionState = 2;
                    break;
                }
            }

            if (reviewSectionState == 1)
            {
                // Kiểm tra tiêu đề cấp 3 "### Vòng N"
                // Phải bắt đầu bằng "### Vòng " (Ordinal)
                if (trimmed.StartsWith("### Vòng ", StringComparison.Ordinal))
                {
                    string rest = trimmed.Substring(9);
                    int digits = 0;
                    while (digits < rest.Length && rest[digits] >= '0' && rest[digits] <= '9')
                    {
                        digits++;
                    }

                    if (digits > 0)
                    {
                        // Phải kết thúc hoặc có khoảng trắng rồi hậu tố tùy ý
                        if (digits == rest.Length || rest[digits] == ' ' || rest[digits] == '\t')
                        {
                            rounds++;
                        }
                    }
                }
            }
        }

        return rounds;
    }

    private static int CountLogRounds(IEnumerable<string?> logs)
    {
        int count = 0;
        foreach (var log in logs)
        {
            if (string.IsNullOrWhiteSpace(log)) continue;
            string text = log;
            if (text.StartsWith('\uFEFF')) text = text.Substring(1);

            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("verdict", out var prop) &&
                    prop.ValueKind == JsonValueKind.String &&
                    prop.GetString() == "CHANGES_REQUESTED")
                {
                    count++;
                }
            }
            catch (JsonException)
            {
                // JSON hỏng đóng góp 0 riêng từng log
            }
        }
        return count;
    }
}
