using System.Text.RegularExpressions;

namespace AITeam.Core.Queue;

/// <summary>Một dòng trên bảng việc: mã task, file Markdown (nếu có) và ô trạng thái cuối.</summary>
public sealed record BoardTask(string Id, string? FileName, string Status);

/// <summary>Một task "Chờ làm" đã xét phụ thuộc; Ready = false thì Reason nêu phụ thuộc còn thiếu.</summary>
public sealed record QueueTask(string Id, string FileName, string Status, IReadOnlyList<string> DependsOn,
    bool Ready, string Reason);

/// <summary>
/// Đọc bảng việc và chọn task "Chờ làm" đủ phụ thuộc. Phụ thuộc lấy từ dòng
/// "- **Phụ thuộc:** T-xxx" trong file task và chỉ đạt khi task đó "✅ Đã merge" trên bảng.
/// File bảng/file task là dữ liệu ngoài: chỉ nhận đường dẫn .md tương đối, bỏ dấu "/" đầu và "..".
/// </summary>
public static class TaskQueue
{
    private static readonly Regex IdRx = new(@"[A-Za-z][A-Za-z0-9]*-\d+", RegexOptions.Compiled);
    private static readonly Regex ExactIdRx = new(@"\A[A-Za-z][A-Za-z0-9]*-\d+\z", RegexOptions.Compiled);
    private static readonly Regex LinkRx = new(@"\]\((?<file>[^)]+\.md)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DepLineRx = new(@"(?im)^[ \t]*-[ \t]*\*\*Phụ thuộc:\*\*[ \t]*(?<deps>[^\r\n]*)", RegexOptions.Compiled);
    private static readonly Regex ScopeSectionRx = new(@"(?ims)^##[ \t]+Phạm vi[^\r\n]*\r?\n(?<body>.*?)(?=^##[ \t]|\z)", RegexOptions.Compiled);
    private static readonly Regex BacktickRx = new(@"`([^`]+)`", RegexOptions.Compiled);

    public static IReadOnlyList<BoardTask> ParseBoard(string? text)
    {
        var result = new List<BoardTask>();
        foreach (var raw in (text ?? "").Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (!line.TrimStart().StartsWith('|')) continue;
            var cells = line.Split('|');
            if (cells.Length < 3) continue;
            var first = cells[1].Trim();
            var id = IdRx.Match(first);
            if (!id.Success) continue;

            var fileName = SafeFileName(LinkRx.Match(first) is { Success: true } link ? link.Groups["file"].Value : null);
            result.Add(new BoardTask(id.Value.ToUpperInvariant(), fileName, cells[^2].Trim()));
        }
        return result;
    }

    /// <summary>Task đã xong trên bảng việc (✅ merge, 🔵 chờ review, 🔴 cần sửa) ⇒ nhả khóa phạm vi.</summary>
    public static bool IsFinished(string? status)
    {
        var text = (status ?? "").Trim();
        return IsMerged(text) || text.Contains("🔵") || text.Contains("🔴");
    }

    public static bool IsMerged(string? status)
    {
        var text = (status ?? "").Trim();
        return text.Contains("✅ Đã merge", StringComparison.Ordinal) ||
               (text.StartsWith('✅') && text.Contains("merge", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Chỉ 🟣 / "Chờ làm" / ô trống là chờ giao; mọi trạng thái khác (kể cả Tạm dừng, Bị chặn, Chờ người làm) đều bỏ qua.</summary>
    public static bool IsPending(string? status)
    {
        var text = (status ?? "").Trim();
        if (text.Length == 0) return true;
        if (text.Contains("🟡") || text.Contains("🔵") || text.Contains("🔴") || text.Contains("✅")) return false;
        if (text.Contains("Tạm dừng") || text.Contains("Bị chặn") || text.Contains("Chờ người làm") ||
            text.Contains("Chờ review") || text.Contains("Đang chạy") || text.Contains("Chờ duyệt") ||
            text.Contains("Cần sửa") || text.Contains("Hủy")) return false;
        if (text.Contains("🟣")) return true;
        return text.Contains("Chờ làm", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> Dependencies(string? taskText)
    {
        var match = DepLineRx.Match(taskText ?? "");
        if (!match.Success) return [];
        var tokens = match.Groups["deps"].Value.Split(
            [' ', '\t', ',', ';', '·', '+', '&'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var raw in tokens)
        {
            var token = raw.Trim('`', '*', '.', ')', '(', '[', ']');
            if (!ExactIdRx.IsMatch(token)) continue;
            var id = token.ToUpperInvariant();
            if (!result.Contains(id, StringComparer.OrdinalIgnoreCase)) result.Add(id);
        }
        return result;
    }

    /// <summary>Các đường dẫn trong dấu nháy ngược ở mục "Phạm vi" (dùng cho khóa file).</summary>
    public static IReadOnlyList<string> ScopePaths(string? taskText)
    {
        var section = ScopeSectionRx.Match(taskText ?? "");
        if (!section.Success) return [];
        return BacktickRx.Matches(section.Groups["body"].Value)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Xét mọi dòng "Chờ làm" có file task. <paramref name="readTaskText"/> trả null khi không đọc được file.
    /// Giữ thứ tự bảng việc; chỉ Ready khi mọi phụ thuộc đã "✅ Đã merge".
    /// </summary>
    public static IReadOnlyList<QueueTask> Evaluate(IReadOnlyList<BoardTask> board, Func<string, string?> readTaskText)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(readTaskText);
        var merged = board.Where(t => IsMerged(t.Status)).Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<QueueTask>();
        foreach (var task in board)
        {
            if (!IsPending(task.Status) || string.IsNullOrWhiteSpace(task.FileName)) continue;
            var text = readTaskText(task.FileName);
            if (text == null)
            {
                result.Add(new QueueTask(task.Id, task.FileName, task.Status, [], false, "Không đọc được file task"));
                continue;
            }
            var dependencies = Dependencies(text);
            var missing = dependencies.Where(d => !merged.Contains(d)).ToList();
            result.Add(missing.Count == 0
                ? new QueueTask(task.Id, task.FileName, task.Status, dependencies, true, "Đủ phụ thuộc")
                : new QueueTask(task.Id, task.FileName, task.Status, dependencies, false, "Chờ phụ thuộc: " + string.Join(", ", missing)));
        }
        return result;
    }

    private static string? SafeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var text = name.Trim().Replace('\\', '/');
        if (text.StartsWith('/') || text.Contains(':') || text.Split('/').Any(p => p is ".." or ".")) return null;
        return text;
    }
}
