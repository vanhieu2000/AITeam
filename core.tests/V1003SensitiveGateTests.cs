using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Tests.Support;

namespace AITeam.Core.Tests;

/// <summary>V10-03 (R-014): cổng KT bắt file nhạy cảm, chặn merge theo mục nhạy cảm và nhật ký Claude tự quyết.</summary>
public sealed class V1003SensitiveGateTests
{
    private const string SayStub = "function Say($text, $color) { }\n";

    private static void PutApproval(TeamScript box, string id, string kind, string status, string task = "V10-03", string commit = "abc")
    {
        var dir = Path.Combine(box.Root, "approvals");
        Directory.CreateDirectory(dir);
        var json = $$"""
            {
              "id": "{{id}}", "kind": "{{kind}}", "title": "{{kind}} {{id}}", "detail": "",
              "project": "AITeam", "task": "{{task}}", "commit": "{{commit}}", "links": [],
              "requestedBy": "Claude", "requestedAt": "2026-11-03T21:00:00", "status": "{{status}}", "decidedAt": null, "note": null
            }
            """;
        File.WriteAllText(Path.Combine(dir, id + ".json"), json, new UTF8Encoding(false));
    }

    [Fact]
    public async Task Get_SensitiveChanges_bat_file_nhay_cam_va_bo_qua_giao_dien()
    {
        using var box = new TeamScript();
        var r = await box.RunAsync("team.ps1", ["Get-SensitiveChanges"], """
            $files = @('hub/Services/VaultService.cs', '.env.local', 'Migrations/001.sql', 'hub/Pages/Tasks.cshtml')
            foreach ($f in @(Get-SensitiveChanges $files $null)) { Write-Output $f }
            """);

        Assert.Equal(0, r.Process.Code);
        Assert.Contains("hub/Services/VaultService.cs", r.Process.Output);
        Assert.Contains(".env.local", r.Process.Output);
        Assert.Contains("Migrations/001.sql", r.Process.Output);
        Assert.DoesNotContain("hub/Pages/Tasks.cshtml", r.Process.Output);
    }

    [Theory]
    [InlineData("security", "pending", "BLOCK")]
    [InlineData("external", "rejected", "BLOCK")]
    [InlineData("security", "approved", "PASS")]
    [InlineData("merge", "pending", "PASS")]
    public async Task Assert_MergeApproval_blocks_sensitive_pending_or_rejected_but_not_old_merge(
        string kind, string status, string expected)
    {
        using var box = new TeamScript();
        PutApproval(box, "A-001", kind, status);
        var r = await box.RunAsync("team.ps1", ["Read-Json", "Get-Approvals", "Assert-MergeApproval"], SayStub + """
            try {
                $x = Assert-MergeApproval @{} 'V10-03' 'abc'
                if ($x) { Write-Output ('PASS-ITEM:' + $x.id) } else { Write-Output 'PASS' }
            } catch { Write-Output ('BLOCK: ' + $_.Exception.Message) }
            """);

        Assert.Equal(0, r.Process.Code);
        Assert.Contains(expected, r.Process.Output);
    }

    [Fact]
    public async Task Approval_request_kind_secret_thieu_Name_thi_loi()
    {
        using var box = new TeamScript();
        var r = await box.RunAsync("team.ps1",
            ["ConvertTo-ApprovalKindJson", "Save-ApprovalFile", "New-ApprovalItem", "Invoke-ApprovalRequest"], SayStub + """
            $Utf8 = New-Object Text.UTF8Encoding $false
            $Kind = 'secret'; $Title = 'Cần khóa CSDL'; $Name = ''; $Risk = 'medium'; $Project = 'AITeam'; $Prompt = ''; $Task = ''
            try { Invoke-ApprovalRequest; Write-Output 'KHONG-LOI' } catch { Write-Output ('LOI: ' + $_.Exception.Message) }
            """);

        Assert.Equal(0, r.Process.Code);
        Assert.Contains("LOI:", r.Process.Output);
        Assert.Contains("Name", r.Process.Output);
        var dir = Path.Combine(box.Root, "approvals");
        Assert.False(Directory.Exists(dir) && Directory.GetFiles(dir, "A-*.json").Length > 0);
    }

