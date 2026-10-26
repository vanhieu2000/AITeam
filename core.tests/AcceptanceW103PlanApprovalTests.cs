using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

public sealed class AcceptanceW103PlanApprovalTests
{
    private static readonly string[] Helpers = ["Read-PlanMockup", "Write-PlanMockup", "Assert-PrepPlanApproval"];
    private const string Approved = "\r\n## Quyết định của quản lý (Claude)\r\n- ĐÃ DUYỆT.\r\n";

    // Biến đổi bản sao plan thật; không tự chế cấu trúc phiếu/kế hoạch nền.
    private static string Plan(TeamScript box, bool ui, bool approved = true, bool typed = true, string? html = null)
    {
        var path = RealFile.Copy("plans/R-011/plan.md", box.Root);
        var text = File.ReadAllText(path);
        var realHtml = Regex.Match(text, @"(?ms)^```html\r?\n(?<html>.*?)\r?\n```").Groups["html"].Value;
        text = Regex.Replace(text, @"(?ms)^## Quyết định của quản lý.*\z", "");
        text = Regex.Replace(text, @"(?ms)^## Mockup\r?\n.*?(?=^## |\z)", "");
        if (typed)
            text = Regex.Replace(text, @"(?m)^(### [^\r\n]+)\r?$", "$1\r\n- **Giao diện:** " + (ui ? "Có" : "Không"));
        if (ui)
            text += "\r\n## Mockup\r\n```html\r\n" + (html ?? realHtml) + "\r\n```\r\n";
        if (approved) text += Approved;
        File.WriteAllText(path, text, new UTF8Encoding(true));
        RealFile.Copy("hub/wwwroot/css/hub.css", box.Root);
        return path;
    }

