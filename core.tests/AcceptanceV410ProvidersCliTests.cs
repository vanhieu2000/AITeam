using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Routing;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V4-10 (Claude viết trước — agent KHÔNG sửa). `aiteam providers --no-check` hiện trạng thái nghỉ.
public sealed class AcceptanceV410ProvidersCliTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v410-" + Guid.NewGuid().ToString("N"));

    public AcceptanceV410ProvidersCliTests()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "team.json"), """{ "agy": { "exe": "C:\\khong-co\\agy.exe" } }""");
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private async Task<ProcessResult> Cli(params string[] args)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        return await new ProcessRunner().RunAsync(new ProcessSpec("dotnet", [dll, .. args], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
    }

    [Fact]
    public async Task No_check_lists_providers_without_running_them_and_shows_rest()
    {
        var store = new ProviderStateStore(_root);
        store.Record(new AgentRunResult("codex", RunStatus.QuotaExhausted, "", TokenUsage.Unknown, TimeSpan.Zero, null, null, "try again in 2 hours"), DateTime.Now);
        var until = store.RestOf("codex", DateTime.Now)!.Until;

        var r = await Cli("providers", "--no-check", "--root", _root);
        Assert.Equal(0, r.ExitCode);
        var lines = r.StdOut.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var agy = Assert.Single(lines, l => l.Contains("] agy ("));
        Assert.StartsWith("  [SẴN SÀNG] agy (Antigravity)", agy);
        var codex = Assert.Single(lines, l => l.Contains("] codex ("));
        Assert.StartsWith($"  [NGHỈ tới {until.ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture)}] codex (Codex)", codex);
        Assert.Contains(lines, l => l.Trim().StartsWith("QuotaExhausted: try again in 2 hours"));
        Assert.Contains("== Routing (thứ tự ưu tiên) ==", r.StdOut);
        Assert.DoesNotContain("CHƯA CÀI", r.StdOut);   // --no-check ⇒ không chạy agy (exe không tồn tại)
    }

    [Fact]
    public async Task No_check_without_state_file_all_ready()
    {
        var r = await Cli("providers", "--no-check", "--root", _root);
        Assert.Equal(0, r.ExitCode);
        Assert.Equal(2, r.StdOut.Split('\n').Count(l => l.Contains("[SẴN SÀNG]")));
    }

    [Fact]
    public async Task Without_no_check_missing_agy_is_reported_and_exit_1()
    {
        // Tắt codex theo máy ⇒ test không gọi CLI ngoài nào (agy.exe không tồn tại ⇒ CHƯA CÀI).
        File.WriteAllText(Path.Combine(_root, "team.local.json"), """{ "providers": { "codex": { "enabled": false } } }""");
        var r = await Cli("providers", "--root", _root);
        Assert.Equal(1, r.ExitCode);
        Assert.Contains("[CHƯA CÀI] agy", r.StdOut);
        Assert.Contains("[TẮT] codex", r.StdOut);
    }
}
