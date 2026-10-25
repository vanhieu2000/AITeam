using System.Text.Json.Nodes;
using AITeam.Core.Process;
using AITeam.Core.Review;

namespace AITeam.Core.Tests;

public sealed class ReviewCliTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "aiteam-cli-v408-" + Guid.NewGuid().ToString("N"));
    public ReviewCliTests() => Directory.CreateDirectory(_tmp);
    public void Dispose() { try { Directory.Delete(_tmp, true); } catch { } }

    [Fact]
    public async Task Missing_args_returns_2_and_auto_medium_task_returns_4_with_skip_json()
    {
        // aiteam.dll được chép kèm vào thư mục test (ProjectReference) ⇒ luôn là bản vừa build, không phụ thuộc thứ tự build.
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);
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

    [Fact]
    public void Counter_ignores_similar_heading_before_exact_review_heading()
    {
        var text = """
            # Task
            ## Review (Claude) khác
            ### Vòng 99
            ## Review (Claude)
            ### Vòng 1
            ### Vòng 2
            ## Kết quả
            ### Vòng 3
            """;
        Assert.Equal(2, ReviewRoundCounter.Count(text, null));
    }

    [Fact]
    public async Task Cli_ignores_logs_directory_under_parent_junction()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        Assert.True(File.Exists(dll), dll);

        var realDir = Path.Combine(_tmp, "real_target");
        var realLogs = Path.Combine(realDir, "logs");
        Directory.CreateDirectory(realLogs);
        File.WriteAllText(Path.Combine(realLogs, "V6-01.review-1.json"), "{\"verdict\":\"CHANGES_REQUESTED\"}");

        var junctionDir = Path.Combine(_tmp, "parent_junction");
        if (OperatingSystem.IsWindows())
        {
            var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junctionDir}\" \"{realDir}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
            p?.WaitForExit();
        }
        else
        {
            Directory.CreateSymbolicLink(junctionDir, realDir);
        }

        var junctionLogs = Path.Combine(junctionDir, "logs");
        var taskFile = Path.Combine(_tmp, "empty_task.md");
        File.WriteAllText(taskFile, "# Task");

        var runner = new ProcessRunner();
        var result = await runner.RunAsync(new ProcessSpec("dotnet",
            new[] { dll, "review-rounds", "--root", _tmp, "--task-file", taskFile, "--logs", junctionLogs, "--task", "V6-01" },
            _tmp, null, TimeSpan.FromSeconds(20)), CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0" + Environment.NewLine, result.StdOut);
    }
}
