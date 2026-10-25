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
    private readonly object _lock = new();
    private readonly List<ResearchJob> _jobs = new();

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
    {
        var map = _accounts.List().ToDictionary(v => v.Service.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<ResearchAiOption>(Ais.Length);
        foreach (var ai in Ais)
        {
            if (!map.TryGetValue(ai, out var v)) continue;
            var canAsk = v.Mode == AiMode.Automatic && _connectorFor(ai) != null;
            result.Add(new ResearchAiOption(ai, v.Service.Name, v.Mode, v.Status, v.StatusText, canAsk, v.Service.OfficialUrl));
        }
        return result;
    }

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
    {
        var req = string.IsNullOrEmpty(requestId) ? null : _requests.Get(requestId);
        if (req == null) throw new ArgumentException("Phiếu không hợp lệ.");

        var picked = Ais.Where(a => ais != null && ais.Any(x => string.Equals(x, a, StringComparison.OrdinalIgnoreCase))).ToList();
        if (picked.Count == 0) throw new ArgumentException("Chọn ít nhất 1 AI.");

        var (prompt, redacted) = ResearchPrompt.Build(req.Id, req.Title, question ?? "", includeRequestText ? req.Verbatim : null);
        var now = _now();
        var qid = ResearchStore.NewQuestionId(now);
        var opts = Options().ToDictionary(o => o.Ai, StringComparer.OrdinalIgnoreCase);
        var outcomes = new List<ResearchAiOutcome>(picked.Count);
        var qTrim = question!.Trim();

        foreach (var ai in picked)
        {
            if (!opts.TryGetValue(ai, out var opt) || opt.Mode == AiMode.Disabled)
            {
                outcomes.Add(new ResearchAiOutcome(ai, "skipped", "Đang tắt — bật ở trang Tài khoản AI"));
                continue;
            }
            if (!opt.CanAsk)
            {
                outcomes.Add(new ResearchAiOutcome(ai, "manual", "Copy lời nhắn, dán câu trả lời"));
                continue;
            }

            var conn = _connectorFor(ai);
            if (conn == null)
            {
                outcomes.Add(new ResearchAiOutcome(ai, "manual", "Copy lời nhắn, dán câu trả lời"));
                continue;
            }

            ResearchJob? startedJob = null;
            lock (_lock)
            {
                if (_jobs.Any(j => j.Ai == ai && j.Running))
                {
                    outcomes.Add(new ResearchAiOutcome(ai, "skipped", "Đang hỏi câu trước"));
                    continue;
                }
                var last = _jobs.Where(j => j.Ai == ai).OrderByDescending(j => j.StartedAt).FirstOrDefault();
                if (last != null && now - last.StartedAt < MinInterval)
                {
                    var n = Math.Max(1, (int)Math.Ceiling((MinInterval - (now - last.StartedAt)).TotalSeconds));
                    outcomes.Add(new ResearchAiOutcome(ai, "skipped", $"Đợi {n} giây nữa"));
                    continue;
                }
                startedJob = new ResearchJob(req.Id, qid, ai, now, true, null, null);
                _jobs.Add(startedJob);
                outcomes.Add(new ResearchAiOutcome(ai, "started", "Đang hỏi…"));
            }

            var jobRef = startedJob;
            _runInBackground(async () =>
            {
                ResearchJob finished;
                try
                {
                    var ans = await conn.AskAsync(prompt, CancellationToken.None);
                    if (ans.Ok)
                    {
                        var entry = _store.Save(req.Id, qid, ai, conn.Via, qTrim, ans.Text, null, _now());
                        finished = jobRef with { Running = false, FilePath = entry.FilePath, Error = null };
                    }
                    else
                    {
                        finished = jobRef with { Running = false, Error = string.IsNullOrEmpty(ans.Error) ? "Lỗi không xác định" : ans.Error };
                    }
                }
                catch (Exception ex)
                {
                    finished = jobRef with { Running = false, Error = "Lỗi: " + ex.Message };
                }
                lock (_lock)
                {
                    var idx = _jobs.IndexOf(jobRef);
                    if (idx >= 0) _jobs[idx] = finished;
                }
            });
        }

        return new ResearchAskResult(qid, prompt, redacted, outcomes);
    }

    /// <summary>
    /// Phiếu không có ⇒ ArgumentException("Phiếu không hợp lệ."); ai không thuộc Ais ⇒ ArgumentException("AI không hợp lệ.");
    /// answer rỗng (Trim) ⇒ ArgumentException("Cần nội dung câu trả lời."); answer dài hơn ResearchStore.MaxAnswer ⇒ ArgumentException("Câu trả lời quá dài (tối đa 20000 ký tự).");
    /// question rỗng ⇒ ArgumentException("Cần nội dung câu hỏi."); questionId sai mẫu ⇒ ArgumentException của ResearchStore bay ra.
    /// Lưu: _store.Save(req.Id, questionId, ai.ToLower(), "Thủ công (dán từ web)", question, answer, sources, now()).
    /// </summary>
    public ResearchEntry SaveManual(string? requestId, string? questionId, string? ai, string? question, string? answer, string? sources)
    {
        var req = string.IsNullOrEmpty(requestId) ? null : _requests.Get(requestId);
        if (req == null) throw new ArgumentException("Phiếu không hợp lệ.");

        var aiKey = (ai ?? "").Trim().ToLowerInvariant();
        if (!Ais.Contains(aiKey)) throw new ArgumentException("AI không hợp lệ.");

        var ansTrim = (answer ?? "").Trim();
        if (ansTrim.Length == 0) throw new ArgumentException("Cần nội dung câu trả lời.");
        if (ansTrim.Length > ResearchStore.MaxAnswer) throw new ArgumentException("Câu trả lời quá dài (tối đa 20000 ký tự).");
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("Cần nội dung câu hỏi.");

        return _store.Save(req.Id, questionId ?? "", aiKey, "Thủ công (dán từ web)", question, answer!, sources, _now());
    }

    public IReadOnlyList<ResearchJob> Jobs(string? requestId)
    {
        if (string.IsNullOrEmpty(requestId)) return Array.Empty<ResearchJob>();
        lock (_lock)
        {
            return _jobs
                .Where(j => string.Equals(j.RequestId, requestId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.StartedAt)
                .ToList();
        }
    }

    public IReadOnlyList<ResearchEntry> Entries(string? requestId)
        => _store.List(requestId ?? "");

    public int RunningCount()
    {
        lock (_lock) return _jobs.Count(j => j.Running);
    }

    /// <summary>
    /// Connector thật:
    ///   "chatgpt" ⇒ ProviderSettings.Load(AITeamRoot), provider id "codex" (không có ⇒ null) ⇒ new ProviderResearchConnector("chatgpt", "codex CLI (tài khoản ChatGPT)",
    ///     settings.Create(cfg, new ProcessRunner()), TimeSpan.FromMinutes(10), r => new ProviderStateStore(AITeamRoot).Record(r, DateTime.Now));
    ///   "gemini" ⇒ provider "agy" (Antigravity, model Gemini) qua ProviderResearchConnector, 10 phút (Gemini CLI không còn nhận tài khoản cá nhân miễn phí — 25/10/2026);
    ///   khác ⇒ null. Lỗi đọc cấu hình ⇒ null.
    /// </summary>
    public static Func<string, IResearchConnector?> DefaultConnectors(HubPaths paths) => ai =>
    {
        try
        {
            var key = (ai ?? "").ToLowerInvariant();
            if (key == "chatgpt")
            {
                var settings = ProviderSettings.Load(paths.AITeamRoot);
                var cfg = settings.Providers.FirstOrDefault(p => string.Equals(p.Id, "codex", StringComparison.OrdinalIgnoreCase));
                if (cfg == null) return null;
                return new ProviderResearchConnector("chatgpt", "codex CLI (tài khoản ChatGPT)",
                    settings.Create(cfg, new ProcessRunner()), TimeSpan.FromMinutes(10),
                    r => new ProviderStateStore(paths.AITeamRoot).Record(r, DateTime.Now));
            }
            if (key == "gemini")
            {
                // Gemini CLI không còn nhận tài khoản cá nhân miễn phí (IneligibleTierError, 25/10/2026) ⇒ hỏi qua agy (model Gemini), chế độ --sandbox.
                var settings = ProviderSettings.Load(paths.AITeamRoot);
                var cfg = settings.Providers.FirstOrDefault(p => string.Equals(p.Id, "agy", StringComparison.OrdinalIgnoreCase));
                if (cfg == null) return null;
                return new ProviderResearchConnector("gemini", "Antigravity (agy · model Gemini)",
                    settings.Create(cfg, new ProcessRunner()), TimeSpan.FromMinutes(10),
                    r => new ProviderStateStore(paths.AITeamRoot).Record(r, DateTime.Now),
                    "Antigravity", "đăng nhập lại ở trang Tài khoản Antigravity");
            }
            return null;
        }
        catch
        {
            return null;
        }
    };
}
