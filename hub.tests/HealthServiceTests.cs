using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class HealthServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void InstallClaudeIntegration_creates_skill_and_appends_block_without_duplication()
    {
        // Tạo template skill trong AITeam/skill/SKILL.md
        string skillTemplateDir = Path.Combine(_env.AITeamRoot, "skill");
        Directory.CreateDirectory(skillTemplateDir);
        File.WriteAllText(_env.Paths.SkillTemplate, "AITeam root path: {{AITEAM}}");

        var configService = new TeamConfigService(_env.Paths);
        var healthService = new HealthService(_env.Paths, configService);

        // Chạy lần 1
        healthService.InstallClaudeIntegration();

        string skillFile = Path.Combine(_env.UserProfile, ".claude", "skills", "aiteam", "SKILL.md");
        Assert.True(File.Exists(skillFile));
        string skillContent = File.ReadAllText(skillFile);
        Assert.Equal($"AITeam root path: {_env.AITeamRoot}", skillContent);

        string claudeMd = Path.Combine(_env.Paths.ClaudeDir, "CLAUDE.md");
        Assert.True(File.Exists(claudeMd));
        string claudeContent = File.ReadAllText(claudeMd);
        Assert.Contains("AITeam: quy trình team", claudeContent);
        Assert.Contains(_env.AITeamRoot, claudeContent);

        // Chạy lần 2: không được thêm trùng khối
        healthService.InstallClaudeIntegration();
        string claudeContent2 = File.ReadAllText(claudeMd);
        int occurrences = (claudeContent2.Length - claudeContent2.Replace("AITeam: quy trình team", "").Length) / "AITeam: quy trình team".Length;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Check_returns_health_items_for_system_and_projects()
    {
        string teamJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json");
        File.Copy(teamJsonFixture, _env.Paths.TeamJson);

        string projectsJsonFixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json");
        File.Copy(projectsJsonFixture, _env.Paths.ProjectsJson);

        var configService = new TeamConfigService(_env.Paths);
        var healthService = new HealthService(_env.Paths, configService);

        var items = healthService.Check();
        Assert.NotEmpty(items);

        // Kiểm tra các mục cốt lõi
        Assert.Contains(items, i => i.Name == "git");
        Assert.Contains(items, i => i.Name.Contains("GitHub"));
        Assert.Contains(items, i => i.Name.Contains("Antigravity"));
        Assert.Contains(items, i => i.Name.Contains("Claude"));
    }
}
