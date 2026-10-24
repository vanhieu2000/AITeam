using System.Globalization;
using AITeam.Core.Messages;
using AITeam.Core.Routing;
using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

// Trang "Văn phòng" (R-005 / V5-04). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C2; mockup docs/design/v5-van-phong-mini-mockup.html.
// OfficeBuilder là HÀM THUẦN (dữ liệu vào ⇒ bàn làm việc ra) — Claude chốt model, agent điền TODO(agent).

public enum DeskState { Idle, Working, Review, Resting, Manager }

/// <summary>Thành viên có bàn: mã agent (A/B/C), tên ("Minh"), vai trò hiển thị, provider chạy ("agy" / "codex").</summary>
public sealed record StaffMember(string Agent, string Name, string Role, string Provider);

/// <summary>Việc đang hiện trên bàn. ElapsedMin null ⇒ không biết (chạy máy khác / chưa chạy).</summary>
public sealed record DeskTask(string Project, string Id, string Title, int? ElapsedMin);

/// <summary>Một bàn. Key: "claude" | mã agent ("A") | "gate" | "research". Feed: tối đa 3 tin MỚI NHẤT TRƯỚC.</summary>
public sealed record Desk(string Key, string Name, string Role, DeskState State, string StateText, DeskTask? Task, IReadOnlyList<TaskMessage> Feed);

public sealed record OfficeView(IReadOnlyList<Desk> Desks, int Working, int Review, int Idle, int Resting, DateTime At);

/// <summary>Đầu vào. Messages(project, taskId) ⇒ tin nhắn của task (thứ tự thời gian tăng dần như MessageStore.Read).</summary>
public sealed record OfficeInput(
    DateTime Now,
    IReadOnlyList<StaffMember> Staff,
    IReadOnlyList<TaskCard> Tasks,
    IReadOnlyList<RunningAgent> Running,
    IReadOnlyList<ProviderRest> Rests,
    Func<string, string, IReadOnlyList<TaskMessage>> Messages,
    RequestThread? CurrentRequest);

public static class OfficeBuilder
{
    /// <summary>
    /// Thứ tự bàn: "claude", rồi Staff theo Agent tăng dần, rồi "gate", rồi "research".
    ///
    /// Bàn agent (Name = StaffMember.Name, Role = StaffMember.Role):
    /// - Task đang giao = TaskCard có Agent == mã agent (không phân biệt hoa thường) và State == Assigned; nhiều task ⇒ ưu tiên task đang có trong Running (so Project + Task, không phân biệt hoa thường), rồi theo thứ tự danh sách.
    /// - Có task ⇒ State Working; ElapsedMin = floor((Now − Running.StartedAt).TotalMinutes) nếu đang chạy trên máy này, ngược lại null;
    ///   StateText = "Đang làm {n}′" hoặc "Đang làm (máy khác / chưa chạy)".
    /// - Không task, provider của agent đang nghỉ (Rests: ProviderId == Provider, không phân biệt hoa thường, Until &gt; Now) ⇒ Resting, StateText "Nghỉ tới HH:mm dd/MM" (InvariantCulture).
    /// - Còn lại ⇒ Idle, StateText "Rảnh".
    /// - Feed = 3 tin cuối của task đang làm (Messages(project, id)), MỚI NHẤT TRƯỚC; không task ⇒ rỗng.
    ///
    /// Bàn "claude": Name "Claude", Role "Quản lý · thiết kế · review", State Manager.
    /// - CurrentRequest có ⇒ Task = DeskTask("", Id, Title, null), StateText = Status; Feed = 2 sự kiện CUỐI của Events (mới nhất trước),
    ///   mỗi cái TaskMessage(Now, Id, "Claude", "note", "{Time} {Text}").
    /// - Không có ⇒ Task null, StateText "Không có phiếu đang làm", Feed rỗng.
    ///
    /// Bàn "gate": Name "Cổng kiểm tra + Review AI", Role "build · test · BOM · phạm vi · review AI".
    /// - n = số TaskCard State == Review. n &gt; 0 ⇒ State Review, StateText "{n} chờ review",
    ///   Task = task Review có LastRun.Time lớn nhất (LastRun null coi như nhỏ nhất; bằng nhau ⇒ thứ tự danh sách), ElapsedMin null.
    /// - n = 0 ⇒ Idle, StateText "Không có việc chờ", Task null.
    ///
    /// Bàn "research": Name "Phòng nghiên cứu", Role "ChatGPT · Gemini · DeepSeek · Grok", State Idle, StateText "Sắp có (V5-07)", Task null, Feed rỗng.
    ///
    /// Đếm: Working / Idle / Resting = số BÀN AGENT ở trạng thái đó; Review = n. At = Now.
    /// </summary>
    public static OfficeView Build(OfficeInput input)
        => throw new NotImplementedException(); // TODO(agent)
}
