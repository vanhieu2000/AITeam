using AITeam.Hub.Models;

namespace AITeam.Hub.Pages.Shared;

/// <summary>R-015 (Claude): hàm hiển thị dùng chung, chuyển từ trang Tổng quan / Đội cũ khi bỏ các trang đó.</summary>
public static class UiFormat
{
    /// <summary>5 ⇒ "h5" … 1 ⇒ "h1"; null hoặc ngoài 1–5 ⇒ "h0".</summary>
    public static string HeatCss(int? score)
        => score is >= 1 and <= 5 ? $"h{score.Value}" : "h0";

    /// <summary>Mã nguyên nhân lỗi ⇒ chữ dễ đọc; mã lạ giữ nguyên.</summary>
    public static string CauseLabel(string code) => code switch
    {
        "TK" => "Task / thiết kế (Claude)",
        "AG" => "Nhân viên (agent)",
        "CK" => "Công cụ / cổng kiểm tra",
        "NV" => "Nghiệp vụ / chủ dự án",
        "HT" => "Hạ tầng / provider",
        _ => code
    };

    /// <summary>Trạng thái task ⇒ (lớp CSS của pill, chữ).</summary>
    public static (string Css, string Text) StatePill(TaskState s) => s switch
    {
        TaskState.Assigned => ("pill run", "Đang làm"),
        TaskState.Review => ("pill rev", "Chờ review"),
        TaskState.NeedsFix => ("pill fix", "Cần sửa"),
        TaskState.Merged => ("pill ok", "Đã merge"),
        _ => ("pill n", "?"),
    };
}
