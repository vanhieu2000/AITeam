using System.Text.RegularExpressions;
using AITeam.Core.Process;
namespace AITeam.Core.Tasks;

public sealed record TaskLintResult(bool Ok, IReadOnlyList<string> Errors);
public static class TaskLinter
{
    private static readonly string[] Required = ["Tóm tắt", "Đọc", "Mục tiêu", "Phạm vi", "Không làm", "Test nghiệm thu", "Tiêu chí hoàn thành"];
    private static string Section(string text, string name)
    {
        var match = Regex.Match(text, @"(?m)^##\s+" + Regex.Escape(name) + @"[^\r\n]*\r?\n(?<body>[\s\S]*?)(?=^##\s|\z)");
        return match.Success ? match.Groups["body"].Value.Trim() : "";
    }
    public static TaskLintResult Inspect(string root, string text)
    {
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", "");
        var errors = new List<string>();
        foreach (var name in Required)
            if (!(name == "Test nghiệm thu" && HasTestExemption(text)) && string.IsNullOrWhiteSpace(Section(text, name)))
                errors.Add("TK: thiếu nội dung mục " + name);
        if (!Regex.IsMatch(Section(text, "Tiêu chí hoàn thành"), @"(?m)^\s*-\s+\[[ xX]\]\s+\S")) errors.Add("TK: tiêu chí thiếu checkbox có nội dung");
        var scope = Section(text, "Phạm vi");
        var paths = Regex.Matches(scope, "`([^`]+)`");
        if (paths.Count == 0) errors.Add("TK: Phạm vi thiếu đường dẫn trong dấu nháy ngược");
        foreach (Match match in paths)
        {
            var relative = match.Groups[1].Value.Replace('\\', '/');
            if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Split('/').Contains("..")) { errors.Add("TK: đường dẫn Phạm vi không an toàn: " + relative); continue; }
            var path = Path.Combine(root, relative);
            var lineStart = scope.LastIndexOf('\n', match.Index) + 1;
            var lineEnd = scope.IndexOf('\n', match.Index); if (lineEnd < 0) lineEnd = scope.Length;
            var line = scope[lineStart..lineEnd];
            if (!File.Exists(path) && !Directory.Exists(path) && !line.Contains("tạo mới", StringComparison.OrdinalIgnoreCase)) errors.Add("TK: đường dẫn không tồn tại, chưa ghi tạo mới: " + relative);
        }
        if (!NoLogic(text) && AcceptanceFilters(text).Count == 0) errors.Add("TN: thiếu đường dẫn file test nghiệm thu .cs");
        return new(errors.Count == 0, errors);
    }
    private static bool HasTestExemption(string text) => Regex.IsMatch(text,
        @"(?im)^[ \t]*-[ \t]*\*\*Test nghiệm thu:\*\*[ \t]*Không[ \t]*[—–-][ \t]*\S[^\r\n]*$");
    private static bool NoLogic(string text)
    {
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", "");
        if (HasTestExemption(text)) return true;
        var paths = Regex.Matches(Section(text, "Phạm vi"), "`([^`]+)`").Select(m => m.Groups[1].Value.Replace('\\', '/')).ToArray();
        return paths.Length > 0 && paths.All(p =>
            p.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            (p.StartsWith("docs/design/", StringComparison.OrdinalIgnoreCase) &&
                new[] { ".html", ".css", ".png", ".svg" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)) ||
            p.TrimEnd('/') is "docs" or "plans" or "requests");
    }
    private static IReadOnlyList<string> AcceptanceFilters(string text) => Regex.Matches(Section(text, "Test nghiệm thu"), "`([^`]+\\.cs)`")
        .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value.Replace('\\', '/'))).Distinct().ToList();

    public static async Task<TaskLintResult> RunAsync(string root, string text, IProcessRunner runner, CancellationToken ct, bool resume = false)
    {
        var check = Inspect(root, text); if (!check.Ok || resume || NoLogic(text)) return check;
        var solutions = Directory.GetFiles(root, "*.sln");
        if (solutions.Length != 1) return new(false, ["TN: cần đúng một solution .sln tại gốc để kiểm test nghiệm thu"]);
        var build = await runner.RunAsync(new("dotnet", ["build", solutions[0], "-warnaserror", "-nodeReuse:false"], root, null, TimeSpan.FromMinutes(3)), ct);
        if (build.ExitCode != 0 || build.TimedOut) return new(false, ["TN: test nghiệm thu chưa build sạch với -warnaserror: " + Clip(build.StdOut + build.StdErr)]);
        var filter = string.Join('|', AcceptanceFilters(Regex.Replace(text, @"<!--[\s\S]*?-->", "")).Select(n => "FullyQualifiedName~" + n));
        var test = await runner.RunAsync(new("dotnet", ["test", solutions[0], "--no-build", "--filter", filter, "--blame-hang-timeout", "2m"], root, null, TimeSpan.FromMinutes(3)), ct);
        var output = test.StdOut + test.StdErr;
        if (test.TimedOut || output.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase) || output.Contains("No test matches", StringComparison.OrdinalIgnoreCase) || output.Contains("No test is available", StringComparison.OrdinalIgnoreCase))
            return new(false, ["TN: harness lỗi, không tìm thấy test hoặc treo: " + Clip(output)]);
        if (test.ExitCode == 0) return new(false, ["TN: test nghiệm thu chưa đỏ trước khi giao; cần quản lý xác nhận test bắt được thiếu code"]);
        if (!Regex.IsMatch(output, @"Failed:\s*[1-9]|\[FAIL\]|Failed\s+\S", RegexOptions.IgnoreCase)) return new(false, ["TN: lệnh test lỗi nhưng chưa chứng minh test đã chạy và đỏ: " + Clip(output)]);
        return new(true, []);
    }
    private static string Clip(string text)
    {
        var safe = AITeam.Core.Research.SecretRedactor.Redact(text).Text;
        return safe.Length <= 2000 ? safe : safe[..2000];
    }
}
