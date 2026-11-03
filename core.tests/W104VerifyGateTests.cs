using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

/// <summary>Kiểm cổng cảnh báo test mới tự dựng tiến trình, tài liệu test chuẩn và checklist QC cố định (W1-04).</summary>
public sealed class W104VerifyGateTests
{
    [Fact]
    public async Task Test_moi_co_ProcessStartInfo_thi_canh_bao()
    {
        const string path = "core.tests/W104MoiTests.cs";
        var r = await RunGateAsync([path], NameStatusLiteral(("A", path)));
        Assert.Equal(0, r.Process.Code);
        Assert.Contains(path, r.Process.Output);
        Assert.Contains("ProcessStartInfo", r.Process.Output);
    }

    [Fact]
    public async Task Test_cu_khong_bi_canh_bao_truy_hoi()
    {
        const string path = "core.tests/W104CuTests.cs";
        var r = await RunGateAsync([path], NameStatusLiteral(("M", path)));
        Assert.Equal(0, r.Process.Code);
        Assert.DoesNotContain(path, r.Process.Output);
    }

    [Fact]
    public async Task Helper_Support_duoc_mien()
    {
        const string core = "core.tests/Support/W104CoreHelperTests.cs";
        const string hub = "hub.tests/Support/W104HubHelperTests.cs";
        var r = await RunGateAsync([core, hub],
            NameStatusLiteral(("A", core), ("A", hub)));
        Assert.Equal(0, r.Process.Code);
        Assert.DoesNotContain("W104CoreHelperTests.cs", r.Process.Output);
        Assert.DoesNotContain("W104HubHelperTests.cs", r.Process.Output);
    }

    [Fact]
    public async Task Duong_dan_co_dau_va_khoang_trang_duoc_giu_nguyen()
    {
        const string path = "core.tests/Ca kiểm thử/W104 MớiTests.cs";
        var r = await RunGateAsync([path], NameStatusLiteral(("A", path)));
        Assert.Equal(0, r.Process.Code);
        Assert.Contains(path, r.Process.Output);
    }

    [Fact]
    public async Task Cong_goi_git_voi_name_status_va_null_separator()
    {
        using var sandbox = new TeamScript();
        var r = await sandbox.RunAsync("team.ps1", ["Get-NewTestProcessStartInfoWarnings"], """
            $w = @(Get-NewTestProcessStartInfoWarnings -Work $Root)
            Write-Output $w.Count
            """, [new FakeCommand("git")]);
        Assert.Equal(0, r.Process.Code);
        var call = Assert.Single(r.Calls);
        Assert.Equal("git", call.Name);
        Assert.Contains("--name-status", call.Arguments);
        Assert.Contains("-z", call.Arguments);
        Assert.Contains("--diff-filter=A", call.Arguments);
        Assert.Contains("origin/main...HEAD", call.Arguments);
    }

    [Fact]
    public void Tai_lieu_test_chuan_khong_qua_60_dong_va_neu_helper_that()
    {
        var path = Path.Combine(SourceRoot(), "docs", "TEST-CHUAN.md");
        Assert.True(File.Exists(path), "Thiếu docs/TEST-CHUAN.md.");
        var text = File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd('\n');
        Assert.InRange(text.Split('\n').Length, 1, 60);
        Assert.Contains("TestShell.cs", text);
        Assert.Contains("TeamScript.cs", text);
        Assert.Contains("HubTestHost.cs", text);
    }

    [Fact]
    public void Qc_co_checklist_co_dinh_va_ghi_unverified()
    {
        var text = File.ReadAllText(Path.Combine(SourceRoot(), "skills", "09-qc", "SKILL.md"))
            .Replace("\r\n", "\n");
        Assert.Contains("unverified", text);
        Assert.Contains("verdict", text);
        var checklist = Section(text, "Checklist bắt buộc");
        foreach (var phrase in new[] { "đúng commit", "phạm vi", "test bất biến", "dữ liệu thật", "bảo mật", "hồi quy" })
            Assert.Contains(phrase, checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"(?m)^- \[ \] ", checklist);
    }

    private static async Task<TeamScriptResult> RunGateAsync(string[] files, string nameStatusExpression)
    {
        using var sandbox = new TeamScript();
        foreach (var relative in files)
        {
            var full = Path.Combine(sandbox.Root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "var info = new " + "ProcessStartInfo();");
        }

        var script = "$ns = " + nameStatusExpression + "\n" +
            "$w = @(Get-NewTestProcessStartInfoWarnings -Work $Root -NameStatus $ns)\n" +
            "foreach ($x in $w) { Write-Output $x }";
        return await sandbox.RunAsync("team.ps1", ["Get-NewTestProcessStartInfoWarnings"], script);
    }

    private static string NameStatusLiteral(params (string Status, string Path)[] entries)
    {
        var parts = new List<string>();
        foreach (var (status, path) in entries)
        {
            parts.Add(PsQuote(status));
            parts.Add("[string][char]0");
            parts.Add(PsQuote(path));
            parts.Add("[string][char]0");
        }
        return string.Join(" + ", parts);
    }

    private static string PsQuote(string value) => "'" + value.Replace("'", "''") + "'";

    private static string Section(string text, string title)
    {
        var match = Regex.Match(text, @"(?ms)^## " + Regex.Escape(title) + @"\n(.*?)(?=^## |\z)");
        Assert.True(match.Success, "Thiếu mục " + title + ".");
        return match.Groups[1].Value.Trim();
    }

    private static string SourceRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, ".."));
}
