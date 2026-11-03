# V10-04 · Dịch vụ số liệu Báo cáo & thống kê (IReportService)

- **Agent:** O
- **Nhánh:** `agent-o/V10-04-dich-vu-bao-cao`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~450 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** L

## Tóm tắt
- Không có màn hình mới trong task này (màn Báo cáo + biểu đồ do Claude làm sau, UI-05 / UI-06).
- Một dịch vụ trả đủ số liệu cho màn Báo cáo: lọc theo khoảng ngày / dự án / người / độ khó; 4 chỉ số có so sánh kỳ trước; chuỗi số cho 5 biểu đồ; bảng điểm hiệu suất 0–100; ma trận lỗi nguyên nhân × loại; dòng thời gian từng bước của một yêu cầu; xuất CSV.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.4.
- `hub/Services/PerformanceService.cs` (đọc task, lượt, độ khó, findings — **dùng lại** các hàm đọc có sẵn, có thể tách ra `internal static` nếu cần, KHÔNG đổi hành vi / chữ ký public của nó).
- `hub/Services/HubMetricReader.cs`, `core/AITeam.Core/Metrics/FindingParser.cs`.
- `hub/Services/WorkDetailService.cs` (10 bước của yêu cầu — dùng lại cho dòng thời gian).

## Mục tiêu
File mới `hub/Services/ReportService.cs`. Hợp đồng (Claude chốt — giao diện gọi đúng chữ ký):
```csharp
namespace AITeam.Hub.Services;

public sealed record ReportFilter(DateOnly From, DateOnly To, string? Project = null, string? Person = null, string? Difficulty = null);
// Person: mã nhân viên / AI như bảng điểm hiện có (vd. "codex", "opencode", "A", "B"); Difficulty: "S" | "M" | "L"

public sealed record ReportKpi(string Key, string Label, double? Value, double? Previous, string Unit);   // Key: done | first_pass_pct | ai_minutes | errors_tk ; Value null = chưa đo (KHÔNG thay bằng 0)
public sealed record WeekPoint(string Week, DateOnly Start, int Done, double? FirstPassPct, int ErrorsTk, int ErrorsAg, int ErrorsCk);  // Week = "T44" (ISO)
public sealed record NamedValue(string Name, double Value);
public sealed record ScoreRow(string Person, string Display, int Score, int Tasks, double? FirstPassPct, double? InBudgetPct, double? OnTimePct, int AgErrors, double? AvgMinutes);
public sealed record ErrorCell(string Cause, string Category, int Count, IReadOnlyList<string> Tasks);
public sealed record ReportData(
    ReportFilter Filter,
    IReadOnlyList<ReportKpi> Kpis,
    IReadOnlyList<WeekPoint> Weeks,
    IReadOnlyList<NamedValue> MinutesByProject,
    IReadOnlyList<NamedValue> DifficultyCounts,        // S, M, L, "chưa rõ"
    IReadOnlyList<NamedValue> TasksByPerson,
    IReadOnlyList<ScoreRow> Scores,                    // Score giảm dần
    IReadOnlyList<ErrorCell> ErrorMatrix,              // Cause ∈ TK, AG, CK, HT, khác; Category theo FindingParser
    IReadOnlyList<string> Projects, IReadOnlyList<string> People,   // giá trị cho ô lọc
    IReadOnlyList<string> Warnings);

public sealed record TimelineStep(int Order, string Name, string? Actor, string? Model, DateTimeOffset? Start, DateTimeOffset? End,
    double? Minutes, long? Tokens, int Runs, int Rounds, IReadOnlyList<string> Findings, double? WaitMinutesBefore, IReadOnlyList<string> Tasks);
public sealed record RequestTimeline(string RequestId, string Title, IReadOnlyList<TimelineStep> Steps, double? TotalHours, IReadOnlyList<string> Warnings);

public interface IReportService
{
    ReportData Query(ReportFilter filter, DateTimeOffset? now = null);
    RequestTimeline? Timeline(string requestId);                  // ^R-\d{3,4}$, sai ⇒ ArgumentException; không có ⇒ null
    string ExportCsv(ReportFilter filter, string table);         // table: scores | weeks | errors | tasks ; khác ⇒ ArgumentException
}
```
- **Kỳ trước**: cùng độ dài, ngay trước `From`.
- **Task xong**: task có bản ghi merge / trạng thái Merged trong khoảng (dùng cách xác định "xong" của `PerformanceService` / bảng việc).
- **Điểm** = round(40·đạt vòng 1 + 25·trong ngân sách + 20·đúng giờ + 15·(1 − tỉ lệ task có lỗi AG)), mỗi thành phần 0–1; thành phần chưa đo ⇒ chia lại trọng số cho các thành phần có số (ghi trong code comment). Người < 2 task ⇒ vẫn hiện nhưng Score tính bình thường.
  - Đạt vòng 1: review vòng 1 ĐẠT. Trong ngân sách: dòng thêm ≤ `**Ngân sách:**`. Đúng giờ: phút lượt ≤ `**Thời gian tối đa:**` (mặc định 30).
