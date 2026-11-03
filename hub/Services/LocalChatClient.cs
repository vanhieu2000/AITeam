using System.Text;
using System.Text.Json;
using AITeam.Core.Process;
using AITeam.Core.Research;

namespace AITeam.Hub.Services;

public sealed record ChatAnswer(string Text, string? Error, long? Tokens);
public sealed record ChatConnection(bool Installed, bool LoggedIn, string Detail);
public interface ILocalChatClient
{
    Task<ChatAnswer> ReplyAsync(ChatSettings settings, IReadOnlyList<ChatMessage> context, CancellationToken ct);
    Task<ChatConnection> CheckAsync(ChatSettings settings, CancellationToken ct);
}

/// <summary>Dùng đăng nhập ChatGPT chính thức của Codex; không đọc/copy token, không nhúng website hoặc gọi API web nội bộ.</summary>
public sealed class CodexChatClient(IProcessRunner runner) : ILocalChatClient
{
    internal static (string Exe, List<string> Prefix) Launch(ChatSettings settings)
    {
        settings.Validate();
        var path = settings.CodexPath;
        if (string.IsNullOrEmpty(path))
        {
            var npm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "@openai", "codex", "bin", "codex.js");
            path = OperatingSystem.IsWindows() && File.Exists(npm) ? npm : "codex";
        }
        return path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? ("node", [path]) : (path, []);
    }

    internal static IReadOnlyList<string> Arguments(ChatSettings settings, string directory, string output)
    {
        var (_, args) = Launch(settings);
        args.AddRange(["exec", "--json", "--ephemeral", "--skip-git-repo-check", "-C", directory,
            "--sandbox", "read-only", "-c", "approval_policy=\"never\"", "-c", "notify=[]", "-c", "mcp_servers={}",
            "-c", "features.shell_tool=false", "-c", "features.unified_exec=false", "-c", "features.apps=false", "-c", "web_search=\"disabled\"",
            "-c", "windows.sandbox=\"unelevated\""]);
        if (settings.BaseUrl.Length > 0) args.AddRange(["-c", "openai_base_url=" + JsonSerializer.Serialize(settings.BaseUrl)]);
        if (settings.Model.Length > 0) args.AddRange(["-m", settings.Model]);
        args.AddRange(["-o", output, "-"]);
        return args;
    }

    public async Task<ChatAnswer> ReplyAsync(ChatSettings settings, IReadOnlyList<ChatMessage> context, CancellationToken ct)
    {
        var dir = Path.Combine(Path.GetTempPath(), "aiteam-chat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            var prompt = Path.Combine(dir, "prompt.txt"); var output = Path.Combine(dir, "answer.txt");
            var history = JsonSerializer.Serialize(context.Select(m => new { role = m.Role, content = m.Text }));
            var text = "Bạn đang trò chuyện trực tiếp với chủ dự án trong AITeam Hub. Đây là hội thoại, không phải tác vụ code. "
                + "Không chạy lệnh, không đọc hoặc sửa file, không gọi công cụ. Không tự nhận đã giao việc/merge. "
                + "Hãy chỉ trả lời câu hỏi cuối, dùng lịch sử dưới đây để giữ ngữ cảnh. "
                + "Lịch sử cũ có thể được rút gọn; không tự bịa thông tin đã mất.\n"
                + settings.Instructions + "\nLịch sử hội thoại (JSON):\n" + history;
            await File.WriteAllTextAsync(prompt, SecretRedactor.Redact(text).Text, new UTF8Encoding(false), ct);
            var (exe, _) = Launch(settings);
            var result = await runner.RunAsync(new ProcessSpec(exe, Arguments(settings, dir, output), dir, prompt,
                TimeSpan.FromSeconds(settings.TimeoutSeconds)), ct);
            if (result.TimedOut) return new("", "AI chưa trả lời trong thời gian cho phép. Anh có thể gửi lại hoặc tăng thời gian chờ.", null);
            return Parse(result, File.Exists(output) && new FileInfo(output).Length <= 256000
                ? await File.ReadAllTextAsync(output, ct) : "");
        }
        catch (System.ComponentModel.Win32Exception) { return new("", "Chưa chạy được Codex CLI. Kiểm tra cài đặt và đường dẫn tại Cài đặt chat.", null); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    internal static ChatAnswer Parse(ProcessResult result, string text)
    {
        long? tokens = null; var completed = false; var failed = false; var errors = new StringBuilder();
        foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line); var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var type)) continue;
                if (type.GetString() is "turn.failed" or "error") { failed = true; errors.AppendLine(line); }
                if (type.GetString() == "turn.completed")
                {
                    completed = true;
                    if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("input_tokens", out var input)
                        && usage.TryGetProperty("output_tokens", out var output) && input.TryGetInt64(out var i) && output.TryGetInt64(out var o)
                        && i >= 0 && o >= 0 && i <= long.MaxValue - o) tokens = i + o;
                }
            }
            catch (JsonException) { /* stdout CLI còn có dòng thông báo không phải JSON */ }
        }
        if (result.ExitCode == 0 && completed && !failed && !string.IsNullOrWhiteSpace(text)) return new(text.Trim(), null, tokens);
        var error = errors + result.StdErr;
        if (error.Contains("403", StringComparison.OrdinalIgnoreCase) && error.Contains("proxy", StringComparison.OrdinalIgnoreCase))
            return new("", "Proxy/mạng đang chặn kết nối ChatGPT (403). Cho phép chatgpt.com trong cấu hình mạng rồi thử lại.", tokens);
        if (error.Contains("429", StringComparison.OrdinalIgnoreCase) || error.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || error.Contains("rate limit", StringComparison.OrdinalIgnoreCase)) return new("", "Tài khoản đang hết hạn mức. Kiểm tra hạn mức ChatGPT trước khi gửi lại.", tokens);
        if (error.Contains("login", StringComparison.OrdinalIgnoreCase) || error.Contains("401", StringComparison.OrdinalIgnoreCase)
            || error.Contains("auth", StringComparison.OrdinalIgnoreCase)) return new("", "Chưa đăng nhập hoặc phiên đã hết hạn. Chạy codex login trên máy này rồi bấm Kiểm tra kết nối.", tokens);
        if (error.Contains("model", StringComparison.OrdinalIgnoreCase)) return new("", "Model chưa được tài khoản hỗ trợ. Để trống model để dùng mặc định CLI hoặc chọn model hợp lệ.", tokens);
        return new("", "Chưa nhận được phản hồi hợp lệ từ Codex. Kiểm tra kết nối, phiên bản CLI và model; nội dung đã gửi vẫn được lưu.", tokens);
    }

    public async Task<ChatConnection> CheckAsync(ChatSettings settings, CancellationToken ct)
    {
        var (exe, prefix) = Launch(settings);
        try
        {
            var version = await runner.RunAsync(new ProcessSpec(exe, [.. prefix, "--version"], Path.GetTempPath(), null, TimeSpan.FromSeconds(15)), ct);
            if (version.ExitCode != 0 || version.TimedOut) return new(false, false, "Không đọc được phiên bản Codex CLI.");
            var login = await runner.RunAsync(new ProcessSpec(exe, [.. prefix, "login", "status"], Path.GetTempPath(), null, TimeSpan.FromSeconds(20)), ct);
            var loggedIn = login.ExitCode == 0 && !login.TimedOut && (login.StdOut + login.StdErr).Contains("ChatGPT", StringComparison.OrdinalIgnoreCase);
            // Không in stdout login: CLI có thể hiển thị thông tin tài khoản.
            var safeVersion = SecretRedactor.Redact(version.StdOut.Trim()).Text;
            return new(true, loggedIn, $"{safeVersion[..Math.Min(100, safeVersion.Length)]} · " + (loggedIn ? "Đã đăng nhập" : "Cần chạy codex login trên máy này"));
        }
        catch (System.ComponentModel.Win32Exception) { return new(false, false, "Chưa tìm thấy Codex CLI. Cài CLI rồi chạy codex login."); }
    }
}

