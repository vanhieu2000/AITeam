using System.Text;
using AITeam.Core.Lessons;
using AITeam.Core.Process;

namespace AITeam.Core.Tests;

public sealed class W202CliTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-cli-w202-" + Guid.NewGuid().ToString("N"));

    public W202CliTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public async Task RulesForTask_with_task_file_sua_loi_outputs_rule()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var runner = new ProcessRunner();

        var lessonService = new LessonService(_root);
        var draft = lessonService.AddDraft("Kiểm tra kỹ trước khi commit", "Luật sửa lỗi bắt buộc.", "meo", null, null, null, null);
        lessonService.Promote(draft.Id, "Kiểm tra kỹ trước khi commit", "Luật sửa lỗi bắt buộc.", ["sua-loi"], ["code"], []);

        var taskFile = Path.Combine(_root, "S-01-fix-bug.md");
        var taskContent = @"# S-01 · Sửa lỗi null reference
- **Loại việc:** Sửa lỗi
- **Agent:** B
- **Nhánh:** `fix/null-ref`
";
        await File.WriteAllTextAsync(taskFile, taskContent, new UTF8Encoding(true));

        var result = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "rules-for-task", "--root", _root, "--task-file", taskFile, "--step", "code"], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("LUẬT CHUẨN ÁP DỤNG CHO VIỆC NÀY (bắt buộc):", result.StdOut);
        Assert.Contains("L-001", result.StdOut);
        Assert.Contains("Kiểm tra kỹ trước khi commit", result.StdOut);
    }

    [Fact]
    public async Task RulesForTask_missing_args_returns_2_and_empty_stdout()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "rules-for-task", "--root", _root], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("", result.StdOut.Trim());
        Assert.Contains("LỖI", result.StdErr);
    }

    [Fact]
    public async Task RulesForTask_nonexistent_file_returns_2_and_empty_stdout()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var runner = new ProcessRunner();

        var nonexistent = Path.Combine(_root, "not-found.md");
        var result = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "rules-for-task", "--root", _root, "--task-file", nonexistent, "--step", "code"], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("", result.StdOut.Trim());
        Assert.Contains("LỖI", result.StdErr);
    }

    [Fact]
    public async Task LessonAdd_creates_draft_lesson_and_prints_id()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "lesson-add", "--root", _root, "--title", "Mẹo giữ BOM", "--body", "Luôn giữ BOM khi sửa file.", "--kind", "meo", "--task", "V10-02"], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("LS-001", result.StdOut.Trim());

        var file = Path.Combine(_root, "lessons", "LS-001.md");
        Assert.True(File.Exists(file));
        var content = await File.ReadAllTextAsync(file, Encoding.UTF8);
        Assert.Contains("Mẹo giữ BOM", content);
        Assert.Contains("V10-02", content);
    }

    [Fact]
    public async Task LessonAdd_missing_args_returns_2_and_stderr()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var runner = new ProcessRunner();

        var result = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "lesson-add", "--root", _root, "--title", "Thiếu body"], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("LỖI", result.StdErr);
    }
}