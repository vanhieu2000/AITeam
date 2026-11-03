using System.Diagnostics;
using AITeam.Hub.Models;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

// TEST NGHIỆM THU H3-01 (Claude viết trước — agent KHÔNG sửa). Thiết kế: docs/design/HUB-V3.md mục 3.
// Dữ liệu mẫu chép từ task thật V4-06 / V4-03 và metrics/runs thật (24/10/2026).
public sealed class AcceptanceH301Tests : IDisposable
{
    private const string V406 = """
        # V4-06 · Trạng thái job + phục hồi sau sự cố (JobStore)

        - **Agent:** B
        - **Nhánh:** `agent-b/V4-06-job-state`
        - **Ngân sách:** ~250 dòng
        - **Yêu cầu:** R-002
        - **Độ khó:** M
        - **Thời gian tối đa:** 25 phút

        ## Tóm tắt
        - [CK] (phạm vi) dòng này nằm ngoài mục Review nên KHÔNG được tính.

        ## Review (Claude)
        ### Vòng 1 — ĐẠT (24/10/2026, Claude)
        - Cổng kiểm tra ĐẠT; 59/59 test core.
        - Mẫu: - [AG|TK|CK] (loại) mô tả
        - **Lỗi của quản lý / công cụ, không phải của Lan:**
          - [TK] (phạm vi) mục Phạm vi quên `JobModels.cs` (file khung của Claude);
          - [CK] (phạm vi) cổng kiểm tra tính file khung là agent sửa ngoài phạm vi.
        - Optional: khe hở khóa mồ côi.

        ## Kết quả
        - **Có gì mới:** lưu trạng thái từng task.

        ## Đánh giá quy trình
        | Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
        |---|---|---|---|---|---|---|
        | 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-002) | — | — | — |
        | 2. Ý tưởng | Claude | 5 | (chung R-002) | — | — | — |
        | 3. Plan / thiết kế | Claude | 5 | ~20' | Bảng chuyển trạng thái chốt trong khung | — | Giữ cách này |
        | 4. Giao việc | Claude | 2 | ~20' | Phạm vi quên `JobModels.cs` | TK | Phạm vi tự thêm file khung |
        | 5. Thực hiện code | Lan (B) | 5 | ~13' (3 lượt) | Buộc gộp file để qua cổng | — | — |
        | 6. Review / QC | Cổng KT + Claude | 2 | ~15' | Cổng KT báo nhầm, tự giao lại 2 lần | CK | Đã sửa cổng KT |
        | 7. Đánh giá & bàn giao | Claude | 5 | ~5' | `merge` chạy trơn tru | — | — |

        ## Mục sau
        | 8. Không thuộc bảng | X | 1 | | | AG | |
        """;

    private const string V403 = """
        # V4-03 · Codex review chỉ đọc

        - **Agent:** A
        - **Yêu cầu:** R-002
        - **Độ khó:** m (vừa)

        ## Review (Claude)
        ### Vòng 1 — ĐẠT
        - [AG] (bảo mật) quên chặn đường dẫn tuyệt đối.
        - [TK] (BOM) task không nói file schema phải không BOM

        ## Đánh giá quy trình
        | Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
        |---|---|---|---|---|---|---|
        | 1. Tiếp nhận & quản lý | Claude | 5 | | | | |
        | 2. Ý tưởng | Claude | 5 |
        | 3. Plan / thiết kế | Claude | 4 | | Thiếu BOM | TK | Ghi rõ |
        | 4. Giao việc | Claude | 4 |
        | 5. Thực hiện code | Minh (A) | 5 |
        | 6. Review / QC | Codex | ? | | Codex hết hạn mức | HT | Reviewer dự phòng |
        | 7. Đánh giá & bàn giao | Claude | 5 |
        """;

    private const string T007 = """
        # T-007 · Giới thiệu: thẻ Hệ thống bị cắt
        - **Agent:** A

        ## Review (Claude)
        ### Vòng 1 — CẦN SỬA
        - [AG] (giao diện) chữ bị cắt
        ### Vòng 2 — ĐẠT
        """;

