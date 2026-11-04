using System.Text;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class F101ProjectAdminExtraTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private sealed class Runner : IProjectScriptRunner
    {
        public (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout)
            => throw new InvalidOperationException("Không được gọi khi cấu hình hỏng.");
    }
    public void Dispose() => _env.Dispose();

    [Fact]
    public void Update_corrupt_local_preserves_projects_and_backup()
    {
        File.WriteAllText(_env.Paths.ProjectsJson, "[{\"name\":\"Alpha\"}]");
        File.WriteAllText(_env.Paths.TeamLocalJson, "{");
        var before = File.ReadAllBytes(_env.Paths.ProjectsJson);
        var service = new ProjectAdminService(_env.Paths, new TeamConfigService(_env.Paths), new Runner());
        var result = service.Update("Alpha", new("", "main", "", "", "", "", "",
            new Dictionary<string, string>(), _env.RootDir));
        Assert.False(result.Ok);
        Assert.Equal(before, File.ReadAllBytes(_env.Paths.ProjectsJson));
        Assert.False(File.Exists(_env.Paths.ProjectsJson + ".bak"));
    }

    [Fact]
    public void SetStatus_keeps_bom_and_original_backup_and_rejects_unknown_enum()
    {
        File.WriteAllText(_env.Paths.ProjectsJson, "[{\"name\":\"Alpha\"}]", new UTF8Encoding(true));
        var before = File.ReadAllBytes(_env.Paths.ProjectsJson);
        var service = new ProjectAdminService(_env.Paths, new TeamConfigService(_env.Paths), new Runner());
        Assert.False(service.SetStatus("Alpha", (ProjectStatus)100).Ok);
        Assert.Equal(before, File.ReadAllBytes(_env.Paths.ProjectsJson));
        Assert.True(service.SetStatus("Alpha", ProjectStatus.Paused).Ok);
        Assert.Equal(before, File.ReadAllBytes(_env.Paths.ProjectsJson + ".bak"));
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(_env.Paths.ProjectsJson).Take(3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Real_runner_closes_stdin_drains_output_and_times_out(bool hang)
    {
        string script = Path.Combine(Path.GetDirectoryName(_env.Paths.ProjectsJson)!, "team.ps1");
        File.WriteAllText(script, """
            [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
            $inputText = [Console]::In.ReadToEnd()
            [Console]::Out.WriteLine('Đầu ra:' + $args[0] + ':' + $inputText.Length)
            [Console]::Error.WriteLine('Lỗi thử nghiệm')
            """ + (hang ? "\nStart-Sleep -Seconds 30" : "\nexit 7"), new UTF8Encoding(true));
        var result = new PowerShellProjectScriptRunner(_env.Paths).Run(["D:\\Du An\\Alpha"],
            TimeSpan.FromSeconds(hang ? 4 : 15));
        Assert.Equal(hang ? 124 : 7, result.ExitCode);
        Assert.Contains("Đầu ra:D:\\Du An\\Alpha:0", result.Output);
        Assert.Contains("Lỗi thử nghiệm", result.Output);
    }
}
