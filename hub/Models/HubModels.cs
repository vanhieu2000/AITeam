namespace AITeam.Hub.Models;

// HỢP ĐỒNG DỮ LIỆU giữa dịch vụ (Agent A) và giao diện (Agent B). Do Claude thiết kế — không tự đổi.
// Thêm thuộc tính mới thì ghi vào báo cáo task. Xem docs/HUB-DESIGN.md mục 3, 6.

/// <summary>Dự án trong projects.json (đã áp đường dẫn riêng của máy).</summary>
public sealed record ProjectInfo(
    string Name,
    string Repo,              // "owner/name"
    string Path,              // thư mục dự án trên máy này
    string WorkDir,           // thư mục worktree + logs của agent
    string Stack,
    string Build,
    string Test,
    IReadOnlyDictionary<string, string> Areas,   // "A" → "src/, tools/…"
    bool ExistsLocally)
{
    public string GitHubUrl => $"https://github.com/{Repo}";
}

/// <summary>Trạng thái task theo emoji trong tasks/README.md: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge.</summary>
public enum TaskState { Assigned, Review, NeedsFix, Merged, Unknown }

public sealed record ChecklistItem(string Text, bool Done);

/// <summary>Một mục "## Tiêu đề" trong file task, nội dung giữ nguyên markdown.</summary>
public sealed record TaskSection(string Title, string Markdown);

public enum RunState { Running, Success, Error }

/// <summary>Một lần chạy agent: logs/&lt;Task&gt;-&lt;n&gt;.json (rỗng = đang chạy).</summary>
public sealed record AgentRun(
    string Project,
    string Task,              // "T-001"
    int Attempt,              // n
    RunState State,
    string? Status,           // "SUCCESS" / "ERROR" như trong log
    string? Error,
    string? Response,         // câu trả lời cuối của agent (markdown)
    double? DurationSeconds,
    long? TotalTokens,
    DateTime Time,            // thời điểm ghi log (LastWriteTime)
    string LogPath);

/// <summary>Một dòng trong bảng việc.</summary>
public sealed record TaskCard(
    string Project,
    string Id,                // "T-001"
    string Title,
    string Agent,             // "A" / "B"
    string Branch,
    TaskState State,
    string FileName,          // "T-001-dotnet10.md" (rỗng nếu không thấy file)
    int Runs,
    AgentRun? LastRun);

public sealed record TaskDetail(
    TaskCard Card,
    IReadOnlyList<TaskSection> Sections,
    IReadOnlyList<ChecklistItem> Criteria,   // các "- [ ]" / "- [x]" trong mục "Tiêu chí hoàn thành"
    IReadOnlyList<AgentRun> Runs)            // mới nhất trước
{
    /// <summary>true khi nội dung đọc từ bản task agent đang sửa (worktree) — tiêu chí được tích ngay trong lúc làm, trước khi merge.</summary>
    public bool FromWorktree { get; init; }
}

/// <summary>Bảng việc của một dự án.</summary>
public sealed record ProjectBoard(ProjectInfo Project, IReadOnlyList<TaskCard> Tasks, string PlanMarkdown);

/// <summary>Tiến trình agy.exe đang chạy.</summary>
public sealed record RunningAgent(int Pid, DateTime StartedAt, string? Project, string? Task, string CommandLine)
{
    public TimeSpan Elapsed => DateTime.Now - StartedAt;
}

public enum HealthLevel { Ok, Warning, Error }

/// <summary>Một dòng kiểm tra máy (giống team.ps1 doctor).</summary>
public sealed record HealthItem(string Name, HealthLevel Level, string Detail, string? Fix);

/// <summary>Một dòng trong inbox.md: "- [ ] 2026-10-20 18:00 · HDVTool: nội dung".</summary>
public sealed record InboxItem(int LineNumber, DateTime? Time, string Project, string Text, bool Done);

/// <summary>Cấu hình có thể sửa trên trang Cài đặt.</summary>
public sealed record TeamSettings(
    string DefaultModel,
    int MaxReviewRounds,
    IReadOnlyList<DeveloperInfo> Developers,
    string ProjectsRoot,      // hiệu lực (sau khi áp team.local.json)
    string WorkRoot,
    string GitHubOwner,
    IReadOnlyList<string> AgyAccounts,
    string AgyExe);

public sealed record DeveloperInfo(string Id, string Name, string Focus);
