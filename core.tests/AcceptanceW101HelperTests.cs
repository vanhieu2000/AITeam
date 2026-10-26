using System.Text;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceW101HelperTests
{
    // Chỉ tìm nguồn có tên đã biết; không quét repo và không dùng .git của worktree.
    private static string Source(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, relative);
    }

    [Theory]
    [InlineData("team.ps1")]
    [InlineData("templates/task.md")]
    [InlineData("scripts/SkillNotes.ps1")]
    [InlineData("tools/workflow-common.ps1")]
    public void Copy_keeps_real_source_bytes_and_tree(string relative)
    {
        using var tmp = new TempRepo(false);
        var expected = File.ReadAllBytes(Source(relative));
        var copy = RealFile.Copy(relative, tmp.Root);
        Assert.Equal(Path.GetFullPath(Path.Combine(tmp.Root, relative)), copy);
        Assert.Equal(expected, File.ReadAllBytes(copy));
        File.WriteAllText(copy, "bản sao cũ");
        Assert.Equal(copy, RealFile.Copy(relative, tmp.Root));
        Assert.Equal(expected, File.ReadAllBytes(copy));
        Assert.Equal(expected, File.ReadAllBytes(Source(relative)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../team.ps1")]
    [InlineData("templates/../team.ps1")]
    [InlineData("..\\team.ps1")]
    [InlineData("C:\\team.ps1")]
    [InlineData("C:team.ps1")]
    [InlineData("\\\\localhost\\share\\team.ps1")]
    [InlineData("/team.ps1")]
    [InlineData("team.ps1:stream")]
    public void Copy_rejects_invalid_path_before_writing(string? relative)
    {
        using var tmp = new TempRepo(false);
        var ex = Assert.Throws<ArgumentException>(() => RealFile.Copy(relative!, tmp.Root));
        Assert.Contains("Đường dẫn bản sao không hợp lệ.", ex.Message);
        Assert.Empty(Directory.EnumerateFileSystemEntries(tmp.Root));
    }

    [Fact]
    public void Copy_reports_missing_file()
    {
        using var tmp = new TempRepo(false);
        var ex = Assert.Throws<FileNotFoundException>(() => RealFile.Copy("templates/w101-khong-ton-tai.md", tmp.Root));
        Assert.Contains("Không tìm thấy file nguồn.", ex.Message);
        Assert.Empty(Directory.EnumerateFileSystemEntries(tmp.Root));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Copy_rejects_destination_junction_and_parent(bool parent)
    {
        using var tmp = new TempRepo(false);
        using var outside = new TempRepo(false);
        var link = Path.Combine(tmp.Root, "link");
        var r = await TestShell.PowerShell(tmp.Root,
            $"New-Item -ItemType Junction -Path '{link}' -Target '{outside.Root}' | Out-Null");
        Assert.Equal(0, r.Code);
        try
        {
            var target = parent ? Path.Combine(link, "con") : link;
            var ex = Assert.Throws<ArgumentException>(() => RealFile.Copy("templates/task.md", target));
            Assert.Contains("Đường dẫn bản sao không hợp lệ.", ex.Message);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void Sandbox_copies_real_sources_and_disposes_idempotently()
    {
        var sandbox = new TeamScript();
        string root;
        try
        {
            root = sandbox.Root;
            Assert.NotEqual(Path.GetDirectoryName(Source("team.ps1")), root);
            foreach (var relative in new[] { "team.ps1", "scripts/SkillNotes.ps1", "tools/workflow-common.ps1", "templates/task.md" })
                Assert.Equal(File.ReadAllBytes(Source(relative)), File.ReadAllBytes(Path.Combine(root, relative)));
            File.SetAttributes(Path.Combine(root, "team.ps1"), FileAttributes.ReadOnly);
        }
        finally { sandbox.Dispose(); }
        sandbox.Dispose();
        Assert.False(Directory.Exists(root));
        Assert.True(File.Exists(Source("team.ps1")));
    }

    [Fact]
    public async Task Optional_folders_missing_and_real_AST_function_still_works()
    {
        using var source = new TempRepo(false);
        RealFile.Copy("team.ps1", source.Root);
        using var sandbox = new TeamScript(source.Root);
        var r = await sandbox.RunAsync("team.ps1", ["Get-NextJournalRun"],
            "Get-NextJournalRun $Root 'khong-ton-tai.md' 'W1-01' 'Prep'");
        Assert.Equal(0, r.Process.Code);
        Assert.Equal("1", r.Process.Output.Trim());
        Assert.Empty(r.Calls);
        Assert.False(Directory.Exists(Path.Combine(sandbox.Root, "tools")));
    }

    // Fixture kỹ thuật nối vào BẢN SAO team.ps1 thật, không giả định định dạng dữ liệu nghiệp vụ.
    private static void AppendProbe(TempRepo source, string suffix = "")
    {
        var path = RealFile.Copy("team.ps1", source.Root);
        File.AppendAllText(path, "\r\nthrow 'dispatcher không được chạy'\r\n" +
            "function Invoke-W101Probe { param([string]$Value) Write-Output $Value; Write-Output $Root }\r\n" + suffix,
            new UTF8Encoding(false));
    }

    [Fact]
    public async Task AST_keeps_unicode_BOM_CRLF_filename_and_never_runs_dispatcher()
    {
        using var source = new TempRepo(false);
        AppendProbe(source);
        var relative = "scripts/nguồn có dấu.ps1";
        Directory.CreateDirectory(Path.Combine(source.Root, "scripts"));
        File.Copy(Path.Combine(source.Root, "team.ps1"), Path.Combine(source.Root, relative));
        using var sandbox = new TeamScript(source.Root);
        var bytes = File.ReadAllBytes(Path.Combine(source.Root, relative));
        Assert.Equal(new byte[] { 239, 187, 191 }, bytes.Take(3).ToArray());
        Assert.Contains("\r\n", Encoding.UTF8.GetString(bytes));
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(sandbox.Root, relative)));
        var r = await sandbox.RunAsync(relative, ["Invoke-W101Probe"], "Invoke-W101Probe 'Tiếng Việt'");
        Assert.Equal(0, r.Process.Code);
        Assert.Contains("Tiếng Việt", r.Process.Output);
        Assert.Contains(sandbox.Root, r.Process.Output);
        Assert.DoesNotContain(source.Root, r.Process.Output);
    }

    [Theory]
    [InlineData("function Broken {", "Invoke-W101Probe", "cú pháp PowerShell không hợp lệ.")]
    [InlineData("", "Missing-W101", "hàm PowerShell không hợp lệ.")]
    [InlineData("function Invoke-W101Probe {}", "Invoke-W101Probe", "hàm PowerShell không hợp lệ.")]
    public async Task AST_errors_are_explicit_and_do_not_execute_script(string suffix, string selected, string message)
    {
        using var source = new TempRepo(false);
        AppendProbe(source, suffix);
        using var sandbox = new TeamScript(source.Root);
        var r = await sandbox.RunAsync("team.ps1", [selected], "Write-Output 'không được chạy'");
        Assert.Equal(1, r.Process.Code);
        Assert.Contains("LỖI: " + message, r.Process.Error);
        Assert.DoesNotContain("không được chạy", r.Process.Output);
        Assert.Empty(r.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task Fakes_capture_arguments_codes_unicode_and_never_launch_AI_or_push(int code)
    {
        using var sandbox = new TeamScript();
        var r = await sandbox.RunAsync("team.ps1", ["Get-NextJournalRun"],
            "& agy 'hai từ' 'a;b'; & codex.exe 'Tiếng Việt'; & git push origin 'nhánh thử'; exit $LASTEXITCODE",
            [new("agy", code, "đầu ra", "lỗi giả"), new("codex", code), new("git", code)]);
        Assert.Equal(code, r.Process.Code);
        Assert.Contains("đầu ra", r.Process.Output);
        Assert.Contains("lỗi giả", r.Process.Error);
        Assert.Equal(3, r.Calls.Count);
        Assert.Equal(new[] { "agy", "codex", "git" }, r.Calls.Select(c => c.Name));
        Assert.Equal(new[] { "hai từ", "a;b" }, r.Calls[0].Arguments);
        Assert.Equal(new[] { "Tiếng Việt" }, r.Calls[1].Arguments);
        Assert.Equal(new[] { "push", "origin", "nhánh thử" }, r.Calls[2].Arguments);
        Assert.All(r.Calls, c => Assert.Equal(code, c.Code));
        Assert.Equal("đầu ra", r.Calls[0].Output);
        Assert.Equal("lỗi giả", r.Calls[0].Error);
    }

    [Fact]
    public async Task Default_guards_cover_exe_aliases_and_logs_do_not_leak_between_runs()
    {
        using var sandbox = new TeamScript();
        var r = await sandbox.RunAsync("team.ps1", ["Get-NextJournalRun"],
            "agy.exe x; codex y; git.exe push; git status; throw 'lỗi sau fake'");
        Assert.Equal(1, r.Process.Code);
        Assert.Contains("LỖI: lỗi sau fake", r.Process.Error);
        Assert.Equal(4, r.Calls.Count);
        Assert.All(r.Calls, c => Assert.Equal(0, c.Code));
        var next = await sandbox.RunAsync("team.ps1", ["Get-NextJournalRun"], "Write-Output 'lần mới'");
        Assert.Equal(0, next.Process.Code);
        Assert.Empty(next.Calls);
        Assert.Contains("lần mới", next.Process.Output);
    }

    [Fact]
    public async Task Sandbox_timeout_stops_child_process()
    {
        using var sandbox = new TeamScript();
        await Assert.ThrowsAsync<TimeoutException>(() => sandbox.RunAsync("team.ps1", ["Get-NextJournalRun"],
            "Start-Sleep -Seconds 20; Set-Content 'khong-duoc-ghi.txt' 'sai'", timeoutSeconds: 1));
        Assert.False(File.Exists(Path.Combine(sandbox.Root, "khong-duoc-ghi.txt")));
    }

    [Theory]
    [InlineData("../team.ps1")]
    [InlineData("C:\\team.ps1")]
    public async Task Sandbox_rejects_script_escape(string relative)
    {
        using var sandbox = new TeamScript();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sandbox.RunAsync(relative, ["X"], ""));
        Assert.Contains("Đường dẫn bản sao không hợp lệ.", ex.Message);
    }

    [Fact]
    public async Task Sandbox_validates_function_list_fake_names_and_timeout()
    {
        using var sandbox = new TeamScript();
        foreach (var names in new[] { Array.Empty<string>(), new[] { " " }, new[] { "X", "x" } })
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sandbox.RunAsync("team.ps1", names, ""));
            Assert.Contains("Tham số sandbox không hợp lệ.", ex.Message);
        }
        await Assert.ThrowsAsync<ArgumentException>(() => sandbox.RunAsync("team.ps1", ["X"], "", [new("unknown")]));
        await Assert.ThrowsAsync<ArgumentException>(() => sandbox.RunAsync("team.ps1", ["X"], "", [new("git"), new("GIT")]));
        await Assert.ThrowsAsync<ArgumentException>(() => sandbox.RunAsync("team.ps1", ["X"], "", timeoutSeconds: 0));
    }

    [Fact]
    public void Sandbox_requires_real_team_source()
    {
        using var source = new TempRepo(false);
        var ex = Assert.Throws<FileNotFoundException>(() => new TeamScript(source.Root));
        Assert.Contains("Không tìm thấy file nguồn.", ex.Message);
    }

    [Fact]
    public async Task Sandbox_rejects_source_reparse_without_following_it()
    {
        using var source = new TempRepo(false);
        using var outside = new TempRepo(false);
        RealFile.Copy("team.ps1", source.Root);
        outside.Write("giữ nguyên.txt", "dữ liệu cũ");
        var link = Path.Combine(source.Root, "tools");
        var r = await TestShell.PowerShell(source.Root,
            $"New-Item -ItemType Junction -Path '{link}' -Target '{outside.Root}' | Out-Null");
        Assert.Equal(0, r.Code);
        try
        {
            var ex = Assert.Throws<ArgumentException>(() => new TeamScript(source.Root));
            Assert.Contains("Đường dẫn bản sao không hợp lệ.", ex.Message);
            Assert.Equal("dữ liệu cũ", File.ReadAllText(Path.Combine(outside.Root, "giữ nguyên.txt")));
        }
        finally { Directory.Delete(link); }
    }
}
