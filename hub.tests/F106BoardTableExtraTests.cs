using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class F106BoardTableExtraTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    public void Short_rows_keep_empty_cells_and_original_line_numbers(string newline)
    {
        var markdown = string.Join(newline, new[]
        {
            "| Task | Agent | Branch | Status |",
            "|:---|---:|---|---|",
            "| T-01 · Một | | `agent-c/T-01` | 🔵 Chờ review",
            "",
            "| T-02",
        });

        var rows = BoardTable.Parse(markdown);

        Assert.Equal(2, rows.Count);
        Assert.Equal("", rows[0].Agent);
        Assert.Equal("agent-c/T-01", rows[0].Branch);
        Assert.Equal("🔵 Chờ review", rows[0].Status);
        Assert.Equal(5, rows[1].Line);
        Assert.Equal("", rows[1].Agent);
        Assert.Equal("", rows[1].Branch);
        Assert.Equal("", rows[1].Status);
    }

    [Fact]
    public void Invalid_divider_is_skipped_and_malformed_link_does_not_throw()
    {
        var markdown = """
            | Task | Status |
            |--- chữ sai ---|
            | T-01 Bỏ qua | ✅ |
            văn bản
            | tAsK | STATUS |
            |---|---|
            | [T-02A · — Tên](a.md) | ✅ |
            | [T-03 Hỏng](a
            """;

        var rows = BoardTable.Parse(markdown);

        var row = Assert.Single(rows);
        Assert.Equal("T-02A", row.Id);
        Assert.Equal("Tên", row.Title);
        Assert.Equal("a.md", row.File);
        Assert.Equal("✅", row.Status);
    }
}
