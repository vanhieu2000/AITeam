using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AITeam.Core.Tests;

/// <summary>Kiểm tra tài liệu được giao và giữ nguyên hướng dẫn cũ bằng bản sao byte thật trước task.</summary>
public sealed class AcceptanceS107WorkflowDocsTests
{
    private static string Root([CallerFilePath] string file = "") => Path.GetDirectoryName(Path.GetDirectoryName(file))!;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root(), path));

    [Theory]
    [InlineData("CLAUDE.md", "CLAUDE.md")]
    [InlineData("templates/task.md", "task.md")]
    [InlineData("templates/improvement.md", "improvement.md")]
    public void HuongDanCu_GiuNguyenThuTuVaNoiDung(string path, string fixture)
    {
        var original = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "S1-07", fixture));
        var current = Read(path).Replace("\r\n", "\n");
        var position = 0;
        foreach (var line in original.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var next = current.IndexOf(line, position, StringComparison.Ordinal);
            Assert.True(next >= position, "Mất hoặc sửa hướng dẫn cũ: " + line);
            position = next + line.Length;
        }
    }

    [Theory]
    [InlineData("CLAUDE.md")]
    [InlineData("templates/task.md")]
    [InlineData("templates/improvement.md")]
    public void MoiMau_NhacDungBuoc_NguonVaChongTrung(string path)
    {
        var text = Read(path);
        Assert.Contains("skill-note -Skill", text);
        Assert.Contains("-Text", text);
        Assert.Contains("-Source", text);
        Assert.Contains("TK / AG / CK", text);
        Assert.Contains("lỗi trùng không ghi lại", text);
        Assert.Contains("skill đúng bước", text);
        Assert.Contains(path == "templates/improvement.md" ? "IMP-" : "S1-07", text);
    }

    [Theory]
    [InlineData("Tiếp nhận", "01-tiep-nhan")]
    [InlineData("Ý tưởng", "02-y-tuong")]
    [InlineData("Nghiên cứu", "03-research")]
    [InlineData("Kế hoạch", "04-ke-hoach")]
    [InlineData("Duyệt kế hoạch", "05-duyet-ke-hoach")]
    [InlineData("Chuẩn bị task", "06-chuan-bi-task")]
    [InlineData("Code", "07-code")]
    [InlineData("Kiểm thử", "08-kiem-thu")]
    [InlineData("QC", "09-qc")]
    [InlineData("Nghiệm thu", "10-nghiem-thu")]
    [InlineData("Publish Git", "11-publish-git")]
    public void Map_CoDungAnhXa(string business, string id)
    {
        var rows = Regex.Matches(Read("plans/R-010/map-skill-pending.md"), @"(?m)^\|\s*([^|]+?)\s*\|\s*`([a-z0-9-]+)`\s*\|")
            .Select(match => (Name: match.Groups[1].Value.Trim(), Id: match.Groups[2].Value)).ToArray();
        Assert.Equal(11, rows.Length);
        Assert.Equal(11, rows.Select(row => row.Id).Distinct().Count());
        Assert.Contains(rows, row => row.Name == business && row.Id == id);
    }

    [Fact]
    public void Map_GhiRoHangChoVaRangBuocDocAnToan()
    {
        var text = Read("plans/R-010/map-skill-pending.md");
        foreach (var requirement in new[] { "R-009", "chưa tạo trang Map", "danh sách cho phép", "..", "đường dẫn tuyệt đối", "reparse point", "symlink", "thiếu file", "chỉ đọc", "encode", "quyền duyệt", "publish" })
            Assert.Contains(requirement, text);
    }
}