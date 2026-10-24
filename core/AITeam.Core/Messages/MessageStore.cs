using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AITeam.Core.Messages;

// Tin nhắn theo task (R-005 / V5): agent báo tiến độ qua MCP (aiteam_post_message), Hub "Văn phòng" đọc ra.
// File: <workDir>\logs\<Task>.messages.jsonl — mỗi dòng 1 JSON, UTF-8 không BOM. Theo MÁY (logs không lên git).

/// <summary>Một tin nhắn. Kind ∈ Kinds. From = người gửi hiển thị ("Minh (A)", "Codex (C)", "Cổng KT", "Claude", "Anh"…).</summary>
public sealed record TaskMessage(DateTime At, string Task, string From, string Kind, string Text);

public sealed class MessageStore
{
    public const int MaxText = 2000;
    public const int MaxFrom = 60;
    /// <summary>progress = tiến độ · question = agent hỏi · done = agent báo xong · system = công cụ (cổng KT, assign) · review = reviewer · note = người dùng / Claude.</summary>
    public static readonly IReadOnlyList<string> Kinds = ["progress", "question", "done", "system", "review", "note"];

    private static readonly Regex SafeTask = new(@"^[A-Za-z0-9][A-Za-z0-9-]{0,63}$", RegexOptions.Compiled);
    private static readonly Regex ControlChars = new(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly UTF8Encoding NoBom = new(false);
    private static readonly object Gate = new();

    private readonly string _logsDir;

    public MessageStore(string logsDir) => _logsDir = logsDir;

    /// <summary>Đường dẫn file của task. Mã task sai mẫu (chặn "..", "/", ":"…) ⇒ ArgumentException.</summary>
    public string PathFor(string task)
    {
        if (string.IsNullOrEmpty(task) || !SafeTask.IsMatch(task)) throw new ArgumentException($"Mã task không hợp lệ: '{task}'", nameof(task));
        return Path.Combine(_logsDir, task + ".messages.jsonl");
    }

    /// <summary>
    /// Ghi 1 tin. Kind lạ ⇒ ArgumentException. Text rỗng / toàn khoảng trắng ⇒ ArgumentException.
    /// Bỏ ký tự điều khiển (giữ \n, \t), cắt Text còn MaxText ký tự (thêm "…"), From còn MaxFrom.
    /// </summary>
    public TaskMessage Append(string task, string from, string kind, string text, DateTime now)
    {
        var path = PathFor(task);
        if (!Kinds.Contains(kind)) throw new ArgumentException($"Loại tin nhắn không hợp lệ: '{kind}'", nameof(kind));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Tin nhắn rỗng", nameof(text));
        var clean = ControlChars.Replace(text.Trim(), "");
        if (clean.Length > MaxText) clean = clean[..MaxText] + "…";
        var who = ControlChars.Replace((from ?? "").Trim(), "");
        if (who.Length == 0) who = "?";
        if (who.Length > MaxFrom) who = who[..MaxFrom];

        var msg = new TaskMessage(now, task, who, kind, clean);
        var line = JsonSerializer.Serialize(msg, Json) + "\n";
        lock (Gate)
        {
            Directory.CreateDirectory(_logsDir);
            // Ghi nối tiếp, cho phép Hub đọc cùng lúc. Thử lại ngắn khi tiến trình khác đang ghi.
            for (var i = 0; ; i++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    var bytes = NoBom.GetBytes(line);
                    fs.Write(bytes, 0, bytes.Length);
                    break;
                }
                catch (IOException) when (i < 5) { Thread.Sleep(50); }
            }
        }
        return msg;
    }

    /// <summary>Tối đa <paramref name="max"/> tin MỚI NHẤT của task, theo thời gian tăng dần. Không có file / dòng hỏng ⇒ bỏ qua. Mã task sai ⇒ rỗng.</summary>
    public IReadOnlyList<TaskMessage> Read(string task, int max = 200)
    {
        string path;
        try { path = PathFor(task); } catch (ArgumentException) { return []; }
        if (!File.Exists(path) || max <= 0) return [];
        string[] lines;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            lines = sr.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (IOException) { return []; }
        var list = new List<TaskMessage>();
        foreach (var l in lines.Skip(Math.Max(0, lines.Length - max)))
        {
            try { if (JsonSerializer.Deserialize<TaskMessage>(l, Json) is { } m && !string.IsNullOrEmpty(m.Text)) list.Add(m); }
            catch (JsonException) { }
        }
        return list;
    }
}
