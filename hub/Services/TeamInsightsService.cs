using System.Diagnostics;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Dữ liệu Hub v3: Tổng quan, Đội ngũ, Quy trình & cải tiến (R-003). Tính lại mỗi lần gọi (dữ liệu nhỏ).</summary>
public interface ITeamInsightsService
{
    TeamInsights Get();
    TaskInsight? GetTask(string project, string taskId);
}

/// <summary>Giờ merge task vào main. Tách riêng để test không cần git.</summary>
public interface IGitHistory
{
    /// <summary>Giờ commit merge mới nhất trên main có tiêu đề "Merge &lt;taskId&gt;" theo sau là "-", ":", " " hoặc hết dòng. Không có / lỗi git ⇒ null.</summary>
    DateTime? MergedAt(string repoPath, string taskId);
}

public sealed class GitHistory : IGitHistory
{
    /// <summary>
    /// Chạy: git -C &lt;repo&gt; log main --merges -E --grep="^Merge &lt;id&gt;([-: ]|$)" -1 --format=%cI
    /// (ArgumentList, không qua shell; taskId chỉ nhận [A-Za-z0-9-], sai ⇒ null; chờ tối đa 10 giây rồi kill).
    /// Kết quả đổi về giờ địa phương (DateTimeOffset.Parse(...).LocalDateTime). Cache theo (repo, id) trong 60 giây.
    /// </summary>
    public DateTime? MergedAt(string repoPath, string taskId)
        => throw new NotImplementedException(); // TODO(agent)
}

/// <summary>
/// Ghép dữ liệu:
/// 1. Dự án: config.GetProjects() có ExistsLocally. Task: board.GetBoard(p).Tasks (Id, Title, Agent, State, FileName).
///    Nội dung task: &lt;Project.Path&gt;/tasks/&lt;FileName&gt; (FileName rỗng / file không có ⇒ chuỗi rỗng, vẫn ra TaskInsight).
/// 2. Lượt: mọi metrics/runs/*.json trong AITeamRoot (InsightParsers.ParseRun, bỏ null), khớp Project + Task (không phân biệt hoa thường).
/// 3. Nhân viên: staff/*.md (ParseStaff). StaffName = tên theo Agent; Agent rỗng ⇒ "Claude"; không có hồ sơ ⇒ mã Agent.
/// 4. MergedAt: chỉ gọi git.MergedAt khi State == Merged.
/// 5. Staff: nhóm task có Request != null và Agent != "" theo Agent (StaffScore — xem chú thích model).
/// 6. StageAverages: đúng 7 phần tử Index 1..7 (tên lấy từ StageNames), TB Score khác null của MỌI task, 1 số lẻ.
/// 7. Issues: mọi StageScore có Cause khác rỗng của mọi task.
/// 8. Improvements: improvements/IMP-*.md (ParseImprovement, bỏ null), theo Id tăng dần. Thư mục không có ⇒ rỗng.
/// 9. Kpis: xem OverviewKpis. Thư mục / file lỗi đọc ⇒ bỏ qua, không ném lỗi.
/// </summary>
public sealed class TeamInsightsService : ITeamInsightsService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly ITaskBoardService _board;
    private readonly IGitHistory _git;

    public TeamInsightsService(HubPaths paths, ITeamConfigService config, ITaskBoardService board, IGitHistory git)
    {
        _paths = paths;
        _config = config;
        _board = board;
        _git = git;
    }

    public TeamInsights Get()
        => throw new NotImplementedException(); // TODO(agent)

    public TaskInsight? GetTask(string project, string taskId)
        => throw new NotImplementedException(); // TODO(agent)
}
