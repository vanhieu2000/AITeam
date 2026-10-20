using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class TeamConfigService : ITeamConfigService
{
    private readonly HubPaths _paths;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TeamConfigService(HubPaths paths)
    {
        _paths = paths;
    }

    public TeamSettings GetSettings()
    {
        JsonObject? teamJson = null;
        if (File.Exists(_paths.TeamJson))
        {
            try
            {
                teamJson = JsonNode.Parse(File.ReadAllText(_paths.TeamJson)) as JsonObject;
            }
            catch
            {
                // Sử dụng mặc định nếu lỗi parse
            }
        }

        JsonObject? localJson = null;
        if (File.Exists(_paths.TeamLocalJson))
        {
            try
            {
                localJson = JsonNode.Parse(File.ReadAllText(_paths.TeamLocalJson)) as JsonObject;
            }
            catch
            {
                // Bỏ qua nếu lỗi
            }
        }

        string projectsRoot = localJson?["projectsRoot"]?.ToString()
            ?? teamJson?["projectsRoot"]?.ToString()
            ?? @"D:\";

        string workRoot = localJson?["workRoot"]?.ToString()
            ?? teamJson?["workRoot"]?.ToString()
            ?? @"D:\AITeam-work";

        string agyExe = localJson?["agyExe"]?.ToString()
            ?? teamJson?["agy"]?["exe"]?.ToString()
            ?? @"C:\Tools\agy\bin\agy.exe";

        string defaultModel = teamJson?["agy"]?["model"]?.ToString() ?? "";

        var agyAccounts = new List<string>();
        if (teamJson?["agy"]?["accounts"] is JsonArray accArr)
        {
            foreach (var item in accArr)
            {
                if (item != null) agyAccounts.Add(item.ToString());
            }
        }

        string gitHubOwner = teamJson?["github"]?["owner"]?.ToString() ?? "";

        int maxReviewRounds = 3;
        if (teamJson?["review"]?["maxRounds"] is JsonValue val && val.TryGetValue<int>(out var mr))
        {
            maxReviewRounds = mr;
        }

        var developers = new List<DeveloperInfo>();
        if (teamJson?["developers"] is JsonArray devArr)
        {
            foreach (var dev in devArr)
            {
                if (dev is JsonObject devObj)
                {
                    string id = devObj["id"]?.ToString() ?? "";
                    string name = devObj["name"]?.ToString() ?? "";
                    string focus = devObj["focus"]?.ToString() ?? "";
                    developers.Add(new DeveloperInfo(id, name, focus));
                }
            }
        }

        return new TeamSettings(
            defaultModel,
            maxReviewRounds,
            developers,
            projectsRoot,
            workRoot,
            gitHubOwner,
            agyAccounts,
            agyExe
        );
    }

    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        var results = new List<ProjectInfo>();
        if (!File.Exists(_paths.ProjectsJson))
        {
            return results;
        }

        JsonArray? array = null;
        try
        {
            array = JsonNode.Parse(File.ReadAllText(_paths.ProjectsJson)) as JsonArray;
        }
        catch
        {
            return results;
        }

        if (array == null) return results;

        var settings = GetSettings();

        // Đọc paths trong team.local.json nếu có
        var localPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(_paths.TeamLocalJson))
        {
            try
            {
                var localNode = JsonNode.Parse(File.ReadAllText(_paths.TeamLocalJson)) as JsonObject;
                if (localNode?["paths"] is JsonObject pathsObj)
                {
                    foreach (var kv in pathsObj)
                    {
                        if (kv.Value != null)
                        {
                            localPaths[kv.Key] = kv.Value.ToString();
                        }
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu lỗi
            }
        }

        foreach (var item in array)
        {
            if (item is not JsonObject obj) continue;

            string name = obj["name"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            string repo = obj["repo"]?.ToString() ?? "";
            string? declaredPath = obj["path"]?.ToString();
            string? declaredWorkDir = obj["workDir"]?.ToString();
            string stack = obj["stack"]?.ToString() ?? "";
            string build = obj["build"]?.ToString() ?? "";
            string test = obj["test"]?.ToString() ?? "";

            var areas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (obj["areas"] is JsonObject areasObj)
            {
                foreach (var area in areasObj)
                {
                    if (area.Value != null)
                    {
                        areas[area.Key] = area.Value.ToString();
                    }
                }
            }

            // Quy tắc Resolve-Project trong team.ps1:
            // Ưu tiên paths.<tên>; nếu không tồn tại thì dùng path trong projects.json; vẫn không tồn tại thì dùng projectsRoot\<tên>
            string resolvedPath;
            if (localPaths.TryGetValue(name, out var customPath) && Directory.Exists(customPath))
            {
                resolvedPath = customPath;
            }
            else if (!string.IsNullOrWhiteSpace(declaredPath) && Directory.Exists(declaredPath))
            {
                resolvedPath = declaredPath;
            }
            else if (localPaths.TryGetValue(name, out var nonExistentCustom) && !string.IsNullOrWhiteSpace(nonExistentCustom))
            {
                resolvedPath = nonExistentCustom;
            }
            else if (!string.IsNullOrWhiteSpace(declaredPath))
            {
                resolvedPath = declaredPath;
            }
            else
            {
                resolvedPath = Path.Combine(settings.ProjectsRoot, name);
            }

            string resolvedWorkDir = !string.IsNullOrWhiteSpace(declaredWorkDir)
                ? declaredWorkDir
                : Path.Combine(settings.WorkRoot, name);

            bool existsLocally = Directory.Exists(resolvedPath);

            results.Add(new ProjectInfo(
                name,
                repo,
                resolvedPath,
                resolvedWorkDir,
                stack,
                build,
                test,
                areas,
                existsLocally
            ));
        }

        return results;
    }

    public ProjectInfo? FindProject(string name)
    {
        return GetProjects().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveSettings(TeamSettings settings)
    {
        // 1. Cập nhật team.json (giữ nguyên các khóa khác kể cả _ghichu; KHÔNG ghi projectsRoot / workRoot vào đây)
        JsonObject teamNode;
        if (File.Exists(_paths.TeamJson))
        {
            teamNode = JsonNode.Parse(File.ReadAllText(_paths.TeamJson)) as JsonObject ?? new JsonObject();
        }
        else
        {
            teamNode = new JsonObject();
        }

        if (teamNode["github"] is not JsonObject githubObj)
        {
            githubObj = new JsonObject();
            teamNode["github"] = githubObj;
        }
        githubObj["owner"] = settings.GitHubOwner;

        if (teamNode["agy"] is not JsonObject agyObj)
        {
            agyObj = new JsonObject();
            teamNode["agy"] = agyObj;
        }
        agyObj["model"] = settings.DefaultModel;

        var accArr = new JsonArray();
        foreach (var acc in settings.AgyAccounts)
        {
            accArr.Add(acc);
        }
        agyObj["accounts"] = accArr;

        var devArr = new JsonArray();
        foreach (var dev in settings.Developers)
        {
            devArr.Add(new JsonObject
            {
                ["id"] = dev.Id,
                ["name"] = dev.Name,
                ["focus"] = dev.Focus
            });
        }
        teamNode["developers"] = devArr;

        if (teamNode["review"] is not JsonObject reviewObj)
        {
            reviewObj = new JsonObject();
            teamNode["review"] = reviewObj;
        }
        reviewObj["maxRounds"] = settings.MaxReviewRounds;

        WriteWithBackup(_paths.TeamJson, teamNode);

        // 2. Cập nhật team.local.json (chứa đường dẫn riêng: projectsRoot, workRoot, agyExe)
        JsonObject localNode;
        if (File.Exists(_paths.TeamLocalJson))
        {
            localNode = JsonNode.Parse(File.ReadAllText(_paths.TeamLocalJson)) as JsonObject ?? new JsonObject();
        }
        else
        {
            localNode = new JsonObject();
        }

        localNode["projectsRoot"] = settings.ProjectsRoot;
        localNode["workRoot"] = settings.WorkRoot;
        if (!string.IsNullOrWhiteSpace(settings.AgyExe))
        {
            localNode["agyExe"] = settings.AgyExe;
        }

        WriteWithBackup(_paths.TeamLocalJson, localNode);
    }

    public void SaveProject(ProjectInfo project)
    {
        JsonArray array;
        if (File.Exists(_paths.ProjectsJson))
        {
            array = JsonNode.Parse(File.ReadAllText(_paths.ProjectsJson)) as JsonArray ?? new JsonArray();
        }
        else
        {
            array = new JsonArray();
        }

        JsonObject? existing = null;
        foreach (var item in array)
        {
            if (item is JsonObject obj && string.Equals(obj["name"]?.ToString(), project.Name, StringComparison.OrdinalIgnoreCase))
            {
                existing = obj;
                break;
            }
        }

        if (existing == null)
        {
            existing = new JsonObject();
            existing["name"] = project.Name;
            array.Add(existing);
        }

        existing["repo"] = project.Repo;
        existing["path"] = project.Path;
        existing["workDir"] = project.WorkDir;
        existing["stack"] = project.Stack;
        existing["build"] = project.Build;
        existing["test"] = project.Test;

        var areasObj = new JsonObject();
        foreach (var kv in project.Areas)
        {
            areasObj[kv.Key] = kv.Value;
        }
        existing["areas"] = areasObj;

        WriteWithBackup(_paths.ProjectsJson, array);
    }

    private static void WriteWithBackup(string path, JsonNode node)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", overwrite: true);
        }

        string json = node.ToJsonString(JsonOptions);
        File.WriteAllText(path, json, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
