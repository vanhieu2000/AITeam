using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace AITeam.Core.Lessons;

// W2-02 (R-014): luật chuẩn rules/L-xxx.md + bài học lessons/LS-xxx.md. KHUNG do Claude viết — giữ nguyên chữ ký,
// agent điền phần thân (TODO(agent)). Thiết kế: tasks/W2-02-luat-chuan-va-bai-hoc.md, docs/design/R-014-van-phong-ai.md mục 4.4, 4.5.

public enum LessonStatus { Draft, Promoted, Dropped }

public enum RuleStatus { Active, Retired }

public sealed record Lesson(
    string Id, string Title, string Body, string Kind,
    string? RequestId, string? TaskId, string? Step, string? Ai,
    LessonStatus Status, string? RuleId, DateTimeOffset CreatedAt);

public sealed record Rule(
    string Id, string Title, string Body,
    IReadOnlyList<string> TaskTypes, IReadOnlyList<string> Steps, IReadOnlyList<string> Ais,
    string? SourceLesson, RuleStatus Status, DateTimeOffset CreatedAt);

public interface ILessonService
{
    IReadOnlyList<Lesson> Lessons(LessonStatus? status = null);
    IReadOnlyList<Rule> Rules(bool activeOnly = false);
    Lesson AddDraft(string title, string body, string kind, string? requestId, string? taskId, string? step, string? ai);
    Rule Promote(string lessonId, string title, string body, IReadOnlyList<string> taskTypes, IReadOnlyList<string> steps, IReadOnlyList<string> ais);
    void Drop(string lessonId);
    void Retire(string ruleId);
    IReadOnlyList<Rule> ForTask(string taskType, string step, string? ai);
    string PromptSection(string taskType, string step, string? ai);
}

public sealed class LessonService : ILessonService
{
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex LessonFilePattern = new(@"^LS-(\d{3,})\.md$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex RuleFilePattern = new(@"^L-(\d{3,})\.md$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AnsiPattern = new(@"\u001b\[[0-9;]*[a-zA-Z]", RegexOptions.CultureInvariant);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.CultureInvariant);

    public LessonService(string aiTeamRoot)
    {
        if (string.IsNullOrWhiteSpace(aiTeamRoot))
            throw new ArgumentException("Thiếu thư mục gốc AITeam.", nameof(aiTeamRoot));
        Root = aiTeamRoot;
    }

    public string Root { get; }

    private string LessonsDir => Path.Combine(Root, "lessons");
    private string RulesDir => Path.Combine(Root, "rules");
    private object LockObj => Locks.GetOrAdd(Path.GetFullPath(Root), _ => new object());

    public IReadOnlyList<Lesson> Lessons(LessonStatus? status = null)
    {
        if (!Directory.Exists(LessonsDir)) return [];

        var list = new List<Lesson>();
        string[] files;
        try
        {
            files = Directory.GetFiles(LessonsDir, "LS-*.md");
        }
        catch
        {
            return [];
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!LessonFilePattern.IsMatch(fileName)) continue;

            var lesson = ParseLesson(file);
            if (lesson is null) continue;
            if (status is null || lesson.Status == status.Value)
            {
                list.Add(lesson);
            }
        }

        return list.OrderBy(l => l.Id, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<Rule> Rules(bool activeOnly = false)
    {
        if (!Directory.Exists(RulesDir)) return [];

        var list = new List<Rule>();
        string[] files;
        try
        {
            files = Directory.GetFiles(RulesDir, "L-*.md");
        }
        catch
        {
            return [];
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!RuleFilePattern.IsMatch(fileName)) continue;

            var rule = ParseRule(file);
            if (rule is null) continue;
            if (!activeOnly || rule.Status == RuleStatus.Active)
            {
                list.Add(rule);
            }
        }

        return list.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    public Lesson AddDraft(string title, string body, string kind, string? requestId, string? taskId, string? step, string? ai)
    {
        AssertNoPath(requestId, nameof(requestId));
        AssertNoPath(taskId, nameof(taskId));
        AssertNoPath(step, nameof(step));
        AssertNoPath(ai, nameof(ai));

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120)
            throw new ArgumentException("Tiêu đề bài học phải từ 1 đến 120 ký tự.", nameof(title));
        if (body is null || body.Length > 4000)
            throw new ArgumentException("Nội dung bài học không được vượt quá 4000 ký tự.", nameof(body));
        if (kind != "meo" && kind != "cai-tien")
            throw new ArgumentException("Loại bài học chỉ nhận 'meo' hoặc 'cai-tien'.", nameof(kind));

        lock (LockObj)
        {
            if (!Directory.Exists(LessonsDir)) Directory.CreateDirectory(LessonsDir);

            int maxId = 0;
            foreach (var f in Directory.GetFiles(LessonsDir, "LS-*.md"))
            {
                var match = LessonFilePattern.Match(Path.GetFileName(f));
                if (match.Success && int.TryParse(match.Groups[1].Value, out var num) && num > maxId)
                {
                    maxId = num;
                }
            }

            string nextId = $"LS-{maxId + 1:D3}";
            var lesson = new Lesson(nextId, title.Trim(), body, kind, requestId, taskId, step, ai, LessonStatus.Draft, null, DateTimeOffset.Now);
            WriteAtomic(Path.Combine(LessonsDir, $"{nextId}.md"), FormatLesson(lesson));
            return lesson;
        }
    }

