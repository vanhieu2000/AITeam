using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class TestEnvironment : IDisposable
{
    public string RootDir { get; }
    public string AITeamRoot { get; }
    public string UserProfile { get; }
    public HubPaths Paths { get; }

    public TestEnvironment()
    {
        RootDir = Path.Combine(Path.GetTempPath(), "aiteam-test-" + Guid.NewGuid().ToString("N"));
        AITeamRoot = Path.Combine(RootDir, "AITeam");
        UserProfile = Path.Combine(RootDir, "UserProfile");

        Directory.CreateDirectory(AITeamRoot);
        Directory.CreateDirectory(UserProfile);

        Paths = new HubPaths(AITeamRoot, UserProfile);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootDir))
            {
                // Xóa thuộc tính readonly nếu có trước khi xóa
                SetNormalAttributes(new DirectoryInfo(RootDir));
                Directory.Delete(RootDir, recursive: true);
            }
        }
        catch
        {
            // Bỏ qua lỗi dọn dẹp file tạm
        }
    }

    private static void SetNormalAttributes(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
        {
            SetNormalAttributes(subDir);
        }
        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }
}