    // Bản ghi thật metrics/runs/20261023235627-DESKTOP-PP14EFS-AITeam-V4-06-3.json (rút gọn số).
    private const string RealRun = """
        {
            "at":  "2026-10-23T23:56:27",
            "machine":  "DESKTOP-PP14EFS",
            "project":  "AITeam",
            "task":  "V4-06",
            "run":  "3",
            "agent":  "B",
            "model":  "mặc định",
            "status":  "SUCCESS",
            "durationSec":  240,
            "outputTokens":  37127,
            "totalTokens":  189519,
            "linesAdded":  436,
            "linesRemoved":  0,
            "verifyOk":  true,
            "verifyFails":  0,
            "autoRetry":  false,
            "log":  "V4-06-3.json",
            "request":  "R-002"
        }
        """;

    private const string Imp = """
        # IMP-001 · Chu kỳ cải tiến sau 3 task

        - **Trạng thái:** 🟡 Đề xuất <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng -->
        - **Task trong chu kỳ:** AITeam/V4-03, AITeam/V4-06 , AITeam/H3-01 <!-- vd. -->
        - **Ngày:** 24/10/2026
        """;

    private readonly TestEnvironment _env = new();
    private readonly string _projectDir;

    public AcceptanceH301Tests()
    {
        _projectDir = Path.Combine(_env.RootDir, "AITeamProject");
        Directory.CreateDirectory(Path.Combine(_projectDir, "tasks"));
    }

    public void Dispose() => _env.Dispose();

    // ───────────── Hàm thuần ─────────────

    [Theory]
    [InlineData("- **Độ khó:** M", Difficulty.M)]
    [InlineData("- **Độ khó:** l (lớn)", Difficulty.L)]
    [InlineData("x\r\n- **Độ khó:** S\r\n", Difficulty.S)]
    [InlineData("- **Độ khó:** X", Difficulty.Unknown)]
    [InlineData("- **Ngân sách:** ~250 dòng", Difficulty.Unknown)]
    [InlineData("", Difficulty.Unknown)]
    public void Difficulty_is_parsed(string md, Difficulty expected)
        => Assert.Equal(expected, InsightParsers.ParseDifficulty(md));

    [Fact]
    public void Request_is_parsed_or_null()
    {
        Assert.Equal("R-002", InsightParsers.ParseRequest(V406));
        Assert.Null(InsightParsers.ParseRequest(T007));
        Assert.Null(InsightParsers.ParseRequest("- **Yêu cầu:** R-xxx"));
    }

    [Fact]
    public void Process_review_table_of_real_task_gives_7_rows()
    {
        var rows = InsightParsers.ParseProcessReview(V406);
        Assert.Equal(7, rows.Count); // dòng "8." ở mục sau không thuộc bảng
        Assert.Equal(Enumerable.Range(1, 7), rows.Select(r => r.Index));

        var r1 = rows[0];
        Assert.Equal("1. Tiếp nhận & quản lý", r1.Stage);
        Assert.Equal(5, r1.Score);
        Assert.Equal("", r1.Issue);
        Assert.Equal("", r1.Cause);
        Assert.Equal("(chung R-002)", r1.Time);

        var r4 = rows[3];
        Assert.Equal("Claude", r4.Who);
        Assert.Equal(2, r4.Score);
        Assert.Equal("TK", r4.Cause);
        Assert.Equal("Phạm vi quên `JobModels.cs`", r4.Issue);
        Assert.Equal("Phạm vi tự thêm file khung", r4.Proposal);

        Assert.Equal("Lan (B)", rows[4].Who);
        Assert.Equal("CK", rows[5].Cause);
    }

