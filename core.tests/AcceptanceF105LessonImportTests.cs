using AITeam.Core.Lessons;
using AITeam.Core.Process;

namespace AITeam.Core.Tests;

/// <summary>Test nghiệm thu F1-05 (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-05.
/// Kho bài học có 0 mục trong khi CLAUDE.md / AGENTS.md có ~40 bài học ⇒ nhập vào thành bản nháp, chạy lại không nhân đôi.</summary>
public sealed class AcceptanceF105LessonImportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-f105-" + Guid.NewGuid().ToString("N"));

    public AcceptanceF105LessonImportTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    // Trích THẬT từ CLAUDE.md (04/11/2026), rút gọn: có mục khác trước / sau, gạch đầu dòng có dòng con thụt lề.
    private const string ClaudeMd = """
        # Sổ tay quản lý team AI

        ## Bảo mật (bắt buộc)
        - KHÔNG BAO GIỜ viết bí mật vào code.

        ## Bài học vận hành
        - PowerShell 5.1: gọi exe với tham số có dấu ngoặc kép bị sai → lời giao việc ghi ra file `.prompt.md`.
        - Script `.ps1` phải lưu UTF-8 **có BOM** (PowerShell 5.1 đọc sai tiếng Việt nếu không có).
        - Muốn dừng một agent: `team.ps1 stop -Task X` (dừng **cả lệnh `assign` bao ngoài**), không chỉ tiến trình `agy`
          — nếu không, cổng kiểm tra sẽ tự giao lại và agent chạy tiếp.

        ## Cần người dùng
        - Đăng nhập GitHub.
        """;

    // Trích THẬT từ AGENTS.md (04/11/2026), rút gọn.
    private const string AgentsMd = """
        ## Lỗi hay gặp (đã xảy ra nhiều lần — bắt buộc tránh)
        - **Giữ nguyên BOM UTF-8** của file đang có BOM khi sửa; script `.ps1` luôn lưu UTF-8 có BOM.
        - **Không viết test luôn pass**: không `if (...) return;` sớm, không `Assert.True(true)`.

        ## Lỗi hay gặp — bổ sung (thêm lỗi MỚI ở cuối file)
        - Dùng `Assert.Single(x, f => …)`, KHÔNG `Assert.Single(x.Where(…))` — cảnh báo xUnit2031 làm build `-warnaserror` hỏng.
        """;

    [Fact]
    public void Extract_reads_only_lesson_sections_one_item_per_top_bullet()
    {
        var items = LessonImporter.Extract(ClaudeMd, "CLAUDE.md");
        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.Equal("CLAUDE.md", i.Source));
        Assert.DoesNotContain(items, i => i.Body.Contains("bí mật vào code") || i.Body.Contains("Đăng nhập GitHub"));

        Assert.Equal("PowerShell 5.1", items[0].Title);
        Assert.StartsWith("PowerShell 5.1: gọi exe", items[0].Body);
        Assert.Equal("Script `.ps1` phải lưu UTF-8 có BOM (PowerShell 5.1 đọc sai tiếng Việt nếu không có)", items[1].Title);
        // Dòng con thụt lề nối vào bài học của gạch đầu dòng trước
        Assert.Contains("cổng kiểm tra sẽ tự giao lại", items[2].Body);
        Assert.Equal("Muốn dừng một agent", items[2].Title);
    }

    [Fact]
    public void Extract_reads_every_section_starting_with_loi_hay_gap()
    {
        var items = LessonImporter.Extract(AgentsMd, "AGENTS.md");
        Assert.Equal(3, items.Count);
        Assert.Equal("Giữ nguyên BOM UTF-8 của file đang có BOM khi sửa; script `.ps1` luôn lưu UTF-8 có BOM", items[0].Title);
        Assert.Equal("Không viết test luôn pass", items[1].Title);
        Assert.Contains("xUnit2031", items[2].Body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("# Tiêu đề\n\n## Khác\n- a")]
    public void Extract_without_lesson_sections_is_empty(string? md)
        => Assert.Empty(LessonImporter.Extract(md!, "x.md"));

    [Fact]
    public void Title_is_capped_at_120_chars()
    {
        var md = "## Bài học\n- " + new string('a', 300) + "\n";
        var item = Assert.Single(LessonImporter.Extract(md, "x.md"));
        Assert.Equal(120, item.Title.Length);
    }

    [Fact]
    public void Import_adds_drafts_with_source_and_second_run_adds_nothing()
    {
        var lessons = new LessonService(_root);
        var items = LessonImporter.Extract(ClaudeMd, "CLAUDE.md").Concat(LessonImporter.Extract(AgentsMd, "AGENTS.md")).ToList();

        var first = LessonImporter.Import(lessons, items);
        Assert.Equal(6, first.Added);
        Assert.Equal(0, first.Skipped);
        var drafts = lessons.Lessons(LessonStatus.Draft);
        Assert.Equal(6, drafts.Count);
        Assert.All(drafts, d => Assert.Equal("meo", d.Kind));
        Assert.Contains(drafts, d => d.Body.EndsWith("Nguồn: CLAUDE.md", StringComparison.Ordinal));
        Assert.Contains(drafts, d => d.Body.EndsWith("Nguồn: AGENTS.md", StringComparison.Ordinal));

        var second = LessonImporter.Import(lessons, items);
        Assert.Equal(0, second.Added);
        Assert.Equal(6, second.Skipped);
        Assert.Equal(6, lessons.Lessons().Count);
    }

    [Fact]
    public void Import_skips_titles_already_present_in_any_status_ignoring_case_and_spaces()
    {
        var lessons = new LessonService(_root);
        var dropped = lessons.AddDraft("powershell   5.1", "cũ", "meo", null, null, null, null);
        lessons.Drop(dropped.Id);

        var result = LessonImporter.Import(lessons, LessonImporter.Extract(ClaudeMd, "CLAUDE.md"));
        Assert.Equal(2, result.Added);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public void Import_truncates_body_to_4000_chars_instead_of_failing()
    {
        var lessons = new LessonService(_root);
        var result = LessonImporter.Import(lessons, [new ImportedLesson("Dài", new string('x', 5000), "x.md")]);
        Assert.Equal(1, result.Added);
        Assert.True(lessons.Lessons().Single().Body.Length <= 4000);
    }

    [Fact]
    public async Task Cli_lessons_import_reads_files_and_prints_counts()
    {
        File.WriteAllText(Path.Combine(_root, "CLAUDE.md"), ClaudeMd);
        File.WriteAllText(Path.Combine(_root, "AGENTS.md"), AgentsMd);
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        var runner = new ProcessRunner();

        var first = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "lessons-import", "--root", _root], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Đã thêm 6, bỏ qua 0", first.StdOut);

        var second = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "lessons-import", "--root", _root], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Đã thêm 0, bỏ qua 6", second.StdOut);
    }

    [Fact]
    public async Task Cli_lessons_import_missing_file_is_skipped_not_fatal()
    {
        File.WriteAllText(Path.Combine(_root, "AGENTS.md"), AgentsMd);
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        var result = await new ProcessRunner().RunAsync(new ProcessSpec("dotnet", [dll, "lessons-import", "--root", _root], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Đã thêm 3, bỏ qua 0", result.StdOut);
    }
}
