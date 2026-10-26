using System.Text.Json.Nodes;

namespace AITeam.Core.Review;

/// <summary>Quyết định phân vai; ProviderId null với vai thủ công hoặc QC cũ; IsExplicit chỉ đúng khi có roles hợp lệ.</summary>
public sealed record WorkflowAssignment(string Role, string Actor, string? ProviderId, bool IsExplicit, bool ReadOnly, bool Manual);

/// <summary>Đọc Map dùng chung cho CLI và điều phối; không ghi cấu hình và không gọi tiến trình.</summary>
public static class WorkflowRoleDispatch
{
    /// <summary>
    /// Chỉ nhận role plan/prep/qc (Ordinal), ngoài ra ArgumentException("Vai trò không được hỗ trợ.").
    /// Đọc teamFile UTF-8 có/không BOM; không theo reparse point của file hoặc tổ tiên.
    /// Thiếu file/workflow/roles: plan/prep = codex, QC ProviderId=null để dùng router cũ; IsExplicit=false.
    /// Có roles: kiểm schema H7-01 đủ 11 mã duy nhất, actor hợp lệ, note chuỗi tối đa 500 UTF-16.
    /// Ánh xạ cố định codex→codex, minh/lan→agy; claude/gate/owner cần thủ công, không tự chạy Claude CLI.
    /// ReadOnly=true với plan/qc, false với prep. IsExplicit=true khi có roles hợp lệ.
    /// Schema sai ném InvalidDataException("Phân vai không hợp lệ."); JSON hỏng/root không object/lỗi đọc/
    /// reparse point ném IOException("Không đọc được cấu hình phân vai."). Không phản chiếu giá trị ngoài vào lỗi.
    /// </summary>
    public static WorkflowAssignment Resolve(string teamFile, string role)
    {
        if (role is not ("plan" or "prep" or "qc"))
            throw new ArgumentException("Vai trò không được hỗ trợ.");
        var legacy = new WorkflowAssignment(role, "codex", role == "qc" ? null : "codex", false, role != "prep", false);
        JsonNode? root;
        try
        {
            var fullPath = Path.GetFullPath(teamFile);
            FileSystemInfo? node = new FileInfo(fullPath);
            while (node != null)
            {
                if (node.LinkTarget != null || (node.Exists && (node.Attributes & FileAttributes.ReparsePoint) != 0))
                    throw new IOException();
                node = node is FileInfo ? new DirectoryInfo(Path.GetDirectoryName(fullPath)!) : ((DirectoryInfo)node).Parent;
            }
            if (!File.Exists(fullPath)) return legacy;
            var bytes = File.ReadAllBytes(fullPath);
            var offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            root = JsonNode.Parse(bytes.AsSpan(offset));
            if (root is not JsonObject) throw new IOException();
        }
        catch (Exception)
        {
            throw new IOException("Không đọc được cấu hình phân vai.");
        }
        var obj = (JsonObject)root;
        if (!obj.ContainsKey("workflow")) return legacy;
        if (obj["workflow"] is not JsonObject workflow) throw new InvalidDataException("Phân vai không hợp lệ.");
        if (!workflow.ContainsKey("roles")) return legacy;
        if (workflow["roles"] is not JsonArray roles || roles.Count != 11)
            throw new InvalidDataException("Phân vai không hợp lệ.");
        var codes = new HashSet<string>(["receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish"], StringComparer.Ordinal);
        var actors = new HashSet<string>(["claude", "codex", "minh", "lan", "gate", "owner"], StringComparer.Ordinal);
        string? selected = null;
        foreach (var row in roles)
        {
            if (row is not JsonObject item ||
                item["code"] is not JsonValue cv || !cv.TryGetValue<string>(out var code) || code == null || !codes.Remove(code) ||
                item["actor"] is not JsonValue av || !av.TryGetValue<string>(out var actor) || actor == null || !actors.Contains(actor) ||
                item["note"] is not JsonValue nv || !nv.TryGetValue<string>(out var note) || note == null || note.Length > 500)
                throw new InvalidDataException("Phân vai không hợp lệ.");
            if (code == role) selected = actor;
        }
        var provider = selected == "codex" ? "codex" : selected is "minh" or "lan" ? "agy" : null;
        return new WorkflowAssignment(role, selected!, provider, true, role != "prep", provider == null);
    }
}
