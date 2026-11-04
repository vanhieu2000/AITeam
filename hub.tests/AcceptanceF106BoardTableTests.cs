using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Test nghiệm thu F1-06 (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-06.
/// 04/11: 1 dòng trống giữa bảng việc làm Hub mất 38 task; dòng thiếu "|" cuối bị bỏ qua.</summary>
public sealed class AcceptanceF106BoardTableTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    public void Dispose() => _env.Dispose();

    // Mẫu thật rút gọn từ tasks/README.md của AITeam (04/11/2026): có dòng trống giữa bảng, dòng thiếu "|" cuối,
    // dòng "Trạng thái: …" sau bảng, rồi mục "## Kế hoạch" có một bảng khác (KHÔNG được đọc).
    private const string RealBoard = """
        # Bảng công việc · AITeam (AITeam Hub)

        Hướng dẫn cho agent: [AGENTS.md](../AGENTS.md).

        | Task | Agent | Nhánh | Trạng thái |
        |---|---|---|---|
        | [V5-01 MCP Gateway: aiteam mcp](V5-01-mcp-gateway.md) | A | `agent-a/V5-01-mcp-gateway` | ✅ Đã merge (vòng 1) |
        | [H7-06 Menu mới](H7-06-menu.md) | B | `agent-b/H7-06-menu` | 🔵 Chờ review · cổng kiểm tra ĐẠT

        | [V9-10 Ghi lượt chạy](V9-10-ghi-luot.md) | C | `agent-c/V9-10-ghi-luot` | 🟡 Đang chạy · máy HIEUDV · 12:00 |


        | [K1-03 Bảng việc: cảnh báo git](K1-03-bang-viec-canh-bao-git.md) | C | `agent-c/K1-03-bang-viec-canh-bao-git` | 🔴 Cần sửa |
        | Không phải task | | | |

        Trạng thái: 🟡 Đã giao · 🔵 Chờ review · 🔴 Cần sửa · ✅ Đã merge

        ## Kế hoạch / hàng chờ

        | O-02 | OpenCode/DeepSeek | agent-c/O-02-opencode-deepseek | Chờ kiểm mạng |
        """;

    [Fact]
    public void Parse_keeps_rows_after_blank_lines_and_rows_without_trailing_pipe()
    {
        var rows = BoardTable.Parse(RealBoard);
        Assert.Equal(new[] { "V5-01", "H7-06", "V9-10", "K1-03" }, rows.Select(r => r.Id));

        var h = rows[1];
        Assert.Equal("Menu mới", h.Title);
        Assert.Equal("H7-06-menu.md", h.File);
        Assert.Equal("B", h.Agent);
        Assert.Equal("agent-b/H7-06-menu", h.Branch);
        Assert.Equal("🔵 Chờ review · cổng kiểm tra ĐẠT", h.Status);

        Assert.Equal("MCP Gateway: aiteam mcp", rows[0].Title);
        Assert.Equal("🟡 Đang chạy · máy HIEUDV · 12:00", rows[2].Status);
        Assert.Equal("🔴 Cần sửa", rows[3].Status);
    }

    [Fact]
    public void Parse_stops_at_text_or_heading_and_ignores_tables_in_later_sections()
    {
        var rows = BoardTable.Parse(RealBoard);
        Assert.DoesNotContain(rows, r => r.Id == "O-02");
        Assert.All(rows, r => Assert.True(r.Line > 0));
        Assert.Equal(7, rows[0].Line);
    }

    [Fact]
    public void Columns_are_found_by_header_name_not_position()
    {
        const string md = """
            | Trạng thái | Task | Nhánh | Agent | Ghi chú |
            |---|---|---|---|---|
            | ✅ Đã merge | [T-001 Một](T-001-mot.md) | `agent-a/T-001` | A | ghi chú có chữ 🔴 |
            """;
        var row = Assert.Single(BoardTable.Parse(md));
        Assert.Equal("T-001", row.Id);
        Assert.Equal("✅ Đã merge", row.Status);
        Assert.Equal("A", row.Agent);
        Assert.Equal("agent-a/T-001", row.Branch);
    }

    [Fact]
    public void Plain_task_cell_without_link_and_missing_columns_are_tolerated()
    {
        const string md = """
            | Task | Trạng thái |
            |---|---|
            | T-009 · Việc không có link | ✅ Đã merge |
            """;
        var row = Assert.Single(BoardTable.Parse(md));
        Assert.Equal("T-009", row.Id);
        Assert.Equal("Việc không có link", row.Title);
        Assert.Equal("", row.File);
        Assert.Equal("", row.Agent);
        Assert.Equal("", row.Branch);
        Assert.Equal("✅ Đã merge", row.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("# Không có bảng\n\nchỉ có chữ")]
    [InlineData("| A | B |\n|---|---|\n| 1 | 2 |")]
    [InlineData("| Task | Agent |\n| [T-001 X](x.md) | A |")]
    public void No_task_table_returns_empty_without_throwing(string? md)
        => Assert.Empty(BoardTable.Parse(md));

    [Fact]
    public void Duplicate_id_keeps_first_row()
    {
        const string md = """
            | Task | Trạng thái |
            |---|---|
            | [T-001 Đầu](a.md) | ✅ Đã merge |
            | [T-001 Sau](b.md) | 🔴 Cần sửa |
            """;
        var row = Assert.Single(BoardTable.Parse(md));
        Assert.Equal("Đầu", row.Title);
    }

    [Fact]
    public void Board_service_reads_every_task_of_real_board()
    {
        var projectPath = Path.Combine(_env.RootDir, "ProjectAlpha");
        Directory.CreateDirectory(Path.Combine(projectPath, "tasks"));
        File.WriteAllText(Path.Combine(projectPath, "tasks", "README.md"), RealBoard);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "team.json"), _env.Paths.TeamJson);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "projects.json"), _env.Paths.ProjectsJson);
        var config = new TeamConfigService(_env.Paths);
        config.SaveProject(config.FindProject("ProjectAlpha")! with { Path = projectPath });
        var boards = new TaskBoardService(_env.Paths, config, new AgentRunService(_env.Paths, config));

        var board = boards.GetBoard("ProjectAlpha");

        Assert.Equal(new[] { "V5-01", "H7-06", "V9-10", "K1-03" }, board.Tasks.Select(t => t.Id));
        Assert.Equal(new[] { TaskState.Merged, TaskState.Review, TaskState.Assigned, TaskState.NeedsFix }, board.Tasks.Select(t => t.State));
        Assert.Equal("agent-c/V9-10-ghi-luot", board.Tasks[2].Branch);
    }

    [Fact]
    public void Every_board_reader_uses_the_shared_parser()
    {
        // Trước F1-06 có 4 bộ đọc bảng riêng (TaskBoardService, WorkOverviewService, PerformanceService, WorkProgressService);
        // 2 bộ lấy trạng thái ở cells[^2] nên dòng thiếu "|" cuối bị đọc sai cột.
        var root = FindRoot();
        foreach (var file in new[] { "TaskBoardService.cs", "WorkOverviewService.cs", "PerformanceService.cs", "WorkProgressService.cs" })
        {
            var src = File.ReadAllText(Path.Combine(root, "hub", "Services", file));
            Assert.Contains("BoardTable.Parse(", src);
            Assert.DoesNotContain("cells[^2]", src);
        }
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("AITeam.sln");
    }
}
