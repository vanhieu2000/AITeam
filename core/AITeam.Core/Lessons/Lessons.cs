namespace AITeam.Core.Lessons;

// W2-02 (R-014): luật chuẩn rules/L-xxx.md + bài học lessons/LS-xxx.md. KHUNG do Claude viết — giữ nguyên chữ ký,
// agent điền phần thân (TODO(agent)). Thiết kế: tasks/W2-02-luat-chuan-va-bai-hoc.md, docs/design/R-014-van-phong-ai.md mục 4.4, 4.5.

public enum LessonStatus { Draft, Promoted, Dropped }

public enum RuleStatus { Active, Retired }

public sealed record Lesson(
    string Id, string Title, string Body, string Kind,
    string? RequestId, string? TaskId, string? Step, string? Ai,
    LessonStatus Status, string? RuleId, DateTimeOffset CreatedAt);

public sealed record Rule(
    string Id, string Title, string Body,
    IReadOnlyList<string> TaskTypes, IReadOnlyList<string> Steps, IReadOnlyList<string> Ais,
    string? SourceLesson, RuleStatus Status, DateTimeOffset CreatedAt);

public interface ILessonService
{
    IReadOnlyList<Lesson> Lessons(LessonStatus? status = null);
    IReadOnlyList<Rule> Rules(bool activeOnly = false);
    Lesson AddDraft(string title, string body, string kind, string? requestId, string? taskId, string? step, string? ai);
    Rule Promote(string lessonId, string title, string body, IReadOnlyList<string> taskTypes, IReadOnlyList<string> steps, IReadOnlyList<string> ais);
    void Drop(string lessonId);
    void Retire(string ruleId);
    IReadOnlyList<Rule> ForTask(string taskType, string step, string? ai);
    string PromptSection(string taskType, string step, string? ai);
}

public sealed class LessonService : ILessonService
{
    public LessonService(string aiTeamRoot) => Root = aiTeamRoot;

    public string Root { get; }

    public IReadOnlyList<Lesson> Lessons(LessonStatus? status = null) => throw new NotImplementedException("TODO(agent) W2-02");
    public IReadOnlyList<Rule> Rules(bool activeOnly = false) => throw new NotImplementedException("TODO(agent) W2-02");
    public Lesson AddDraft(string title, string body, string kind, string? requestId, string? taskId, string? step, string? ai) => throw new NotImplementedException("TODO(agent) W2-02");
    public Rule Promote(string lessonId, string title, string body, IReadOnlyList<string> taskTypes, IReadOnlyList<string> steps, IReadOnlyList<string> ais) => throw new NotImplementedException("TODO(agent) W2-02");
    public void Drop(string lessonId) => throw new NotImplementedException("TODO(agent) W2-02");
    public void Retire(string ruleId) => throw new NotImplementedException("TODO(agent) W2-02");
    public IReadOnlyList<Rule> ForTask(string taskType, string step, string? ai) => throw new NotImplementedException("TODO(agent) W2-02");
    public string PromptSection(string taskType, string step, string? ai) => throw new NotImplementedException("TODO(agent) W2-02");
}
