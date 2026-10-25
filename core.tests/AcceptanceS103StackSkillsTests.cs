using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

/// <summary>
/// Kiểm hợp đồng nội dung bốn tài liệu tĩnh S1-03; không chạy ứng dụng,
/// không đọc cấu hình AITeam hoặc hồ sơ người dùng. Thiếu file/nội dung phải thất bại.
/// </summary>
public sealed class AcceptanceS103StackSkillsTests
{
    private static readonly string[] Sections =
    [
        "Mục tiêu", "Đầu vào", "Đầu ra (định dạng)", "Các bước",
        "Checklist bắt buộc", "Lỗi hay gặp", "Ví dụ", "Nguồn"
    ];

    [Theory]
    [InlineData("code-winforms")]
    [InlineData("code-web")]
    [InlineData("code-app")]
    [InlineData("code-test")]
    public void Skill_has_complete_sections_examples_sources_and_at_most_150_lines(string skill)
    {
        var text = Read(skill);
        Assert.InRange(text.TrimEnd('\r', '\n').Split('\n').Length, 1, 150);
        Assert.DoesNotContain("TODO", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{{", text);
        foreach (var section in Sections)
            Assert.True(Section(text, section).Length >= 20, $"{skill}: mục {section} thiếu nội dung.");
        Assert.Contains("- [ ]", Section(text, "Checklist bắt buộc"));
        Assert.Contains("AGENTS.md", Section(text, "Lỗi hay gặp"));
        var examples = Section(text, "Ví dụ");
        Assert.Matches(@"(?is)Tốt[^`]*```[^\n]*\n.+?```", examples);
        Assert.Matches(@"(?is)Xấu[^`]*```[^\n]*\n.+?```", examples);
    }

    [Theory]
    [InlineData("code-winforms", "winforms-expert/SKILL.md")]
    [InlineData("code-web", "create-datadriven-aspnetcore/SKILL.md")]
    [InlineData("code-app", "learn.microsoft.com/dotnet/maui/")]
    [InlineData("code-test", "scaffold-dotnet-test-project/SKILL.md")]
    public void Sources_identify_original_skills_or_official_maui_sections(string skill, string source)
    {
        var sources = Section(Read(skill), "Nguồn").Replace('\\', '/');
        Assert.Contains(source, sources, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AGENTS.md", sources);
        if (skill == "code-web")
            Require(sources, "dotnet-webapi/SKILL.md", "author-component/SKILL.md");
        if (skill == "code-app")
            Require(sources, "app-lifecycle", "data-binding", "dependency-injection", "shell/navigation");
    }

    [Theory]
    [InlineData("code-winforms", new[] { "TableLayoutPanel", "FlowLayoutPanel", "DPI", "100", "125", "150", "200", "MinimumSize", "chuỗi dài", "Designer", "STA", "UiThread", "LayoutChecker", "UiSnapshot", "ẩn", "nghiệm thu" })]
    [InlineData("code-web", new[] { "Razor", "HTMX", "antiforgery", "hx-confirm", "form", "Html.Raw", "script", "ViewData", "partial", "render", "markup", "1000", "không tràn", "Blazor", "API", "khi dự án dùng", "CSS", "ghi cứng" })]
    [InlineData("code-app", new[] { "MAUI", "lifecycle", "binding", "DI", "navigation", "Main", "ngoại lệ", "stderr", "LỖI:", "ArgumentList", "UseShellExecute", "false", "timeout", "output", "shell" })]
    [InlineData("code-test", new[] { "xUnit", "return sớm", "Assert.True(true)", "Contains", "StartsWith", "DOS", "CON", "PRN", "AUX", "NUL", "COM1", "LPT1", "phần mở rộng", "hoa thường", "string.Concat", "ProjectReference", "AppContext.BaseDirectory", "agy", "codex", "fixture" })]
    public void Stack_guidance_covers_approved_contract(string skill, string[] terms)
    {
        var text = Read(skill);
        Require(text, terms);
        Assert.Contains("- [ ]", Section(text, "Checklist bắt buộc"));
        if (skill == "code-web")
        {
            var example = Regex.Match(Section(text, "Ví dụ"), @"(?is)Tốt[^`]*```[^\n]*\n(?<code>.+?)```");
            Assert.True(example.Success, "Thiếu ví dụ form HTMX tốt.");
            var code = example.Groups["code"].Value;
            Assert.Matches(@"(?is)<form\b[^>]*\bhx-confirm\s*=", code);
            Assert.DoesNotMatch(@"(?is)<button\b[^>]*\bhx-confirm\s*=", code);
            Assert.DoesNotContain("Html.Raw", code);
        }
    }

    [Fact]
    public void Winforms_background_test_never_instructs_show_and_checklist_addresses_R001()
    {
        var text = Read("code-winforms");
        Assert.DoesNotMatch(@"\bShow(?:Dialog)?\s*\(", text);
        Require(Section(text, "Checklist bắt buộc"), "R-001", "DPI", "chuỗi dài", "MinimumSize");
        Require(text, "cắt chữ", "chồng lấn", "PerformLayout");
    }

    [Fact]
    public void Cli_good_example_catches_exceptions_and_returns_failure_without_shell()
    {
        var examples = Section(Read("code-app"), "Ví dụ");
        var match = Regex.Match(examples, @"(?is)Tốt[^`]*```[^\n]*\n(?<code>.+?)```");
        Assert.True(match.Success, "Thiếu ví dụ CLI tốt.");
        var code = match.Groups["code"].Value;
        Require(code, "try", "catch", "Exception", "Console.Error.WriteLine", "LỖI:", "return 1");
        Require(Read("code-app"), "NotImplementedException", "LỖI: chưa làm");
    }

    [Fact]
    public void Test_checklist_covers_untrusted_paths_legacy_inputs_and_isolation()
    {
        var checklist = Section(Read("code-test"), "Checklist bắt buộc");
        Require(checklist, "..", "tuyệt đối", "symlink", "reparse point", "giới hạn",
            "dữ liệu hỏng", "ghi đè", "thư mục tạm", "HubPaths", "null", "rỗng", "trùng",
            "lần đầu", "dữ liệu cũ", "portable", "bản cài", "BOM", "debug");
        Require(Read("code-test"), "nuốt", "bin/Debug");
    }

    [Fact]
    public void Secret_example_builds_value_at_runtime_instead_of_embedding_a_key()
    {
        var text = Read("code-test");
        var examples = Section(text, "Ví dụ");
        Require(examples, "string.Concat", "new string");
        var keyPrefixes = new[] { string.Concat("s", "k-"), string.Concat("gh", "p_"), string.Concat("AI", "za") };
        foreach (var prefix in keyPrefixes)
            Assert.DoesNotMatch(Regex.Escape(prefix) + @"[A-Za-z0-9_\-]{20,}", text);
    }

    private static void Require(string text, params string[] terms)
    {
        foreach (var term in terms)
            Assert.Contains(term, text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Section(string text, string heading)
    {
        var match = Regex.Match(text, @"(?m)^## " + Regex.Escape(heading) + @"\s*\r?\n(?<body>[\s\S]*?)(?=^## |\z)");
        Assert.True(match.Success, $"Thiếu mục {heading}.");
        // Không để comment khung hoặc ví dụ TODO được tính là nội dung.
        return Regex.Replace(match.Groups["body"].Value, @"<!--[\s\S]*?-->", "").Trim();
    }

    private static string Read(string skill, [CallerFilePath] string sourceFile = "")
    {
        // Chỉ bốn tài liệu nguồn cố định; không tìm repo hay đọc dữ liệu vận hành.
        Assert.Contains(skill, new[] { "code-winforms", "code-web", "code-app", "code-test" });
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, ".."));
        var path = Path.Combine(root, "skills", skill, "SKILL.md");
        Assert.True(File.Exists(path), $"Thiếu tài liệu {skill}.");
        return File.ReadAllText(path, new UTF8Encoding(false, true));
    }
}
