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
        => BuildCore(input);

    private static OfficeView BuildCore(OfficeInput input)
    {
        var desks = new List<Desk>();

        if (input.CurrentRequest is { } req)
        {
            var feed = (req.Events ?? Array.Empty<RequestEvent>())
                .TakeLast(2)
                .Reverse()
                .Select(e => new TaskMessage(input.Now, req.Id, "Claude", "note", $"{e.Time} {e.Text}"))
                .ToList();
            desks.Add(new Desk("claude", "Claude", "Quản lý · thiết kế · review", DeskState.Manager, req.Status,
                new DeskTask("", req.Id, req.Title, null), feed));
        }
        else
        {
            desks.Add(new Desk("claude", "Claude", "Quản lý · thiết kế · review", DeskState.Manager,
                "Không có phiếu đang làm", null, Array.Empty<TaskMessage>()));
        }

        int working = 0, idle = 0, resting = 0;

        RunningAgent? FindRunning(TaskCard t) =>
            input.Running.FirstOrDefault(r =>
                string.Equals(r.Project, t.Project, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Task, t.Id, StringComparison.OrdinalIgnoreCase));

        foreach (var s in input.Staff.OrderBy(x => x.Agent, StringComparer.OrdinalIgnoreCase))
        {
            var assigned = input.Tasks
                .Where(t => t.State == TaskState.Assigned && string.Equals(t.Agent, s.Agent, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var task = assigned.FirstOrDefault(t => FindRunning(t) != null) ?? assigned.FirstOrDefault();

            if (task != null)
            {
                working++;
                var run = FindRunning(task);
                int? elapsed = run != null ? (int)Math.Floor((input.Now - run.StartedAt).TotalMinutes) : null;
                var stateText = elapsed is { } m ? $"Đang làm {m}′" : "Đang làm (máy khác / chưa chạy)";
                var feed = (input.Messages(task.Project, task.Id) ?? Array.Empty<TaskMessage>())
                    .TakeLast(3)
                    .Reverse()
                    .ToList();
                desks.Add(new Desk(s.Agent, s.Name, s.Role, DeskState.Working, stateText,
                    new DeskTask(task.Project, task.Id, task.Title, elapsed), feed));
            }
            else if (input.Rests.FirstOrDefault(r =>
                string.Equals(r.ProviderId, s.Provider, StringComparison.OrdinalIgnoreCase) && r.Until > input.Now) is { } rest)
            {
                resting++;
                var stateText = $"Nghỉ tới {rest.Until.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture)}";
                desks.Add(new Desk(s.Agent, s.Name, s.Role, DeskState.Resting, stateText, null, Array.Empty<TaskMessage>()));
            }
            else
            {
                idle++;
                desks.Add(new Desk(s.Agent, s.Name, s.Role, DeskState.Idle, "Rảnh", null, Array.Empty<TaskMessage>()));
            }
        }

        var reviewTasks = input.Tasks.Where(t => t.State == TaskState.Review).ToList();
        var reviewCount = reviewTasks.Count;
        if (reviewCount > 0)
        {
            var latest = reviewTasks.OrderByDescending(t => t.LastRun?.Time ?? DateTime.MinValue).First();
            var feed = (input.Messages(latest.Project, latest.Id) ?? Array.Empty<TaskMessage>())
                .TakeLast(3)
                .Reverse()
                .ToList();
            desks.Add(new Desk("gate", "Cổng kiểm tra + Review AI", "build · test · BOM · phạm vi · review AI",
                DeskState.Review, $"{reviewCount} chờ review",
                new DeskTask(latest.Project, latest.Id, latest.Title, null), feed));
        }
        else
        {
            desks.Add(new Desk("gate", "Cổng kiểm tra + Review AI", "build · test · BOM · phạm vi · review AI",
                DeskState.Idle, "Không có việc chờ", null, Array.Empty<TaskMessage>()));
        }

        desks.Add(new Desk("research", "Phòng nghiên cứu", "ChatGPT · Gemini · DeepSeek · Grok",
            DeskState.Idle, "Sắp có (V5-07)", null, Array.Empty<TaskMessage>()));

        return new OfficeView(desks, working, reviewCount, idle, resting, input.Now);
    }
}
