using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AITeam.Core.Providers;
using AITeam.Core.Review;
using AITeam.Core.Routing;
using AITeam.Core.Skills;
using Xunit;

namespace AITeam.Core.Tests;

public sealed class AcceptanceS105ReviewSkillsTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "s105-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(_tmp, "root");
    private string Profile => Path.Combine(_tmp, "profile");
    private string Repo => Path.Combine(_tmp, "repo");
    private string Work => Path.Combine(_tmp, "worktrees");
    private string TaskFile => Path.Combine(Repo, "tasks", "S1-05.md");
    private const string TaskText = "# S1-05 · Kiểm tra\n- **Agent:** B\n- **Review AI:** có\n## Mục tiêu\nKiểm tra độc lập\n";
    private const string Diff = "diff --git a/Main.cs b/Main.cs\n+// WinForms không phải cấu hình stack\n";
    private static readonly string[] Skills = ["09-qc", "code-test", "code-winforms", "code-web", "code-app"];

    public AcceptanceS105ReviewSkillsTests()
    {
        foreach (var dir in new[] { Root, Profile, Repo, Work, Path.GetDirectoryName(TaskFile)! })
            Directory.CreateDirectory(dir);
        foreach (var id in Skills)
        {
            var dir = Path.Combine(Root, "skills", id);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "SKILL.md"), "NỘI DUNG KHÔNG NHÚNG");
        }
        File.WriteAllText(TaskFile, TaskText);
        Config("ASP.NET Razor Pages");
    }

    private void Config(string? stack) => File.WriteAllText(Path.Combine(Root, "projects.json"),
        JsonSerializer.Serialize(new { projects = new[] { new { name = "demo", path = Repo, workDir = Work, stack } } }));
    private string Instructions(string? work = null, string? task = null) =>
        SkillPromptBuilder.BuildReviewInstructions(Root, work ?? Work, task ?? TaskFile, Profile);
    private string SkillPath(string id) => Path.Combine(Root, "skills", id, "SKILL.md");
    private void AssertSkills(string text, string? stack)
    {
        foreach (var id in Skills)
        {
            if (id is "09-qc" or "code-test" || id == stack) Assert.Contains(SkillPath(id), text);
            else Assert.DoesNotContain(SkillPath(id), text);
        }
        Assert.DoesNotContain("NỘI DUNG KHÔNG NHÚNG", text);
    }

    [Theory]
    [InlineData("WinForms", "code-winforms")]
    [InlineData("ASP.NET Razor Pages", "code-web")]
    [InlineData("maui", "code-app")]
    [InlineData("console", "code-app")]
    [InlineData("CLI", "code-app")]
    public void StackLayTuCauHinh_WorktreeKhacRoot(string stack, string expected)
    {
        Config(stack);
        var text = Instructions(Path.Combine(Work, "agent-b", "S1-05"));
        AssertSkills(text, expected);
        Assert.Contains("Đọc skill:", text);
        Assert.True(text.IndexOf(SkillPath("09-qc"), StringComparison.Ordinal) < text.IndexOf(SkillPath("code-test"), StringComparison.Ordinal));
        Assert.True(text.IndexOf(SkillPath("code-test"), StringComparison.Ordinal) < text.IndexOf(SkillPath(expected), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("không biết")]
    [InlineData("../../outside")]
    public void StackCuRongHoacLa_KhongDoanTuDiff(string? stack)
    {
        Config(stack);
        AssertSkills(Instructions(), null);
    }

    [Fact]
    public void CauHinhCuKhongCoStack()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), JsonSerializer.Serialize(new
        { projects = new[] { new { name = "demo", path = Repo, workDir = Work } } }));
        AssertSkills(Instructions(), null);
    }

    [Fact]
    public void KhongTimDuocDuAn_KhongKhopTienToThuMuc()
        => AssertSkills(Instructions(Work + "-khac", Repo + "-khac/task.md"), null);

    [Fact]
    public void TimTuTaskKhiWorkDirKhongNamTrongCauHinh()
        => AssertSkills(Instructions(Path.Combine(_tmp, "unknown")), "code-web");

    [Fact]
    public void ThieuCauHinh_ChiBoStack()
    {
        File.Delete(Path.Combine(Root, "projects.json"));
        AssertSkills(Instructions(), null);
    }

    [Fact]
    public void ThieuMotSkill_ConLaiVanDuocDuaVao()
    {
        File.Delete(SkillPath("09-qc"));
        var text = Instructions();
        Assert.DoesNotContain(SkillPath("09-qc"), text);
        Assert.Contains(SkillPath("code-test"), text);
        Assert.Contains(SkillPath("code-web"), text);
    }

    [Fact]
    public void ThieuTatCaSkill_KhongThemChiDan()
    {
        foreach (var id in Skills) File.Delete(SkillPath(id));
        Assert.Equal("", Instructions());
    }

    [Fact]
    public void JsonHong_KhongAmThamCoiLaCauHinhRong()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), "{ hỏng");
        Assert.ThrowsAny<JsonException>(() => Instructions());
    }

    [Fact]
    public void HaiDuAnMauThuan_BoStack()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), JsonSerializer.Serialize(new { projects = new[] {
            new { name = "demo", path = Repo, workDir = Work, stack = "WinForms" },
            new { name = "khac", path = Path.Combine(_tmp, "other"), workDir = Path.Combine(_tmp, "other-work"), stack = "MAUI" }
        } }));
        AssertSkills(Instructions(Path.Combine(_tmp, "other-work")), null);
    }

    [Fact]
    public void LoiGoiCuKhongRoot_GiuNguyenPack()
    {
        var input = new ReviewRunInput(TaskFile, "diff", null, Work, Path.Combine(_tmp, "logs"), "agy", TimeSpan.FromMinutes(1));
        var expected = new ReviewService().BuildPack(new ReviewPackInput(TaskText, Diff, null, null));
        Assert.Equal(expected, ReviewRunner.BuildReviewPack(input, TaskText, Diff, null));
        Assert.Equal("", SkillPromptBuilder.BuildReviewInstructions(null, Work, TaskFile, Profile));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderGiaNhanPromptCuoi_TrucTiepVaTuDong_GiuSchemaHash(bool auto)
    {
        Assert.True(!auto || ReviewPolicy.Decide(TaskText).AutoReview);
        var fake = new CaptureProvider();
        var outcome = await Run(fake);
        Assert.Equal(ReviewRunner.ExitPass, outcome.ExitCode);
        AssertSkills(fake.Prompt!, "code-web");
        Assert.Contains("CHỈ ĐỌC", fake.Prompt!);
        Assert.Contains(Diff.Trim(), fake.Prompt!);
        Assert.Equal(ReviewService.OutputSchemaJson, fake.Schema);
        Assert.Equal(RunMode.ReadOnly, fake.Request!.Mode);
        Assert.Equal(Work, fake.Request.WorkDir);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Diff))).ToLowerInvariant();
        Assert.Equal(hash, outcome.Result!.DiffHash);
        using var json = JsonDocument.Parse(ReviewRunner.OutcomeJson(outcome));
        Assert.Equal(new[] { "diffHash", "exitCode", "feedback", "notes", "provider", "summary", "verdict" },
            json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray());
        Assert.Equal(hash, json.RootElement.GetProperty("diffHash").GetString());
    }

    [Fact]
    public async Task CamTuDuyet_VanHieuLucKhiCoSkill()
    {
        var fake = new CaptureProvider { Id = "agy" };
        var outcome = await Run(fake);
        Assert.Equal(ReviewRunner.ExitBlocked, outcome.ExitCode);
        Assert.Null(fake.Request);
    }

    [Fact]
    public void DiffDai_GiuHashDayDuVaCoCat()
    {
        var diff = new string('x', 120_001);
        var input = new ReviewRunInput(TaskFile, "diff", null, Work, Path.Combine(_tmp, "logs"), "agy", TimeSpan.FromMinutes(1), Root, Profile);
        var pack = ReviewRunner.BuildReviewPack(input, TaskText, diff, null);
        Assert.True(pack.DiffTruncated);
        Assert.Contains("[ĐÃ CẮT", pack.Prompt);
        AssertSkills(pack.Prompt, "code-web");
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(diff))).ToLowerInvariant(), pack.DiffHash);
    }

    [Fact]
    public void DuAnLongNhau_ChonDuongDanDaiNhat()
    {
        var nested = Path.Combine(Work, "nested");
        File.WriteAllText(Path.Combine(Root, "projects.json"), JsonSerializer.Serialize(new { projects = new[] {
            new { name = "cha", path = Repo, workDir = Work, stack = "WinForms" },
            new { name = "con", path = Path.Combine(_tmp, "nested-repo"), workDir = nested, stack = "MAUI" }
        } }));
        AssertSkills(Instructions(Path.Combine(nested, "agent-b"), Path.Combine(nested, "tasks", "task.md")), "code-app");
    }

    [Fact]
    public async Task JsonHong_RunnerBlockedKhongGoiProvider()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), "{ hỏng");
        var fake = new CaptureProvider();
        var outcome = await Run(fake);
        Assert.Equal(ReviewRunner.ExitBlocked, outcome.ExitCode);
        Assert.Contains("Json", outcome.Summary);
        Assert.Null(fake.Request);
    }
    private async Task<ReviewRunOutcome> Run(CaptureProvider fake)
    {
        var diffFile = Path.Combine(_tmp, "diff.patch");
        File.WriteAllText(diffFile, Diff);
        var settings = ProviderSettings.Default("không-chạy-agy");
        var state = new ProviderStateStore(Root);
        var runner = new ReviewRunner(settings, new ProviderRouter(settings, state), state, _ => fake, () => DateTime.Now);
        return await runner.RunAsync(new ReviewRunInput(TaskFile, diffFile, null, Work,
            Path.Combine(_tmp, "logs"), "agy", TimeSpan.FromMinutes(1), Root, Profile), CancellationToken.None);
    }

    private sealed class CaptureProvider : IAgentProvider
    {
        public string Id { get; init; } = "codex";
        public ProviderKind Kind => ProviderKind.Codex;
        public bool HasReadOnlySandbox => true;
        public AgentRunRequest? Request { get; private set; }
        public string? Prompt { get; private set; }
        public string? Schema { get; private set; }
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(Id, true, true, "giả"));
        public async Task<AgentRunResult> RunAsync(AgentRunRequest request, CancellationToken ct)
        {
            Request = request;
            Prompt = await File.ReadAllTextAsync(request.PromptFile, ct);
            Schema = await File.ReadAllTextAsync(request.OutputSchemaFile!, ct);
            return new AgentRunResult(Id, RunStatus.Succeeded, "{\"verdict\":\"PASS\",\"findings\":[],\"unverified\":[]}",
                TokenUsage.Unknown, TimeSpan.Zero, null, null, "");
        }
    }

    public void Dispose() => Directory.Delete(_tmp, true);
}