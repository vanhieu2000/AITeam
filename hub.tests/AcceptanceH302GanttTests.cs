using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-02 phần biểu đồ thời gian (Claude viết trước — agent KHÔNG sửa).
// Mốc giờ lấy từ V4-03 / V4-06 thật (23–24/10/2026). Khung 100 phút, vùng vẽ 1000px ⇒ 1 phút = 10px, X = 150 + phút·10.
public sealed class AcceptanceH302GanttTests
{
    private static DateTime T(string hhmmss, int day = 23) => new DateTime(2026, 10, day).Add(TimeSpan.Parse(hhmmss));

    private static TaskInsight Task(string id, string staff, params TimelineSegment[] tl) => new(
        "AITeam", id, id, "A", staff, Difficulty.M, "R-002", TaskState.Merged, 0, 0, 1, true, true, null, null,
        Array.Empty<StageScore>(), Array.Empty<ReviewIssue>(), tl);

    private static readonly TaskInsight V406 = Task("V4-06", "Lan",
        new TimelineSegment(SegmentKind.Coding, T("23:43:00"), T("23:48:00")),
        new TimelineSegment(SegmentKind.Fix, T("23:47:17"), T("23:51:17")),
        new TimelineSegment(SegmentKind.Review, T("23:51:17"), T("23:59:33")),
        new TimelineSegment(SegmentKind.Merge, T("23:59:33"), T("23:59:33")));

    private static readonly TaskInsight V403 = Task("V4-03", "Minh",
        new TimelineSegment(SegmentKind.Coding, T("23:32:00"), T("23:41:00")),
        new TimelineSegment(SegmentKind.Review, T("23:41:00"), T("23:45:38")),
        new TimelineSegment(SegmentKind.Merge, T("23:45:38"), T("23:45:38")));

    private static readonly TaskInsight T007 = Task("T-007", "Minh",
        new TimelineSegment(SegmentKind.Merge, T("17:28:44", 21), T("17:28:44", 21)));

    private static readonly TaskInsight NoTimeline = Task("H3-09", "Lan");

    private static readonly TaskInsight[] All = { V406, NoTimeline, T007, V403 };

    [Fact]
    public void Rows_bars_and_tooltips_in_100_minute_window()
    {
        var c = GanttLayout.Build(All, T("23:00:00"), T("00:40:00", 24), width: 1160);

        Assert.Equal(new[] { "V4-03", "V4-06" }, c.Rows.Select(r => r.Id)); // T-007 ngoài khung, H3-09 không có đoạn; cũ ở trên
        Assert.Equal(("V4-03 · Minh", 10.0), (c.Rows[0].Label, c.Rows[0].Y));
        Assert.Equal(44.0, c.Rows[1].Y);
        Assert.Equal(100.0, c.Height);              // 10 + 2·34 + 22
        Assert.Equal(Difficulty.M, c.Rows[0].Difficulty);

        var b = c.Rows[1].Bars;
        Assert.Equal(new[] { "coding", "fix", "review", "merge" }, b.Select(x => x.Css));
        Assert.Equal((580.0, 50.0), (b[0].X, b[0].Width));
        Assert.Equal((622.8, 40.0), (b[1].X, b[1].Width));
        Assert.Equal((662.8, 82.7), (b[2].X, b[2].Width));
        Assert.Equal((745.5, 3.0), (b[3].X, b[3].Width));  // điểm merge vẫn rộng tối thiểu 3px

        Assert.Equal("Code 23:43–23:48 (5′)", b[0].Tooltip);
        Assert.Equal("Sửa lại 23:47–23:51 (4′)", b[1].Tooltip);
        Assert.Equal("Review 23:51–23:59 (8′)", b[2].Tooltip);
        Assert.Equal("Merge 23:59", b[3].Tooltip);

        Assert.Equal((470.0, 90.0), (c.Rows[0].Bars[0].X, c.Rows[0].Bars[0].Width));
    }