    [Fact]
    public void Process_review_tolerates_short_rows_and_bad_scores()
    {
        var rows = InsightParsers.ParseProcessReview(V403);
        Assert.Equal(7, rows.Count);
        Assert.Equal(5, rows[1].Score);
        Assert.Equal("", rows[1].Issue); // dòng chỉ có 3 ô
        Assert.Null(rows[5].Score);      // "?"
        Assert.Equal("HT", rows[5].Cause);
        Assert.Empty(InsightParsers.ParseProcessReview(T007));
        Assert.Empty(InsightParsers.ParseProcessReview("## Đánh giá quy trình\n"));
    }

    [Fact]
    public void Review_issues_only_inside_review_section_and_only_5_codes()
    {
        var issues = InsightParsers.ParseReviewIssues(V406, "AITeam", "V4-06");
        Assert.Equal(2, issues.Count);
        Assert.Equal(("TK", "phạm vi"), (issues[0].Cause, issues[0].Kind));
        Assert.Equal("mục Phạm vi quên `JobModels.cs` (file khung của Claude)", issues[0].Text); // bỏ ";" cuối
        Assert.Equal("CK", issues[1].Cause);
        Assert.EndsWith("ngoài phạm vi", issues[1].Text); // bỏ "." cuối
        Assert.All(issues, i => Assert.Equal(("AITeam", "V4-06"), (i.Project, i.Task)));

        var v403 = InsightParsers.ParseReviewIssues(V403, "AITeam", "V4-03");
        Assert.Equal(new[] { "AG", "TK" }, v403.Select(i => i.Cause));
        Assert.Equal("BOM", v403[1].Kind);
    }

    [Fact]
    public void Review_issue_without_kind_has_empty_kind()
    {
        var md = "## Review (Claude)\n- [NV] Lan dừng giữa chừng\n";
        var i = Assert.Single(InsightParsers.ParseReviewIssues(md, "P", "T"));
        // V9-12 (R-012): mã nguyên nhân ngoài 9 mã chuẩn (NV cũ) ⇒ "khác"; nội dung giữ nguyên.
        Assert.Equal(("khác", "", "Lan dừng giữa chừng"), (i.Cause, i.Kind, i.Text));
    }

    [Theory]
    [InlineData("### Vòng 1 — ĐẠT (24/10/2026, Claude)", 1, true)]
    [InlineData("### Vòng 1 - ĐẠT", 1, true)]
    [InlineData("### Vòng 1 — CẦN SỬA\n### Vòng 2 — ĐẠT", 2, false)]
    [InlineData("không có review", 0, false)]
    public void Review_rounds(string md, int rounds, bool passed1)
        => Assert.Equal((rounds, passed1), InsightParsers.ParseReviewRounds(md));

    [Fact]
    public void Real_run_record_is_parsed()
    {
        var r = InsightParsers.ParseRun(RealRun);
        Assert.NotNull(r);
        Assert.Equal(new DateTime(2026, 10, 23, 23, 56, 27), r!.At);
        Assert.Equal(("AITeam", "V4-06", 3, "B"), (r.Project, r.Task, r.Run, r.Agent));
        Assert.Equal(("SUCCESS", 240, 189519L, 436), (r.Status, r.DurationSec, r.TotalTokens, r.LinesAdded));
        Assert.True(r.VerifyOk);
        Assert.False(r.AutoRetry);
        Assert.Equal("R-002", r.Request);
    }

    [Fact]
    public void Run_record_variants_and_garbage()
    {
        var n = InsightParsers.ParseRun("""{"at":"2026-10-21T10:00:00","project":"HDVTool","task":"T-007","run":2}""");
        Assert.NotNull(n);
        Assert.Equal(2, n!.Run);
        Assert.Equal(0, n.DurationSec);
        Assert.False(n.VerifyOk);
        Assert.Null(n.Request);

        Assert.Null(InsightParsers.ParseRun("{ hỏng"));
        Assert.Null(InsightParsers.ParseRun("""{"at":"2026-10-21T10:00:00","project":"P"}"""));
        Assert.Null(InsightParsers.ParseRun("""{"project":"P","task":"T"}"""));
        Assert.Null(InsightParsers.ParseRun(""));
    }

