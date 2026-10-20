using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>Test bổ sung khi review H-001 (Claude): an toàn dữ liệu kho bí mật và chống chèn lệnh / ghi tùy ý.</summary>
public sealed class ReviewH001SecurityTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void Corrupted_vault_is_never_overwritten()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_env.Paths.VaultFile)!);
        const string broken = "{ \"DB_PASSWORD\": \"AQAAANCMnd8B…\", hỏng";
        File.WriteAllText(_env.Paths.VaultFile, broken);
        var vault = new VaultService(_env.Paths);

        Assert.Throws<InvalidOperationException>(() => vault.ListNames());
        Assert.Throws<InvalidOperationException>(() => vault.Set("NEW_SECRET", "x"));
        Assert.Equal(broken, File.ReadAllText(_env.Paths.VaultFile)); // dữ liệu cũ còn nguyên để cứu
    }

    [Fact]
    public void Export_env_refuses_unknown_project_or_raw_path()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json"), _env.Paths.TeamJson, overwrite: true);
        File.WriteAllText(_env.Paths.ProjectsJson, "[]");
        var vault = new VaultService(_env.Paths, new TeamConfigService(_env.Paths));
        vault.Set("TEST_SECRET", "x");

        string outside = Path.Combine(_env.RootDir, "outside");
        Directory.CreateDirectory(outside);
        Assert.Throws<InvalidOperationException>(() => vault.ExportEnv(outside, ["TEST_SECRET"]));
        Assert.Throws<InvalidOperationException>(() => new VaultService(_env.Paths).ExportEnv("bat-ky", ["TEST_SECRET"]));
        Assert.False(File.Exists(Path.Combine(outside, ".env.local")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("agy.exe")]                                   // không phải đường dẫn đầy đủ
    [InlineData(@"C:\Windows\System32\cmd.exe")]              // sai tên file
    [InlineData(@"C:\x\agy.exe'; Remove-Item C:\ -Recurse; '")] // thử chèn lệnh
    [InlineData(@"C:\khong-co\agy.exe")]                      // không tồn tại
    public void Switch_account_only_accepts_a_real_agy_exe(string? exe) =>
        Assert.False(HealthService.IsValidAgyExe(exe));

    [Fact]
    public void Real_agy_exe_path_is_accepted()
    {
        string dir = Path.Combine(_env.RootDir, "Tools", "agy", "bin");
        Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "agy.exe");
        File.WriteAllText(exe, "");
        Assert.True(HealthService.IsValidAgyExe(exe));
    }
}
