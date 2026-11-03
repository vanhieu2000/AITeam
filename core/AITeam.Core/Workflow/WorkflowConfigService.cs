using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AITeam.Core.Workflow;

/// <summary>
/// Đọc / ghi &lt;root&gt;/config/workflow.json (UTF-8 có BOM, camelCase, thụt lề 2).
/// Ghi bằng file tạm + đổi tên, khóa theo đường dẫn, chặn reparse point như WorkflowRolesService.
/// </summary>
public sealed class WorkflowConfigService : IWorkflowConfigService
{
    private const string Unclassified = "chua-phan-loai";
    private const int MaxTaskTypes = 30;
    private const int MaxSteps = 20;

    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex StepCodePattern = new("^[a-z][a-z0-9_]{1,31}$", RegexOptions.CultureInvariant);
    private static readonly Regex TaskTypeIdPattern = new("^[a-z][a-z0-9-]{1,31}$", RegexOptions.CultureInvariant);
    private static readonly Regex ActorPattern = new("^[a-z0-9][a-z0-9-]{1,31}$", RegexOptions.CultureInvariant);
    private static readonly Regex RulePattern = new("^L-[0-9]{3}$", RegexOptions.CultureInvariant);
    private static readonly Regex PrefixPattern = new("^([A-Za-z]+)", RegexOptions.CultureInvariant);
    private static readonly Regex TaskTypeLinePattern = new(
        "(?m)^[ \\t]*(?:-[ \\t]*)?\\*\\*Loại việc:\\*\\*[ \\t]*(?<value>.+?)[ \\t]*$", RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _skillsDir;
    private readonly string _file;

    public WorkflowConfigService(string aiTeamRoot)
    {
        if (string.IsNullOrWhiteSpace(aiTeamRoot)) throw new ArgumentException("Thiếu thư mục gốc AITeam.", nameof(aiTeamRoot));
        _skillsDir = Path.Combine(aiTeamRoot, "skills");
        _file = Path.Combine(aiTeamRoot, "config", "workflow.json");
        Root = aiTeamRoot;
    }

    /// <summary>Thư mục gốc AITeam dùng để tìm skills/.</summary>
    public string Root { get; }

    /// <inheritdoc />
    public WorkflowConfig Load()
    {
        if (HasReparsePoint(_file))
            throw new IOException("Không đọc được cấu hình quy trình.");

        if (!File.Exists(_file))
            return Defaults();

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(_file);
        }
        catch (Exception ex)
        {
            throw new IOException("Không đọc được cấu hình quy trình.", ex);
        }

        return Parse(bytes) with { Revision = Convert.ToHexString(SHA256.HashData(bytes)) };
    }

