using System.Text.Json;
using System.Text.Json.Nodes;
using AITeam.Core.Process;

namespace AITeam.Core.Providers;

/// <summary>Cấu hình 1 provider (team.json → "providers"; team.local.json ghi đè theo Id, ví dụ đường dẫn exe trên máy khác).</summary>
public sealed record ProviderConfig
{
    public string Id { get; init; } = "";
    public ProviderKind Kind { get; init; }
    public string Exe { get; init; } = "";
    public string? NodeExe { get; init; }
    public string? Model { get; init; }
    public IReadOnlyList<AgentRole> Roles { get; init; } = [];
    public int MaxConcurrent { get; init; } = 1;
    public int TimeoutMinutes { get; init; } = 30;
    public bool Enabled { get; init; } = true;
    public bool Paid { get; init; }
    public bool PrivateRepository { get; init; } = true;
    public string? PermissionsFile { get; init; }
}

/// <summary>Danh sách provider + thứ tự ưu tiên theo vai trò (routing / fallback).</summary>
public sealed record ProviderSettings(IReadOnlyList<ProviderConfig> Providers, IReadOnlyDictionary<AgentRole, IReadOnlyList<string>> Routing)
{
    /// <summary>Mặc định khi team.json chưa có mục "providers": agy viết code (2 job), Codex review chỉ đọc.</summary>
    public static ProviderSettings Default(string agyExe) => new(
        [
            new ProviderConfig { Id = "agy", Kind = ProviderKind.Antigravity, Exe = agyExe, Roles = [AgentRole.Developer], MaxConcurrent = 2, TimeoutMinutes = 30 },
            new ProviderConfig { Id = "codex", Kind = ProviderKind.Codex, Exe = "codex", Model = "gpt-6.1-sol", Roles = [AgentRole.Reviewer, AgentRole.Developer], MaxConcurrent = 1, TimeoutMinutes = 15 },
        ],
        new Dictionary<AgentRole, IReadOnlyList<string>>
        {
            [AgentRole.Developer] = ["agy", "codex"],
            [AgentRole.Reviewer] = ["codex"],
        });

    /// <summary>Đọc team.json (+ team.local.json ghi đè). Không có mục "providers" ⇒ mặc định, vẫn dùng agy.exe trong team.json.</summary>
    public static ProviderSettings Load(string aiteamRoot)
    {
        var team = ReadJson(Path.Combine(aiteamRoot, "team.json")) ?? throw new FileNotFoundException("Không đọc được team.json", Path.Combine(aiteamRoot, "team.json"));
        var local = ReadJson(Path.Combine(aiteamRoot, "team.local.json"));
        var agyExe = local?["agy"]?["exe"]?.GetValue<string>() ?? team["agy"]?["exe"]?.GetValue<string>() ?? @"C:\Tools\agy\bin\agy.exe";
        var settings = Default(agyExe);

        if (team["providers"] is JsonArray arr)
        {
            var list = new List<ProviderConfig>();
            foreach (var node in arr)
            {
                var p = node.Deserialize<ProviderConfig>(Json) ?? throw new InvalidDataException("Mục providers sai định dạng");
                if (string.IsNullOrWhiteSpace(p.Id)) throw new InvalidDataException("Provider thiếu id");
                list.Add(p);
            }
            if (list.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != list.Count)
                throw new InvalidDataException("Trùng id provider");
            settings = settings with { Providers = list };
        }
        if (team["routing"] is JsonObject routing)
        {
            var map = new Dictionary<AgentRole, IReadOnlyList<string>>();
            foreach (var kv in routing)
            {
                if (!Enum.TryParse<AgentRole>(kv.Key, true, out var role)) throw new InvalidDataException($"Vai trò lạ trong routing: {kv.Key}");
                map[role] = kv.Value?.Deserialize<List<string>>() ?? [];
            }
            settings = settings with { Routing = map };
        }
        // Ghi đè theo máy: team.local.json → "providers": { "<id>": { "exe": "...", "nodeExe": "..." } }
        if (local?["providers"] is JsonObject overrides)
        {
            settings = settings with
            {
                Providers = settings.Providers.Select(p => overrides[p.Id] is JsonObject o
                    ? p with
                    {
                        Exe = o["exe"]?.GetValue<string>() ?? p.Exe,
                        NodeExe = o["nodeExe"]?.GetValue<string>() ?? p.NodeExe,
                        Enabled = o["enabled"]?.GetValue<bool>() ?? p.Enabled,
                    }
                    : p).ToList(),
            };
        }
        foreach (var (role, ids) in settings.Routing)
            foreach (var id in ids)
                if (!settings.Providers.Any(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException($"routing.{role} trỏ tới provider không có: {id}");
        return settings;
    }

    public IAgentProvider Create(ProviderConfig c, IProcessRunner runner) => c.Kind switch
    {
        ProviderKind.Antigravity => new AgyProvider(c, runner),
        ProviderKind.Codex => new CodexProvider(c, runner),
        ProviderKind.OpenCode => new OpenCodeProvider(c, runner),
        _ => throw new NotSupportedException($"Chưa hỗ trợ provider {c.Kind} (P1)"),
    };

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static JsonNode? ReadJson(string path)
    {
        if (!File.Exists(path)) return null;
        // Lỗi cú pháp: ném lỗi (fail-safe) — không âm thầm dùng mặc định khi cấu hình hỏng.
        return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
    }
}
