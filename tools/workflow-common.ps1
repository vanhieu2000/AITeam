# Khung V8-01: hợp đồng đã chốt; agent chỉ điền phần TODO.
# /// <summary>Chỉ trả true khi Code khác 0, mọi dòng lỗi đều thuộc danh sách hạ tầng.
# /// Build/Test: MSB3491 kèm denied/being used; Access to the path '…' is denied;
# /// The process cannot access the file '…' because it is being used by another process.
# /// MCP: chỉ dòng chính xác MCP transport error: Connection reset by peer (os error 10054).
# /// Lỗi code, warning code (trừ NU1900), test fail/hang, lỗi khác hoặc output rỗng luôn false.</summary>
function Test-InfrastructureFailure {
    param([ValidateSet('Build', 'Test', 'Mcp')][string]$Stage, [int]$Code, [string]$Output)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Gọi Run với số lần 1; chỉ lỗi hạ tầng mới gọi lại với số lần 2, không lần 3.
# /// Run trả {Code,Output}; trả {Code,Output,Attempts}, Output cuối giữ nguyên,
# /// Attempts là mảng {Number,Code,Output} giữ nguyên cả stdout/stderr đã hợp nhất.
# /// Không bỏ qua lỗi của lần 2; ngoại lệ callback truyền ra, không biến thành đạt.</summary>
function Invoke-WorkflowCheck {
    param([ValidateSet('Build', 'Test', 'Mcp')][string]$Stage, [scriptblock]$Run)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Gọi Verify một lần; chỉ khi Verify.Ok mới gọi Qc một lần, kể cả đã phục hồi lỗi máy.
# /// Trả {Verify,Review}; Review null khi trượt hoặc QC chủ động bỏ qua.
# /// Không giữ khóa repo trong Verify/Qc; ngoại lệ truyền ra để điểm vào CLI bắt.</summary>
function Invoke-WorkflowGate {
    param([scriptblock]$Verify, [scriptblock]$Qc)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Khóa liên tiến trình theo đường dẫn repo chuẩn hóa, không phân biệt hoa thường trên Windows.
# /// Cùng repo dùng chung khóa cho bảng việc/metrics/nhật ký, repo khác độc lập.
# /// Gọi Action và trả output; luôn nhả khóa khi lỗi; tái nhập cùng tiến trình được phép.
# /// Hết TimeoutSeconds báo 'Không lấy được khóa giao dịch repo.'; không gọi AI trong Action.</summary>
function Invoke-RepoTransaction {
    param([string]$RepoPath, [scriptblock]$Action, [int]$TimeoutSeconds = 30)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Dưới khóa repo, đọc tasks/TaskFile (chỉ tên file .md trực tiếp, không wildcard),
# /// lấy **Yêu cầu:** R-nnn và thêm đúng một dòng vào ## Nhật ký của requests/R-nnn.md.
# /// Dòng: - yyyy-MM-dd HH:mm:ss zzz | TaskId | Event | lượt Run | Result &lt;!-- workflow:TaskId:Event:Run --&gt;
# /// Event chỉ Prep/Assign/Verify/Qc/Merge; cùng TaskId/Event/Run bỏ trùng dù Result khác.
# /// Chèn trước heading ## kế tiếp, giữ nguyên phần khác, BOM và xuống dòng của phiếu.
# /// Thiếu Yêu cầu ở task cũ trả false và không sửa file. Thành công mới true; trùng trả false.
# /// Thiếu/hỏng task hoặc phiếu, ID không hợp lệ, nhiều Nhật ký, thiếu Nhật ký, UTF-8 hỏng,
# /// đường dẫn tuyệt đối/../reparse point ở bất cứ thành phần nào: ném 'Không ghi được nhật ký phiếu.'
# /// Không tạo/thay phiếu hỏng; TaskId/Event/Result không nhận CR/LF, dấu | hoặc HTML để chống chèn dòng.
# /// Hàm chỉ sửa file, caller commit cùng bảng việc dưới cùng khóa; không gọi git/AI tại đây.</summary>
function Update-WorkflowJournal {
    param([string]$RepoPath, [string]$TaskFile, [string]$TaskId,
        [ValidateSet('Prep', 'Assign', 'Verify', 'Qc', 'Merge')][string]$Event,
        [int]$Run, [string]$Result, [datetimeoffset]$At = [datetimeoffset]::Now)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}
