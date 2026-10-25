using Microsoft.AspNetCore.Http;

namespace AITeam.Hub.Services;

/// <summary>Cổng gửi chat Văn phòng; chỉ đọc phiếu qua IRequestService và gửi qua IInboxService.</summary>
public interface IOfficeChatService
{
    /// <summary>
    /// Gửi đúng một mục inbox, không tạo hoặc sửa phiếu. Kiểm tra theo thứ tự: nội dung,
    /// dự án qua FindProject, rồi phiếu. Nội dung null/rỗng/toàn khoảng trắng ném ArgumentException
    /// với Message đúng "Nội dung yêu cầu không được để trống."; độ dài gốc trên 4000 ký tự
    /// ném ArgumentException("Nội dung quá dài (tối đa 4000 ký tự).").
    /// Dự án không tìm thấy ném ArgumentException("Dự án không hợp lệ.").
    /// requestId null hoặc rỗng là gửi mới, chuyển text.Trim() nguyên văn tới inbox.
    /// Còn lại phải khớp toàn bộ R-[0-9]{3} (không trim, không nhận xuống dòng), gọi Get(id)
    /// và chỉ nhận phiếu có Id bằng chính id theo Ordinal; sai/mất/giả mạo ném
    /// ArgumentException("Phiếu không hợp lệ.") trước khi Add. Không dùng FileName làm đường dẫn.
    /// Gửi bổ sung bằng "[Bổ sung {id}] {text.Trim()}"; giới hạn 4000 chỉ tính nội dung người dùng.
    /// Add nhận tên chuẩn từ ProjectInfo.Name. Ngoại lệ đọc phiếu/inbox truyền nguyên ra ngoài;
    /// không thử lại, không ghi bù; không đọc GetAll và không chạy tiến trình ngoài.
    /// </summary>
    void Send(string project, string? text, string? requestId = null);
}

/// <summary>Dịch vụ không giữ trạng thái từng lần gửi; dùng được trong DI singleton.</summary>
public sealed class OfficeChatService(
    IRequestService requests, IInboxService inbox, ITeamConfigService config) : IOfficeChatService
{
    /// <inheritdoc />
    public void Send(string project, string? text, string? requestId = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}

/// <summary>Đặt trước MapRazorPages: chỉ GET /Agents và /Inbox (không phân biệt hoa thường)
/// trả 302, Location chính xác "/", bỏ query và không gọi next hay dịch vụ.
/// Mọi phương thức/đường dẫn khác chuyển next đúng một lần, không đổi phản hồi.
/// Không chuyển /Agents/child, không thực hiện handler cũ kể cả query handler.</summary>
public sealed class OfficeChatRedirectMiddleware(RequestDelegate next)
{
    /// <summary>Áp dụng chuyển hướng theo hợp đồng của lớp, không ghi trạng thái ứng dụng.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
