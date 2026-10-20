using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class VaultServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private static readonly byte[] VaultEntropy = Encoding.UTF8.GetBytes("AITeam.vault.v1");

    public void Dispose() => _env.Dispose();

    [Fact]
    public void Set_ListNames_ManualDecrypt_and_Remove_work_correctly()
    {
        var service = new VaultService(_env.Paths);

        // Ban đầu trống
        Assert.Empty(service.ListNames());

        // Set 2 bí mật
        service.Set("DB_PASSWORD", "SecretPass123!@#");
        service.Set("API_KEY", "key-xyz-789");

        var names = service.ListNames();
        Assert.Equal(2, names.Count);
        Assert.Contains("DB_PASSWORD", names);
        Assert.Contains("API_KEY", names);

        // Kiểm tra giải mã bằng công thức của team.ps1
        Assert.True(File.Exists(_env.Paths.VaultFile));
        string vaultJson = File.ReadAllText(_env.Paths.VaultFile);
        var obj = JsonNode.Parse(vaultJson) as JsonObject;
        Assert.NotNull(obj);

        string dbPassCipher = obj["DB_PASSWORD"]?.ToString() ?? "";
        Assert.NotEmpty(dbPassCipher);
        byte[] dbPassBytes = ProtectedData.Unprotect(Convert.FromBase64String(dbPassCipher), VaultEntropy, DataProtectionScope.CurrentUser);
        string dbPassPlain = Encoding.UTF8.GetString(dbPassBytes);
        Assert.Equal("SecretPass123!@#", dbPassPlain);

        string apiKeyCipher = obj["API_KEY"]?.ToString() ?? "";
        byte[] apiKeyBytes = ProtectedData.Unprotect(Convert.FromBase64String(apiKeyCipher), VaultEntropy, DataProtectionScope.CurrentUser);
        string apiKeyPlain = Encoding.UTF8.GetString(apiKeyBytes);
        Assert.Equal("key-xyz-789", apiKeyPlain);

        // Xóa bí mật
        Assert.True(service.Remove("DB_PASSWORD"));
        Assert.False(service.Remove("DB_PASSWORD")); // Lần 2 trả false vì không còn
        var remaining = service.ListNames();
        Assert.Single(remaining);
        Assert.Contains("API_KEY", remaining);
    }

    [Theory]
    [InlineData("123BAD")]
    [InlineData("DB-PASS")]
    [InlineData("DB PASS")]
    [InlineData("DB.PASS")]
    [InlineData("")]
    public void Set_rejects_invalid_names(string invalidName)
    {
        var service = new VaultService(_env.Paths);
        Assert.Throws<ArgumentException>(() => service.Set(invalidName, "value"));
    }

    [Fact]
    public void ExportEnv_rejects_when_not_git_ignored_and_succeeds_when_ignored()
    {
        // Tạo git repo tạm thời
        string testRepo = Path.Combine(_env.RootDir, "TestRepo");
        Directory.CreateDirectory(testRepo);

        // Chạy git init
        var initProc = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{testRepo}\" init",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        initProc?.WaitForExit(5000);

        // Đăng ký repo tạm là một dự án thật trong projects.json (ExportEnv chỉ nhận dự án đã biết — review H-001).
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json"), _env.Paths.TeamJson, overwrite: true);
        File.WriteAllText(_env.Paths.ProjectsJson,
            "[ { \"name\": \"TestRepo\", \"repo\": \"x/TestRepo\", \"path\": " + System.Text.Json.JsonSerializer.Serialize(testRepo) + " } ]");
        var service = new VaultService(_env.Paths, new TeamConfigService(_env.Paths));
        service.Set("TEST_SECRET", "super_secret_value");

        // 1. Khi chưa có .gitignore hoặc chưa ignore .env.local -> ném ngoại lệ
        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.ExportEnv("TestRepo", new[] { "TEST_SECRET" }));
        Assert.Contains(".env.local CHƯA bị .gitignore", ex.Message);

        // 2. Thêm .env.* vào .gitignore
        File.WriteAllText(Path.Combine(testRepo, ".gitignore"), ".env.*\r\n");

        // Gọi lại ExportEnv
        service.ExportEnv("TestRepo", new[] { "TEST_SECRET" });

        string envLocalPath = Path.Combine(testRepo, ".env.local");
        Assert.True(File.Exists(envLocalPath));
        string content = File.ReadAllText(envLocalPath);
        Assert.Equal("TEST_SECRET=super_secret_value\r\n", content);
    }
}
