using System.Globalization;
using System.Text;
using System.Text.Json;
using AITeam.Core.Routing;

namespace AITeam.Core.Queue;

/// <summary>Một lượt giữ khóa phạm vi: task, provider đang làm, giờ lấy và các đường dẫn Phạm vi.</summary>
public sealed record ScopeLock(string Task, string Provider, DateTime At, IReadOnlyList<string> Scope);

/// <summary>
/// Khóa phạm vi dùng chung giữa các tiến trình: &lt;AITeamRoot&gt;/.state/locks.json.
/// Ghi file tạm rồi move (nguyên tử) dưới khóa liên tiến trình; file hỏng được sao lưu nguyên byte
/// thành locks.json.hong-&lt;giờ&gt; rồi chạy tiếp từ rỗng. Không nhận đường dẫn tuyệt đối, ".." hoặc reparse point.
/// </summary>
public sealed class ScopeLockStore
{
    public const int MaxFileBytes = 1024 * 1024;

    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _root;
    private readonly string _scopeRoot;

    /// <param name="aiteamRoot">Gốc AITeam chứa thư mục .state.</param>
    /// <param name="scopeRoot">Gốc dự án để kiểm đường dẫn Phạm vi; mặc định bằng <paramref name="aiteamRoot"/>.</param>
    public ScopeLockStore(string aiteamRoot, string? scopeRoot = null)
    {
        if (string.IsNullOrWhiteSpace(aiteamRoot)) throw new ArgumentException("Thư mục gốc không hợp lệ.", nameof(aiteamRoot));
        _root = Path.GetFullPath(aiteamRoot);
        _scopeRoot = string.IsNullOrWhiteSpace(scopeRoot) ? _root : Path.GetFullPath(scopeRoot);
        FilePath = Path.Combine(_root, ".state", "locks.json");
    }

    public string FilePath { get; }

    /// <summary>Hai tập phạm vi giao nhau khi trùng file hoặc một bên là thư mục chứa bên kia (không phân biệt hoa thường).</summary>
    public static bool Overlaps(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        foreach (var a in left)
            foreach (var b in right)
                if (SameOrContains(a, b)) return true;
        return false;
    }

