using System.Text.RegularExpressions;

namespace AITeam.Hub.Services;

// Trang "Giao việc" (R-003 / H3-05): mỗi phiếu requests/R-xxx-*.md là 1 luồng hội thoại.
// Lời chủ dự án = tin nhắn bên phải; mỗi dòng Nhật ký của Claude = tin nhắn bên trái. Hub chỉ ĐỌC phiếu.

/// <summary>Một dòng bảng "## 3. Nhật ký": | Giờ | Sự kiện | Ghi chú |.</summary>
public sealed record RequestEvent(string Time, string Text, string Note);

/// <summary>Một dòng bảng "## 2. Chia việc": | Task | Dự án | Ai | … |. Code = mã đầu ô Task (vd. "H3-01", "P0-4/5"), không có ⇒ "".</summary>
public sealed record RequestTaskRef(string Code, string Title, string Project, string Who);

public sealed record RequestThread(
    string Id,              // "R-003" (lấy từ TÊN FILE)
    string Title,           // dòng "# R-003 · Tiêu đề" bỏ "R-003 · "
    string Status,          // giá trị "- **Trạng thái:**"
    string ReceivedAt,      // giá trị "- **Nhận lúc:**"
    string Verbatim,        // các dòng "> …" ngay sau "- **Lời chủ dự án:**", bỏ "> ", nối bằng "\n"
    IReadOnlyList<RequestTaskRef> Tasks,
    IReadOnlyList<RequestEvent> Events,
    bool Evaluated,         // ô giá trị của dòng "Tổng thời gian" trong mục "## 4." khác rỗng
    string FileName);

public static class RequestParser
{
    /// <summary>
    /// fileName phải khớp ^R-\d{3} (vd. "R-003-nang-cap.md"), sai ⇒ null. Không ném lỗi với nội dung hỏng / thiếu mục:
    /// mục thiếu ⇒ chuỗi rỗng / danh sách rỗng / false.
    /// Bảng: bỏ dòng tiêu đề (ô đầu "Task" / "Giờ" / "Chỉ số") và dòng "|---|"; ô "—" giữ nguyên văn; Trim mọi ô.
    /// Chia việc: bỏ dòng ô đầu rỗng; Code = khớp ^[A-Za-z0-9]+-\d+[A-Za-z0-9/]* ở đầu ô Task (không khớp ⇒ "").
    /// Nhật ký: bỏ dòng ô Sự kiện rỗng.
    /// Verbatim: chỉ nhận các dòng liền nhau bắt đầu bằng "&gt;" (sau khoảng trắng) ngay sau dòng "Lời chủ dự án"; dừng ở dòng đầu tiên không phải trích dẫn.
    /// </summary>
    public static RequestThread? Parse(string fileName, string markdown)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var idMatch = Regex.Match(fileName, @"^(R-\d{3})(?!\d)");
        if (!idMatch.Success) return null;

        var id = idMatch.Groups[1].Value;
        string title = "", status = "", receivedAt = "";
        var verbatimLines = new List<string>();
        var tasks = new List<RequestTaskRef>();
        var events = new List<RequestEvent>();
        var evaluated = false;
        int section = 0;
        var inVerbatim = false;

