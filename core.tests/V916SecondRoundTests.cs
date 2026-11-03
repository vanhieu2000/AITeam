using AITeam.Core.Process;
using AITeam.Core.Tasks;
namespace AITeam.Core.Tests;
public sealed class V916SecondRoundTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"v916-d2-"+Guid.NewGuid().ToString("N"));
    private const string TaskText="## Tóm tắt\nSửa\n## Đọc\nAGENTS.md\n## Mục tiêu\nHoàn thiện\n## Phạm vi\n- `new.cs` (tạo mới)\n## Không làm\nKhông merge\n## Test nghiệm thu\n- `tests/AcceptanceNewTests.cs`\n## Tiêu chí hoàn thành\n- [ ] Đạt\n";
    public V916SecondRoundTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    private sealed class NoProcesses:IProcessRunner
    {
        public int Calls;
        public Task<ProcessResult> RunAsync(ProcessSpec spec,CancellationToken ct){Calls++;throw new InvalidOperationException("Không được build/test ở ca này");}
    }
    [Fact]public async Task Resume_only_inspects_structure_even_without_solution()
    {
        var fake=new NoProcesses();Assert.True((await TaskLinter.RunAsync(root,TaskText,fake,CancellationToken.None,resume:true)).Ok);Assert.Equal(0,fake.Calls);
        Assert.False((await TaskLinter.RunAsync(root,TaskText.Replace("## Đọc\nAGENTS.md\n",""),fake,CancellationToken.None,resume:true)).Ok);
    }
    [Theory][InlineData("README.md")][InlineData("team.sample.json")][InlineData("docs/design/mockup.html")]
    public async Task Documentation_json_and_mockup_do_not_require_tests_or_build(string path)
    {
        var fake=new NoProcesses();var text=TaskText.Replace("new.cs",path).Replace("- `tests/AcceptanceNewTests.cs`","Không — chỉ sửa dữ liệu/tài liệu");
        Assert.True((await TaskLinter.RunAsync(root,text,fake,CancellationToken.None)).Ok);Assert.Equal(0,fake.Calls);
    }
    [Fact]public async Task Explicit_exemption_requires_reason_and_does_not_skip_path_safety()
    {
        var fake=new NoProcesses();var text=TaskText.Replace("## Test nghiệm thu\n- `tests/AcceptanceNewTests.cs`","- **Test nghiệm thu:** Không — dùng kiểm thủ công đã nêu");
        Assert.True((await TaskLinter.RunAsync(root,text,fake,CancellationToken.None)).Ok);Assert.Equal(0,fake.Calls);
        Assert.False(TaskLinter.Inspect(root,text.Replace("new.cs","../escape.md")).Ok);
        Assert.False(TaskLinter.Inspect(root,text.Replace("Không — dùng kiểm thủ công đã nêu","Không —")).Ok);
    }
}
