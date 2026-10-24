using AITeam.Core.Mcp;
using AITeam.Core.Messages;

namespace AITeam.Cli;

using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

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
    {
        return RunCoreAsync(args);
    }

    private static readonly Regex SafeTask = new(@"^[A-Za-z0-9][A-Za-z0-9-]*$", RegexOptions.Compiled);

    private static async Task<int> RunCoreAsync(string[] args)
    {
        var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                opts[args[i]] = args[++i];
        }

        if (!opts.TryGetValue("--project", out var project) || string.IsNullOrWhiteSpace(project) ||
            !opts.TryGetValue("--task", out var task) || !SafeTask.IsMatch(task) ||
            !opts.TryGetValue("--role", out var role) ||
            !opts.TryGetValue("--task-file", out var taskFile) || string.IsNullOrWhiteSpace(taskFile) ||
            !opts.TryGetValue("--logs", out var logs) || string.IsNullOrWhiteSpace(logs))
        {
            await Console.Error.WriteLineAsync("LỖI: thiếu hoặc sai tham số cho aiteam mcp.");
            return 2;
        }

        var allowed = McpTaskTools.ToolsFor(role);
        if (allowed.Count == 0)
        {
            await Console.Error.WriteLineAsync($"LỖI: vai trò '{role}' không hợp lệ.");
            return 2;
        }

        var from = opts.TryGetValue("--from", out var f) && !string.IsNullOrWhiteSpace(f) ? f : "agent";
        var tools = new McpTaskTools(new McpSession(project, task, role, from, taskFile, logs), new MessageStore(logs), () => DateTime.Now);

        var map = new Dictionary<string, McpServerTool>(StringComparer.Ordinal)
        {
            ["aiteam_get_task"] = McpServerTool.Create(
                () => tools.GetTask(),
                new McpServerToolCreateOptions { Name = "aiteam_get_task", Description = "Đọc nội dung file task được giao hiện tại." }),
            ["aiteam_post_message"] = McpServerTool.Create(
                ([Description("Nội dung tin nhắn")] string text, [Description("Loại tin: progress, question, done, review")] string kind = "progress") => tools.PostMessage(text, kind),
                new McpServerToolCreateOptions { Name = "aiteam_post_message", Description = "Gửi tin nhắn báo tiến độ / hỏi quản lý / báo xong." }),
            ["aiteam_get_test_results"] = McpServerTool.Create(
                () => tools.GetTestResults(),
                new McpServerToolCreateOptions { Name = "aiteam_get_test_results", Description = "Xem báo cáo cổng kiểm tra mới nhất của task." }),
            ["aiteam_get_artifacts"] = McpServerTool.Create(
                () => tools.GetArtifacts(),
                new McpServerToolCreateOptions { Name = "aiteam_get_artifacts", Description = "Xem danh sách file kết quả của task trong thư mục logs." })
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services
            .AddMcpServer(o => o.ServerInfo = new() { Name = "aiteam", Version = "1.0" })
            .WithStdioServerTransport()
            .WithTools(allowed.Where(map.ContainsKey).Select(k => map[k]));

        using var app = builder.Build();
        await app.RunAsync();
        return 0;
    }
}
