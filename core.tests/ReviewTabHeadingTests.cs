using AITeam.Core.Review;

namespace AITeam.Core.Tests;

public sealed class ReviewTabHeadingTests
{
    [Fact]
    public void Tab_after_second_level_heading_starts_and_ends_review_section()
    {
        const string task = "##\tReview (Claude)\n### Vòng 1 — CẦN SỬA\n##\tKết quả\n### Vòng 2 — Không thuộc review\n";
        Assert.Equal(1, ReviewRoundCounter.Count(task, null));
    }
}
