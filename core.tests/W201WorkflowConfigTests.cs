using AITeam.Core.Workflow;

namespace AITeam.Core.Tests;

public sealed class W201WorkflowConfigTests : IDisposable
{
    private static readonly string[] FakeSkills =
    [
        "01-tiep-nhan", "03-research", "04-ke-hoach", "06-chuan-bi-task", "07-code",
        "08-kiem-thu", "09-qc", "10-nghiem-thu", "code-test", "code-web"
    ];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-w201-" + Guid.NewGuid().ToString("N"));
    private readonly WorkflowConfigService _service;

    public W201WorkflowConfigTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "skills"));
        foreach (var skill in FakeSkills)
            Directory.CreateDirectory(Path.Combine(_root, "skills", skill));
        _service = new WorkflowConfigService(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) return dir.FullName;
        throw new InvalidOperationException("Không tìm thấy thư mục repo (AITeam.sln).");
    }

    private static WorkflowConfig ReplaceStep(WorkflowConfig config, string typeId, int index, Func<WorkflowStep, WorkflowStep> change) =>
        config with
        {
            TaskTypes = config.TaskTypes
                .Select(type => type.Id != typeId
                    ? type
                    : type with
                    {
                        Steps = type.Steps.Select((step, i) => i == index ? change(step) : step).ToArray()
                    })
                .ToArray()
        };

    private static WorkflowConfig ReplaceType(WorkflowConfig config, string typeId, Func<TaskType, TaskType> change) =>
        config with
        {
            TaskTypes = config.TaskTypes.Select(type => type.Id == typeId ? change(type) : type).ToArray()
        };

    private static string ErrorsOf(WorkflowConfigService service, WorkflowConfig config)
    {
        var validation = service.Validate(config);
        Assert.False(validation.Ok);
        Assert.NotEmpty(validation.Errors);
        return string.Join(" ", validation.Errors);
    }

    private static void SameConfig(WorkflowConfig expected, WorkflowConfig actual)
    {
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.TaskTypes.Count, actual.TaskTypes.Count);
        for (int i = 0; i < expected.TaskTypes.Count; i++)
        {
            var left = expected.TaskTypes[i];
            var right = actual.TaskTypes[i];
            Assert.Equal(left.Id, right.Id);
            Assert.Equal(left.Name, right.Name);
            Assert.Equal(left.CodePrefixes, right.CodePrefixes);
            Assert.Equal(left.Steps.Count, right.Steps.Count);
            for (int j = 0; j < left.Steps.Count; j++)
            {
                var a = left.Steps[j];
                var b = right.Steps[j];
                Assert.Equal(a.Code, b.Code);
                Assert.Equal(a.Name, b.Name);
                Assert.Equal(a.Owner, b.Owner);
                Assert.Equal(a.Fallback, b.Fallback);
                Assert.Equal(a.Skills, b.Skills);
                Assert.Equal(a.Rules, b.Rules);
                Assert.Equal(a.MaxMinutes, b.MaxMinutes);
                Assert.Equal(a.MaxRounds, b.MaxRounds);
                Assert.Equal(a.OwnerApproval, b.OwnerApproval);
            }
        }
    }

    [Fact]
    public void Load_without_file_returns_six_defaults_and_validates()
    {
        var config = _service.Load();

        Assert.Equal(new[] { "giao-dien", "tinh-nang-loi", "sua-loi", "tai-lieu", "nghien-cuu", "bao-mat" },
            config.TaskTypes.Select(t => t.Id));
        Assert.Equal("", config.Revision);
        var validation = _service.Validate(config);
        Assert.True(validation.Ok, string.Join("; ", validation.Errors));
        Assert.False(File.Exists(Path.Combine(_root, "config", "workflow.json")));
    }

    [Fact]
    public void Repo_config_file_is_utf8_bom_and_valid()
    {
        string repo = RepoRoot();
        string file = Path.Combine(repo, "config", "workflow.json");
        Assert.True(File.Exists(file), "Thiếu config/workflow.json trong repo.");

        byte[] bytes = File.ReadAllBytes(file);
        Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "config/workflow.json phải là UTF-8 có BOM.");

        var service = new WorkflowConfigService(repo);
        var config = service.Load();
        Assert.False(string.IsNullOrEmpty(config.Revision));
        Assert.Equal(6, config.TaskTypes.Count);
        var validation = service.Validate(config);
        Assert.True(validation.Ok, string.Join("; ", validation.Errors));
    }

    [Fact]
    public void Save_then_load_roundtrips_and_rejects_stale_revision()
    {
        var original = _service.Load();
        var saved = _service.Save(original, "");

        Assert.False(string.IsNullOrEmpty(saved.Revision));
        var loaded = _service.Load();
        Assert.Equal(saved.Revision, loaded.Revision);
        SameConfig(original, loaded);

        var stale = Assert.Throws<InvalidOperationException>(() => _service.Save(original, ""));
        Assert.Contains("Cấu hình đã bị sửa ở nơi khác", stale.Message);
    }

    [Fact]
    public void Save_rejects_invalid_config_with_argument_exception()
    {
        var bad = ReplaceStep(_service.Load(), "tinh-nang-loi", 3, step => step with { MaxRounds = 9 });
        Assert.Throws<ArgumentException>(() => _service.Save(bad, ""));
    }

    [Fact]
    public void Validate_rejects_duplicate_step_code()
    {
        var config = ReplaceStep(_service.Load(), "tinh-nang-loi", 1, step => step with { Code = "tiep_nhan" });
        Assert.Contains("trùng", ErrorsOf(_service, config));
    }

    [Fact]
    public void Validate_rejects_unknown_skill()
    {
        var config = ReplaceStep(_service.Load(), "tinh-nang-loi", 3, step => step with { Skills = ["khong-co-skill"] });
        Assert.Contains("không có trong skills/", ErrorsOf(_service, config));
    }

    [Fact]
    public void Validate_rejects_skill_path_traversal()
    {
        var config = ReplaceStep(_service.Load(), "tinh-nang-loi", 3, step => step with { Skills = ["../x"] });
        Assert.Contains("../x", ErrorsOf(_service, config));
    }

    [Fact]
    public void Validate_rejects_fallback_equal_owner()
    {
        var config = ReplaceStep(_service.Load(), "tinh-nang-loi", 3, step => step with { Fallback = step.Owner });
        Assert.Contains("trùng người phụ trách", ErrorsOf(_service, config));
    }

    [Fact]
    public void Validate_rejects_max_rounds_out_of_range()
    {
        var config = ReplaceStep(_service.Load(), "tinh-nang-loi", 3, step => step with { MaxRounds = 9 });
        Assert.Contains("1–5", ErrorsOf(_service, config));
    }

    [Fact]
    public void Validate_rejects_prefix_used_by_two_types()
    {
        var config = ReplaceType(_service.Load(), "sua-loi", type => type with { CodePrefixes = ["H"] });
        Assert.Contains("Tiền tố \"H\" bị trùng", ErrorsOf(_service, config));
    }

    [Fact]
    public void Resolve_prefers_task_type_line()
    {
        var resolved = _service.Resolve("X-1", "# Phiếu\n- **Loại việc:** Bảo mật\n- **Ngân sách:** 100");
        Assert.Equal("bao-mat", resolved.Id);
    }

    [Fact]
    public void Resolve_falls_back_to_code_prefix()
    {
        Assert.Equal("giao-dien", _service.Resolve("H9-03", null).Id);
        Assert.Equal("tinh-nang-loi", _service.Resolve("V914-routing", "").Id);
        Assert.Equal("tai-lieu", _service.Resolve("D-1", "không có dòng loại việc").Id);
    }

    [Fact]
    public void Resolve_unknown_uses_tinh_nang_loi_steps()
    {
        var resolved = _service.Resolve("ZZ-1", null);
        var core = _service.Load().TaskTypes.First(type => type.Id == "tinh-nang-loi");

        Assert.Equal("chua-phan-loai", resolved.Id);
        Assert.Equal(core.Steps.Select(step => step.Code), resolved.Steps.Select(step => step.Code));
    }

    [Fact]
    public void Load_broken_json_throws_invalid_data()
    {
        Directory.CreateDirectory(Path.Combine(_root, "config"));
        File.WriteAllText(Path.Combine(_root, "config", "workflow.json"), "{ \"version\": 1, \"taskTypes\": [");

        var ex = Assert.Throws<InvalidDataException>(() => new WorkflowConfigService(_root).Load());
        Assert.Contains("hỏng", ex.Message);
    }
}
