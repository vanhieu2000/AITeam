using AITeam.Core.Accounts;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Research;
using AITeam.Core.Routing;

namespace AITeam.Hub.Services;

// Phòng nghiên cứu trên Hub (R-005 / V5-07b). Thiết kế: docs/design/V5-VAN-PHONG-MINI.md mục C4 + G2.

/// <summary>Một AI trong phòng. CanAsk: chế độ Automatic VÀ có connector (chatgpt, gemini) ⇒ nút "Hỏi" chạy tự động; còn lại ⇒ thủ công (copy / dán).</summary>
public sealed record ResearchAiOption(string Ai, string Name, AiMode Mode, AiStatus Status, string StatusText, bool CanAsk, string OfficialUrl);

/// <summary>Một lần hỏi tự động. Running ⇒ chưa xong; xong: Error null ⇒ FilePath là file kết quả.</summary>
public sealed record ResearchJob(string RequestId, string QuestionId, string Ai, DateTime StartedAt, bool Running, string? Error, string? FilePath);

/// <summary>Kết quả bấm "Hỏi": lời nhắn đã dựng (để copy cho AI thủ công) + kết cục từng AI đã chọn.</summary>
public sealed record ResearchAskResult(string QuestionId, string Prompt, int Redacted, IReadOnlyList<ResearchAiOutcome> Outcomes);

/// <summary>Kind: "started" (đang hỏi tự động) · "manual" (copy lời nhắn, dán kết quả) · "skipped" (Text = lý do).</summary>
public sealed record ResearchAiOutcome(string Ai, string Kind, string Text);

public interface IResearchService
{
    /// <summary>Đúng 4 AI theo thứ tự: chatgpt, gemini, deepseek, grok — trạng thái lấy từ IAiAccountsService.List().</summary>
    IReadOnlyList<ResearchAiOption> Options();

    /// <summary>Xem ResearchService.Ask.</summary>
    ResearchAskResult Ask(string? requestId, string? question, IReadOnlyList<string>? ais, bool includeRequestText);

    /// <summary>Lưu câu trả lời anh dán (AI thủ công). Xem ResearchService.SaveManual.</summary>
    ResearchEntry SaveManual(string? requestId, string? questionId, string? ai, string? question, string? answer, string? sources);

    /// <summary>Các lần hỏi tự động của phiếu từ lúc mở Hub, mới nhất trước.</summary>
    IReadOnlyList<ResearchJob> Jobs(string? requestId);

    /// <summary>ResearchStore.List(requestId); requestId sai ⇒ rỗng.</summary>
    IReadOnlyList<ResearchEntry> Entries(string? requestId);

    /// <summary>Số lần hỏi tự động đang chạy (mọi phiếu) — cho bàn "Phòng nghiên cứu" ở Văn phòng.</summary>
    int RunningCount();
}

public sealed class ResearchService : IResearchService
{
    public static readonly string[] Ais = ["chatgpt", "gemini", "deepseek", "grok"];
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(60);

    private readonly IRequestService _requests;
    private readonly IAiAccountsService _accounts;
    private readonly ResearchStore _store;
    private readonly Func<string, IResearchConnector?> _connectorFor;
    private readonly Func<DateTime> _now;
    private readonly Action<Func<Task>> _runInBackground;

    /// <param name="connectorFor">ai ⇒ connector tự động (null ⇒ không có). null ⇒ DefaultConnectors(paths).</param>
    /// <param name="runInBackground">chạy việc nền; null ⇒ f => _ = Task.Run(f). Test truyền hàm chạy đồng bộ.</param>
    public ResearchService(HubPaths paths, IRequestService requests, IAiAccountsService accounts,
        Func<string, IResearchConnector?>? connectorFor = null, Func<DateTime>? now = null, Action<Func<Task>>? runInBackground = null)
    {
        _requests = requests;
        _accounts = accounts;
        _store = new ResearchStore(paths.AITeamRoot);
        _connectorFor = connectorFor ?? DefaultConnectors(paths);
        _now = now ?? (() => DateTime.Now);
        _runInBackground = runInBackground ?? (f => _ = Task.Run(f));
    }

