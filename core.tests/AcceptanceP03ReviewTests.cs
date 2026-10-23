using AITeam.Core.Providers;
using AITeam.Core.Review;

namespace AITeam.Core.Tests;

/// <summary>Test nghiệm thu P0-3 (Claude viết trước, agent KHÔNG sửa). Thiết kế: docs/design/V4-MULTI-AI.md mục B5.</summary>
public sealed class AcceptanceP03ReviewTests : IDisposable
{
    private readonly string _log = Directory.CreateTempSubdirectory("aiteam-review-").FullName;
    public void Dispose() => Directory.Delete(_log, true);

    private sealed class FakeProvider(string id, AgentRunResult result, bool readOnly = true) : IAgentProvider
    {
        public int Calls;
        public AgentRunRequest? LastRequest;
        public string? LastPrompt;
        public string Id => id;
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => readOnly;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(id, true, true, ""));
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
        {
            Calls++;
            LastRequest = request;
            LastPrompt = File.ReadAllText(request.PromptFile);
            return Task.FromResult(result with { ProviderId = id });
        }
    }

    private static AgentRunResult Answer(string text, RunStatus status = RunStatus.Succeeded) =>
        new("x", status, text, new TokenUsage(1000, 600, 50), TimeSpan.FromSeconds(30), "gpt-6.1-sol", "t1", status == RunStatus.Succeeded ? "" : "lỗi");

    private const string TaskText = """
        # T-100 · Demo
        - **Agent:** A
        ## Tóm tắt
        - Sửa hàm cộng.
        ## Mục tiêu
        Hàm Add trả đúng tổng.
        ## Phạm vi (được sửa)
        - `src/Calc.cs`
        ## Tiêu chí hoàn thành
        - [ ] Add(2,3) == 5
        ## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
        - `tests/AcceptanceT100.cs`
        ## Báo cáo của agent
        Đã sửa Add.
        ## Review (Claude)
        ### Vòng 1 — CẦN SỬA
        KẾT-LUẬN-CŨ-KHÔNG-ĐƯỢC-ĐƯA-VÀO
        """;

    private static ReviewPack Pack(string diff = "diff --git a/src/Calc.cs b/src/Calc.cs\n+return a + b;") =>
        new ReviewService().BuildPack(new ReviewPackInput(TaskText, diff, "Cổng kiểm tra: ĐẠT · 12/12 test", "Thiết kế mục 3: Add trả tổng"));

    private Task<ReviewResult> Run(IAgentProvider reviewer, string developer = "agy") =>
        new ReviewService().ReviewAsync(Pack(), reviewer, developer, _log, _log, TimeSpan.FromMinutes(5), CancellationToken.None);

    [Fact]
    public void Pack_contains_task_criteria_report_verify_design_and_diff_but_not_old_review()
    {
        var p = Pack();
        foreach (var must in new[] { "Sửa hàm cộng", "Add(2,3) == 5", "tests/AcceptanceT100.cs", "Đã sửa Add", "12/12 test", "Thiết kế mục 3", "+return a + b;", "src/Calc.cs" })
            Assert.Contains(must, p.Prompt);
        Assert.DoesNotContain("KẾT-LUẬN-CŨ-KHÔNG-ĐƯỢC-ĐƯA-VÀO", p.Prompt);
        Assert.False(p.DiffTruncated);
    }

    [Fact]
    public void Diff_hash_is_sha256_of_full_diff_and_changes_with_diff()
    {
        var a = Pack("diff A");
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("diff A"))).ToLowerInvariant(), a.DiffHash);
        Assert.NotEqual(a.DiffHash, Pack("diff B").DiffHash);
    }

    [Fact]
    public void Huge_diff_is_truncated_with_marker_but_hash_uses_full_diff()
    {
        var big = new string('x', 5000);
        var p = new ReviewService().BuildPack(new ReviewPackInput(TaskText, big, null, null, MaxDiffChars: 1000));
        Assert.True(p.DiffTruncated);
        Assert.Contains("ĐÃ CẮT", p.Prompt);
        Assert.True(p.Prompt.Length < 5000);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(big))).ToLowerInvariant(), p.DiffHash);
    }

    [Fact]
    public async Task Pass_is_parsed_and_saved_with_hash_and_tokens()
    {
        var fake = new FakeProvider("codex", Answer("""{"verdict":"PASS","findings":[],"unverified":["Chưa chạy app"]}"""));
        var r = await Run(fake);
        Assert.Equal(ReviewVerdict.Pass, r.Verdict);
        Assert.Equal("codex", r.ProviderId);
        Assert.Equal(Pack().DiffHash, r.DiffHash);
        Assert.Equal(1000, r.Tokens.Input);
        Assert.Equal(["Chưa chạy app"], r.Unverified);
        Assert.Equal(RunMode.ReadOnly, fake.LastRequest!.Mode);
        Assert.NotNull(fake.LastRequest.OutputSchemaFile);
        Assert.Contains("PASS", File.ReadAllText(fake.LastRequest.OutputSchemaFile!));
        var saved = Directory.GetFiles(_log, "review-*.json");
        Assert.Single(saved);
        Assert.Contains(r.DiffHash, File.ReadAllText(saved[0]));
    }

    [Fact]
    public async Task Changes_requested_findings_are_parsed()
    {
        var fake = new FakeProvider("codex", Answer("""
            ```json
            {"verdict":"changes_requested","findings":[{"severity":"REQUIRED","file":"src/Calc.cs","line":7,"evidence":"Add(2,3) trả 6","fix":"dùng a + b"}],"unverified":[]}
            ```
            """));
        var r = await Run(fake);
        Assert.Equal(ReviewVerdict.ChangesRequested, r.Verdict);
        var f = Assert.Single(r.Findings);
        Assert.Equal(FindingSeverity.Required, f.Severity);
        Assert.Equal("src/Calc.cs", f.File);
        Assert.Equal(7, f.Line);
    }

    [Fact]
    public async Task Pass_with_blocker_finding_becomes_changes_requested()
    {
        var fake = new FakeProvider("codex", Answer("""{"verdict":"PASS","findings":[{"severity":"blocker","file":"a","line":1,"evidence":"lộ mật khẩu","fix":"bỏ"}],"unverified":[]}"""));
        Assert.Equal(ReviewVerdict.ChangesRequested, (await Run(fake)).Verdict);
    }

    [Theory]
    [InlineData(RunStatus.TimedOut)]
    [InlineData(RunStatus.QuotaExhausted)]
    [InlineData(RunStatus.NotLoggedIn)]
    [InlineData(RunStatus.ConfigError)]
    [InlineData(RunStatus.Unavailable)]
    [InlineData(RunStatus.Failed)]
    public async Task Provider_failure_is_blocked_never_pass(RunStatus status)
    {
        // Kể cả khi provider lỡ trả văn bản PASS hợp lệ kèm trạng thái lỗi.
        var fake = new FakeProvider("codex", Answer("""{"verdict":"PASS","findings":[],"unverified":[]}""", status));
        var r = await Run(fake);
        Assert.Equal(ReviewVerdict.Blocked, r.Verdict);
        Assert.Contains(status.ToString(), r.BlockedReason);
    }

    [Theory]
    [InlineData("không phải json")]
    [InlineData("""{"findings":[],"unverified":[]}""")]
    [InlineData("""{"verdict":"LGTM","findings":[],"unverified":[]}""")]
    [InlineData("""{"verdict":"PASS","findings":[{"severity":"nguy-hiem","file":"a","line":1,"evidence":"e","fix":"f"}],"unverified":[]}""")]
    [InlineData("")]
    public async Task Unreadable_output_is_blocked(string text)
    {
        var r = await Run(new FakeProvider("codex", Answer(text)));
        Assert.Equal(ReviewVerdict.Blocked, r.Verdict);
        Assert.False(string.IsNullOrWhiteSpace(r.BlockedReason));
    }

    [Fact]
    public async Task Self_review_is_blocked_without_calling_provider()
    {
        var fake = new FakeProvider("AGY", Answer("""{"verdict":"PASS","findings":[],"unverified":[]}"""));
        var r = await Run(fake, developer: "agy");
        Assert.Equal(ReviewVerdict.Blocked, r.Verdict);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task Reviewer_without_read_only_sandbox_is_blocked_without_calling_provider()
    {
        var fake = new FakeProvider("claude-cli", Answer("""{"verdict":"PASS","findings":[],"unverified":[]}"""), readOnly: false);
        var r = await Run(fake);
        Assert.Equal(ReviewVerdict.Blocked, r.Verdict);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public void ParseOutput_accepts_real_codex_answer_from_23_10()
    {
        var real = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "codex-review-last.json"));
        var parsed = ReviewService.ParseOutput(real);
        Assert.NotNull(parsed);
        Assert.Equal(ReviewVerdict.ChangesRequested, parsed!.Value.Verdict);
        Assert.Equal(2, parsed.Value.Findings.Count);
    }
}
