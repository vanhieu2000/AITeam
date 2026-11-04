using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

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

    public IReadOnlyList<ProjectProfile> List()
    {
        lock (TeamFileLock.GetLock(_paths.ProjectsJson))
        lock (TeamFileLock.GetLock(_paths.TeamLocalJson))
        {
            var array = ReadProjects();
            ReadLocal(); // Không che giấu cấu hình hỏng bằng giá trị mặc định.
            var resolved = _config.GetProjects();
            return array.OfType<JsonObject>().Where(p => !string.IsNullOrWhiteSpace(Text(p, "name")))
                .Select(p =>
                {
                    string name = Text(p, "name");
                    var info = resolved.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                    var areas = new Dictionary<string, string>();
                    if (p["areas"] is JsonObject obj)
                        foreach (var kv in obj) areas[kv.Key] = kv.Value?.ToString() ?? "";
                    var status = Text(p, "status") switch
                    {
                        "paused" => ProjectStatus.Paused,
                        "archived" => ProjectStatus.Archived,
                        _ => ProjectStatus.Active
                    };
                    return new ProjectProfile(name, Text(p, "description"), status, Text(p, "branch", "main"),
                        Text(p, "repo"), info?.Path ?? "", info?.ExistsLocally ?? false, Text(p, "stack"),
                        Text(p, "build"), Text(p, "test"), Text(p, "try"), Text(p, "defaultAi"), areas);
                }).ToList();
        }
    }

    public ProjectProfile? Get(string name)
        => List().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public ProjectResult Update(string name, ProjectEdit edit)
    {
        if (!ValidName(name) || edit is null || !Line(edit.Description, 200) ||
            !Line(edit.Branch, 100) || !Match(edit.Branch, @"^[A-Za-z0-9._/-]{1,100}$") ||
            !Line(edit.Stack, 300) || !Line(edit.Build, 300) || !Line(edit.Test, 300) || !Line(edit.Try, 300) ||
            !new[] { "", "codex", "opencode", "antigravity", "claude" }.Contains(edit.DefaultAi) ||
            edit.Areas is null || edit.Areas.Any(kv => !Match(kv.Key, "^[A-Z]$") || !Line(kv.Value, 300)) ||
            edit.LocalPath is null || (edit.LocalPath.Length > 0 && !ValidPath(edit.LocalPath)))
            return new(false, "Thông tin dự án không hợp lệ.");
        return Change(name, p =>
        {
            p["description"] = edit.Description;
            p["branch"] = edit.Branch;
            p["stack"] = edit.Stack;
            p["build"] = edit.Build;
            p["test"] = edit.Test;
            p["try"] = edit.Try;
            p["defaultAi"] = edit.DefaultAi;
            var areas = new JsonObject();
            foreach (var kv in edit.Areas) areas[kv.Key] = kv.Value;
            p["areas"] = areas;
        }, edit.LocalPath);
    }

    public ProjectResult SetStatus(string name, ProjectStatus status)
    {
        if (!ValidName(name) || !Enum.IsDefined(status)) return new(false, "Trạng thái dự án không hợp lệ.");
        return Change(name, p => p["status"] = status.ToString().ToLowerInvariant(), "");
    }

    public ProjectResult Add(ProjectAdd add)
    {
        if (add is null || !ValidName(add.Name) || !Enum.IsDefined(add.Mode) ||
            (add.Repo is not null && !Match(add.Repo, @"^[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$")) ||
            (add.LocalPath is not null && !ValidPath(add.LocalPath)) ||
            (add.Mode == ProjectAddMode.GitHub && add.Repo is null) ||
            (add.Mode == ProjectAddMode.Folder && add.LocalPath is null))
            return new(false, "Thông tin thêm dự án không hợp lệ.");
        try
        {
            lock (TeamFileLock.GetLock(_paths.ProjectsJson))
            lock (TeamFileLock.GetLock(_paths.TeamLocalJson))
            {
                if (Find(ReadProjects(), add.Name) is not null) return new(false, "Dự án đã tồn tại.");
                ReadLocal();
                CheckFile(_paths.ProjectsJson);
                CheckFile(_paths.TeamLocalJson);
                var args = new List<string> { "add-project", "-Project", add.Name };
                if (add.Mode == ProjectAddMode.New) args.Add("-New");
                else if (add.Mode == ProjectAddMode.GitHub) args.AddRange(["-Repo", add.Repo!]);
                else args.AddRange(["-Path", add.LocalPath!]);
                var result = _runner.Run(args, TimeSpan.FromMinutes(10));
                if (result.ExitCode == 0) return new(true, null);
                string last = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "Không có thông tin lỗi.";
                return new(false, "Thêm dự án thất bại: " + last[..Math.Min(last.Length, 300)]);
            }
        }
        catch (Exception) { return new(false, "Không thể thêm dự án."); }
    }

    private ProjectResult Change(string name, Action<JsonObject> change, string localPath)
    {
        try
        {
            lock (TeamFileLock.GetLock(_paths.ProjectsJson))
            lock (TeamFileLock.GetLock(_paths.TeamLocalJson))
            {
                var array = ReadProjects();
                var project = Find(array, name);
                if (project is null) return new(false, "Không tìm thấy dự án.");
                JsonObject? local = null;
                CheckFile(_paths.ProjectsJson);
                if (localPath.Length > 0)
                {
                    local = ReadLocal();
                    CheckFile(_paths.TeamLocalJson);
                    if (local["paths"] is not null && local["paths"] is not JsonObject)
                        throw new IOException("Cấu hình đường dẫn không hợp lệ.");
                    var paths = local["paths"] as JsonObject ?? new JsonObject();
                    if (local["paths"] is null) local["paths"] = paths;
                    string key = Text(project, "name");
                    string existing = paths.Select(kv => kv.Key).FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) ?? key;
                    paths[existing] = localPath;
                }
                change(project);
                TeamConfigService.WriteWithBackup(_paths.ProjectsJson, array);
                if (local is not null) TeamConfigService.WriteWithBackup(_paths.TeamLocalJson, local);
                return new(true, null);
            }
        }
        catch (Exception) { return new(false, "Không thể lưu thông tin dự án."); }
    }

    private JsonArray ReadProjects()
    {
        CheckFile(_paths.ProjectsJson);
        return File.Exists(_paths.ProjectsJson)
            ? JsonNode.Parse(File.ReadAllText(_paths.ProjectsJson)) as JsonArray ?? throw new IOException("Danh sách dự án không hợp lệ.")
            : new JsonArray();
    }

    private JsonObject ReadLocal()
    {
        CheckFile(_paths.TeamLocalJson);
        return File.Exists(_paths.TeamLocalJson)
            ? JsonNode.Parse(File.ReadAllText(_paths.TeamLocalJson)) as JsonObject ?? throw new IOException("Cấu hình máy không hợp lệ.")
            : new JsonObject();
    }

    private static void CheckFile(string path)
    {
        if (TeamFileLock.HasReparsePoint(path) || TeamFileLock.HasReparsePoint(path + ".bak"))
            throw new IOException("Không truy cập cấu hình qua liên kết.");
    }

    private static JsonObject? Find(JsonArray array, string name) => array.OfType<JsonObject>()
        .FirstOrDefault(p => string.Equals(Text(p, "name"), name, StringComparison.OrdinalIgnoreCase));
    private static string Text(JsonObject p, string key, string fallback = "") => p[key]?.ToString() ?? fallback;
    private static bool Match(string? value, string pattern) => value is not null && Regex.IsMatch(value, pattern + @"\z", RegexOptions.CultureInvariant);
    private static bool ValidName(string? value) => Match(value, "^[A-Za-z][A-Za-z0-9._-]{0,63}$");
    private static bool Line(string? value, int limit) => value is not null && value.Length <= limit && !value.Any(char.IsControl);
    private static bool ValidPath(string value) => Line(value, 240) && Path.IsPathFullyQualified(value) &&
        !value.Split(['/', '\\']).Contains("..");

}

