using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace AITeam.Core.Routing;

/// <summary>Chuỗi lỗi riêng biệt với hợp đồng providers.json cũ; không lưu log/bí mật.</summary>
internal static class FailureStreakStore
{
    internal static IDisposable Lock(string root)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(root).ToUpperInvariant())));
        var mutex = new Mutex(false, (OperatingSystem.IsWindows() ? @"Global\" : "") + "AITeam.ProviderFailures." + hash);
        bool owned;
        try { owned = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
        catch (AbandonedMutexException) { owned = true; }
        if (!owned) { mutex.Dispose(); throw new IOException("Không lấy được khóa cập nhật chuỗi lỗi provider."); }
        return new Lease(mutex);
    }
    private sealed class Lease(Mutex mutex) : IDisposable
    {
        public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
    }
    internal static int Update(string root, string provider, bool failed)
    {
        using var gate = Lock(root);
        var directory = Path.Combine(root, ".state"); var file = Path.Combine(directory, "provider-failures.json");
        foreach (var path in new[] { directory, file })
            if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Không cập nhật chuỗi lỗi qua symlink/reparse point.");
        if (File.Exists(file) && new FileInfo(file).Length > 1024 * 1024) throw new InvalidDataException("Dữ liệu chuỗi lỗi quá lớn.");
        var values = File.Exists(file) ? JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(file)) ?? throw new InvalidDataException("Dữ liệu chuỗi lỗi rỗng.") : [];
        var counters = new Dictionary<string, int>(values, StringComparer.OrdinalIgnoreCase);
        if (counters.Values.Any(v => v < 0)) throw new InvalidDataException("Dữ liệu chuỗi lỗi âm.");
        var count = failed ? checked(counters.GetValueOrDefault(provider) + 1) : 0;
        if (failed) counters[provider] = count; else counters.Remove(provider);
        if (counters.Count == 0) { if (File.Exists(file)) File.Delete(file); return count; }
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, "provider-failures." + Guid.NewGuid().ToString("N") + ".tmp");
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(counters), new UTF8Encoding(false)); File.Move(temporary, file, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return count;
    }
}
