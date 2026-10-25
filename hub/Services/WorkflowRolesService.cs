using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AITeam.Hub.Services;

/// <summary>Một dòng phân vai. Code và Actor phân biệt hoa thường; Note không null, tối đa 500 đơn vị UTF-16, giữ nguyên văn.</summary>
public sealed record WorkflowRole(string Code, string Actor, string Note);

/// <summary>11 dòng theo thứ tự chuẩn; Revision = SHA-256 hex toàn bộ byte file, hoặc "missing" nếu chưa có file.</summary>
public sealed record WorkflowRolesSnapshot(IReadOnlyList<WorkflowRole> Roles, string Revision);

/// <summary>Hợp đồng riêng cho Map; không thay Contracts.cs hoặc HubModels.cs.</summary>
public interface IWorkflowRolesService
{
    /// <summary>
    /// Không ghi file. Thiếu file/workflow/roles trả mặc định v4; null/rỗng/sai kiểu không phải thiếu.
    /// Mã theo thứ tự: receive, idea, research, plan, approve_plan, prep, code, gate, qc, accept, publish.
    /// Tác nhân: claude, codex, minh, lan, gate (Cổng KT), owner (Chủ dự án).
    /// Mặc định claude: receive/idea/approve_plan/accept/publish; codex: research/plan/prep/qc;
    /// minh: code; gate: gate; Note = "". JSON roles là mảng object {code, actor, note} chữ thường.
    /// Sai dữ liệu ném InvalidDataException("Phân vai không hợp lệ."). JSON hỏng, root không là object,
    /// lỗi đọc hoặc reparse point ném IOException("Không đọc được cấu hình phân vai.").
    /// Không đưa dữ liệu gốc vào thông báo. Không theo reparse point của file hoặc thư mục tổ tiên.
    /// </summary>
    WorkflowRolesSnapshot GetRoles();

    /// <summary>
    /// Đủ 11 mã duy nhất và tác nhân hợp lệ; Note không null, Length &lt;= 500. roles null, dòng null,
    /// mã lạ/trùng/thiếu hoặc revision null/rỗng ném InvalidDataException("Phân vai không hợp lệ.").
    /// Chỉ cập nhật workflow.roles; giữ khóa lạ kể cả workflow.*, giữ BOM; file mới không BOM.
    /// Dùng khóa chung theo đường dẫn chuẩn hóa với SaveSettings, đọc lại trong khóa.
    /// Revision khác byte hiện tại ném InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.").
    /// JSON hỏng ném IOException("Không đọc được cấu hình phân vai."). Ghi tạm tên duy nhất cùng thư mục,
    /// flush rồi kiểm tra lại revision và thay nguyên tử (file mới move không ghi đè).
    /// Lỗi ghi/quyền/reparse point ném IOException("Không lưu được cấu hình phân vai."); giữ file cũ, dọn tạm.
    /// Không theo reparse point của file/thư mục tổ tiên. Trả snapshot mới. Không ghi team.local.json.
    /// </summary>
    WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision);
}

internal static class TeamFileLock
{
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);

    public static object GetLock(string path)
    {
        return Locks.GetOrAdd(Path.GetFullPath(path), _ => new object());
    }

    public static bool HasReparsePoint(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var fi = new FileInfo(fullPath);
            if (fi.LinkTarget != null || (fi.Exists && (fi.Attributes & FileAttributes.ReparsePoint) != 0))
            {
                return true;
            }

            var dir = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(dir))
            {
                var di = new DirectoryInfo(dir);
                if (di.LinkTarget != null || (di.Exists && (di.Attributes & FileAttributes.ReparsePoint) != 0))
                {
                    return true;
                }
                var parent = di.Parent;
                dir = parent?.FullName;
            }
            return false;
        }
        catch
        {
            return true;
        }
    }
}

/// <summary>Singleton; mọi instance cùng file phối hợp với đường ghi Settings.</summary>
public sealed class WorkflowRolesService(HubPaths paths) : IWorkflowRolesService
{
    private static readonly string[] StandardCodes =
    [
        "receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish"
    ];

    private static readonly HashSet<string> StandardCodesSet = new(StandardCodes, StringComparer.Ordinal);

