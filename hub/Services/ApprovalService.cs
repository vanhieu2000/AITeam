using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public interface IApprovalService
{
    /// <summary>Mọi approvals/A-*.json đọc được (file hỏng / thiếu id / id không khớp tên file ⇒ bỏ qua).
    /// Sắp xếp: Pending trước; trong mỗi nhóm RequestedAt mới nhất trước. Thư mục không có ⇒ rỗng.</summary>
    IReadOnlyList<Approval> GetAll();

    /// <summary>Theo id (phải khớp ^A-\d{3}$, sai ⇒ null). Không có / hỏng ⇒ null.</summary>
    Approval? Get(string id);

    /// <summary>
    /// Ghi quyết định. Lỗi:
    /// - id sai mẫu ^A-\d{3}$ ⇒ ArgumentException (chặn "..\x", "A-001.json", "A-1");
    /// - không có file ⇒ KeyNotFoundException;
    /// - Status khác Pending ⇒ InvalidOperationException (không đổi quyết định đã ghi);
    /// - note dài hơn 500 ký tự ⇒ ArgumentException.
    /// Ghi: status = "approved" / "rejected", decidedAt = giờ địa phương "yyyy-MM-ddTHH:mm:ss", note = note.Trim() (rỗng ⇒ null).
    /// Mọi trường khác giữ nguyên (sửa bằng JsonNode, không serialize lại từ record).
    /// Ghi file tạm cùng thư mục rồi File.Move(overwrite: true); UTF-8 không BOM; thụt lề 2 dấu cách.
    /// Trả Approval sau khi ghi.
    /// </summary>
    Approval Decide(string id, bool approve, string? note);
}

/// <summary>Đọc / ghi &lt;AITeamRoot&gt;/approvals/*.json. Enum đọc không phân biệt hoa thường; giá trị lạ ⇒ bỏ qua file.</summary>
public sealed class ApprovalService : IApprovalService
{
    private static readonly Regex IdRegex = new(@"^A-\d{3}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HubPaths _paths;

    public ApprovalService(HubPaths paths) => _paths = paths;

    public string Folder => Path.Combine(_paths.AITeamRoot, "approvals");

    public IReadOnlyList<Approval> GetAll()
    {
        if (!Directory.Exists(Folder)) return Array.Empty<Approval>();
        var list = new List<Approval>();
        foreach (var file in Directory.GetFiles(Folder, "A-*.json"))
        {
            var fileId = Path.GetFileNameWithoutExtension(file);
            if (!IdRegex.IsMatch(fileId)) continue;
            var a = TryRead(file, fileId);
            if (a != null) list.Add(a);
        }
        return list.OrderBy(a => a.Status == ApprovalStatus.Pending ? 0 : 1).ThenByDescending(a => a.RequestedAt).ToList();
    }


    public Approval? Get(string id)
    {
        if (string.IsNullOrEmpty(id) || !IdRegex.IsMatch(id) || !Directory.Exists(Folder)) return null;
        var path = Path.Combine(Folder, id + ".json");
        return File.Exists(path) ? TryRead(path, id) : null;
    }


    public Approval Decide(string id, bool approve, string? note)
    {
        if (string.IsNullOrEmpty(id) || !IdRegex.IsMatch(id))
            throw new ArgumentException("Mã duyệt không hợp lệ.", nameof(id));
        if (note is { Length: > 500 })
            throw new ArgumentException("Ghi chú không được vượt quá 500 ký tự.", nameof(note));

        var path = Path.Combine(Folder, id + ".json");
        if (!File.Exists(path))
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu duyệt {id}.");

        var obj = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) as JsonObject
            ?? throw new InvalidOperationException($"Dữ liệu {id} không hợp lệ.");
        var current = ParseNode(obj, id)
            ?? throw new InvalidOperationException($"Dữ liệu {id} không hợp lệ.");
        if (current.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Yêu cầu {id} đã được quyết định trước đó.");

        var now = DateTime.Now;
        var cleanNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        obj["status"] = approve ? "approved" : "rejected";
        obj["decidedAt"] = now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        obj["note"] = cleanNote;

        var tmp = Path.Combine(Folder, $"{id}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tmp, obj.ToJsonString(WriteOpts), new UTF8Encoding(false));
        File.Move(tmp, path, overwrite: true);

        return ParseNode(obj, id)!;
    }

    private static Approval? TryRead(string path, string expectedId)
    {
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) as JsonObject;
            return obj == null ? null : ParseNode(obj, expectedId);
        }
        catch { return null; }
    }

    private static Approval? ParseNode(JsonObject obj, string expectedId)
    {
        var id = (string?)obj["id"];
        if (string.IsNullOrEmpty(id) || id != expectedId || !IdRegex.IsMatch(id)) return null;
        var kindStr = (string?)obj["kind"];
        if (string.IsNullOrEmpty(kindStr) || !Enum.TryParse<ApprovalKind>(kindStr, true, out var kind) || !Enum.IsDefined(kind)) return null;
        var statusStr = (string?)obj["status"];
        if (string.IsNullOrEmpty(statusStr) || !Enum.TryParse<ApprovalStatus>(statusStr, true, out var status) || !Enum.IsDefined(status)) return null;
        var reqAtStr = (string?)obj["requestedAt"];
        if (string.IsNullOrEmpty(reqAtStr) || !DateTime.TryParse(reqAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var reqAt)) return null;

        DateTime? decAt = null;
        var decAtStr = (string?)obj["decidedAt"];
        if (!string.IsNullOrEmpty(decAtStr) && DateTime.TryParse(decAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDec))
            decAt = parsedDec;

        var links = obj["links"] is JsonArray arr
            ? arr.Select(x => (string?)x).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList()
            : (IReadOnlyList<string>)Array.Empty<string>();

        return new Approval(id, kind, (string?)obj["title"] ?? "", (string?)obj["detail"] ?? "",
            (string?)obj["project"] ?? "", (string?)obj["task"], (string?)obj["commit"], links,
            (string?)obj["requestedBy"] ?? "", reqAt, status, decAt, (string?)obj["note"]);
    }

}
