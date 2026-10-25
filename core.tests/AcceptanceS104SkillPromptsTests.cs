using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AITeam.Core.Tests;

/// <summary>Chạy hàm điều phối thật qua AST, thay mọi ranh giới git/AI bằng hàm giả trong tiến trình PowerShell cô lập.</summary>
public sealed class AcceptanceS104SkillPromptsTests : IDisposable
{
    private readonly string tmp = Path.Combine(Path.GetTempPath(), "s104-" + Guid.NewGuid().ToString("N"));
    private readonly string root;
    private static readonly string[] Ids = ["03-research", "04-ke-hoach", "06-chuan-bi-task", "07-code", "code-test", "code-winforms", "code-web", "code-app"];
    private const string Permission = "Được phép đọc các skill được liệt kê dù danh sách file cũ bị giới hạn.";

    public AcceptanceS104SkillPromptsTests()
    {
        root = Path.Combine(tmp, "Gốc AITeam có dấu");
        foreach (var id in Ids)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Skill(id))!);
            File.WriteAllText(Skill(id), "NỘI DUNG KHÔNG NHÚNG");
        }
    }

    private string Skill(string id) => Path.Combine(root, "skills", id, "SKILL.md");
    private static string Source([CallerFilePath] string file = "") => Path.GetDirectoryName(Path.GetDirectoryName(file))!;

    [Theory]
    [InlineData("plan", "wInFoRmS", "03-research,04-ke-hoach,code-winforms")]
    [InlineData("prep", "ASP.NET / bLaZoR", "06-chuan-bi-task,code-test,code-web")]
    [InlineData("code", "mAuI console CLI", "07-code,code-test,code-app")]
    [InlineData("code", "WinForms ASP.NET Blazor MAUI console CLI", "07-code,code-test,code-winforms,code-web,code-app")]
    [InlineData("plan", null, "03-research,04-ke-hoach")]
    [InlineData("prep", "", "06-chuan-bi-task,code-test")]
    [InlineData("code", "   ", "07-code,code-test")]
    [InlineData("code", "không biết", "07-code,code-test")]
    public void ChonDungSkill_KhongLap_GiuPromptCu(string stage, string? stack, string expected)
    {
        var result = Run("helper", stage, stack);
        AssertSkills(result.Prompt, expected.Split(','));
        Assert.Contains("CHỈ đọc old.cs", result.Prompt);
        Assert.Contains(Permission, result.Prompt);
        Assert.Equal(result.Prompt, result.Again);
    }

    [Theory]
    [InlineData("plan", "codex", "03-research,04-ke-hoach,code-web")]
    [InlineData("prep", "codex", "06-chuan-bi-task,code-test,code-web")]
    [InlineData("assign", "agy", "07-code,code-test,code-web")]
    [InlineData("assign", "codex", "07-code,code-test,code-web")]
    [InlineData("assign", "fallback", "07-code,code-test,code-web")]
    public void ProviderGiaNhanPromptThat_MoiNhanh(string operation, string provider, string expected)
    {
        var result = Run(operation, provider, "ASP.NET Blazor");
        AssertSkills(result.Prompt, expected.Split(','));
        Assert.Contains(Permission, result.Prompt);
        Assert.Contains(operation == "assign" ? "tasks/S1-04.md" : "CHỈ đọc old.cs", result.Prompt);
        Assert.Equal(operation == "plan", result.ReadOnly);
        Assert.Equal(operation == "plan" ? Path.Combine(tmp, "dự án ngoài AITeam") : Path.Combine(tmp, "work", operation == "prep" ? "prep-s1-04" : "a-s1-04"), result.WorkDir);
        if (operation == "assign") Assert.Contains("YÊU CẦU CỦA QUẢN LÝ: giữ phản hồi", result.Prompt);
        if (operation == "assign" && provider != "agy") Assert.Contains("CHẠY BẰNG CODEX", result.Prompt);
        Assert.Equal(provider == "fallback" ? 2 : 1, result.Calls);
    }

    [Fact]
    public void ThieuMotFile_ChiBoFileDo()
    {
        File.Delete(Skill("06-chuan-bi-task"));
        AssertSkills(Run("helper", "prep", "WinForms").Prompt, ["code-test", "code-winforms"]);
    }

    [Fact]
    public void ThuMucTrungTenSkillFile_KhongDuocCoiLaFile()
    {
        File.Delete(Skill("07-code"));
        Directory.CreateDirectory(Skill("07-code"));
        AssertSkills(Run("helper", "code", null).Prompt, ["code-test"]);
    }

    [Fact]
    public void ThieuTatCa_GiuNguyenPromptCuKhongThemQuyen()
    {
        Directory.Delete(Path.Combine(root, "skills"), true);
        var result = Run("helper", "code", null);
        Assert.Equal("CHỈ đọc old.cs", result.Prompt);
        Assert.Equal(result.Prompt, result.Again);
    }

    [Fact]
    public void CauHinhCuKhongStack_KhongDoanTuPrompt()
        => AssertSkills(Run("assign", "agy", null).Prompt, ["07-code", "code-test"]);

    [Fact]
    public void StackKhongDuocThucThiHayChenVaoPrompt()
    {
        var marker = Path.Combine(tmp, "khong-duoc-tao.txt");
        var stack = "../../outside; $(Set-Content '" + marker + "' 'sai')";
        var result = Run("helper", "code", stack);
        AssertSkills(result.Prompt, ["07-code", "code-test"]);
        Assert.DoesNotContain(stack, result.Prompt);
        Assert.False(File.Exists(marker));
    }

    [Theory]
    [InlineData("bad-stage", "Bước skill không hợp lệ.")]
    [InlineData("relative-root", "Gốc AITeam phải là đường dẫn tuyệt đối.")]
    public void ThamSoSai_BaoLoiRo(string kind, string message)
        => Assert.Equal(message, Run("invalid", kind, null).Error);

    [Fact]
    public void ThuMucSkillLaJunction_KhongTroRaNgoai()
    {
        var outside = Path.Combine(tmp, "ngoài");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "SKILL.md"), "không được đọc");
        Directory.Delete(Path.GetDirectoryName(Skill("07-code"))!, true);
        AssertSkills(Run("junction", "code", outside).Prompt, ["code-test"]);
    }

    private void AssertSkills(string prompt, string[] expected)
    {
        var previous = -1;
        foreach (var id in expected)
        {
            var line = "Đọc skill: `" + Skill(id) + "`";
            Assert.Contains(line, prompt);
            Assert.Equal(1, prompt.Split(Skill(id), StringSplitOptions.None).Length - 1);
            var index = prompt.IndexOf(line, StringComparison.Ordinal);
            Assert.True(index > previous);
            previous = index;
        }
        foreach (var id in Ids.Except(expected)) Assert.DoesNotContain(Skill(id), prompt);
        Assert.DoesNotContain("NỘI DUNG KHÔNG NHÚNG", prompt);
        Assert.DoesNotContain("outside/SKILL.md", prompt);
    }

    private Capture Run(string operation, string mode, string? stack)
    {
        var input = Path.Combine(tmp, "input.json");
        File.WriteAllText(input, JsonSerializer.Serialize(new { root, tmp, source = Source(), operation, mode, stack }));
        var script = Path.Combine(tmp, "harness.ps1");
        File.WriteAllText(script, Harness, new UTF8Encoding(true));
        using var process = new System.Diagnostics.Process { StartInfo = new ProcessStartInfo("powershell.exe")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true } };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, input }) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        var exited = process.WaitForExit(30_000);
        if (!exited) process.Kill(true);
        Assert.True(exited, "PowerShell vượt 30 giây.");
        Assert.True(process.ExitCode == 0, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        return JsonSerializer.Deserialize<Capture>(output.GetAwaiter().GetResult())!;
    }

    private sealed record Capture(string Prompt, string? Again, bool ReadOnly, string WorkDir, int Calls, string? Error);

    // Chỉ nạp định nghĩa hàm từ AST, không chạy heartbeat/switch cấp cao nhất của team.ps1.
    // Mọi đường gọi AI/git được thay bằng hàm giả; dừng ngay lúc provider nhận prompt cuối.
    private const string Harness = """
param($inputFile)
$ErrorActionPreference = 'Stop'
$x = Get-Content -LiteralPath $inputFile -Raw -Encoding UTF8 | ConvertFrom-Json
. (Join-Path $x.source 'scripts/SkillPrompts.ps1')
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $x.source 'team.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw ($errors | Out-String) }
$names=@('Invoke-Plan','Invoke-Prep','Invoke-Assign','Invoke-CodexDevelop')
foreach ($f in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$true)) {
    if ($f.Name -in $names) { . ([scriptblock]::Create($f.Extent.Text)) }
}
$Root=$x.root; $Utf8=New-Object Text.UTF8Encoding $false
$Project='demo'; $Task='S1-04'; $Request='R-010'; $Name='S1'; $Resume=$true; $Feedback='giữ phản hồi'
$Dev=if($x.mode -eq 'codex'){'codex'}else{''}
$repo=Join-Path $x.tmp 'dự án ngoài AITeam'; $logs=Join-Path $x.tmp 'logs'; $workRoot=Join-Path $x.tmp 'work'
$planDir=Join-Path $Root 'plans/R-010'
foreach($d in @($repo,$logs,$workRoot,$planDir,(Join-Path $Root 'templates'),(Join-Path $Root 'staff'),(Join-Path $repo 'tasks'))) { New-Item -ItemType Directory -Force $d | Out-Null }
# Các worktree giả cùng trỏ tới repo tạm qua Resolve-Path giả; không có thao tác trên repo thật.
foreach($d in @((Join-Path $workRoot 'a-s1-04'),(Join-Path $workRoot 'prep-s1-04'))) { New-Item -ItemType Directory -Force $d | Out-Null }
[IO.File]::WriteAllText((Join-Path $repo 'tasks/S1-04.md'), "- **Agent:** A`n- **Nhánh:** ``agent-a/S1-04``")
[IO.File]::WriteAllText((Join-Path $planDir 'brief.md'),'brief giả')
[IO.File]::WriteAllText((Join-Path $planDir 'plan.md'),"### S1-04 · Kết nối`n- **Agent:** A`n- **Ngân sách:** ~280 dòng")
foreach($f in @('codex-plan.md','codex-prep.md')) { [IO.File]::WriteAllText((Join-Path $Root "templates/$f"),'CHỈ đọc old.cs {REQUEST} {TASK}') }
$info=[pscustomobject]@{name='demo';build='build giả';test='test giả'}
if ($null -ne $x.stack) { $info | Add-Member NoteProperty stack $x.stack }
$r=[pscustomobject]@{Info=$info;Path=$repo;Logs=$logs;Work=$workRoot}
$cfg=[pscustomobject]@{developers=@();agy=[pscustomobject]@{exe='fake-agy';model=''};github=[pscustomobject]@{owner='giả'}}
function Get-Config { $cfg }; function Resolve-Project { $r }; function Get-PlanDir { $planDir }
function Get-AITeamExe { 'FakeProvider' }; function Say { }; function git { '' }
function Find-TaskFile { Get-Item (Join-Path $repo 'tasks/S1-04.md') }
function Select-FreeAgent { 'A' }; function ConvertTo-AsciiSlug { 'ket-noi' }
function Copy-Template { param($template,$path,$vars) [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null; [IO.File]::WriteAllText($path,'- **Yêu cầu:** R-xxx') }
function Assert-NotClaimedElsewhere { }; function Get-ScopeOverlaps { }; function Set-BoardStatus { }
function Get-ClaimText { 'giả' }; function Set-ManagerIdentity { $false }; function Set-AgentIdentity { }
function Set-McpConfig { $false }; function Get-StaffName { 'giả' }; function Get-EmptyInput { '' }
function Test-AgentTouchedMain { }; function Get-RunRecord { }; function Save-RunMetrics { }
function Test-QuotaExhausted { $x.mode -eq 'fallback' }
$script:captured=$null; $script:calls=0; $script:ro=$false; $script:cwd=$repo
function FakeProvider {
    $script:calls++; $a=@($args); $pf=$a[[Array]::IndexOf($a,'--prompt-file')+1]
    $script:captured=[IO.File]::ReadAllText($pf); $script:ro=$a -contains '--read-only'
    $script:cwd=$a[[Array]::IndexOf($a,'--workdir')+1]
    $ErrorActionPreference='Stop'; throw 'PROVIDER_CAPTURED'
}
function Start-Process {
    param($FilePath,$ArgumentList,$WorkingDirectory,[switch]$NoNewWindow,[switch]$Wait,[switch]$PassThru,$RedirectStandardInput,$RedirectStandardOutput,$RedirectStandardError)
    $script:calls++
    if($x.mode -eq 'fallback') {
        [IO.File]::WriteAllText($RedirectStandardOutput,'quota'); [IO.File]::WriteAllText($RedirectStandardError,'quota')
        return [pscustomobject]@{ExitCode=1}
    }
    $script:cwd=$WorkingDirectory
    $script:captured=[IO.File]::ReadAllText($promptFile)
    throw 'PROVIDER_CAPTURED'
}
try {
    $again=$null; $err=$null
    switch($x.operation) {
        'invalid' {
            $stage=if($x.mode -eq 'bad-stage'){'bad'}else{'code'}
            $base=if($x.mode -eq 'relative-root'){'relative'}else{$Root}
            try { $null=Add-SkillPrompt -Prompt old -Root $base -Stage $stage } catch [ArgumentException] { $err=$_.Exception.Message }
            if(!$err) { throw 'Không nhận được lỗi hợp đồng.' }; $script:captured=''
        }
        'junction' {
            New-Item -ItemType Junction -Path (Join-Path $Root 'skills/07-code') -Target $x.stack | Out-Null
            $script:captured=Add-SkillPrompt -Prompt old -Root $Root -Stage code
        }
        'helper' {
            $script:captured=Add-SkillPrompt -Prompt 'CHỈ đọc old.cs' -Root $Root -Stage $x.mode -Stack $x.stack
            $again=Add-SkillPrompt -Prompt $script:captured -Root $Root -Stage $x.mode -Stack $x.stack
        }
        'plan' { Invoke-Plan }
        'prep' { Invoke-Prep }
        'assign' { Invoke-Assign }
        default { throw 'Thao tác test không hợp lệ.' }
    }
} catch { if($_.Exception.Message -ne 'PROVIDER_CAPTURED' -or $null -eq $script:captured) { [Console]::Error.WriteLine($_); exit 1 } }
if($null -eq $script:captured) { [Console]::Error.WriteLine('Provider chưa nhận prompt.'); exit 1 }
@{Prompt=$script:captured;Again=$again;ReadOnly=$script:ro;WorkDir=$script:cwd;Calls=$script:calls;Error=$err} | ConvertTo-Json -Compress
""";

    public void Dispose()
    {
        var link = Path.GetDirectoryName(Skill("07-code"))!;
        if (Directory.Exists(link) && File.GetAttributes(link).HasFlag(FileAttributes.ReparsePoint)) Directory.Delete(link);
        Directory.Delete(tmp, true);
    }
}
