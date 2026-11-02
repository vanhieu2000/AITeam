using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Research;
using Microsoft.Data.Sqlite;

namespace AITeam.Hub.Services;

public sealed record ChatSettings
{
    public string Model { get; init; } = "";
    public string CodexPath { get; init; } = "";
    public string BaseUrl { get; init; } = "https://chatgpt.com/backend-api/codex";
    public int TimeoutSeconds { get; init; } = 180;
    public string Instructions { get; init; } = "Trả lời bằng tiếng Việt. Bạn là trợ lý của chủ dự án AITeam. Hãy nhớ nội dung hội thoại được cung cấp, nói rõ điều chưa biết và không tự nhận đã thực hiện công việc.";

    public void Validate()
    {
        if (Model == null || !Regex.IsMatch(Model, @"\A(?:[a-zA-Z0-9][a-zA-Z0-9._/-]{0,99})?\z"))
            throw new ArgumentException("Tên model không hợp lệ.");
        if (CodexPath == null || CodexPath.Length > 1000 || CodexPath.Any(char.IsControl))
            throw new ArgumentException("Đường dẫn Codex không hợp lệ.");
        if (CodexPath.Length > 0)
        {
            var name = Path.GetFileName(CodexPath);
            if (name is not ("codex" or "codex.exe" or "codex.js"))
                throw new ArgumentException("Chọn codex, codex.exe hoặc codex.js; không chọn shell/script khác.");
        }
        if (BaseUrl != "" && BaseUrl != "https://chatgpt.com/backend-api/codex")
            throw new ArgumentException("Chat tài khoản ChatGPT chỉ dùng endpoint chính thức; để trống để dùng cấu hình CLI.");
        if (Instructions == null || Instructions.Length > 4000 || TimeoutSeconds is < 30 or > 600)
            throw new ArgumentException("Lời hướng dẫn tối đa 4000 ký tự; thời gian chờ từ 30 đến 600 giây.");
    }
}

public sealed record ChatConversation(string Id, string Title, string UpdatedAt, bool Busy);
public sealed record ChatMessage(long Id, string Role, string Text, string Status, string At, string Model, long? Tokens, string? Error);
public sealed record ChatTurn(long MessageId, string ConversationId, IReadOnlyList<ChatMessage> Context, ChatSettings Settings);

