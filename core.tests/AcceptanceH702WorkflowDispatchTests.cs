using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AITeam.Core.Review;
using AITeam.Core.Providers;
using AITeam.Core.Routing;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceH702WorkflowDispatchTests
{
    // Bản sao nguyên byte team.json của worktree lúc chuẩn bị; không đọc cấu hình máy thật.
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "H702", "team.json");
    private static readonly string Script = Path.Combine(AppContext.BaseDirectory, "S1-06Runtime", "team.ps1");
    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    private static string CopyConfig(TempRepo tmp)
    {
        var path = Path.Combine(tmp.Root, "team.json");
        File.Copy(Fixture, path);
        return path;
    }
    private static string CopyTask(TempRepo tmp)
    {
        const string name = "H7-02-dieu-phoi-plan-prep-qc-theo-map.md";
        var path = Path.Combine(tmp.Root, "tasks", name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "H702", name), path);
        return path;
    }    private static JsonObject Config() => JsonNode.Parse(File.ReadAllText(Fixture))!.AsObject();
    private static JsonArray Roles() => new(
        new[] { "receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish" }
        .Select(code => (JsonNode)new JsonObject { ["code"] = code,
            ["actor"] = code == "code" ? "minh" : code == "gate" ? "gate" :
                new[] { "research", "plan", "prep", "qc" }.Contains(code) ? "codex" : "claude", ["note"] = "" }).ToArray());
    private static string WriteMapped(TempRepo tmp, string role, string actor)
    {
        var root = Config(); var roles = Roles();
        roles.Single(r => (string)r!["code"]! == role)!["actor"] = actor;
        root["workflow"] = new JsonObject { ["roles"] = roles };
        return tmp.Write("team.json", root.ToJsonString());
    }
    private static string LoadFunctions(params string[] names) => $$"""
        $tokens=$null; $errors=$null
        $ast=[Management.Automation.Language.Parser]::ParseFile({{Quote(Script)}},[ref]$tokens,[ref]$errors)
        if ($errors.Count) { throw 'Script không phân tích được.' }
        $wanted=@({{string.Join(",", names.Select(Quote))}})
        $defs=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in $wanted},$true))
        if ($defs.Count -ne $wanted.Count) { throw 'Thiếu hàm điều phối.' }
        foreach ($def in $defs) { . ([scriptblock]::Create($def.Extent.Text)) }
        """;

    [Theory]
    [InlineData("plan", "codex", true)]
    [InlineData("prep", "codex", false)]
    [InlineData("qc", null, true)]
    public void Legacy_config_preserves_bytes_and_old_routing(string role, string? provider, bool readOnly)
    {
        using var tmp = new TempRepo(false); var path = CopyConfig(tmp); var before = File.ReadAllBytes(path);
        var result = WorkflowRoleDispatch.Resolve(path, role);
        Assert.Equal(provider, result.ProviderId); Assert.False(result.IsExplicit); Assert.False(result.Manual);
        Assert.Equal(readOnly, result.ReadOnly); Assert.Equal(before, File.ReadAllBytes(path));
    }

    public static IEnumerable<object[]> Actors() => from role in new[] { "plan", "prep", "qc" }
        from actor in new[] { "codex", "minh", "lan", "claude", "gate", "owner" } select new object[] { role, actor };
    [Theory, MemberData(nameof(Actors))]
    public void Each_role_maps_identity_without_guessing_provider(string role, string actor)
    {
        using var tmp = new TempRepo(false); var path = WriteMapped(tmp, role, actor); var before = File.ReadAllBytes(path);
        var result = WorkflowRoleDispatch.Resolve(path, role);
        Assert.Equal(role, result.Role); Assert.Equal(actor, result.Actor); Assert.True(result.IsExplicit);
        Assert.Equal(role != "prep", result.ReadOnly);
        Assert.Equal(actor is "claude" or "gate" or "owner", result.Manual);
        Assert.Equal(actor == "codex" ? "codex" : actor is "minh" or "lan" ? "agy" : null, result.ProviderId);
        Assert.Equal(before, File.ReadAllBytes(path));
        foreach (var other in new[] { "plan", "prep", "qc" }.Where(r => r != role))
            Assert.Equal("codex", WorkflowRoleDispatch.Resolve(path, other).Actor);
    }

    [Theory]
    [InlineData("missing")][InlineData("workflow-missing")][InlineData("roles-missing")]
    public void Missing_sections_use_legacy(string variant)
    {
        using var tmp = new TempRepo(false); var path = Path.Combine(tmp.Root, "team.json");
        if (variant != "missing")
        {
            var root = Config();
            if (variant == "roles-missing") root["workflow"] = new JsonObject { ["unrelated"] = 42 };
            tmp.Write("team.json", root.ToJsonString());
        }
        Assert.False(WorkflowRoleDispatch.Resolve(path, "qc").IsExplicit);
        Assert.Null(WorkflowRoleDispatch.Resolve(path, "qc").ProviderId);
    }

    [Theory]
    [InlineData("null-workflow")][InlineData("null-roles")][InlineData("object-roles")]
    [InlineData("empty")][InlineData("duplicate")][InlineData("missing-code")][InlineData("bad-code")]
    [InlineData("bad-actor")][InlineData("upper-actor")][InlineData("null-note")][InlineData("long-note")]
    public void Invalid_schema_is_not_missing_and_is_never_echoed(string variant)
    {
        using var tmp = new TempRepo(false); var root = Config(); var roles = Roles();
        switch (variant)
        {
            case "empty": roles.Clear(); break;
            case "duplicate": roles[1]!["code"] = "receive"; break;
            case "missing-code": roles.RemoveAt(0); break;
            case "bad-code": roles[0]!["code"] = "run"; break;
            case "bad-actor": roles[3]!["actor"] = "codex; $(Set-Content injected yes)"; break;
            case "upper-actor": roles[3]!["actor"] = "Codex"; break;
            case "null-note": roles[3]!["note"] = null; break;
            case "long-note": roles[3]!["note"] = new string('x', 501); break;
        }
        root["workflow"] = variant == "null-workflow" ? null : new JsonObject {
            ["roles"] = variant == "null-roles" ? null : variant == "object-roles" ? new JsonObject() : roles };
        var path = tmp.Write("team.json", root.ToJsonString()); var before = File.ReadAllBytes(path);
        var ex = Assert.Throws<InvalidDataException>(() => WorkflowRoleDispatch.Resolve(path, "plan"));
        Assert.Equal("Phân vai không hợp lệ.", ex.Message);
        Assert.Equal(before, File.ReadAllBytes(path)); Assert.False(File.Exists(Path.Combine(tmp.Root, "injected")));
    }

    [Theory]
    [InlineData("{")][InlineData("[]")][InlineData("null")]
    public void Corrupt_json_reports_read_error(string text)
    {
        using var tmp = new TempRepo(false); var path = tmp.Write("team.json", text);
        Assert.Equal("Không đọc được cấu hình phân vai.",
            Assert.Throws<IOException>(() => WorkflowRoleDispatch.Resolve(path, "plan")).Message);
    }
    [Theory]
    [InlineData("code")][InlineData("PLAN")][InlineData("plan\n")][InlineData("")]
    public void Other_steps_are_not_dispatchable(string role)
    {
        using var tmp = new TempRepo(false);
        Assert.Equal("Vai trò không được hỗ trợ.", Assert.Throws<ArgumentException>(() => WorkflowRoleDispatch.Resolve(CopyConfig(tmp), role)).Message);
    }

    [Theory]
    [InlineData("plan", "codex", true)][InlineData("plan", "agy", true)]
    [InlineData("prep", "codex", false)][InlineData("prep", "agy", false)]
    [InlineData("qc", "codex", true)][InlineData("qc", "agy", true)]
    public async Task Stub_process_receives_exact_provider_flags_and_lock(string role, string provider, bool readOnly)
    {
        using var tmp = new TempRepo(false);
        var script = LoadFunctions("Invoke-WorkflowRoleRun") + $$"""

            $script:calls=@(); $script:locks=0
            function Invoke-WorkflowCodexLock { param([scriptblock]$Action) $script:locks++; & $Action }
            function Invoke-FakeCli { $script:calls+=,@($args); $global:LASTEXITCODE=7 }
            $assignment=[pscustomobject]@{Role='{{role}}';Actor='{{(provider == "codex" ? "codex" : "minh")}}';ProviderId='{{provider}}';IsExplicit=$true;ReadOnly=${{readOnly.ToString().ToLowerInvariant()}};Manual=$false}
            $command=if ('{{role}}' -eq 'qc') {'review'} else {'run'}
            Invoke-WorkflowRoleRun $assignment 'Invoke-FakeCli' @($command,'--out','résultat avec espace.json','--root','x') | Out-Null
            [pscustomobject]@{calls=$script:calls;locks=$script:locks;code=$LASTEXITCODE} | ConvertTo-Json -Depth 8 -Compress
            """;
        var result = await TestShell.PowerShell(tmp.Root, script);
        Assert.True(result.Code == 0, result.Output + result.Error);
        using var doc = JsonDocument.Parse(result.Output.Trim()); var root = doc.RootElement;
        var call = Assert.Single(root.GetProperty("calls").EnumerateArray()).EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains(role == "qc" ? "--reviewer-provider" : "--provider", call);
        Assert.Contains(provider, call); Assert.Contains("résultat avec espace.json", call);
        Assert.Equal(role == "plan", call.Contains("--read-only"));
        Assert.Equal(provider == "codex" ? 1 : 0, root.GetProperty("locks").GetInt32());
        Assert.Equal(7, root.GetProperty("code").GetInt32());
    }

    [Theory]
    [InlineData("plan")][InlineData("prep")][InlineData("qc")]
    public async Task Manual_assignment_never_invokes_process_or_lock(string role)
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, LoadFunctions("Invoke-WorkflowRoleRun") + $$"""

            function Invoke-FakeCli { throw 'KHÔNG ĐƯỢC GỌI TIẾN TRÌNH' }
            function Invoke-WorkflowCodexLock { throw 'KHÔNG ĐƯỢC KHÓA' }
            $a=[pscustomobject]@{Role='{{role}}';Actor='owner';ProviderId=$null;Manual=$true;IsExplicit=$true}
            try { Invoke-WorkflowRoleRun $a 'Invoke-FakeCli' @('run'); throw 'KHÔNG DỪNG' }
            catch { if ($_.Exception.Message -cne 'Tác nhân được gán cần thực hiện thủ công.') { throw }; 'ĐÃ CHẶN' }
            """);
        Assert.True(result.Code == 0, result.Output + result.Error); Assert.Contains("ĐÃ CHẶN", result.Output);
    }

    [Theory]
    [InlineData("Invoke-Plan")][InlineData("Invoke-Prep")][InlineData("Invoke-AutoReview")]
    public async Task Real_entrypoints_use_shared_dispatch(string name)
    {
        using var tmp = new TempRepo(false);
        var result = await TestShell.PowerShell(tmp.Root, $$"""
            $t=$null;$e=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile({{Quote(Script)}},[ref]$t,[ref]$e)
            $f=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq '{{name}}'},$true)
            $f.Body.FindAll({param($n) $n -is [Management.Automation.Language.CommandAst]},$true) | ForEach-Object {$_.GetCommandName()}
            """);
        Assert.Equal(0, result.Code); Assert.Contains("Resolve-WorkflowAssignment", result.Output);
        Assert.Contains("Invoke-WorkflowRoleRun", result.Output);
    }

    [Theory]
    [InlineData("plan")][InlineData("prep")][InlineData("qc")]
    public async Task Cli_exports_decision_and_does_not_write_config(string role)
    {
        using var tmp = new TempRepo(false); var path = WriteMapped(tmp, role, "lan"); var before = File.ReadAllBytes(path);
        var result = await TestShell.Run(tmp.Root, "dotnet", 30, Path.Combine(AppContext.BaseDirectory, "aiteam.dll"),
            "workflow-role", "--root", tmp.Root, "--role", role);
        Assert.True(result.Code == 0, result.Output + result.Error);
        using var doc = JsonDocument.Parse(result.Output); Assert.Equal("agy", doc.RootElement.GetProperty("providerId").GetString());
        Assert.Equal(before, File.ReadAllBytes(path));
    }
    [Theory]
    [InlineData(RunStatus.Succeeded, 0)]
    [InlineData(RunStatus.QuotaExhausted, 3)]
    [InlineData(RunStatus.NotLoggedIn, 3)]
    [InlineData(RunStatus.TimedOut, 3)]
    [InlineData(RunStatus.Failed, 3)]
    public async Task Assigned_qc_never_uses_router_or_fallback(RunStatus status, int exit)
    {
        using var tmp = new TempRepo(false);
        CopyConfig(tmp);
        var settings = ProviderSettings.Load(tmp.Root);
        var fake = new RecordingProvider(status);
        var created = new List<string>();
        var runner = new ReviewRunner(settings, new ForbiddenRouter(), new ProviderStateStore(tmp.Root),
            cfg => { created.Add(cfg.Id); return fake; }, () => new DateTime(2026, 10, 26, 12, 0, 0));
        var task = CopyTask(tmp);
        var diff = tmp.Write("diff.txt", "diff --git a/a.cs b/a.cs\r\n+// thay đổi\r\n");
        var result = await runner.RunAssignedAsync(new(task, diff, null, tmp.Root, Path.Combine(tmp.Root, "qc"),
            "agy", TimeSpan.FromSeconds(5)), "codex", CancellationToken.None);
        Assert.Equal(exit, result.ExitCode); Assert.Equal(new[] { "codex" }, created);
        Assert.Equal(RunMode.ReadOnly, Assert.Single(fake.Requests).Mode);
        Assert.NotNull(fake.Requests[0].OutputSchemaFile);
        Assert.Equal("codex", result.Result!.ProviderId);
        Assert.Equal("codex", JsonDocument.Parse(ReviewRunner.OutcomeJson(result)).RootElement.GetProperty("provider").GetString());
    }

    [Theory]
    [InlineData("codex", "CODEX", 3)]
    [InlineData("missing-provider", "agy", 2)]
    public async Task Assigned_qc_rejects_self_review_and_missing_provider_before_factory(string provider, string dev, int exit)
    {
        using var tmp = new TempRepo(false);
        CopyConfig(tmp);
        var runner = new ReviewRunner(ProviderSettings.Load(tmp.Root), new ForbiddenRouter(), new ProviderStateStore(tmp.Root),
            _ => throw new InvalidOperationException("Không được gọi factory."), () => DateTime.Now);
        var result = await runner.RunAssignedAsync(new(CopyTask(tmp),
            tmp.Write("diff.txt", "+ thay đổi"), null, tmp.Root, tmp.Root, dev, TimeSpan.FromSeconds(5)), provider, CancellationToken.None);
        Assert.Equal(exit, result.ExitCode); Assert.Null(result.Result);
    }

    private sealed class ForbiddenRouter : IProviderRouter
    {
        public RouteDecision Pick(AgentRole role, string? excludeProviderId, DateTime now) =>
            throw new InvalidOperationException("Không được fallback khi đã gán QC.");
    }
    private sealed class RecordingProvider(RunStatus status) : IAgentProvider
    {
        public string Id => "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public List<AgentRunRequest> Requests { get; } = [];
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => throw new InvalidOperationException("Không kiểm tra tiến trình thật.");
        public Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(new AgentRunResult(Id, status,
                "{\"verdict\":\"PASS\",\"findings\":[],\"unverified\":[]}", TokenUsage.Unknown,
                TimeSpan.Zero, null, null, "Kiểm thử giả lập"));
        }
    }    [Theory]
    [InlineData("disabled", 2)][InlineData("wrong-role", 2)][InlineData("resting", 3)]
    public async Task Assigned_qc_unavailable_does_not_change_assignee(string variant, int exit)
    {
        using var tmp = new TempRepo(false); CopyConfig(tmp);
        var settings = ProviderSettings.Load(tmp.Root);
        settings = settings with { Providers = settings.Providers.Select(p => p.Id != "codex" ? p : p with {
            Enabled = variant != "disabled", Roles = variant == "wrong-role" ? new[] { AgentRole.Developer } : p.Roles }).ToArray() };
        var state = new ProviderStateStore(tmp.Root); var now = new DateTime(2026, 10, 26, 12, 0, 0);
        if (variant == "resting") state.Record(new("codex", RunStatus.QuotaExhausted, "", TokenUsage.Unknown,
            TimeSpan.Zero, null, null, "Kiểm thử"), now);
        var created = 0;
        var runner = new ReviewRunner(settings, new ForbiddenRouter(), state,
            _ => { created++; return new RecordingProvider(RunStatus.Succeeded); }, () => now);
        var result = await runner.RunAssignedAsync(new(CopyTask(tmp),
            tmp.Write("diff.txt", "+ thay đổi"), null, tmp.Root, tmp.Root, "agy", TimeSpan.FromSeconds(5)), "codex", CancellationToken.None);
        Assert.Equal(exit, result.ExitCode); Assert.Equal(0, created);
    }

    [Fact]
    public async Task Parent_junction_is_rejected_without_reading_target()
    {
        using var tmp = new TempRepo(false); var path = CopyConfig(tmp);
        var target = Path.Combine(tmp.Root, "target"); Directory.CreateDirectory(target);
        File.Move(path, Path.Combine(target, "team.json"));
        var link = Path.Combine(tmp.Root, "linked");
        var result = await TestShell.PowerShell(tmp.Root, $"New-Item -ItemType Junction -Path {Quote(link)} -Target {Quote(target)} | Out-Null");
        Assert.True(result.Code == 0, result.Error);
        try
        {
            Assert.Equal("Không đọc được cấu hình phân vai.", Assert.Throws<IOException>(() =>
                WorkflowRoleDispatch.Resolve(Path.Combine(link, "team.json"), "plan")).Message);
        }
        finally { Directory.Delete(link); }
    }}