    [Fact]
    public void Improvement_file_is_parsed()
    {
        var imp = InsightParsers.ParseImprovement("IMP-001-chu-ky-1.md", Imp);
        Assert.NotNull(imp);
        Assert.Equal("IMP-001", imp!.Id);
        Assert.Equal("IMP-001 · Chu kỳ cải tiến sau 3 task", imp.Title);
        Assert.Equal("🟡 Đề xuất", imp.Status);
        Assert.Equal(new[] { "AITeam/V4-03", "AITeam/V4-06", "AITeam/H3-01" }, imp.Tasks);
        Assert.Equal("IMP-001-chu-ky-1.md", imp.FileName);
        Assert.Null(InsightParsers.ParseImprovement("ghi-chu.md", Imp));
    }

    [Fact]
    public void Staff_file_is_parsed()
    {
        Assert.Equal(("A", "Minh"), InsightParsers.ParseStaff("A-minh.md", "# Minh\n\n- **Tên:** Minh\n- **Mã:** A"));
        Assert.Null(InsightParsers.ParseStaff("A-minh.md", "# không có dòng tên"));
    }

    private static RunRecord Run(string task, int n, string at, int sec, bool verifyOk = true, string? req = "R-002", string agent = "B")
        => new(DateTime.Parse(at), "M", "AITeam", task, n, agent, "SUCCESS", sec, 1000, 10, verifyOk, n > 1, req);

    [Fact]
    public void Timeline_from_runs_and_merge_time()
    {
        var runs = new[]
        {
            Run("V4-06", 2, "2026-10-23T23:51:17", 240),
            Run("V4-06", 1, "2026-10-23T23:48:00", 300), // cố ý không theo thứ tự
        };
        var merged = new DateTime(2026, 10, 23, 23, 59, 33);
        var t = InsightParsers.BuildTimeline(runs, merged);

        Assert.Equal(new[] { SegmentKind.Coding, SegmentKind.Fix, SegmentKind.Review, SegmentKind.Merge }, t.Select(s => s.Kind));
        Assert.Equal(new DateTime(2026, 10, 23, 23, 43, 0), t[0].Start);
        Assert.Equal(new DateTime(2026, 10, 23, 23, 48, 0), t[0].End);
        Assert.Equal(new DateTime(2026, 10, 23, 23, 47, 17), t[1].Start);
        Assert.Equal((new DateTime(2026, 10, 23, 23, 51, 17), merged), (t[2].Start, t[2].End));
        Assert.Equal((merged, merged), (t[3].Start, t[3].End));
    }

    [Fact]
    public void Timeline_edge_cases()
    {
        var merged = new DateTime(2026, 10, 21, 17, 0, 0);
        Assert.Equal(new[] { SegmentKind.Merge }, InsightParsers.BuildTimeline(Array.Empty<RunRecord>(), merged).Select(s => s.Kind));
        Assert.Empty(InsightParsers.BuildTimeline(Array.Empty<RunRecord>(), null));
        var one = InsightParsers.BuildTimeline(new[] { Run("X", 1, "2026-10-21T10:00:00", 60) }, null);
        Assert.Equal(SegmentKind.Coding, Assert.Single(one).Kind);
    }

    // ───────────── Dịch vụ ghép dữ liệu ─────────────

    private sealed class FakeGit : IGitHistory
    {
        public readonly List<string> Calls = new();
        public readonly Dictionary<string, DateTime> Merges = new(StringComparer.OrdinalIgnoreCase);
        public DateTime? MergedAt(string repoPath, string taskId)
        {
            Calls.Add(taskId);
            return Merges.TryGetValue(taskId, out var d) ? d : null;
        }
    }

