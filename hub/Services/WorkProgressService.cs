using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Log chỉ đọc, Name là tên file an toàn; Content nguyên văn, không suy diễn kết quả gate.</summary>
public sealed record WorkProgressLog(string Name, string Content);

/// <summary>Task nối bằng metadata Yêu cầu; Agent từ metadata Agent; Progress theo W1-05.</summary>
public sealed record WorkTaskProgress(string Id, string Title, string Agent, WorkProgress Progress,
    IReadOnlyList<WorkProgressLog> Logs);

/// <summary>Yêu cầu luôn hiện kể cả chưa có task. StepCounts đủ chín khóa, đếm task theo CurrentStep.
/// Progress lấy task chưa merge đầu tiên theo Id, hoặc task đầu nếu tất cả merge;
/// khi chưa có task tính bằng plan và nhật ký yêu cầu.</summary>
public sealed record WorkRequestProgress(string Id, string Title, WorkProgress Progress,
    IReadOnlyList<WorkTaskProgress> Tasks, IReadOnlyDictionary<WorkStep, int> StepCounts);

/// <summary>Snapshot mới mỗi lần đọc; Requests, Tasks và UnlinkedTasks xếp Id OrdinalIgnoreCase.</summary>
public sealed record WorkProjectProgress(ProjectInfo Project, IReadOnlyList<WorkRequestProgress> Requests,
    IReadOnlyList<WorkTaskProgress> UnlinkedTasks);

/// <summary>Hợp đồng riêng của trang Việc, không thay Contracts.cs hoặc HubModels.cs.</summary>
public interface IWorkProgressService
{
    /// <summary>
    /// Lấy dự án bằng ITeamConfigService.FindProject; không dùng tên dự án làm đường dẫn.
    /// Đọc requests/R-xxx-*.md, plans/R-xxx/plan.md, tasks/*.md tại Project.Path;
    /// logs/&lt;task&gt;-verify-N.md và logs/&lt;task&gt;.review-N.json chỉ tại Project.WorkDir.
    /// Không đòi .git; thiếu thư mục con trả rỗng, thiếu repo là lỗi nguồn.
    /// Nối bằng đúng metadata '- **Yêu cầu:** R-xxx'; thiếu liên kết hoặc phiếu không tồn tại vào UnlinkedTasks.
    /// Tiêu đề lấy heading đầu, bỏ Id và dấu ·. Mã lấy tên file hợp lệ, không lấy đường dẫn từ văn xuôi.
    /// PlanApproved khi có 'ĐÃ DUYỆT', loại dòng chứa 'CHƯA ĐÃ DUYỆT'; plan mất là chưa có bằng chứng.
    /// Dùng WorkProgressParser.Parse/Calculate và boardStatus đúng dòng task ở tasks/README.md.
    /// ReceivedAt từ Nhận lúc (UTC+07); thiếu giờ giữ 'Chưa có dữ liệu'.
    /// Actor lấy IWorkflowRolesService.GetRoles: receive/plan/approve_plan/prep/code/gate/qc/accept/publish.
    /// Code ưu tiên DeveloperInfo.Name có Id khớp Agent; còn lại tra Developers rồi tên chuẩn:
    /// claude=Claude, codex=Codex, minh=Minh, lan=Lan, gate=Cổng KT, owner=Chủ dự án.
    /// Chỉ gán ActorText cho bước Done/Failed/Current; không bịa giờ hoặc gán người cho Waiting.
    /// Log thiếu trả Logs rỗng; log hiện có phải UTF-8 hợp lệ, review là JSON object.
    /// Logs xếp Name OrdinalIgnoreCase; giữ nguyên Content, không đoán schema/kết quả log ngoài W1-05.
    /// null/rỗng/tên dự án không cấu hình: InvalidDataException('Việc không hợp lệ.').
    /// Lỗi nguồn (UTF-8/JSON hỏng, lỗi đọc, mã/link sai, trùng Id, reparse point, vượt giới hạn)
    /// ném IOException('Không đọc được tiến độ công việc. Nguồn: &lt;tên an toàn&gt;').
    /// Tên an toàn chỉ tên file hoặc requests/plans/tasks/logs/cấu hình; không lộ đường dẫn,
    /// nội dung hoặc InnerException chứa bí mật. Chặn ../đường dẫn tuyệt đối trong metadata.
    /// Từ chối reparse point ở file, thư mục nguồn và mọi tổ tiên đến gốc.
    /// Giới hạn mỗi file 2 MiB, mỗi thư mục nguồn 2000 file; không chạy tiến trình, ghi/migrate/tạo thư mục.
    /// Nhiều lời gọi đồng thời phải độc lập, không lẫn dữ liệu dự án, không cache cũ.
    /// </summary>
    WorkProjectProgress GetProject(string project);

