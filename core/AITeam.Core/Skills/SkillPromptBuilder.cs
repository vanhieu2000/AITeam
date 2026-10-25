namespace AITeam.Core.Skills;

/// <summary>Chỉ dẫn đọc skill cho review độc lập; không chạy tiến trình hoặc sửa cấu hình.</summary>
public static class SkillPromptBuilder
{
    /// <summary>
    /// Trả các dòng "Đọc skill: `&lt;đường dẫn tuyệt đối&gt;`" theo thứ tự 09-qc, code-test, skill stack.
    /// Chỉ trỏ SKILL.md tồn tại trong root/skills; không nhúng nội dung file.
    /// Đọc root/projects.json: mục projects là mảng name/path/workDir/stack; không đọc hồ sơ thật.
    /// Chọn dự án từ workDir hoặc taskFile dưới path/workDir cấu hình, ưu tiên đường dẫn dài nhất;
    /// khớp ranh giới thư mục. Hai nguồn chỉ hai dự án khác nhau ⇒ bỏ stack.
    /// WinForms ⇒ code-winforms; ASP.NET/Razor ⇒ code-web; MAUI/console/CLI ⇒ code-app, không phân biệt hoa thường.
    /// Stack thiếu/rỗng/lạ hoặc không tìm được dự án ⇒ vẫn có 09-qc/code-test nếu tồn tại; không đoán từ diff.
    /// Root null/rỗng ⇒ chuỗi rỗng. Thiếu projects.json hoặc skill ⇒ bỏ phần tương ứng, không báo lỗi.
    /// JSON hỏng ⇒ JsonException, không coi thành cấu hình rỗng. Không theo symlink/reparse point khi đọc nguồn.
    /// Tên stack không dùng ghép đường dẫn; không ghi file, gọi shell hoặc đọc ngoài root cấu hình.
    /// profile là ngữ cảnh tùy chọn, không tự suy ra USERPROFILE.
    /// </summary>
    public static string BuildReviewInstructions(string? root, string workDir, string taskFile, string? profile = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}