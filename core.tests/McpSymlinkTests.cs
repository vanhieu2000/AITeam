using AITeam.Core.Mcp;
using AITeam.Core.Messages;

namespace AITeam.Core.Tests;

// Review V5-01 (Codex phát hiện, Claude sửa): công cụ MCP không được đọc file ngoài task qua symlink trong thư mục log.
public sealed class McpSymlinkTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aiteam-v501-link-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    [Fact]
    public void Symlinked_verify_report_outside_task_is_not_read()
    {
        var logs = Path.Combine(_root, "logs"); Directory.CreateDirectory(logs);
        var secret = Path.Combine(_root, "ngoai-vung.txt"); File.WriteAllText(secret, "NOI-DUNG-NGOAI-VUNG");
        try { File.CreateSymbolicLink(Path.Combine(logs, "V5-1-verify-999.md"), secret); }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            // Windows không bật Developer Mode / không admin: user thường (như agent) cũng KHÔNG tạo được symlink ⇒ không thể tấn công kiểu này.
            return;
        }
        var tools = new McpTaskTools(new McpSession("AITeam", "V5-1", "developer", "x", Path.Combine(_root, "t.md"), logs), new MessageStore(logs), () => DateTime.Now);
        var r = tools.GetTestResults();
        Assert.DoesNotContain("NOI-DUNG-NGOAI-VUNG", r);
        Assert.StartsWith("LỖI: từ chối đọc liên kết", r);
    }
}