    private static bool SameOrContains(string left, string right)
    {
        var a = NormalizeOne(left);
        var b = NormalizeOne(right);
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;
        return a.StartsWith(b + "/", StringComparison.OrdinalIgnoreCase) ||
               b.StartsWith(a + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Chuẩn hóa "\" thành "/", bỏ "/" cuối; chặn rỗng, tuyệt đối, ổ đĩa, "." và "..".</summary>
    public static string NormalizeOne(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Đường dẫn phạm vi rỗng.", nameof(path));
        var text = path.Trim().Replace('\\', '/');
        if (text.StartsWith('/') || text.Contains(':'))
            throw new ArgumentException("Đường dẫn phạm vi là tuyệt đối: " + path, nameof(path));
        var parts = text.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(p => p is "." or ".."))
            throw new ArgumentException("Đường dẫn phạm vi không an toàn: " + path, nameof(path));
        return string.Join('/', parts);
    }

    /// <summary>Toàn bộ khóa, bỏ các task đã xong nếu có bộ lọc (✅/🔵/🔴 trên bảng việc).</summary>
    public IReadOnlyList<ScopeLock> All(Func<string, bool>? isFinished = null)
    {
        using var gate = FailureStreakStore.Lock(_root);
        var all = ReadCore();
        return isFinished == null ? all : all.Where(l => !isFinished(l.Task)).ToList();
    }

    /// <summary>
    /// Lấy khóa cho task. Cùng task ⇒ cập nhật provider/giờ/phạm vi. Trùng task khác ⇒ trả false kèm chủ khóa.
    /// </summary>
    public bool TryAcquire(string task, string provider, IReadOnlyList<string> scope, DateTime now,
        Func<string, bool>? isFinished, out string? holder)
    {
        if (string.IsNullOrWhiteSpace(task)) throw new ArgumentException("Mã task rỗng.", nameof(task));
        holder = null;
        var prepared = PrepareScope(scope);
        using var gate = FailureStreakStore.Lock(_root);
        var all = ReadCore().Where(l => isFinished == null || !isFinished(l.Task)).ToList();

        var same = all.FirstOrDefault(l => l.Task.Equals(task.Trim(), StringComparison.OrdinalIgnoreCase));
        if (same != null)
        {
            all.Remove(same);
            all.Add(same with { Provider = (provider ?? "").Trim(), At = now, Scope = prepared });
            Save(all);
            return true;
        }

        var conflict = all.FirstOrDefault(l => Overlaps(l.Scope, prepared));
        if (conflict != null)
        {
            holder = conflict.Task;
            return false;
        }

        all.Add(new ScopeLock(task.Trim(), (provider ?? "").Trim(), now, prepared));
        Save(all);
        return true;
    }

    /// <summary>Nhả khóa của task; false nếu không có khóa nào.</summary>
    public bool Release(string task)
    {
        if (string.IsNullOrWhiteSpace(task)) throw new ArgumentException("Mã task rỗng.", nameof(task));
        using var gate = FailureStreakStore.Lock(_root);
        var all = ReadCore();
        var kept = all.Where(l => !l.Task.Equals(task.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (kept.Count == all.Count) return false;
        if (kept.Count == 0)
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            return true;
        }
        Save(kept);
        return true;
    }

    private List<ScopeLock> ReadCore()
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        foreach (var path in new[] { _root, directory, FilePath })
            if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Không đọc/di chuyển khóa phạm vi qua symlink hoặc reparse point.");

        if (!File.Exists(FilePath)) return [];
        if (new FileInfo(FilePath).Length > MaxFileBytes) throw new InvalidDataException("Dữ liệu khóa phạm vi quá lớn.");
        try
        {
            var records = JsonSerializer.Deserialize<List<ScopeLock?>>(File.ReadAllText(FilePath, Encoding.UTF8), JsonOpts);
            if (records == null) throw new JsonException("Dữ liệu khóa phạm vi rỗng.");
            var result = new List<ScopeLock>();
            foreach (var record in records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Task) || record.Scope == null)
                    throw new JsonException("Bản ghi khóa phạm vi không hợp lệ.");
                result.Add(record with
                {
                    Task = record.Task.Trim(),
                    Provider = (record.Provider ?? "").Trim(),
                    Scope = record.Scope.Select(NormalizeOne).ToList(),
                });
            }
            return result;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            // Đổi tên nguyên file, không serialize lại: giữ mọi byte hỏng và chỉ sao lưu một lần.
            var backup = FilePath + ".hong-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N");
            File.Move(FilePath, backup, overwrite: false);
            return [];
        }
    }

    private IReadOnlyList<string> PrepareScope(IReadOnlyList<string> scope)
    {
        if (scope == null || scope.Count == 0) throw new ArgumentException("Phạm vi khóa rỗng.", nameof(scope));
        var prepared = scope.Select(NormalizeOne).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (prepared.Count == 0) throw new ArgumentException("Phạm vi khóa rỗng.", nameof(scope));

        var scopeRoot = Path.GetFullPath(_scopeRoot);
        var prefix = scopeRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var item in prepared)
        {
            var full = Path.GetFullPath(Path.Combine(scopeRoot, item.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Đường dẫn phạm vi thoát khỏi dự án: " + item, nameof(scope));
            for (var node = full; node != null && node.Length >= scopeRoot.Length; node = Path.GetDirectoryName(node))
            {
                if (!File.Exists(node) && !Directory.Exists(node)) continue;
                if ((File.GetAttributes(node) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Không khóa phạm vi qua symlink hoặc reparse point.");
            }
        }
        return prepared;
    }

    private void Save(List<ScopeLock> locks)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, "locks." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(locks, JsonOpts), Utf8NoBom);
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
