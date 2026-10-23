using AITeam.Core.Review;

namespace AITeam.Core.Tests;

public sealed class ReviewServiceEdgeTests
{
    [Fact]
    public void ParseOutput_handles_optional_finding_and_blocked_verdict()
    {
        var opt = ReviewService.ParseOutput("""{"verdict":"PASS","findings":[{"severity":"optional","file":"a.cs","line":10,"evidence":"nhỏ","fix":"gợi ý"}],"unverified":[]}""");
        Assert.NotNull(opt);
        Assert.Equal(ReviewVerdict.Pass, opt!.Value.Verdict);
        Assert.Equal(FindingSeverity.Optional, Assert.Single(opt.Value.Findings).Severity);

        var blocked = ReviewService.ParseOutput("""{"verdict":"BLOCKED","findings":[],"unverified":["Thiếu môi trường"]}""");
        Assert.NotNull(blocked);
        Assert.Equal(ReviewVerdict.Blocked, blocked!.Value.Verdict);
        Assert.Equal(["Thiếu môi trường"], blocked.Value.Unverified);
    }

    [Fact]
    public async Task Schema_and_prompt_files_are_written_without_bom()
    {
        // Codex từ chối lược đồ JSON có BOM (chạy thật 23/10) — file phải là UTF-8 không BOM.
        var log = Directory.CreateTempSubdirectory("aiteam-bom-").FullName;
        try
        {
            var svc = new ReviewService();
            var pack = svc.BuildPack(new ReviewPackInput("# T-1 · a\n## Tóm tắt\nx", "diff", null, null));
            var reviewer = new SchemaSpy();
            await svc.ReviewAsync(pack, reviewer, "agy", log, log, TimeSpan.FromMinutes(1), CancellationToken.None);
            foreach (var file in new[] { reviewer.SchemaFile!, reviewer.PromptFile! })
            {
                var bytes = File.ReadAllBytes(file);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, $"{file} có BOM");
            }
            System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(reviewer.SchemaFile!)); // đọc được như byte thô, không ném lỗi
        }
        finally { Directory.Delete(log, true); }
    }

    private sealed class SchemaSpy : AITeam.Core.Providers.IAgentProvider
    {
        public string? SchemaFile, PromptFile;
        public string Id => "codex";
        public AITeam.Core.Providers.ProviderKind Kind => AITeam.Core.Providers.ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public Task<AITeam.Core.Providers.ProviderHealth> CheckAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<AITeam.Core.Providers.AgentRunResult> RunAsync(AITeam.Core.Providers.AgentRunRequest r, CancellationToken ct)
        {
            SchemaFile = r.OutputSchemaFile; PromptFile = r.PromptFile;
            return Task.FromResult(new AITeam.Core.Providers.AgentRunResult("codex", AITeam.Core.Providers.RunStatus.Succeeded,
                """{"verdict":"PASS","findings":[],"unverified":[]}""", AITeam.Core.Providers.TokenUsage.Unknown, TimeSpan.Zero, null, null, ""));
        }
    }
}