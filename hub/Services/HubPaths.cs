namespace AITeam.Hub.Services;

/// <summary>
/// Các đường dẫn gốc. Mặc định: thư mục AITeam là thư mục cha của hub/ (hoặc biến môi trường AITEAM_HOME);
/// vault ở %USERPROFILE%\.aiteam; Claude ở %USERPROFILE%\.claude. Test truyền thư mục tạm.
/// </summary>
public sealed record HubPaths(string AITeamRoot, string UserProfile)
{
    public string TeamJson => Path.Combine(AITeamRoot, "team.json");
    public string TeamLocalJson => Path.Combine(AITeamRoot, "team.local.json");
    public string ProjectsJson => Path.Combine(AITeamRoot, "projects.json");
    public string InboxFile => Path.Combine(AITeamRoot, "inbox.md");
    public string SkillTemplate => Path.Combine(AITeamRoot, "skill", "SKILL.md");
    public string VaultFile => Path.Combine(UserProfile, ".aiteam", "vault.json");
    public string ClaudeDir => Path.Combine(UserProfile, ".claude");

    public static HubPaths Detect()
    {
        string? env = Environment.GetEnvironmentVariable("AITEAM_HOME");
        string root = !string.IsNullOrWhiteSpace(env) ? env : FindRoot(AppContext.BaseDirectory) ?? FindRoot(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Không tìm thấy thư mục AITeam (chứa team.json). Đặt biến môi trường AITEAM_HOME.");
        return new HubPaths(root, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "team.json"))) return dir.FullName;
        return null;
    }
}
