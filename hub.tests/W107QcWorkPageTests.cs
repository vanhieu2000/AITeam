using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using AITeam.Hub.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AITeam.Hub.Tests;

/// <summary>Hồi quy ba yêu cầu QC W1-07, giữ nguyên bộ nghiệm thu của quản lý.</summary>
public sealed class W107QcWorkPageTests
{
    private sealed class Config(ProjectInfo project) : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => [project];
        public ProjectInfo? FindProject(string name) => name == project.Name ? project : null;
        public TeamSettings GetSettings() => new("", 1, [], "", "", "", [], "");
        public void SaveSettings(TeamSettings settings) => throw new InvalidOperationException("Không được ghi.");
        public void SaveProject(ProjectInfo value) => throw new InvalidOperationException("Không được ghi.");
    }

    private sealed class Source(WorkProjectProgress snapshot) : IWorkProgressService
    {
        public WorkProjectProgress GetProject(string project) => snapshot;
        public WorkRequestProgress GetRequest(string project, string requestId) =>
            throw new InvalidOperationException("Không được đọc lại nguồn.");
    }

    [Theory]
    [InlineData(WorkStepState.Done, WorkStep.Merge, "done", true)]
    [InlineData(WorkStepState.Failed, WorkStep.Qc, "failed", false)]
    public async Task Current_step_keeps_state_and_snapshot_metadata_and_long_detail_title(
        WorkStepState state, WorkStep current, string stateClass, bool merged)
    {
        using var env = new TestEnvironment();
        var project = new ProjectInfo("AITeam", "test/repo", "unused", "unused", "", "", "",
            new Dictionary<string, string>(), true);
        var title = new string('X', 140);
        const string time = "09:41 26/10/2026";
        const string actor = "Người phụ trách thử nghiệm";
        var error = state == WorkStepState.Failed ? "Lỗi từ snapshot <nguồn>" : null;
        var progress = new WorkProgress(current, Enum.GetValues<WorkStep>().Select(s =>
            new WorkProgressStep(s, s == current ? state : WorkStepState.Waiting,
                s == current ? time : "Chưa có dữ liệu", s == current ? actor : "Chưa có dữ liệu")).ToArray(),
            error, merged);
        var task = new WorkTaskProgress("S1-04", title, "B", progress, []);
        var request = new WorkRequestProgress("R-010", title, progress, [task],
            Enum.GetValues<WorkStep>().ToDictionary(s => s, s => s == WorkStep.Merge ? 1 : 0));
        var snapshot = new WorkProjectProgress(project, [request], []);
        await using var host = await HubTestHost.StartAsync(env, services =>
        {
            services.AddSingleton<ITeamConfigService>(new Config(project));
            services.AddSingleton<IWorkProgressService>(new Source(snapshot));
        });
        var response = await host.Client.GetAsync("/p/AITeam?requestId=R-010&taskId=S1-04");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var currentItem = Assert.Single(Regex.Matches(html, @"<li\b[^>]*aria-current=""step""[^>]*>.*?</li>",
            RegexOptions.Singleline).Cast<Match>());
        Assert.Matches($@"class=""work-step [^""]*\bcurrent\b[^""]*\b{stateClass}\b", currentItem.Value);
        Assert.Contains($"data-work-step=\"{(int)current}\"", currentItem.Value);
        Assert.Contains(time, WebUtility.HtmlDecode(currentItem.Value));
        Assert.Contains(actor, WebUtility.HtmlDecode(currentItem.Value));
        Assert.DoesNotContain("17:32", html);
        Assert.DoesNotContain("17:03", html);
        if (error != null)
        {
            Assert.Contains(error, WebUtility.HtmlDecode(html));
            Assert.DoesNotContain(error, html);
        }
        var heading = Regex.Match(html, @"<div class=""work-detail-title"">.*?<h2[^>]*>(.*?)</h2>",
            RegexOptions.Singleline);
        Assert.True(heading.Success, "Tiêu đề chi tiết phải dùng class co và xuống dòng.");
        Assert.Contains(title, heading.Groups[1].Value);
        var css = File.ReadAllText(CssPath());
        Assert.Matches(@"(?s)\.work-detail-title\s*\{[^}]*min-width:\s*0;[^}]*overflow-wrap:\s*anywhere;", css);
        Assert.Matches(@"(?s)\.work-steps\s+\.current\s*\{[^}]*font-weight:\s*(600|700|bold)", css);
    }

    private static string CssPath([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../hub/wwwroot/css/hub.css"));
}