    private static readonly HashSet<string> ValidActorsSet = new(StringComparer.Ordinal)
    {
        "claude", "codex", "minh", "lan", "gate", "owner"
    };

    private static readonly Dictionary<string, string> DefaultActors = new()
    {
        ["receive"] = "claude",
        ["idea"] = "claude",
        ["research"] = "codex",
        ["plan"] = "codex",
        ["approve_plan"] = "claude",
        ["prep"] = "codex",
        ["code"] = "minh",
        ["gate"] = "gate",
        ["qc"] = "codex",
        ["accept"] = "claude",
        ["publish"] = "claude"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static IReadOnlyList<WorkflowRole> CreateDefaultRoles() =>
        StandardCodes.Select(c => new WorkflowRole(c, DefaultActors[c], "")).ToArray();

    private static bool TryGetString(JsonObject obj, string propertyName, out string value)
    {
        value = string.Empty;
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
            return false;
        if (node is not JsonValue jsonValue)
            return false;
        if (jsonValue.GetValueKind() != JsonValueKind.String)
            return false;
        return jsonValue.TryGetValue(out value!) && value != null;
    }

    /// <inheritdoc />
    public WorkflowRolesSnapshot GetRoles()
    {
        if (TeamFileLock.HasReparsePoint(paths.TeamJson))
            throw new IOException("Không đọc được cấu hình phân vai.");

        if (!File.Exists(paths.TeamJson))
            return new WorkflowRolesSnapshot(CreateDefaultRoles(), "missing");

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(paths.TeamJson);
        }
        catch (Exception ex)
        {
            throw new IOException("Không đọc được cấu hình phân vai.", ex);
        }

        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        ReadOnlySpan<byte> jsonSpan = hasBom ? bytes.AsSpan(3) : bytes.AsSpan();

        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(jsonSpan);
        }
        catch (Exception ex)
        {
            throw new IOException("Không đọc được cấu hình phân vai.", ex);
        }

        if (rootNode is not JsonObject rootObj)
            throw new IOException("Không đọc được cấu hình phân vai.");

        string revision = Convert.ToHexString(SHA256.HashData(bytes));

        if (!rootObj.ContainsKey("workflow"))
            return new WorkflowRolesSnapshot(CreateDefaultRoles(), revision);

        var workflowNode = rootObj["workflow"];
        if (workflowNode is not JsonObject workflowObj)
            throw new InvalidDataException("Phân vai không hợp lệ.");

        if (!workflowObj.ContainsKey("roles"))
            return new WorkflowRolesSnapshot(CreateDefaultRoles(), revision);

        var rolesNode = workflowObj["roles"];
        if (rolesNode is not JsonArray rolesArr || rolesArr.Count != 11)
            throw new InvalidDataException("Phân vai không hợp lệ.");

        var parsed = new Dictionary<string, WorkflowRole>(StringComparer.Ordinal);
        foreach (var item in rolesArr)
        {
            if (item is not JsonObject rowObj)
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (!TryGetString(rowObj, "code", out var code) ||
                !TryGetString(rowObj, "actor", out var actor) ||
                !TryGetString(rowObj, "note", out var note))
            {
                throw new InvalidDataException("Phân vai không hợp lệ.");
            }

            if (!StandardCodesSet.Contains(code) || parsed.ContainsKey(code))
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (!ValidActorsSet.Contains(actor))
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (note.Length > 500)
                throw new InvalidDataException("Phân vai không hợp lệ.");

            parsed[code] = new WorkflowRole(code, actor, note);
        }

