using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class H701ExtraTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "H701Extra-" + Guid.NewGuid());
    private readonly HubPaths _paths;
    private static readonly string[] Codes = ["receive", "idea", "research", "plan", "approve_plan", "prep", "code", "gate", "qc", "accept", "publish"];
    private static readonly string[] Actors = ["claude", "claude", "codex", "codex", "claude", "codex", "minh", "gate", "codex", "claude", "claude"];

    public H701ExtraTests()
    {
        Directory.CreateDirectory(_root);
        _paths = new HubPaths(_root, Path.Combine(_root, "profile"));
    }

    private WorkflowRolesService Service() => new(_paths);
    private static WorkflowRole[] Defaults() => Codes.Select((c, i) => new WorkflowRole(c, Actors[i], "")).ToArray();

    [Fact]
    public void SaveRoles_with_invalid_workflow_type_throws_and_preserves_file()
    {
        // File chứa workflow sai kiểu (array thay vì object)
        string json = "{\"workflow\":[1]}";
        File.WriteAllText(_paths.TeamJson, json, Encoding.UTF8);
        byte[] beforeBytes = File.ReadAllBytes(_paths.TeamJson);
        string revision = Convert.ToHexString(SHA256.HashData(beforeBytes));

        var ex = Assert.Throws<InvalidDataException>(() => Service().SaveRoles(Defaults(), revision));
        Assert.Equal("Phân vai không hợp lệ.", ex.Message);
        Assert.Equal(beforeBytes, File.ReadAllBytes(_paths.TeamJson));
    }

    [Fact]
    public void SaveRoles_directory_creation_failure_throws_IOException()
    {
        // Đặt paths trỏ tới một thư mục không thể tạo (ví dụ một file đã tồn tại có cùng tên với thư mục cha)
        string blockedDir = Path.Combine(_root, "blocked_dir");
        File.WriteAllText(blockedDir, "blocking file"); // Tạo file ngăn Directory.CreateDirectory
        var invalidPaths = new HubPaths(Path.Combine(blockedDir, "sub"), Path.Combine(_root, "profile"));
        var service = new WorkflowRolesService(invalidPaths);

        var ex = Assert.Throws<IOException>(() => service.SaveRoles(Defaults(), "missing"));
        Assert.Equal("Không lưu được cấu hình phân vai.", ex.Message);
    }

    [Fact]
    public void SaveSettings_rejects_bak_symlink_or_reparse_point_and_preserves_target()
    {
        // Chuẩn bị team.json
        File.WriteAllText(_paths.TeamJson, "{\"custom\":1}", Encoding.UTF8);

        // Tạo target file để kiểm tra xem có bị ghi đè không
        string targetFile = Path.Combine(_root, "do_not_overwrite.txt");
        string originalContent = "ORIGINAL_SECRET_CONTENT";
        File.WriteAllText(targetFile, originalContent, Encoding.UTF8);

        string bakPath = _paths.TeamJson + ".bak";

        // Thử tạo symlink cho .bak
        bool symlinkCreated = false;
        try
        {
            File.CreateSymbolicLink(bakPath, targetFile);
            symlinkCreated = true;
        }
        catch
        {
            // Môi trường không đủ quyền tạo file symlink (không có Developer Mode)
        }

        if (symlinkCreated)
        {
            var config = new TeamConfigService(_paths);
            var settings = config.GetSettings() with { DefaultModel = "test-model" };

            var ex = Assert.Throws<IOException>(() => config.SaveSettings(settings));
            Assert.Equal("Không lưu được cấu hình.", ex.Message);

            // Dữ liệu file đích không bị thay đổi
            Assert.Equal(originalContent, File.ReadAllText(targetFile));
        }
        else
        {
            // Test reparse point qua Directory Junction (cmd /c mklink /J)
            string realDir = Path.Combine(_root, "real_dir");
            Directory.CreateDirectory(realDir);
            string junctionDir = Path.Combine(_root, "junc_dir");

            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{junctionDir}\" \"{realDir}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            proc?.WaitForExit();

            if (Directory.Exists(junctionDir))
            {
                var junctionPaths = new HubPaths(junctionDir, Path.Combine(_root, "profile"));
                File.WriteAllText(Path.Combine(realDir, "team.json"), "{\"custom\":1}", Encoding.UTF8);

                var config = new TeamConfigService(junctionPaths);
                var settings = config.GetSettings() with { DefaultModel = "test-model" };

                var ex = Assert.Throws<IOException>(() => config.SaveSettings(settings));
                Assert.Equal("Không lưu được cấu hình.", ex.Message);
            }
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}
