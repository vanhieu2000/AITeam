using AITeam.Core.Messages;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class OfficeServiceTests
{
    private sealed class StubConfig(ProjectInfo proj) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => new[] { proj };
        public ProjectInfo? FindProject(string name) => proj.Name == name ? proj : null;
        public TeamSettings GetSettings() => throw new NotImplementedException();
        public void SaveSettings(TeamSettings settings) => throw new NotImplementedException();
        public void SaveProject(ProjectInfo project) => throw new NotImplementedException();
    }

    private sealed class StubBoard(ProjectInfo proj, IReadOnlyList<TaskCard> tasks) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string project) => new(proj, tasks, "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private sealed class StubRuns : IAgentRunService
    {
        public IReadOnlyList<AgentRun> GetRuns(string project, string? taskId = null) => Array.Empty<AgentRun>();
        public IReadOnlyList<AgentRun> GetRecentRuns(int count = 20) => Array.Empty<AgentRun>();
        public IReadOnlyList<RunningAgent> GetRunning() => Array.Empty<RunningAgent>();
        public bool Stop(int pid) => false;
    }

    [Fact]
    public void Get_aggregates_staff_tasks_messages_and_active_request()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "aiteam-v504-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(tmp, "staff"));
            File.WriteAllText(Path.Combine(tmp, "staff", "B-lan.md"), "- **Tên:** Lan\n- **Chuyên môn:** Giao diện\n- **Chạy bằng:** agy");
            Directory.CreateDirectory(Path.Combine(tmp, "requests"));
            File.WriteAllText(Path.Combine(tmp, "requests", "R-005-vp.md"), "# R-005 · Văn phòng\n- **Trạng thái:** 🟡 Đang làm\n## 3. Nhật ký\n| Giờ | Sự kiện | Ghi chú |\n|---|---|---|\n| 01:00 | Khởi tạo | — |");

            var workDir = Path.Combine(tmp, "work");
            new MessageStore(Path.Combine(workDir, "logs")).Append("V5-04", "Lan (B)", "progress", "Đang làm", DateTime.Now);

            var paths = new HubPaths(tmp, Path.Combine(tmp, "profile"));
            var proj = new ProjectInfo("AITeam", "o/r", tmp, workDir, "dotnet", "b", "t", new Dictionary<string, string>(), true);
            var card = new TaskCard("AITeam", "V5-04", "Trang VP", "B", "agent-b/V5-04", TaskState.Assigned, "V5-04.md", 1, null);
            var svc = new OfficeService(paths, new StubConfig(proj), new StubBoard(proj, new[] { card }), new StubRuns(), new RequestService(paths));

            var view = svc.Get();
            Assert.Equal("R-005", view.Desks.Single(d => d.Key == "claude").Task!.Id);
            var lan = view.Desks.Single(d => d.Key == "B");
            Assert.Equal(DeskState.Working, lan.State);
            Assert.Equal("Đang làm", Assert.Single(lan.Feed).Text);
        }
        finally
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        }
    }
}
