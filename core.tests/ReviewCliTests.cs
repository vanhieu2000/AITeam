using System.Text.Json.Nodes;
using AITeam.Core.Process;

namespace AITeam.Core.Tests;

public sealed class ReviewCliTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "aiteam-cli-v408-" + Guid.NewGuid().ToString("N"));
    public ReviewCliTests() => Directory.CreateDirectory(_tmp);
    public void Dispose() { try { Directory.Delete(_tmp, true); } catch { } }

    [Fact]
    public async Task Missing_args_returns_2_and_auto_medium_task_returns_4_with_skip_json()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) dir = dir.Parent;
        var dll = Path.Combine(dir!.FullName, "core", "AITeam.Cli", "bin", "Debug", "net10.0-windows", "aiteam.dll");
        var runner = new ProcessRunner();

        var r1 = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "review", "--task-file", "x.md"], _tmp, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, r1.ExitCode);

        var (task, diff, outFile) = (Path.Combine(_tmp, "task.md"), Path.Combine(_tmp, "diff.txt"), Path.Combine(_tmp, "out.json"));
        await File.WriteAllTextAsync(task, "# V4-08 · Thử\n- **Độ khó:** M\n");
        await File.WriteAllTextAsync(diff, "diff --git a/a b/a\n+1\n");

        var r2 = await runner.RunAsync(new ProcessSpec("dotnet", [dll, "review", "--task-file", task, "--diff-file", diff, "--workdir", _tmp, "--logdir", _tmp, "--dev", "agy", "--out", outFile, "--auto"], _tmp, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(4, r2.ExitCode);
        Assert.StartsWith("SKIP: ", r2.StdOut.Trim());
        var j = JsonNode.Parse(await File.ReadAllTextAsync(outFile))!;
        Assert.Equal((4, "SKIPPED"), ((int)j["exitCode"]!, (string?)j["verdict"]));
    }
}