    /// <summary>
    /// Mỗi ai trong Ais: dòng AiAccountView tương ứng (không có ⇒ bỏ qua); CanAsk = Mode Automatic &amp;&amp; _connectorFor(ai) != null.
    /// </summary>
    public IReadOnlyList<ResearchAiOption> Options()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// 1. Phiếu: _requests.Get(requestId) null ⇒ ArgumentException("Phiếu không hợp lệ.").
    /// 2. ais: chỉ giữ id có trong Ais (không phân biệt hoa thường, bỏ trùng, theo thứ tự Ais); rỗng ⇒ ArgumentException("Chọn ít nhất 1 AI.").
    /// 3. ResearchPrompt.Build(req.Id, req.Title, question, includeRequestText ? req.Verbatim : null) — lỗi ArgumentException bay ra nguyên vẹn.
    /// 4. QuestionId = ResearchStore.NewQuestionId(now).
    /// 5. Từng ai (khóa lock cho bước kiểm tra + thêm job):
    ///    - Option Mode Disabled ⇒ skipped "Đang tắt — bật ở trang Tài khoản AI".
    ///    - !CanAsk ⇒ manual "Copy lời nhắn, dán câu trả lời".
    ///    - đang có job Running của ai này ⇒ skipped "Đang hỏi câu trước".
    ///    - job gần nhất của ai này bắt đầu chưa đủ MinInterval ⇒ skipped "Đợi {n} giây nữa" (n = số giây còn lại, làm tròn LÊN).
    ///    - ngược lại: thêm job Running, outcome started "Đang hỏi…", rồi _runInBackground(async () => {
    ///        ans = await connector.AskAsync(prompt, CancellationToken.None) (ném lỗi ⇒ coi như Error = "Lỗi: " + ex.Message);
    ///        Ok ⇒ _store.Save(req.Id, qid, ai, connector.Via, question.Trim(), ans.Text, null, now()) ⇒ job xong, FilePath;
    ///        !Ok ⇒ job xong, Error = ans.Error }).
    /// Trả ResearchAskResult(qid, prompt, redacted, outcomes).
    /// </summary>
    public ResearchAskResult Ask(string? requestId, string? question, IReadOnlyList<string>? ais, bool includeRequestText)
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Phiếu không có ⇒ ArgumentException("Phiếu không hợp lệ."); ai không thuộc Ais ⇒ ArgumentException("AI không hợp lệ.");
    /// answer rỗng (Trim) ⇒ ArgumentException("Cần nội dung câu trả lời."); answer dài hơn ResearchStore.MaxAnswer ⇒ ArgumentException("Câu trả lời quá dài (tối đa 20000 ký tự).");
    /// question rỗng ⇒ ArgumentException("Cần nội dung câu hỏi."); questionId sai mẫu ⇒ ArgumentException của ResearchStore bay ra.
    /// Lưu: _store.Save(req.Id, questionId, ai.ToLower(), "Thủ công (dán từ web)", question, answer, sources, now()).
    /// </summary>
    public ResearchEntry SaveManual(string? requestId, string? questionId, string? ai, string? question, string? answer, string? sources)
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<ResearchJob> Jobs(string? requestId)
        => throw new NotImplementedException(); // TODO(agent)

    public IReadOnlyList<ResearchEntry> Entries(string? requestId)
        => throw new NotImplementedException(); // TODO(agent)

    public int RunningCount()
        => throw new NotImplementedException(); // TODO(agent)

    /// <summary>
    /// Connector thật:
    ///   "chatgpt" ⇒ ProviderSettings.Load(AITeamRoot), provider id "codex" (không có ⇒ null) ⇒ new ProviderResearchConnector("chatgpt", "codex CLI (tài khoản ChatGPT)",
    ///     settings.Create(cfg, new ProcessRunner()), TimeSpan.FromMinutes(10), r => new ProviderStateStore(AITeamRoot).Record(r, DateTime.Now));
    ///   "gemini" ⇒ exe = CliProbes.FindOnPath("gemini", PATH) (null ⇒ null) ⇒ new GeminiResearchConnector(exe, new ProcessRunner(), TimeSpan.FromMinutes(5));
    ///   khác ⇒ null. Lỗi đọc cấu hình ⇒ null.
    /// </summary>
    public static Func<string, IResearchConnector?> DefaultConnectors(HubPaths paths)
        => throw new NotImplementedException(); // TODO(agent)
}
