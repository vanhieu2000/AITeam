using AITeam.Core.Messages;

namespace AITeam.Core.Mcp;

// MCP Gateway của AITeam (R-005 / V5-01). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C1.
// Mỗi tiến trình `aiteam mcp` GẮN với đúng 1 task + 1 vai trò ⇒ agent chỉ thấy task của mình.
// Không công cụ nào chạy lệnh, đọc bí mật, đọc file ngoài task, hay gọi mạng.

/// <summary>Phiên MCP. Role: "developer" | "reviewer". From: tên hiển thị khi ghi tin ("Minh (A)", "Codex (C)").</summary>
public sealed record McpSession(string Project, string TaskId, string Role, string From, string TaskFile, string LogsDir);

/// <summary>Logic thuần của các công cụ (dễ test). Mọi hàm KHÔNG ném lỗi: lỗi ⇒ chuỗi bắt đầu "LỖI: ".</summary>
public sealed class McpTaskTools
{
    public const int MaxMessagesPerSession = 60;
    public const int MaxRead = 20_000;
    public const int MaxArtifacts = 30;

    /// <summary>Tên công cụ theo vai trò (đúng thứ tự): developer và reviewer đều có aiteam_get_task, aiteam_post_message, aiteam_get_test_results, aiteam_get_artifacts (khác nhau ở loại tin được ghi). Vai trò khác ⇒ rỗng.</summary>
    public static IReadOnlyList<string> ToolsFor(string role)
        => throw new NotImplementedException(); // TODO(agent) — role lạ ⇒ rỗng

    private readonly McpSession _s;
    private readonly MessageStore _store;
    private readonly Func<DateTime> _now;

    public McpTaskTools(McpSession session, MessageStore store, Func<DateTime> now)
    {
        _s = session;
        _store = store;
        _now = now;
    }

    /// <summary>
    /// aiteam_get_task: nội dung file task. Không có file ⇒ "LỖI: không thấy file task {TaskId}".
    /// Dài hơn MaxRead ⇒ cắt còn MaxRead ký tự + dòng "\n…(đã cắt, file dài {n} ký tự)".
    /// </summary>
    public string GetTask()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// aiteam_post_message: ghi tin vào MessageStore với From = session.From.
    /// Kind cho phép theo vai trò: developer ⇒ progress | question | done; reviewer ⇒ progress | review. Kind khác ⇒ "LỖI: loại '{kind}' không được phép cho {Role}".
    /// Quá MaxMessagesPerSession tin trong phiên ⇒ "LỖI: quá 60 tin nhắn trong lượt này" (không ghi).
    /// Text rỗng ⇒ "LỖI: tin nhắn rỗng". Thành công ⇒ "Đã ghi tin nhắn ({k}/60)." với k = số tin đã ghi trong phiên.
    /// </summary>
    public string PostMessage(string text, string kind = "progress")
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// aiteam_get_test_results: nội dung báo cáo cổng KT MỚI NHẤT "{TaskId}-verify-{n}.md" trong LogsDir (n số lớn nhất, so sánh SỐ không phải chữ).
    /// Không có ⇒ "Chưa có kết quả cổng kiểm tra cho {TaskId}." Cắt MaxRead như GetTask.
    /// </summary>
    public string GetTestResults()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// aiteam_get_artifacts: danh sách file trong LogsDir thuộc task: tên bắt đầu "{TaskId}-" hoặc "{TaskId}." (KHÔNG khớp task khác có tiền tố, vd. V5-1 vs V5-10).
    /// Mỗi dòng "{tên} · {kích thước KB, 1 số lẻ, InvariantCulture (vd. 2.0)} KB · {LastWriteTime:HH:mm dd/MM}" (nối "\n"), mới nhất trước, tối đa MaxArtifacts dòng. Chỉ tên, KHÔNG nội dung.
    /// Không có ⇒ "Chưa có file nào của {TaskId}."
    /// </summary>
    public string GetArtifacts()
        => throw new NotImplementedException(); // TODO(agent)
}