        var orderedRoles = StandardCodes.Select(c => parsed[c]).ToArray();
        return new WorkflowRolesSnapshot(orderedRoles, revision);
    }

    /// <inheritdoc />
    public WorkflowRolesSnapshot SaveRoles(IReadOnlyList<WorkflowRole> roles, string expectedRevision)
    {
        if (roles == null || string.IsNullOrEmpty(expectedRevision) || roles.Count != 11)
            throw new InvalidDataException("Phân vai không hợp lệ.");

        var roleMap = new Dictionary<string, WorkflowRole>(StringComparer.Ordinal);
        foreach (var r in roles)
        {
            if (r == null || r.Code == null || r.Actor == null || r.Note == null)
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (!StandardCodesSet.Contains(r.Code) || roleMap.ContainsKey(r.Code))
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (!ValidActorsSet.Contains(r.Actor))
                throw new InvalidDataException("Phân vai không hợp lệ.");

            if (r.Note.Length > 500)
                throw new InvalidDataException("Phân vai không hợp lệ.");

            roleMap[r.Code] = r;
        }

        var orderedRoles = StandardCodes.Select(c => roleMap[c]).ToArray();

        lock (TeamFileLock.GetLock(paths.TeamJson))
        {
            if (TeamFileLock.HasReparsePoint(paths.TeamJson))
                throw new IOException("Không lưu được cấu hình phân vai.");

            bool fileExists = File.Exists(paths.TeamJson);
            bool hasBom = false;
            JsonObject rootObj;

            if (fileExists)
            {
                byte[] currentBytes;
                try
                {
                    currentBytes = File.ReadAllBytes(paths.TeamJson);
                }
                catch (Exception ex)
                {
                    throw new IOException("Không đọc được cấu hình phân vai.", ex);
                }

                hasBom = currentBytes.Length >= 3 && currentBytes[0] == 0xEF && currentBytes[1] == 0xBB && currentBytes[2] == 0xBF;
                ReadOnlySpan<byte> jsonSpan = hasBom ? currentBytes.AsSpan(3) : currentBytes.AsSpan();

                // Kiểm tra JSON hỏng trước kiểm tra revision
                JsonNode? parsed;
                try
                {
                    parsed = JsonNode.Parse(jsonSpan);
                }
                catch (Exception ex)
                {
                    throw new IOException("Không đọc được cấu hình phân vai.", ex);
                }

                if (parsed is not JsonObject obj)
                    throw new IOException("Không đọc được cấu hình phân vai.");

                rootObj = obj;

                string currentRevision = Convert.ToHexString(SHA256.HashData(currentBytes));
                if (!string.Equals(currentRevision, expectedRevision, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.");
            }
            else
            {
                if (!string.Equals(expectedRevision, "missing", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.");

                rootObj = new JsonObject();
            }

            JsonObject workflowObj;
            if (rootObj.ContainsKey("workflow"))
            {
                var wfNode = rootObj["workflow"];
                if (wfNode is not JsonObject wfObj)
                    throw new InvalidDataException("Phân vai không hợp lệ.");
                workflowObj = wfObj;
            }
            else
            {
                workflowObj = new JsonObject();
                rootObj["workflow"] = workflowObj;
            }

            var rolesArr = new JsonArray();
            foreach (var r in orderedRoles)
            {
                rolesArr.Add(new JsonObject
                {
                    ["code"] = r.Code,
                    ["actor"] = r.Actor,
                    ["note"] = r.Note
                });
            }
            workflowObj["roles"] = rolesArr;

            string dir = Path.GetDirectoryName(paths.TeamJson)!;
            string tempFile = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, $"team.json.tmp.{Guid.NewGuid():N}");
            byte[] writtenBytes;

            try
            {
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = rootObj.ToJsonString(JsonOptions);
                var encoding = new UTF8Encoding(hasBom);

                using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(fs, encoding, bufferSize: 1024, leaveOpen: true))
                    {
                        writer.Write(json);
                        writer.Flush();
                    }
                    fs.Flush(true);
                }

                writtenBytes = File.ReadAllBytes(tempFile);

                if (fileExists)
                {
                    byte[] checkBytes = File.ReadAllBytes(paths.TeamJson);
                    string checkRevision = Convert.ToHexString(SHA256.HashData(checkBytes));
                    if (!string.Equals(checkRevision, expectedRevision, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.");

                    File.Replace(tempFile, paths.TeamJson, null);
                }
                else
                {
                    if (File.Exists(paths.TeamJson))
                        throw new InvalidOperationException("Cấu hình đã thay đổi. Vui lòng tải lại.");

                    File.Move(tempFile, paths.TeamJson);
                }
            }
            catch (InvalidOperationException)
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                throw new IOException("Không lưu được cấu hình phân vai.", ex);
            }

            string newRevision = Convert.ToHexString(SHA256.HashData(writtenBytes));
            return new WorkflowRolesSnapshot(orderedRoles, newRevision);
        }
    }
}