/// <summary>SQLite cục bộ, câu lệnh tham số hóa; connection riêng mỗi thao tác, không giữ khóa DB trong lúc gọi AI.</summary>
public sealed class LocalChatStore : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _anchor;
    private FileStream? _lease;
    private readonly object _gate = new();
    public string Location { get; }
    public bool IsDemo { get; }

    public LocalChatStore(string file)
    {
        IsDemo = file == ":memory:";
        Location = IsDemo ? "Trong bộ nhớ · dữ liệu minh họa" : Path.GetFullPath(file);
        if (!IsDemo)
        {
            // Cấm liên kết dẫn DB / WAL ra ngoài vùng lưu trữ do ứng dụng chọn.
            var parent = Path.GetDirectoryName(Location)!;
            for (var dir = new DirectoryInfo(parent); dir != null; dir = dir.Parent)
                if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Thư mục dữ liệu chat không được là liên kết.");
            foreach (var path in new[] { Location, Location + "-wal", Location + "-shm", Location + ".lock" })
                if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("File dữ liệu chat không được là liên kết.");
            Directory.CreateDirectory(parent);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(parent, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            // Một Hub sở hữu DB để startup recovery không đánh dấu nhầm lượt của Hub khác.
            _lease = new FileStream(Location + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = IsDemo ? "chat-" + Guid.NewGuid().ToString("N") : Location,
            Mode = IsDemo ? SqliteOpenMode.Memory : SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, Pooling = false, ForeignKeys = true, DefaultTimeout = 5
        }.ToString();
        if (IsDemo) { _anchor = new SqliteConnection(_connectionString); _anchor.Open(); }
        try
        {
        using var db = Open();
        using (var version = db.CreateCommand())
        {
            version.CommandText = "PRAGMA user_version";
            if (Convert.ToInt32(version.ExecuteScalar(), CultureInfo.InvariantCulture) > 2)
                throw new InvalidDataException("Dữ liệu chat thuộc phiên bản mới hơn. Không ghi đè.");
        }
        Execute(db, """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS conversations(id TEXT PRIMARY KEY, title TEXT NOT NULL, updated TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS messages(
                id INTEGER PRIMARY KEY AUTOINCREMENT, conversation TEXT NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
                role TEXT NOT NULL, text TEXT NOT NULL, status TEXT NOT NULL, at TEXT NOT NULL, model TEXT NOT NULL DEFAULT '',
                tokens INTEGER, error TEXT, request_key TEXT UNIQUE, reply_to INTEGER REFERENCES messages(id) ON DELETE CASCADE,
                config TEXT NOT NULL DEFAULT '{}');
            CREATE INDEX IF NOT EXISTS ix_chat_messages ON messages(conversation,id);
            CREATE TABLE IF NOT EXISTS chat_settings(id INTEGER PRIMARY KEY CHECK(id=1), json TEXT NOT NULL);
            UPDATE messages SET status='error', error='Phản hồi bị gián đoạn khi Hub dừng. Anh có thể gửi lại.' WHERE status IN ('queued','generating');
            """);
        using (var columns = Command(db, "PRAGMA table_info(messages)"))
        {
            using var reader = columns.ExecuteReader(); var hasConfig = false;
            while (reader.Read()) if (reader.GetString(1) == "config") hasConfig = true;
            reader.Close();
            if (!hasConfig) Execute(db, "ALTER TABLE messages ADD COLUMN config TEXT NOT NULL DEFAULT '{}'");
        }
        Execute(db, "PRAGMA user_version=2");
        if (!IsDemo && !OperatingSystem.IsWindows()) File.SetUnixFileMode(Location, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch { _anchor?.Dispose(); _lease?.Dispose(); throw; }
    }

    private SqliteConnection Open() { var db = new SqliteConnection(_connectionString); db.Open(); return db; }
    private static SqliteCommand Command(SqliteConnection db, string sql, params (string Key, object? Value)[] parameters)
    {
        var cmd = db.CreateCommand(); cmd.CommandText = sql;
        foreach (var (key, value) in parameters) cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return cmd;
    }
    private static void Execute(SqliteConnection db, string sql, params (string Key, object? Value)[] parameters)
    { using var cmd = Command(db, sql, parameters); cmd.ExecuteNonQuery(); }
    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    private static void ValidateId(string id)
    { if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Hội thoại không hợp lệ."); }
    private static void RequireConversation(SqliteConnection db, string id)
    {
        ValidateId(id);
        using var cmd = Command(db, "SELECT count(*) FROM conversations WHERE id=$id", ("$id", id));
        if ((long)cmd.ExecuteScalar()! == 0) throw new KeyNotFoundException("Không tìm thấy hội thoại.");
    }

    public ChatSettings Settings()
    {
        lock (_gate)
        {
            using var db = Open(); using var cmd = Command(db, "SELECT json FROM chat_settings WHERE id=1");
            var json = cmd.ExecuteScalar() as string;
            var settings = json == null ? new ChatSettings() : JsonSerializer.Deserialize<ChatSettings>(json)
                ?? throw new InvalidDataException("Cấu hình chat bị hỏng; không ghi đè.");
            settings.Validate(); return settings;
        }
    }
    public void SaveSettings(ChatSettings settings)
    {
        settings.Validate();
        var clean = settings with { Instructions = SecretRedactor.Redact(settings.Instructions).Text };
        lock (_gate)
        {
            using var db = Open();
            Execute(db, "INSERT INTO chat_settings(id,json) VALUES(1,$json) ON CONFLICT(id) DO UPDATE SET json=excluded.json", ("$json", JsonSerializer.Serialize(clean)));
        }
    }

    public string CreateConversation()
    {
        lock (_gate)
        {
            using var db = Open();
            using var count = Command(db, "SELECT count(*) FROM conversations");
            if ((long)count.ExecuteScalar()! >= 500) throw new InvalidOperationException("Đã có 500 hội thoại. Xuất và xóa hội thoại cũ trước khi tạo thêm.");
            var id = Guid.NewGuid().ToString("N");
            Execute(db, "INSERT INTO conversations VALUES($id,'Cuộc trò chuyện mới',$at)", ("$id", id), ("$at", Now()));
            return id;
        }
    }
    public IReadOnlyList<ChatConversation> Conversations()
    {
        lock (_gate)
        {
            using var db = Open(); using var cmd = Command(db, """
                SELECT id,title,updated,EXISTS(SELECT 1 FROM messages m WHERE m.conversation=c.id AND m.status IN ('queued','generating'))
                FROM conversations c ORDER BY updated DESC LIMIT 500
                """);
            using var r = cmd.ExecuteReader(); var rows = new List<ChatConversation>();
            while (r.Read()) rows.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetBoolean(3)));
            return rows;
        }
    }
    public IReadOnlyList<ChatMessage> Messages(string id, bool all = false)
    {
        lock (_gate)
        {
            using var db = Open(); RequireConversation(db, id);
            using var cmd = Command(db, "SELECT id,role,text,status,at,model,tokens,error FROM (SELECT * FROM messages WHERE conversation=$id ORDER BY id DESC LIMIT $limit) ORDER BY id",
                ("$id", id), ("$limit", all ? 2000 : 200));
            using var r = cmd.ExecuteReader(); var rows = new List<ChatMessage>();
            while (r.Read()) rows.Add(new(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.IsDBNull(6) ? null : r.GetInt64(6), r.IsDBNull(7) ? null : r.GetString(7)));
            return rows;
        }
    }

    public long Enqueue(string id, string text, string key)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 8000) throw new ArgumentException("Nội dung cần từ 1 đến 8000 ký tự.");
        ValidateId(key);
        text = SecretRedactor.Redact(text.Trim()).Text;
        lock (_gate)
        {
            var settings = Settings();
            using var db = Open(); using var tx = db.BeginTransaction(); RequireConversation(db, id);
            using var existing = Command(db, "SELECT id,conversation,text FROM messages WHERE request_key=$key", ("$key", key));
            using (var r = existing.ExecuteReader())
                if (r.Read())
                {
                    if (r.GetString(1) != id) throw new ArgumentException("Mã gửi đã được dùng trong hội thoại khác.");
                    if (r.GetString(2) != text) throw new ArgumentException("Nội dung đã đổi so với lần gửi trước. Anh gửi lại bằng mã mới.");
                    return r.GetInt64(0);
                }
            using var busy = Command(db, "SELECT count(*) FROM messages WHERE conversation=$id AND status IN ('queued','generating')", ("$id", id));
            if ((long)busy.ExecuteScalar()! > 0) throw new InvalidOperationException("AI đang trả lời. Chờ hoặc bấm Dừng trước khi gửi tiếp.");
            using var count = Command(db, "SELECT count(*) FROM messages WHERE conversation=$id", ("$id", id));
            if ((long)count.ExecuteScalar()! >= 2000) throw new InvalidOperationException("Hội thoại đã đủ 1000 lượt. Tạo hội thoại mới để tiếp tục.");
            using var queued = Command(db, "SELECT count(*) FROM messages WHERE status IN ('queued','generating')");
            if ((long)queued.ExecuteScalar()! >= 20) throw new InvalidOperationException("Hàng đợi đã đầy. Anh thử lại sau.");
            var now = Now();
            Execute(db, "INSERT INTO messages(conversation,role,text,status,at,request_key) VALUES($id,'user',$text,'complete',$at,$key)", ("$id", id), ("$text", text), ("$at", now), ("$key", key));
            using var last = Command(db, "SELECT last_insert_rowid()"); var userId = (long)last.ExecuteScalar()!;
            Execute(db, "INSERT INTO messages(conversation,role,text,status,at,model,reply_to,config) VALUES($id,'assistant','','queued',$at,$model,$user,$config)",
                ("$id", id), ("$at", now), ("$model", settings.Model), ("$user", userId), ("$config", JsonSerializer.Serialize(settings)));
            Execute(db, "UPDATE conversations SET title=CASE WHEN title='Cuộc trò chuyện mới' THEN $title ELSE title END,updated=$at WHERE id=$id", ("$title", text[..Math.Min(70, text.Length)]), ("$at", now), ("$id", id));
            tx.Commit(); return userId;
        }
    }

    public ChatTurn? Claim()
    {
        lock (_gate)
        {
            using var db = Open(); using var tx = db.BeginTransaction();
            using var cmd = Command(db, "SELECT id,conversation,reply_to,config FROM messages WHERE status='queued' ORDER BY id LIMIT 1");
            long message, user; string conversation; ChatSettings settings;
            using (var r = cmd.ExecuteReader()) { if (!r.Read()) return null; message = r.GetInt64(0); conversation = r.GetString(1); user = r.GetInt64(2); settings = JsonSerializer.Deserialize<ChatSettings>(r.GetString(3)) ?? new(); }
            Execute(db, "UPDATE messages SET status='generating' WHERE id=$id", ("$id", message));
            // Chỉ các cặp đã hoàn tất + câu hỏi hiện tại. Không đưa lượt lỗi vào ngữ cảnh.
            using var history = Command(db, """
                SELECT m.id,m.role,m.text,m.status,m.at,m.model,m.tokens,m.error FROM messages m
                WHERE m.conversation=$c AND m.id<=$current AND
                (m.id=$user OR (m.role='assistant' AND m.status='complete') OR
                 (m.role='user' AND EXISTS(SELECT 1 FROM messages a WHERE a.reply_to=m.id AND a.status='complete')))
                ORDER BY m.id DESC LIMIT 41
                """, ("$c", conversation), ("$current", message), ("$user", user));
            using var reader = history.ExecuteReader(); var context = new List<ChatMessage>(); var chars = 0;
            while (reader.Read())
            {
                var text = reader.GetString(2); if (chars + text.Length > 48000) break; chars += text.Length;
                context.Add(new(reader.GetInt64(0), reader.GetString(1), text, reader.GetString(3), reader.GetString(4), reader.GetString(5), null, null));
            }
            reader.Close(); context.Reverse(); tx.Commit(); return new(message, conversation, context, settings);
        }
    }

    public void Finish(long id, string text, string? error, long? tokens)
    {
        lock (_gate)
        {
            using var db = Open();
            var safe = SecretRedactor.Redact(text).Text;
            Execute(db, "UPDATE messages SET text=$text,error=$error,tokens=$tokens,status=$status WHERE id=$id AND status='generating'",
                ("$text", safe[..Math.Min(64000, safe.Length)]), ("$error", error), ("$tokens", tokens), ("$status", error == null ? "complete" : "error"), ("$id", id));
        }
    }
    public void Cancel(string id)
    {
        lock (_gate)
        {
            using var db = Open(); RequireConversation(db, id);
            Execute(db, "UPDATE messages SET status='cancelled',error='Đã dừng theo yêu cầu.' WHERE conversation=$id AND status IN ('queued','generating')", ("$id", id));
        }
    }
    public bool IsGenerating(long id)
    {
        lock (_gate)
        {
            using var db = Open(); using var cmd = Command(db, "SELECT status FROM messages WHERE id=$id", ("$id", id));
            return cmd.ExecuteScalar() as string == "generating";
        }
    }
    public void Delete(string id)
    {
        lock (_gate)
        {
            using var db = Open(); RequireConversation(db, id);
            using var busy = Command(db, "SELECT count(*) FROM messages WHERE conversation=$id AND status IN ('queued','generating')", ("$id", id));
            if ((long)busy.ExecuteScalar()! > 0) throw new InvalidOperationException("Dừng phản hồi trước khi xóa hội thoại.");
            Execute(db, "DELETE FROM conversations WHERE id=$id", ("$id", id));
        }
    }
    public void Dispose() { _anchor?.Dispose(); _lease?.Dispose(); }
}
