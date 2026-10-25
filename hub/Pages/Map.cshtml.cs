using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Dòng nhập Map; Code/Actor phân biệt hoa thường, Note giữ nguyên văn, tối đa 500 đơn vị UTF-16.</summary>
public sealed class MapRoleInput
{
    public string Code { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Note { get; set; } = "";
}

/// <summary>Trang /Map dùng dịch vụ H7-01 đã đăng ký DI; không ghi file trực tiếp hay cấp quyền thực thi/duyệt.</summary>
public sealed class MapModel(IWorkflowRolesService service) : PageModel
{
    [BindProperty] public List<MapRoleInput> Roles { get; set; } = [];
    [BindProperty] public string Revision { get; set; } = "";
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    /// <summary>
    /// Gọi GetRoles một lần, chép đủ 11 dòng và Revision, kể cả mặc định v4 khi cấu hình cũ thiếu roles.
    /// InvalidDataException: “Phân vai không hợp lệ.”; IOException: “Không đọc được cấu hình phân vai.”.
    /// Khi lỗi không dựng form có thể lưu; không ghi file, không hiện exception.Message hay dữ liệu gốc.
    /// </summary>
    public void OnGet()
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <summary>
    /// POST /Map dùng antiforgery mặc định Razor. Yêu cầu đủ 11 mã duy nhất, sáu Actor của H7-01,
    /// Note tối đa 500 đơn vị UTF-16 và Revision không rỗng; Note bỏ trống chuyển thành chuỗi rỗng,
    /// không trim. ModelState sai hoặc dữ liệu sai: “Phân vai không hợp lệ.”, không gọi SaveRoles.
    /// Gọi SaveRoles một lần với revision từ POST, không đọc lại để thay revision. Thành công trả Page,
    /// hiện “Đã lưu phân vai.” và dùng snapshot mới. IOException: “Không lưu được cấu hình phân vai.”;
    /// InvalidDataException: “Phân vai không hợp lệ.”; InvalidOperationException:
    /// “Cấu hình đã thay đổi. Vui lòng tải lại.”. Lỗi giữ form/revision cũ, không báo thành công,
    /// không hiện exception.Message; mọi giá trị được Razor mã hóa.
    /// </summary>
    public IActionResult OnPost()
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