    private static string Load => "$p = Join-Path $Root 'plans/R-011/plan.md'; $text = [IO.File]::ReadAllText($p); ";
    private static string Export => "$m = Write-PlanMockup 'R-011' $text; ";
    private static string Bind => "$text += ('- Mockup SHA256: ' + $m.Fingerprint + [Environment]::NewLine); ";
    private static string Guard => "Assert-PrepPlanApproval 'R-011' $text; 'ACCEPTED'";
    private static Dictionary<string, string> Snapshot(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        // TeamScript tạo shim riêng trong Root; đây là hạ tầng test, không phải tác dụng phụ sản phẩm.
        .Where(p => !Path.GetRelativePath(root, p).Split(Path.DirectorySeparatorChar)[0].StartsWith(".w101-shims-", StringComparison.Ordinal))
        .ToDictionary(p => Path.GetRelativePath(root, p), p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
    private static void Unchanged(Dictionary<string, string> before, string root)
    {
        var after = Snapshot(root);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (name, hash) in before) Assert.Equal(hash, after[name]);
    }
    private static void Rejected(TeamScriptResult result, string message)
    {
        Assert.Equal(1, result.Process.Code);
        Assert.Contains(message, result.Process.Error);
        Assert.DoesNotContain("ACCEPTED", result.Process.Output);
        Assert.Empty(result.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Core_new_and_legacy_keep_real_approval_without_output(bool typed)
    {
        using var box = new TeamScript();
        Plan(box, false, typed: typed);
        var before = Snapshot(box.Root);
        var r = await box.RunAsync("team.ps1", Helpers, Load + (typed ? "$m = Write-PlanMockup 'R-011' $text; if ($null -ne $m) { throw 'Lõi sinh mockup.' }; " : "") + Guard);
        Assert.Equal(0, r.Process.Code);
        Assert.Contains("ACCEPTED", r.Process.Output);
        Assert.Empty(r.Calls);
        Assert.False(Directory.Exists(Path.Combine(box.Root, "docs/design")));
        Unchanged(before, box.Root);
    }

    [Fact]
    public async Task Ui_exports_bom_crlf_local_css_and_real_byte_fingerprint()
    {
        using var box = new TeamScript();
        Plan(box, true);
        var r = await box.RunAsync("team.ps1", Helpers, Load + Export + "$m | ConvertTo-Json -Compress");
        Assert.Equal(0, r.Process.Code);
        using var json = JsonDocument.Parse(r.Process.Output.Trim());
        var path = Path.Combine(box.Root, "docs/design/R-011-mockup.html");
        Assert.Equal(path, json.RootElement.GetProperty("Path").GetString());
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 239, 187, 191 }, bytes.Take(3));
        var html = Encoding.UTF8.GetString(bytes[3..]);
        Assert.Contains("<main>", html);
        Assert.Contains("\r\n", html);
        Assert.DoesNotContain("\n", html.Replace("\r\n", ""));
        Assert.Contains(new Uri(Path.Combine(box.Root, "hub/wwwroot/css/hub.css")).AbsoluteUri, html);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), json.RootElement.GetProperty("Fingerprint").GetString());
        Assert.Empty(r.Calls);
    }

    [Theory]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public async Task Html_line_limit_counts_content_only(int lines, bool valid)
    {
        using var box = new TeamScript();
        Plan(box, true, html: string.Join("\r\n", Enumerable.Repeat("<p>Việc</p>", lines)));
        var r = await box.RunAsync("team.ps1", Helpers, Load + Export + "'ACCEPTED'");
        if (valid)
        {
            Assert.Equal(0, r.Process.Code);
            Assert.Contains("ACCEPTED", r.Process.Output);
            Assert.True(File.Exists(Path.Combine(box.Root, "docs/design/R-011-mockup.html")));
        }
        else Rejected(r, "Mockup không hợp lệ.");
    }

    [Theory]
    [InlineData("<ScRiPt>alert(1)</ScRiPt>")]
    [InlineData("<p ONCLICK='x()'>Việc</p>")]
    [InlineData("<a href='javascript:alert(1)'>Việc</a>")]
    [InlineData("<a href='jav&#97;script:alert(1)'>Việc</a>")]
    [InlineData("<img src='https://example.invalid/a'>")]
    [InlineData("<link rel='stylesheet' href='//example.invalid/a.css'>")]
    [InlineData("<style>@import 'https://example.invalid/a';</style>")]
    [InlineData("<style>p{background:url(file:///C:/x)}</style>")]
    [InlineData("<style>p{background:u\\72l(https://example.invalid/a)}</style>")]
    [InlineData("<iframe srcdoc='<script>x()</script>'></iframe>")]
    [InlineData("<meta http-equiv='refresh' content='0;url=https://example.invalid'>")]
    [InlineData("<svg onload='x()'></svg>")]
    [InlineData("<form action='#'><button>Gửi</button></form>")]
    [InlineData("<object data='data:text/html,x'></object>")]
    [InlineData("<p>x</p>\r\n```\r\n```html\r\n<p>hai</p>")]
    [InlineData("")]
    public async Task Unsafe_or_ambiguous_html_never_writes(string html)
    {
        using var box = new TeamScript();
        Plan(box, true, html: html);
        var before = Snapshot(box.Root);
        var r = await box.RunAsync("team.ps1", Helpers, Load + Export + "'ACCEPTED'");
        Rejected(r, "Mockup không hợp lệ.");
        Unchanged(before, box.Root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_or_missing_type_requires_clarification(bool unknown)
    {
        using var box = new TeamScript();
        var path = Plan(box, false, typed: unknown);
        if (unknown) File.WriteAllText(path, File.ReadAllText(path).Replace("**Giao diện:** Không", "**Giao diện:** Chưa rõ"), new UTF8Encoding(true));
        var r = await box.RunAsync("team.ps1", Helpers, Load + Export);
        Rejected(r, "Chưa xác định loại task; cần bổ sung Giao diện: Có/Không.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\r\n## Quyết định của quản lý\r\n<!-- - ĐÃ DUYỆT. -->")]
    [InlineData("\r\n## Quyết định của quản lý\r\n```text\r\n- ĐÃ DUYỆT.\r\n```")]
    [InlineData("\r\n## Research khác\r\n- ĐÃ DUYỆT.")]
    [InlineData("\r\n## Quyết định của quản lý\r\n- Chưa ĐÃ DUYỆT.")]
    [InlineData("\r\n<!--\r\n## Quyết định của quản lý\r\n- ĐÃ DUYỆT.\r\n-->")]
    public async Task Approval_must_be_in_real_decision_section(string suffix)
    {
        using var box = new TeamScript();
        var path = Plan(box, false, approved: false);
        File.AppendAllText(path, suffix, new UTF8Encoding(false));
        var before = Snapshot(box.Root);
        var r = await box.RunAsync("team.ps1", Helpers, Load + Guard);
        Rejected(r, "Kế hoạch chưa được duyệt.");
        Unchanged(before, box.Root);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("missing")]
    [InlineData("wrong")]
    [InlineData("comment")]
    [InlineData("code")]
    [InlineData("html-changed")]
    [InlineData("file-changed")]
    [InlineData("file-missing")]
    [InlineData("untyped-ui")]
    public async Task Ui_approval_binds_current_html_and_exported_bytes(string mode)
    {
        using var box = new TeamScript();
        Plan(box, true);
        var mutation = mode switch
        {
            "missing" => "",
            "wrong" => "$text += ('- Mockup SHA256: ' + ('0' * 64)); ",
            "comment" => "$text += ('<!-- - Mockup SHA256: ' + $m.Fingerprint + ' -->'); ",
            "code" => "$text += ('```text' + [Environment]::NewLine + '- Mockup SHA256: ' + $m.Fingerprint + [Environment]::NewLine + '```'); ",
            "html-changed" => Bind + "$text = $text.Replace('<main>', '<main><p>Đổi</p>'); ",
            "file-changed" => Bind + "[IO.File]::AppendAllText($m.Path, '<p>Đổi</p>'); ",
            "file-missing" => Bind + "[IO.File]::Delete($m.Path); ",
            "untyped-ui" => Bind + @"$text = $text -replace '(?m)^- \*\*Giao diện:\*\* Có\r?\n', ''; ",
            _ => Bind
        };
        // Plan cũ có Mockup vẫn phải kiểm UI, không được lách bằng thiếu khai báo.
        var r = await box.RunAsync("team.ps1", Helpers, Load + Export + mutation + Guard);
        if (mode is "valid" or "untyped-ui")
        {
            Assert.Equal(0, r.Process.Code);
            Assert.Contains("ACCEPTED", r.Process.Output);
            Assert.Empty(r.Calls);
        }
        else Rejected(r, "Mockup chưa được duyệt hoặc đã thay đổi.");
    }

    [Theory]
    [InlineData("../R-011")]
    [InlineData("R-011/../../x")]
    [InlineData("C:\\x")]
    public async Task Invalid_request_cannot_escape_output(string id)
    {
        using var box = new TeamScript();
        Plan(box, true);
        var before = Snapshot(box.Root);
        var r = await box.RunAsync("team.ps1", Helpers, Load + "Write-PlanMockup '" + id + "' $text");
        Rejected(r, "Đường dẫn mockup không hợp lệ.");
        Unchanged(before, box.Root);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Real_prep_rejects_before_agent_fetch_worktree_logs_or_provider(bool noLaunch, bool ui)
    {
        using var box = new TeamScript();
        Plan(box, ui, approved: ui);
        var before = Snapshot(box.Root);
        var r = await box.RunAsync("team.ps1", [.. Helpers, "Invoke-Prep", "Get-PlanDir"], """
            $Request = 'R-011'; $Task = 'W1-03'; $Project = ''; $NoLaunch = NO_LAUNCH
            function Resolve-Project { @{ Path=$Root; Logs=(Join-Path $Root 'new-logs'); Work=(Join-Path $Root 'new-work'); Info=@{name='AITeam'} } }
            function Select-FreeAgent { throw 'Đã chọn agent trước guard.' }
            function Get-AITeamExe { throw 'Đã gọi provider trước guard.' }
            Invoke-Prep
            'ACCEPTED'
            """.Replace("NO_LAUNCH", noLaunch ? "$true" : "$false"));
        Rejected(r, ui ? "Mockup chưa được duyệt hoặc đã thay đổi." : "Kế hoạch chưa được duyệt.");
        Unchanged(before, box.Root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_plan_exports_only_when_static_html_is_valid(bool unsafeHtml)
    {
        using var box = new TeamScript();
        var planPath = Plan(box, true, approved: false, html: unsafeHtml ? "<script>x()</script>" : null);
        var planText = File.ReadAllText(planPath);
        File.Delete(planPath);
        var logs = Path.Combine(box.Root, "plan-logs");
        Directory.CreateDirectory(logs);
        File.WriteAllText(Path.Combine(logs, "R-011.plan.json"),
            JsonSerializer.Serialize(new { text = planText, summary = "Kế hoạch giả" }), new UTF8Encoding(true));
        RealFile.Copy("plans/R-011/brief.md", box.Root);
        var r = await box.RunAsync("team.ps1", [.. Helpers, "Invoke-Plan", "Get-PlanDir"], """
            $Request = 'R-011'; $Project = ''; $Name = 'W1'; $Utf8 = New-Object Text.UTF8Encoding $true
            function Resolve-Project { @{ Path=$Root; Logs=(Join-Path $Root 'plan-logs'); Info=@{stack='dotnet'} } }
            function Get-AITeamExe { 'codex' }
            function Say { param($text, $color) }
            function Add-SkillPrompt { param($Prompt, $Root, $Stage, $Stack) $Prompt }
            function Read-Json { param($path) [IO.File]::ReadAllText($path) | ConvertFrom-Json }
            Invoke-Plan
            'ACCEPTED'
            """);
        Assert.Contains(r.Calls, c => c.Name == "codex" && c.Arguments.Contains("--read-only"));
        if (unsafeHtml)
        {
            Assert.Equal(1, r.Process.Code);
            Assert.Contains("Mockup không hợp lệ.", r.Process.Error);
            Assert.False(File.Exists(planPath));
            Assert.False(File.Exists(Path.Combine(box.Root, "docs/design/R-011-mockup.html")));
        }
        else
        {
            Assert.Equal(0, r.Process.Code);
            Assert.Contains("ACCEPTED", r.Process.Output);
            Assert.True(File.Exists(planPath));
            Assert.Contains("<main>", File.ReadAllText(Path.Combine(box.Root, "docs/design/R-011-mockup.html")));
        }
    }

    [Fact]
    public async Task Export_rejects_junction_parent_without_touching_target()
    {
        using var box = new TeamScript();
        using var outside = new TempRepo(false);
        Plan(box, true);
        Directory.CreateDirectory(Path.Combine(box.Root, "docs"));
        var link = Path.Combine(box.Root, "docs/design");
        var create = await TestShell.PowerShell(box.Root,
            "New-Item -ItemType Junction -Path '" + link.Replace("'", "''") + "' -Target '" + outside.Root.Replace("'", "''") + "' | Out-Null");
        Assert.Equal(0, create.Code);
        try
        {
            var r = await box.RunAsync("team.ps1", Helpers, Load + Export);
            Rejected(r, "Đường dẫn mockup không hợp lệ.");
            Assert.Empty(Directory.GetFiles(outside.Root));
        }
        finally { Directory.Delete(link); }
    }
}
