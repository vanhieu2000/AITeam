using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AITeam.Core.Providers;
using Microsoft.Data.Sqlite;
using LegacyCatalog = AITeam.Core.Accounts.AiCatalog;

namespace AITeam.Core.AiServices;

/// <summary>
/// Kho danh bạ dịch vụ AI trong SQLite cục bộ (office.db). Một kết nối cho mỗi thao tác,
/// ghi bằng BEGIN IMMEDIATE; không bao giờ lưu giá trị bí mật — chỉ tên khóa trong két.
/// </summary>
public sealed class SqliteAiServiceRegistry : IAiServiceRegistry, IDisposable
{
    public const int MaxIniBytes = 256 * 1024;

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS ai_service(
            id TEXT PRIMARY KEY, name TEXT NOT NULL, adapter TEXT NOT NULL, command TEXT,
            args_template TEXT NOT NULL DEFAULT '[]', base_url TEXT,
            models_json TEXT NOT NULL DEFAULT '[]', roles_json TEXT NOT NULL DEFAULT '[]',
            max_jobs INTEGER NOT NULL DEFAULT 1, paid INTEGER NOT NULL DEFAULT 0,
            secret_key_name TEXT, manage_url TEXT, enabled INTEGER NOT NULL DEFAULT 1,
            created_at TEXT NOT NULL, updated_at TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS ai_account(
            id TEXT PRIMARY KEY, service_id TEXT NOT NULL, alias TEXT NOT NULL, mode TEXT NOT NULL,
            login_state TEXT, checked_at TEXT);
        CREATE INDEX IF NOT EXISTS ix_ai_account_service ON ai_account(service_id);
        CREATE TABLE IF NOT EXISTS ai_quota(
            service_id TEXT NOT NULL, account_id TEXT, remaining_pct REAL, rest_until TEXT,
            source TEXT NOT NULL, observed_at TEXT NOT NULL);
        CREATE INDEX IF NOT EXISTS ix_ai_quota_observed ON ai_quota(service_id, account_id, observed_at);
        CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY, value TEXT);
        """;

    private static readonly Regex IdPattern = new(@"\A[a-z0-9][a-z0-9-]{1,31}\z", RegexOptions.CultureInvariant);
    private static readonly Regex PlainCommand = new(@"\A[A-Za-z0-9._-]{1,260}\z", RegexOptions.CultureInvariant);
    private static readonly Regex SecretName = new(@"\A[A-Z][A-Z0-9_]{1,63}\z", RegexOptions.CultureInvariant);
    private static readonly char[] ShellChars = ['&', '|', ';', '<', '>', '^', '%', '"', '\''];
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly string _connectionString;
    private readonly SqliteConnection? _anchor;
    private readonly object _gate = new();
    private readonly bool _isMemory;

    public string Location { get; }

    public SqliteAiServiceRegistry(string dbFile, string aiTeamRoot, string? legacyAccountsFile = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbFile);
        _isMemory = string.Equals(dbFile, ":memory:", StringComparison.Ordinal);
        Location = _isMemory ? "Trong bộ nhớ · dữ liệu minh họa" : Path.GetFullPath(dbFile);

        if (!_isMemory)
        {
            var parent = Path.GetDirectoryName(Location)!;
            for (var dir = new DirectoryInfo(parent); dir != null; dir = dir.Parent)
                if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Thư mục dữ liệu AI không được là liên kết.");
            foreach (var path in new[] { Location, Location + "-wal", Location + "-shm" })
                if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("File dữ liệu AI không được là liên kết.");
            Directory.CreateDirectory(parent);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _isMemory ? "ai-" + Guid.NewGuid().ToString("N") : Location,
            Mode = _isMemory ? SqliteOpenMode.Memory : SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 5,
        }.ToString();

        if (_isMemory)
        {
            _anchor = new SqliteConnection(_connectionString);
            _anchor.Open();
        }

        try
        {
            if (!_isMemory)
            {
                using var journal = Open();
                Execute(journal, "PRAGMA journal_mode=WAL");
            }
            var fresh = false;
            Write(db =>
            {
                Execute(db, Schema);
                using var count = Command(db, "SELECT count(*) FROM schema_info");
                fresh = (long)count.ExecuteScalar()! == 0;
                if (fresh) Execute(db, "INSERT INTO schema_info(version) VALUES(1)");
            });
            using (var db = Open())
            using (var version = Command(db, "SELECT version FROM schema_info ORDER BY version DESC LIMIT 1"))
            {
                var value = version.ExecuteScalar();
                if (value != null && Convert.ToInt32(value, CultureInfo.InvariantCulture) > 1)
                    throw new InvalidDataException("Dữ liệu danh bạ AI thuộc phiên bản mới hơn. Không ghi đè.");
            }
            if (fresh && !_isMemory) Migrate(aiTeamRoot, legacyAccountsFile);
        }
        catch
        {
            _anchor?.Dispose();
            throw;
        }
    }

    // ───────────────────────── Hạ tầng SQLite ─────────────────────────

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(_connectionString);
        db.Open();
        return db;
    }

    private static SqliteCommand Command(SqliteConnection db, string sql, params (string Key, object? Value)[] parameters)
    {
        var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (key, value) in parameters) cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return cmd;
    }

    private static void Execute(SqliteConnection db, string sql, params (string Key, object? Value)[] parameters)
    {
        using var cmd = Command(db, sql, parameters);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Ghi an toàn: khóa trong tiến trình + BEGIN IMMEDIATE (chống ghi chen giữa 2 tiến trình).</summary>
    private void Write(Action<SqliteConnection> action)
    {
        lock (_gate)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using var db = Open();
                    Execute(db, "BEGIN IMMEDIATE");
                    try
                    {
                        action(db);
                        Execute(db, "COMMIT");
                    }
                    catch
                    {
                        try { Execute(db, "ROLLBACK"); } catch (SqliteException) { }
                        throw;
                    }
                    return;
                }
                catch (SqliteException ex) when (attempt < 4 && (ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6))
                {
                    Thread.Sleep(20 * (attempt + 1));
                }
            }
        }
    }

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static string SerializeList(IEnumerable<string> values) =>
        JsonSerializer.Serialize(values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()), JsonOpts);

    private static List<string> ReadList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<AiRole> ReadRoles(string? json)
    {
        var roles = new List<AiRole>();
        foreach (var raw in ReadList(json))
            if (AiServiceText.TryParseRole(raw, out var role)) roles.Add(role);
        return roles;
    }

    private static string SerializeRoles(IEnumerable<AiRole> roles) =>
        JsonSerializer.Serialize(roles.Distinct().Select(r => r.ToStorage()), JsonOpts);

    private static DateTimeOffset ReadTime(object? value, DateTimeOffset fallback) =>
        value is string text
        && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : fallback;

    private static AiService ReadService(SqliteDataReader reader)
    {
        var now = DateTimeOffset.UtcNow;
        return new AiService(
            reader.GetString(0),
            reader.GetString(1),
            AiServiceText.TryParseAdapter(reader.GetString(2), out var adapter) ? adapter : AiAdapter.CliText,
            reader.IsDBNull(3) ? null : reader.GetString(3),
            ReadList(reader.GetString(4)),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            ReadList(reader.GetString(6)),
            ReadRoles(reader.GetString(7)),
            reader.GetInt32(8),
            reader.GetInt64(9) != 0,
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.GetInt64(12) != 0,
            ReadTime(reader.GetValue(13), now),
            ReadTime(reader.GetValue(14), now));
    }

    private const string SelectService = """
        SELECT id,name,adapter,command,args_template,base_url,models_json,roles_json,
               max_jobs,paid,secret_key_name,manage_url,enabled,created_at,updated_at
        FROM ai_service
        """;

    // ───────────────────────── Hợp đồng ─────────────────────────

    public IReadOnlyList<AiService> List(bool includeDisabled = true)
    {
        lock (_gate)
        {
            using var db = Open();
            using var cmd = Command(db, SelectService + (includeDisabled ? "" : " WHERE enabled=1") + " ORDER BY name COLLATE NOCASE, id");
            using var reader = cmd.ExecuteReader();
            var rows = new List<AiService>();
            while (reader.Read()) rows.Add(ReadService(reader));
            return rows;
        }
    }

    public AiService? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        lock (_gate)
        {
            using var db = Open();
            using var cmd = Command(db, SelectService + " WHERE id=$id", ("$id", id.Trim().ToLowerInvariant()));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadService(reader) : null;
        }
    }

    public AiServiceValidation Validate(AiService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        var errors = new List<string>();

        if (string.IsNullOrEmpty(service.Id) || !IdPattern.IsMatch(service.Id))
            errors.Add("Mã dịch vụ không hợp lệ (chữ thường, số, gạch nối; 2–32 ký tự).");

        var name = service.Name?.Trim() ?? "";
        if (name.Length is < 1 or > 60) errors.Add("Tên dịch vụ phải từ 1 đến 60 ký tự.");
        else if (name.Any(char.IsControl)) errors.Add("Tên dịch vụ không được chứa ký tự điều khiển.");

        var args = service.Args ?? [];
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i] ?? "";
            if (arg.Length > 200) errors.Add($"Tham số thứ {i + 1} dài quá 200 ký tự.");
            if (arg.Contains('\r') || arg.Contains('\n')) errors.Add($"Tham số thứ {i + 1} không được xuống dòng.");
        }

        if (service.Adapter == AiAdapter.OpenAiCompatibleHttp)
        {
            var baseUrl = service.BaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) errors.Add("Địa chỉ API không được để trống.");
            else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                errors.Add("Địa chỉ API không hợp lệ.");
            else if (uri.UserInfo.Length > 0)
                errors.Add("Địa chỉ API không được chứa tài khoản / mật khẩu.");
            else if (uri.Scheme == Uri.UriSchemeHttp
                && !string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal)
                && !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                errors.Add("Địa chỉ API phải là https:// hoặc http://127.0.0.1 / http://localhost.");
        }
        else
        {
            var command = service.Command?.Trim();
            if (string.IsNullOrEmpty(command)) errors.Add("Lệnh CLI không được để trống.");
            else if (command.Any(char.IsControl) || command.IndexOfAny(ShellChars) >= 0)
                errors.Add("Lệnh CLI không được chứa ký tự shell (& | ; < > ^ % \" ').");
            else if (command.Contains('\\') || command.Contains('/'))
            {
                if (!command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    && !command.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                    && !command.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Lệnh CLI phải là tệp .exe / .cmd / .bat hoặc tên lệnh.");
            }
            else if (!PlainCommand.IsMatch(command))
                errors.Add("Lệnh CLI phải là tệp .exe / .cmd / .bat hoặc tên lệnh.");
        }

        var manageUrl = service.ManageUrl?.Trim();
        if (!string.IsNullOrEmpty(manageUrl))
        {
            if (!Uri.TryCreate(manageUrl, UriKind.Absolute, out var manage)
                || manage.Scheme != Uri.UriSchemeHttps
                || manage.UserInfo.Length > 0)
                errors.Add("Link quản lý phải bắt đầu bằng https://.");
        }

        var secretName = service.SecretKeyName?.Trim();
        if (!string.IsNullOrEmpty(secretName) && !SecretName.IsMatch(secretName))
            errors.Add("Tên khóa bí mật chỉ gồm chữ in hoa, số, gạch dưới (2–64 ký tự).");

        if (service.MaxJobs is < 1 or > 8) errors.Add("Số việc song song phải từ 1 đến 8.");
        if (service.Roles == null || service.Roles.Count == 0) errors.Add("Phải chọn ít nhất một vai trò.");

        return new AiServiceValidation(errors.Count == 0, errors);
    }

    public AiService Upsert(AiService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        var validation = Validate(service);
        if (!validation.Ok) throw new ArgumentException(string.Join(" ", validation.Errors));

        var id = service.Id;
        var now = DateTimeOffset.UtcNow;
        Write(db => SaveCore(db, service, now));
        return Get(id) ?? throw new InvalidOperationException("Không đọc lại được dịch vụ vừa lưu.");
    }

    /// <summary>Ghi 1 dòng dịch vụ (dùng chung cho Upsert và nhập dữ liệu cũ).</summary>
    private static void SaveCore(SqliteConnection db, AiService service, DateTimeOffset now)
    {
        using var existing = Command(db, "SELECT created_at FROM ai_service WHERE id=$id", ("$id", service.Id));
        var raw = existing.ExecuteScalar() as string;
        var created = raw == null
            ? (service.CreatedAt == default ? now : service.CreatedAt)
            : ReadTime(raw, now);
        Execute(db, """
            INSERT INTO ai_service(id,name,adapter,command,args_template,base_url,models_json,roles_json,
                max_jobs,paid,secret_key_name,manage_url,enabled,created_at,updated_at)
            VALUES($id,$name,$adapter,$command,$args,$baseUrl,$models,$roles,$maxJobs,$paid,$secret,$manage,$enabled,$created,$updated)
            ON CONFLICT(id) DO UPDATE SET
                name=excluded.name, adapter=excluded.adapter, command=excluded.command,
                args_template=excluded.args_template, base_url=excluded.base_url,
                models_json=excluded.models_json, roles_json=excluded.roles_json,
                max_jobs=excluded.max_jobs, paid=excluded.paid,
                secret_key_name=excluded.secret_key_name, manage_url=excluded.manage_url,
                enabled=excluded.enabled, updated_at=excluded.updated_at
            """,
            ("$id", service.Id),
            ("$name", (service.Name ?? "").Trim()),
            ("$adapter", service.Adapter.ToStorage()),
            ("$command", NullIfEmpty(service.Command)),
            ("$args", SerializeList(service.Args ?? [])),
            ("$baseUrl", NullIfEmpty(service.BaseUrl)),
            ("$models", SerializeList(service.Models ?? [])),
            ("$roles", SerializeRoles(service.Roles ?? [])),
            ("$maxJobs", service.MaxJobs),
            ("$paid", service.Paid ? 1 : 0),
            ("$secret", NullIfEmpty(service.SecretKeyName)),
            ("$manage", NullIfEmpty(service.ManageUrl)),
            ("$enabled", service.Enabled ? 1 : 0),
            ("$created", created.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            ("$updated", now.ToString("O", CultureInfo.InvariantCulture)));
    }

    private static object? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void SetEnabled(string id, bool enabled)
    {
        var key = NormalizeId(id);
        Write(db =>
        {
            using var existing = Command(db, "SELECT count(*) FROM ai_service WHERE id=$id", ("$id", key));
            if ((long)existing.ExecuteScalar()! == 0) throw new KeyNotFoundException("Không tìm thấy dịch vụ.");
            Execute(db, "UPDATE ai_service SET enabled=$enabled, updated_at=$at WHERE id=$id",
                ("$enabled", enabled ? 1 : 0), ("$at", Now()), ("$id", key));
        });
    }

    private static string NormalizeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Mã dịch vụ không hợp lệ.");
        return id.Trim().ToLowerInvariant();
    }

    public IReadOnlyList<AiAccount> Accounts(string? serviceId = null)
    {
        lock (_gate)
        {
            using var db = Open();
            using var cmd = string.IsNullOrWhiteSpace(serviceId)
                ? Command(db, "SELECT id,service_id,alias,mode,login_state,checked_at FROM ai_account ORDER BY service_id, id")
                : Command(db, "SELECT id,service_id,alias,mode,login_state,checked_at FROM ai_account WHERE service_id=$sid ORDER BY id",
                    ("$sid", serviceId.Trim().ToLowerInvariant()));
            using var reader = cmd.ExecuteReader();
            var rows = new List<AiAccount>();
            while (reader.Read())
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : ReadTime(reader.GetValue(5), DateTimeOffset.UtcNow)));
            return rows;
        }
    }

    public AiAccount UpsertAccount(AiAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var id = account.Id?.Trim() ?? "";
        if (id.Length == 0 || id.Length > 64 || id.Any(char.IsControl))
            throw new ArgumentException("Mã tài khoản không hợp lệ.");
        var serviceId = NormalizeId(account.ServiceId);
        var alias = CleanAlias(account.Alias);
        var mode = account.Mode?.Trim() ?? "";
        if (mode.Length == 0 || mode.Length > 40 || mode.Any(char.IsControl))
            throw new ArgumentException("Chế độ tài khoản không hợp lệ.");

        Write(db =>
        {
            using var svc = Command(db, "SELECT count(*) FROM ai_service WHERE id=$id", ("$id", serviceId));
            if ((long)svc.ExecuteScalar()! == 0) throw new ArgumentException("Dịch vụ của tài khoản không tồn tại.");
            Execute(db, """
                INSERT INTO ai_account(id,service_id,alias,mode,login_state,checked_at)
                VALUES($id,$sid,$alias,$mode,$state,$checked)
                ON CONFLICT(id) DO UPDATE SET
                    service_id=excluded.service_id, alias=excluded.alias, mode=excluded.mode,
                    login_state=excluded.login_state, checked_at=excluded.checked_at
                """,
                ("$id", id), ("$sid", serviceId), ("$alias", alias), ("$mode", mode),
                ("$state", NullIfEmpty(account.LoginState)),
                ("$checked", account.CheckedAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
        });

        return Accounts(serviceId).FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException("Không đọc lại được tài khoản vừa lưu.");
    }

    private static string CleanAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return "";
        var text = new string(alias.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return text.Length > 60 ? text[..60].Trim() : text;
    }

    public IReadOnlyList<AiQuota> LatestQuotas()
    {
        lock (_gate)
        {
            using var db = Open();
            using var cmd = Command(db, """
                SELECT service_id,account_id,remaining_pct,rest_until,source,observed_at FROM (
                    SELECT *, ROW_NUMBER() OVER (
                        PARTITION BY service_id, COALESCE(account_id,'') ORDER BY observed_at DESC, rowid DESC) AS rn
                    FROM ai_quota)
                WHERE rn=1
                ORDER BY service_id, account_id
                """);
            using var reader = cmd.ExecuteReader();
            var rows = new List<AiQuota>();
            while (reader.Read())
                rows.Add(new(
                    reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetDouble(2),
                    reader.IsDBNull(3) ? null : ReadTime(reader.GetValue(3), DateTimeOffset.UtcNow),
                    reader.GetString(4),
                    ReadTime(reader.GetValue(5), DateTimeOffset.UtcNow)));
            return rows;
        }
    }

    public void RecordQuota(AiQuota quota)
    {
        ArgumentNullException.ThrowIfNull(quota);
        var serviceId = NormalizeId(quota.ServiceId);
        var source = quota.Source?.Trim() ?? "";
        if (source.Length == 0 || source.Length > 100 || source.Any(char.IsControl))
            throw new ArgumentException("Nguồn số đo không hợp lệ.");
        if (quota.RemainingPct is < 0 or > 100)
            throw new ArgumentException("Hạn mức còn lại phải trong khoảng 0–100.");
        var observed = quota.ObservedAt == default ? DateTimeOffset.UtcNow : quota.ObservedAt;
        Write(db => Execute(db, "INSERT INTO ai_quota(service_id,account_id,remaining_pct,rest_until,source,observed_at) VALUES($sid,$aid,$pct,$rest,$source,$at)",
            ("$sid", serviceId),
            ("$aid", NullIfEmpty(quota.AccountId)),
            ("$pct", quota.RemainingPct),
            ("$rest", quota.RestUntil?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            ("$source", source),
            ("$at", observed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))));
    }

    public string? GetSetting(string key)
    {
        var name = NormalizeSettingKey(key);
        lock (_gate)
        {
            using var db = Open();
            using var cmd = Command(db, "SELECT value FROM settings WHERE key=$key", ("$key", name));
            return cmd.ExecuteScalar() as string;
        }
    }

    public void SetSetting(string key, string? value)
    {
        var name = NormalizeSettingKey(key);
        if (value != null && value.Length > 8192) throw new ArgumentException("Giá trị cài đặt dài quá 8192 ký tự.");
        Write(db => Execute(db, value == null
            ? "DELETE FROM settings WHERE key=$key"
            : "INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value",
            ("$key", name), ("$value", value)));
    }

    private static string NormalizeSettingKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || key.Any(char.IsControl))
            throw new ArgumentException("Khóa cài đặt không hợp lệ.");
        return key.Trim();
    }

    // ───────────────────────── .ini ─────────────────────────

    public string ExportIni()
    {
        var builder = new StringBuilder();
        foreach (var service in List(true))
        {
            builder.Append("[service:").Append(service.Id).Append("]\r\n");
            builder.Append("name=").Append(Line(service.Name)).Append("\r\n");
            builder.Append("adapter=").Append(service.Adapter.ToStorage()).Append("\r\n");
            builder.Append("command=").Append(Line(service.Command)).Append("\r\n");
            builder.Append("args=").Append(Line(string.Join('|', service.Args ?? []))).Append("\r\n");
            builder.Append("base_url=").Append(Line(service.BaseUrl)).Append("\r\n");
            builder.Append("models=").Append(Line(string.Join(',', service.Models ?? []))).Append("\r\n");
            builder.Append("roles=").Append(string.Join(',', service.Roles.Select(r => r.ToStorage()))).Append("\r\n");
            builder.Append("max_jobs=").Append(service.MaxJobs.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            builder.Append("paid=").Append(service.Paid ? "true" : "false").Append("\r\n");
            builder.Append("secret_key_name=").Append(Line(service.SecretKeyName)).Append("\r\n");
            builder.Append("manage_url=").Append(Line(service.ManageUrl)).Append("\r\n");
            builder.Append("enabled=").Append(service.Enabled ? "true" : "false").Append("\r\n\r\n");
        }
        return builder.ToString();
    }

    private static string Line(string? value) => (value ?? "").Replace("\r", "").Replace("\n", "");

    public IniImportResult ImportIni(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Encoding.UTF8.GetByteCount(text) > MaxIniBytes)
            throw new ArgumentException("Tệp .ini quá lớn (tối đa 256 KB).");

        var sections = new List<(string Id, Dictionary<string, string> Values)>();
        Dictionary<string, string>? current = null;
        foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var name = line[1..^1].Trim();
                current = name.StartsWith("service:", StringComparison.OrdinalIgnoreCase)
                    ? NewSection(sections, name["service:".Length..].Trim())
                    : null;
                continue;
            }
            if (current == null) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim().ToLowerInvariant();
            var value = line[(eq + 1)..].Trim();
            if (key.Length == 0) continue;
            current[key] = value;
        }

        var skipped = new List<string>();
        var added = 0;
        var updated = 0;
        foreach (var (id, values) in sections)
        {
            // Không bao giờ nhận giá trị bí mật: chỉ cho phép đúng khóa secret_key_name.
            var secrets = values.Keys
                .Where(k => k != "secret_key_name" && (k.Contains("key") || k.Contains("token") || k.Contains("password") || k.Contains("secret")))
                .ToList();
            foreach (var key in secrets) skipped.Add($"[service:{id}] bỏ khóa bí mật: {key}");
            foreach (var key in secrets) values.Remove(key);

            var errors = new List<string>();
            var createdAt = DateTimeOffset.UtcNow;
            AiAdapter adapter = AiAdapter.CliJson;
            if (values.TryGetValue("adapter", out var adapterRaw) && !AiServiceText.TryParseAdapter(adapterRaw, out adapter))
                errors.Add("Kiểu adapter không hợp lệ.");

            var roles = new List<AiRole>();
            if (values.TryGetValue("roles", out var rolesRaw))
                foreach (var part in rolesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (AiServiceText.TryParseRole(part, out var role)) roles.Add(role);
                    else errors.Add($"Vai trò lạ: {part}.");
                }

            var maxJobs = 1;
            if (values.TryGetValue("max_jobs", out var maxRaw)
                && !int.TryParse(maxRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxJobs))
                errors.Add("Số việc song song không phải số nguyên.");

            var paid = ParseBool(values, "paid", false, errors);
            var enabled = ParseBool(values, "enabled", true, errors);

            var service = new AiService(
                id,
                values.TryGetValue("name", out var name) ? name : id,
                adapter,
                values.TryGetValue("command", out var command) ? command : null,
                SplitList(values, "args", '|'),
                values.TryGetValue("base_url", out var baseUrl) ? baseUrl : null,
                SplitList(values, "models", ','),
                roles,
                maxJobs,
                paid,
                values.TryGetValue("secret_key_name", out var secret) ? secret : null,
                values.TryGetValue("manage_url", out var manage) ? manage : null,
                enabled,
                createdAt,
                createdAt);

            if (errors.Count > 0)
            {
                skipped.Add($"[service:{id}] {string.Join(" ", errors)}");
                continue;
            }
            var validation = Validate(service);
            if (!validation.Ok)
            {
                skipped.Add($"[service:{id}] {string.Join(" ", validation.Errors)}");
                continue;
            }

            var exists = Get(id) != null;
            Upsert(service);
            if (exists) updated++;
            else added++;
        }

        return new IniImportResult(added, updated, skipped);
    }

    private static Dictionary<string, string> NewSection(List<(string Id, Dictionary<string, string> Values)> sections, string id)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        sections.Add((id, values));
        return values;
    }

    private static List<string> SplitList(IReadOnlyDictionary<string, string> values, string key, char separator) =>
        values.TryGetValue(key, out var raw)
            ? raw.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];

    private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key, bool fallback, List<string> errors)
    {
        if (!values.TryGetValue(key, out var raw)) return fallback;
        return raw.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => Invalid(),
        };

        bool Invalid()
        {
            errors.Add($"Giá trị {key} không phải đúng / sai.");
            return fallback;
        }
    }

    // ───────────────────────── Nhập dữ liệu cũ một lần ─────────────────────────

    private void Migrate(string aiTeamRoot, string? legacyAccountsFile)
    {
        var now = DateTimeOffset.UtcNow;
        var failures = new List<string>();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!SetupProviders(aiTeamRoot, now, failures, existing))
        {
            // team.json hỏng / thiếu: vẫn dựng danh mục cũ để Hub không sập.
        }

        foreach (var legacy in LegacyCatalog.All)
        {
            if (existing.Contains(legacy.Id)) continue;
            if (string.Equals(legacy.Id, "antigravity", StringComparison.OrdinalIgnoreCase)
                && existing.Contains("antigravity")) continue;
            var roles = legacy.Role.Contains("nghiên cứu", StringComparison.OrdinalIgnoreCase)
                ? new[] { AiRole.Research }
                : new[] { AiRole.Code };
            var command = legacy.Id switch
            {
                "antigravity" or "gemini" => "agy",
                "codex" or "chatgpt" => "codex",
                "claude" => "claude",
                _ => null,
            };
            var service = new AiService(
                legacy.Id, legacy.Name, AiAdapter.CliText, command, [], null, [], roles,
                1, false, null,
                Uri.TryCreate(legacy.OfficialUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps ? legacy.OfficialUrl : null,
                false, now, now);
            TryInsert(service, failures, existing);
        }

        if (!string.IsNullOrWhiteSpace(legacyAccountsFile)) ImportLegacyAccounts(legacyAccountsFile!, failures);

        SetSettingInternal("migrated_at", now.ToString("O", CultureInfo.InvariantCulture));
        if (failures.Count > 0) SetSettingInternal("migrated_skipped", string.Join(" | ", failures));
    }

    private bool SetupProviders(string aiTeamRoot, DateTimeOffset now, List<string> failures, HashSet<string> existing)
    {
        List<ProviderConfig> providers;
        try
        {
            if (string.IsNullOrWhiteSpace(aiTeamRoot)) return false;
            var teamPath = Path.Combine(aiTeamRoot, "team.json");
            if (!File.Exists(teamPath)) return false;
            var team = ReadJson(teamPath) ?? throw new InvalidDataException("team.json rỗng.");
            var local = ReadJson(Path.Combine(aiTeamRoot, "team.local.json"));
            var agyExe = local?["agy"]?["exe"]?.GetValue<string>()
                ?? team["agy"]?["exe"]?.GetValue<string>()
                ?? @"C:\Tools\agy\bin\agy.exe";
            var settings = ProviderSettings.Default(agyExe);
            if (team["providers"] is JsonArray array)
            {
                var list = new List<ProviderConfig>();
                foreach (var node in array)
                    list.Add(node.Deserialize<ProviderConfig>(ProviderSettings.Json)
                        ?? throw new InvalidDataException("Mục providers sai định dạng."));
                settings = settings with { Providers = list };
            }
            if (local?["providers"] is JsonObject overrides)
                settings = settings with
                {
                    Providers = settings.Providers.Select(p => overrides[p.Id] is JsonObject o
                        ? p with
                        {
                            Exe = o["exe"]?.GetValue<string>() ?? p.Exe,
                            NodeExe = o["nodeExe"]?.GetValue<string>() ?? p.NodeExe,
                            Enabled = o["enabled"]?.GetValue<bool>() ?? p.Enabled,
                        }
                        : p).ToList(),
                };
            var baseUrl = local?["codex"]?["baseUrl"]?.GetValue<string>() ?? team["codex"]?["baseUrl"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(baseUrl))
                settings = settings with
                {
                    Providers = settings.Providers.Select(p => p.Kind == ProviderKind.Codex ? p with { BaseUrl = baseUrl } : p).ToList(),
                };
            providers = settings.Providers.ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            failures.Add("Không đọc được team.json: " + ex.Message);
            return false;
        }

        foreach (var provider in providers)
        {
            var id = NormalizeProviderId(provider.Id);
            if (id == null) { failures.Add($"Mã provider lạ: {provider.Id}"); continue; }
            var adapter = provider.Kind == ProviderKind.ClaudeCli ? AiAdapter.CliText : AiAdapter.CliJson;
            var roles = provider.Roles.Select(MapRole).Distinct().ToList();
            if (roles.Count == 0) roles = [AiRole.Code];
            var service = new AiService(
                id,
                provider.Kind switch
                {
                    ProviderKind.Antigravity => "Antigravity",
                    ProviderKind.Codex => "Codex",
                    ProviderKind.OpenCode => "OpenCode",
                    ProviderKind.ClaudeCli => "Claude",
                    _ => provider.Id,
                },
                adapter,
                string.IsNullOrWhiteSpace(provider.Exe) ? null : provider.Exe.Trim(),
                [],
                string.IsNullOrWhiteSpace(provider.BaseUrl) ? null : provider.BaseUrl.Trim(),
                string.IsNullOrWhiteSpace(provider.Model) ? [] : [provider.Model.Trim()],
                roles,
                Math.Clamp(provider.MaxConcurrent, 1, 8),
                provider.Paid,
                null,
                null,
                provider.Enabled,
                now,
                now);
            TryInsert(service, failures, existing);
        }
        return true;
    }

    private static JsonNode? ReadJson(string path)
    {
        if (!File.Exists(path)) return null;
        return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
    }

    private static AiRole MapRole(AgentRole role) => role switch
    {
        AgentRole.QA or AgentRole.Reviewer => AiRole.Qc,
        _ => AiRole.Code,
    };

    private static string? NormalizeProviderId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var id = raw.Trim().ToLowerInvariant();
        return IdPattern.IsMatch(id) ? id : null;
    }

    private void TryInsert(AiService service, List<string> failures, HashSet<string> existing)
    {
        // Dữ liệu cũ (team.json, AiCatalog) được giữ nguyên cả khi chưa đủ trường:
        // hiện ở dạng tắt để người dùng sửa, không làm sập Hub.
        try
        {
            var now = DateTimeOffset.UtcNow;
            Write(db => SaveCore(db, service, now));
            existing.Add(service.Id);
        }
        catch (Exception ex) when (ex is SqliteException or ArgumentException or InvalidOperationException)
        {
            failures.Add($"[{service.Id}] {ex.Message}");
        }
    }

    private void ImportLegacyAccounts(string file, List<string> failures)
    {
        try
        {
            if (!File.Exists(file)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(file, Encoding.UTF8));
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("accounts", out var array)
                || array.ValueKind != JsonValueKind.Array) return;
            var index = 0;
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var rawId = item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String ? idProp.GetString() : null;
                var service = Get(rawId ?? "");
                if (service == null) { failures.Add($"Tài khoản cũ có dịch vụ lạ: {rawId}"); continue; }
                var alias = item.TryGetProperty("alias", out var aliasProp) && aliasProp.ValueKind == JsonValueKind.String ? aliasProp.GetString() : null;
                var mode = item.TryGetProperty("mode", out var modeProp) && modeProp.ValueKind == JsonValueKind.String ? modeProp.GetString() : null;
                try
                {
                    UpsertAccount(new AiAccount("legacy-" + service.Id, service.Id, alias ?? "", string.IsNullOrWhiteSpace(mode) ? "Manual" : mode!.Trim(), null, null));
                }
                catch (ArgumentException ex)
                {
                    failures.Add($"Tài khoản cũ {rawId}: {ex.Message}");
                }
                index++;
                if (index >= 200) break;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            failures.Add("Không đọc được accounts.json cũ: " + ex.Message);
        }
    }

    private void SetSettingInternal(string key, string value)
    {
        Write(db => Execute(db, "INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value",
            ("$key", key), ("$value", value)));
    }

    /// <summary>Chế độ HUB_FAKE: 3 dịch vụ mẫu trong bộ nhớ (không ghi file).</summary>
    public void SeedSampleServices()
    {
        if (List(true).Count > 0) return;
        var now = DateTimeOffset.UtcNow;
        Upsert(new AiService("antigravity", "Antigravity", AiAdapter.CliJson, "agy", [], null, ["agy"],
            [AiRole.Code, AiRole.Research], 2, false, null, "https://antigravity.google", true, now, now));
        Upsert(new AiService("codex", "Codex", AiAdapter.CliJson, "codex", [], null, [],
            [AiRole.Code, AiRole.Qc], 1, true, null, "https://chatgpt.com/codex", true, now, now));
        Upsert(new AiService("opencode", "OpenCode", AiAdapter.CliJson, "opencode", [], null, [],
            [AiRole.Code], 2, false, null, null, true, now, now));
    }

    public void Dispose() => _anchor?.Dispose();
}
