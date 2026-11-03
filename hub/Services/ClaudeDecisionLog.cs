using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>
/// Nhật ký "Claude tự quyết": &lt;AITeamRoot&gt;/approvals/claude-decisions.jsonl, CHỈ THÊM DÒNG (append-only).
/// Mỗi dòng: {"type":"decision",...} hoặc {"type":"reopen","ref":"D-…","note":…,"at":…}.
/// Đọc phòng thủ (dòng hỏng bỏ qua, tối đa 5 MB cuối file), chặn reparse point khi đọc / ghi.
/// </summary>
public sealed class ClaudeDecisionLog : IClaudeDecisionLog
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Regex IdRegex = new(@"^D-\d{14}-[a-z0-9]{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly object WriteGate = new();

    private readonly HubPaths _paths;

    public ClaudeDecisionLog(HubPaths paths) => _paths = paths;

    public string Folder => Path.Combine(_paths.AITeamRoot, "approvals");

    public string FilePath => Path.Combine(Folder, "claude-decisions.jsonl");

    public IReadOnlyList<ClaudeDecision> List(int max = 200)
    {
        if (max <= 0 || !File.Exists(FilePath)) return Array.Empty<ClaudeDecision>();
        if (HasReparsePoint(FilePath)) return Array.Empty<ClaudeDecision>();

        var decisions = new Dictionary<string, ClaudeDecision>(StringComparer.Ordinal);
        var order = new List<string>();
        var reopens = new Dictionary<string, (string Note, DateTimeOffset At)>(StringComparer.Ordinal);

        foreach (var line in ReadTailLines(FilePath))
        {
            JsonObject? node;
            try { node = JsonNode.Parse(line) as JsonObject; }
            catch { continue; }
            if (node == null) continue;

            var type = (string?)node["type"];
            if (string.Equals(type, "decision", StringComparison.OrdinalIgnoreCase))
            {
                var id = (string?)node["id"];
                if (string.IsNullOrEmpty(id) || !IdRegex.IsMatch(id)) continue;
                if (!TryAt(node["at"], out var at)) continue;
                var title = (string?)node["title"];
                if (string.IsNullOrWhiteSpace(title)) continue;
                if (!decisions.ContainsKey(id)) order.Add(id);
                decisions[id] = new ClaudeDecision(id, at, (string?)node["kind"] ?? "", title,
                    (string?)node["project"], (string?)node["task"], (string?)node["request"], (string?)node["detail"],
                    false, null, null);
            }
            else if (string.Equals(type, "reopen", StringComparison.OrdinalIgnoreCase))
            {
                var reference = (string?)node["ref"];
                if (string.IsNullOrEmpty(reference) || !IdRegex.IsMatch(reference)) continue;
                var note = ((string?)node["note"])?.Trim();
                if (string.IsNullOrEmpty(note)) continue;
                if (!TryAt(node["at"], out var at)) continue;
                if (!reopens.TryGetValue(reference, out var previous) || at >= previous.At) reopens[reference] = (note, at);
            }
        }

        var list = new List<ClaudeDecision>(order.Count);
        foreach (var id in order)
        {
            var decision = decisions[id];
            if (reopens.TryGetValue(id, out var reopened))
                decision = decision with { Reopened = true, ReopenNote = reopened.Note, ReopenedAt = reopened.At };
            list.Add(decision);
        }

        return list
            .OrderByDescending(d => d.ReopenedAt ?? d.At)
            .ThenByDescending(d => d.At)
            .Take(max)
            .ToList();
    }

    public ClaudeDecision Reopen(string id, string note)
    {
        if (string.IsNullOrEmpty(id) || !IdRegex.IsMatch(id))
            throw new ArgumentException("Mã quyết định không hợp lệ.", nameof(id));
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Ghi chú mở lại không được rỗng.", nameof(note));

        var current = List(int.MaxValue).FirstOrDefault(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy quyết định {id}.");

        var cleanNote = note.Trim();
        var at = DateTimeOffset.Now;
        var line = new JsonObject
        {
            ["type"] = "reopen",
            ["ref"] = id,
            ["note"] = cleanNote,
            ["at"] = at.ToString("o", CultureInfo.InvariantCulture),
        }.ToJsonString();

        AppendLine(line);
        return current with { Reopened = true, ReopenNote = cleanNote, ReopenedAt = at };
    }

    private void AppendLine(string line)
    {
        EnsureSafeFolder();
        var bytes = new UTF8Encoding(false).GetBytes(line + "\n");
        lock (WriteGate)
        {
            using var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }
    }

    private void EnsureSafeFolder()
    {
        for (var node = Folder; !string.IsNullOrEmpty(node); node = Path.GetDirectoryName(node))
        {
            if (HasReparsePoint(node)) throw new InvalidOperationException("Đường dẫn nhật ký không an toàn.");
            if (Path.GetPathRoot(node) == node) break;
        }
        Directory.CreateDirectory(Folder);
        if (HasReparsePoint(Folder)) throw new InvalidOperationException("Đường dẫn nhật ký không an toàn.");
    }

    private static List<string> ReadTailLines(string path)
    {
        var lines = new List<string>();
        byte[] bytes;
        long start = 0;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length > MaxBytes) start = stream.Length - MaxBytes;
            stream.Seek(start, SeekOrigin.Begin);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            bytes = buffer.ToArray();
        }
        catch { return lines; }

        var text = Encoding.UTF8.GetString(bytes);
        if (start > 0)
        {
            var newline = text.IndexOf('\n');
            if (newline < 0) return lines;
            text = text[(newline + 1)..];
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(trimmed)) lines.Add(trimmed);
        }
        return lines;
    }

    private static bool TryAt(JsonNode? node, out DateTimeOffset value)
    {
        value = default;
        var raw = (string?)node;
        return !string.IsNullOrEmpty(raw) &&
               DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
    }

    private static bool HasReparsePoint(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return false;
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch { return true; }
    }
}
