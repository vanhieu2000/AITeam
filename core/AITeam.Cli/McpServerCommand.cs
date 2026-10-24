using AITeam.Core.Mcp;
using AITeam.Core.Messages;

namespace AITeam.Cli;

/// <summary>
/// `aiteam mcp --project P --task T --role developer|reviewer --task-file F --logs D [--from "Minh (A)"]` — máy chủ MCP stdio (R-005 / V5-01).
/// Dùng SDK chính thức ModelContextProtocol 2.3.0 (đã PoC với agy 25/10):
///   Host.CreateApplicationBuilder; Logging ra STDERR (stdout dành cho JSON-RPC: AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace));
///   AddMcpServer(o => o.ServerInfo = new() { Name = "aiteam", Version = "1.0" }).WithStdioServerTransport();
///   Đăng ký ĐÚNG các công cụ của McpTaskTools.ToolsFor(role) (dùng McpServerTool.Create(delegate, new McpServerToolCreateOptions { Name = …, Description = … }) hoặc WithTools(…)).
/// Tên công cụ: aiteam_get_task, aiteam_post_message (tham số text, kind mặc định "progress"), aiteam_get_test_results, aiteam_get_artifacts.
/// Mô tả công cụ bằng tiếng Việt, ngắn, nói rõ khi nào dùng (vd. post_message: "báo tiến độ / hỏi quản lý / báo xong").
/// </summary>
public static class McpServerCommand
{
    /// <summary>
    /// Kiểm tra tham số TRƯỚC khi khởi động máy chủ (lỗi ⇒ in stderr, trả 2, KHÔNG khởi động):
    /// thiếu --project / --task / --role / --task-file / --logs; role khác developer | reviewer; task sai mẫu ^[A-Za-z0-9][A-Za-z0-9-]*$.
    /// --from mặc định = "agent". Chạy tới khi stdin đóng; trả 0.
    /// </summary>
    public static Task<int> RunAsync(string[] args)
        => throw new NotImplementedException(); // TODO(agent)
}
