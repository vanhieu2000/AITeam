using AITeam.Core.Mcp;
using AITeam.Core.Messages;
using AITeam.Core.Process;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AITeam.Core.Tests;

// TEST NGHIỆM THU V5-01 (Claude viết trước — agent KHÔNG sửa). MCP Gateway: công cụ theo task + chạy thật qua client MCP chính thức.
public sealed class AcceptanceV501McpTests : IDisposable
{
    private static readonly DateTime T0 = new(2026, 10, 25, 10, 0, 0);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v501-" + Guid.NewGuid().ToString("N"));
    private readonly string _logs, _taskFile;

    public AcceptanceV501McpTests()
    {
        _logs = Path.Combine(_root, "logs"); Directory.CreateDirectory(_logs);
        _taskFile = Path.Combine(_root, "V5-1-demo.md");
        File.WriteAllText(_taskFile, "# V5-1 · Thử MCP\n\n## Tiêu chí hoàn thành\n- [ ] chạy được\n");
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private McpTaskTools Tools(string role = "developer", string task = "V5-1") =>
        new(new McpSession("AITeam", task, role, "Minh (A)", _taskFile, _logs), new MessageStore(_logs), () => T0);

    // ───────────── Công cụ thuần ─────────────

    [Fact]
    public void Tools_by_role()
    {
        var all = new[] { "aiteam_get_task", "aiteam_post_message", "aiteam_get_test_results", "aiteam_get_artifacts" };
        Assert.Equal(all, McpTaskTools.ToolsFor("developer"));
        Assert.Equal(all, McpTaskTools.ToolsFor("reviewer"));
        Assert.Empty(McpTaskTools.ToolsFor("admin"));
    }

    [Fact]
    public void Get_task_reads_own_file_and_cuts_long_text()
    {
        Assert.StartsWith("# V5-1 · Thử MCP", Tools().GetTask());
        File.WriteAllText(_taskFile, new string('x', McpTaskTools.MaxRead + 500));
        var t = Tools().GetTask();
        Assert.StartsWith(new string('x', McpTaskTools.MaxRead), t);
        Assert.EndsWith($"…(đã cắt, file dài {McpTaskTools.MaxRead + 500} ký tự)", t);
        File.Delete(_taskFile);
        Assert.Equal("LỖI: không thấy file task V5-1", Tools().GetTask());
    }

    [Fact]
    public void Post_message_writes_to_store_with_session_name_and_counts()
    {
        var tools = Tools();
        Assert.Equal("Đã ghi tin nhắn (1/60).", tools.PostMessage("Bắt đầu"));
        Assert.Equal("Đã ghi tin nhắn (2/60).", tools.PostMessage("Hỏi: dùng Regex hay Split?", "question"));
        var m = new MessageStore(_logs).Read("V5-1");
        Assert.Equal(new[] { ("Minh (A)", "progress", "Bắt đầu"), ("Minh (A)", "question", "Hỏi: dùng Regex hay Split?") },
            m.Select(x => (x.From, x.Kind, x.Text)));
        Assert.Equal(T0, m[0].At);
    }

    [Theory]
    [InlineData("developer", "review")]
    [InlineData("developer", "system")]
    [InlineData("developer", "note")]
    [InlineData("reviewer", "done")]
    [InlineData("reviewer", "question")]
    public void Post_message_kind_is_limited_by_role(string role, string kind)
    {
        Assert.Equal($"LỖI: loại '{kind}' không được phép cho {role}", Tools(role).PostMessage("x", kind));
        Assert.Empty(new MessageStore(_logs).Read("V5-1"));
    }

    [Fact]
    public void Post_message_limits_and_empty()
    {
        var tools = Tools();
        Assert.Equal("LỖI: tin nhắn rỗng", tools.PostMessage("   "));
        for (var i = 0; i < McpTaskTools.MaxMessagesPerSession; i++) tools.PostMessage($"tin {i}");
        Assert.Equal("LỖI: quá 60 tin nhắn trong lượt này", tools.PostMessage("thêm"));
        Assert.Equal(60, new MessageStore(_logs).Read("V5-1", 500).Count);
        Assert.StartsWith("Đã ghi", Tools("reviewer").PostMessage("Review: ổn", "review"));   // phiên mới đếm lại
    }

    [Fact]
    public void Test_results_take_highest_number_not_text_order()
    {
        Assert.Equal("Chưa có kết quả cổng kiểm tra cho V5-1.", Tools().GetTestResults());
        File.WriteAllText(Path.Combine(_logs, "V5-1-verify-9.md"), "lần 9");
        File.WriteAllText(Path.Combine(_logs, "V5-1-verify-10.md"), "lần 10");
        File.WriteAllText(Path.Combine(_logs, "V5-10-verify-99.md"), "task khác");
        Assert.Equal("lần 10", Tools().GetTestResults());
    }

    [Fact]
    public void Artifacts_only_this_task_names_only()
    {
        Assert.Equal("Chưa có file nào của V5-1.", Tools().GetArtifacts());
        File.WriteAllText(Path.Combine(_logs, "V5-1-1.json"), new string('a', 2048));
        File.WriteAllText(Path.Combine(_logs, "V5-1.review-1.json"), "{}");
        File.WriteAllText(Path.Combine(_logs, "V5-10-1.json"), "khác");
        File.WriteAllText(Path.Combine(_logs, "V5-1x.json"), "khác");
        var lines = Tools().GetArtifacts().Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, l => l.StartsWith("V5-1-1.json · 2.0 KB · "));
        Assert.Contains(lines, l => l.StartsWith("V5-1.review-1.json · "));
        Assert.DoesNotContain("aaaa", string.Join("\n", lines));
    }

