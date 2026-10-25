namespace AITeam.Core.Tests.Support;

public sealed class TestShellTests
{
    [Fact]
    public async Task PowerShell_GiuTiengVietVaStderrKhongCliXml()
    {
        using var repo = new TempRepo(git: false);
        var r = await TestShell.PowerShell(repo.Root,
            "Write-Output 'Tiếng Việt đủ dấu'; [Console]::Error.WriteLine('LỖI: thử'); exit 3");
        Assert.Equal(3, r.Code);
        Assert.Equal("Tiếng Việt đủ dấu", r.Output.Trim());
        Assert.Equal("LỖI: thử", r.Error.Trim());
    }

    [Fact]
    public void TempRepo_CommitTrungVaDonDuocObjectChiDoc()
    {
        string root;
        using (var repo = new TempRepo())
        {
            root = repo.Root;
            repo.Write("a.md", "# A\r\n");
            repo.Git("add", "--", "a.md");
            repo.Git("commit", "-q", "-m", "lần 1");
            repo.Git("commit", "-q", "-m", "trùng nội dung");
            Assert.Equal("2", repo.Git("rev-list", "--count", "HEAD").Trim());
        }
        Assert.False(Directory.Exists(root));
    }
}
