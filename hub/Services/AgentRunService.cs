using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class AgentRunService : IAgentRunService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _configService;
    private static readonly Regex LogFileRegex = new(@"^(?<task>[A-Za-z0-9\-]+)-(?<attempt>\d+)\.json$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PromptMatchRegex = new(@"[\\/](?<task>[A-Za-z0-9\-]+)-\d+\.prompt\.md", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AgentRunService(HubPaths paths, ITeamConfigService configService)
    {
        _paths = paths;
        _configService = configService;
    }

    public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null)
    {
        var runs = new List<AgentRun>();
        var projectInfo = _configService.FindProject(project);
        if (projectInfo == null || string.IsNullOrWhiteSpace(projectInfo.WorkDir))
        {
            return runs;
        }

        string logsDir = Path.Combine(projectInfo.WorkDir, "logs");
        if (!Directory.Exists(logsDir))
        {
            return runs;
        }

        var files = Directory.GetFiles(logsDir, "*.json");
        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            if (fileName.StartsWith("ask-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = LogFileRegex.Match(fileName);
            if (!match.Success)
            {
                continue;
            }

            string taskName = match.Groups["task"].Value;
            if (!int.TryParse(match.Groups["attempt"].Value, out int attempt))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(taskId) &&
                !string.Equals(taskName, taskId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fi = new FileInfo(file);
            DateTime time = fi.LastWriteTime;

            if (fi.Length == 0)
            {
                runs.Add(new AgentRun(
                    projectInfo.Name,
                    taskName,
                    attempt,
                    RunState.Running,
                    null,
                    null,
                    null,
                    null,
                    null,
                    time,
                    file
                ));
                continue;
            }

            string content;
            try
            {
                using var reader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                content = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                runs.Add(new AgentRun(
                    projectInfo.Name,
                    taskName,
                    attempt,
                    RunState.Error,
                    "ERROR",
                    "Không thể đọc file: " + ex.Message,
                    null,
                    null,
                    null,
                    time,
                    file
                ));
                continue;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                runs.Add(new AgentRun(
                    projectInfo.Name,
                    taskName,
                    attempt,
                    RunState.Running,
                    null,
                    null,
                    null,
                    null,
                    null,
                    time,
                    file
                ));
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                string? status = root.TryGetProperty("status", out var sProp) && sProp.ValueKind == JsonValueKind.String
                    ? sProp.GetString()
                    : null;

                string? error = root.TryGetProperty("error", out var eProp) && eProp.ValueKind == JsonValueKind.String
                    ? eProp.GetString()
                    : null;

                string? response = root.TryGetProperty("response", out var rProp) && rProp.ValueKind == JsonValueKind.String
                    ? rProp.GetString()
                    : null;

                double? duration = null;
                if (root.TryGetProperty("duration_seconds", out var dProp) && dProp.TryGetDouble(out var dVal))
                {
                    duration = dVal;
                }

                long? tokens = null;
                if (root.TryGetProperty("usage", out var uProp) &&
                    uProp.ValueKind == JsonValueKind.Object &&
                    uProp.TryGetProperty("total_tokens", out var tProp) &&
                    tProp.TryGetInt64(out var tVal))
                {
                    tokens = tVal;
                }
                else if (root.TryGetProperty("total_tokens", out var ttProp) && ttProp.TryGetInt64(out var ttVal))
                {
                    tokens = ttVal;
                }

                RunState state = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase)
                    ? RunState.Success
                    : RunState.Error;

                runs.Add(new AgentRun(
                    projectInfo.Name,
                    taskName,
                    attempt,
                    state,
                    status,
                    error,
                    response,
                    duration,
                    tokens,
                    time,
                    file
                ));
            }
            catch (Exception ex)
            {
                runs.Add(new AgentRun(
                    projectInfo.Name,
                    taskName,
                    attempt,
                    RunState.Error,
                    "ERROR",
                    "Log hỏng: " + ex.Message,
                    null,
                    null,
                    null,
                    time,
                    file
                ));
            }
        }

        return runs.OrderByDescending(r => r.Time).ThenByDescending(r => r.Attempt).ToList();
    }

    public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20)
    {
        var allRuns = new List<AgentRun>();
        var projects = _configService.GetProjects();
        foreach (var p in projects)
        {
            allRuns.AddRange(GetRuns(p.Name));
        }

        return allRuns.OrderByDescending(r => r.Time)
                      .ThenByDescending(r => r.Attempt)
                      .Take(count)
                      .ToList();
    }

    public IReadOnlyList<RunningAgent> GetRunning()
    {
        var results = new List<RunningAgent>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CreationDate, CommandLine FROM Win32_Process WHERE Name = 'agy.exe'");
            using var collection = searcher.Get();

            var projects = _configService.GetProjects();

            foreach (var item in collection)
            {
                int pid = Convert.ToInt32(item["ProcessId"]);
                DateTime startedAt = DateTime.Now;
                if (item["CreationDate"] != null)
                {
                    try
                    {
                        startedAt = ManagementDateTimeConverter.ToDateTime(item["CreationDate"].ToString());
                    }
                    catch
                    {
                        // Giữ DateTime.Now
                    }
                }

                string commandLine = item["CommandLine"]?.ToString() ?? "";

                string? task = null;
                var match = PromptMatchRegex.Match(commandLine);
                if (match.Success)
                {
                    task = match.Groups["task"].Value;
                }

                string? project = null;
                foreach (var p in projects)
                {
                    if (!string.IsNullOrWhiteSpace(p.WorkDir) &&
                        commandLine.Contains(p.WorkDir, StringComparison.OrdinalIgnoreCase))
                    {
                        project = p.Name;
                        break;
                    }
                }

                if (project == null)
                {
                    var dirMatch = Regex.Match(commandLine, @"[\\/](?<work>[^\\/]+)[\\/]logs[\\/]", RegexOptions.IgnoreCase);
                    if (dirMatch.Success)
                    {
                        project = dirMatch.Groups["work"].Value;
                    }
                }

                results.Add(new RunningAgent(pid, startedAt, project, task, commandLine));
            }
        }
        catch
        {
            // Bỏ qua lỗi WMI trên môi trường không hỗ trợ
        }

        return results;
    }

    public bool Stop(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            string name = proc.ProcessName;
            if (!string.Equals(name, "agy", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, "agy.exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            proc.Kill(entireProcessTree: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