public sealed class DemoChatClient : ILocalChatClient
{
    public async Task<ChatAnswer> ReplyAsync(ChatSettings settings, IReadOnlyList<ChatMessage> context, CancellationToken ct)
    {
        await Task.Delay(350, ct);
        return new("Đây là phản hồi minh họa, chưa gọi ChatGPT thật. Tôi đã nhận: “" + context[^1].Text
            + "”\n\nHội thoại hiện có " + context.Count(m => m.Role == "user") + " câu hỏi trong ngữ cảnh. Tắt HUB_FAKE và đăng nhập Codex CLI để trò chuyện thật.", null, null);
    }
    public Task<ChatConnection> CheckAsync(ChatSettings settings, CancellationToken ct) => Task.FromResult(new ChatConnection(false, false, "Chế độ minh họa · không chạy CLI hoặc ghi dữ liệu thật."));
}

public sealed class LocalChatWorker(LocalChatStore store, ILocalChatClient client, ILogger<LocalChatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var turn = store.Claim(); if (turn == null) continue;
                using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var reply = Reply(turn, cancelled.Token);
                while (!reply.IsCompleted)
                {
                    await Task.WhenAny(reply, Task.Delay(250, stoppingToken));
                    if (stoppingToken.IsCancellationRequested || !store.IsGenerating(turn.MessageId)) { cancelled.Cancel(); break; }
                }
                try
                {
                    var answer = await reply;
                    store.Finish(turn.MessageId, answer.Text, answer.Error, answer.Tokens);
                }
                catch (OperationCanceledException) { store.Finish(turn.MessageId, "", "Phản hồi bị gián đoạn hoặc đã dừng.", null); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
    private async Task<ChatAnswer> Reply(ChatTurn turn, CancellationToken ct)
    {
        try { return await client.ReplyAsync(turn.Settings, turn.Context, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Chỉ tên loại lỗi; không đưa prompt/token/output nhà cung cấp vào log.
            logger.LogWarning("Chat thất bại: {ErrorType}", ex.GetType().Name);
            return new("", "Không kết nối được AI. Nội dung đã lưu; anh kiểm tra cài đặt và thử lại.", null);
        }
    }
}

public static class LocalChatRegistration
{
    public static IServiceCollection AddLocalChat(this IServiceCollection services, bool demo)
    {
        services.AddSingleton(sp => new LocalChatStore(demo ? ":memory:" : Path.Combine(sp.GetRequiredService<HubPaths>().UserProfile, ".aiteam", "hub", "chat.db")));
        services.AddSingleton<ILocalChatClient>(_ => demo ? new DemoChatClient() : new CodexChatClient(new ProcessRunner()));
        services.AddHostedService<LocalChatWorker>();
        return services;
    }
}
