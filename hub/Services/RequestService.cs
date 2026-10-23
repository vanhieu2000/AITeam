namespace AITeam.Hub.Services;

// Trang "Giao việc" (R-003 / H3-05): mỗi phiếu requests/R-xxx-*.md là 1 luồng hội thoại.
// Lời chủ dự án = tin nhắn bên phải; mỗi dòng Nhật ký của Claude = tin nhắn bên trái. Hub chỉ ĐỌC phiếu.

/// <summary>Một dòng bảng "## 3. Nhật ký": | Giờ | Sự kiện | Ghi chú |.</summary>
public sealed record RequestEvent(string Time, string Text, string Note);

/// <summary>Một dòng bảng "## 2. Chia việc": | Task | Dự án | Ai | … |. Code = mã đầu ô Task (vd. "H3-01", "P0-4/5"), không có ⇒ "".</summary>
public sealed record RequestTaskRef(string Code, string Title, string Project, string Who);

public sealed record RequestThread(
    string Id,              // "R-003" (lấy từ TÊN FILE)
    string Title,           // dòng "# R-003 · Tiêu đề" bỏ "R-003 · "
    string Status,          // giá trị "- **Trạng thái:**"
    string ReceivedAt,      // giá trị "- **Nhận lúc:**"
    string Verbatim,        // các dòng "> …" ngay sau "- **Lời chủ dự án:**", bỏ "> ", nối bằng "\n"
    IReadOnlyList<RequestTaskRef> Tasks,
    IReadOnlyList<RequestEvent> Events,
    bool Evaluated,         // ô giá trị của dòng "Tổng thời gian" trong mục "## 4." khác rỗng
    string FileName);

public static class RequestParser
{
    /// <summary>
    /// fileName phải khớp ^R-\d{3} (vd. "R-003-nang-cap.md"), sai ⇒ null. Không ném lỗi với nội dung hỏng / thiếu mục:
    /// mục thiếu ⇒ chuỗi rỗng / danh sách rỗng / false.
    /// Bảng: bỏ dòng tiêu đề (ô đầu "Task" / "Giờ" / "Chỉ số") và dòng "|---|"; ô "—" giữ nguyên văn; Trim mọi ô.
    /// Chia việc: bỏ dòng ô đầu rỗng; Code = khớp ^[A-Za-z0-9]+-\d+[A-Za-z0-9/]* ở đầu ô Task (không khớp ⇒ "").
    /// Nhật ký: bỏ dòng ô Sự kiện rỗng.
    /// Verbatim: chỉ nhận các dòng liền nhau bắt đầu bằng "&gt;" (sau khoảng trắng) ngay sau dòng "Lời chủ dự án"; dừng ở dòng đầu tiên không phải trích dẫn.
    /// </summary>
    public static RequestThread? Parse(string fileName, string markdown)
        => throw new NotImplementedException(); // TODO(agent)
}

public interface IRequestService
{
    /// <summary>Mọi requests/R-*.md đọc được, Id GIẢM dần (mới nhất trước). Thư mục không có ⇒ rỗng.</summary>
    IReadOnlyList<RequestThread> GetAll();

    /// <summary>id phải khớp ^R-\d{3}$ (sai ⇒ null, không đụng đĩa); không có ⇒ null.</summary>
    RequestThread? Get(string id);
}

public sealed class RequestService : IRequestService
{
    private readonly HubPaths _paths;

    public RequestService(HubPaths paths) => _paths = paths;

    public string Folder => Path.Combine(_paths.AITeamRoot, "requests");

    public IReadOnlyList<RequestThread> GetAll()
        => throw new NotImplementedException(); // TODO(agent)

    public RequestThread? Get(string id)
        => throw new NotImplementedException(); // TODO(agent)
}
