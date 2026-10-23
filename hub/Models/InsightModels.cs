namespace AITeam.Hub.Models;

// HỢP ĐỒNG DỮ LIỆU Hub v3 (R-003) — Claude thiết kế, agent KHÔNG đổi. Thiết kế: docs/design/HUB-V3.md.
// Nguồn: file task (Độ khó, Yêu cầu, Review, Đánh giá quy trình), metrics/runs/*.json, improvements/IMP-*.md, staff/*.md, git log.

public enum Difficulty { Unknown, S, M, L }

/// <summary>Một dòng bảng "## Đánh giá quy trình" của file task. Ô "—" / trống ⇒ chuỗi rỗng; Score ngoài 1–5 ⇒ null.</summary>
public sealed record StageScore(
    int Index,            // 1..7 lấy từ số đầu ô giai đoạn ("4. Giao việc" ⇒ 4); không có số ⇒ 0
    string Stage,         // nguyên văn ô, vd. "4. Giao việc"
    string Who,
    int? Score,
    string Time,
    string Issue,
    string Cause,         // "TK" / "AG" / "CK" / "NV" / "HT" hoặc rỗng
    string Proposal);

/// <summary>Một lỗi review dạng "- [AG|TK|CK|NV|HT] (loại) mô tả" trong mục "## Review (Claude)".</summary>
public sealed record ReviewIssue(string Project, string Task, string Cause, string Kind, string Text);

/// <summary>Một bản ghi metrics/runs/*.json (team.ps1 ghi sau mỗi lượt agent). At = lúc lượt KẾT THÚC.</summary>
public sealed record RunRecord(
    DateTime At,
    string Machine,
    string Project,
    string Task,
    int Run,
    string Agent,
    string Status,
    int DurationSec,
    long TotalTokens,
    int LinesAdded,
    bool VerifyOk,
    bool AutoRetry,
    string? Request);

public enum SegmentKind { Coding, Fix, Review, Merge }

/// <summary>Một đoạn trên biểu đồ thời gian. Merge là điểm: Start == End.</summary>
public sealed record TimelineSegment(SegmentKind Kind, DateTime Start, DateTime End);

/// <summary>Một task đã ghép đủ dữ liệu để vẽ Tổng quan / Chi tiết / Đội ngũ.</summary>
public sealed record TaskInsight(
    string Project,
    string Id,                 // "V4-06"
    string Title,
    string Agent,              // "A" / "B" / "" (Claude tự làm)
    string StaffName,          // "Minh" / "Lan" / mã agent nếu không có hồ sơ / "Claude" nếu Agent rỗng
    Difficulty Difficulty,
    string? Request,           // "R-002" hoặc null (task theo quy trình cũ)
    TaskState State,
    int AgentMinutes,          // tổng DurationSec mọi lượt / 60, làm tròn
    int Runs,                  // số bản ghi metrics
    int ReviewRounds,          // số "### Vòng N" lớn nhất; 0 nếu chưa review
    bool PassedRound1,         // có "### Vòng 1 — ĐẠT"
    bool? FirstRunVerifyOk,    // VerifyOk của lượt đầu; null nếu không có lượt
    double? ProcessScore,      // TB điểm 7 giai đoạn (bỏ ô null), làm tròn 1 số lẻ; null nếu không có điểm
    DateTime? MergedAt,
    IReadOnlyList<StageScore> Stages,
    IReadOnlyList<ReviewIssue> Issues,
    IReadOnlyList<TimelineSegment> Timeline);

/// <summary>Thẻ đánh giá một nhân viên (chỉ tính task có Request != null).</summary>
public sealed record StaffScore(
    string Agent,              // "A"
    string Name,               // "Minh"
    int Tasks,
    int Runs,
    double AvgMinutesPerRun,   // tổng phút / tổng lượt, 1 số lẻ; 0 nếu không có lượt
    double? VerifyFirstPassRate,   // tỉ lệ task có FirstRunVerifyOk == true trên task có lượt; null nếu không có
    double? Round1Rate,            // tỉ lệ PassedRound1 trên task có ReviewRounds > 0; null nếu không có
    IReadOnlyDictionary<string, int> AgErrorsByKind);   // lỗi [AG] theo loại, vd. "giao diện" → 2

/// <summary>Điểm TB một giai đoạn qua các task (Index 1..7).</summary>
public sealed record StageAverage(int Index, string Stage, double? Average, int Count);

/// <summary>Một vấn đề lấy từ dòng "Đánh giá quy trình" có Nguyên nhân.</summary>
public sealed record TeamIssue(string Project, string Task, int StageIndex, string Stage, string Cause, string Issue, string Proposal);

/// <summary>improvements/IMP-xxx-*.md.</summary>
public sealed record Improvement(string Id, string Title, string Status, IReadOnlyList<string> Tasks, string FileName, string Markdown);

/// <summary>
/// Chỉ số Tổng quan trên tối đa 6 task ĐẦU của TeamInsights.Tasks (đã sắp mới nhất trước) có Request != null:
/// TasksCounted = số task đó; Round1Rate = tỉ lệ PassedRound1 trong các task có ReviewRounds &gt; 0 (không có ⇒ null);
/// AvgAgentMinutes = TB AgentMinutes của các task có Runs &gt; 0, 1 số lẻ (không có ⇒ null).
/// </summary>
public sealed record OverviewKpis(double? Round1Rate, double? AvgAgentMinutes, int TasksCounted);

public sealed record TeamInsights(
    IReadOnlyList<TaskInsight> Tasks,          // mới nhất trước (MergedAt hoặc At lượt cuối; không có ⇒ cuối danh sách)
    IReadOnlyList<StaffScore> Staff,           // theo Agent tăng dần
    IReadOnlyList<StageAverage> StageAverages, // đúng 7 phần tử, Index 1..7
    IReadOnlyList<TeamIssue> Issues,
    IReadOnlyList<Improvement> Improvements,   // theo Id tăng dần
    OverviewKpis Kpis);
