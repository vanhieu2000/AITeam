using System.Text;
using AITeam.Hub.Services;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Test nghiệm thu F1-02 (Claude). Thiết kế: docs/design/R-015-chuc-nang.md mục F1-02.
/// Văn phòng hiện "0/0" cho mọi phiếu vì chỉ đếm bảng "Chia việc"; task thật gắn phiếu bằng dòng "- **Yêu cầu:** R-xxx".</summary>
public sealed class AcceptanceF102RequestProgressTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly string _projectPath;
    private readonly string _requestFile;
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 20, 5, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    // Phiếu THẬT rút gọn (R-014, 04/11/2026): có BOM, CRLF, bảng Chia việc với ô "UI-01..09" (không được đếm).
    private const string RequestText =
        "# R-014 · Thiết kế lại Hub thành văn phòng AI\r\n\r\n" +
        "- **Nhận lúc:** 20:36 03/11/2026 · máy HIEUDV\r\n" +
        "- **Trạng thái:** 🟡 Đang làm\r\n" +
        "- **Lời chủ dự án:**\r\n  > làm văn phòng AI\r\n\r\n" +
        "## 2. Chia việc\r\n| Task | Dự án | Ai |\r\n|---|---|---|\r\n| UI-01..09 | AITeam | Claude |\r\n\r\n" +
        "## 3. Nhật ký\r\n| Giờ | Sự kiện | Ghi chú |\r\n|---|---|---|\r\n| 20:40 03/11 | Nhận yêu cầu | |\r\n\r\n" +
        "## 4. Đánh giá sau khi xong\r\n| Chỉ số | Giá trị |\r\n|---|---|\r\n| Tổng thời gian | |\r\n";

    private const string Board = """
        | Task | Agent | Nhánh | Trạng thái |
        |---|---|---|---|
        | [K1-01 Cổng KT](K1-01-cong-kt.md) | C | `agent-c/K1-01-cong-kt` | ✅ Đã merge 04/11 (Codex, vòng 1) |
        | [K1-02 Canh OpenCode](K1-02-canh.md) | C | `agent-c/K1-02-canh` | 🔵 Chờ review · cổng kiểm tra ĐẠT |

        | [K1-03 Bảng việc](K1-03-bang.md) | C | `agent-c/K1-03-bang` | 🔴 Cần sửa
        | [K1-04 Codex mất kết nối](K1-04-codex.md) | C | `agent-c/K1-04-codex` | 🟡 Đang chạy · máy HIEUDV · 12:00 |
        | [V9-99 Việc phiếu khác](V9-99-khac.md) | A | `agent-a/V9-99` | ✅ Đã merge |
        """;

    public AcceptanceF102RequestProgressTests()
    {
        _projectPath = Path.Combine(_env.RootDir, "AITeamRepo");
        var tasks = Path.Combine(_projectPath, "tasks");
        Directory.CreateDirectory(tasks);
        File.WriteAllText(Path.Combine(tasks, "README.md"), Board);
        WriteTask(tasks, "K1-01-cong-kt.md", "K1-01 · Cổng KT: lỗi mạng", "R-014");
        WriteTask(tasks, "K1-02-canh.md", "K1-02 · Canh OpenCode đứng im", "R-014");
        WriteTask(tasks, "K1-03-bang.md", "K1-03 · Bảng việc: cảnh báo git", "R-014");
        WriteTask(tasks, "K1-04-codex.md", "K1-04 · Codex mất kết nối", "R-014");
        WriteTask(tasks, "K1-05-chua-giao.md", "K1-05 · Chưa có trên bảng", "R-014");
        WriteTask(tasks, "V9-99-khac.md", "V9-99 · Việc phiếu khác", "R-013");
        // Dòng Yêu cầu có chữ thêm ⇒ không khớp (CLAUDE.md: dòng này CHỈ ghi mã).
        WriteTask(tasks, "K1-06-sai-dong.md", "K1-06 · Sai dòng yêu cầu", "R-014 (đợt 2)");

        File.WriteAllText(_env.Paths.ProjectsJson, $$"""
            [ { "name": "AITeam", "repo": "o/AITeam", "path": {{System.Text.Json.JsonSerializer.Serialize(_projectPath)}}, "areas": {} },
              { "name": "Missing", "repo": "o/Missing", "path": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(_env.RootDir, "khong-co"))}}, "areas": {} } ]
            """);

        var requests = Path.Combine(_env.AITeamRoot, "requests");
        Directory.CreateDirectory(requests);
        _requestFile = Path.Combine(requests, "R-014-thiet-ke-lai-hub.md");
        File.WriteAllText(_requestFile, RequestText, new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(requests, "R-013-tich-hop.md"), "# R-013 · OpenCode\n\n- **Trạng thái:** ✅ Xong 03/11\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(requests, "R-012-huy.md"), "# R-012 · Bỏ\n\n- **Trạng thái:** ❌ Hủy — không cần nữa\n");
    }

    public void Dispose() => _env.Dispose();

    private static void WriteTask(string dir, string file, string title, string request)
        => File.WriteAllText(Path.Combine(dir, file), $"# {title}\n\n- **Agent:** C\n- **Yêu cầu:** {request}\n\n## Tóm tắt\n- x\n", new UTF8Encoding(true));

    private RequestProgressService Service()
    {
        var config = new TeamConfigService(_env.Paths);
        return new RequestProgressService(_env.Paths, config, new RequestService(_env.Paths), new FixedTime(Now));
    }

    [Fact]
    public void Counts_tasks_linked_by_request_line_with_board_status()
    {
        var p = Service().Get("R-014")!;

        Assert.Equal(RequestState.Open, p.State);
        Assert.Equal(new[] { "K1-01", "K1-02", "K1-03", "K1-04", "K1-05" }, p.Tasks.Select(t => t.Code));
        Assert.Equal(5, p.Total);
        Assert.Equal(1, p.Merged);
        Assert.Equal(1, p.Review);
        Assert.Equal(1, p.NeedsFix);
        Assert.Equal(1, p.Running);
        Assert.False(p.AllMerged);
        Assert.All(p.Tasks, t => Assert.Equal("AITeam", t.Project));
        Assert.Equal("Cổng KT: lỗi mạng", p.Tasks[0].Title);
        Assert.Equal(OfficeTaskStatus.Planned, p.Tasks.Single(t => t.Code == "K1-05").Status);
        Assert.DoesNotContain(p.Tasks, t => t.Code is "UI-01" or "V9-99" or "K1-06");
    }

    [Fact]
    public void All_merged_and_states_from_status_line()
    {
        var all = Service().GetAll();
        Assert.Equal(new[] { "R-014", "R-013", "R-012" }, all.Select(r => r.Id));
        Assert.Equal(RequestState.Done, all[1].State);
        Assert.Equal(RequestState.Cancelled, all[2].State);
        Assert.Equal(0, all[2].Total);
        Assert.False(all[2].AllMerged);

        File.WriteAllText(Path.Combine(_projectPath, "tasks", "README.md"),
            "| Task | Trạng thái |\n|---|---|\n| [V9-99 X](V9-99-khac.md) | ✅ Đã merge |\n");
        var r013 = Service().Get("R-013")!;
        Assert.Equal(1, r013.Total);
        Assert.True(r013.AllMerged);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R-14")]
    [InlineData("R-014x")]
    [InlineData("..\\R-014")]
    [InlineData("R-999")]
    public void Unknown_or_bad_id_returns_null(string id) => Assert.Null(Service().Get(id));

    [Fact]
    public void SetState_done_rewrites_only_status_line_keeps_bom_crlf_and_appends_journal()
    {
        var r = Service().SetState("R-014", RequestState.Done, "  mọi task\r\nđã merge | xong  ");
        Assert.True(r.Ok, r.Error);

        var bytes = File.ReadAllBytes(_requestFile);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.Contains("- **Trạng thái:** ✅ Xong 20:05 04/11/2026 — mọi task đã merge | xong\r\n", text);
        Assert.Contains("| 20:05 04/11 | Đổi trạng thái phiếu: Xong (Hub) | mọi task đã merge / xong |\r\n", text);
        // Dòng nhật ký mới nằm ngay sau dòng nhật ký cũ (cuối bảng mục 3), không lọt sang mục 4
        Assert.True(text.IndexOf("Đổi trạng thái phiếu", StringComparison.Ordinal) < text.IndexOf("## 4.", StringComparison.Ordinal));
        Assert.True(text.IndexOf("Nhận yêu cầu", StringComparison.Ordinal) < text.IndexOf("Đổi trạng thái phiếu", StringComparison.Ordinal));
        // Phần còn lại giữ nguyên
        Assert.Equal(RequestText.Replace("- **Trạng thái:** 🟡 Đang làm\r\n", "").Replace("| 20:40 03/11 | Nhận yêu cầu | |\r\n", ""),
            text.Replace("- **Trạng thái:** ✅ Xong 20:05 04/11/2026 — mọi task đã merge | xong\r\n", "")
                .Replace("| 20:40 03/11 | Nhận yêu cầu | |\r\n", "")
                .Replace("| 20:05 04/11 | Đổi trạng thái phiếu: Xong (Hub) | mọi task đã merge / xong |\r\n", ""));
        Assert.Equal(RequestState.Done, Service().Get("R-014")!.State);
    }

    [Fact]
    public void SetState_cancel_and_reopen_without_note()
    {
        var s = Service();
        Assert.True(s.SetState("R-014", RequestState.Cancelled, null).Ok);
        Assert.Contains("- **Trạng thái:** ❌ Hủy 20:05 04/11/2026\r\n", File.ReadAllText(_requestFile));
        Assert.Equal(RequestState.Cancelled, s.Get("R-014")!.State);

        Assert.True(s.SetState("R-014", RequestState.Open, "   ").Ok);
        Assert.Contains("- **Trạng thái:** 🟡 Đang làm\r\n", File.ReadAllText(_requestFile));
        Assert.Contains("Đổi trạng thái phiếu: Mở lại (Hub)", File.ReadAllText(_requestFile));
    }

    [Fact]
    public void SetState_note_is_capped_at_200_chars()
    {
        Assert.True(Service().SetState("R-014", RequestState.Done, new string('a', 500)).Ok);
        var line = File.ReadAllLines(_requestFile).Single(l => l.StartsWith("- **Trạng thái:**", StringComparison.Ordinal));
        Assert.EndsWith(" — " + new string('a', 200), line);
    }

    [Theory]
    [InlineData("R-999")]
    [InlineData("..\\..\\x")]
    [InlineData("R-014/../R-013")]
    public void SetState_bad_or_missing_id_fails_without_writing(string id)
    {
        var before = File.ReadAllBytes(_requestFile);
        var r = Service().SetState(id, RequestState.Done, null);
        Assert.False(r.Ok);
        Assert.False(string.IsNullOrWhiteSpace(r.Error));
        Assert.Equal(before, File.ReadAllBytes(_requestFile));
    }

    [Fact]
    public void SetState_request_without_status_line_fails_without_writing()
    {
        var file = Path.Combine(_env.AITeamRoot, "requests", "R-011-khong-trang-thai.md");
        File.WriteAllText(file, "# R-011 · Không có dòng trạng thái\n");
        var r = Service().SetState("R-011", RequestState.Done, null);
        Assert.False(r.Ok);
        Assert.Equal("# R-011 · Không có dòng trạng thái\n", File.ReadAllText(file));
    }

    [Fact]
    public void Registered_in_di()
    {
        var src = File.ReadAllText(Path.Combine(FindRoot(), "hub", "Services", "HubV3Registration.cs"));
        Assert.Contains("IRequestProgressService", src);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AITeam.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("AITeam.sln");
    }
}
