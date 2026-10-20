using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

public sealed class HealthService : IHealthService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _configService;

    public HealthService(HubPaths paths, ITeamConfigService configService)
    {
        _paths = paths;
        _configService = configService;
    }

    public IReadOnlyList<HealthItem> Check()
    {
        var items = new List<HealthItem>();
        var settings = _configService.GetSettings();

        // 1. git
        var (gitExit, gitOut, _) = RunCommand("git", "--version");
        if (gitExit == 0 && !string.IsNullOrWhiteSpace(gitOut))
        {
            string ver = gitOut.Replace("git version ", "").Trim();
            items.Add(new HealthItem("git", HealthLevel.Ok, $"git {ver}", null));
        }
        else
        {
            items.Add(new HealthItem("git", HealthLevel.Error, "Thiếu git", "winget install Git.Git"));
        }

        // 2. GitHub CLI (gh)
        string? ghExe = FindGh();
        if (ghExe != null)
        {
            var (authExit, authOut, authErr) = RunCommand(ghExe, "auth status");
            string combined = authOut + " " + authErr;
            var match = Regex.Match(combined, @"Logged in to github\.com account (\S+)");
            if (match.Success)
            {
                items.Add(new HealthItem("GitHub", HealthLevel.Ok, $"Đã đăng nhập: {match.Groups[1].Value}", null));
            }
            else
            {
                items.Add(new HealthItem("GitHub", HealthLevel.Warning, "Chưa đăng nhập GitHub", "gh auth login --web"));
            }
        }
        else
        {
            items.Add(new HealthItem("GitHub CLI", HealthLevel.Error, "Thiếu GitHub CLI", "winget install GitHub.cli"));
        }

        // 3. Antigravity CLI (agy)
        string agyExe = settings.AgyExe;
        if (File.Exists(agyExe))
        {
            var (agyExit, agyOut, _) = RunCommand(agyExe, "--version");
            if (agyExit == 0 && !string.IsNullOrWhiteSpace(agyOut))
            {
                items.Add(new HealthItem("Antigravity CLI", HealthLevel.Ok, $"{agyOut.Trim()} ({agyExe})", null));
            }
            else
            {
                items.Add(new HealthItem("Antigravity CLI", HealthLevel.Ok, $"Có file tại {agyExe}", null));
            }
        }
        else
        {
            items.Add(new HealthItem(
                "Antigravity CLI",
                HealthLevel.Error,
                $"Thiếu Antigravity CLI tại {agyExe}",
                "irm https://antigravity.google/cli/install.ps1 -OutFile agy-install.ps1; .\\agy-install.ps1 --dir C:\\Tools\\agy\\bin"
            ));
        }

        // 4. Claude integration
        string claudeMd = Path.Combine(_paths.ClaudeDir, "CLAUDE.md");
        string skillFile = Path.Combine(_paths.UserProfile, ".claude", "skills", "aiteam", "SKILL.md");
        bool claudeHasAITeam = File.Exists(claudeMd) && File.ReadAllText(claudeMd).Contains("AITeam");
        bool skillExists = File.Exists(skillFile);

        if (claudeHasAITeam)
        {
            items.Add(new HealthItem("Claude integration", HealthLevel.Ok, $"Claude biết quy trình team ({claudeMd})", null));
        }
        else
        {
            items.Add(new HealthItem("Claude integration", HealthLevel.Warning, "Claude chưa biết quy trình team", "Chạy InstallClaudeIntegration hoặc team.ps1 setup"));
        }

        // 5. Dự án và pre-commit hook
        var projects = _configService.GetProjects();
        foreach (var p in projects)
        {
            if (p.ExistsLocally)
            {
                items.Add(new HealthItem($"Dự án {p.Name}", HealthLevel.Ok, p.Path, null));
            }
            else
            {
                items.Add(new HealthItem($"Dự án {p.Name}", HealthLevel.Warning, $"{p.Name} chưa có trên máy", $"team.ps1 add-project -Project {p.Name} -Repo {p.Repo}"));
            }

            // Hook
            bool hasHook = false;
            string hookPath = Path.Combine(p.Path, ".git", "hooks", "pre-commit");
            if (File.Exists(hookPath))
            {
                try
                {
                    if (File.ReadAllText(hookPath).Contains("AITeam")) hasHook = true;
                }
                catch { }
            }
            else if (Directory.Exists(p.Path))
            {
                var (commonExit, commonOut, _) = RunCommand("git", "rev-parse --git-common-dir", p.Path);
                if (commonExit == 0 && !string.IsNullOrWhiteSpace(commonOut))
                {
                    string commonDir = commonOut.Trim();
                    if (!Path.IsPathRooted(commonDir)) commonDir = Path.Combine(p.Path, commonDir);
                    string customHook = Path.Combine(commonDir, "hooks", "pre-commit");
                    if (File.Exists(customHook) && File.ReadAllText(customHook).Contains("AITeam"))
                    {
                        hasHook = true;
                    }
                }
            }

            if (hasHook)
            {
                items.Add(new HealthItem($"Hook {p.Name}", HealthLevel.Ok, "Đã cài hook chặn bí mật", null));
            }
            else if (p.ExistsLocally)
            {
                items.Add(new HealthItem($"Hook {p.Name}", HealthLevel.Warning, "Chưa cài hook chặn bí mật", "team.ps1 install-hooks"));
            }
        }

        return items;
    }

    public async Task<HealthItem> CheckAgyLoginAsync(CancellationToken ct = default)
    {
        var settings = _configService.GetSettings();
        string exe = settings.AgyExe;
        if (!File.Exists(exe))
        {
            return new HealthItem("Antigravity Login", HealthLevel.Error, $"Thiếu Antigravity CLI tại {exe}", null);
        }

        int? startedPid = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "-p \"Tra loi dung mot chu: OK\" --output-format json --print-timeout 2m",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();
            startedPid = proc.Id;
            // Đóng stdin ngay để pipe không bị mở (tránh agy treo)
            proc.StandardInput.Close();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);

            await proc.WaitForExitAsync(cts.Token);
            string output = (await stdoutTask) + " " + (await stderrTask);

            if (output.Contains("\"status\":\"SUCCESS\"") || output.Contains("\"status\": \"SUCCESS\""))
            {
                return new HealthItem("Antigravity Login", HealthLevel.Ok, "Antigravity đã đăng nhập, chạy ngầm được", null);
            }
            else if (Regex.IsMatch(output, @"auth|login|credential", RegexOptions.IgnoreCase))
            {
                return new HealthItem("Antigravity Login", HealthLevel.Warning, "Antigravity chưa đăng nhập", "team.ps1 switch-account");
            }
            else
            {
                return new HealthItem("Antigravity Login", HealthLevel.Error, "Antigravity: " + output.Trim(), null);
            }
        }
        catch (OperationCanceledException)
        {
            // Review H-001: hủy chờ không tự dừng tiến trình → phải tự kill, nếu không agy chạy ngầm mãi.
            try { if (startedPid is int pid) Process.GetProcessById(pid).Kill(entireProcessTree: true); } catch { }
            return new HealthItem("Antigravity Login", HealthLevel.Error, "Kiểm tra Antigravity quá thời gian (2 phút)", null);
        }
        catch (Exception ex)
        {
            return new HealthItem("Antigravity Login", HealthLevel.Error, "Lỗi kiểm tra Antigravity: " + ex.Message, null);
        }
    }

    public void OpenSwitchAccountWindow()
    {
        var settings = _configService.GetSettings();
        string exe = settings.AgyExe;
        // Review H-001: đường dẫn lấy từ cấu hình (sau này sửa được trên trang Cài đặt) — không ghép vào lệnh PowerShell
        // (chèn được lệnh), chỉ chạy thẳng đúng file agy.exe có thật, trong cửa sổ console mới.
        if (!IsValidAgyExe(exe))
            throw new InvalidOperationException($"Đường dẫn Antigravity CLI không hợp lệ: {exe}");
        Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
    }

    internal static bool IsValidAgyExe(string? exe) =>
        !string.IsNullOrWhiteSpace(exe)
        && Path.IsPathFullyQualified(exe)
        && string.Equals(Path.GetFileName(exe), "agy.exe", StringComparison.OrdinalIgnoreCase)
        && File.Exists(exe);

    public void InstallClaudeIntegration()
    {
        // 1. Cài skill aiteam
        string skillDir = Path.Combine(_paths.UserProfile, ".claude", "skills", "aiteam");
        if (!Directory.Exists(skillDir))
        {
            Directory.CreateDirectory(skillDir);
        }

        if (File.Exists(_paths.SkillTemplate))
        {
            string template = File.ReadAllText(_paths.SkillTemplate);
            string processed = template.Replace("{{AITEAM}}", _paths.AITeamRoot);
            File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), processed, new UTF8Encoding(false));
        }

        // 2. Gắn khối AITeam vào ~/.claude/CLAUDE.md nếu chưa có
        if (!Directory.Exists(_paths.ClaudeDir))
        {
            Directory.CreateDirectory(_paths.ClaudeDir);
        }

        string claudeMd = Path.Combine(_paths.ClaudeDir, "CLAUDE.md");
        string current = File.Exists(claudeMd) ? File.ReadAllText(claudeMd) : "";

        if (!current.Contains("AITeam: quy trình team"))
        {
            string block = $"\r\n\r\n<!-- AITeam: quy trình team AI của HieuDV -->\r\nKhi người dùng giao việc cho \"team\" / \"đội\" / nhắc tới Antigravity, agent, dự án mới, hoặc làm việc trong dự án có trong\r\n{_paths.AITeamRoot}\\projects.json: ĐỌC VÀ LÀM THEO {_paths.AITeamRoot}\\CLAUDE.md (bạn là quản lý team, điều phối bằng {_paths.AITeamRoot}\\team.ps1).\r\n";
            File.AppendAllText(claudeMd, block, new UTF8Encoding(false));
        }
    }

    private static string? FindGh()
    {
        var (exitCode, stdout, _) = RunCommand("where.exe", "gh");
        if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
        {
            string first = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(first) && File.Exists(first)) return first;
        }

        string defaultGh = @"C:\Program Files\GitHub CLI\gh.exe";
        if (File.Exists(defaultGh)) return defaultGh;

        return null;
    }

    private static (int exitCode, string stdout, string stderr) RunCommand(string file, string args, string? workingDir = null)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    WorkingDirectory = workingDir ?? "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(true); } catch { }
                return (-1, "", "Timeout");
            }
            return (proc.ExitCode, proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd());
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }
}
