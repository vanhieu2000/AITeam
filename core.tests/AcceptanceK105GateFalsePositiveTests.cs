using System.Text.RegularExpressions;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

// Test nghiệm thu K1-05 (Claude, R-015 hàng chờ mục 1). Cổng kiểm tra báo nhầm 2 kiểu (04/11):
// - F1-01: commit "F1-01: đăng ký DI sẵn trong khung (Claude)" (SAU commit đầu "Test nghiệm thu") bị tính "sửa NGOÀI PHẠM VI".
// - F1-05: dữ liệu test (trích AGENTS.md trong chuỗi raw) có chữ `Assert.True(true)` bị báo "test có thể luôn pass".
public sealed class AcceptanceK105GateFalsePositiveTests
{
    private const string Manager = "vanhieu2000@users.noreply.github.com";

    private static string LoadFunctions()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "team.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root!.FullName, "team.ps1").Replace("'", "''");
        return """
            $tokens=$null; $errors=$null
            $ast=[Management.Automation.Language.Parser]::ParseFile('__SOURCE__',[ref]$tokens,[ref]$errors)
            if ($errors.Count) { throw 'Không phân tích được team.ps1' }
            $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -in @('Test-AlwaysPassLine','Get-ClaudeOwnedFiles')},$false) | ForEach-Object { . ([scriptblock]::Create($_.Extent.Text)) }

            """.Replace("__SOURCE__", path);
    }

    private static string FunctionText(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.ps1"))) dir = dir.Parent;
        var text = File.ReadAllText(Path.Combine(dir!.FullName, "team.ps1"));
        var m = Regex.Match(text, @"(?ms)^function " + Regex.Escape(name) + @"\b.*?(?=^function )");
        Assert.True(m.Success, $"Không tìm thấy hàm {name}");
        return m.Value;
    }

    [Theory]
    // Câu lệnh thật trong test ⇒ BÁO
    [InlineData("        Assert.True(true);", true)]
    [InlineData("Assert.True( true );", true)]
    [InlineData("        Assert.True(true, \"tạm\");", true)]
    [InlineData("        if (x == null) return;", true)]
    [InlineData("        if (!File.Exists(p)) { return; }", true)]
    // Dữ liệu / chú thích / chuỗi có nhắc tới ⇒ KHÔNG báo (F1-05: trích AGENTS.md thật)
    [InlineData("        - **Không viết test luôn pass**: không `if (...) return;` sớm, không `Assert.True(true)`.", false)]
    [InlineData("        var s = \"Assert.True(true);\";", false)]
    [InlineData("        // không dùng Assert.True(true);", false)]
    [InlineData("        Assert.True(result.Ok);", false)]
    [InlineData("        Assert.True(true == flag);", false)]
    [InlineData("        if (x == null) return default;", false)]
    [InlineData("        return;", false)]
    [InlineData("", false)]
    public async Task Always_pass_only_for_real_statements(string line, bool expected)
    {
        var script = LoadFunctions() + "$l = [IO.File]::ReadAllText($env:K105_LINE)\nif (Test-AlwaysPassLine $l) { 'YES' } else { 'NO' }\n";
        var file = Path.Combine(Path.GetTempPath(), "k105-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(file, line, new System.Text.UTF8Encoding(false));
        try
        {
            Environment.SetEnvironmentVariable("K105_LINE", file);
            var r = await TestShell.PowerShell(Path.GetTempPath(), script);
            Assert.True(r.Code == 0, r.Error + r.Output);
            Assert.Equal(expected ? "YES" : "NO", r.Output.Trim());
        }
        finally
        {
            Environment.SetEnvironmentVariable("K105_LINE", null);
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Files_last_changed_by_manager_Claude_commits_are_owned_by_Claude()
    {
        using var repo = new TempRepo();
        repo.Write("base.cs", "base");
        repo.Git("add", "-A"); repo.Git("commit", "-q", "-m", "gốc");
        var baseSha = repo.Git("rev-parse", "HEAD").Trim();

        void Commit(string email, string subject, params string[] files)
        {
            foreach (var f in files) repo.Write(f, subject + Guid.NewGuid());
            repo.Git("add", "-A");
            repo.Git("-c", "user.email=" + email, "commit", "-q", "-m", subject);
        }

        Commit(Manager, "Test nghiệm thu K1-05 + khung (Claude)", "tests/AcceptanceX.cs", "src/Skel.cs");
        Commit("agent@example.invalid", "K1-05: agent viết code", "src/Code.cs", "src/Skel2.cs");
        Commit(Manager, "K1-05: đăng ký DI sẵn trong khung (Claude)", "src/Reg.cs");           // commit Claude SAU commit đầu
        Commit(Manager, "K1-05: sửa dữ liệu giả test cũ (Claude)", "tests/OldTests.cs");
        Commit(Manager, "Bảng việc và nhật ký: K1-05 🟡 Đang chạy", "src/NotClaude.cs");       // email quản lý nhưng không "(Claude)"
        Commit(Manager, "K1-05: khung Y (Claude)", "src/Y.cs");
        Commit("agent@example.invalid", "K1-05: agent sửa tiếp", "src/Y.cs");                 // agent sửa lại sau Claude ⇒ của agent
        Commit("agent@example.invalid", "K1-05: (Claude) giả mạo trong tiêu đề", "src/Fake.cs"); // email agent ⇒ không tính

        var files = new[] { "tests/AcceptanceX.cs", "src/Skel.cs", "src/Code.cs", "src/Skel2.cs", "src/Reg.cs", "tests/OldTests.cs", "src/NotClaude.cs", "src/Y.cs", "src/Fake.cs" };
        var script = LoadFunctions() +
            "$o = @(Get-ClaudeOwnedFiles -Work (Get-Location).Path -Base '" + baseSha + "' -Files @('" + string.Join("','", files) + "') -ManagerEmail '" + Manager + "')\n" +
            "($o | Sort-Object) -join ';'\n";
        var r = await TestShell.PowerShell(repo.Root, script);
        Assert.True(r.Code == 0, r.Error + r.Output);
        Assert.Equal("src/Reg.cs;src/Skel.cs;tests/AcceptanceX.cs;tests/OldTests.cs", r.Output.Trim());
    }

    [Fact]
    public async Task No_manager_commits_means_nothing_owned()
    {
        using var repo = new TempRepo();
        repo.Write("a.cs", "a"); repo.Git("add", "-A"); repo.Git("commit", "-q", "-m", "gốc");
        var baseSha = repo.Git("rev-parse", "HEAD").Trim();
        repo.Write("b.cs", "b"); repo.Git("add", "-A"); repo.Git("commit", "-q", "-m", "agent");
        var r = await TestShell.PowerShell(repo.Root, LoadFunctions() +
            "$o = @(Get-ClaudeOwnedFiles -Work (Get-Location).Path -Base '" + baseSha + "' -Files @('b.cs') -ManagerEmail '" + Manager + "')\n$o.Count\n");
        Assert.True(r.Code == 0, r.Error + r.Output);
        Assert.Equal("0", r.Output.Trim());
    }

    [Fact]
    public void Verify_uses_both_functions_and_old_checks_are_gone()
    {
        var verify = FunctionText("Invoke-Verify");
        Assert.Contains("Get-ClaudeOwnedFiles", verify);
        Assert.Contains("Test-AlwaysPassLine", verify);
        Assert.DoesNotContain("--grep=\"^Test nghiệm thu\"", verify);
        Assert.DoesNotContain("'Assert\\.True\\(\\s*true\\s*\\)'", verify);
    }
}
