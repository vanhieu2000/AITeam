using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class InboxServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void Add_creates_file_with_header_and_merges_multiline_content()
    {
        var service = new InboxService(_env.Paths);

        Assert.False(File.Exists(_env.Paths.InboxFile));
        Assert.Empty(service.GetItems());

        // Thêm tin nhắn có nhiều dòng
        string multiline = "Dòng một\r\nDòng hai\n   Dòng ba";
        service.Add("HDVTool", multiline);

        Assert.True(File.Exists(_env.Paths.InboxFile));
        string content = File.ReadAllText(_env.Paths.InboxFile);
        Assert.StartsWith("# Hộp thư yêu cầu cho team", content);
        Assert.Contains("HDVTool: Dòng một Dòng hai Dòng ba", content);

        var items = service.GetItems();
        Assert.Single(items);
        Assert.Equal("HDVTool", items[0].Project);
        Assert.Equal("Dòng một Dòng hai Dòng ba", items[0].Text);
        Assert.False(items[0].Done);
        Assert.NotNull(items[0].Time);
    }

    [Fact]
    public void GetItems_parses_done_and_undone_items_correctly()
    {
        string text = @"# Hộp thư yêu cầu cho team

- [ ] 2026-10-20 18:00 · ProjectAlpha: Yêu cầu tính năng A
- [x] 2026-10-20 19:30 · ProjectBeta: Đã xử lý xong việc B
";
        File.WriteAllText(_env.Paths.InboxFile, text);

        var service = new InboxService(_env.Paths);
        var items = service.GetItems();

        Assert.Equal(2, items.Count);

        Assert.False(items[0].Done);
        Assert.Equal("ProjectAlpha", items[0].Project);
        Assert.Equal("Yêu cầu tính năng A", items[0].Text);
        Assert.Equal(new DateTime(2026, 10, 20, 18, 0, 0), items[0].Time);

        Assert.True(items[1].Done);
        Assert.Equal("ProjectBeta", items[1].Project);
        Assert.Equal("Đã xử lý xong việc B", items[1].Text);
        Assert.Equal(new DateTime(2026, 10, 20, 19, 30, 0), items[1].Time);
    }
}
