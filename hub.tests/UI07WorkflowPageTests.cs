using AITeam.Core.AiServices;
using AITeam.Core.Lessons;
using AITeam.Core.Workflow;
using AITeam.Hub.Pages;
using AITeam.Hub.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace AITeam.Hub.Tests;

// R-014 UI-07 (Claude): Quy trình & vai trò — sửa bước, lưu có kiểm hợp lệ và chống ghi đè.
public sealed class UI07WorkflowPageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-ui07-" + Guid.NewGuid().ToString("N"));
    private readonly WorkflowConfigService _workflow;
    private readonly LessonService _lessons;
    private readonly SqliteAiServiceRegistry _registry;

    public UI07WorkflowPageTests()
    {
        Directory.CreateDirectory(_root);
        _workflow = new WorkflowConfigService(_root);
        // Cấu hình mặc định tham chiếu skill trong skills/ ⇒ tạo thư mục để lưu hợp lệ.
        foreach (var skill in _workflow.Load().TaskTypes.SelectMany(t => t.Steps).SelectMany(s => s.Skills).Distinct())
            Directory.CreateDirectory(Path.Combine(_root, "skills", skill));
        _lessons = new LessonService(_root);
        _registry = new SqliteAiServiceRegistry(":memory:", _root);
    }

    public void Dispose()
    {
        _registry.Dispose();
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private sealed class MemTempData : ITempDataProvider
    {
        private IDictionary<string, object> _d = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _d;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _d = values;
    }

    private WorkflowModel Page() => new(_workflow, _lessons, _registry)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemTempData()),
    };

    private static List<WorkflowStepInput> Inputs(TaskType t) => t.Steps.Select(s => new WorkflowStepInput
    {
        Code = s.Code, Name = s.Name, Owner = s.Owner, Fallback = s.Fallback,
        Skills = string.Join(", ", s.Skills), Rules = string.Join(", ", s.Rules),
        MaxMinutes = s.MaxMinutes, MaxRounds = s.MaxRounds, OwnerApproval = s.OwnerApproval,
    }).ToList();

    [Fact]
    public void Get_defaults_to_first_type_and_lists_fixed_actors()
    {
        var p = Page();
        p.OnGet();
        Assert.NotNull(p.Current);
        Assert.Equal(p.Config!.TaskTypes[0].Id, p.TypeId);
        Assert.Contains(p.Actors, a => a.Id == "claude");
        Assert.Contains(p.Actors, a => a.Id == "owner");
        Assert.Contains(p.Actors, a => a.Id == "gate");

        var q = Page();
        q.TypeId = "<khong-co>";
        q.OnGet();
        Assert.Equal(p.TypeId, q.TypeId);
    }

    [Fact]
    public void Save_reorders_renames_and_persists()
    {
        var p = Page();
        p.OnGet();
        var type = p.Current!;
        var steps = Inputs(type);
        steps.Reverse();
        steps[0].Name = "Bước đầu mới";
        steps[0].MaxMinutes = 20;

        var result = Page().OnPostSave(type.Id, p.Config!.Revision, type.Name + " 2", "Q, Z", steps);
        Assert.IsType<RedirectToPageResult>(result);

        var saved = _workflow.Load().TaskTypes.Single(t => t.Id == type.Id);
        Assert.Equal(type.Name + " 2", saved.Name);
        Assert.Equal(["Q", "Z"], saved.CodePrefixes);
        Assert.Equal("Bước đầu mới", saved.Steps[0].Name);
        Assert.Equal(20, saved.Steps[0].MaxMinutes);
        Assert.Equal(type.Steps[^1].Code, saved.Steps[0].Code);
    }

    [Fact]
    public void Invalid_save_keeps_edits_and_lists_errors()
    {
        var p = Page();
        p.OnGet();
        var type = p.Current!;
        var steps = Inputs(type);
        steps[0].Name = "";
        steps[0].MaxRounds = 99;
        steps.Add(new WorkflowStepInput { Code = steps[1].Code, Name = "Trùng", Owner = "claude" });

        var page = Page();
        Assert.IsType<PageResult>(page.OnPostSave(type.Id, p.Config!.Revision, type.Name, string.Join(",", type.CodePrefixes), steps));
        Assert.Contains(page.SaveErrors, e => e.Contains("tên không được rỗng"));
        Assert.Contains(page.SaveErrors, e => e.Contains("vòng"));
        Assert.Contains(page.SaveErrors, e => e.Contains("trùng"));
        Assert.Equal(steps.Count, page.Current!.Steps.Count);   // giữ bản đang sửa
        Assert.False(File.Exists(Path.Combine(_root, "config", "workflow.json")));
    }

    [Fact]
    public void Stale_revision_is_refused()
    {
        var p = Page();
        p.OnGet();
        var type = p.Current!;
        Assert.IsType<RedirectToPageResult>(Page().OnPostSave(type.Id, p.Config!.Revision, type.Name, string.Join(",", type.CodePrefixes), Inputs(type)));

        var stale = Page();
        Assert.IsType<PageResult>(stale.OnPostSave(type.Id, p.Config.Revision, "Đổi tên", string.Join(",", type.CodePrefixes), Inputs(type)));
        Assert.Contains("vừa bị thay đổi", Assert.Single(stale.SaveErrors));
        Assert.Equal(type.Name, _workflow.Load().TaskTypes.Single(t => t.Id == type.Id).Name);
    }

    [Theory]
    [InlineData("a, b;c  d", new[] { "a", "b", "c", "d" })]
    [InlineData("a,a", new[] { "a" })]
    [InlineData(null, new string[0])]
    public void Split_lists(string? input, string[] expected) => Assert.Equal(expected, WorkflowModel.Split(input));

    [Fact]
    public async Task Page_renders_with_named_inputs_and_antiforgery()
    {
        using var env = new TestEnvironment();
        await using var host = await HubTestHost.StartAsync(env, s =>
        {
            s.AddSingleton<IWorkflowConfigService>(_workflow);
            s.AddSingleton<ILessonService>(_lessons);
        });
        var html = await host.Client.GetStringAsync("/Workflow");
        Assert.Contains("name=\"steps[0].Code\"", html);
        Assert.Contains("name=\"steps[0].OwnerApproval\"", html);
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("handler=Save", html);
        Assert.Contains("wf-node", html);
    }
}
