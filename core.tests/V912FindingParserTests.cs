using AITeam.Core.Metrics;
namespace AITeam.Core.Tests;
public sealed class V912FindingParserTests
{
    [Theory]
    [InlineData("TK")][InlineData("TN")][InlineData("AG")][InlineData("CK")][InlineData("HT")][InlineData("HM")][InlineData("CH")][InlineData("TO")][InlineData("DP")]
    public void Standard_codes_keep_round_group_and_original_description(string code)
    {
        var item=Assert.Single(FindingParser.Parse($"### Vòng 2 — CẦN SỬA\n- [{code}] (test) Nội dung gốc; giữ dấu."));
        Assert.Equal(code,item.Cause);Assert.Equal(2,item.Round);Assert.Equal("test",item.Kind);Assert.Equal("Nội dung gốc; giữ dấu.",item.Text);
    }
    [Theory][InlineData("NV")][InlineData("ZZ")]
    public void Unknown_code_keeps_content(string code)
    {
        var item=Assert.Single(FindingParser.Parse($"- [{code}] Nội dung gốc"));
        Assert.Equal("khác",item.Cause);Assert.Equal("Nội dung gốc",item.Text);
    }
    [Fact]public void Five_real_reviews_are_read_without_losing_unknown_findings()
    {
        var files=Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Fixtures","V912"),"*.md").Where(p=>Path.GetFileName(p)!="SOURCE.md").ToArray();
        Assert.Equal(5,files.Length);
        foreach(var file in files) { var issues=FindingParser.Parse(File.ReadAllText(file));Assert.NotEmpty(issues);Assert.All(issues,i=>Assert.Contains(i.Cause,FindingParser.Causes.Append("khác"))); }
    }
    [Fact]public void Fenced_examples_comments_and_checkbox_are_not_findings()
    {
        Assert.Empty(FindingParser.Parse("```\n- [AG] ví dụ\n```\n<!--\n- [TK] ẩn\n-->\n- [x] hoàn thành\n- [AG|TK|CK] mẫu"));
    }
}
