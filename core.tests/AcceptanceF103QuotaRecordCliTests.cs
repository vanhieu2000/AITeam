using AITeam.Core.AiServices;
using AITeam.Core.Process;

namespace AITeam.Core.Tests;

/// <summary>Test nghiệm thu F1-03 phần CLI (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-03.
/// team.ps1 assign phát hiện hết hạn mức ⇒ ghi số đo vào office.db để trang Đội AI hiện "hết hạn mức, hồi lúc …".</summary>
public sealed class AcceptanceF103QuotaRecordCliTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 21, 0, 0, TimeSpan.Zero);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-f103-" + Guid.NewGuid().ToString("N"));
    private readonly string _db;

    public AcceptanceF103QuotaRecordCliTests()
    {
        Directory.CreateDirectory(_root);
        _db = Path.Combine(_root, "office.db");
        new SqliteAiServiceRegistry(_db, _root).Upsert(new AiService("codex", "Codex", AiAdapter.CliJson, "codex", [], null, ["m"], [AiRole.Code], 1, false, null, null, true, Now, Now));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_root, true); } catch { }
    }

    private Task<ProcessResult> Cli(params string[] args)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aiteam.dll");
        return new ProcessRunner().RunAsync(new ProcessSpec("dotnet", [dll, "quota-record", "--db", _db, .. args], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
    }

    [Fact]
    public async Task Records_quota_with_rest_time()
    {
        var r = await Cli("--service", "codex", "--remaining", "0", "--rest-until", "2026-11-05T02:00:00Z", "--source", "assign");
        Assert.Equal(0, r.ExitCode);

        var q = Assert.Single(new SqliteAiServiceRegistry(_db, _root).LatestQuotas());
        Assert.Equal("codex", q.ServiceId);
        Assert.Equal(0, q.RemainingPct);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 2, 0, 0, TimeSpan.Zero), q.RestUntil);
        Assert.Equal("assign", q.Source);
    }

    [Fact]
    public async Task Rest_time_is_optional()
    {
        var r = await Cli("--service", "codex", "--remaining", "0", "--source", "assign");
        Assert.Equal(0, r.ExitCode);
        Assert.Null(Assert.Single(new SqliteAiServiceRegistry(_db, _root).LatestQuotas()).RestUntil);
    }

    [Theory]
    [InlineData("--service", "khong-co", "--remaining", "0", "--source", "assign")]
    [InlineData("--service", "codex", "--remaining", "150", "--source", "assign")]
    [InlineData("--service", "codex", "--remaining", "abc", "--source", "assign")]
    [InlineData("--service", "codex", "--remaining", "0", "--rest-until", "mai", "--source", "assign")]
    [InlineData("--service", "codex", "--remaining", "0")]
    public async Task Bad_input_fails_without_writing(params string[] args)
    {
        var r = await Cli(args);
        Assert.NotEqual(0, r.ExitCode);
        Assert.StartsWith("LỖI", r.StdErr.TrimStart());
        Assert.Empty(new SqliteAiServiceRegistry(_db, _root).LatestQuotas());
    }
}
