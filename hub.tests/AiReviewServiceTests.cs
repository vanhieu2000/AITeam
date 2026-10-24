using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// R-002 P0-8 (Claude): Hub đọc kết luận review AI ở <WorkDir>/logs/<Task>.review-<n>.json.
public sealed class AiReviewServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly string _work;
    private readonly string _logs;

    public AiReviewServiceTests()
    {
        _work = Path.Combine(_env.RootDir, "work");
        _logs = Path.Combine(_work, "logs");
        Directory.CreateDirectory(_logs);
    }

    public void Dispose() => _env.Dispose();

    private sealed class Config(string work) : ITeamConfigService
    {
        private readonly ProjectInfo _p = new("AITeam", "vanhieu2000/AITeam", work, work, "", "", "", new Dictionary<string, string>(), true);
        public IReadOnlyList<ProjectInfo> GetProjects() => new[] { _p };
        public ProjectInfo? FindProject(string name) => name == "AITeam" ? _p : null;
        public TeamSettings GetSettings() => new("", 3, Array.Empty<DeveloperInfo>(), "", "", "", Array.Empty<string>(), "");
        public void SaveSettings(TeamSettings settings) { }
        public void SaveProject(ProjectInfo project) { }
    }

    // Định dạng thật do aiteam review (ReviewRunner.OutcomeJson) ghi — tiếng Việt bị mã \uXXXX.
    private const string Changes = """
        {
          "exitCode": 1,
          "verdict": "CHANGES_REQUESTED",
          "summary": "CHANGES_REQUESTED \u00B7 codex \u00B7 1 ph\u00E1t hi\u1EC7n (0 blocker, 1 required, 0 optional) \u00B7 43s",
          "notes": [ "codex: ngh\u1EC9 t\u1EDBi 04:19 24/10 (QuotaExhausted)" ],
          "feedback": "Reviewer codex y\u00EAu c\u1EA7u s\u1EEDa:\n- [required] a.cs:1 \u2014 x \u21D2 y",
          "provider": "codex",
          "diffHash": "abc"
        }
        """;

    [Fact]
    public void Reads_reviews_newest_first_and_skips_bad_files()
    {
        File.WriteAllText(Path.Combine(_logs, "V4-08.review-1.json"), Changes);
        File.WriteAllText(Path.Combine(_logs, "V4-08.review-2.json"), """{"exitCode":0,"verdict":"PASS","summary":"PASS · codex","notes":[],"feedback":"","provider":"codex"}""");
        File.WriteAllText(Path.Combine(_logs, "V4-08.review-3.json"), "{ hỏng");
        File.WriteAllText(Path.Combine(_logs, "V4-08.review-4.json"), """{"verdict":"SKIPPED"}""");
        File.WriteAllText(Path.Combine(_logs, "V4-08-1.json"), """{"verdict":"PASS"}""");        // log lượt chạy, không phải review
        File.WriteAllText(Path.Combine(_logs, "V4-080.review-1.json"), """{"verdict":"PASS"}"""); // task khác

        var r = new AiReviewService(new Config(_work)).GetReviews("AITeam", "V4-08");
        Assert.Equal(new[] { 2, 1 }, r.Select(x => x.Attempt));
        Assert.Equal("PASS", r[0].Verdict);
        var c = r[1];
        Assert.Equal(("CHANGES_REQUESTED", "codex"), (c.Verdict, c.Provider));
        Assert.StartsWith("CHANGES_REQUESTED · codex · 1 phát hiện", c.Summary);
        Assert.Equal("codex: nghỉ tới 04:19 24/10 (QuotaExhausted)", Assert.Single(c.Notes));
        Assert.StartsWith("Reviewer codex yêu cầu sửa:\n- [required] a.cs:1", c.Feedback);
    }

    [Theory]
    [InlineData("AITeam", "../x")]
    [InlineData("AITeam", "")]
    [InlineData("AITeam", "V4-08*")]
    [InlineData("KhongCo", "V4-08")]
    public void Unsafe_or_unknown_gives_empty(string project, string task)
    {
        File.WriteAllText(Path.Combine(_logs, "V4-08.review-1.json"), Changes);
        Assert.Empty(new AiReviewService(new Config(_work)).GetReviews(project, task));
    }

    [Fact]
    public void Missing_logs_folder_gives_empty()
    {
        Directory.Delete(_logs, true);
        Assert.Empty(new AiReviewService(new Config(_work)).GetReviews("AITeam", "V4-08"));
    }
}