    /// <inheritdoc />
    public WorkflowValidation Validate(WorkflowConfig config)
    {
        var errors = new List<string>();
        if (config is null)
        {
            errors.Add("Cấu hình quy trình trống.");
            return new WorkflowValidation(false, errors);
        }

        var types = config.TaskTypes;
        if (types is null)
        {
            errors.Add("Cấu hình quy trình thiếu danh sách loại việc.");
            return new WorkflowValidation(false, errors);
        }

        if (types.Count == 0) errors.Add("Chưa có loại việc nào.");
        if (types.Count > MaxTaskTypes) errors.Add($"Có {types.Count} loại việc, tối đa {MaxTaskTypes}.");

        var knownSkills = new HashSet<string>(KnownSkills(), StringComparer.Ordinal);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var prefixOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in types)
        {
            if (type is null)
            {
                errors.Add("Có loại việc trống.");
                continue;
            }

            var tag = string.IsNullOrWhiteSpace(type.Id) ? "(không mã)" : type.Id;
            if (string.IsNullOrWhiteSpace(type.Id) || !TaskTypeIdPattern.IsMatch(type.Id))
                errors.Add($"Loại việc \"{tag}\": mã phải theo mẫu ^[a-z][a-z0-9-]{{1,31}}$.");
            else if (!seenIds.Add(type.Id))
                errors.Add($"Loại việc \"{tag}\": mã bị trùng.");

            if (string.IsNullOrWhiteSpace(type.Name)) errors.Add($"Loại việc \"{tag}\": tên không được rỗng.");
            else if (type.Name.Length > 40) errors.Add($"Loại việc \"{tag}\": tên dài {type.Name.Length} ký tự, tối đa 40.");

            foreach (var prefix in type.CodePrefixes ?? [])
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    errors.Add($"Loại việc \"{tag}\": tiền tố mã task rỗng.");
                }
                else if (prefixOwners.TryGetValue(prefix, out var other))
                {
                    errors.Add($"Tiền tố \"{prefix}\" bị trùng giữa loại việc \"{other}\" và \"{tag}\".");
                }
                else
                {
                    prefixOwners[prefix] = tag;
                }
            }

            var steps = type.Steps ?? [];
            if (steps.Count == 0) errors.Add($"Loại việc \"{tag}\": chưa có bước nào.");
            else if (steps.Count > MaxSteps) errors.Add($"Loại việc \"{tag}\": có {steps.Count} bước, tối đa {MaxSteps}.");

            var seenCodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in steps)
            {
                if (step is null)
                {
                    errors.Add($"Loại việc \"{tag}\": có bước trống.");
                    continue;
                }

                var code = step.Code ?? "";
                var where = string.IsNullOrWhiteSpace(code)
                    ? $"Loại việc \"{tag}\" bước (không mã)"
                    : $"Loại việc \"{tag}\" bước \"{code}\"";

                if (string.IsNullOrWhiteSpace(code) || !StepCodePattern.IsMatch(code))
                    errors.Add($"{where}: mã bước phải theo mẫu ^[a-z][a-z0-9_]{{1,31}}$.");
                else if (!seenCodes.Add(code))
                    errors.Add($"{where}: mã bước bị trùng trong loại việc.");

                if (string.IsNullOrWhiteSpace(step.Name)) errors.Add($"{where}: tên không được rỗng.");
                else if (step.Name.Length > 40) errors.Add($"{where}: tên dài {step.Name.Length} ký tự, tối đa 40.");

                if (!IsActor(step.Owner)) errors.Add($"{where}: người phụ trách \"{step.Owner}\" sai mẫu.");
                if (step.Fallback is not null)
                {
                    if (!IsActor(step.Fallback)) errors.Add($"{where}: AI dự phòng \"{step.Fallback}\" sai mẫu.");
                    else if (string.Equals(step.Fallback, step.Owner, StringComparison.Ordinal))
                        errors.Add($"{where}: AI dự phòng trùng người phụ trách.");
                }

                foreach (var skill in step.Skills ?? [])
                {
                    if (string.IsNullOrWhiteSpace(skill) || skill.Contains("..", StringComparison.Ordinal) ||
                        skill.Contains('/') || skill.Contains('\\'))
                        errors.Add($"{where}: tên skill \"{skill}\" không hợp lệ.");
                    else if (!knownSkills.Contains(skill))
                        errors.Add($"{where}: skill \"{skill}\" không có trong skills/.");
                }

                foreach (var rule in step.Rules ?? [])
                {
                    if (string.IsNullOrWhiteSpace(rule) || !RulePattern.IsMatch(rule))
                        errors.Add($"{where}: mã luật \"{rule}\" phải theo mẫu L-xxx.");
                }

                if (step.MaxMinutes is < 1 or > 240)
                    errors.Add($"{where}: giới hạn phút {step.MaxMinutes} ngoài khoảng 1–240.");
                if (step.MaxRounds is < 1 or > 5)
                    errors.Add($"{where}: giới hạn vòng {step.MaxRounds} ngoài khoảng 1–5.");
            }
        }

        return new WorkflowValidation(errors.Count == 0, errors);
    }

    /// <inheritdoc />
    public WorkflowConfig Save(WorkflowConfig config, string expectedRevision)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        expectedRevision ??= string.Empty;
        var validation = Validate(config);
        if (!validation.Ok)
            throw new ArgumentException(string.Join(" ", validation.Errors), nameof(config));

        lock (GetLock(_file))
        {
            if (HasReparsePoint(_file))
                throw new IOException("Không lưu được cấu hình quy trình.");

            bool exists = File.Exists(_file);
            bool hasBom = true;

            if (exists)
            {
                byte[] current;
                try
                {
                    current = File.ReadAllBytes(_file);
                }
                catch (Exception ex)
                {
                    throw new IOException("Không đọc được cấu hình quy trình.", ex);
                }

                hasBom = current.Length >= 3 && current[0] == 0xEF && current[1] == 0xBB && current[2] == 0xBF;
                if (!string.Equals(CurrentRevision(current), expectedRevision, StringComparison.OrdinalIgnoreCase))
                    throw Changed();
            }
            else if (!string.IsNullOrEmpty(expectedRevision))
            {
                throw Changed();
            }

            string dir = Path.GetDirectoryName(_file)!;
            string tempFile = Path.Combine(dir, $"workflow.json.tmp.{Guid.NewGuid():N}");
            byte[] writtenBytes;

            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = ToJson(config);
                using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(fs, new UTF8Encoding(hasBom), bufferSize: 1024, leaveOpen: true))
                    {
                        writer.Write(json);
                        writer.Flush();
                    }
                    fs.Flush(true);
                }

                writtenBytes = File.ReadAllBytes(tempFile);

                if (exists)
                {
                    byte[] check = File.ReadAllBytes(_file);
                    if (!string.Equals(CurrentRevision(check), expectedRevision, StringComparison.OrdinalIgnoreCase))
                        throw Changed();

                    File.Replace(tempFile, _file, null);
                }
                else
                {
                    if (File.Exists(_file)) throw Changed();
                    File.Move(tempFile, _file);
                }
            }
            catch (InvalidOperationException)
            {
                TryDelete(tempFile);
                throw;
            }
            catch (Exception ex)
            {
                TryDelete(tempFile);
                throw new IOException("Không lưu được cấu hình quy trình.", ex);
            }

            return config with { Revision = CurrentRevision(writtenBytes) };
        }
    }

    /// <inheritdoc />
    public TaskType Resolve(string taskId, string? taskText)
    {
        var types = Load().TaskTypes ?? [];

        var lineValue = ReadTaskTypeLine(taskText);
        if (lineValue is not null)
        {
            var typed = types.FirstOrDefault(t => t is not null &&
                (string.Equals(t.Id, lineValue, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.Name, lineValue, StringComparison.OrdinalIgnoreCase)));
            if (typed is not null) return typed;
        }

        var prefix = LeadingPrefix(taskId);
        if (prefix is not null)
        {
            var byPrefix = types.FirstOrDefault(t => t?.CodePrefixes is not null &&
                t.CodePrefixes.Any(p => string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase)));
            if (byPrefix is not null) return byPrefix;
        }

        var unclassified = types.FirstOrDefault(t => t is not null && string.Equals(t.Id, Unclassified, StringComparison.OrdinalIgnoreCase));
        if (unclassified is not null) return unclassified;

        var core = types.FirstOrDefault(t => t is not null && string.Equals(t.Id, "tinh-nang-loi", StringComparison.OrdinalIgnoreCase))
            ?? Defaults().TaskTypes.First(t => t.Id == "tinh-nang-loi");
        return new TaskType(Unclassified, "Chưa phân loại", [], core.Steps);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> KnownSkills()
    {
        if (!Directory.Exists(_skillsDir)) return [];
        try
        {
            return Directory.GetDirectories(_skillsDir)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray()!;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static WorkflowConfig Defaults() => new(1,
    [
        Type("giao-dien", "Tính năng có giao diện", ["H"],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("thiet_ke", "Thiết kế", "claude", null, []),
            Step("mockup_duyet", "Duyệt mockup", "owner", null, [], approval: true),
            Step("chuan_bi", "Chuẩn bị", "codex", null, ["06-chuan-bi-task"], minutes: 25),
            Step("code_loi", "Code lõi", "codex", "opencode", ["07-code", "code-test"], minutes: 30, rounds: 3),
            Step("giao_dien", "Giao diện", "claude", null, ["code-web"]),
            Step("cong_kt", "Cổng kiểm tra", "gate", null, ["08-kiem-thu"]),
            Step("qc", "QC", "claude", null, ["09-qc"]),
            Step("nghiem_thu", "Nghiệm thu", "claude", null, ["10-nghiem-thu"]),
        ]),
        Type("tinh-nang-loi", "Tính năng lõi / CLI", ["V", "O"],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("ke_hoach", "Kế hoạch", "claude", null, ["04-ke-hoach"]),
            Step("chuan_bi", "Chuẩn bị", "codex", null, ["06-chuan-bi-task"], minutes: 25),
            Step("code", "Code", "codex", "opencode", ["07-code", "code-test"], minutes: 30, rounds: 3),
            Step("cong_kt", "Cổng kiểm tra", "gate", null, ["08-kiem-thu"]),
            Step("qc", "QC", "claude", null, ["09-qc"]),
            Step("nghiem_thu", "Nghiệm thu", "claude", null, ["10-nghiem-thu"]),
        ]),
        Type("sua-loi", "Sửa lỗi", [],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("chan_doan", "Chẩn đoán", "claude", null, []),
            Step("code", "Code", "opencode", "codex", ["07-code", "code-test"], minutes: 30, rounds: 3),
            Step("cong_kt", "Cổng kiểm tra", "gate", null, ["08-kiem-thu"]),
            Step("qc", "QC", "claude", null, ["09-qc"]),
            Step("nghiem_thu", "Nghiệm thu", "claude", null, ["10-nghiem-thu"]),
        ]),
        Type("tai-lieu", "Tài liệu / dữ liệu", ["D"],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("code", "Code", "opencode", null, ["07-code", "code-test"], minutes: 30, rounds: 3),
            Step("qc", "QC", "claude", null, ["09-qc"]),
        ]),
        Type("nghien-cuu", "Nghiên cứu (chỉ đọc)", ["R"],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("nghien_cuu", "Nghiên cứu", "codex", "opencode", ["03-research"]),
            Step("tong_hop", "Tổng hợp", "claude", null, []),
        ]),
        Type("bao-mat", "Bảo mật", [],
        [
            Step("tiep_nhan", "Tiếp nhận", "claude", null, ["01-tiep-nhan"]),
            Step("ke_hoach", "Kế hoạch", "claude", null, ["04-ke-hoach"]),
            Step("chuan_bi", "Chuẩn bị", "codex", null, ["06-chuan-bi-task"], minutes: 25),
            Step("code", "Code", "codex", "opencode", ["07-code", "code-test"], minutes: 30, rounds: 3),
            Step("cong_kt", "Cổng kiểm tra", "gate", null, ["08-kiem-thu"]),
            Step("qc", "QC", "claude", null, ["09-qc"]),
            Step("duyet_bao_mat", "Duyệt bảo mật", "owner", null, [], approval: true),
            Step("nghiem_thu", "Nghiệm thu", "claude", null, ["10-nghiem-thu"]),
        ]),
    ], "");

    private static TaskType Type(string id, string name, string[] prefixes, WorkflowStep[] steps) =>
        new(id, name, prefixes, steps);

    private static WorkflowStep Step(
        string code,
        string name,
        string owner,
        string? fallback,
        string[] skills,
        int? minutes = null,
        int? rounds = null,
        bool approval = false) =>
        new(code, name, owner, fallback, skills, [], minutes, rounds, approval);

    private static WorkflowConfig Parse(byte[] bytes)
    {
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        ReadOnlySpan<byte> json = hasBom ? bytes.AsSpan(3) : bytes.AsSpan();

        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            long line = (ex.LineNumber ?? 0) + 1;
            throw new InvalidDataException($"Cấu hình quy trình hỏng ở dòng {line}: không đọc được JSON.", ex);
        }

        if (rootNode is not JsonObject root)
            throw new InvalidDataException("Cấu hình quy trình hỏng tại gốc: nội dung phải là object.");

        int version = ReadInt(root, "version", "gốc") ?? 1;

        if (root["taskTypes"] is not JsonArray typeArray)
            throw new InvalidDataException("Cấu hình quy trình hỏng tại gốc: thiếu mảng \"taskTypes\".");

        var types = new List<TaskType>();
        for (int i = 0; i < typeArray.Count; i++)
        {
            string where = $"taskTypes[{i}]";
            if (typeArray[i] is not JsonObject typeObj)
                throw new InvalidDataException($"Cấu hình quy trình hỏng tại {where}: loại việc phải là object.");

            string id = ReadString(typeObj, "id", where) ?? throw Missing(where, "id");
            string name = ReadString(typeObj, "name", where) ?? throw Missing(where, "name");
            var prefixes = ReadStringArray(typeObj, "codePrefixes", where, required: false);

            if (typeObj["steps"] is not JsonArray stepArray)
                throw new InvalidDataException($"Cấu hình quy trình hỏng tại {where}: thiếu mảng \"steps\".");

            var steps = new List<WorkflowStep>();
            for (int j = 0; j < stepArray.Count; j++)
            {
                string stepWhere = $"{where}.steps[{j}]";
                if (stepArray[j] is not JsonObject stepObj)
                    throw new InvalidDataException($"Cấu hình quy trình hỏng tại {stepWhere}: bước phải là object.");

                steps.Add(new WorkflowStep(
                    ReadString(stepObj, "code", stepWhere) ?? throw Missing(stepWhere, "code"),
                    ReadString(stepObj, "name", stepWhere) ?? throw Missing(stepWhere, "name"),
                    ReadString(stepObj, "owner", stepWhere) ?? throw Missing(stepWhere, "owner"),
                    ReadString(stepObj, "fallback", stepWhere),
                    ReadStringArray(stepObj, "skills", stepWhere, required: true),
                    ReadStringArray(stepObj, "rules", stepWhere, required: false),
                    ReadInt(stepObj, "maxMinutes", stepWhere),
                    ReadInt(stepObj, "maxRounds", stepWhere),
                    ReadBool(stepObj, "ownerApproval", stepWhere) ?? false));
            }

            types.Add(new TaskType(id, name, prefixes, steps));
        }

        return new WorkflowConfig(version, types, "");
    }

    private static string ToJson(WorkflowConfig config)
    {
        var types = new JsonArray();
        foreach (var type in config.TaskTypes)
        {
            var prefixes = new JsonArray();
            foreach (var prefix in type.CodePrefixes) prefixes.Add(prefix);

            var steps = new JsonArray();
            foreach (var step in type.Steps)
            {
                steps.Add(new JsonObject
                {
                    ["code"] = step.Code,
                    ["name"] = step.Name,
                    ["owner"] = step.Owner,
                    ["fallback"] = step.Fallback is null ? null : JsonValue.Create(step.Fallback),
                    ["skills"] = ToArray(step.Skills),
                    ["rules"] = ToArray(step.Rules),
                    ["maxMinutes"] = step.MaxMinutes is null ? null : JsonValue.Create(step.MaxMinutes.Value),
                    ["maxRounds"] = step.MaxRounds is null ? null : JsonValue.Create(step.MaxRounds.Value),
                    ["ownerApproval"] = step.OwnerApproval
                });
            }

            types.Add(new JsonObject
            {
                ["id"] = type.Id,
                ["name"] = type.Name,
                ["codePrefixes"] = prefixes,
                ["steps"] = steps
            });
        }

        return new JsonObject
        {
            ["version"] = config.Version,
            ["taskTypes"] = types
        }.ToJsonString(JsonOptions);
    }

    private static JsonArray ToArray(IReadOnlyList<string>? values)
    {
        var array = new JsonArray();
        if (values is null) return array;
        foreach (var value in values) array.Add(value);
        return array;
    }

    private static JsonValue? Node(JsonObject obj, string property) =>
        obj.TryGetPropertyValue(property, out var node) ? node as JsonValue : null;

    private static string? ReadString(JsonObject obj, string property, string where)
    {
        var node = Node(obj, property);
        if (node is null) return null;
        if (node.GetValueKind() == JsonValueKind.Null) return null;
        if (node.GetValueKind() != JsonValueKind.String) throw WrongType(where, property, "chuỗi");
        return node.GetValue<string>();
    }

    private static int? ReadInt(JsonObject obj, string property, string where)
    {
        var node = Node(obj, property);
        if (node is null) return null;
        var kind = node.GetValueKind();
        if (kind == JsonValueKind.Null) return null;
        if (kind != JsonValueKind.Number || !node.TryGetValue<int>(out var value))
            throw WrongType(where, property, "số nguyên");
        return value;
    }

    private static bool? ReadBool(JsonObject obj, string property, string where)
    {
        var node = Node(obj, property);
        if (node is null) return null;
        var kind = node.GetValueKind();
        if (kind == JsonValueKind.Null) return null;
        if (kind is not (JsonValueKind.True or JsonValueKind.False))
            throw WrongType(where, property, "true/false");
        return node.GetValue<bool>();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject obj, string property, string where, bool required)
    {
        if (!obj.TryGetPropertyValue(property, out var node) || node is null)
        {
            if (required) throw new InvalidDataException($"Cấu hình quy trình hỏng tại {where}: thiếu mảng \"{property}\".");
            return [];
        }

        if (node is not JsonArray array)
            throw WrongType(where, property, "mảng chuỗi");

        var values = new List<string>();
        foreach (var item in array)
        {
            if (item is not JsonValue value || value.GetValueKind() != JsonValueKind.String)
                throw new InvalidDataException($"Cấu hình quy trình hỏng tại {where}: \"{property}\" phải toàn chuỗi.");
            values.Add(value.GetValue<string>());
        }
        return values;
    }

    private static InvalidDataException Missing(string where, string property) =>
        new($"Cấu hình quy trình hỏng tại {where}: thiếu \"{property}\".");

    private static InvalidDataException WrongType(string where, string property, string expected) =>
        new($"Cấu hình quy trình hỏng tại {where}: \"{property}\" phải là {expected}.");

    private static bool IsActor(string? actor) => actor is not null && ActorPattern.IsMatch(actor);

    private static string? ReadTaskTypeLine(string? taskText)
    {
        if (string.IsNullOrWhiteSpace(taskText)) return null;
        var match = TaskTypeLinePattern.Match(taskText);
        if (!match.Success) return null;
        var value = match.Groups["value"].Value.Trim();
        return value.Length == 0 ? null : value;
    }

    private static string? LeadingPrefix(string? taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return null;
        var match = PrefixPattern.Match(taskId.Trim());
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string CurrentRevision(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static InvalidOperationException Changed() =>
        new("Cấu hình đã bị sửa ở nơi khác. Vui lòng tải lại trước khi lưu.");

    private static object GetLock(string path) => Locks.GetOrAdd(Path.GetFullPath(path), _ => new object());

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static bool HasReparsePoint(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (file.LinkTarget != null || (file.Exists && (file.Attributes & FileAttributes.ReparsePoint) != 0))
                return true;

            var dir = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(dir))
            {
                var info = new DirectoryInfo(dir);
                if (info.LinkTarget != null || (info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0))
                    return true;
                dir = info.Parent?.FullName;
            }
            return false;
        }
        catch
        {
            return true;
        }
    }
}
