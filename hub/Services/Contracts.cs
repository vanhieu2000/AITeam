using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// HỢP ĐỒNG DỊCH VỤ — Agent A hiện thực; Agent B dùng trong Pages (dùng bản giả ở hub/DevData khi chưa có).
// Mọi dịch vụ đọc file nhận HubPaths qua constructor để test bằng thư mục tạm.

public interface ITeamConfigService
{
    IReadOnlyList<ProjectInfo> GetProjects();
    ProjectInfo? FindProject(string name);
    TeamSettings GetSettings();
    /// <summary>Ghi team.json / team.local.json (đường dẫn máy). Kiểm tra hợp lệ, giữ .bak.</summary>
    void SaveSettings(TeamSettings settings);
    /// <summary>Ghi lại build/test/stack/areas của một dự án trong projects.json.</summary>
    void SaveProject(ProjectInfo project);
}

public interface ITaskBoardService
{
    ProjectBoard GetBoard(string project);
    TaskDetail? GetTask(string project, string taskId);
}

public interface IAgentRunService
{
    IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null);
    IReadOnlyList<AgentRun> GetRecentRuns(int count = 20);
    IReadOnlyList<RunningAgent> GetRunning();
    /// <summary>Dừng tiến trình — CHỈ khi PID đó là agy.exe. Trả false nếu không phải / không còn.</summary>
    bool Stop(int pid);
}

public interface IHealthService
{
    IReadOnlyList<HealthItem> Check();
    /// <summary>Gọi agy thật ("Tra loi dung mot chu: OK"), tối đa 2 phút.</summary>
    Task<HealthItem> CheckAgyLoginAsync(CancellationToken ct = default);
    /// <summary>Mở cửa sổ PowerShell chạy agy để người dùng /logout và đăng nhập tài khoản khác.</summary>
    void OpenSwitchAccountWindow();
    /// <summary>Cài skill aiteam + khối AITeam trong ~/.claude/CLAUDE.md (giống team.ps1 setup).</summary>
    void InstallClaudeIntegration();
}

/// <summary>Kho bí mật cục bộ, tương thích 2 chiều với team.ps1 (DPAPI CurrentUser, entropy "AITeam.vault.v1").</summary>
public interface IVaultService
{
    IReadOnlyList<string> ListNames();
    void Set(string name, string value);
    bool Remove(string name);
    /// <summary>Ghi &lt;dự án&gt;\.env.local. Ném lỗi nếu file chưa bị .gitignore hoặc thiếu tên.</summary>
    void ExportEnv(string project, IReadOnlyList<string> names);
    // KHÔNG có hàm trả về giá trị bí mật cho giao diện.
}

public interface IInboxService
{
    IReadOnlyList<InboxItem> GetItems();
    void Add(string project, string text);
}
