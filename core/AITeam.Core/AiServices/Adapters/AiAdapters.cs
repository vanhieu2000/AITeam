using AITeam.Core.Process;
using AITeam.Core.Providers;

namespace AITeam.Core.AiServices.Adapters;

// V10-02 (R-014): chạy AI theo danh bạ. KHUNG do Claude viết — giữ nguyên chữ ký, agent điền phần thân (TODO(agent)).
// Thiết kế: tasks/V10-02-adapter-ai-tu-danh-ba.md, docs/design/R-014-van-phong-ai.md mục 4.6, 5, 8.

/// <summary>Tra giá trị khóa trong két theo TÊN. Core không biết két nằm đâu; Hub cắm vào (VaultSecretLookup).</summary>
public interface ISecretLookup
{
    string? Get(string name);
}

public interface IAiProviderFactory
{
    /// <summary>
    /// Id "codex" ⇒ CodexProvider; "opencode" ⇒ OpenCodeProvider; "agy" hoặc "antigravity" ⇒ AgyProvider (lớp riêng có sẵn).
    /// Còn lại theo Adapter: CliJson ⇒ CliJsonProvider, CliText ⇒ CliTextProvider, OpenAiCompatibleHttp ⇒ OpenAiHttpProvider.
    /// Dịch vụ tắt ⇒ InvalidOperationException("Dịch vụ AI &lt;id&gt; đang tắt.").
    /// </summary>
    IAgentProvider Create(AiService service);

    /// <summary>registry.List(includeDisabled: false) lọc theo vai trò, giữ thứ tự registry.</summary>
    IReadOnlyList<AiService> ForRole(AiRole role);
}

public sealed class AiProviderFactory(IAiServiceRegistry registry, IProcessRunner runner, ISecretLookup secrets, HttpMessageHandler? http = null) : IAiProviderFactory
{
    private readonly IAiServiceRegistry _registry = registry;
    private readonly IProcessRunner _runner = runner;
    private readonly ISecretLookup _secrets = secrets;
    private readonly HttpMessageHandler? _http = http;

    public IAgentProvider Create(AiService service) => throw new NotImplementedException("TODO(agent) V10-02");

    public IReadOnlyList<AiService> ForRole(AiRole role) => throw new NotImplementedException("TODO(agent) V10-02");
}

/// <summary>CLI in chữ thường ra stdout. Args thay chỗ giữ {prompt_file} {workdir} {model} TRONG từng phần tử, không qua shell.</summary>
public sealed class CliTextProvider(AiService service, IProcessRunner runner) : IAgentProvider
{
    private readonly IProcessRunner _runner = runner;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => false;
    public Task<ProviderHealth> CheckAsync(CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
    public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
}

/// <summary>CLI in JSON lines: Text = nối "text" của dòng type=text (hoặc part.text); token từ tokens.input / tokens.output.</summary>
public sealed class CliJsonProvider(AiService service, IProcessRunner runner) : IAgentProvider
{
    private readonly IProcessRunner _runner = runner;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => false;
    public Task<ProviderHealth> CheckAsync(CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
    public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
}

/// <summary>POST {BaseUrl}/chat/completions; chỉ RunMode.ReadOnly. Giá trị khóa không bao giờ ra Text / Detail / ngoại lệ.</summary>
public sealed class OpenAiHttpProvider(AiService service, ISecretLookup secrets, HttpMessageHandler? http = null) : IAgentProvider
{
    private readonly ISecretLookup _secrets = secrets;
    private readonly HttpMessageHandler? _http = http;
    public string Id => service.Id;
    public ProviderKind Kind => ProviderKind.Generic;
    public bool HasReadOnlySandbox => true;
    public Task<ProviderHealth> CheckAsync(CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
    public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct) => throw new NotImplementedException("TODO(agent) V10-02");
}