        foreach (var raw in (markdown ?? "").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
        {
            var trimmed = raw.Trim();
            if (inVerbatim)
            {
                if (trimmed.StartsWith('>')) { verbatimLines.Add(trimmed[1..].Trim()); continue; }
                inVerbatim = false;
            }
            if (trimmed.StartsWith("# ") && string.IsNullOrEmpty(title))
            {
                var h = trimmed[2..].Trim();
                var m = Regex.Match(h, @"^R-\d{3}\s*[·\-:]\s*(.*)$");
                title = m.Success ? m.Groups[1].Value.Trim() : (Regex.IsMatch(h, @"^R-\d{3}$") ? "" : h);
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                section = (trimmed.Contains("Chia việc", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("## 2.", StringComparison.Ordinal)) ? 2
                    : (trimmed.Contains("Nhật ký", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("## 3.", StringComparison.Ordinal)) ? 3
                    : (trimmed.StartsWith("## 4.", StringComparison.Ordinal) || trimmed.Contains("Đánh giá sau khi xong", StringComparison.OrdinalIgnoreCase)) ? 4 : 1;
                continue;
            }
            if (section == 0)
            {
                var stMatch = Regex.Match(trimmed, @"^-\s*\*\*Trạng thái:\*\*\s*(.*)$", RegexOptions.IgnoreCase);
                if (stMatch.Success) { status = stMatch.Groups[1].Value.Trim(); continue; }
                var rcvMatch = Regex.Match(trimmed, @"^-\s*\*\*Nhận lúc:\*\*\s*(.*)$", RegexOptions.IgnoreCase);
                if (rcvMatch.Success) { receivedAt = rcvMatch.Groups[1].Value.Trim(); continue; }
                if (Regex.IsMatch(trimmed, @"^-\s*\*\*Lời chủ dự án:\*\*", RegexOptions.IgnoreCase)) { inVerbatim = true; continue; }
            }
            if (!trimmed.StartsWith('|')) continue;
            var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length == 0 || cells.All(c => c.All(ch => ch is '-' or ':' or ' '))) continue;
            if (cells[0].Equals("Task", StringComparison.OrdinalIgnoreCase) ||
                cells[0].Equals("Giờ", StringComparison.OrdinalIgnoreCase) ||
                cells[0].Equals("Chỉ số", StringComparison.OrdinalIgnoreCase)) continue;

            if (section == 2 && !string.IsNullOrEmpty(cells[0]))
            {
                var codeMatch = Regex.Match(cells[0], @"^[A-Za-z0-9]+-\d+[A-Za-z0-9/]*");
                tasks.Add(new RequestTaskRef(codeMatch.Success ? codeMatch.Value : "", cells[0], cells.Length > 1 ? cells[1] : "", cells.Length > 2 ? cells[2] : ""));
            }
            else if (section == 3 && cells.Length >= 2 && !string.IsNullOrEmpty(cells[1]))
                events.Add(new RequestEvent(cells[0], cells[1], cells.Length > 2 ? cells[2] : ""));
            else if (section == 4 && cells.Length >= 2 && cells[0].Contains("Tổng thời gian", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cells[1]))
                evaluated = true;
        }
        return new RequestThread(id, title, status, receivedAt, string.Join("\n", verbatimLines), tasks, events, evaluated, fileName);
    }
}

public interface IRequestService
{
    /// <summary>Mọi requests/R-*.md đọc được, Id GIẢM dần (mới nhất trước). Thư mục không có ⇒ rỗng.</summary>
    IReadOnlyList<RequestThread> GetAll();

    /// <summary>id phải khớp ^R-\d{3}$ (sai ⇒ null, không đụng đĩa); không có ⇒ null.</summary>
    RequestThread? Get(string id);
}

public sealed class RequestService : IRequestService
{
    private readonly HubPaths _paths;

    public RequestService(HubPaths paths) => _paths = paths;

    public string Folder => Path.Combine(_paths.AITeamRoot, "requests");

    public IReadOnlyList<RequestThread> GetAll()
    {
        if (!Directory.Exists(Folder)) return Array.Empty<RequestThread>();
        return Directory.EnumerateFiles(Folder, "R-*.md")
            .Select(p => RequestParser.Parse(Path.GetFileName(p), File.ReadAllText(p)))
            .Where(t => t != null).Select(t => t!)
            .OrderByDescending(t => t.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public RequestThread? Get(string id)
    {
        if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^R-\d{3}$") || !Directory.Exists(Folder)) return null;
        return Directory.EnumerateFiles(Folder, $"{id}*.md")
            .Select(p => RequestParser.Parse(Path.GetFileName(p), File.ReadAllText(p)))
            .FirstOrDefault(t => t != null && string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
