using AITeam.Hub.Models;
using AITeam.Hub.Pages.Shared;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class H007ExtraTests
{
    [Fact]
    public void MarkdownRenderer_escapes_script_and_blocks_javascript_urls()
    {
        var malicious = "Phản hồi có <script>alert('XSS')</script> và link [bấm vào](javascript:alert('evil')).";
        var rendered = MarkdownRenderer.Render(malicious).Value;

        Assert.NotNull(rendered);
        Assert.DoesNotContain("<script>", rendered);
        Assert.Contains("&lt;script&gt;", rendered);
        Assert.DoesNotContain("href=\"javascript:", rendered);
    }

    [Fact]
    public void RenderInlineSafe_escapes_html_and_renders_markdown_safely()
    {
        var raw = "<img src=x onerror=alert(1)> **In đậm** và `code`";
        var rendered = MarkdownRenderer.RenderInlineSafe(raw).Value;

        Assert.NotNull(rendered);
        Assert.DoesNotContain("<img", rendered);
        Assert.Contains("&lt;img", rendered);
        Assert.Contains("<strong>In đậm</strong>", rendered);
        Assert.Contains("<code class=\"inline-code\">code</code>", rendered);
    }

    [Fact]
    public void ReviewParser_handles_no_items_and_mixed_verdicts()
    {
        var md = """
            ### Vòng 1 — ĐẠT VÀ HOÀN TẤT
            Chỉ có nhận xét chung mà không có danh sách lỗi nào.
            """;
        var rounds = ReviewParser.Parse(md);
        Assert.Single(rounds);
        Assert.Equal(ReviewVerdict.Pass, rounds[0].Verdict);
        Assert.Empty(rounds[0].Items);
        Assert.Contains("Chỉ có nhận xét chung", rounds[0].Markdown);
    }

    [Fact]
    public void BoardViews_handles_empty_and_less_than_take()
    {
        var empty = BoardViews.MergedPreview(Array.Empty<TaskCard>());
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.Remaining);

        var cards = new[]
        {
            new TaskCard("P", "C-002", "T2", "A", "b", TaskState.Merged, "f", 1, null),
            new TaskCard("P", "C-001", "T1", "A", "b", TaskState.Merged, "f", 1, null)
        };
        var res = BoardViews.MergedPreview(cards, 7);
        Assert.Equal(2, res.Items.Count);
        Assert.Equal("C-002", res.Items[0].Id);
        Assert.Equal(0, res.Remaining);
    }
}
