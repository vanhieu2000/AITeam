using System.Diagnostics;
using System.Text;

namespace AITeam.Core.Tests;

public sealed class AcceptanceS106SkillNotesTests
{
    private const string Id = "06-chuan-bi-task";
    private const string RelativeSkill = "skills/06-chuan-bi-task/SKILL.md";
    private const string Note = "Giữ nguyên tiếng Việt và dữ liệu cũ.";

    [Fact]
    public async Task Cli_rejects_invalid_id_without_running_git()
    {
        using var area = new Area(false);
        var result = await area.Note("..", Note);
        Assert.Equal(1, result.Code);
        Assert.Equal("LỖI: Skill không hợp lệ.", result.Error.Trim());
        Assert.DoesNotContain("Đã ghi", result.Output);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task Inserts_exact_bullet_preserving_real_document(bool crlf, bool bom, bool middle)
    {
        using var area = new Area();
        var nl = crlf ? "\r\n" : "\n";
        var original = area.RealText.Replace("\r\n", "\n").Replace("\n", nl);
        var next = original.IndexOf("## Ví dụ", StringComparison.Ordinal);
        Assert.True(next > 0);
        if (!middle) original = original[..next].TrimEnd('\r', '\n');
        File.WriteAllText(area.SkillFile, original, new UTF8Encoding(bom));
        await area.Git("add", "--", RelativeSkill);
        await area.Git("commit", "-m", "Đặt biến thể fixture");
        var head = await area.Git("rev-parse", "HEAD");
        var result = await area.Note(Id, "  Giữ nguyên\t tiếng Việt và dữ liệu cũ.  ", "  IMP-106  ");
        Assert.Equal(0, result.Code);
        Assert.Contains("Đã ghi và commit bài học.", result.Output);
        var bullet = "- " + Note + " (Nguồn: IMP-106)";
        var expected = middle ? original.Insert(next, bullet + nl) : original + nl + bullet;
        Assert.Equal(Encode(expected, bom), File.ReadAllBytes(area.SkillFile));
        Assert.NotEqual(head, await area.Git("rev-parse", "HEAD"));
        Assert.Equal(RelativeSkill, (await area.Git("diff-tree", "--no-commit-id", "--name-only", "-r", "HEAD")).Trim());
        Assert.Equal("", (await area.Git("status", "--porcelain", "--", RelativeSkill)).Trim());
    }

    [Fact]
    public async Task Duplicate_ignores_source_and_whitespace_without_touching_file_or_head()
    {
        using var area = new Area();
        Assert.Equal(0, (await area.Note(Id, Note, "IMP-106")).Code);
        var bytes = File.ReadAllBytes(area.SkillFile);
        var stamp = File.GetLastWriteTimeUtc(area.SkillFile);
        var head = await area.Git("rev-parse", "HEAD");
        var result = await area.Note(Id, "  Giữ nguyên\t tiếng Việt và dữ liệu cũ. ", "Nguồn khác");
        Assert.Equal(0, result.Code);
        Assert.Contains("Bài học đã tồn tại.", result.Output);
        Assert.Equal(bytes, File.ReadAllBytes(area.SkillFile));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(area.SkillFile));
        Assert.Equal(head, await area.Git("rev-parse", "HEAD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_source_is_not_invented(string source)
    {
        using var area = new Area();
        var result = await area.Note(Id, Note, source);
        Assert.Equal(0, result.Code);
        Assert.Contains("- " + Note + "\r\n", File.ReadAllText(area.SkillFile));
        Assert.DoesNotContain("- " + Note + " (Nguồn:", File.ReadAllText(area.SkillFile));
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("../06-chuan-bi-task")]
    [InlineData("..\\06-chuan-bi-task")]
    [InlineData("C:\\outside")]
    [InlineData("/outside")]
    [InlineData("\\\\server\\share")]
    [InlineData("CON")]
    [InlineData("prn")]
    [InlineData("AuX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    [InlineData("con.txt")]
    [InlineData("lpt9.md")]
    [InlineData("COM9")]
    [InlineData("LPT9")]
    [InlineData("x:y")]
    public async Task Unsafe_id_never_changes_data(string id)
    {
        using var area = new Area();
        await area.AssertFailure(id, Note, "", "Skill không hợp lệ.");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" \t ", "")]
    [InlineData("a\nb", "")]
    [InlineData("a\rb", "")]
    [InlineData("hợp lệ", "a\nb")]
    [InlineData("hợp lệ", "a\rb")]
    [InlineData("LONG_TEXT", "")]
    [InlineData("hợp lệ", "LONG_SOURCE")]
    public async Task Invalid_content_is_rejected_before_write(string text, string source)
    {
        using var area = new Area();
        await area.AssertFailure(Id, text == "LONG_TEXT" ? new string('a', 1001) : text,
            source == "LONG_SOURCE" ? new string('b', 101) : source, "Nội dung ghi chú không hợp lệ.");
    }

    [Fact]
    public async Task Exact_length_limits_are_accepted_as_literal_arguments()
    {
        using var area = new Area();
        var text = "Unicode Việt; $(throw 'sai') ` & \" " + new string('a', 900);
        text = text.PadRight(1000, 'b');
        Assert.Equal(1000, text.Length);
        var result = await area.Note(Id, text, new string('s', 100));
        Assert.Equal(0, result.Code);
        Assert.Contains("- " + text + " (Nguồn: " + new string('s', 100) + ")", File.ReadAllText(area.SkillFile));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_file_or_section_is_reported(bool missingSection)
    {
        using var area = new Area();
        if (missingSection)
            File.WriteAllText(area.SkillFile, area.RealText.Replace("## Lỗi hay gặp", "## Bài học cũ"), new UTF8Encoding(true));
        else File.Delete(area.SkillFile);
        await area.Git("add", "--", RelativeSkill);
        await area.Git("commit", "-m", "Thiếu dữ liệu");
        var head = await area.Git("rev-parse", "HEAD");
        var before = File.Exists(area.SkillFile) ? File.ReadAllBytes(area.SkillFile) : null;
        var result = await area.Note(Id, Note);
        Assert.Equal(1, result.Code);
        Assert.Equal("LỖI: Không tìm thấy skill hoặc mục Lỗi hay gặp.", result.Error.Trim());
        Assert.Equal(head, await area.Git("rev-parse", "HEAD"));
        Assert.Equal(before, File.Exists(area.SkillFile) ? File.ReadAllBytes(area.SkillFile) : null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dirty_skill_in_worktree_or_index_is_never_overwritten(bool staged)
    {
        using var area = new Area();
        File.AppendAllText(area.SkillFile, "\r\nDữ liệu người dùng.", new UTF8Encoding(false));
        if (staged) await area.Git("add", "--", RelativeSkill);
        await area.AssertFailure(Id, Note, "", "File skill đang có thay đổi chưa commit.");
    }

    [Fact]
    public async Task Commit_excludes_other_staged_and_unstaged_files_and_does_not_push()
    {
        using var area = new Area();
        File.WriteAllText(Path.Combine(area.Root, "other.txt"), "sửa chưa stage");
        File.WriteAllText(Path.Combine(area.Root, "staged.txt"), "đã stage");
        await area.Git("add", "--", "staged.txt");
        var staged = await area.Git("show", ":staged.txt");
        // Remote không tồn tại: lệnh đúng vẫn thành công vì không được push.
        await area.Git("remote", "add", "origin", Path.Combine(area.Root, "missing-remote.git"));
        var result = await area.Note(Id, Note);
        Assert.Equal(0, result.Code);
        Assert.Equal(RelativeSkill, (await area.Git("diff-tree", "--no-commit-id", "--name-only", "-r", "HEAD")).Trim());
        Assert.Equal(staged, await area.Git("show", ":staged.txt"));
        Assert.Contains("A  staged.txt", await area.Git("status", "--porcelain"));
        Assert.Contains(" M other.txt", await area.Git("status", "--porcelain"));
        Assert.Equal("sửa chưa stage", File.ReadAllText(Path.Combine(area.Root, "other.txt")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Git_failure_cannot_report_success(bool failingHook)
    {
        using var area = new Area();
        var head = await area.Git("rev-parse", "HEAD");
        if (failingHook)
            File.WriteAllText(Path.Combine(area.Root, ".git", "hooks", "pre-commit"), "#!/bin/sh\nexit 1\n", new UTF8Encoding(false));
        else File.WriteAllText(Path.Combine(area.Root, ".git", "index.lock"), "khóa test");
        var result = await area.Note(Id, Note);
        Assert.Equal(1, result.Code);
        Assert.Equal("LỖI: Không thể ghi hoặc commit bài học.", result.Error.Trim());
        Assert.DoesNotContain("Đã ghi và commit", result.Output);
        Assert.Equal(head, await area.Git("rev-parse", "HEAD"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Junction_outside_skills_is_rejected(bool entireSkills)
    {
        using var area = new Area();
        var outside = Path.Combine(area.Root, "outside");
        Directory.CreateDirectory(Path.Combine(outside, Id));
        var target = entireSkills ? Path.Combine(area.Root, "skills") : Path.GetDirectoryName(area.SkillFile)!;
        var destination = entireSkills ? outside : Path.Combine(outside, Id);
        File.Copy(area.SkillFile, Path.Combine(outside, Id, "SKILL.md"));
        var original = File.ReadAllBytes(Path.Combine(outside, Id, "SKILL.md"));
        Directory.Move(target, target + "-backup");
        var script = "New-Item -ItemType Junction -Path " + Quote(target) + " -Target " + Quote(destination) + " | Out-Null";
        Assert.Equal(0, (await RunPowerShell(area.Root, script)).Code);
        try
        {
            var result = await area.Note(Id, Note);
            Assert.Equal(1, result.Code);
            Assert.Equal("LỖI: Skill không hợp lệ.", result.Error.Trim());
            Assert.Equal(original, File.ReadAllBytes(Path.Combine(outside, Id, "SKILL.md")));
        }
        finally { Directory.Delete(target); }
    }

    [Fact]
    public async Task Existing_real_bullet_is_duplicate_even_with_new_source()
    {
        using var area = new Area();
        var section = area.RealText.IndexOf("## Lỗi hay gặp", StringComparison.Ordinal);
        var bullet = area.RealText[(section + "## Lỗi hay gặp".Length)..]
            .Split('\n').First(line => line.StartsWith("- ", StringComparison.Ordinal)).Trim()[2..];
        var before = File.ReadAllBytes(area.SkillFile);
        var head = await area.Git("rev-parse", "HEAD");
        var result = await area.Note(Id, bullet, "Nguồn mới");
        Assert.Equal(0, result.Code);
        Assert.Contains("Bài học đã tồn tại.", result.Output);
        Assert.Equal(before, File.ReadAllBytes(area.SkillFile));
        Assert.Equal(head, await area.Git("rev-parse", "HEAD"));
    }

    [Fact]
    public async Task Missing_valid_id_is_not_an_invalid_id()
    {
        using var area = new Area();
        await area.AssertFailure("skill-chua-co", Note, "", "Không tìm thấy skill hoặc mục Lỗi hay gặp.");
    }

    [Fact]
    public async Task Same_bullet_outside_target_section_does_not_block_insertion()
    {
        using var area = new Area();
        var original = area.RealText.Replace("## Lỗi hay gặp", "## Lỗi hay gặp (bổ sung)")
            .Replace("## Ví dụ", "## Ví dụ\r\n- " + Note);
        File.WriteAllText(area.SkillFile, original, new UTF8Encoding(true));
        await area.Git("add", "--", RelativeSkill);
        await area.Git("commit", "-m", "Mục có hậu tố và bullet ngoài mục");
        var result = await area.Note(Id, Note);
        Assert.Equal(0, result.Code);
        var final = File.ReadAllText(area.SkillFile);
        var end = final.IndexOf("## Ví dụ", StringComparison.Ordinal);
        Assert.Contains("- " + Note, final[..end]);
        Assert.Contains("- " + Note, final[end..]);
    }
    private static byte[] Encode(string text, bool bom) =>
        (bom ? Encoding.UTF8.GetPreamble() : []).Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    private record Result(int Code, string Output, string Error);

    private static Task<Result> RunPowerShell(string root, string script) => Run(root, "powershell.exe",
        "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
        Convert.ToBase64String(Encoding.Unicode.GetBytes("[Console]::OutputEncoding = New-Object Text.UTF8Encoding $false; $ErrorActionPreference='Stop'; " + script)));

    private static async Task<Result> Run(string root, string exe, params string[] args)
    {
        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
            info.Environment.Remove(key);
        info.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        info.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(root, "empty.gitconfig");
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var arg in args) info.ArgumentList.Add(arg);
        using var process = System.Diagnostics.Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        return new(process.ExitCode, await stdout, await stderr);
    }

    private sealed class Area : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "S106-" + Guid.NewGuid().ToString("N"));
        public string SkillFile => Path.Combine(Root, RelativeSkill);
        public string RealText { get; }
        public Area(bool repository = true)
        {
            Directory.CreateDirectory(Root);
            var runtime = Path.Combine(AppContext.BaseDirectory, "S1-06Runtime");
            foreach (var file in Directory.GetFiles(runtime, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(Root, Path.GetRelativePath(runtime, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "S1-06", Id, "SKILL.md");
            Directory.CreateDirectory(Path.GetDirectoryName(SkillFile)!);
            File.Copy(fixture, SkillFile);
            RealText = File.ReadAllText(SkillFile);
            if (repository)
            {
                Git("init").GetAwaiter().GetResult();
                Git("config", "user.name", "Kiểm thử").GetAwaiter().GetResult();
                Git("config", "user.email", "test@example.invalid").GetAwaiter().GetResult();
                Git("config", "core.autocrlf", "false").GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(Root, "other.txt"), "ban đầu");
                Git("add", ".").GetAwaiter().GetResult();
                Git("commit", "-m", "Dữ liệu mẫu thật").GetAwaiter().GetResult();
            }
        }
        public Task<Result> Note(string id, string text, string source = "") => RunPowerShell(Root,
            "& " + Quote(Path.Combine(Root, "team.ps1")) + " skill-note -Skill " + Quote(id) + " -Text " + Quote(text) + " -Source " + Quote(source) + "; exit $LASTEXITCODE");
        public async Task<string> Git(params string[] args)
        {
            var result = await Run(Root, "git", args);
            Assert.True(result.Code == 0, result.Error + result.Output);
            return result.Output;
        }
        public async Task AssertFailure(string id, string text, string source, string error)
        {
            var before = File.ReadAllBytes(SkillFile);
            var head = await Git("rev-parse", "HEAD");
            var index = await Git("diff", "--cached", "--binary");
            var result = await Note(id, text, source);
            Assert.Equal(1, result.Code);
            Assert.Equal("LỖI: " + error, result.Error.Trim());
            Assert.DoesNotContain("Đã ghi và commit", result.Output);
            Assert.Equal(before, File.ReadAllBytes(SkillFile));
            Assert.Equal(head, await Git("rev-parse", "HEAD"));
            Assert.Equal(index, await Git("diff", "--cached", "--binary"));
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
}