    [Fact]
    public void Ticks_choose_step_with_at_most_8_marks()
    {
        var c = GanttLayout.Build(All, T("23:00:00"), T("00:40:00", 24), width: 1160);
        Assert.Equal(new[] { "23:00", "23:20", "23:40", "00:00", "00:20", "00:40" }, c.Ticks.Select(t => t.Label)); // bước 10′ ra 11 mốc ⇒ 20′
        Assert.Equal(new[] { 150.0, 350.0, 550.0, 750.0, 950.0, 1150.0 }, c.Ticks.Select(t => t.X));

        var shifted = GanttLayout.Build(All, T("23:05:00"), T("00:45:00", 24), width: 1160);
        Assert.Equal(new[] { "23:20", "23:40", "00:00", "00:20", "00:40" }, shifted.Ticks.Select(t => t.Label));

        var week = GanttLayout.Build(All, new DateTime(2026, 10, 21), new DateTime(2026, 10, 28));
        Assert.Equal(new[] { "21/10", "22/10", "23/10", "24/10", "25/10", "26/10", "27/10", "28/10" }, week.Ticks.Select(t => t.Label));

        var tenDays = GanttLayout.Build(All, new DateTime(2026, 10, 21), new DateTime(2026, 10, 31));
        Assert.Equal(new[] { "21/10", "23/10", "25/10", "27/10", "29/10", "31/10" }, tenDays.Ticks.Select(t => t.Label));
    }

    [Fact]
    public void Segments_outside_window_are_clamped()
    {
        // Khung 23:45–00:05 (20 phút) ⇒ 1 phút = 50px. Đoạn Code của V4-03 nằm hẳn trước khung ⇒ dính mép trái, rộng tối thiểu.
        var c = GanttLayout.Build(new[] { V403 }, T("23:45:00"), T("00:05:00", 24), width: 1160);
        var bars = Assert.Single(c.Rows).Bars;
        Assert.Equal((150.0, 3.0), (bars[0].X, bars[0].Width));
        Assert.Equal((150.0, 31.7), (bars[1].X, bars[1].Width));
        Assert.All(c.Rows.SelectMany(r => r.Bars), x => Assert.True(x.X >= 150 && x.X + x.Width <= 1160 - 10 + 3));
    }

    [Fact]
    public void Max_rows_keeps_latest_tasks()
    {
        var c = GanttLayout.Build(All, new DateTime(2026, 10, 21), new DateTime(2026, 10, 25), maxRows: 2);
        Assert.Equal(new[] { "V4-03", "V4-06" }, c.Rows.Select(r => r.Id)); // T-007 cũ nhất bị bỏ
        Assert.Throws<ArgumentException>(() => GanttLayout.Build(All, T("23:00:00"), T("23:00:00")));
    }

    [Fact]
    public void Tooltip_shows_date_when_segment_crosses_midnight()
    {
        var t = Task("X-01", "Lan", new TimelineSegment(SegmentKind.Review, T("23:50:00"), T("00:10:00", 24)));
        var c = GanttLayout.Build(new[] { t }, T("23:00:00"), T("01:00:00", 24));
        Assert.Equal("Review 23:50–00:10 24/10 (20′)", c.Rows[0].Bars[0].Tooltip);
    }

    [Fact]
    public void Fit_window_short_span_is_widened_to_30_minutes_then_padded()
    {
        // V4-03 + V4-06: 23:32:00 → 23:59:33 (27,55′ < 30′) ⇒ 30′ quanh tâm 23:45:46,5 ⇒ đệm 3% (54 giây) mỗi bên.
        var (from, to) = GanttLayout.FitWindow(new[] { V406, NoTimeline, V403 }, T("01:00:00", 24));
        Assert.Equal(T("23:29:52.5"), from);
        Assert.Equal(T("00:01:40.5", 24), to);

        var (f1, t1) = GanttLayout.FitWindow(All, T("01:00:00", 24), maxRows: 1); // chỉ V4-06: 23:43:00 → 23:59:33
        Assert.Equal(T("23:35:22.5"), f1);
        Assert.Equal(T("00:07:10.5", 24), t1);
    }

    [Fact]
    public void Fit_window_long_span_and_empty()
    {
        var (from, to) = GanttLayout.FitWindow(new[] { T007, V406 }, T("01:00:00", 24));
        var span = T("23:59:33") - T("17:28:44", 21);
        Assert.True(Math.Abs((from - (T("17:28:44", 21) - span * 0.03)).TotalSeconds) < 1);
        Assert.True(Math.Abs((to - (T("23:59:33") + span * 0.03)).TotalSeconds) < 1);

        var now = T("08:00:00", 24);
        Assert.Equal((now.AddHours(-24), now), GanttLayout.FitWindow(new[] { NoTimeline }, now));
    }
}
