using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace AITeam.Hub.Pages.Shared;

/// <summary>
/// Biểu đồ SVG vẽ phía server cho màn Báo cáo (R-014 UI-05, thiết kế mục 4.4 + 6): không thêm thư viện JS.
/// Mọi chữ đi qua HtmlEncoder. Màu lấy từ biến CSS (--c1..--c4) để chạy cả nền sáng / tối.
/// Mỗi biểu đồ có &lt;title&gt; + role="img" + aria-label; bảng số thay thế do trang tự in.
/// </summary>
public static class SvgCharts
{
    public sealed record Bar(string Label, double Value, string? Href = null);

    public sealed record Series(string Name, string CssVar, IReadOnlyList<double> Values);

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static string E(string s) => HtmlEncoder.Default.Encode(s);

    private static string F(double v) => v.ToString("0.#", Inv);

    /// <summary>Số đẹp cho trục: 1, 2, 5 × 10^n ≥ max.</summary>
    public static double NiceMax(double max)
    {
        if (max <= 0 || double.IsNaN(max)) return 1;
        var exp = Math.Pow(10, Math.Floor(Math.Log10(max)));
        foreach (var m in new[] { 1d, 2d, 5d, 10d })
            if (m * exp >= max) return m * exp;
        return 10 * exp;
    }

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    /// <summary>Thanh ngang (so sánh giữa nhóm). Nhãn dài bị cắt, nhãn đầy đủ nằm trong &lt;title&gt;.</summary>
    public static IHtmlContent HBars(string title, IReadOnlyList<Bar> bars, string unit, string cssVar = "--c1")
    {
        const int w = 520, labelW = 150, rowH = 26, top = 6, valueW = 64;
        var h = top * 2 + Math.Max(1, bars.Count) * rowH;
        var max = NiceMax(bars.Count == 0 ? 0 : bars.Max(b => b.Value));
        var plotW = w - labelW - valueW;
        var sb = new StringBuilder();
        sb.Append($"<svg class=\"chart\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-label=\"{E(title)}\" preserveAspectRatio=\"xMinYMin meet\"><title>{E(title)}</title>");
        if (bars.Count == 0)
            sb.Append($"<text x=\"0\" y=\"20\" class=\"ch-empty\">Chưa có số liệu trong khoảng này.</text>");
        for (var i = 0; i < bars.Count; i++)
        {
            var b = bars[i];
            var y = top + i * rowH;
            var bw = Math.Max(b.Value > 0 ? 2 : 0, b.Value / max * plotW);
            var tip = $"{b.Label}: {F(b.Value)} {unit}";
            if (b.Href != null) sb.Append($"<a href=\"{E(b.Href)}\">");
            sb.Append($"<g class=\"ch-row\"><title>{E(tip)}</title>");
            sb.Append($"<text x=\"{labelW - 8}\" y=\"{y + 17}\" text-anchor=\"end\" class=\"ch-lbl\">{E(Clip(b.Label, 22))}</text>");
            sb.Append($"<rect x=\"{labelW}\" y=\"{y + 5}\" width=\"{F(bw)}\" height=\"{rowH - 10}\" rx=\"3\" style=\"fill:var({cssVar})\"/>");
            sb.Append($"<text x=\"{F(labelW + bw + 6)}\" y=\"{y + 17}\" class=\"ch-val\">{E(F(b.Value))}</text></g>");
            if (b.Href != null) sb.Append("</a>");
        }
        sb.Append("</svg>");
        return new HtmlString(sb.ToString());
    }