    public Rule Promote(string lessonId, string title, string body, IReadOnlyList<string> taskTypes, IReadOnlyList<string> steps, IReadOnlyList<string> ais)
    {
        AssertNoPath(lessonId, nameof(lessonId));
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120)
            throw new ArgumentException("Tiêu đề luật phải từ 1 đến 120 ký tự.", nameof(title));
        if (body is null || body.Length > 4000)
            throw new ArgumentException("Nội dung luật không được vượt quá 4000 ký tự.", nameof(body));

        lock (LockObj)
        {
            var lessonFile = Path.Combine(LessonsDir, $"{lessonId}.md");
            if (!File.Exists(lessonFile))
                throw new InvalidOperationException($"Không tìm thấy bài học '{lessonId}'.");

            var lesson = ParseLesson(lessonFile);
            if (lesson is null || lesson.Status != LessonStatus.Draft)
                throw new InvalidOperationException($"Bài học '{lessonId}' không ở trạng thái Draft.");

            if (!Directory.Exists(RulesDir)) Directory.CreateDirectory(RulesDir);

            int maxId = 0;
            foreach (var f in Directory.GetFiles(RulesDir, "L-*.md"))
            {
                var match = RuleFilePattern.Match(Path.GetFileName(f));
                if (match.Success && int.TryParse(match.Groups[1].Value, out var num) && num > maxId)
                {
                    maxId = num;
                }
            }

            string ruleId = $"L-{maxId + 1:D3}";
            var rule = new Rule(ruleId, title.Trim(), body, taskTypes ?? [], steps ?? [], ais ?? [], lessonId, RuleStatus.Active, DateTimeOffset.Now);
            WriteAtomic(Path.Combine(RulesDir, $"{ruleId}.md"), FormatRule(rule));

            var updatedLesson = lesson with { Status = LessonStatus.Promoted, RuleId = ruleId };
            WriteAtomic(lessonFile, FormatLesson(updatedLesson));
            return rule;
        }
    }

    public void Drop(string lessonId)
    {
        AssertNoPath(lessonId, nameof(lessonId));

        lock (LockObj)
        {
            var lessonFile = Path.Combine(LessonsDir, $"{lessonId}.md");
            if (!File.Exists(lessonFile))
                throw new InvalidOperationException($"Không tìm thấy bài học '{lessonId}'.");

            var lesson = ParseLesson(lessonFile);
            if (lesson is null || lesson.Status != LessonStatus.Draft)
                throw new InvalidOperationException($"Bài học '{lessonId}' không ở trạng thái Draft.");

            var updatedLesson = lesson with { Status = LessonStatus.Dropped };
            WriteAtomic(lessonFile, FormatLesson(updatedLesson));
        }
    }

    public void Retire(string ruleId)
    {
        AssertNoPath(ruleId, nameof(ruleId));

        lock (LockObj)
        {
            var ruleFile = Path.Combine(RulesDir, $"{ruleId}.md");
            if (!File.Exists(ruleFile))
                throw new InvalidOperationException($"Không tìm thấy luật '{ruleId}'.");

            var rule = ParseRule(ruleFile);
            if (rule is null)
                throw new InvalidOperationException($"Luật '{ruleId}' bị hỏng hoặc không hợp lệ.");

            var updatedRule = rule with { Status = RuleStatus.Retired };
            WriteAtomic(ruleFile, FormatRule(updatedRule));
        }
    }

    public IReadOnlyList<Rule> ForTask(string taskType, string step, string? ai)
    {
        var active = Rules(activeOnly: true);
        return active.Where(r =>
            r.Status == RuleStatus.Active &&
            (r.TaskTypes.Count == 0 || r.TaskTypes.Contains(taskType, StringComparer.OrdinalIgnoreCase)) &&
            (r.Steps.Count == 0 || r.Steps.Contains(step, StringComparer.OrdinalIgnoreCase)) &&
            (r.Ais.Count == 0 || (!string.IsNullOrEmpty(ai) && r.Ais.Contains(ai, StringComparer.OrdinalIgnoreCase)))
        ).OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    public string PromptSection(string taskType, string step, string? ai)
    {
        var rules = ForTask(taskType, step, ai);
        if (rules.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append("LUẬT CHUẨN ÁP DỤNG CHO VIỆC NÀY (bắt buộc):\n");
        int count = Math.Min(15, rules.Count);
        for (int i = 0; i < count; i++)
        {
            var r = rules[i];
            string cleanTitle = CleanPromptText(r.Title);
            string cleanBody = CleanPromptText(r.Body);
            if (cleanBody.Length > 300) cleanBody = cleanBody.Substring(0, 300);
            sb.Append($"- [{r.Id}] {cleanTitle}: {cleanBody}\n");
        }

        if (rules.Count > 15)
        {
            sb.Append($"- …còn {rules.Count - 15} luật\n");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    private static void AssertNoPath(string? value, string paramName)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Contains("..") || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException($"'{paramName}' không được chứa ký tự đường dẫn: {value}", paramName);
    }

    private static string CleanPromptText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var noAnsi = AnsiPattern.Replace(text, "");
        var sb = new StringBuilder(noAnsi.Length);
        foreach (var c in noAnsi)
        {
            if (char.IsControl(c))
                sb.Append(' ');
            else
                sb.Append(c);
        }
        return WhitespacePattern.Replace(sb.ToString(), " ").Trim();
    }

    private void WriteAtomic(string targetPath, string content)
    {
        if (HasReparsePoint(targetPath))
            throw new IOException("Không thể ghi file vì phát hiện liên kết không an toàn.");

        string dir = Path.GetDirectoryName(targetPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string tempFile = Path.Combine(dir, $"{Path.GetFileName(targetPath)}.tmp.{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(tempFile, content, new UTF8Encoding(true));
            if (File.Exists(targetPath))
            {
                File.Replace(tempFile, targetPath, null);
            }
            else
            {
                File.Move(tempFile, targetPath);
            }
        }
        catch
        {
            TryDelete(tempFile);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static bool HasReparsePoint(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (file.LinkTarget != null || (file.Exists && (file.Attributes & FileAttributes.ReparsePoint) != 0))
                return true;

            var dir = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(dir))
            {
                var info = new DirectoryInfo(dir);
                if (info.LinkTarget != null || (info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0))
                    return true;
                dir = info.Parent?.FullName;
            }
            return false;
        }
        catch
        {
            return true;
        }
    }

    private static string FormatLesson(Lesson lesson)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {lesson.Id} · {lesson.Title}");
        sb.AppendLine($"- **Trạng thái:** {lesson.Status}");
        sb.AppendLine($"- **Loại:** {lesson.Kind}");
        if (!string.IsNullOrEmpty(lesson.RequestId)) sb.AppendLine($"- **Yêu cầu:** {lesson.RequestId}");
        if (!string.IsNullOrEmpty(lesson.TaskId)) sb.AppendLine($"- **Task:** {lesson.TaskId}");
        if (!string.IsNullOrEmpty(lesson.Step)) sb.AppendLine($"- **Bước:** {lesson.Step}");
        if (!string.IsNullOrEmpty(lesson.Ai)) sb.AppendLine($"- **AI:** {lesson.Ai}");
        if (!string.IsNullOrEmpty(lesson.RuleId)) sb.AppendLine($"- **Luật:** {lesson.RuleId}");
        sb.AppendLine($"- **Ngày:** {lesson.CreatedAt:yyyy-MM-ddTHH:mm:sszzz}");
        sb.AppendLine();
        sb.Append(lesson.Body);
        return sb.ToString();
    }

    private static string FormatRule(Rule rule)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {rule.Id} · {rule.Title}");
        sb.AppendLine($"- **Trạng thái:** {rule.Status}");
        if (rule.TaskTypes.Count > 0) sb.AppendLine($"- **Loại việc:** {string.Join(", ", rule.TaskTypes)}");
        if (rule.Steps.Count > 0) sb.AppendLine($"- **Bước:** {string.Join(", ", rule.Steps)}");
        if (rule.Ais.Count > 0) sb.AppendLine($"- **AI:** {string.Join(", ", rule.Ais)}");
        if (!string.IsNullOrEmpty(rule.SourceLesson)) sb.AppendLine($"- **Nguồn:** {rule.SourceLesson}");
        sb.AppendLine($"- **Ngày:** {rule.CreatedAt:yyyy-MM-ddTHH:mm:sszzz}");
        sb.AppendLine();
        sb.Append(rule.Body);
        return sb.ToString();
    }

    private static Lesson? ParseLesson(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            using var reader = new StringReader(text);
            string? firstLine = reader.ReadLine();
            if (firstLine is null) return null;

            var headerMatch = Regex.Match(firstLine, @"^#\s+(?<id>LS-\d{3,})\s*·\s*(?<title>.+)$");
            if (!headerMatch.Success) return null;

            string id = headerMatch.Groups["id"].Value;
            if (!string.Equals(id, fileName, StringComparison.OrdinalIgnoreCase)) return null;
            string title = headerMatch.Groups["title"].Value.Trim();

            var status = LessonStatus.Draft;
            string kind = "meo";
            string? requestId = null;
            string? taskId = null;
            string? step = null;
            string? ai = null;
            string? ruleId = null;
            DateTimeOffset createdAt = DateTimeOffset.Now;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) break;

                var m = Regex.Match(trimmed, @"^-\s+\*\*(?<key>[^*:]+):\*\*\s*(?<val>.*)$");
                if (!m.Success) break;

                string key = m.Groups["key"].Value.Trim();
                string val = m.Groups["val"].Value.Trim();

                switch (key)
                {
                    case "Trạng thái":
                        if (Enum.TryParse<LessonStatus>(val, true, out var st)) status = st;
                        break;
                    case "Loại":
                        if (!string.IsNullOrEmpty(val)) kind = val;
                        break;
                    case "Yêu cầu":
                        requestId = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "Task":
                        taskId = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "Bước":
                        step = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "AI":
                        ai = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "Luật":
                        ruleId = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "Ngày":
                        if (DateTimeOffset.TryParse(val, out var dt)) createdAt = dt;
                        break;
                }
            }

            string body;
            if (line != null && !string.IsNullOrEmpty(line.Trim()) && !line.Trim().StartsWith("- **"))
            {
                body = line + "\n" + reader.ReadToEnd();
            }
            else
            {
                body = reader.ReadToEnd();
            }
            body = body.TrimStart('\r', '\n');
            return new Lesson(id, title, body, kind, requestId, taskId, step, ai, status, ruleId, createdAt);
        }
        catch
        {
            return null;
        }
    }

    private static Rule? ParseRule(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            using var reader = new StringReader(text);
            string? firstLine = reader.ReadLine();
            if (firstLine is null) return null;

            var headerMatch = Regex.Match(firstLine, @"^#\s+(?<id>L-\d{3,})\s*·\s*(?<title>.+)$");
            if (!headerMatch.Success) return null;

            string id = headerMatch.Groups["id"].Value;
            if (!string.Equals(id, fileName, StringComparison.OrdinalIgnoreCase)) return null;
            string title = headerMatch.Groups["title"].Value.Trim();

            var status = RuleStatus.Active;
            var taskTypes = new List<string>();
            var steps = new List<string>();
            var ais = new List<string>();
            string? sourceLesson = null;
            DateTimeOffset createdAt = DateTimeOffset.Now;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) break;

                var m = Regex.Match(trimmed, @"^-\s+\*\*(?<key>[^*:]+):\*\*\s*(?<val>.*)$");
                if (!m.Success) break;

                string key = m.Groups["key"].Value.Trim();
                string val = m.Groups["val"].Value.Trim();

                switch (key)
                {
                    case "Trạng thái":
                        if (Enum.TryParse<RuleStatus>(val, true, out var st)) status = st;
                        break;
                    case "Loại việc":
                        if (!string.IsNullOrEmpty(val))
                            taskTypes = val.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                        break;
                    case "Bước":
                        if (!string.IsNullOrEmpty(val))
                            steps = val.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                        break;
                    case "AI":
                        if (!string.IsNullOrEmpty(val))
                            ais = val.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                        break;
                    case "Nguồn":
                        sourceLesson = string.IsNullOrEmpty(val) || val == "-" ? null : val;
                        break;
                    case "Ngày":
                        if (DateTimeOffset.TryParse(val, out var dt)) createdAt = dt;
                        break;
                }
            }

            string body;
            if (line != null && !string.IsNullOrEmpty(line.Trim()) && !line.Trim().StartsWith("- **"))
            {
                body = line + "\n" + reader.ReadToEnd();
            }
            else
            {
                body = reader.ReadToEnd();
            }
            body = body.TrimStart('\r', '\n');
            return new Rule(id, title, body, taskTypes, steps, ais, sourceLesson, status, createdAt);
        }
        catch
        {
            return null;
        }
    }
}