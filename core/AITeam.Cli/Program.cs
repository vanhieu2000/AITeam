using System.Text;
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
    var runner = new ProcessRunner();
    Console.WriteLine("== Provider ==");
    int bad = 0;
    foreach (var c in settings.Providers)
    {
        if (!c.Enabled) { Console.WriteLine($"  [TẮT] {c.Id}"); continue; }
        var h = await settings.Create(c, runner).CheckAsync(CancellationToken.None);
        var tag = !h.Installed ? "CHƯA CÀI" : h.LoggedIn == true ? "OK" : h.LoggedIn == false ? "CHƯA ĐĂNG NHẬP" : "KHÔNG RÕ";
        if (tag != "OK") bad++;
        Console.WriteLine($"  [{tag}] {c.Id} ({c.Kind}) · vai trò: {string.Join(", ", c.Roles)} · tối đa {c.MaxConcurrent} job · model: {c.Model ?? "mặc định"}");
        if (tag != "OK") Console.WriteLine($"         {h.Detail.Split('\n')[0]}");
    }
    Console.WriteLine("== Routing (thứ tự ưu tiên) ==");
    foreach (var (role, ids) in settings.Routing) Console.WriteLine($"  {role}: {string.Join(" → ", ids)}");
    return bad == 0 ? 0 : 1;
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
    Console.WriteLine("aiteam providers [--root D:\\AITeam]      Kiểm tra từng provider (cài chưa, đăng nhập chưa) + thứ tự routing");
    Console.WriteLine("aiteam task-info --file <task.md>        Đọc thông tin chuẩn hóa của 1 file task");
    return 0;
}

string? Arg(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRoot()
{
    // aiteam.exe nằm trong <AITeam>\core\AITeam.Cli\bin\... ⇒ đi lên tới thư mục có team.json.
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "team.json"))) dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