    // ───────────── Chạy thật qua client MCP chính thức ─────────────

    private static string Dll => Path.Combine(AppContext.BaseDirectory, "aiteam.dll");

    [Fact]
    public async Task Real_stdio_server_lists_tools_and_posts_message()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "aiteam",
            Command = "dotnet",
            Arguments = [Dll, "mcp", "--project", "AITeam", "--task", "V5-1", "--role", "developer", "--task-file", _taskFile, "--logs", _logs, "--from", "Minh (A)"],
        });
        // Ghim giao thức 2025-11-25 (bản cuối còn `initialize`): SDK 2.3.0 mặc định thử 2026-07-28 (`server/discover`);
        // máy bận ngay sau build ⇒ dò chậm, client rơi về `initialize` mà vẫn gắn 2026-07-28 ⇒ server từ chối
        // ("Method 'initialize' is not available on protocol version '2026-07-28'") — gốc của lỗi "chập chờn" (R-006, Claude sửa).
        await using var client = await McpClient.CreateAsync(transport, new McpClientOptions { ProtocolVersion = "2025-11-25" });

        var tools = (await client.ListToolsAsync()).Select(t => t.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "aiteam_get_artifacts", "aiteam_get_task", "aiteam_get_test_results", "aiteam_post_message" }, tools);
        Assert.All(await client.ListToolsAsync(), t => Assert.False(string.IsNullOrWhiteSpace(t.Description)));

        var r = await client.CallToolAsync("aiteam_post_message", new Dictionary<string, object?> { ["text"] = "Đã đọc task, bắt đầu code" });
        Assert.NotEqual(true, r.IsError);
        Assert.Equal("Đã ghi tin nhắn (1/60).", r.Content.OfType<TextContentBlock>().Single().Text);

        var task = await client.CallToolAsync("aiteam_get_task", new Dictionary<string, object?>());
        Assert.StartsWith("# V5-1 · Thử MCP", task.Content.OfType<TextContentBlock>().Single().Text);

        var m = Assert.Single(new MessageStore(_logs).Read("V5-1"));
        Assert.Equal(("Minh (A)", "progress", "Đã đọc task, bắt đầu code"), (m.From, m.Kind, m.Text));
    }

    [Theory]
    [InlineData("--role", "admin")]
    [InlineData("--task", "../x")]
    public async Task Bad_arguments_exit_2_without_starting(string key, string value)
    {
        var a = new List<string> { Dll, "mcp", "--project", "AITeam", "--task", "V5-1", "--role", "developer", "--task-file", _taskFile, "--logs", _logs };
        a[a.IndexOf(key) + 1] = value;
        var r = await new ProcessRunner().RunAsync(new ProcessSpec("dotnet", a, _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, r.ExitCode);
    }

    [Fact]
    public async Task Missing_logs_argument_exit_2()
    {
        var r = await new ProcessRunner().RunAsync(new ProcessSpec("dotnet", [Dll, "mcp", "--project", "AITeam", "--task", "V5-1", "--role", "developer", "--task-file", _taskFile], _root, null, TimeSpan.FromSeconds(30), null), CancellationToken.None);
        Assert.Equal(2, r.ExitCode);
    }
}
