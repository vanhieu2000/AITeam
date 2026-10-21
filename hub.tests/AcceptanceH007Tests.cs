using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

/// <summary>
/// Test nghiệm thu H-007 (Claude viết trước khi giao — agent KHÔNG sửa file này).
/// Hợp đồng: tasks/H-007-hub-v2-project-task.md mục "Mục tiêu" 3.
/// Mẫu review rút gọn từ file task thật (HDVTool T-006, ChinaAndroidToolbox C-011, C-013) ngày 21/10/2026.
/// </summary>
public sealed class AcceptanceH007Tests
{
    private const string RealReview = """
        ### Vòng 1 — CẦN SỬA
        Đã kiểm tra: cổng kiểm tra `verify` ĐẠT (build 0 warning, 175/175 test).

        Vấn đề chung: nhiều trang đặt `BodyScrolls = false`.

        **Blocker**
        1. `DubbingView` — cột Tùy chọn bên phải TRỐNG.

        **Required**
        2. Công cụ IT › Sao lưu: cột 3 tràn và bị cắt.
        3. Công cụ IT — tiêu đề: dòng mô tả bị menu đè.

        **Optional** (hàng chờ): trang Giới thiệu cột "Hệ thống" 2 ô xếp dọc.

        ### Vòng 2 — CẦN SỬA
        **Required**
        1. **Trang chủ hỏng** (`home.png` mới): 3 thẻ co còn ~30px.
        - `clip.txt` còn `it-security`.

        ### Kiểm tra giữa chừng (sau 4 lượt bị dừng vì giới hạn credits) — CẦN LÀM NỐT
        1. Xóa `out/` khỏi git.
        2. Cài đặt › ADB: hàng nút bị cắt.

        ### Vòng 3 — ĐẠT
        Đã kiểm tra: build 0 warning; 198/198 test.
        **Optional** (hàng chờ): mô tả ô "Dùng gần đây" mất ký tự "&".
        """;

    [Fact]
    public void Parse_splits_rounds_by_h3_and_reads_verdicts()
    {
        var rounds = ReviewParser.Parse(RealReview);
        Assert.Equal(4, rounds.Count);
        Assert.Equal("Vòng 1 — CẦN SỬA", rounds[0].Title);
        Assert.Equal(ReviewVerdict.NeedsFix, rounds[0].Verdict);
        Assert.Equal(ReviewVerdict.NeedsFix, rounds[1].Verdict);
        Assert.Equal(ReviewVerdict.NeedsFix, rounds[2].Verdict);   // "CẦN LÀM NỐT"
        Assert.Equal(ReviewVerdict.Pass, rounds[3].Verdict);       // "ĐẠT" — dù vòng 1 có chữ "ĐẠT" trong nội dung, chỉ xét tiêu đề
    }

    [Fact]
    public void Parse_assigns_list_items_to_severity_groups()
    {
        var r1 = ReviewParser.Parse(RealReview)[0];
        Assert.Contains(r1.Items, i => i.Severity == ReviewSeverity.Blocker && i.Text.Contains("DubbingView"));
        Assert.Equal(2, r1.Items.Count(i => i.Severity == ReviewSeverity.Required));
        Assert.DoesNotContain(r1.Items, i => i.Text.StartsWith("1.") || i.Text.StartsWith("- "));   // bỏ ký hiệu danh sách
    }

    [Fact]
    public void Parse_inline_optional_line_counts_as_optional_item()
    {
        var r1 = ReviewParser.Parse(RealReview)[0];
        Assert.Contains(r1.Items, i => i.Severity == ReviewSeverity.Optional && i.Text.Contains("Giới thiệu"));
        var r4 = ReviewParser.Parse(RealReview)[3];
        Assert.Contains(r4.Items, i => i.Severity == ReviewSeverity.Optional && i.Text.Contains("Dùng gần đây"));
    }

    [Fact]
    public void Parse_bullets_and_numbers_under_required_and_items_without_group_are_None()
    {
        var rounds = ReviewParser.Parse(RealReview);
        Assert.Equal(2, rounds[1].Items.Count(i => i.Severity == ReviewSeverity.Required));
        Assert.Equal(2, rounds[2].Items.Count);
        Assert.All(rounds[2].Items, i => Assert.Equal(ReviewSeverity.None, i.Severity));
    }

    [Fact]
    public void Parse_keeps_round_markdown_for_display()
    {
        var r1 = ReviewParser.Parse(RealReview)[0];
        Assert.Contains("BodyScrolls = false", r1.Markdown);
        Assert.DoesNotContain("Vòng 2", r1.Markdown);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<!-- Claude điền -->")]
    public void Parse_empty_or_placeholder_gives_no_rounds_or_one_other_round(string? md)
    {
        var rounds = ReviewParser.Parse(md);
        Assert.True(rounds.Count == 0 || (rounds.Count == 1 && rounds[0].Verdict == ReviewVerdict.Other && rounds[0].Items.Count == 0));
    }

    [Fact]
    public void Parse_text_without_h3_is_single_other_round()
    {
        var rounds = ReviewParser.Parse("**Required**\n1. Sửa A\n");
        var r = Assert.Single(rounds);
        Assert.Equal(ReviewVerdict.Other, r.Verdict);
        Assert.Single(r.Items, i => i.Severity == ReviewSeverity.Required && i.Text.Contains("Sửa A"));
    }

    // ───── BoardViews.MergedPreview ─────

    private static TaskCard Card(string id, TaskState state) =>
        new("ChinaAndroidToolbox", id, "Tiêu đề " + id, "A", "agent-a/" + id, state, id + ".md", 0, null);

    [Fact]
    public void MergedPreview_orders_by_number_desc_takes_7_and_counts_rest()
    {
        var cards = new[] { "C-001", "C-002", "C-003", "C-004", "C-005", "C-007", "C-008", "C-009", "C-010", "C-013", "C-011" }
            .Select(id => Card(id, id == "C-011" ? TaskState.Assigned : TaskState.Merged))
            .ToList();

        var (items, remaining) = BoardViews.MergedPreview(cards);

        Assert.Equal(new[] { "C-013", "C-010", "C-009", "C-008", "C-007", "C-005", "C-004" }, items.Select(c => c.Id).ToArray());
        Assert.Equal(3, remaining);
    }

    [Fact]
    public void MergedPreview_handles_few_items_and_non_numeric_ids()
    {
        var (items, remaining) = BoardViews.MergedPreview(new[] { Card("H-002", TaskState.Merged), Card("X", TaskState.Merged), Card("H-010", TaskState.Merged) });
        Assert.Equal(3, items.Count);
        Assert.Equal("H-010", items[0].Id);
        Assert.Equal(0, remaining);
        Assert.Empty(BoardViews.MergedPreview(Array.Empty<TaskCard>()).Items);
    }
}