    [Fact]
    public async Task Approval_request_kind_secret_chi_ghi_ten_khoa()
    {
        using var box = new TeamScript();
        var r = await box.RunAsync("team.ps1",
            ["ConvertTo-ApprovalKindJson", "Save-ApprovalFile", "New-ApprovalItem", "Invoke-ApprovalRequest"], SayStub + """
            $Utf8 = New-Object Text.UTF8Encoding $false
            $Kind = 'secret'; $Title = 'Cần khóa CSDL'; $Name = 'DB_PASSWORD'; $Risk = 'high'; $Project = 'AITeam'; $Prompt = 'cần để chạy thử'; $Task = ''
            Invoke-ApprovalRequest
            """);

        Assert.Equal(0, r.Process.Code);
        var file = Path.Combine(box.Root, "approvals", "A-001.json");
        Assert.True(File.Exists(file), r.Process.Output + r.Process.Error);
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        Assert.Equal("secret", doc.RootElement.GetProperty("kind").GetString());
        Assert.Equal("high", doc.RootElement.GetProperty("risk").GetString());
        Assert.Equal("DB_PASSWORD", doc.RootElement.GetProperty("secretName").GetString());
    }

    [Fact]
    public async Task Add_ClaudeDecision_ghi_dung_mot_dong_json_hop_le()
    {
        using var box = new TeamScript();
        var r = await box.RunAsync("team.ps1", ["Add-ClaudeDecision"], """
            $id = Add-ClaudeDecision -Kind 'qc' -Title 'QC đạt vòng 1' -Task 'V10-03' -Request 'R-014' -Detail 'đạt'
            Write-Output ('ID:' + $id)
            """);

        Assert.Equal(0, r.Process.Code);
        var match = Regex.Match(r.Process.Output, @"ID:(D-\d{14}-[a-z0-9]{4})");
        Assert.True(match.Success, r.Process.Output + r.Process.Error);

        var lines = File.ReadAllLines(Path.Combine(box.Root, "approvals", "claude-decisions.jsonl"), Encoding.UTF8)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        var line = Assert.Single(lines);
        using var doc = JsonDocument.Parse(line);
        Assert.Equal("decision", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(match.Groups[1].Value, doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("qc", doc.RootElement.GetProperty("kind").GetString());
        Assert.Equal("QC đạt vòng 1", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("V10-03", doc.RootElement.GetProperty("task").GetString());
        Assert.Equal("R-014", doc.RootElement.GetProperty("request").GetString());
    }

    [Fact]
    public async Task Assert_MergeApproval_ignores_sensitive_items_from_older_commits()
    {
        using var box = new TeamScript();
        PutApproval(box, "A-001", "security", "pending", task: "V10-03", commit: "commitX");
        PutApproval(box, "A-002", "security", "approved", task: "V10-03", commit: "commitY");

        var r1 = await box.RunAsync("team.ps1", ["Read-Json", "Get-Approvals", "Assert-MergeApproval"], SayStub + """
            try {
                $x = Assert-MergeApproval @{} 'V10-03' 'commitY'
                Write-Output 'PASS'
            } catch { Write-Output ('BLOCK: ' + $_.Exception.Message) }
            """);

        Assert.Equal(0, r1.Process.Code);
        Assert.Contains("PASS", r1.Process.Output);
        Assert.DoesNotContain("BLOCK", r1.Process.Output);

        PutApproval(box, "A-003", "security", "pending", task: "V10-03", commit: "commitY");

        var r2 = await box.RunAsync("team.ps1", ["Read-Json", "Get-Approvals", "Assert-MergeApproval"], SayStub + """
            try {
                $x = Assert-MergeApproval @{} 'V10-03' 'commitY'
                Write-Output 'PASS'
            } catch { Write-Output ('BLOCK: ' + $_.Exception.Message) }
            """);

        Assert.Equal(0, r2.Process.Code);
        Assert.Contains("BLOCK", r2.Process.Output);
    }
}
