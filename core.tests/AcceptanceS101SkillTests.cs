using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

/// <summary>Kiểm hợp đồng tài liệu; Claude vẫn review ý nghĩa và nguồn bằng chứng.</summary>
public sealed class AcceptanceS101SkillTests
{
    private static readonly string[] Sections =
    [
        "Mục tiêu", "Đầu vào", "Đầu ra (định dạng)", "Các bước",
        "Checklist bắt buộc", "Lỗi hay gặp", "Ví dụ ngắn tốt / xấu"
    ];

    [Theory]
    [InlineData("01-tiep-nhan")]
    [InlineData("02-y-tuong")]
    [InlineData("03-research")]
    [InlineData("04-ke-hoach")]
    [InlineData("05-duyet-ke-hoach")]
    public void Skill_HasIdentitySevenNonemptySectionsAndNoUnfinishedScaffold(string id)
    {
        var text = ReadSkill(id);
        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.InRange(lines.Length, 1, 150);
        Assert.StartsWith("---\n", text.Replace("\r\n", "\n"));
        Assert.Contains($"name: {id}", text);
        Assert.Matches(@"(?m)^description: \S.+$", text);
        Assert.Contains($"# {id}", text);
        Assert.DoesNotContain("TODO", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NotImplementedException", text);
        Assert.Equal(Sections, Regex.Matches(text, @"(?m)^## (.+?)\r?$")
            .Select(match => match.Groups[1].Value).ToArray());
        foreach (var title in Sections)
        {
            var body = Section(text, title);
            Assert.False(string.IsNullOrWhiteSpace(body), $"{id}: mục {title} rỗng.");
            Assert.DoesNotContain("<!--", body);
        }
    }

    [Theory]
    [InlineData("01-tiep-nhan")]
    [InlineData("02-y-tuong")]
    [InlineData("03-research")]
    [InlineData("04-ke-hoach")]
    [InlineData("05-duyet-ke-hoach")]
    public void Skill_HasChecklistExamplesAndTraceableRealError(string id)
    {
        var text = ReadSkill(id);
        Assert.Matches(@"(?m)^- \[[ x]\] .+", Section(text, "Checklist bắt buộc"));
        var examples = Section(text, "Ví dụ ngắn tốt / xấu");
        Assert.Matches(@"(?im)^- Tốt: .{20,}", examples);
        Assert.Matches(@"(?im)^- Xấu: .{20,}", examples);
        var errors = Section(text, "Lỗi hay gặp");
        var citations = Regex.Matches(errors, @"AGENTS\.md:(\d+)");
        Assert.NotEmpty(citations);
        var sourceLines = File.ReadAllLines(Path.Combine(SourceRoot(), "AGENTS.md"));
        foreach (Match citation in citations)
        {
            var line = int.Parse(citation.Groups[1].Value);
            Assert.InRange(line, 1, sourceLines.Length);
            Assert.Matches(@"(?i)(IMP-\d+|V\d+-\d+|H-\d+|R-\d+)", sourceLines[line - 1]);
        }
        Assert.Matches(@"(?i)(IMP-\d+|V\d+-\d+|H-\d+|R-\d+)", errors);
    }

    [Theory]
    [InlineData("01-tiep-nhan", "yêu cầu", "phạm vi", "thiếu", "cũ", "xác nhận")]
    [InlineData("02-y-tuong", "phương án", "đánh đổi", "giả định", "CHƯA XÁC MINH", "bằng chứng")]
    [InlineData("03-research", "chỉ đọc", "file:dòng", "CHƯA XÁC MINH", "mâu thuẫn", "không chạy")]
    [InlineData("04-ke-hoach", "400", "15 phút", "phụ thuộc", "nghiệm thu", "rủi ro")]
    [InlineData("05-duyet-ke-hoach", "phạm vi", "phụ thuộc", "rủi ro", "song song", "CẦN SỬA")]
    public void Skill_ContainsStageDecisionsInInstructionsRatherThanOnlyExamples(
        string id, string first, string second, string third, string fourth, string fifth)
    {
        var text = ReadSkill(id);
        var instructions = string.Join("\n", Sections.Take(5).Select(title => Section(text, title)));
        foreach (var required in new[] { first, second, third, fourth, fifth })
            Assert.Contains(required, instructions, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadSkill(string id)
    {
        var path = Path.Combine(SourceRoot(), "skills", id, "SKILL.md");
        Assert.True(File.Exists(path), $"Thiếu skill {id}.");
        return File.ReadAllText(path);
    }

    private static string Section(string text, string title)
    {
        var match = Regex.Match(text,
            @"(?ms)^## " + Regex.Escape(title) + @"\r?\n(.*?)(?=^## |\z)");
        Assert.True(match.Success, $"Thiếu mục {title}.");
        return match.Groups[1].Value.Trim();
    }

    // Chỉ đọc tài liệu mã nguồn cố định, không dò repo hay đọc dữ liệu vận hành/hồ sơ người dùng.
    private static string SourceRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, ".."));
}
