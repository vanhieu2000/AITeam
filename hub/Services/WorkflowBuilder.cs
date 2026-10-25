using AITeam.Hub.Models;

namespace AITeam.Hub.Services;

/// <summary>Bảy mốc theo thứ tự quy trình.</summary>
public enum WorkflowStage { Intake = 1, Idea, Plan, Preparation, Code, Quality, Acceptance }
/// <summary>Chưa có bằng chứng; đang làm; hoàn tất; cần sửa.</summary>
public enum WorkflowStatus { Pending, Running, Done, NeedsFix }
/// <summary>Quyết định đã chuẩn hóa, danh sách cũ trước mới sau; phần tử cuối mỗi mốc thắng.</summary>
public sealed record WorkflowDecision(WorkflowStage Stage, bool Approved, bool IsFinal, string Evidence);
/// <summary>Vòng là số nguyên dương; chọn số lớn nhất, cùng vòng chọn phần tử cuối.</summary>
public sealed record WorkflowReview(int Round, bool Passed, string Evidence);
/// <summary>Snapshot task; Prepared nghĩa là đã giao khung; null Reviews/Logs tương đương rỗng.</summary>
public sealed record WorkflowTaskSnapshot(string Id, string Owner, TaskState State, bool Prepared,
    IReadOnlyList<WorkflowReview>? Reviews = null, IReadOnlyList<AgentRun>? Logs = null);
/// <summary>Snapshot trong bộ nhớ; chuỗi null/trắng là thiếu, danh sách null là rỗng.</summary>
public sealed record WorkflowSnapshot(string? Brief = null, string? Plan = null,
    IReadOnlyList<WorkflowDecision>? Decisions = null, IReadOnlyList<WorkflowTaskSnapshot>? Tasks = null);
/// <summary>Mốc có tên tiếng Việt, người phụ trách và bằng chứng văn bản thuần, không phải HTML để thực thi.</summary>
public sealed record WorkflowStep(WorkflowStage Stage, string Name, WorkflowStatus Status,
    string Owner, IReadOnlyList<string> Evidence);
/// <summary>Tiến độ riêng: đúng ba mốc Chuẩn bị, Code, QC theo thứ tự.</summary>
public sealed record WorkflowTaskProgress(string Id, IReadOnlyList<WorkflowStep> Steps);
/// <summary>Tổng phiếu có bảy mốc; Tasks xếp Id theo Ordinal.</summary>
public sealed record WorkflowResult(IReadOnlyList<WorkflowStep> Steps, IReadOnlyList<WorkflowTaskProgress> Tasks);

