namespace AITeam.Core.Review;

/// <summary>Đếm vòng review từ nội dung đã nạp; không đọc file, cấu hình hay gọi provider.</summary>
public static class ReviewRoundCounter
{
    /// <summary>
    /// Trả số tiêu đề cấp 3 "### Vòng N" (N là dãy chữ số ASCII, có thể có hậu tố)
    /// trong mục "## Review (Claude)" đầu tiên đến tiêu đề cấp 2 tiếp theo, cộng số JSON
    /// có thuộc tính gốc verdict dạng chuỗi đúng "CHANGES_REQUESTED" (phân biệt hoa thường).
    /// Đếm từng tiêu đề, kể cả N trùng/không liên tiếp và mọi kết luận; không lấy max N.
    /// Bỏ văn bản thường, tiêu đề cấp khác, dòng thụt >= 4 dấu cách/tab và khối mã
    /// rào bằng >= 3 dấu backtick hoặc dấu ngã (rào đóng cùng loại, dài >= rào mở).
    /// Chấp nhận UTF-8 BOM đầu nội dung, LF/CRLF; null/rỗng ở mỗi nguồn đóng góp 0.
    /// JSON hỏng, gốc không phải object, thiếu/sai kiểu verdict hoặc verdict khác đóng góp 0
    /// riêng từng log, không làm mất log tốt. Không phát thông báo lỗi, không sửa đầu vào.
    /// </summary>
    public static int Count(string? taskContent, IEnumerable<string?>? reviewLogs)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