    /// <summary>Đọc lại theo GetProject. requestId khớp ^R-\d+$, không phân biệt hoa thường.
    /// Mã sai hoặc không có phiếu: InvalidDataException('Việc không hợp lệ.'); không đọc đường dẫn người dùng.</summary>
    WorkRequestProgress GetRequest(string project, string requestId);
}

/// <summary>Dịch vụ chỉ đọc, nhận HubPaths để kiểm thử trong thư mục tạm.</summary>
public sealed class WorkProgressService(HubPaths paths, ITeamConfigService config, IWorkflowRolesService roles) : IWorkProgressService
{
    private const string Missing = "Chưa có dữ liệu";
    private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly Regex RequestId = new(@"\AR-\d+\z", RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public WorkProjectProgress GetProject(string project)
    {
        if (string.IsNullOrWhiteSpace(project)) throw InvalidWork();
        var source = "cấu hình";
        ProjectInfo? selected;
        try
        {
            CheckPath(paths.ProjectsJson);
            CheckPath(paths.TeamJson);
            CheckPath(paths.TeamLocalJson);
            selected = config.FindProject(project);
        }
        catch { throw SourceError(source); }
        if (selected == null) throw InvalidWork();
        try
        {
            var developers = config.GetSettings().Developers.ToArray();
            var assignments = roles.GetRoles().Roles.ToArray();
            source = "requests";
            var root = Path.GetFullPath(selected.Path);
            CheckPath(root);
            if (!Directory.Exists(root)) throw SourceError(source);
            var requestFiles = Files(Path.Combine(root, "requests"));
            source = "plans";
            var planRoot = Path.Combine(root, "plans");
            CheckPath(planRoot);
            if (File.Exists(planRoot)) throw SourceError(source);
            var plans = new Dictionary<string, (bool Exists, bool Approved)>(IdComparer);
            if (Directory.Exists(planRoot))
            {
                var directories = Directory.EnumerateDirectories(planRoot).Take(2001).ToArray();
                if (directories.Length > 2000) throw SourceError(source);
                _ = Files(planRoot);
                foreach (var dir in directories)
                {
                    source = "plans";
                    CheckPath(dir);
                    var id = Path.GetFileName(dir);
                    var files = Files(dir);
                    if (!RequestId.IsMatch(id)) continue;
                    var plan = files.SingleOrDefault(f => Path.GetFileName(f).Equals("plan.md", StringComparison.OrdinalIgnoreCase));
                    if (plan == null) continue;
                    source = Path.GetFileName(plan);
                    var content = Read(plan);
                    var approved = Lines(content).Any(l => l.Contains("ĐÃ DUYỆT", StringComparison.OrdinalIgnoreCase)
                        && !l.Contains("CHƯA ĐÃ DUYỆT", StringComparison.OrdinalIgnoreCase));
                    if (!plans.TryAdd(id, (true, approved))) throw SourceError(source);
                }
            }
            source = "tasks";
            var taskFiles = Files(Path.Combine(root, "tasks"));
            var boardPath = taskFiles.SingleOrDefault(f => Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase));
            var board = new Dictionary<string, string>(IdComparer);
            if (boardPath != null)
            {
                source = Path.GetFileName(boardPath);
                foreach (var line in Lines(Read(boardPath)))
                {
                    var match = Regex.Match(line, @"^\|\s*\[([A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9-]*)(?:\s|\])");
                    if (match.Success)
                        board[match.Groups[1].Value] = line.Trim().Trim('|').Split('|').Last().Trim();
                }
            }
            source = "logs";
            var logFiles = Files(Path.Combine(Path.GetFullPath(selected.WorkDir), "logs"));
            var requests = new Dictionary<string, (string Title, IReadOnlyList<WorkProgressEvent> Events)>(IdComparer);
            foreach (var file in requestFiles.Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
            {
                source = Path.GetFileName(file);
                var match = Regex.Match(source, @"^(R-\d+)-.+\.md$", RegexOptions.IgnoreCase);
                if (!match.Success) throw SourceError(source);
                var id = match.Groups[1].Value.ToUpperInvariant();
                var content = Read(file);
                var received = Regex.Match(content, @"(?m)^- \*\*Nhận lúc:\*\*\s*(\d{1,2}:\d{2} \d{1,2}/\d{1,2}/\d{4})");
                DateTimeOffset? receivedAt = DateTime.TryParseExact(received.Groups[1].Value,
                    new[] { "H:mm d/M/yyyy", "HH:mm dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? new DateTimeOffset(date, TimeSpan.FromHours(7)) : null;
                if (!requests.TryAdd(id, (Title(content, id), WorkProgressParser.Parse(content, receivedAt))))
                    throw SourceError(source);
            }
            var tasks = new List<(string Request, WorkTaskProgress Task)>();
            var taskIds = new HashSet<string>(IdComparer);
            foreach (var file in taskFiles.Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase)))
            {
                source = Path.GetFileName(file);
                var match = Regex.Match(source, @"^([A-Za-z][A-Za-z0-9]*-\d+[A-Za-z0-9]*)(?:-.+)?\.md$", RegexOptions.IgnoreCase);
                if (!match.Success) throw SourceError(source);
                var id = match.Groups[1].Value;
                if (!taskIds.Add(id)) throw SourceError(source);
                var content = Read(file);
                var links = Regex.Matches(content, @"(?m)^- \*\*Yêu cầu:\*\*([^\r\n]*)");
                var request = links.Count == 0 ? "" : links[0].Groups[1].Value.Trim();
                if (links.Count > 1 || (links.Count != 0 && !RequestId.IsMatch(request))) throw SourceError(source);
                var agent = Regex.Match(content, @"(?m)^- \*\*Agent:\*\*([^\r\n]*)").Groups[1].Value.Trim();
                var events = requests.TryGetValue(request, out var parent) ? parent.Events : Array.Empty<WorkProgressEvent>();
                var plan = plans.GetValueOrDefault(request);
                var progress = Decorate(WorkProgressParser.Calculate(id, events, plan.Exists, plan.Approved,
                    board.GetValueOrDefault(id)), agent);
                var logs = new List<WorkProgressLog>();
                foreach (var log in logFiles.OrderBy(Path.GetFileName, IdComparer))
                {
                    var name = Path.GetFileName(log);
                    var logMatch = Regex.Match(name, "^" + Regex.Escape(id) + @"(?:-verify-([1-9]\d*)\.md|\.review-([1-9]\d*)\.json)$", RegexOptions.IgnoreCase);
                    if (!logMatch.Success || !int.TryParse(logMatch.Groups[1].Success ? logMatch.Groups[1].Value : logMatch.Groups[2].Value, out _)) continue;
                    source = name;
                    var logContent = Read(log);
                    if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        using var json = JsonDocument.Parse(logContent);
                        if (json.RootElement.ValueKind != JsonValueKind.Object) throw SourceError(source);
                    }
                    logs.Add(new WorkProgressLog(name, logContent));
                }
                tasks.Add((request, new WorkTaskProgress(id, Title(content, id), agent, progress, logs.ToArray())));
            }
            var results = requests.OrderBy(r => r.Key, IdComparer).Select(r =>
            {
                var children = tasks.Where(t => IdComparer.Equals(t.Request, r.Key)).Select(t => t.Task).OrderBy(t => t.Id, IdComparer).ToArray();
                var plan = plans.GetValueOrDefault(r.Key);
                var progress = children.FirstOrDefault(t => !t.Progress.IsMerged)?.Progress ?? children.FirstOrDefault()?.Progress
                    ?? Decorate(WorkProgressParser.Calculate(r.Key, r.Value.Events, plan.Exists, plan.Approved), "");
                return new WorkRequestProgress(r.Key, r.Value.Title, progress, children,
                    Enum.GetValues<WorkStep>().ToDictionary(s => s, s => children.Count(t => t.Progress.CurrentStep == s)));
            }).ToArray();
            return new WorkProjectProgress(selected, results, tasks.Where(t => !requests.ContainsKey(t.Request))
                .Select(t => t.Task).OrderBy(t => t.Id, IdComparer).ToArray());

            WorkProgress Decorate(WorkProgress progress, string agent)
            {
                var codes = new[] { "receive", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish" };
                return progress with { Steps = progress.Steps.Select(s =>
                {
                    if (s.State == WorkStepState.Waiting) return s with { ActorText = Missing };
                    var actor = assignments.FirstOrDefault(a => a.Code == codes[(int)s.Step])?.Actor ?? "";
                    var person = s.Step == WorkStep.Code ? developers.FirstOrDefault(d => IdComparer.Equals(d.Id, agent)) : null;
                    person ??= developers.FirstOrDefault(d => IdComparer.Equals(d.Id, actor));
                    var name = person?.Name ?? actor.ToLowerInvariant() switch
                    {
                        "claude" => "Claude", "codex" => "Codex", "minh" => "Minh", "lan" => "Lan",
                        "gate" => "Cổng KT", "owner" => "Chủ dự án", _ => Missing
                    };
                    return s with { ActorText = name };
                }).ToArray() };
            }
        }
        catch { throw SourceError(source); }
    }

    /// <inheritdoc />
    public WorkRequestProgress GetRequest(string project, string requestId)
    {
        if (requestId == null || !RequestId.IsMatch(requestId)) throw InvalidWork();
        return GetProject(project).Requests.FirstOrDefault(r => IdComparer.Equals(r.Id, requestId)) ?? throw InvalidWork();
    }

    private static InvalidDataException InvalidWork() => new("Việc không hợp lệ.");
    private static IOException SourceError(string source) => new($"Không đọc được tiến độ công việc. Nguồn: {source}");
    private static void CheckPath(string path)
    {
        if (TeamFileLock.HasReparsePoint(Path.GetFullPath(path))) throw SourceError("cấu hình");
    }
    private static string[] Files(string directory)
    {
        CheckPath(directory);
        if (File.Exists(directory)) throw SourceError("cấu hình");
        if (!Directory.Exists(directory)) return Array.Empty<string>();
        var files = Directory.EnumerateFiles(directory).Take(2001).ToArray();
        if (files.Length > 2000) throw SourceError("cấu hình");
        return files;
    }
    private static string Read(string file)
    {
        CheckPath(file);
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 2 * 1024 * 1024) throw SourceError("cấu hình");
        using var buffer = new MemoryStream();
        var bytes = new byte[8192];
        int count;
        while ((count = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
            if (buffer.Length + count > 2 * 1024 * 1024) throw SourceError("cấu hình");
            buffer.Write(bytes, 0, count);
        }
        var content = new UTF8Encoding(false, true).GetString(buffer.ToArray());
        return content.Length > 0 && content[0] == '\uFEFF' ? content[1..] : content;
    }
    private static string[] Lines(string content) => content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    private static string Title(string content, string id)
    {
        var heading = Lines(content).FirstOrDefault(l => Regex.IsMatch(l, @"^#\s+"));
        if (heading == null) return id;
        return Regex.Replace(heading, @"^#\s+(?:" + Regex.Escape(id) + @"\b\s*[·:–—-]?\s*)?", "", RegexOptions.IgnoreCase).Trim();
    }
}
