using System.Text;
using AITeam.Core.Process;
using AITeam.Core.Review;

namespace AITeam.Core.Tests;

public sealed class AcceptanceV601ReviewRoundsTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "aiteam-v601-" + Guid.NewGuid().ToString("N"));
    private const string Changes = "{\"verdict\":\"CHANGES_REQUESTED\"}";
    public AcceptanceV601ReviewRoundsTests() => Directory.CreateDirectory(_tmp);
    public void Dispose() => Directory.Delete(_tmp, true);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("# Task\n## Review (Claude)\nChưa review.")]
    [InlineData("### Vòng 7 — ĐẠT\n## Kết quả\n### Vòng 1")]
    public void Empty_or_legacy_task_has_zero_rounds(string? text) =>
        Assert.Equal(0, ReviewRoundCounter.Count(text, null));

    [Theory]
    [InlineData("\n", "")]
    [InlineData("\r\n", "")]
    [InlineData("\n", "\uFEFF")]
    [InlineData("\r\n", "\uFEFF")]
    public void Counts_occurrences_not_max_or_verdict(string newline, string bom)
    {
        var text = bom + string.Join(newline, "# Task", "## Review (Claude)",
            "### Vòng 9 — ĐẠT", "### Vòng 2 — CẦN SỬA", "### Vòng 2", "### Vòng 001 — ghi chú");
        Assert.Equal(4, ReviewRoundCounter.Count(text, []));
    }

    [Fact]
    public void Only_headings_inside_first_review_section_count()
    {
        var text = """
            # Task
            ### Vòng 88
            ## Báo cáo của agent
            ### Vòng 77
            ## Review (Claude)
            Văn bản ### Vòng 66 — ĐẠT
            > ### Vòng 55
            - ### Vòng 44
            #### Vòng 33
            ### Vòng xyz
            ### Vòng 1abc
            ### Vòng 1 — ĐẠT
            #### Ghi chú
            ### Vòng 3 — CẦN SỬA
            ## Kết quả
            ### Vòng 22
            ## Review (Claude)
            ### Vòng 11
            """;
        Assert.Equal(2, ReviewRoundCounter.Count(text, []));
    }

    [Theory]
    [InlineData("```markdown", "```")]
    [InlineData("~~~~", "~~~~")]
    [InlineData("````", "````")]
    public void Code_blocks_cannot_open_or_close_review_sections(string opening, string closing)
    {
        var text = $"{opening}\n## Review (Claude)\n### Vòng 99\n{closing}\n" +
            $"## Review (Claude)\n{opening}\n## Kết quả\n### Vòng 88\n{closing}\n" +
            "    ### Vòng 77\n\t### Vòng 66\n### Vòng 1";
        Assert.Equal(1, ReviewRoundCounter.Count(text, []));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("{", 0)]
    [InlineData("{\"verdict\":\"CHANGES_REQUESTED\",}", 0)]
    [InlineData("[]", 0)]
    [InlineData("null", 0)]
    [InlineData("\"CHANGES_REQUESTED\"", 0)]
    [InlineData("{}", 0)]
    [InlineData("{\"verdict\":null}", 0)]
    [InlineData("{\"verdict\":1}", 0)]
    [InlineData("{\"verdict\":true}", 0)]
    [InlineData("{\"verdict\":[]}", 0)]
    [InlineData("{\"verdict\":{}}", 0)]
    [InlineData("{\"nested\":{\"verdict\":\"CHANGES_REQUESTED\"}}", 0)]
    [InlineData("{\"Verdict\":\"CHANGES_REQUESTED\"}", 0)]
    [InlineData("{\"verdict\":\"changes_requested\"}", 0)]
    [InlineData("{\"verdict\":\"PASS\"}", 0)]
    [InlineData("{\"verdict\":\"BLOCKED\"}", 0)]
    [InlineData("{\"verdict\":\"SKIPPED\"}", 0)]
    [InlineData("{\"verdict\":\"CHANGES_REQUESTED\"}", 1)]
    [InlineData("\uFEFF{\"verdict\":\"CHANGES_REQUESTED\"}", 1)]
    public void Json_requires_exact_root_string_verdict(string? json, int expected) =>
        Assert.Equal(expected, ReviewRoundCounter.Count(null, [json]));

    [Fact]
    public void Adds_both_sources_and_preserves_good_logs_around_bad_log()
    {
        var realLog = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "codex-review-last.json"));
        Assert.Equal(4, ReviewRoundCounter.Count("## Review (Claude)\n### Vòng 4\n### Vòng 4",
            [Changes, "{hỏng", realLog]));
    }

    [Fact]
    public async Task Cli_filters_exact_review_prefix_extension_and_top_directory()
    {
        var task = Write("phiếu có dấu.md", "## Review (Claude)\n### Vòng 7 — ĐẠT");
        var logs = Directory.CreateDirectory(Path.Combine(_tmp, "nhật ký có dấu")).FullName;
        WriteLog(logs, "V6-01.review-1.json", Changes);
        WriteLog(logs, "V6-01.review-2.json", "{");
        WriteLog(logs, "V6-01.review-3.json", Changes);
        WriteLog(logs, "V6-01.review-4.json", "{\"verdict\":\"PASS\"}");
        foreach (var name in new[] { "V6-010.review-1.json", "V6-02.review-1.json", "V6-01-1.json",
                     "V6-01.prep.json", "V6-01.review-1.json.bak", "xV6-01.review-1.json" })
            WriteLog(logs, name, Changes);
        var child = Directory.CreateDirectory(Path.Combine(logs, "thu-muc-con")   /* "con" là tên thiết bị DOS — Windows cấm (Claude sửa)*/).FullName;
        WriteLog(child, "V6-01.review-5.json", Changes);
        var result = await Cli("--task-file", task, "--logs", logs, "--task", "V6-01");
        AssertSuccess(result, 3);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    public async Task Missing_sources_contribute_zero_independently(bool hasTask, bool hasLogs, int count)
    {
        var task = Path.Combine(_tmp, "không có.md");
        var logs = Path.Combine(_tmp, "không có logs");
        if (hasTask) File.WriteAllText(task, "## Review (Claude)\n### Vòng 1");
        if (hasLogs) WriteLog(Directory.CreateDirectory(logs).FullName, "V6-01.review-1.json", Changes);
        AssertSuccess(await Cli("--task-file", task, "--logs", logs, "--task", "V6-01"), count);
    }

    [Fact]
    public async Task Unreadable_task_and_log_do_not_discard_readable_logs()
    {
        var task = Write("task.md", "## Review (Claude)\n### Vòng 1");
        WriteLog(_tmp, "V6-01.review-1.json", Changes);
        var lockedLog = Write("V6-01.review-2.json", Changes);
        using var taskLock = new FileStream(task, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var logLock = new FileStream(lockedLog, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertSuccess(await Cli("--task-file", task, "--logs", _tmp, "--task", "V6-01"), 1);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("a_1-2")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task Valid_task_id_boundaries_select_their_logs(string id)
    {
        WriteLog(_tmp, id + ".review-1.json", Changes);
        AssertSuccess(await Cli("--task-file", Path.Combine(_tmp, "missing.md"), "--logs", _tmp, "--task", id), 1);
    }

    [Theory]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("../V6-01")]
    [InlineData("..\\V6-01")]
    [InlineData("V6-*")]
    [InlineData("V6-?")]
    [InlineData("V6-[01]")]
    [InlineData("V6-01\n")]
    [InlineData("V6-01\r\n")]
    [InlineData("V6 01")]
    [InlineData("V6-Đ")]
    [InlineData("V6;01")]
    [InlineData("/V6-01")]
    public async Task Invalid_task_id_returns_exact_error(string id)
    {
        var result = await Cli("--task-file", "missing.md", "--logs", _tmp, "--task", id);
        AssertError(result, "LỖI: mã task không hợp lệ.");
    }

    [Theory]
    [InlineData()]
    [InlineData("--task-file", "x", "--logs", "x")]
    [InlineData("--task-file", "x", "--task", "A")]
    [InlineData("--logs", "x", "--task", "A")]
    [InlineData("--task-file", "--logs", "x", "--task", "A")]
    [InlineData("--task-file", "x", "--logs", "x", "--task")]
    [InlineData("--task-file", " ", "--logs", "x", "--task", "../bad")]
    [InlineData("--task-file", "x", "--logs", "x", "--task", "")]
    public async Task Missing_arguments_take_priority_and_return_exact_error(params string[] arguments) =>
        AssertError(await Cli(arguments), "LỖI: thiếu tham số bắt buộc (--task-file, --logs, --task).");

    private string Write(string name, string text)
    {
        var path = Path.Combine(_tmp, name);
        File.WriteAllText(path, text, new UTF8Encoding(true));
        return path;
    }

    private static void WriteLog(string directory, string name, string text) =>
        File.WriteAllText(Path.Combine(directory, name), text, new UTF8Encoding(false));

    private async Task<ProcessResult> Cli(params string[] arguments)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
        var result = await new ProcessRunner().RunAsync(new ProcessSpec("dotnet",
            new[] { dll, "review-rounds", "--root", _tmp }.Concat(arguments).ToArray(),
            _tmp, null, TimeSpan.FromSeconds(20)), CancellationToken.None);
        Assert.False(result.TimedOut);
        return result;
    }

    private static void AssertSuccess(ProcessResult result, int count)
    {
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(count + Environment.NewLine, result.StdOut);
        Assert.Equal("", result.StdErr);
    }

    private static void AssertError(ProcessResult result, string message)
    {
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("", result.StdOut);
        Assert.Equal(message + Environment.NewLine, result.StdErr);
    }
}
