namespace AITeam.Core.Tests.Support;

/// <summary>Đáp ứng giả cho agy/codex/git (không phân biệt hoa thường); mặc định code 0, output/error rỗng.</summary>
public sealed record FakeCommand(string Name, int Code = 0, string Output = "", string Error = "");

/// <summary>Một lần gọi giả, giữ từng đối số nguyên vẹn và kết quả đã cấu hình, theo đúng thứ tự gọi.</summary>
public sealed record FakeCall(string Name, string[] Arguments, int Code, string Output, string Error);

/// <summary>Kết quả tiến trình và nhật ký gọi giả, đọc được cả khi PowerShell trả lỗi.</summary>
public sealed record TeamScriptResult(TestShell.Result Process, IReadOnlyList<FakeCall> Calls);

/// <summary>Sandbox cho script tin cậy trong test; không phải bộ cách ly mã PowerShell độc hại hay đường dẫn exe tường minh.</summary>
public sealed class TeamScript : IDisposable
{
    /// <summary>
    /// Tạo TempRepo(git: false), chép team.ps1 bắt buộc và toàn bộ tools/, scripts/, templates/
    /// nếu có, giữ cây/tên/byte. sourceRoot null dùng cùng cách tìm gốc với RealFile;
    /// sourceRoot khác null phải là thư mục tuyệt đối. Thư mục tùy chọn thiếu bỏ qua.
    /// Chặn reparse point trong nguồn/cây đích và cha trước khi đọc/ghi:
    /// ArgumentException("Đường dẫn bản sao không hợp lệ."). Thiếu team.ps1:
    /// FileNotFoundException("Không tìm thấy file nguồn."). Lỗi khởi tạo phải tự dọn sandbox.
    /// </summary>
    public TeamScript(string? sourceRoot = null)
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <summary>Đường dẫn tuyệt đối của sandbox riêng; không phải nguồn thật.</summary>
    public string Root
    {
        get { throw new NotImplementedException(); } // TODO(agent)
    }

    /// <summary>
    /// Dùng TestShell.PowerShell(Root, ..., timeoutSeconds). ParseFile bằng AST, chỉ nạp
    /// FunctionDefinitionAst cấp script có tên được chọn, không dot-source cả file,
    /// không chạy dispatcher/param/khởi tạo/dot-source ngoài hàm; không tự nạp phụ thuộc.
    /// $Root và $PSScriptRoot của hàm trỏ tới sandbox/thư mục script đã chép.
    /// relativeScript chịu cùng kiểm đường dẫn như RealFile nhưng tính từ Root.
    /// Cài lại fake SAU khi nạp hàm: agy/codex/git và alias .exe luôn bị chặn;
    /// mọi git đều giả (bao gồm push), không khởi chạy git/AI thật.
    /// Ghi từng đối số và code/output/error; đặt LASTEXITCODE, stdout/stderr theo fake.
    /// fakes null dùng mặc định; chỉ nhận tên agy/codex/git, không trùng (bỏ qua hoa thường).
    /// script là đoạn test tin cậy, chạy sau khi nạp; mọi lần Run độc lập, log riêng.
    /// AST lỗi trả Code 1, stderr chứa "LỖI: cú pháp PowerShell không hợp lệ.";
    /// thiếu/trùng tên hàm trả Code 1, stderr chứa "LỖI: hàm PowerShell không hợp lệ.";
    /// lỗi script trả Code 1 và stderr "LỖI: " + thông điệp lỗi, vẫn giữ Calls.
    /// Tham số functionNames rỗng/null, tên trắng/trùng, fake không hợp lệ, timeout &lt;= 0:
    /// ArgumentException("Tham số sandbox không hợp lệ."). Quá hạn: TimeoutException
    /// từ TestShell, dừng cây tiến trình. File thiếu: FileNotFoundException như RealFile.
    /// </summary>
    public Task<TeamScriptResult> RunAsync(string relativeScript, string[] functionNames,
        string script, FakeCommand[]? fakes = null, int timeoutSeconds = 60)
    {
        throw new NotImplementedException(); // TODO(agent)
    }

    /// <summary>Dọn TempRepo và toàn bộ file sinh trong sandbox, kể cả chỉ đọc; gọi lại không lỗi, không đụng nguồn.</summary>
    public void Dispose()
    {
        throw new NotImplementedException(); // TODO(agent)
    }
}
