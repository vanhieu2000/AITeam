using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class SkeletonTests
{
    [Fact]
    public void Paths_are_derived_from_root()
    {
        var p = new HubPaths(@"C:\x\AITeam", @"C:\Users\u");
        Assert.Equal(@"C:\x\AITeam\projects.json", p.ProjectsJson);
        Assert.Equal(@"C:\Users\u\.aiteam\vault.json", p.VaultFile);
    }
}
