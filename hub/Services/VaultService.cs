using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AITeam.Hub.Services;

public sealed class VaultService : IVaultService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService? _configService;
    private static readonly byte[] VaultEntropy = Encoding.UTF8.GetBytes("AITeam.vault.v1");
    private static readonly Regex ValidNameRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public VaultService(HubPaths paths, ITeamConfigService? configService = null)
    {
        _paths = paths;
        _configService = configService;
    }

    public IReadOnlyList<string> ListNames()
    {
        var dict = ReadVault();
        return dict.Keys.ToList();
    }

    public void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || !ValidNameRegex.IsMatch(name))
        {
            throw new ArgumentException("Tên bí mật dạng CHU_HOA_GACH_DUOI, ví dụ DB_PASSWORD", nameof(name));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Giá trị rỗng");
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(value);
        byte[] cipherBytes = ProtectedData.Protect(plainBytes, VaultEntropy, DataProtectionScope.CurrentUser);
        string cipherBase64 = Convert.ToBase64String(cipherBytes);

        var dict = ReadVault();
        dict[name] = cipherBase64;
        WriteVault(dict);
    }

    public bool Remove(string name)
    {
        var dict = ReadVault();
        if (dict.Remove(name))
        {
            WriteVault(dict);
            return true;
        }

        return false;
    }

    public void ExportEnv(string project, IReadOnlyList<string> names)
    {
        string projectPath = project;
        if (_configService != null)
        {
            var p = _configService.FindProject(project);
            if (p != null)
            {
                projectPath = p.Path;
            }
        }

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Không tìm thấy thư mục dự án: {projectPath}");
        }

        // Chạy git -C <dự án> check-ignore -q .env.local
        int exitCode = RunGitCheckIgnore(projectPath, ".env.local");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($".env.local CHƯA bị .gitignore trong {projectPath} — thêm dòng '.env.*' vào .gitignore trước.");
        }

        var vault = ReadVault();
        var lines = new List<string>();

        foreach (var name in names)
        {
            if (!vault.TryGetValue(name, out var cipherBase64) || string.IsNullOrWhiteSpace(cipherBase64))
            {
                throw new KeyNotFoundException($"Không có bí mật '{name}' (team.ps1 secret-list)");
            }

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, VaultEntropy, DataProtectionScope.CurrentUser);
                string plain = Encoding.UTF8.GetString(plainBytes);
                lines.Add($"{name}={plain}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi giải mã bí mật '{name}': {ex.Message}", ex);
            }
        }

        string target = Path.Combine(projectPath, ".env.local");
        string fileContent = lines.Count > 0
            ? string.Join("\r\n", lines) + "\r\n"
            : "";

        File.WriteAllText(target, fileContent, new UTF8Encoding(false));
    }

    private Dictionary<string, string> ReadVault()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(_paths.VaultFile))
        {
            return dict;
        }

        try
        {
            string content = File.ReadAllText(_paths.VaultFile);
            var node = JsonNode.Parse(content) as JsonObject;
            if (node != null)
            {
                foreach (var kv in node)
                {
                    if (kv.Value != null)
                    {
                        dict[kv.Key] = kv.Value.ToString();
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua nếu file hỏng
        }

        return dict;
    }

    private void WriteVault(Dictionary<string, string> dict)
    {
        string? dir = Path.GetDirectoryName(_paths.VaultFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var obj = new JsonObject();
        foreach (var kv in dict)
        {
            obj[kv.Key] = kv.Value;
        }

        string json = obj.ToJsonString(JsonOptions);
        File.WriteAllText(_paths.VaultFile, json, new UTF8Encoding(false));
    }

    private static int RunGitCheckIgnore(string repoPath, string relativeFile)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"-C \"{repoPath}\" check-ignore -q \"{relativeFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(true); } catch { }
                return -1;
            }
            return proc.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
