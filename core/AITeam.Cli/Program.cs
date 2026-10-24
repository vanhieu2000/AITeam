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
        "review" => await Review(),
        "run" => await Run(),
        "mcp" => await AITeam.Cli.McpServerCommand.RunAsync(args),
        _ => Help(),
    };
}
catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"LỖI cấu hình: {ex.Message}");
    return 2;
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

int Help()
{
    Console.WriteLine("aiteam providers [--no-check] [--root D:\\AITeam]  Kiểm tra từng provider (cài chưa, đăng nhập chưa) + thứ tự routing");
    Console.WriteLine("aiteam task-info --file <task.md>        Đọc thông tin chuẩn hóa của 1 file task");
    Console.WriteLine("aiteam review --task-file <f> --diff-file <f> [--verify-file <f>] --workdir <d> --logdir <d> --dev <id> [--timeout-min 15] [--out <f>] [--auto]");
    Console.WriteLine("aiteam run --provider <id> --workdir <d> --prompt-file <f> --logdir <d> [--timeout-min 30] [--model <m>] [--out <f>]");
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
    var input = new ReviewRunInput(taskFile, diffFile, verifyFile, workDir, logDir, dev, TimeSpan.FromMinutes(timeoutMin));
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
    var runner = new DevRunner(settings, state, c => settings.Create(c, proc), () => DateTime.Now);
    var input = new DevRunInput(provider, workDir, promptFile, logDir, TimeSpan.FromMinutes(timeoutMin), model);
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
