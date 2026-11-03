using AITeam.Core.Process;
using AITeam.Core.Tasks;
namespace AITeam.Core.Tests;
public sealed class V916TaskLintTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "v916-" + Guid.NewGuid().ToString("N"));
    private const string Valid = "## Tóm tắt\nNội dung\n## Đọc\nTài liệu\n## Mục tiêu\nLàm việc\n## Phạm vi\n- `new.cs` (tạo mới)\n## Không làm\nKhông merge\n## Test nghiệm thu\n- `tests/AcceptanceNewTests.cs`\n## Tiêu chí hoàn thành\n- [ ] Có kết quả\n";
    public V916TaskLintTests() { Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "Test.sln"), ""); }
    public void Dispose() => Directory.Delete(root, true);
    private sealed class Fake(params ProcessResult[] results) : IProcessRunner
    {
        public List<ProcessSpec> Calls { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct) { Calls.Add(spec); return Task.FromResult(results[Calls.Count - 1]); }
    }
    private static ProcessResult Result(int code, string output) => new(code, output, "", TimeSpan.Zero, false);
    [Fact] public async Task Complete_task_requires_clean_build_and_red_test()
    {
        var fake = new Fake(Result(0, "Build succeeded"), Result(1, "Failed: 1"));
        Assert.True((await TaskLinter.RunAsync(root, Valid, fake, CancellationToken.None)).Ok);
        Assert.Contains("-warnaserror", fake.Calls[0].Arguments);
        Assert.Contains("-nodeReuse:false", fake.Calls[0].Arguments);
        Assert.Contains("FullyQualifiedName~AcceptanceNewTests", fake.Calls[1].Arguments);
    }
    [Fact] public async Task Missing_section_or_unsafe_scope_never_launches_build()
    {
        var fake = new Fake();
        Assert.False((await TaskLinter.RunAsync(root, Valid.Replace("## Đọc\nTài liệu\n", ""), fake, CancellationToken.None)).Ok);
        Assert.Empty(fake.Calls);
        Assert.False(TaskLinter.Inspect(root, Valid.Replace("new.cs", "../outside.cs")).Ok);
        Assert.False(TaskLinter.Inspect(root, Valid.Replace(" (tạo mới)", "")).Ok);
    }
    [Fact] public async Task Warning_build_blocks_before_test()
    {
        var fake = new Fake(Result(1, "warning treated as error"));
        var result = await TaskLinter.RunAsync(root, Valid, fake, CancellationToken.None);
        Assert.False(result.Ok); Assert.Single(fake.Calls); Assert.Contains("TN:", Assert.Single(result.Errors));
    }
    [Fact] public async Task Red_acceptance_in_one_project_is_accepted_when_other_test_project_has_no_match()
    {
        var output = "Failed!  - Failed:    12, Passed:     0, Skipped:     0, Total:    12 - AITeam.Core.Tests.dll\n"
            + "No test matches the given testcase filter `FullyQualifiedName~V1002AiAdapterTests` in D:\\x\\AITeam.Hub.Tests.dll";
        var fake = new Fake(Result(0, ""), Result(1, output));
        Assert.True((await TaskLinter.RunAsync(root, Valid, fake, CancellationToken.None)).Ok);
    }
    [Theory] [InlineData(0, "Passed: 1")] [InlineData(1, "Failed: 1 NullReferenceException")] [InlineData(1, "No test matches")] [InlineData(1, "process crashed")]
    public async Task Green_harness_errors_and_missing_tests_are_not_accepted_as_red(int code, string output)
    {
        var fake = new Fake(Result(0, ""), Result(code, output));
        Assert.False((await TaskLinter.RunAsync(root, Valid, fake, CancellationToken.None)).Ok);
    }
}
