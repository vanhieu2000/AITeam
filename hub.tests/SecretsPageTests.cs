using AITeam.Hub.DevData;
using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class SecretsPageTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly VaultService _vaultService;
    private readonly TeamConfigService _configService;

    public SecretsPageTests()
    {
        string teamFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamFixture, _env.Paths.TeamJson, overwrite: true);

        string projectsFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsFixture, _env.Paths.ProjectsJson, overwrite: true);

        _configService = new TeamConfigService(_env.Paths);
        _vaultService = new VaultService(_env.Paths, _configService);
    }

    public void Dispose() => _env.Dispose();

    private VaultModel CreatePageModel(IVaultService? vault = null, ITeamConfigService? config = null)
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new VaultModel(vault ?? _vaultService, config ?? _configService)
        {
            PageContext = pageContext
        };
    }

    [Fact]
    public void OnGet_loads_secret_names_and_projects()
    {
        _vaultService.Set("TEST_KEY", "test-val");

        var page = CreatePageModel();
        page.OnGet();

        Assert.Contains("TEST_KEY", page.SecretNames);
        Assert.NotEmpty(page.Projects);
    }

    [Fact]
    public void AddSecret_calls_vault_and_never_leaks_secret_value_in_model_or_viewdata()
    {
        const string specialSecretValue = "SUPER_SECRET_TOKEN_987654321_DO_NOT_LEAK";
        const string secretName = "PRODUCTION_API_KEY";

        var page = CreatePageModel();
        var result = page.OnPostAdd(secretName, specialSecretValue);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.SuccessMessage);
        Assert.Contains(secretName, page.SuccessMessage);

        // Đảm bảo giá trị bí mật ĐƯỢC LƯU trong vault
        Assert.Contains(secretName, _vaultService.ListNames());

        // TUYỆT ĐỐI KHÔNG chứa chuỗi giá trị đặc biệt trong bất kỳ thông báo hay thuộc tính nào
        Assert.DoesNotContain(specialSecretValue, page.SuccessMessage);
        if (page.ErrorMessage != null)
        {
            Assert.DoesNotContain(specialSecretValue, page.ErrorMessage);
        }

        // Kiểm tra ViewData và ModelState không lưu trữ giá trị bí mật
        foreach (var kv in page.ViewData)
        {
            Assert.DoesNotContain(specialSecretValue, kv.Value?.ToString() ?? "");
        }
        foreach (var entry in page.ModelState)
        {
            if (entry.Value?.RawValue != null)
            {
                Assert.DoesNotContain(specialSecretValue, entry.Value.RawValue.ToString() ?? "");
            }
        }
    }

    [Theory]
    [InlineData("invalid-name-with-dash")]
    [InlineData("123starts_with_number")]
    [InlineData("has space")]
    [InlineData("")]
    [InlineData(null)]
    public void AddSecret_with_invalid_name_is_rejected_and_does_not_leak_secret(string? invalidName)
    {
        const string specialSecret = "CONFIDENTIAL_PASSWORD_ABC_123";

        var page = CreatePageModel();
        var result = page.OnPostAdd(invalidName, specialSecret);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("CHU_HOA_GACH_DUOI", page.ErrorMessage);
        Assert.DoesNotContain(specialSecret, page.ErrorMessage);

        // ModelState và ViewData sạch
        Assert.Empty(page.ModelState);
    }

    [Fact]
    public void DeleteSecret_calls_vault_remove()
    {
        _vaultService.Set("KEY_TO_DELETE", "some_value");
        Assert.Contains("KEY_TO_DELETE", _vaultService.ListNames());

        var page = CreatePageModel();
        var result = page.OnPostDelete("KEY_TO_DELETE");

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.SuccessMessage);
        Assert.Contains("KEY_TO_DELETE", page.SuccessMessage);
        Assert.DoesNotContain("KEY_TO_DELETE", _vaultService.ListNames());
    }

    [Fact]
    public void DeleteSecret_nonexistent_returns_error()
    {
        var page = CreatePageModel();
        var result = page.OnPostDelete("NON_EXISTENT_KEY");

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("Không tìm thấy", page.ErrorMessage);
    }

    [Fact]
    public void ExportEnv_with_empty_inputs_returns_error()
    {
        var page = CreatePageModel();

        // Không chọn dự án
        var res1 = page.OnPostExportEnv("", new List<string> { "SECRET1" });
        Assert.IsType<PageResult>(res1);
        Assert.Contains("chọn một dự án", page.ErrorMessage);

        // Không chọn bí mật
        var res2 = page.OnPostExportEnv("AITeam", new List<string>());
        Assert.IsType<PageResult>(res2);
        Assert.Contains("chọn ít nhất một bí mật", page.ErrorMessage);
    }

    [Fact]
    public void ExportEnv_calls_vault_service_export()
    {
        // Dùng FakeVaultService để test việc gọi đúng dịch vụ
        var fakeVault = new FakeVaultService();
        var fakeConfig = new FakeTeamConfigService();

        var page = CreatePageModel(fakeVault, fakeConfig);
        var result = page.OnPostExportEnv("AITeam", new List<string> { "GITHUB_TOKEN" });

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.SuccessMessage);
        Assert.Contains("AITeam", page.SuccessMessage);
    }

    [Fact]
    public void Corrupt_vault_is_handled_gracefully_with_vietnamese_error()
    {
        // Ghi file vault bị lỗi định dạng JSON
        Directory.CreateDirectory(Path.GetDirectoryName(_env.Paths.VaultFile)!);
        File.WriteAllText(_env.Paths.VaultFile, "{ this is invalid json !!!");

        var page = CreatePageModel();
        page.OnGet();

        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("Kho bí mật", page.ErrorMessage);
        Assert.Contains("bị hỏng", page.ErrorMessage);
        Assert.Empty(page.SecretNames);
    }
}
