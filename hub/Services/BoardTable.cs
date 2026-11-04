namespace AITeam.Hub.Services;

// F1-06 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-06.

/// <summary>Một dòng task trong bảng việc tasks/README.md.</summary>
public sealed record BoardRow(string Id, string Title, string File, string Agent, string Branch, string Status, int Line);

/// <summary>Bộ đọc bảng việc dùng chung: chịu dòng trống giữa bảng, dòng thiếu "|" cuối, cột theo tên tiêu đề.</summary>
public static class BoardTable
{
    public static IReadOnlyList<BoardRow> Parse(string? markdown)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
