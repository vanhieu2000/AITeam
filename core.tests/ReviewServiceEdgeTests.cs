using AITeam.Core.Review;

namespace AITeam.Core.Tests;

public sealed class ReviewServiceEdgeTests
{
    [Fact]
    public void ParseOutput_handles_optional_finding_and_blocked_verdict()
    {
        var opt = ReviewService.ParseOutput("""{"verdict":"PASS","findings":[{"severity":"optional","file":"a.cs","line":10,"evidence":"nhỏ","fix":"gợi ý"}],"unverified":[]}""");
        Assert.NotNull(opt);
        Assert.Equal(ReviewVerdict.Pass, opt!.Value.Verdict);
        Assert.Equal(FindingSeverity.Optional, Assert.Single(opt.Value.Findings).Severity);

        var blocked = ReviewService.ParseOutput("""{"verdict":"BLOCKED","findings":[],"unverified":["Thiếu môi trường"]}""");
        Assert.NotNull(blocked);
        Assert.Equal(ReviewVerdict.Blocked, blocked!.Value.Verdict);
        Assert.Equal(["Thiếu môi trường"], blocked.Value.Unverified);
    }
}
