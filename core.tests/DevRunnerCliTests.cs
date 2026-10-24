using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace AITeam.Core.Tests;

public sealed class DevRunnerCliTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-cli-v409-" + Guid.NewGuid().ToString("N"));

    public DevRunnerCliTests() => Directory.CreateDirectory(_root);
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private static async Task<(int Exit, string Out, string Err)> RunCli(params string[] cliArgs)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "aiteam.dll"));
        foreach (var a in cliArgs) psi.ArgumentList.Add(a);
        using var p = System.Diagnostics.Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, await stdout, await stderr);
    }

    [Fact]
    public async Task Run_missing_required_args_returns_2()
    {
        var r1 = await RunCli("run", "--provider", "codex", "--workdir", _root);
        Assert.Equal(2, r1.Exit);
        Assert.Contains("thiếu tham số bắt buộc", r1.Err);

        var r2 = await RunCli("run", "--provider", "--workdir", _root, "--prompt-file", "p.md", "--logdir", _root);
        Assert.Equal(2, r2.Exit);
    }

    [Fact]
    public async Task Run_unknown_provider_writes_out_json_and_returns_2()
    {
        File.WriteAllText(Path.Combine(_root, "team.json"), """{"providers":[],"routing":{}}""");
        var outFile = Path.Combine(_root, "out", "run.json");
        var r = await RunCli("run", "--root", _root, "--provider", "ghost", "--workdir", _root, "--prompt-file", "p.md", "--logdir", _root, "--out", outFile);
        Assert.Equal(2, r.Exit);
        Assert.Contains("LỖI: không có provider ghost", r.Out);
        var j = JsonNode.Parse(File.ReadAllText(outFile))!;
        Assert.Equal((2, "Config"), ((int)j["exitCode"]!, (string?)j["status"]));
    }
}