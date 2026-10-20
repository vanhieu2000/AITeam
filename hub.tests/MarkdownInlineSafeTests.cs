using AITeam.Hub.Pages.Shared;

namespace AITeam.Hub.Tests;

/// <summary>Review H-002 (Claude): tiêu chí task hiển thị bị mã hóa 2 lần; hàm an toàn phải mã hóa đúng 1 lần.</summary>
public sealed class MarkdownInlineSafeTests
{
    [Fact]
    public void Inline_code_renders_as_html_not_as_escaped_text()
    {
        string html = MarkdownRenderer.RenderInlineSafe("Chạy `dotnet test` 0 warning").ToString();
        Assert.Contains("<code class=\"inline-code\">dotnet test</code>", html);
        Assert.DoesNotContain("&lt;code", html);
    }

    [Fact]
    public void Raw_html_in_criterion_is_encoded_exactly_once()
    {
        string html = MarkdownRenderer.RenderInlineSafe("<script>alert(1)</script> & **đậm**").ToString();
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Contains("&amp;", html);
        Assert.DoesNotContain("&amp;lt;", html);
        Assert.Contains("<strong>đậm</strong>", html);
    }

    [Fact]
    public void Javascript_link_is_not_rendered_as_link()
    {
        string html = MarkdownRenderer.RenderInlineSafe("[bấm](javascript:alert(1))").ToString();
        Assert.DoesNotContain("<a ", html);
    }
}
