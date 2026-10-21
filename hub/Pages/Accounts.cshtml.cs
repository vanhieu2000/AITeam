using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Pages;

public sealed class AccountsModel : PageModel
{
    private readonly IHealthService _healthService;
    private readonly ITeamConfigService _configService;
    private readonly IAgentRunService? _runService;

    [ActivatorUtilitiesConstructor]
    public AccountsModel(IHealthService healthService, ITeamConfigService configService, IAgentRunService? runService = null)
    {
        _healthService = healthService;
        _configService = configService;
        _runService = runService;
    }

    public HealthItem? GitHubHealth { get; private set; }
    public HealthItem? AgyHealth { get; private set; }
    public HealthItem? ClaudeHealth { get; private set; }

    public string GitHubOwner { get; private set; } = "";
    public IReadOnlyList<string> AgyAccounts { get; private set; } = Array.Empty<string>();
    public AntigravityStatus AgyStatus { get; private set; } = new(false, null, null, null);

    [TempData]
    public string? Message { get; set; }

    public string? ErrorMessage { get; set; }

    public HealthItem? AgyLoginCheckResult { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public async Task<IActionResult> OnPostCheckAgyLoginAsync(CancellationToken ct)
    {
        try
        {
            var item = await _healthService.CheckAgyLoginAsync(ct);
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return CreatePartialResult(item);
            }
            AgyLoginCheckResult = item;
        }
        catch (Exception ex)
        {
            var errorItem = new HealthItem("Antigravity Login", HealthLevel.Error, "Lỗi kiểm tra: " + ex.Message, null);
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return CreatePartialResult(errorItem);
            }
            AgyLoginCheckResult = errorItem;
        }

        LoadData();
        return Page();
    }

    private PartialViewResult CreatePartialResult(HealthItem item)
    {
        var viewData = new ViewDataDictionary<HealthItem>(new EmptyModelMetadataProvider(), ModelState)
        {
            Model = item
        };
        return new PartialViewResult
        {
            ViewName = "_AgyLoginResult",
            ViewData = viewData
        };
    }

    public IActionResult OnPostSwitchAccount()
    {
        try
        {
            _healthService.OpenSwitchAccountWindow();
            Message = "Đã mở cửa sổ Antigravity. Hướng dẫn: gõ /logout → đăng nhập tài khoản khác → gõ /quit để hoàn tất.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi mở cửa sổ đổi tài khoản: " + ex.Message;
        }

        LoadData();
        return Page();
    }

    public IActionResult OnPostInstallClaude()
    {
        try
        {
            _healthService.InstallClaudeIntegration();
            Message = "Đã cài đặt skill aiteam và cập nhật cấu hình CLAUDE.md thành công.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi cài đặt Claude integration: " + ex.Message;
        }

        LoadData();
        return Page();
    }

    private void LoadData()
    {
        var items = _healthService.Check();
        GitHubHealth = items.FirstOrDefault(i => i.Name.StartsWith("GitHub", StringComparison.OrdinalIgnoreCase));
        AgyHealth = items.FirstOrDefault(i => i.Name.StartsWith("Antigravity", StringComparison.OrdinalIgnoreCase));
        ClaudeHealth = items.FirstOrDefault(i => i.Name.StartsWith("Claude", StringComparison.OrdinalIgnoreCase));

        var settings = _configService.GetSettings();
        GitHubOwner = settings.GitHubOwner;
        AgyAccounts = settings.AgyAccounts;

        if (_runService != null)
        {
            var runs = _runService.GetRecentRuns(50);
            AgyStatus = AntigravityStatuses.From(runs);
        }
        else
        {
            AgyStatus = new AntigravityStatus(false, null, null, null);
        }
    }
}
