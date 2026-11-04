using System.Text;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// F1-02 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-02.

public enum RequestState { Open, Done, Cancelled }

public sealed record RequestTaskStatus(string Project, string Code, string Title, OfficeTaskStatus Status);

public sealed record RequestProgress(string Id, RequestState State, int Total, int Merged, int Running, int Review, int NeedsFix,
    bool AllMerged, IReadOnlyList<RequestTaskStatus> Tasks);

public sealed record RequestStateResult(bool Ok, string? Error);

public interface IRequestProgressService
{
    /// <summary>Theo IRequestService.GetAll(), cùng thứ tự.</summary>
    IReadOnlyList<RequestProgress> GetAll();

    /// <summary>id sai dạng ^R-\d{3}$ hoặc không có ⇒ null.</summary>
    RequestProgress? Get(string id);

    /// <summary>Ghi dòng "- **Trạng thái:**" của phiếu + 1 dòng nhật ký. Sai ⇒ Ok=false, không ghi.</summary>
    RequestStateResult SetState(string id, RequestState state, string? note);
}

public sealed class RequestProgressService : IRequestProgressService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly IRequestService _requests;
    private readonly TimeProvider _clock;

    public RequestProgressService(HubPaths paths, ITeamConfigService config, IRequestService requests, TimeProvider clock)
    {
        _paths = paths;
        _config = config;
        _requests = requests;
        _clock = clock;
    }

    public IReadOnlyList<RequestProgress> GetAll()
    {
        var linked = new Dictionary<string, List<RequestTaskStatus>>(StringComparer.Ordinal);
        foreach (var project in _config.GetProjects().Where(p => p.ExistsLocally))
        {
            var folder = Path.Combine(project.Path, "tasks");
            try
            {
                if (!Directory.Exists(folder)) continue;
                var boardFile = Path.Combine(folder, "README.md");
                var board = BoardTable.Parse(File.Exists(boardFile) ? File.ReadAllText(boardFile) : null)
                    .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
                foreach (var file in Directory.EnumerateFiles(folder, "*.md"))
                {
                    if (Path.GetFileName(file).Equals("README.md", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        var code = Regex.Match(Path.GetFileName(file), @"^[A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9]*").Value;
                        if (code.Length == 0) continue;
                        var text = File.ReadAllText(file);
                        var request = Regex.Match(text, @"^- \*\*Yêu cầu:\*\*[^\S\r\n]*(R-\d{3})[^\S\r\n]*\r?$", RegexOptions.Multiline);
                        if (!request.Success) continue;
                        var title = Regex.Match(text, @"^# ([^\r\n]*)", RegexOptions.Multiline).Groups[1].Value;
                        if (title.StartsWith(code, StringComparison.OrdinalIgnoreCase)) title = title[code.Length..].TrimStart(' ', '·', '-', ':', '–', '—');
                        var status = OfficeTaskStatus.Planned;
                        if (board.TryGetValue(code, out var row))
                        {
                            var raw = row.Status;
                            var fallback = raw.Contains("🟡") ? TaskState.Assigned : raw.Contains("🔵") ? TaskState.Review
                                : raw.Contains("🔴") ? TaskState.NeedsFix : raw.Contains("✅") ? TaskState.Merged : TaskState.Unknown;
                            status = WorkOverviewService.ParseStatus(raw, fallback);
                        }
                        if (!linked.TryGetValue(request.Groups[1].Value, out var tasks))
                            linked[request.Groups[1].Value] = tasks = new();
                        tasks.Add(new(project.Name, code, title.Trim(), status));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Bỏ qua task không đọc được. */ }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Bỏ qua dự án không đọc được. */ }
        }
        return _requests.GetAll().Select(request =>
        {
            var tasks = linked.GetValueOrDefault(request.Id, new()).OrderBy(t => t.Project, StringComparer.Ordinal)
                .ThenBy(t => t.Code, StringComparer.Ordinal).ToList();
            int total = tasks.Count(t => t.Status != OfficeTaskStatus.Cancelled);
            int merged = tasks.Count(t => t.Status == OfficeTaskStatus.Merged);
            var state = request.Status.Contains("✅") ? RequestState.Done
                : request.Status.Contains("❌") || request.Status.Contains("Hủy", StringComparison.OrdinalIgnoreCase) ? RequestState.Cancelled : RequestState.Open;
            return new RequestProgress(request.Id, state, total, merged,
                tasks.Count(t => t.Status is OfficeTaskStatus.Running or OfficeTaskStatus.Gate),
                tasks.Count(t => t.Status == OfficeTaskStatus.Review),
                tasks.Count(t => t.Status is OfficeTaskStatus.NeedsFix or OfficeTaskStatus.Blocked), total > 0 && merged == total, tasks);
        }).ToList();
    }

    public RequestProgress? Get(string id) => ValidId(id) ? GetAll().FirstOrDefault(r => r.Id == id) : null;

    private static bool ValidId(string? id) => id != null && Regex.IsMatch(id, @"\AR-\d{3}\z");

    public RequestStateResult SetState(string id, RequestState state, string? note)
    {
        if (!ValidId(id) || !Enum.IsDefined(state)) return new(false, "Mã phiếu hoặc trạng thái không hợp lệ.");
        lock (_paths)
        {
            string? temporary = null;
            try
            {
                var folder = Path.GetFullPath(Path.Combine(_paths.AITeamRoot, "requests"));
                if (!Directory.Exists(folder) || !SafePath(folder)) return new(false, "Không tìm thấy thư mục phiếu an toàn.");
                var files = Directory.EnumerateFiles(folder, id + "*.md")
                    .Where(f => Regex.IsMatch(Path.GetFileName(f), @"\A" + Regex.Escape(id) + @"(?:-|\.md\z)" )).ToList();
                if (files.Count != 1 || !SafePath(files[0])) return new(false, "Không tìm thấy phiếu duy nhất và an toàn.");
                var file = Path.GetFullPath(files[0]);
                if (!string.Equals(Path.GetDirectoryName(file), folder, StringComparison.OrdinalIgnoreCase)) return new(false, "Đường dẫn phiếu không hợp lệ.");
                var bytes = File.ReadAllBytes(file);
                bool bom = bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf });
                var encoding = new UTF8Encoding(false, true);
                var text = encoding.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                var statusLine = Regex.Match(text, @"^- \*\*Trạng thái:\*\*[^\r\n]*", RegexOptions.Multiline);
                if (!statusLine.Success) return new(false, "Phiếu thiếu dòng trạng thái.");
                var clean = Regex.Replace(new string((note ?? "").Select(c => char.IsControl(c) ? ' ' : c).ToArray()), @"\s+", " ").Trim();
                if (clean.Length > 200) clean = clean[..200];
                var now = _clock.GetLocalNow();
                var label = state == RequestState.Done ? "Xong" : state == RequestState.Cancelled ? "Hủy" : "Mở lại";
                var value = state == RequestState.Open ? "🟡 Đang làm" : (state == RequestState.Done ? "✅ Xong " : "❌ Hủy ") + now.ToString("HH:mm dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                if (clean.Length > 0) value += " — " + clean;
                text = text.Remove(statusLine.Index, statusLine.Length).Insert(statusLine.Index, "- **Trạng thái:** " + value);
                var journal = Regex.Match(text, @"^## 3\. Nhật ký[^\r\n]*(?:\r\n|\n|\r)", RegexOptions.Multiline);
                if (journal.Success)
                {
                    var lines = Regex.Matches(text[(journal.Index + journal.Length)..], @"[^\r\n]+(?:\r\n|\n|\r|\z)|(?:\r\n|\n|\r)");
                    int position = journal.Index + journal.Length, end = -1, rows = 0;
                    foreach (Match line in lines)
                    {
                        var trimmed = line.Value.Trim();
                        if (trimmed.StartsWith("## ", StringComparison.Ordinal)) break;
                        if (trimmed.StartsWith('|')) { end = position + line.Length; rows++; }
                        else if (rows > 0 && trimmed.Length > 0) break;
                        position += line.Length;
                    }
                    if (rows >= 2)
                    {
                        var newline = journal.Value.EndsWith("\r\n", StringComparison.Ordinal) ? "\r\n" : journal.Value.EndsWith('\r') ? "\r" : "\n";
                        var entry = $"| {now.ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture)} | Đổi trạng thái phiếu: {label} (Hub) | {clean.Replace('|', '/')} |" + newline;
                        if (end > 0 && text[end - 1] is not ('\r' or '\n')) entry = newline + entry;
                        text = text.Insert(end, entry);
                    }
                }
                temporary = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
                var output = encoding.GetBytes(text);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    if (bom) stream.Write(new byte[] { 0xef, 0xbb, 0xbf });
                    stream.Write(output);
                    stream.Flush(true);
                }
                if (!SafePath(folder) || !SafePath(file)) return new(false, "Đường dẫn phiếu không an toàn.");
                File.Move(temporary, file, true);
                return new(true, null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return new(false, "Không thể cập nhật phiếu an toàn.");
            }
            finally
            {
                if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }

    private static bool SafePath(string path)
    {
        for (var current = new FileInfo(path) as FileSystemInfo; current != null;
            current = current is DirectoryInfo directory ? directory.Parent : ((FileInfo)current).Directory)
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0) return false;
        return true;
    }
}