/// <summary>Runner thật: powershell.exe -NoProfile -ExecutionPolicy Bypass -File &lt;AITeamRoot&gt;\team.ps1 …, ArgumentList, stdin rỗng.</summary>
public sealed class PowerShellProjectScriptRunner : IProjectScriptRunner
{
    private readonly HubPaths _paths;

    public PowerShellProjectScriptRunner(HubPaths paths) => _paths = paths;

    public (int ExitCode, string Output) Run(IReadOnlyList<string> args, TimeSpan timeout)
    {
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
            Path.Combine(Path.GetDirectoryName(_paths.ProjectsJson)!, "team.ps1") }.Concat(args))
            start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        process.Start();
        process.StandardInput.Close();
        var output = new StringBuilder();
        var gate = new object();
        async Task Drain(StreamReader reader)
        {
            var buffer = new char[4096];
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory())) > 0)
                lock (gate) output.Append(buffer, 0, count);
        }
        var drains = Task.WhenAll(Drain(process.StandardOutput), Drain(process.StandardError));
        using var cancellation = new CancellationTokenSource(timeout);
        bool timedOut = false;
        try { process.WaitForExitAsync(cancellation.Token).GetAwaiter().GetResult(); }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { /* Tiến trình vừa tự kết thúc. */ }
        }
        // Không chờ vô hạn nếu một tiến trình con còn giữ luồng output.
        try { drains.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); }
        catch (TimeoutException) { }
        lock (gate) return (timedOut ? 124 : process.ExitCode, output.ToString());
    }
}
