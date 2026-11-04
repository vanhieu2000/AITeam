using System.Text.Json.Nodes;

namespace AITeam.Hub.Services;

// F1-01 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-01.

public enum ProjectStatus { Active, Paused, Archived }

public enum ProjectAddMode { New, GitHub, Folder }

/// <summary>Thông tin một dự án: projects.json (chung 2 máy) + đường dẫn trên máy này (team.local.json / mặc định).</summary>
public sealed record ProjectProfile(string Name, string Description, ProjectStatus Status, string Branch, string Repo,
    string LocalPath, bool ExistsLocally, string Stack, string Build, string Test, string Try, string DefaultAi,
    IReadOnlyDictionary<string, string> Areas);

public sealed record ProjectEdit(string Description, string Branch, string Stack, string Build, string Test, string Try,
    string DefaultAi, IReadOnlyDictionary<string, string> Areas, string LocalPath);

public sealed record ProjectAdd(string Name, ProjectAddMode Mode, string? Repo, string? LocalPath);

public sealed record ProjectResult(bool Ok, string? Error);

/// <summary>Chạy team.ps1 với danh sách đối số (KHÔNG ghép chuỗi lệnh).</summary>
public interface IProjectScriptRunner
{
    (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout);
}

public interface IProjectAdminService
{
    IReadOnlyList<ProjectProfile> List();
    ProjectProfile? Get(string name);
    ProjectResult Update(string name, ProjectEdit edit);
    ProjectResult SetStatus(string name, ProjectStatus status);
    ProjectResult Add(ProjectAdd add);
}

public sealed class ProjectAdminService : IProjectAdminService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly IProjectScriptRunner _runner;

    public ProjectAdminService(HubPaths paths, ITeamConfigService config, IProjectScriptRunner runner)
    {
        _paths = paths;
        _config = config;
        _runner = runner;
    }

    public IReadOnlyList<ProjectProfile> List() => throw new NotImplementedException(); // TODO(agent)

    public ProjectProfile? Get(string name) => throw new NotImplementedException(); // TODO(agent)

    public ProjectResult Update(string name, ProjectEdit edit) => throw new NotImplementedException(); // TODO(agent)

    public ProjectResult SetStatus(string name, ProjectStatus status) => throw new NotImplementedException(); // TODO(agent)

    public ProjectResult Add(ProjectAdd add) => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>Runner thật: powershell.exe -NoProfile -ExecutionPolicy Bypass -File &lt;AITeamRoot&gt;\team.ps1 …, ArgumentList, stdin rỗng.</summary>
public sealed class PowerShellProjectScriptRunner : IProjectScriptRunner
{
    private readonly HubPaths _paths;

    public PowerShellProjectScriptRunner(HubPaths paths) => _paths = paths;

    public (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout)
        => throw new NotImplementedException(); // TODO(agent)
}
