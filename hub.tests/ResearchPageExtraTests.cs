using AITeam.Core.Accounts;
using AITeam.Core.Research;
using AITeam.Hub.DevData;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class ResearchPageExtraTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    public void Dispose() => _env.Dispose();

    private sealed class EmptyRequests : IRequestService
    {
        public IReadOnlyList<RequestThread> GetAll() => Array.Empty<RequestThread>();
        public RequestThread? Get(string id) => null;
    }

    private sealed class FailingRequests : IRequestService
    {
        public IReadOnlyList<RequestThread> GetAll() => throw new IOException("lỗi đĩa");
        public RequestThread? Get(string id) => throw new IOException("lỗi đĩa");
    }

    private sealed class SimpleAccounts : IAiAccountsService
    {
        public IReadOnlyList<AiAccountView> List() => AiCatalog.All
            .Select(s => new AiAccountView(s, "", AiMode.Manual, AiStatus.ManualOnly, "Thủ công", "", null))
            .ToList();
        public Task<AiAccountView> CheckAsync(string? id, CancellationToken ct) => throw new NotSupportedException();
        public AiAccountView Save(string? id, string? alias, string? mode) => throw new NotSupportedException();
    }

    [Fact]
    public void Page_handles_empty_requests_and_read_errors()
    {
        var svc = new ResearchService(_env.Paths, new EmptyRequests(), new SimpleAccounts(), _ => null);
        var p = new ResearchModel(svc, new EmptyRequests());
        p.OnGet(null);
        Assert.Null(p.Current);
        Assert.Empty(p.Requests);
        Assert.Empty(p.Jobs);
        Assert.Empty(p.Entries);
        Assert.Null(p.Error);

        var bad = new ResearchModel(svc, new FailingRequests());
        bad.OnGet("R-005");
        Assert.Equal("Không đọc được dữ liệu phòng nghiên cứu: lỗi đĩa", bad.Error);
    }

    [Fact]
    public void Default_connectors_returns_null_for_unknown_ai()
    {
        var fn = ResearchService.DefaultConnectors(_env.Paths);
        Assert.Null(fn("deepseek"));
        Assert.Null(fn("grok"));
    }
}
