using AITeam.Core.Accounts;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AITeam.Hub.Pages;

/// <summary>Khung AI &amp; tài khoản; thao tác dùng endpoint /AiAccounts hiện có.</summary>
public class AIModel(IAiAccountsService accounts) : PageModel
{
    /// <summary>Khóa tab chính xác: status, accounts, providers, vault; giá trị khác về status.</summary>
    public string Tab { get; private set; } = "status";

    /// <summary>Danh sách từ List ở status; rỗng ở tab khác hoặc khi đọc lỗi.</summary>
    public IReadOnlyList<AiAccountView> Rows { get; private set; } = Array.Empty<AiAccountView>();

    /// <summary>Lỗi List: "Không đọc được danh sách tài khoản AI."; không lộ chi tiết ngoại lệ.</summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Chỉ nhận bốn khóa tab cho phép, không trim hay đổi hoa/thường.
    /// Status gọi List đúng một lần, không CheckAsync; tab khác không gọi dịch vụ.
    /// Lỗi List trả Rows rỗng và Error "Không đọc được danh sách tài khoản AI.".
    /// </summary>
    public void OnGet(string? tab = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}