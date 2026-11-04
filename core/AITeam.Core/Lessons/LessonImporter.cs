namespace AITeam.Core.Lessons;

// F1-05 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-05.

/// <summary>Một bài học đọc từ file markdown (CLAUDE.md / AGENTS.md).</summary>
public sealed record ImportedLesson(string Title, string Body, string Source);

/// <summary>Kết quả nhập: số bài thêm mới, số bài bỏ qua vì đã có.</summary>
public sealed record LessonImportResult(int Added, int Skipped, IReadOnlyList<string> Warnings);

public static class LessonImporter
{
    /// <summary>Mỗi gạch đầu dòng cấp 1 trong mục "## Bài học…" / "## Lỗi hay gặp…" là 1 bài học.</summary>
    public static IReadOnlyList<ImportedLesson> Extract(string markdown, string source)
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <summary>Thêm bản nháp cho bài chưa có (so tiêu đề đã chuẩn hóa với mọi bài học hiện có).</summary>
    public static LessonImportResult Import(ILessonService lessons, IEnumerable<ImportedLesson> items)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
