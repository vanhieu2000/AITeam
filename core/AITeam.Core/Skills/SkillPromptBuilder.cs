using System.Text.Json;
using System.Text.Json.Serialization;

namespace AITeam.Core.Skills;

/// <summary>Chỉ dẫn đọc skill cho review độc lập; không chạy tiến trình hoặc sửa cấu hình.</summary>
public static class SkillPromptBuilder
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ProjectsDoc
    {
        [JsonPropertyName("projects")]
        public List<ProjectItem>? Projects { get; set; }
    }

    private sealed class ProjectItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("workDir")]
        public string? WorkDir { get; set; }

        [JsonPropertyName("stack")]
        public string? Stack { get; set; }
    }

    /// <summary>
    /// Trả các dòng "Đọc skill: `<đường dẫn tuyệt đối>`" theo thứ tự 09-qc, code-test, skill stack.
    /// Chỉ trỏ SKILL.md tồn tại trong root/skills; không nhúng nội dung file.
    /// Đọc root/projects.json: mục projects là mảng name/path/workDir/stack; không đọc hồ sơ thật.
    /// Chọn dự án từ workDir hoặc taskFile dưới path/workDir cấu hình, ưu tiên đường dẫn dài nhất;
    /// khớp ranh giới thư mục. Hai nguồn chỉ hai dự án khác nhau ⇒ bỏ stack.
    /// WinForms ⇒ code-winforms; ASP.NET/Razor ⇒ code-web; MAUI/console/CLI ⇒ code-app, không phân biệt hoa thường.
    /// Stack thiếu/rỗng/lạ hoặc không tìm được dự án ⇒ vẫn có 09-qc/code-test nếu tồn tại; không đoán từ diff.
    /// Root null/rỗng ⇒ chuỗi rỗng. Thiếu projects.json hoặc skill ⇒ bỏ phần tương ứng, không báo lỗi.
    /// JSON hỏng ⇒ JsonException, không coi thành cấu hình rỗng. Không theo symlink/reparse point khi đọc nguồn.
    /// Tên stack không dùng ghép đường dẫn; không ghi file, gọi shell hoặc đọc ngoài root cấu hình.
    /// profile là ngữ cảnh tùy chọn, không tự suy ra USERPROFILE.
    /// </summary>
    public static string BuildReviewInstructions(string? root, string workDir, string taskFile, string? profile = null)
    {
        if (string.IsNullOrWhiteSpace(root))
            return "";

        string? stackSkillId = null;
        var projectsFile = Path.Combine(root, "projects.json");

        if (File.Exists(projectsFile) && !HasReparsePoint(projectsFile))
        {
            var content = File.ReadAllText(projectsFile);
            var doc = JsonSerializer.Deserialize<ProjectsDoc>(content, JsonOpts);
            var projects = doc?.Projects;
            if (projects != null && projects.Count > 0)
            {
                var projWork = FindProject(workDir, projects);
                var projTask = FindProject(taskFile, projects);

                ProjectItem? selected = null;
                if (projWork != null && projTask != null)
                {
                    bool same = ReferenceEquals(projWork, projTask) ||
                        string.Equals(projWork.Name, projTask.Name, StringComparison.OrdinalIgnoreCase);
                    if (same)
                        selected = projWork;
                    else
                        selected = null; // Hai nguồn nhận diện hai dự án khác nhau => bỏ stack
                }
                else if (projWork != null)
                {
                    selected = projWork;
                }
                else if (projTask != null)
                {
                    selected = projTask;
                }

                if (selected != null)
                {
                    stackSkillId = MapStackToSkillId(selected.Stack);
                }
            }
        }

        var candidateSkills = new List<string> { "09-qc", "code-test" };
        if (!string.IsNullOrEmpty(stackSkillId) && !candidateSkills.Contains(stackSkillId))
        {
            candidateSkills.Add(stackSkillId);
        }

        var lines = new List<string>();
        foreach (var id in candidateSkills)
        {
            var skillPath = Path.Combine(root, "skills", id, "SKILL.md");
            if (File.Exists(skillPath) && !HasReparsePoint(skillPath))
            {
                var fullPath = Path.GetFullPath(skillPath);
                lines.Add($"Đọc skill: `{fullPath}`");
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "";
    }

    private static ProjectItem? FindProject(string? targetPath, List<ProjectItem> projects)
    {
        if (string.IsNullOrWhiteSpace(targetPath)) return null;

        string normalizedTarget;
        try
        {
            normalizedTarget = NormalizePath(targetPath);
        }
        catch
        {
            return null;
        }

        ProjectItem? bestMatch = null;
        int bestLength = -1;

        foreach (var proj in projects)
        {
            int matchLen = GetMatchLength(normalizedTarget, proj.Path, proj.WorkDir);
            if (matchLen > bestLength)
            {
                bestLength = matchLen;
                bestMatch = proj;
            }
        }

        return bestMatch;
    }

    private static int GetMatchLength(string normalizedTarget, string? path, string? workDir)
    {
        int maxLen = -1;
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                var norm = NormalizePath(path);
                if (IsSubpath(normalizedTarget, norm) && norm.Length > maxLen)
                    maxLen = norm.Length;
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(workDir))
        {
            try
            {
                var norm = NormalizePath(workDir);
                if (IsSubpath(normalizedTarget, norm) && norm.Length > maxLen)
                    maxLen = norm.Length;
            }
            catch { }
        }

        return maxLen;
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsSubpath(string child, string parent)
    {
        if (string.Equals(child, parent, StringComparison.OrdinalIgnoreCase))
            return true;

        var parentWithSep = parent + Path.DirectorySeparatorChar;
        return child.StartsWith(parentWithSep, StringComparison.OrdinalIgnoreCase);
    }

    private static string? MapStackToSkillId(string? stack)
    {
        if (string.IsNullOrWhiteSpace(stack)) return null;
        var s = stack.Trim();
        if (s.Contains("..") || s.Contains('/') || s.Contains('\\')) return null;

        if (s.Contains("winforms", StringComparison.OrdinalIgnoreCase))
            return "code-winforms";

        if (s.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("razor", StringComparison.OrdinalIgnoreCase))
            return "code-web";

        var words = s.Split([' ', ',', ';', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => w.Equals("maui", StringComparison.OrdinalIgnoreCase) ||
                           w.Equals("console", StringComparison.OrdinalIgnoreCase) ||
                           w.Equals("cli", StringComparison.OrdinalIgnoreCase)))
        {
            return "code-app";
        }

        if (s.Equals("maui", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("console", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("cli", StringComparison.OrdinalIgnoreCase))
        {
            return "code-app";
        }

        return null;
    }

    private static bool HasReparsePoint(string path)
    {
        try
        {
            var info = new FileInfo(path);
            FileSystemInfo? current = info;
            while (current != null)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    return true;

                if (current is FileInfo fi)
                    current = fi.Directory;
                else if (current is DirectoryInfo di)
                    current = di.Parent;
                else
                    break;
            }
        }
        catch
        {
            return true;
        }
        return false;
    }
}
