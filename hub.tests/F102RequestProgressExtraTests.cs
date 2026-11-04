using System.Text;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class F102RequestProgressExtraTests
{
    [Fact]
    public void Update_preserves_lf_without_bom_and_rejects_invalid_state()
    {
        using var env = new TestEnvironment();
        var folder = Path.Combine(env.AITeamRoot, "requests");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "R-010-test.md");
        const string original = "# R-010 · Kiểm tra\n- **Trạng thái:** 🟡 Đang làm\n\n## 4. Đánh giá\nGiữ nguyên\n";
        File.WriteAllText(file, original, new UTF8Encoding(false));
        var service = new RequestProgressService(env.Paths, new TeamConfigService(env.Paths), new RequestService(env.Paths), TimeProvider.System);
        Assert.False(service.SetState("R-010", (RequestState)99, null).Ok);
        Assert.Equal(original, File.ReadAllText(file));
        Assert.True(service.SetState("R-010", RequestState.Open, " a\0\t b | c ").Ok);
        var expected = original.Replace("🟡 Đang làm", "🟡 Đang làm — a b | c");
        Assert.Equal(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(file));
        Assert.False(service.SetState("R-010\n", RequestState.Done, null).Ok);
        Assert.Equal(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(file));
    }
}