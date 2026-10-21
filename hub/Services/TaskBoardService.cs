using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class TaskBoardService : ITaskBoardService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _configService;
    private readonly IAgentRunService _runService;

    private static readonly Regex MarkdownLinkRegex = new(@"\[(?<text>[^\]]+)\]\((?<link>[^\)]+)\)", RegexOptions.Compiled);
    private static readonly Regex TaskIdTitleRegex = new(@"^(?<id>[A-Za-z0-9\-]+)(?:[\s:\-–—]+(?<title>.*))?$", RegexOptions.Compiled);
    private static readonly Regex CheckboxRegex = new(@"^\s*-\s*\[(?<check>[ xX])\]\s*(?<text>.*)$", RegexOptions.Compiled);

    public TaskBoardService(HubPaths paths, ITeamConfigService configService, IAgentRunService runService)
    {
        _paths = paths;
        _configService = configService;
        _runService = runService;
    }

    public ProjectBoard GetBoard(string project)
    {
        var projectInfo = _configService.FindProject(project)
            ?? throw new ArgumentException($"Không tìm thấy dự án '{project}'");

        string readmePath = Path.Combine(projectInfo.Path, "tasks", "README.md");
        if (!File.Exists(readmePath))
        {
            return new ProjectBoard(projectInfo, Array.Empty<TaskCard>(), "");
        }

        string content = File.ReadAllText(readmePath);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var tasks = new List<TaskCard>();
        int tableStartIndex = -1;
        int taskCol = 0, agentCol = 1, branchCol = 2, statusCol = 3;

        // Tìm bảng markdown đầu tiên có cột "Task"
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith('|') && line.EndsWith('|'))
            {
                var headers = line.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim()).ToArray();
                int idx = Array.FindIndex(headers, h => string.Equals(h, "Task", StringComparison.OrdinalIgnoreCase));
                if (idx >= 0 && i + 1 < lines.Length && lines[i + 1].Trim().Contains("---"))
                {
                    tableStartIndex = i + 2;
                    taskCol = idx;
                    int aIdx = Array.FindIndex(headers, h => string.Equals(h, "Agent", StringComparison.OrdinalIgnoreCase));
                    if (aIdx >= 0) agentCol = aIdx;
                    int bIdx = Array.FindIndex(headers, h => h.Contains("Nhánh", StringComparison.OrdinalIgnoreCase) || h.Contains("Branch", StringComparison.OrdinalIgnoreCase));
                    if (bIdx >= 0) branchCol = bIdx;
                    int sIdx = Array.FindIndex(headers, h => h.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase) || h.Contains("Status", StringComparison.OrdinalIgnoreCase));
                    if (sIdx >= 0) statusCol = sIdx;
                    break;
                }
            }
        }

        if (tableStartIndex >= 0)
        {
            for (int i = tableStartIndex; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith('|')) break;

                var cells = line.Split('|').Select(c => c.Trim()).ToList();
                // Bỏ phần tử rỗng ở đầu và cuối do dòng bắt đầu và kết thúc bằng '|'
                if (cells.Count > 0 && string.IsNullOrEmpty(cells[0])) cells.RemoveAt(0);
                if (cells.Count > 0 && string.IsNullOrEmpty(cells[^1])) cells.RemoveAt(cells.Count - 1);

                if (cells.Count <= taskCol) continue;

                string rawTask = cells[taskCol];
                string taskId = "";
                string taskTitle = "";
                string fileName = "";

                var linkMatch = MarkdownLinkRegex.Match(rawTask);
                if (linkMatch.Success)
                {
                    string text = linkMatch.Groups["text"].Value.Trim();
                    fileName = Path.GetFileName(linkMatch.Groups["link"].Value.Trim());

                    var idMatch = TaskIdTitleRegex.Match(text);
                    if (idMatch.Success)
                    {
                        taskId = idMatch.Groups["id"].Value.Trim();
                        taskTitle = idMatch.Groups["title"].Value.Trim();
                    }
                    else
                    {
                        taskId = text;
                        taskTitle = text;
                    }
                }
                else
                {
                    var idMatch = TaskIdTitleRegex.Match(rawTask);
                    if (idMatch.Success)
                    {
                        taskId = idMatch.Groups["id"].Value.Trim();
                        taskTitle = idMatch.Groups["title"].Value.Trim();
                    }
                    else
                    {
                        taskId = rawTask;
                        taskTitle = rawTask;
                    }
                }

                string agent = cells.Count > agentCol ? cells[agentCol].Trim('`', ' ') : "";
                string branch = cells.Count > branchCol ? cells[branchCol].Trim('`', ' ') : "";
                string rawStatus = cells.Count > statusCol ? cells[statusCol] : "";

                TaskState state = TaskState.Unknown;
                if (rawStatus.Contains("🟡")) state = TaskState.Assigned;
                else if (rawStatus.Contains("🔵")) state = TaskState.Review;
                else if (rawStatus.Contains("🔴")) state = TaskState.NeedsFix;
                else if (rawStatus.Contains("✅")) state = TaskState.Merged;

                var runs = _runService.GetRuns(project, taskId);
                int runsCount = runs.Count;
                AgentRun? lastRun = runs.FirstOrDefault();

                tasks.Add(new TaskCard(
                    projectInfo.Name,
                    taskId,
                    taskTitle,
                    agent,
                    branch,
                    state,
                    fileName,
                    runsCount,
                    lastRun
                ));
            }
        }

        // PlanMarkdown = nội dung từ heading ## Kế hoạch hoặc ## Hàng chờ (tìm theo tiền tố) tới hết
        string planMarkdown = "";
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("## Kế hoạch", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("## Hàng chờ", StringComparison.OrdinalIgnoreCase))
            {
                planMarkdown = string.Join("\n", lines.Skip(i)).Trim();
                break;
            }
        }

        return new ProjectBoard(projectInfo, tasks, planMarkdown);
    }

    public TaskDetail? GetTask(string project, string taskId)
    {
        var board = GetBoard(project);
        var card = board.Tasks.FirstOrDefault(t => string.Equals(t.Id, taskId, StringComparison.OrdinalIgnoreCase));

        string? taskFilePath = null;
        if (card != null && !string.IsNullOrWhiteSpace(card.FileName))
        {
            string p = Path.Combine(board.Project.Path, "tasks", card.FileName);
            if (File.Exists(p)) taskFilePath = p;
        }

        if (taskFilePath == null)
        {
            string tasksDir = Path.Combine(board.Project.Path, "tasks");
            if (Directory.Exists(tasksDir))
            {
                var files = Directory.GetFiles(tasksDir, $"{taskId}-*.md");
                if (files.Length > 0)
                {
                    taskFilePath = files[0];
                }
                else if (File.Exists(Path.Combine(tasksDir, $"{taskId}.md")))
                {
                    taskFilePath = Path.Combine(tasksDir, $"{taskId}.md");
                }
            }
        }

        if (card == null && taskFilePath == null)
        {
            return null;
        }

        var runs = _runService.GetRuns(project, taskId);

        // Trường hợp card không có trong README.md nhưng có file task
        if (card == null)
        {
            card = new TaskCard(
                project,
                taskId,
                taskId,
                "",
                "",
                TaskState.Unknown,
                taskFilePath != null ? Path.GetFileName(taskFilePath) : "",
                runs.Count,
                runs.FirstOrDefault()
            );
        }

        // Trường hợp có card nhưng file task không tồn tại (file task thiếu)
        if (taskFilePath == null || !File.Exists(taskFilePath))
        {
            return new TaskDetail(card, Array.Empty<TaskSection>(), Array.Empty<ChecklistItem>(), runs);
        }

        // Agent sửa file task (tích tiêu chí, viết báo cáo) trên nhánh riêng trong worktree; bản trên main chỉ cập nhật khi merge.
        // Có bản worktree mới hơn ⇒ đọc bản đó để Hub thấy tiến độ ngay trong lúc agent làm.
        bool fromWorktree = false;
        string? workDir = board.Project.WorkDir;
        if (!string.IsNullOrWhiteSpace(workDir) && Directory.Exists(workDir))
        {
            var fileName = Path.GetFileName(taskFilePath);
            var mainTime = File.GetLastWriteTimeUtc(taskFilePath);
            var newest = Directory.GetDirectories(workDir)
                .Select(d => Path.Combine(d, "tasks", fileName))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (newest != null && File.GetLastWriteTimeUtc(newest) > mainTime)
            {
                taskFilePath = newest;
                fromWorktree = true;
            }
        }

        string content = File.ReadAllText(taskFilePath);

        // Bảng việc thiếu dòng của task (giao bằng mã khác tên file) ⇒ lấy agent / nhánh từ chính file task, không đoán.
        if (string.IsNullOrWhiteSpace(card.Agent) || string.IsNullOrWhiteSpace(card.Branch))
        {
            var agentM = System.Text.RegularExpressions.Regex.Match(content, @"\*\*Agent:\*\*\s*(\S+)");
            var branchM = System.Text.RegularExpressions.Regex.Match(content, @"\*\*Nhánh:\*\*\s*`([^`]+)`");
            card = card with
            {
                Agent = string.IsNullOrWhiteSpace(card.Agent) && agentM.Success ? agentM.Groups[1].Value : card.Agent,
                Branch = string.IsNullOrWhiteSpace(card.Branch) && branchM.Success ? branchM.Groups[1].Value : card.Branch,
            };
        }
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var sections = new List<TaskSection>();
        string? currentTitle = null;
        var currentLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("## "))
            {
                if (currentTitle != null)
                {
                    sections.Add(new TaskSection(currentTitle, string.Join("\n", currentLines).Trim()));
                    currentLines.Clear();
                }
                currentTitle = line.TrimStart().Substring(3).Trim();
            }
            else
            {
                if (currentTitle != null)
                {
                    currentLines.Add(line);
                }
            }
        }

        if (currentTitle != null)
        {
            sections.Add(new TaskSection(currentTitle, string.Join("\n", currentLines).Trim()));
        }

        // Criteria: lấy các dòng checkbox trong mục bắt đầu bằng "Tiêu chí"
        var criteria = new List<ChecklistItem>();
        var criteriaSection = sections.FirstOrDefault(s => s.Title.StartsWith("Tiêu chí", StringComparison.OrdinalIgnoreCase));
        if (criteriaSection != null)
        {
            var secLines = criteriaSection.Markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var sLine in secLines)
            {
                var match = CheckboxRegex.Match(sLine);
                if (match.Success)
                {
                    bool done = match.Groups["check"].Value.Equals("x", StringComparison.OrdinalIgnoreCase);
                    string text = match.Groups["text"].Value.Trim();
                    criteria.Add(new ChecklistItem(text, done));
                }
            }
        }

        return new TaskDetail(card, sections, criteria, runs) { FromWorktree = fromWorktree };
    }
}