    private sealed class FakeConfig(ProjectInfo p) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => new[] { p };
        public ProjectInfo? FindProject(string name) => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase) ? p : null;
        public TeamSettings GetSettings() => new("", 3, Array.Empty<DeveloperInfo>(), "", "", "", Array.Empty<string>(), "");
        public void SaveSettings(TeamSettings settings) { }
        public void SaveProject(ProjectInfo project) { }
    }

    private sealed class FakeBoard(ProjectInfo p, IReadOnlyList<TaskCard> cards) : ITaskBoardService
    {
        public ProjectBoard GetBoard(string project) => new(p, cards, "");
        public TaskDetail? GetTask(string project, string taskId) => null;
    }

    private (TeamInsightsService Svc, FakeGit Git) BuildService(bool withData = true)
    {
        var p = new ProjectInfo("AITeam", "vanhieu2000/AITeam", _projectDir, _projectDir, "", "", "", new Dictionary<string, string>(), true);
        var cards = new List<TaskCard>
        {
            new("AITeam", "T-007", "Giới thiệu", "A", "agent-a/T-007", TaskState.Merged, "T-007-about.md", 0, null),
            new("AITeam", "V4-03", "Codex review", "A", "agent-a/V4-03", TaskState.Merged, "V4-03-codex-review.md", 0, null),
            new("AITeam", "H3-09", "Chưa có file", "B", "agent-b/H3-09", TaskState.Assigned, "", 0, null),
            new("AITeam", "V4-06", "Job state", "B", "agent-b/V4-06", TaskState.Merged, "V4-06-job-state.md", 0, null),
        };
        var git = new FakeGit();
        git.Merges["V4-06"] = new DateTime(2026, 10, 23, 23, 59, 33);
        git.Merges["V4-03"] = new DateTime(2026, 10, 23, 23, 45, 38);
        git.Merges["T-007"] = new DateTime(2026, 10, 21, 17, 28, 44);

        if (withData)
        {
            File.WriteAllText(Path.Combine(_projectDir, "tasks", "V4-06-job-state.md"), V406);
            File.WriteAllText(Path.Combine(_projectDir, "tasks", "V4-03-codex-review.md"), V403);
            File.WriteAllText(Path.Combine(_projectDir, "tasks", "T-007-about.md"), T007);

            var runs = Path.Combine(_env.AITeamRoot, "metrics", "runs");
            Directory.CreateDirectory(runs);
            File.WriteAllText(Path.Combine(runs, "a.json"), """{"at":"2026-10-23T23:43:15","project":"AITeam","task":"V4-06","run":"1","agent":"B","durationSec":300,"verifyOk":false,"request":"R-002"}""");
            File.WriteAllText(Path.Combine(runs, "b.json"), """{"at":"2026-10-23T23:51:17","project":"AITeam","task":"V4-06","run":"2","agent":"B","durationSec":240,"verifyOk":false,"autoRetry":true,"request":"R-002"}""");
            File.WriteAllText(Path.Combine(runs, "c.json"), RealRun);
            File.WriteAllText(Path.Combine(runs, "d.json"), """{"at":"2026-10-23T23:41:00","project":"aiteam","task":"v4-03","run":"1","agent":"A","durationSec":540,"verifyOk":true,"request":"R-002"}""");
            File.WriteAllText(Path.Combine(runs, "e.json"), """{"at":"2026-10-23T23:41:00","project":"HDVTool","task":"V4-03","run":"1","agent":"A","durationSec":9999}""");
            File.WriteAllText(Path.Combine(runs, "f.json"), "{ hỏng");

            var staff = Path.Combine(_env.AITeamRoot, "staff");
            Directory.CreateDirectory(staff);
            File.WriteAllText(Path.Combine(staff, "A-minh.md"), "# Minh\n- **Tên:** Minh\n");
            File.WriteAllText(Path.Combine(staff, "B-lan.md"), "# Lan\n- **Tên:** Lan\n");

            var imp = Path.Combine(_env.AITeamRoot, "improvements");
            Directory.CreateDirectory(imp);
            File.WriteAllText(Path.Combine(imp, "IMP-002-b.md"), Imp.Replace("IMP-001", "IMP-002"));
            File.WriteAllText(Path.Combine(imp, "IMP-001-a.md"), Imp);
            File.WriteAllText(Path.Combine(imp, "README.md"), "# không phải IMP");
        }

        return (new TeamInsightsService(_env.Paths, new FakeConfig(p), new FakeBoard(p, cards), git), git);
    }

    [Fact]
    public void Insight_of_real_task_V4_06()
    {
        var (svc, _) = BuildService();
        var t = svc.GetTask("AITeam", "V4-06");
        Assert.NotNull(t);
        Assert.Equal(("Job state", "B", "Lan"), (t!.Title, t.Agent, t.StaffName));
        Assert.Equal((Difficulty.M, "R-002", TaskState.Merged), (t.Difficulty, t.Request, t.State));
        Assert.Equal(3, t.Runs);
        Assert.Equal(13, t.AgentMinutes);              // 300 + 240 + 240 giây
        Assert.Equal((1, true), (t.ReviewRounds, t.PassedRound1));
        Assert.False(t.FirstRunVerifyOk);
        Assert.Equal(4.1, t.ProcessScore);             // (5+5+5+2+5+2+5)/7 = 4,14
        Assert.Equal(new DateTime(2026, 10, 23, 23, 59, 33), t.MergedAt);
        Assert.Equal(7, t.Stages.Count);
        Assert.Equal(2, t.Issues.Count);
        Assert.Equal(new[] { SegmentKind.Coding, SegmentKind.Fix, SegmentKind.Fix, SegmentKind.Review, SegmentKind.Merge }, t.Timeline.Select(s => s.Kind));
    }

    [Fact]
    public void Run_matching_ignores_case_but_not_other_projects()
    {
        var (svc, _) = BuildService();
        var t = svc.GetTask("aiteam", "v4-03");
        Assert.NotNull(t);
        Assert.Equal("V4-03", t!.Id);
        Assert.Equal(1, t.Runs);          // bản ghi HDVTool/V4-03 không tính
        Assert.Equal(9, t.AgentMinutes);
        Assert.Equal(Difficulty.M, t.Difficulty);
        Assert.Equal(4.7, t.ProcessScore); // (5+5+4+4+5+5)/6 — ô "?" bỏ qua
        Assert.Null(svc.GetTask("AITeam", "KHONG-CO"));
    }

    [Fact]
    public void Aggregates_staff_stages_issues_improvements_kpis()
    {
        var (svc, git) = BuildService();
        var all = svc.Get();

        Assert.Equal(new[] { "V4-06", "V4-03", "T-007", "H3-09" }, all.Tasks.Select(t => t.Id)); // mới nhất trước; không ngày ⇒ cuối
        Assert.DoesNotContain("H3-09", git.Calls); // chưa merge ⇒ không hỏi git

        var h309 = all.Tasks.Single(t => t.Id == "H3-09");
        Assert.Equal((Difficulty.Unknown, 0, (double?)null), (h309.Difficulty, h309.Runs, h309.ProcessScore));
        Assert.Empty(h309.Timeline);

        // Đội ngũ: chỉ task có Yêu cầu R-xxx ⇒ T-007 (quy trình cũ) không tính cho Minh.
        Assert.Equal(new[] { "A", "B" }, all.Staff.Select(s => s.Agent));
        var minh = all.Staff[0];
        Assert.Equal(("Minh", 1, 1, 9.0), (minh.Name, minh.Tasks, minh.Runs, minh.AvgMinutesPerRun));
        Assert.Equal(1.0, minh.VerifyFirstPassRate);
        Assert.Equal(1.0, minh.Round1Rate);
        Assert.Equal(1, minh.AgErrorsByKind["bảo mật"]);
        Assert.False(minh.AgErrorsByKind.ContainsKey("giao diện")); // lỗi của T-007 không tính
        var lan = all.Staff[1];
        Assert.Equal(("Lan", 1, 3, 4.3), (lan.Name, lan.Tasks, lan.Runs, lan.AvgMinutesPerRun));
        Assert.Equal(0.0, lan.VerifyFirstPassRate);
        Assert.Empty(lan.AgErrorsByKind);

        Assert.Equal(Enumerable.Range(1, 7), all.StageAverages.Select(s => s.Index));
        Assert.Equal(InsightParsers.StageNames, all.StageAverages.Select(s => s.Stage));
        Assert.Equal(3.0, all.StageAverages[3].Average);   // Giao việc: (2+4)/2
        Assert.Equal(2, all.StageAverages[3].Count);
        Assert.Equal(1, all.StageAverages[5].Count);       // Review/QC: chỉ điểm 2 của V4-06; ô "?" của V4-03 không tính
        Assert.Equal(2.0, all.StageAverages[5].Average);
        Assert.Equal(4.5, all.StageAverages[2].Average);   // Plan: (5+4)/2

        // Theo thứ tự task (mới nhất trước), rồi thứ tự dòng trong bảng.
        Assert.Equal(new[] { "V4-06/TK", "V4-06/CK", "V4-03/TK", "V4-03/HT" }, all.Issues.Select(i => i.Task + "/" + i.Cause));
        Assert.Equal((4, "4. Giao việc", "Phạm vi tự thêm file khung"), (all.Issues[0].StageIndex, all.Issues[0].Stage, all.Issues[0].Proposal));

        Assert.Equal(new[] { "IMP-001", "IMP-002" }, all.Improvements.Select(i => i.Id));

        Assert.Equal(2, all.Kpis.TasksCounted);
        Assert.Equal(1.0, all.Kpis.Round1Rate);
        Assert.Equal(11.0, all.Kpis.AvgAgentMinutes); // (13 + 9) / 2
    }

    [Fact]
    public void Missing_folders_and_files_do_not_throw()
    {
        var (svc, _) = BuildService(withData: false);
        var all = svc.Get();
        Assert.Equal(4, all.Tasks.Count);
        Assert.All(all.Tasks, t => Assert.Equal(0, t.Runs));
        Assert.Empty(all.Staff);
        Assert.Empty(all.Improvements);
        Assert.Equal(7, all.StageAverages.Count);
        Assert.All(all.StageAverages, s => Assert.Null(s.Average));
        Assert.Equal((0, (double?)null, (double?)null), (all.Kpis.TasksCounted, all.Kpis.Round1Rate, all.Kpis.AvgAgentMinutes));
    }

    // ───────────── Git thật (không giả) ─────────────

    [Theory]
    [InlineData("--all")]
    [InlineData("V4-06\"; echo x")]
    [InlineData("../x")]
    [InlineData("")]
    public void Git_history_rejects_unsafe_task_ids(string id)
        => Assert.Null(new GitHistory().MergedAt(_projectDir, id));

    [Fact]
    public void Git_history_reads_real_merge_commit()
    {
        var repo = Path.Combine(_env.RootDir, "repo");
        Directory.CreateDirectory(repo);
        void Git(params string[] args)
        {
            var psi = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            psi.ArgumentList.Add("-C"); psi.ArgumentList.Add(repo);
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            p.WaitForExit(20000);
            Assert.True(p.ExitCode == 0, string.Join(" ", args) + ": " + p.StandardError.ReadToEnd());
        }
        Git("init", "-q", "-b", "main");
        Git("config", "user.email", "t@t"); Git("config", "user.name", "t");
        Git("commit", "-q", "--allow-empty", "-m", "goc");
        Git("checkout", "-q", "-b", "agent-a/X-01");
        Git("commit", "-q", "--allow-empty", "-m", "X-01 code");
        Git("checkout", "-q", "main");
        Git("merge", "--no-ff", "-q", "agent-a/X-01", "-m", "Merge X-01-abc (Minh (A), review đạt vòng 1)");

        var at = new GitHistory().MergedAt(repo, "X-01");
        Assert.NotNull(at);
        Assert.True(Math.Abs((DateTime.Now - at!.Value).TotalMinutes) < 5);
        Assert.Null(new GitHistory().MergedAt(repo, "X-0"));   // tiền tố không được khớp nhầm
        Assert.Null(new GitHistory().MergedAt(repo, "X-02"));
    }
}
