using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>
/// Hàm thuần đọc dữ liệu Hub v3 từ chuỗi (không đụng đĩa) — R-003 / H3-01. Thiết kế: docs/design/HUB-V3.md mục 3.
/// Mọi hàm KHÔNG ném lỗi với dữ liệu hỏng / thiếu: trả giá trị mặc định (Unknown, null, danh sách rỗng).
/// Tiêu đề mục so khớp sau Trim, không phân biệt hoa thường; dòng có thể kết thúc bằng \r\n hoặc \n.
/// </summary>
public static class InsightParsers
{
    /// <summary>Tên 7 giai đoạn chuẩn (Index 1..7) — dùng khi một giai đoạn không có dòng nào.</summary>
    public static readonly IReadOnlyList<string> StageNames = new[]
    {
        "1. Tiếp nhận & quản lý", "2. Ý tưởng", "3. Plan / thiết kế", "4. Giao việc",
        "5. Thực hiện code", "6. Review / QC", "7. Đánh giá & bàn giao",
    };

    /// <summary>Dòng "- **Độ khó:** M" (chấp nhận "M (vừa)", chữ thường "m"). Không có / sai ⇒ Unknown.</summary>
    public static Difficulty ParseDifficulty(string taskMarkdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>Dòng "- **Yêu cầu:** R-002" ⇒ "R-002" (khớp R-\d{3}). Không có ⇒ null.</summary>
    public static string? ParseRequest(string taskMarkdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Bảng trong mục "## Đánh giá quy trình" (tới mục "## " kế tiếp). Bỏ dòng tiêu đề và dòng "|---|".
    /// Mỗi dòng 7 ô: Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất. Thiếu ô ⇒ ô rỗng.
    /// Ô chỉ có "—" / "-" ⇒ "". Bỏ dấu ` quanh nội dung KHÔNG cần (giữ nguyên văn). Không có mục ⇒ rỗng.
    /// </summary>
    public static IReadOnlyList<StageScore> ParseProcessReview(string taskMarkdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Trong mục "## Review (Claude)" (tới "## " kế tiếp; "### Vòng N" vẫn thuộc mục): mọi dòng (kể cả thụt lề)
    /// dạng "- [AG] (giao diện) mô tả" ⇒ Cause "AG", Kind "giao diện", Text "mô tả". Không có "(loại)" ⇒ Kind "".
    /// Chỉ nhận 5 mã AG, TK, CK, NV, HT (dòng "- [AG|TK|CK] …" của mẫu KHÔNG tính). Dấu ";" / "." cuối Text bỏ đi.
    /// </summary>
    public static IReadOnlyList<ReviewIssue> ParseReviewIssues(string taskMarkdown, string project, string task)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// "### Vòng N — ĐẠT" / "### Vòng N — CẦN SỬA" (gạch "—" hoặc "-"; có thể kèm "(ngày, ai)").
    /// Rounds = N lớn nhất (0 nếu không có); PassedRound1 = có "Vòng 1" với chữ "ĐẠT" ngay sau gạch.
    /// </summary>
    public static (int Rounds, bool PassedRound1) ParseReviewRounds(string taskMarkdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Một file metrics/runs/*.json (mẫu thật trong test nghiệm thu). "run" có thể là chuỗi "3" hoặc số.
    /// "at" là giờ địa phương không múi ("2026-10-23T23:56:27"). Thiếu project / task / at hoặc JSON hỏng ⇒ null.
    /// Trường số thiếu ⇒ 0; bool thiếu ⇒ false; "request" thiếu / rỗng ⇒ null.
    /// </summary>
    public static RunRecord? ParseRun(string json)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// improvements/IMP-001-ten.md. Id = "IMP-001" lấy từ TÊN FILE (khớp ^IMP-\d{3}); không khớp ⇒ null.
    /// Title = dòng "# " đầu tiên bỏ "# " (không có ⇒ Id). Status = giá trị "- **Trạng thái:**" đã bỏ chú thích &lt;!-- --&gt; và Trim.
    /// Tasks = "- **Task trong chu kỳ:**" tách theo dấu phẩy, Trim, bỏ rỗng, bỏ chú thích.
    /// </summary>
    public static Improvement? ParseImprovement(string fileName, string markdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>staff/A-minh.md: mã = phần trước "-" đầu tiên của tên file ("A"); tên = giá trị "- **Tên:**". Thiếu ⇒ null.</summary>
    public static (string Agent, string Name)? ParseStaff(string fileName, string markdown)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Biểu đồ thời gian từ các lượt (đã lọc đúng task), sắp theo At tăng dần:
    /// - lượt đầu ⇒ Coding [At − DurationSec, At]; các lượt sau ⇒ Fix [At − DurationSec, At];
    /// - mergedAt có và &gt; At lượt cuối ⇒ thêm Review [At lượt cuối, mergedAt] rồi Merge [mergedAt, mergedAt];
    /// - mergedAt có nhưng không có lượt nào ⇒ chỉ Merge; không mergedAt ⇒ không có Review / Merge.
    /// </summary>
    public static IReadOnlyList<TimelineSegment> BuildTimeline(IReadOnlyList<RunRecord> runs, DateTime? mergedAt)
        => throw new NotImplementedException(); // TODO(agent)
}
