using AITeam.Hub.Models;
using AITeam.Hub.Services;
namespace AITeam.Hub.Tests;
public sealed class H803ReviewPresentationTests
{
    [Fact] public void Legacy_plain_and_bold_rounds_are_separate_and_keep_prose()
    {
        var rounds = ReviewParser.Parse("Vòng 1 — CẦN SỬA\nGiải thích quan trọng\n- Sửa ca biên\n**Vòng 2 — ĐẠT**\n- Đã kiểm tra");
        Assert.Equal(2, rounds.Count);
        Assert.Equal(ReviewVerdict.NeedsFix, rounds[0].Verdict);
        Assert.Contains("Giải thích quan trọng", rounds[0].Markdown);
        Assert.Equal(ReviewVerdict.Pass, rounds[1].Verdict);
    }
    [Fact] public void Page_retains_actual_page_size_including_last_page()
    {
        var runs = Enumerable.Range(1, 11).Select(i => new AgentRun("P", "T", i, RunState.Success, "SUCCESS", null, "", 1, 1, DateTime.Now.AddMinutes(i), "log"));
        var page = RunHistory.Page(runs, RunFilter.All, null, 3, 5);
        Assert.Equal(5, page.PageSize); Assert.Single(page.Items); Assert.Equal(11, page.Total); Assert.Equal(3, page.Page);
    }
}