    /// <summary>
    /// Cột (chồng nếu nhiều dãy) theo nhãn trục X, tùy chọn kèm đường % (trục phải 0–100).
    /// </summary>
    public static IHtmlContent Columns(string title, IReadOnlyList<string> xLabels, IReadOnlyList<Series> stacks, string unit,
        IReadOnlyList<double?>? linePct = null, string lineName = "")
    {
        const int w = 560, h = 240, left = 36, right = 40, top = 12, bottom = 34;
        var n = xLabels.Count;
        var plotW = w - left - right;
        var plotH = h - top - bottom;
        var totals = Enumerable.Range(0, n).Select(i => stacks.Sum(s => i < s.Values.Count ? s.Values[i] : 0)).ToList();
        var max = NiceMax(totals.Count == 0 ? 0 : totals.Max());
        var sb = new StringBuilder();
        sb.Append($"<svg class=\"chart\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-label=\"{E(title)}\" preserveAspectRatio=\"xMinYMin meet\"><title>{E(title)}</title>");

        // Lưới + trục trái.
        for (var t = 0; t <= 4; t++)
        {
            var y = top + plotH - plotH * t / 4.0;
            sb.Append($"<line x1=\"{left}\" x2=\"{w - right}\" y1=\"{F(y)}\" y2=\"{F(y)}\" class=\"ch-grid\"/>");
            sb.Append($"<text x=\"{left - 6}\" y=\"{F(y + 4)}\" text-anchor=\"end\" class=\"ch-ax\">{F(max * t / 4)}</text>");
            if (linePct != null)
                sb.Append($"<text x=\"{w - right + 6}\" y=\"{F(y + 4)}\" class=\"ch-ax\">{t * 25}%</text>");
        }
        if (n == 0)
        {
            sb.Append($"<text x=\"{left + 8}\" y=\"{top + plotH / 2}\" class=\"ch-empty\">Chưa có số liệu trong khoảng này.</text></svg>");
            return new HtmlString(sb.ToString());
        }

        var slot = plotW / (double)n;
        var barW = Math.Min(36, slot * 0.6);
        var labelEvery = (int)Math.Ceiling(n / 12.0);
        for (var i = 0; i < n; i++)
        {
            var cx = left + slot * i + slot / 2;
            double acc = 0;
            var tip = new StringBuilder(xLabels[i]);
            foreach (var s in stacks)
            {
                var v = i < s.Values.Count ? s.Values[i] : 0;
                if (v > 0)
                {
                    var bh = v / max * plotH;
                    var y = top + plotH - (acc + v) / max * plotH;
                    sb.Append($"<rect x=\"{F(cx - barW / 2)}\" y=\"{F(y)}\" width=\"{F(barW)}\" height=\"{F(bh)}\" style=\"fill:var({s.CssVar})\"><title>{E($"{xLabels[i]} · {s.Name}: {F(v)} {unit}")}</title></rect>");
                }
                acc += v;
                tip.Append($" · {s.Name} {F(v)}");
            }
            if (i % labelEvery == 0)
                sb.Append($"<text x=\"{F(cx)}\" y=\"{h - bottom + 16}\" text-anchor=\"middle\" class=\"ch-ax\">{E(Clip(xLabels[i], 9))}</text>");
        }

        if (linePct != null)
        {
            var pts = new List<string>();
            for (var i = 0; i < n && i < linePct.Count; i++)
            {
                if (linePct[i] is not { } p) continue;
                var cx = left + slot * i + slot / 2;
                var y = top + plotH - Math.Clamp(p, 0, 100) / 100 * plotH;
                pts.Add($"{F(cx)},{F(y)}");
                sb.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(y)}\" r=\"3.5\" class=\"ch-dot\"><title>{E($"{xLabels[i]} · {lineName}: {F(p)}%")}</title></circle>");
            }
            if (pts.Count > 1)
                sb.Append($"<polyline points=\"{string.Join(' ', pts)}\" class=\"ch-line\"/>");
        }
        sb.Append("</svg>");
        return new HtmlString(sb.ToString());
    }

    /// <summary>Một thanh chồng 100% (tỉ trọng nhóm, ví dụ độ khó S/M/L). Luôn kèm chú giải có số.</summary>
    public static IHtmlContent StackedShare(string title, IReadOnlyList<(string Name, double Value, string CssVar)> parts)
    {
        const int w = 520, h = 30;
        var total = parts.Sum(p => p.Value);
        var sb = new StringBuilder();
        sb.Append($"<svg class=\"chart chart-thin\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-label=\"{E(title)}\" preserveAspectRatio=\"xMinYMin meet\"><title>{E(title)}</title>");
        if (total <= 0)
        {
            sb.Append("<text x=\"0\" y=\"20\" class=\"ch-empty\">Chưa có số liệu trong khoảng này.</text></svg>");
            return new HtmlString(sb.ToString());
        }
        double x = 0;
        foreach (var (name, value, css) in parts)
        {
            if (value <= 0) continue;
            var pw = value / total * w;
            var pct = value / total * 100;
            sb.Append($"<rect x=\"{F(x)}\" y=\"2\" width=\"{F(pw)}\" height=\"{h - 4}\" style=\"fill:var({css})\"><title>{E($"{name}: {F(value)} ({F(pct)}%)")}</title></rect>");
            if (pw > 44)
                sb.Append($"<text x=\"{F(x + pw / 2)}\" y=\"{h / 2 + 4}\" text-anchor=\"middle\" class=\"ch-in\">{E(name)} {F(value)}</text>");
            x += pw;
        }
        sb.Append("</svg>");
        return new HtmlString(sb.ToString());
    }
}
