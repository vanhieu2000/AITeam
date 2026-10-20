using AITeam.Hub.Pages.Shared;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Heading_rendered_correctly()
    {
        var md = "# Tiêu đề 1\n## Tiêu đề 2\n### Tiêu đề 3";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<h1>Tiêu đề 1</h1>", html);
        Assert.Contains("<h2>Tiêu đề 2</h2>", html);
        Assert.Contains("<h3>Tiêu đề 3</h3>", html);
    }

    [Fact]
    public void Bold_and_italic_inline_rendered()
    {
        var md = "Chữ **đậm** và chữ *nghiêng*";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<strong>đậm</strong>", html);
        Assert.Contains("<em>nghiêng</em>", html);
    }

    [Fact]
    public void Inline_code_and_code_block_rendered()
    {
        var md = "Dùng lệnh `dotnet build` để biên dịch.\n```csharp\nvar x = 10;\n```";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<code class=\"inline-code\">dotnet build</code>", html);
        Assert.Contains("<pre class=\"code-block\"><code class=\"language-csharp\">var x = 10;</code></pre>", html);
    }

    [Fact]
    public void Lists_and_checkboxes_rendered()
    {
        var md = "- [ ] Chưa xong\n- [x] Đã xong\n- Việc thường";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<input type=\"checkbox\" disabled /> Chưa xong", html);
        Assert.Contains("<input type=\"checkbox\" checked disabled /> Đã xong", html);
        Assert.Contains("<li>Việc thường</li>", html);
    }

    [Fact]
    public void Table_rendered_correctly()
    {
        var md = "| Cột 1 | Cột 2 |\n|---|---|\n| Giá trị A | Giá trị B |";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<table class=\"markdown-table\">", html);
        Assert.Contains("<th>Cột 1</th>", html);
        Assert.Contains("<th>Cột 2</th>", html);
        Assert.Contains("<td>Giá trị A</td>", html);
        Assert.Contains("<td>Giá trị B</td>", html);
    }

    [Fact]
    public void Links_rendered_safely()
    {
        var md = "[Trang chủ](https://example.com) và [Nội bộ](/p/AITeam)";
        var html = MarkdownRenderer.Render(md).Value;

        Assert.Contains("<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Trang chủ</a>", html);
        Assert.Contains("<a href=\"/p/AITeam\" target=\"_blank\" rel=\"noopener noreferrer\">Nội bộ</a>", html);
    }

    [Fact]
    public void Xss_script_tags_are_encoded()
    {
        var evil = "<script>alert('xss')</script>";
        var html = MarkdownRenderer.Render(evil).Value;

        // Thẻ script phải bị mã hóa HTML, không được xuất hiện thẻ raw HTML
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
    }

    [Fact]
    public void Xss_img_onerror_is_encoded()
    {
        var evil = "<img src=x onerror=\"alert('hacked')\" />";
        var html = MarkdownRenderer.Render(evil).Value;

        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public void Javascript_pseudo_protocol_links_are_neutralized()
    {
        var evilLink = "[Click me](javascript:alert(1))";
        var html = MarkdownRenderer.Render(evilLink).Value;

        Assert.DoesNotContain("<a href=\"javascript:", html);
        Assert.Contains("Click me", html);
    }
}
