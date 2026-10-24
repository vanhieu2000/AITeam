using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Hub.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AITeam.Hub.Tests;

public sealed class ProvidersPageTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 24, 10, 0, 0);
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private ProvidersModel Page() => new(_env.Paths)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
        Now = () => Now,
    };

    [Fact]
    public void Broken_config_still_loads_history_and_clear_sets_error_message()
    {
        var store = new ProviderStateStore(_env.AITeamRoot);
        store.Record(new AgentRunResult("codex", RunStatus.NotLoggedIn, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "login required"), Now.AddMinutes(-10));
        File.WriteAllText(_env.Paths.TeamJson, "{ broken json");

        var p = Page();
        p.OnGet();
        Assert.StartsWith("Không đọc được cấu hình provider: ", p.Error);
        Assert.Single(p.History);

        Assert.IsType<RedirectToPageResult>(p.OnPostClear("codex"));
        Assert.StartsWith("Không đọc được cấu hình provider: ", p.ErrorMessage);
    }
}
