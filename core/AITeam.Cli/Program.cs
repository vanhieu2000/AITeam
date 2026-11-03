using System.Text;
using System.Text.Json;
using AITeam.Core.Review;
using AITeam.Core.Routing;
using AITeam.Core.Process;
using AITeam.Core.Providers;
using AITeam.Core.Tasks;

Console.OutputEncoding = Encoding.UTF8;
var root = Arg("--root") ?? FindRoot();
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "help";

try
{
    return command switch
    {
        "providers" => await Providers(),
        "task-info" => TaskInfo(),
        "check-harness-change" => CheckHarnessChange(),
        "review" => await Review(),
        "review-rounds" => ReviewRounds(),
        "run" => await Run(),
        "diagnose-run" => DiagnoseRun(),
        "opencode-check" => await OpenCodeCheck(),
        "mcp" => await AITeam.Cli.McpServerCommand.RunAsync(args),
        _ => Help(),
    };
}
catch (NotImplementedException)
{
    Console.Error.WriteLine("LỖI: chưa làm");
    return 1;
}
catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"LỖI cấu hình: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"LỖI: {ex.Message}");
    return 1;
}

int CheckHarnessChange()
{
    var file = Arg("--diff-file") ?? throw new InvalidDataException("Thiếu --diff-file");
    if (new FileInfo(file).Length > 10 * 1024 * 1024) throw new InvalidDataException("Diff harness quá lớn.");
    var allowed = AcceptanceHarnessPolicy.Allows(File.ReadAllText(file), args.Contains("--allow"));
    Console.WriteLine(allowed ? "Chấp nhận thay harness đã khai báo; Claude vẫn cần review phạm vi dựng môi trường." : "Chặn: chưa khai báo hoặc sửa assert/điều kiện chọn test.");
    return allowed ? 0 : 2;
}

