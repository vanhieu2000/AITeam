using System.Text;
using AITeam.Core.Lessons;

namespace AITeam.Core.Tests;

// Test nghiệm thu W2-02 (Claude): luật chuẩn + bài học, chèn luật vào prompt. KHÔNG sửa file này.
public sealed class W202LessonServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-w202-" + Guid.NewGuid().ToString("N"));

    public W202LessonServiceTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private LessonService Service() => new(_root);

    private static bool HasBom(string file)
    {
        var b = File.ReadAllBytes(file);
        return b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
    }

    private Rule AddRule(LessonService s, string title, string[] types, string[] steps, string[] ais, string? body = null)
    {
        var lesson = s.AddDraft(title, body ?? "Thân " + title, "meo", null, null, null, null);
        return s.Promote(lesson.Id, title, body ?? "Thân " + title, types, steps, ais);
    }

    [Fact]
    public void AddDraft_assigns_increasing_ids_and_round_trips_with_bom()
    {
        var s = Service();
        var a = s.AddDraft("Giữ BOM file mới", "Agent hay làm mất BOM.", "meo", "R-014", "V10-03", "code", "opencode");
        var b = s.AddDraft("Commit sớm", "Phiên có thể bị ngắt.", "cai-tien", null, null, null, null);

        Assert.Equal("LS-001", a.Id);
        Assert.Equal("LS-002", b.Id);
        var file = Path.Combine(_root, "lessons", "LS-001.md");
        Assert.True(File.Exists(file));
        Assert.True(HasBom(file));

        var read = new LessonService(_root).Lessons().Single(l => l.Id == "LS-001");
        Assert.Equal("Giữ BOM file mới", read.Title);
        Assert.Equal("Agent hay làm mất BOM.", read.Body.Trim());
        Assert.Equal("meo", read.Kind);
        Assert.Equal("R-014", read.RequestId);
        Assert.Equal("V10-03", read.TaskId);
        Assert.Equal("code", read.Step);
        Assert.Equal("opencode", read.Ai);
        Assert.Equal(LessonStatus.Draft, read.Status);
    }

    [Fact]
    public void Promote_creates_active_rule_and_blocks_double_promote_or_drop()
    {
        var s = Service();
        var lesson = s.AddDraft("Mẹo", "Thân", "meo", null, null, null, null);
        var rule = s.Promote(lesson.Id, "Luật 1", "Thân luật", ["tinh-nang-loi"], ["code"], []);

        Assert.Equal("L-001", rule.Id);
        Assert.Equal(RuleStatus.Active, rule.Status);
        Assert.Equal(lesson.Id, rule.SourceLesson);
        Assert.True(File.Exists(Path.Combine(_root, "rules", "L-001.md")));
        var promoted = s.Lessons(LessonStatus.Promoted).Single();
        Assert.Equal("L-001", promoted.RuleId);

        Assert.Throws<InvalidOperationException>(() => s.Promote(lesson.Id, "Lại", "x", [], [], []));
        Assert.Throws<InvalidOperationException>(() => s.Drop(lesson.Id));

        var other = s.AddDraft("Bỏ", "x", "meo", null, null, null, null);
        s.Drop(other.Id);
        Assert.Equal(LessonStatus.Dropped, s.Lessons().Single(l => l.Id == other.Id).Status);
    }

    [Fact]
    public void ForTask_matches_scope_and_skips_retired()
    {
        var s = Service();
        var a = AddRule(s, "A mọi loại bước code", [], ["code"], []);
        AddRule(s, "B chỉ giao diện", ["giao-dien"], [], []);
        var c = AddRule(s, "C đã bỏ", [], [], []);
        s.Retire(c.Id);

        var ids = s.ForTask("tinh-nang-loi", "code", null).Select(r => r.Id).ToList();
        Assert.Equal([a.Id], ids);
        Assert.Contains("B chỉ giao diện", s.ForTask("giao-dien", "code", null).Select(r => r.Title));
        Assert.DoesNotContain(s.ForTask("tinh-nang-loi", "qc", null), r => r.Id == a.Id);
    }

    [Fact]
    public void PromptSection_clips_strips_control_chars_and_caps_at_15()
    {
        var s = Service();
        Assert.Equal("", s.PromptSection("tinh-nang-loi", "code", null));

        AddRule(s, "Dài", [], [], [], new string('x', 1000));
        AddRule(s, "Bẩn", [], [], [], "trước\u001b[31msau");
        var section = s.PromptSection("tinh-nang-loi", "code", null);
        Assert.StartsWith("LUẬT CHUẨN ÁP DỤNG CHO VIỆC NÀY (bắt buộc):", section);
        Assert.DoesNotContain("\u001b", section);
        foreach (var line in section.Split('\n').Where(l => l.StartsWith("- [L-", StringComparison.Ordinal)))
            Assert.True(line.Length <= 300 + 40, "Dòng luật quá dài: " + line.Length);

        for (var i = 0; i < 18; i++) AddRule(s, "Luật " + i, [], [], []);
        var many = s.PromptSection("tinh-nang-loi", "code", null);
        Assert.Equal(15, many.Split('\n').Count(l => l.StartsWith("- [L-", StringComparison.Ordinal)));
        Assert.Contains("còn 5 luật", many);
    }

    [Fact]
    public void Broken_or_foreign_rule_files_are_ignored_without_throwing()
    {
        var s = Service();
        var ok = AddRule(s, "Tốt", [], [], []);
        File.WriteAllText(Path.Combine(_root, "rules", "L-009.md"), "rác không đúng định dạng\n\u0000", new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(_root, "rules", "x.md"), "# x", new UTF8Encoding(true));

        var rules = new LessonService(_root).Rules();
        Assert.Contains(rules, r => r.Id == ok.Id);
        Assert.DoesNotContain(rules, r => r.Id == "L-009");
        Assert.DoesNotContain(rules, r => r.Id == "x");
        Assert.Contains(ok.Id, new LessonService(_root).PromptSection("sua-loi", "code", null));
    }

    [Theory]
    [InlineData("../x")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void Path_like_ids_are_rejected(string bad)
    {
        var s = Service();
        Assert.Throws<ArgumentException>(() => s.AddDraft("t", "b", "meo", null, bad, null, null));
        Assert.Throws<ArgumentException>(() => s.Drop(bad));
    }
}
