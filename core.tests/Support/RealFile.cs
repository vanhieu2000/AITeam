namespace AITeam.Core.Tests.Support;

/// <summary>Chép nguồn thật từ gốc repo tìm bằng team.ps1 và templates/task.md khi đi ngược từ AppContext.BaseDirectory; không dùng CWD hay .git.</summary>
public static class RealFile
{
    internal static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        if (relativePath.Contains(':') ||
            relativePath.StartsWith('/') ||
            relativePath.StartsWith('\\') ||
            Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");
        }

        var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.None);
        foreach (var part in parts)
        {
            if (part == ".." || part == ".")
                throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");
        }
    }

    internal static bool HasReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            var fa = File.GetAttributes(fullPath);
            if ((fa & FileAttributes.ReparsePoint) != 0) return true;
        }

        var dir = new DirectoryInfo(fullPath);
        while (dir != null)
        {
            if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
                return true;
            dir = dir.Parent;
        }
        return false;
    }

    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "team.ps1")) &&
                File.Exists(Path.Combine(dir.FullName, "templates", "task.md")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Không tìm thấy gốc repo.");
    }

    /// <summary>
    /// Trả đường dẫn tuyệt đối targetDir/relativePathInRepo, tạo thư mục cha và ghi đè bản sao,
    /// giữ nguyên tên, cây tương đối và mọi byte (BOM/CRLF). Không sửa nguồn.
    /// Chặn null/rỗng/trắng, mọi thành phần '..', đường dẫn tuyệt đối/ổ đĩa/UNC/ADS,
    /// và reparse point ở file, gốc hoặc bất kỳ thư mục cha nguồn/đích trước khi ghi.
    /// Sai đường dẫn: ArgumentException("Đường dẫn bản sao không hợp lệ.").
    /// File nguồn thiếu: FileNotFoundException("Không tìm thấy file nguồn.").
    /// Không thấy gốc repo: DirectoryNotFoundException("Không tìm thấy gốc repo.").
    /// Lỗi I/O khác phải truyền ra, không coi là dữ liệu rỗng.
    /// </summary>
    public static string Copy(string relativePathInRepo, string targetDir)
    {
        ValidateRelativePath(relativePathInRepo);

        if (string.IsNullOrWhiteSpace(targetDir))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        if (HasReparsePoint(targetDir))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        var repoRoot = FindRepoRoot();
        if (HasReparsePoint(repoRoot))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        var sourcePath = Path.Combine(repoRoot, relativePathInRepo);
        if (HasReparsePoint(sourcePath))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Không tìm thấy file nguồn.", sourcePath);

        var destPath = Path.Combine(targetDir, relativePathInRepo);
        if (HasReparsePoint(destPath))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        var targetFullPath = Path.GetFullPath(targetDir);
        var destFullPath = Path.GetFullPath(destPath);
        if (!destFullPath.StartsWith(targetFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Đường dẫn bản sao không hợp lệ.");

        var destDir = Path.GetDirectoryName(destFullPath)!;
        Directory.CreateDirectory(destDir);
        File.Copy(sourcePath, destFullPath, overwrite: true);

        return destFullPath;
    }
}