- **Dòng thời gian**: đọc phiếu `requests/R-xxx-*.md` (nhật ký giờ), task có `**Yêu cầu:** R-xxx`, `metrics/runs`. Mỗi bước: ai làm, model, phút / token từ lượt, số vòng, lỗi `[TK|AG|CK…]` gắn bước code / review, khoảng chờ giữa hai bước liên tiếp (> 30 phút mới ghi).
- **CSV**: UTF-8 có BOM, dấu phẩy, bọc `"` khi cần; chặn chèn công thức: ô bắt đầu bằng `= + - @` ⇒ thêm `'` phía trước.
- Thiếu file / dòng hỏng ⇒ thêm vào `Warnings`, KHÔNG ném lỗi.
- DI: `IReportService` trong `hub/Services/HubV3Registration.cs`.

## Phạm vi (được sửa những gì)
- `hub/Services/ReportService.cs` (mới)
- `hub/Services/PerformanceService.cs` (chỉ tách hàm dùng chung, không đổi public)
- `hub/Services/HubV3Registration.cs`
- `hub.tests/V1004ReportServiceTests.cs` (mới)
- `hub.tests/Fixtures/report/` (mới, dữ liệu giả nhỏ)
- `tasks/V10-04-dich-vu-bao-cao.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không đổi định dạng `metrics/runs`, phiếu, file task.

## Ràng buộc
- Dữ liệu không tin cậy (file task / phiếu sửa tay): đọc giới hạn kích thước như `HubMetricReader.ReadText`, đường dẫn tương đối, không theo reparse point.
- Không biến "chưa đo" thành 0 ở bất kỳ chỉ số nào.
- `Query` trên dữ liệu thật (≈ 200 task, 300 lượt) phải < 1 giây: đọc mỗi file một lần mỗi lần gọi.

## Test nghiệm thu
`hub.tests/V1004ReportServiceTests.cs` với fixture tạm (2 dự án, 3 người, 8 task, 12 lượt, 1 phiếu):
- Lọc dự án / người / độ khó đổi đúng số task.
- KPI kỳ trước đúng khoảng; chỉ số không có lượt ⇒ Value null.
- Weeks: đúng nhãn ISO `T44`, đủ tuần trong khoảng kể cả tuần rỗng (Done 0, FirstPassPct null).
- Score: một ca tính tay khớp; ca thiếu ngân sách ⇒ chia lại trọng số.
- ErrorMatrix: `[TK] (test)` ×2 + `[AG] (giao diện)` ⇒ ô đúng, có danh sách task.
- Timeline R-xxx: đủ bước có trong nhật ký, phút / token cộng từ lượt, chờ > 30 phút được ghi; id `../x` ⇒ ArgumentException.
- ExportCsv: có BOM, ô `=SUM(1)` ⇒ `'=SUM(1)`; table lạ ⇒ ArgumentException.
- File task hỏng ⇒ Warnings, không ném.

## Tiêu chí hoàn thành
- Build 0 warning; toàn bộ test xanh; giữ BOM.
- Commit: `V10-04: dịch vụ số liệu báo cáo, điểm hiệu suất, ma trận lỗi, dòng thời gian (OpenCode)`.

## Báo cáo của agent

## Review (Claude)
