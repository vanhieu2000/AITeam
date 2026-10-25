using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

/// <summary>
/// Kiểm hợp đồng tài liệu S1-02; chỉ đọc sáu skill nguồn, không chạy công cụ hay dữ liệu người dùng.
/// Thiếu file hoặc placeholder phải thất bại; không dùng build làm bằng chứng nghiệm thu UI.
/// </summary>
public sealed class AcceptanceS102SkillWorkflowTests
{
    public static TheoryData<string> Skills => new()
    {
        "06-chuan-bi-task", "07-code", "08-kiem-thu",
        "09-qc", "10-nghiem-thu", "11-publish-git"
    };

    private static readonly string[] Headings =
    [
        "Mục tiêu", "Đầu vào", "Đầu ra (định dạng)", "Các bước",
        "Checklist bắt buộc", "Lỗi hay gặp", "Ví dụ"
    ];

    [Theory]
    [MemberData(nameof(Skills))]
    public void SkillHasOrderedSectionsWithRealContent(string id)
    {
        var text = ReadSkill(id);
        var headings = Regex.Matches(text, @"(?m)^## (.+)\r?$")
            .Select(m => m.Groups[1].Value.Trim()).ToArray();
        Assert.Equal(Headings, headings);
        foreach (var heading in Headings)
        {
            var content = Section(text, heading);
            var visible = Regex.Replace(content, @"<!--[\s\S]*?-->", "").Trim();
            Assert.True(visible.Length >= 20, $"{id}: mục {heading} chưa có hướng dẫn.");
        }
        Assert.DoesNotContain("<!-- TODO(agent)", text);
    }

    [Theory]
    [MemberData(nameof(Skills))]
    public void SkillHasSourceAndConcreteGoodBadExamples(string id)
    {
        var text = ReadSkill(id);
        var errors = Section(text, "Lỗi hay gặp");
        Assert.Contains("Nguồn:", errors);
        Assert.Matches(@"AGENTS\.md|plans/R-010/brief\.md", errors);
        var examples = Section(text, "Ví dụ");
        Assert.Matches(@"(?m)^- Tốt:\s*\S.{19,}", examples);
        Assert.Matches(@"(?m)^- Xấu:\s*\S.{19,}", examples);
    }

    [Theory]
    [MemberData(nameof(Skills))]
    public void MandatoryChecklistProtectsStagePolicy(string id)
    {
        string[] required = id switch
        {
            "06-chuan-bi-task" => ["khung code", "test nghiệm thu", "NotImplementedException",
                "TODO(agent)", "LỖI: chưa làm", "stderr", "mã 1"],
            "07-code" => ["phạm vi", "Không sửa test nghiệm thu", "Không tự merge",
                "commit", "chưa xác minh", "Không báo pass khi chưa chạy"],
            "08-kiem-thu" => ["dotnet build AITeam.sln", "dotnet test AITeam.sln",
                "0 warning", "commit", "Không báo pass khi chưa chạy",
                "Không dùng build thay bằng chứng UI"],
            "09-qc" => ["độc lập", "commit", "Blocker", "Required", "Optional",
                "Không báo pass khi chưa chạy", "Không dùng build thay bằng chứng UI"],
            "10-nghiem-thu" => ["trực quan", "commit", "TK", "AG", "CK", "skill-note",
                "-Skill", "-Text", "-Source", "Không báo pass khi chưa chạy",
                "Không dùng build thay bằng chứng UI"],
            "11-publish-git" => ["người có quyền", "private", "duyệt merge",
                "clone sạch", "build", "test", "push", "commit", "Không force-push main",
                "Không báo pass khi chưa chạy"],
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
        var checklist = Section(ReadSkill(id), "Checklist bắt buộc");
        foreach (var phrase in required)
            Assert.Contains(phrase, checklist, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SkillsStayWithinPerFileAndTotalLineBudgets()
    {
        var total = 0;
        foreach (var row in Skills)
        {
            var text = ReadSkill(row);
            using var reader = new StringReader(text);
            var lines = 0;
            while (reader.ReadLine() is not null)
                lines++;
            Assert.InRange(lines, 1, 150);
            total += lines;
        }
        Assert.InRange(total, 6, 360);
    }

    private static string Section(string text, string heading)
    {
        var match = Regex.Match(text,
            @"(?m)^## " + Regex.Escape(heading) + @"\r?\n([\s\S]*?)(?=^## |\z)");
        Assert.True(match.Success, $"Thiếu mục: {heading}");
        return match.Groups[1].Value.Trim();
    }

    private static string ReadSkill(string id)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AITeam.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var path = Path.Combine(directory.FullName, "skills", id, "SKILL.md");
        Assert.True(File.Exists(path), $"Thiếu skill: {id}");
        return File.ReadAllText(path);
    }
}