/// <summary>Hàm static thuần, không phải dịch vụ Hub, không cần DI.</summary>
public static class WorkflowBuilder
{
    /// <summary>
    /// Null snapshot tương đương rỗng; luôn trả Tiếp nhận, Ý tưởng, Plan, Chuẩn bị, Code, QC, Nghiệm thu.
    /// Tiếp nhận Done khi Brief không trắng, bằng chứng Brief nguyên văn. Ý tưởng theo quyết định Idea cuối:
    /// Approved là Done, từ chối NeedsFix, thiếu Pending. Plan từ chối NeedsFix; Done cần Plan không trắng
    /// và quyết định Plan Approved; còn lại Pending. Bằng chứng Plan gồm Plan và quyết định cuối nếu có.
    /// Chuẩn bị task Done nếu Prepared hoặc Merged, còn lại Pending; khi Done bằng chứng là Id.
    /// Code: Merged Done; NeedsFix hoặc review cuối thất bại là NeedsFix; sau đó Review Done;
    /// còn lại lấy log đúng Task có Attempt lớn nhất (cùng số lấy cuối): Running/Success/Error
    /// lần lượt Running/Done/NeedsFix; thiếu log Pending. Không dùng thời gian để chọn vòng.
    /// QC: Merged Done; TaskState.NeedsFix Pending; review cuối Passed Done, thất bại NeedsFix;
    /// thiếu review thì Review Running, còn lại Pending. Review cuối lấy Round số lớn nhất.
    /// Merged cũ thiếu log/review vẫn Done cả ba mốc, Code/QC có bằng chứng "Đã merge: {Id}".
    /// Code có bằng chứng LogPath được chọn và Evidence review được chọn; QC có Evidence review được chọn.
    /// Tổng mỗi mốc task: không task Pending; tất cả Done mới Done; có NeedsFix thì NeedsFix;
    /// còn lại có trạng thái khác Pending thì Running, tất cả Pending thì Pending.
    /// Nghiệm thu chỉ theo quyết định Acceptance cuối: thiếu/chưa IsFinal là Pending;
    /// đã chốt Approved Done, từ chối NeedsFix, không suy từ PASS/merge. Bằng chứng là quyết định cuối.
    /// Owner mốc 1/2/3/6/7 là Claude, mốc 4 Codex, mốc 5 Owner task;
    /// tổng Code nối Owner khác trắng, duy nhất, xếp Ordinal bằng ", ".
    /// Mọi Evidence bỏ chuỗi trắng, bỏ trùng, xếp Ordinal; tổng mốc task hợp bằng chứng mốc tương ứng.
    /// Không sửa đầu vào, không mở đường dẫn, diễn giải HTML, đọc đĩa/mạng/đồng hồ hay chạy tiến trình.
    /// Dữ liệu thiếu nêu trên không gây lỗi nghiệp vụ; không có câu báo lỗi cho người dùng.
    /// Khung chưa triển khai ném NotImplementedException.
    /// </summary>
    public static WorkflowResult Build(WorkflowSnapshot? snapshot)
    {
        // 1. Tiep nhan
        var intakeStatus = !string.IsNullOrWhiteSpace(snapshot?.Brief) ? WorkflowStatus.Done : WorkflowStatus.Pending;
        var intakeEvidence = NormalizeEvidence(new[] { snapshot?.Brief });
        var intakeStep = new WorkflowStep(WorkflowStage.Intake, "Ti\u1EBFp nh\u1EADn", intakeStatus, "Claude", intakeEvidence);

        // 2. Y tuong
        var lastIdeaDec = snapshot?.Decisions?.LastOrDefault(d => d.Stage == WorkflowStage.Idea);
        WorkflowStatus ideaStatus;
        if (lastIdeaDec == null)
        {
            ideaStatus = WorkflowStatus.Pending;
        }
        else
        {
            ideaStatus = lastIdeaDec.Approved ? WorkflowStatus.Done : WorkflowStatus.NeedsFix;
        }
        var ideaEvidence = NormalizeEvidence(new[] { lastIdeaDec?.Evidence });
        var ideaStep = new WorkflowStep(WorkflowStage.Idea, "\u00DD t\u01B0\u1EDFng", ideaStatus, "Claude", ideaEvidence);

        // 3. Plan
        var lastPlanDec = snapshot?.Decisions?.LastOrDefault(d => d.Stage == WorkflowStage.Plan);
        WorkflowStatus planStatus;
        if (lastPlanDec != null && !lastPlanDec.Approved)
        {
            planStatus = WorkflowStatus.NeedsFix;
        }
        else if (!string.IsNullOrWhiteSpace(snapshot?.Plan) && lastPlanDec != null && lastPlanDec.Approved)
        {
            planStatus = WorkflowStatus.Done;
        }
        else
        {
            planStatus = WorkflowStatus.Pending;
        }
        var planEvidence = NormalizeEvidence(new[] { snapshot?.Plan, lastPlanDec?.Evidence });
        var planStep = new WorkflowStep(WorkflowStage.Plan, "Plan", planStatus, "Claude", planEvidence);

        // Tien do tung task
        var tasks = snapshot?.Tasks?.OrderBy(t => t.Id, StringComparer.Ordinal).ToArray() ?? Array.Empty<WorkflowTaskSnapshot>();
        var taskProgressList = new List<WorkflowTaskProgress>(tasks.Length);
        var taskPrepSteps = new List<WorkflowStep>(tasks.Length);
        var taskCodeSteps = new List<WorkflowStep>(tasks.Length);
        var taskQcSteps = new List<WorkflowStep>(tasks.Length);

        foreach (var task in tasks)
        {
            // Review cuoi theo Round so lon nhat, cung Round chon phan tu cuoi
            WorkflowReview? selectedReview = null;
            if (task.Reviews != null && task.Reviews.Count > 0)
            {
                int maxRound = task.Reviews.Max(r => r.Round);
                selectedReview = task.Reviews.Last(r => r.Round == maxRound);
            }

            // Log cuoi dung Task theo Attempt lon nhat, cung Attempt chon phan tu cuoi
            AgentRun? selectedLog = null;
            if (task.Logs != null && task.Logs.Count > 0)
            {
                var taskLogs = task.Logs.Where(l => l.Task == task.Id).ToList();
                if (taskLogs.Count > 0)
                {
                    int maxAttempt = taskLogs.Max(l => l.Attempt);
                    selectedLog = taskLogs.Last(l => l.Attempt == maxAttempt);
                }
            }

            // Chuan bi
            bool isPrepDone = task.Prepared || task.State == TaskState.Merged;
            var prepStatus = isPrepDone ? WorkflowStatus.Done : WorkflowStatus.Pending;
            var prepEvidence = isPrepDone ? NormalizeEvidence(new[] { task.Id }) : Array.Empty<string>();
            var prepStep = new WorkflowStep(WorkflowStage.Preparation, "Chu\u1EA9n b\u1ECB", prepStatus, "Codex", prepEvidence);

            // Code
            WorkflowStatus codeStatus;
            if (task.State == TaskState.Merged)
            {
                codeStatus = WorkflowStatus.Done;
            }
            else if (task.State == TaskState.NeedsFix || (selectedReview != null && !selectedReview.Passed))
            {
                codeStatus = WorkflowStatus.NeedsFix;
            }
            else if (task.State == TaskState.Review)
            {
                codeStatus = WorkflowStatus.Done;
            }
            else if (selectedLog != null)
            {
                codeStatus = selectedLog.State switch
                {
                    RunState.Running => WorkflowStatus.Running,
                    RunState.Success => WorkflowStatus.Done,
                    RunState.Error => WorkflowStatus.NeedsFix,
                    _ => WorkflowStatus.Pending
                };
            }
            else
            {
                codeStatus = WorkflowStatus.Pending;
            }

            var codeEvList = new List<string?>();
            if (selectedLog != null)
            {
                codeEvList.Add(selectedLog.LogPath);
            }
            if (selectedReview != null)
            {
                codeEvList.Add(selectedReview.Evidence);
            }
            if (task.State == TaskState.Merged && selectedLog == null && selectedReview == null)
            {
                codeEvList.Add($"\u0110\u00E3 merge: {task.Id}");
            }
            var codeEvidence = NormalizeEvidence(codeEvList);
            var codeStep = new WorkflowStep(WorkflowStage.Code, "Code", codeStatus, task.Owner ?? "", codeEvidence);

            // QC
            WorkflowStatus qcStatus;
            if (task.State == TaskState.Merged)
            {
                qcStatus = WorkflowStatus.Done;
            }
            else if (task.State == TaskState.NeedsFix)
            {
                qcStatus = WorkflowStatus.Pending;
            }
            else if (selectedReview != null)
            {
                qcStatus = selectedReview.Passed ? WorkflowStatus.Done : WorkflowStatus.NeedsFix;
            }
            else if (task.State == TaskState.Review)
            {
                qcStatus = WorkflowStatus.Running;
            }
            else
            {
                qcStatus = WorkflowStatus.Pending;
            }

            var qcEvList = new List<string?>();
            if (selectedReview != null)
            {
                qcEvList.Add(selectedReview.Evidence);
            }
            if (task.State == TaskState.Merged && selectedReview == null)
            {
                qcEvList.Add($"\u0110\u00E3 merge: {task.Id}");
            }
            var qcEvidence = NormalizeEvidence(qcEvList);
            var qcStep = new WorkflowStep(WorkflowStage.Quality, "QC", qcStatus, "Claude", qcEvidence);

            taskPrepSteps.Add(prepStep);
            taskCodeSteps.Add(codeStep);
            taskQcSteps.Add(qcStep);

            var taskSteps = new WorkflowStep[] { prepStep, codeStep, qcStep };
            taskProgressList.Add(new WorkflowTaskProgress(task.Id, taskSteps));
        }

        // 4. Chuan bi (Tong)
        var totalPrepStatus = AggregateStatus(taskPrepSteps);
        var totalPrepEvidence = NormalizeEvidence(taskPrepSteps.SelectMany(s => s.Evidence));
        var totalPrepStep = new WorkflowStep(WorkflowStage.Preparation, "Chu\u1EA9n b\u1ECB", totalPrepStatus, "Codex", totalPrepEvidence);

        // 5. Code (Tong)
        var totalCodeStatus = AggregateStatus(taskCodeSteps);
        var totalCodeEvidence = NormalizeEvidence(taskCodeSteps.SelectMany(s => s.Evidence));
        var distinctCodeOwners = tasks
            .Select(t => t.Owner)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(o => o, StringComparer.Ordinal);
        var totalCodeOwner = string.Join(", ", distinctCodeOwners);
        var totalCodeStep = new WorkflowStep(WorkflowStage.Code, "Code", totalCodeStatus, totalCodeOwner, totalCodeEvidence);

        // 6. QC (Tong)
        var totalQcStatus = AggregateStatus(taskQcSteps);
        var totalQcEvidence = NormalizeEvidence(taskQcSteps.SelectMany(s => s.Evidence));
        var totalQcStep = new WorkflowStep(WorkflowStage.Quality, "QC", totalQcStatus, "Claude", totalQcEvidence);

        // 7. Nghiem thu
        var lastAcceptDec = snapshot?.Decisions?.LastOrDefault(d => d.Stage == WorkflowStage.Acceptance);
        WorkflowStatus acceptStatus;
        if (lastAcceptDec == null || !lastAcceptDec.IsFinal)
        {
            acceptStatus = WorkflowStatus.Pending;
        }
        else
        {
            acceptStatus = lastAcceptDec.Approved ? WorkflowStatus.Done : WorkflowStatus.NeedsFix;
        }
        var acceptEvidence = NormalizeEvidence(new[] { lastAcceptDec?.Evidence });
        var acceptStep = new WorkflowStep(WorkflowStage.Acceptance, "Nghi\u1EC7m thu", acceptStatus, "Claude", acceptEvidence);

        var steps = new WorkflowStep[]
        {
            intakeStep,
            ideaStep,
            planStep,
            totalPrepStep,
            totalCodeStep,
            totalQcStep,
            acceptStep
        };

        return new WorkflowResult(steps, taskProgressList);
    }

    private static WorkflowStatus AggregateStatus(IReadOnlyList<WorkflowStep> taskStepsForStage)
    {
        if (taskStepsForStage.Count == 0)
        {
            return WorkflowStatus.Pending;
        }
        if (taskStepsForStage.All(s => s.Status == WorkflowStatus.Done))
        {
            return WorkflowStatus.Done;
        }
        if (taskStepsForStage.Any(s => s.Status == WorkflowStatus.NeedsFix))
        {
            return WorkflowStatus.NeedsFix;
        }
        if (taskStepsForStage.Any(s => s.Status != WorkflowStatus.Pending))
        {
            return WorkflowStatus.Running;
        }
        return WorkflowStatus.Pending;
    }

    private static IReadOnlyList<string> NormalizeEvidence(IEnumerable<string?> items)
    {
        return items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }
}
