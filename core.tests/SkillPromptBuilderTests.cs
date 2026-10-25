using System.Text.Json;
using AITeam.Core.Skills;
using Xunit;

namespace AITeam.Core.Tests;

public sealed class SkillPromptBuilderTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "s105-edge-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(_tmp, "root");
    private string Work1 => Path.Combine(_tmp, "work1");
    private string Repo1 => Path.Combine(_tmp, "repo1");
    private string Work2 => Path.Combine(_tmp, "work2");
    private string Repo2 => Path.Combine(_tmp, "repo2");
    private string TaskFile1 => Path.Combine(Repo1, "tasks", "T1.md");
    private string TaskFile2 => Path.Combine(Repo2, "tasks", "T2.md");

    public SkillPromptBuilderTests()
    {
        foreach (var dir in new[] { Root, Work1, Repo1, Work2, Repo2, Path.GetDirectoryName(TaskFile1)!, Path.GetDirectoryName(TaskFile2)! })
            Directory.CreateDirectory(dir);

        foreach (var id in new[] { "09-qc", "code-test", "code-winforms", "code-app" })
        {
            var dir = Path.Combine(Root, "skills", id);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "SKILL.md"), "skill content");
        }

        File.WriteAllText(TaskFile1, "# Task 1");
        File.WriteAllText(TaskFile2, "# Task 2");
    }

    [Fact]
    public void HaiDuAnTrungTen_KhacMucCauHinh_BoStack()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), JsonSerializer.Serialize(new
        {
            projects = new[]
            {
                new { name = "demo", path = Repo1, workDir = Work1, stack = "WinForms" },
                new { name = "demo", path = Repo2, workDir = Work2, stack = "MAUI" }
            }
        }));

        // workDir khớp mục 1 (WinForms), taskFile khớp mục 2 (MAUI)
        var result = SkillPromptBuilder.BuildReviewInstructions(Root, Work1, TaskFile2);

        Assert.Contains("09-qc", result);
        Assert.Contains("code-test", result);
        Assert.DoesNotContain("code-winforms", result);
        Assert.DoesNotContain("code-app", result);
    }

    [Fact]
    public void HaiDuAnCungThieuTen_KhacMucCauHinh_BoStack()
    {
        File.WriteAllText(Path.Combine(Root, "projects.json"), JsonSerializer.Serialize(new
        {
            projects = new[]
            {
                new { name = (string?)null, path = Repo1, workDir = Work1, stack = "WinForms" },
                new { name = (string?)null, path = Repo2, workDir = Work2, stack = "MAUI" }
            }
        }));

        // workDir khớp mục 1 (WinForms), taskFile khớp mục 2 (MAUI)
        var result = SkillPromptBuilder.BuildReviewInstructions(Root, Work1, TaskFile2);

        Assert.Contains("09-qc", result);
        Assert.Contains("code-test", result);
        Assert.DoesNotContain("code-winforms", result);
        Assert.DoesNotContain("code-app", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmp))
            Directory.Delete(_tmp, true);
    }
}