int DiagnoseRun()
{
    var file = Arg("--input") ?? throw new FileNotFoundException("Thiếu --input.");
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };
    var current = JsonSerializer.Deserialize<AITeam.Core.Metrics.RunDiagnosticInput>(File.ReadAllText(file), options)
        ?? throw new InvalidDataException("Số đo lượt không hợp lệ.");
    var history = new List<AITeam.Core.Metrics.RunDiagnosticInput>();
    var directory = Arg("--history");
    if (directory != null && Directory.Exists(directory))
        foreach (var item in Directory.EnumerateFiles(directory, "*.json"))
        {
            try { var row = JsonSerializer.Deserialize<AITeam.Core.Metrics.RunDiagnosticInput>(File.ReadAllText(item), options); if (row != null) history.Add(row); }
            catch (JsonException) { /* Không coi file số đo hỏng là lượt đo 0. */ }
        }
    var previous = history.Where(r => r.Project == current.Project && r.Task == current.Task && r.Run == current.Run - 1).LastOrDefault();
    var peers = history.Where(r => current.Difficulty is "S" or "M" or "L" && r.Difficulty == current.Difficulty && r.KnownTokens).ToList();
    double? average = peers.Count == 0 ? null : peers.Average(r => (double)r.TotalTokens!.Value);
    Console.WriteLine(JsonSerializer.Serialize(AITeam.Core.Metrics.RunDiagnostics.Evaluate(current, previous, average), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    return 0;
}

async Task<int> OpenCodeCheck()
{
    var provider = new OpenCodeProvider(OpenCodeProvider.Configuration(root, false), new ProcessRunner());
    var health = await provider.CheckAsync(CancellationToken.None);
    Console.WriteLine(JsonSerializer.Serialize(health, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    return health.Installed ? 0 : 1;
}

async Task<int> Providers()
{
    var settings = ProviderSettings.Load(root);
    var state = new ProviderStateStore(root);
    var now = DateTime.Now;
    bool noCheck = args.Contains("--no-check");
    var runner = new ProcessRunner();
    Console.WriteLine("== Provider ==");
    int bad = 0;
    foreach (var c in settings.Providers)
    {
        if (!c.Enabled) { Console.WriteLine($"  [TẮT] {c.Id}"); continue; }
        var rest = state.RestOf(c.Id, now);
        string tag;
        string? checkDetail = null;
        if (noCheck)
        {
            tag = rest != null ? $"NGHỈ tới {rest.Until.ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture)}" : "SẴN SÀNG";
        }
        else
        {
            var h = await settings.Create(c, runner).CheckAsync(CancellationToken.None);
            tag = !h.Installed ? "CHƯA CÀI" : h.LoggedIn == true ? "OK" : h.LoggedIn == false ? "CHƯA ĐĂNG NHẬP" : "KHÔNG RÕ";
            if (tag != "OK") { bad++; checkDetail = h.Detail; }
        }
        Console.WriteLine($"  [{tag}] {c.Id} ({c.Kind}) · vai trò: {string.Join(", ", c.Roles)} · tối đa {c.MaxConcurrent} job · model: {c.Model ?? "mặc định"}");
        if (checkDetail != null) Console.WriteLine($"         {checkDetail.Split('\n')[0]}");
        if (rest != null) Console.WriteLine($"         {rest.Reason}: {(rest.Detail ?? "").Split('\n')[0]}");
    }
    Console.WriteLine("== Routing (thứ tự ưu tiên) ==");
    foreach (var (role, ids) in settings.Routing) Console.WriteLine($"  {role}: {string.Join(" → ", ids)}");
    return noCheck || bad == 0 ? 0 : 1;
}

int TaskInfo()
{
    var file = Arg("--file") ?? throw new FileNotFoundException("Thiếu --file <đường dẫn task.md>");
    var t = TaskFile.Parse(file, File.ReadAllText(file));
    Console.WriteLine($"{t.Id} · {t.Title}");
    Console.WriteLine($"  Agent: {t.Agent} · Nhánh: {t.Branch} · Yêu cầu: {t.Request ?? "-"} · Ngân sách: {t.BudgetLines?.ToString() ?? "-"} dòng · Tối đa: {t.MaxMinutes?.ToString() ?? "-"} phút · Model: {t.Model ?? "mặc định"}");
    Console.WriteLine($"  Mục: {string.Join(" | ", t.Sections.Keys)}");
    return 0;
}

/// <summary>
/// Lệnh review-rounds --task-file &lt;f&gt; --logs &lt;d&gt; --task &lt;T&gt;.
/// Thiếu/giá trị trắng của bất kỳ tham số bắt buộc nào (kiểm tra trước mã task):
/// stderr "LỖI: thiếu tham số bắt buộc (--task-file, --logs, --task).", stdout rỗng, trả 2.
/// T chỉ gồm [A-Za-z0-9][A-Za-z0-9_-]{0,63}, khớp toàn chuỗi (kể cả từ chối LF cuối):
/// sai thì stderr "LỖI: mã task không hợp lệ.", stdout rỗng, trả 2 trước khi đọc nguồn.
/// Đọc UTF-8 có/không BOM. Chỉ file trực tiếp trong logs có tên bắt đầu bằng
/// T + ".review-" và kết thúc ".json" (so sánh Ordinal); không duyệt thư mục con,
/// không theo file/thư mục reparse point. Không dùng T làm mẫu wildcard hay ghép lệnh.
/// Nguồn thiếu, không đọc được, đường dẫn hỏng: riêng nguồn đó đóng góp 0, vẫn đọc nguồn khác.
/// Gọi ReviewRoundCounter.Count; stdout duy nhất số nguyên thập phân + xuống dòng,
/// stderr rỗng, trả 0. Không đọc cấu hình, ghi file hoặc gọi agy/codex.
/// </summary>
int ReviewRounds()
{
    var taskFile = Arg("--task-file");
    var logsDir = Arg("--logs");
    var taskId = Arg("--task");

    if (string.IsNullOrWhiteSpace(taskFile) ||
        string.IsNullOrWhiteSpace(logsDir) ||
        string.IsNullOrWhiteSpace(taskId))
    {
        Console.Error.WriteLine("LỖI: thiếu tham số bắt buộc (--task-file, --logs, --task).");
        return 2;
    }

    if (!IsValidTaskId(taskId))
    {
        Console.Error.WriteLine("LỖI: mã task không hợp lệ.");
        return 2;
    }

    string? taskContent = null;
    try
    {
        if (File.Exists(taskFile))
        {
            taskContent = File.ReadAllText(taskFile, Encoding.UTF8);
        }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
    {
        taskContent = null;
    }

    var logsList = new List<string?>();
    try
    {
        var dirInfo = new DirectoryInfo(logsDir);
        if (dirInfo.Exists && !HasReparsePointInPath(dirInfo))
        {
            string prefix = taskId + ".review-";
            string suffix = ".json";

            foreach (var file in dirInfo.EnumerateFiles())
            {
                if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;

                string name = file.Name;
                if (name.StartsWith(prefix, StringComparison.Ordinal) &&
                    name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    try
                    {
                        string content = File.ReadAllText(file.FullName, Encoding.UTF8);
                        logsList.Add(content);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                    {
                        logsList.Add(null);
                    }
                }
            }
        }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
    {
        // thiếu/không liệt kê được logs → logs đóng góp 0
    }

    int count = ReviewRoundCounter.Count(taskContent, logsList);
    Console.WriteLine(count);
    return 0;

    static bool HasReparsePointInPath(DirectoryInfo dir)
    {
        try
        {
            var current = new DirectoryInfo(dir.FullName);
            while (current != null)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    return true;
                current = current.Parent;
            }
        }
        catch
        {
            return true;
        }
        return false;
    }

    static bool IsValidTaskId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
        char first = id[0];
        if (!((first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z') || (first >= '0' && first <= '9')))
            return false;
        for (int i = 1; i < id.Length; i++)
        {
            char c = id[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-'))
                return false;
        }
        return true;
    }
}
int Help()
{
    Console.WriteLine("aiteam providers [--no-check] [--root D:\\AITeam]  Kiểm tra từng provider (cài chưa, đăng nhập chưa) + thứ tự routing");
    Console.WriteLine("aiteam task-info --file <task.md>        Đọc thông tin chuẩn hóa của 1 file task");
    Console.WriteLine("aiteam review --task-file <f> --diff-file <f> [--verify-file <f>] --workdir <d> --logdir <d> --dev <id> [--timeout-min 15] [--out <f>] [--auto]");
    Console.WriteLine("aiteam run --provider <id> --workdir <d> --prompt-file <f> --logdir <d> [--timeout-min 30] [--model <m>] [--out <f>] [--read-only]");
    return 0;
}

async Task<int> Review()
{
    var taskFile = Arg("--task-file");
    var diffFile = Arg("--diff-file");
    var verifyFile = Arg("--verify-file");
    var workDir = Arg("--workdir");
    var logDir = Arg("--logdir");
    var dev = Arg("--dev");
    var outFile = Arg("--out");
    if (string.IsNullOrWhiteSpace(taskFile) || string.IsNullOrWhiteSpace(diffFile) ||
        string.IsNullOrWhiteSpace(workDir) || string.IsNullOrWhiteSpace(logDir) || string.IsNullOrWhiteSpace(dev))
    {
        Console.Error.WriteLine("LỖI: thiếu tham số bắt buộc (--task-file, --diff-file, --workdir, --logdir, --dev).");
        return 2;
    }

    var utf8NoBom = new UTF8Encoding(false);
    if (args.Contains("--auto") && File.Exists(taskFile))
    {
        var decision = ReviewPolicy.Decide(await File.ReadAllTextAsync(taskFile));
        if (!decision.AutoReview)
        {
            var summary = $"SKIP: {decision.Reason}";
            Console.WriteLine(summary);
            if (!string.IsNullOrWhiteSpace(outFile))
            {
                if (Path.GetDirectoryName(outFile) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
                var skipJson = JsonSerializer.Serialize(new
                {
                    exitCode = 4,
                    verdict = "SKIPPED",
                    summary,
                    notes = Array.Empty<string>(),
                    feedback = "",
                    provider = (string?)null,
                    diffHash = (string?)null,
                });
                await File.WriteAllTextAsync(outFile, skipJson, utf8NoBom);
            }
            return 4;
        }
    }

    var timeoutMin = int.TryParse(Arg("--timeout-min"), out var tm) && tm > 0 ? tm : 15;
    var settings = ProviderSettings.Load(root);
    var state = new ProviderStateStore(root);
    var router = new ProviderRouter(settings, state);
    var proc = new ProcessRunner();
    var runner = new ReviewRunner(settings, router, state, c => settings.Create(c, proc), () => DateTime.Now);
    var input = CreateReviewInput(taskFile, diffFile, verifyFile, workDir, logDir, dev, timeoutMin, root);
    var outcome = await runner.RunAsync(input, CancellationToken.None);

    Console.WriteLine(outcome.Summary);
    foreach (var note in outcome.Notes)
        Console.WriteLine($"  - {note}");
    if (outcome.Result != null && ReviewRunner.Feedback(outcome.Result) is { Length: > 0 } fb)
        Console.WriteLine(fb);

    if (!string.IsNullOrWhiteSpace(outFile))
    {
        if (Path.GetDirectoryName(outFile) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outFile, ReviewRunner.OutcomeJson(outcome), utf8NoBom);
    }
    return outcome.ExitCode;
}

async Task<int> Run()
{
    var provider = Arg("--provider");
    var workDir = Arg("--workdir");
    var promptFile = Arg("--prompt-file");
    var logDir = Arg("--logdir");
    var model = Arg("--model");
    var outFile = Arg("--out");
    if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(workDir) ||
        string.IsNullOrWhiteSpace(promptFile) || string.IsNullOrWhiteSpace(logDir))
    {
        Console.Error.WriteLine("LỖI: thiếu tham số bắt buộc (--provider, --workdir, --prompt-file, --logdir).");
        return 2;
    }

    var timeoutMin = int.TryParse(Arg("--timeout-min"), out var tm) && tm > 0 ? tm : 30;
    var settings = ProviderSettings.Load(root);
    var state = new ProviderStateStore(root);
    var proc = new ProcessRunner();
    if (string.Equals(provider, "opencode", StringComparison.OrdinalIgnoreCase))
    {
        var openCode = OpenCodeProvider.Configuration(root, args.Contains("--public-repo"), settings.Providers.FirstOrDefault(p => p.Id.Equals("opencode", StringComparison.OrdinalIgnoreCase)));
        settings = settings with { Providers = settings.Providers.Where(p => !p.Id.Equals("opencode", StringComparison.OrdinalIgnoreCase)).Append(openCode).ToList() };
    }
    var runner = new DevRunner(settings, state, c => settings.Create(c, proc), () => DateTime.Now);
    var mode = args.Contains("--read-only") ? RunMode.ReadOnly : RunMode.Develop;   // R-006: lập kế hoạch chỉ đọc
    var input = new DevRunInput(provider, workDir, promptFile, logDir, TimeSpan.FromMinutes(timeoutMin), model, mode) { Title = Arg("--title") };
    var outcome = await runner.RunAsync(input, CancellationToken.None);

    Console.WriteLine(outcome.Summary);
    if (!string.IsNullOrWhiteSpace(outFile))
    {
        if (Path.GetDirectoryName(outFile) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outFile, DevRunner.OutcomeJson(outcome), new UTF8Encoding(false));
    }
    return outcome.ExitCode;
}

string? Arg(string name)
{
    var i = Array.IndexOf(args, name);
    // Giá trị không được là tùy chọn khác ("--dev --auto" ⇒ thiếu giá trị, KHÔNG phải dev="--auto") — Codex review V4-08 phát hiện.
    return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[i + 1] : null;
}

static string FindRoot()
{
    // aiteam.exe nằm trong <AITeam>\core\AITeam.Cli\bin\... ⇒ đi lên tới thư mục có team.json.
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.json"))) dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}

/// <summary>
/// Tạo đầu vào với Root lấy từ --root/FindRoot, cho review trực tiếp lẫn --auto.
/// Giữ nguyên đường dẫn, developer; Timeout theo phút; không dò stack tại CLI.
/// Khung chưa làm được Main bắt: stderr "LỖI: chưa làm", mã 1.
/// </summary>
static ReviewRunInput CreateReviewInput(string taskFile, string diffFile, string? verifyFile,
    string workDir, string logDir, string dev, int timeoutMin, string root)
{
    return new ReviewRunInput(taskFile, diffFile, verifyFile, workDir, logDir, dev, TimeSpan.FromMinutes(timeoutMin), root);
}
