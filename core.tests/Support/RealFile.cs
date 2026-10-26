namespace AITeam.Core.Tests.Support;

/// <summary>Chép nguồn thật từ gốc repo tìm bằng team.ps1 và templates/task.md khi đi ngược từ AppContext.BaseDirectory; không dùng CWD hay .git.</summary>
public static class RealFile
{
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
        throw new NotImplementedException(); // TODO(agent)
    }
}
