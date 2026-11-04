using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class TaskBoardService : ITaskBoardService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _configService;
    private readonly IAgentRunService _runService;

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
        foreach (var row in BoardTable.Parse(content))
        {
            TaskState state = TaskState.Unknown;
            if (row.Status.Contains("🟡")) state = TaskState.Assigned;
            else if (row.Status.Contains("🔵")) state = TaskState.Review;
            else if (row.Status.Contains("🔴")) state = TaskState.NeedsFix;
            else if (row.Status.Contains("✅")) state = TaskState.Merged;
            var runs = _runService.GetRuns(project, row.Id);
            tasks.Add(new TaskCard(projectInfo.Name, row.Id, row.Title, row.Agent, row.Branch,
                state, row.File, runs.Count, runs.FirstOrDefault()));
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
