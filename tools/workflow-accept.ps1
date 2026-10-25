# Khung V8-03 — Antigravity điền phần thân, không đổi hợp đồng nghiệm thu.
# /// <summary>Đọc các file TaskId.accept-N.json (N nguyên dương), không khớp tiền tố gần giống.
# /// Envelope mới: {task,head,run,verify:{ok,source},qc:{verdict,findings,source},metrics:{source}}.
# /// findings: {id,severity,open,classification}; classification chỉ Optional do quản lý truyền
# /// qua -Optional mới cho phép hoãn Required; Blocker mở luôn chặn. Không tin phân loại AI tự ghi.
# /// Chọn run lớn nhất khớp task chính xác và SHA đầy đủ Head; cùng lượt mâu thuẫn ⇒ không rõ.
# /// Đọc metrics.source JSON theo mẫu thật, kiểm project/task/run/head trước lấy durationSec/60,
# /// totalTokens; tên task là mã hoặc mã-slug có ranh giới, không nhận V8-030 cho V8-03.
# /// Trả {TaskId,Head,Run,VerifyOk,Verdict,Findings,Minutes,Tokens,Sources,Score}.
# /// Thiếu/hỏng/log cũ không SHA ⇒ VerifyOk=$false, Verdict/Minutes/Tokens/Score='không rõ'.
# /// Sources giữ đường dẫn nguồn; điểm chỉ lấy từ nguồn có thật, không suy ra từ PASS.
# /// Chặn .., tuyệt đối và reparse point trong source; không ghi file, không gọi tiến trình.
# /// Lỗi đọc nguồn chỉ làm trường tương ứng không rõ, không ném lỗi đọc log.</summary>
function Get-WorkflowAcceptEvidence {
    param([string]$TaskId, [string]$Head, [string]$LogsPath, [string]$MetricsPath)
    throw [System.NotImplementedException]::new() # TODO(agent)
}

# /// <summary>Items đã lọc theo Request và bảng việc chờ nghiệm thu, theo thứ tự plan.
# /// Mỗi item {Id,Evidence,Added,Budget,Changes,DependsOn}; Changes là đường dẫn tương đối.
# /// Trả string[]: KT/QC, từng finding mở, Added/Budget (Added đã loại khung/test nghiệm thu),
# /// UI: /TênTrang từ hub/Pages/*.cshtml(.cs); wwwroot/shared không xác định ⇒ 'không rõ'.
# /// Không UI ghi 'Không'; không có item trả đúng 'Không có task chờ nghiệm thu.'.
# /// Không bỏ finding để đủ 40 dòng: gói quá 40 dòng ghi rõ vượt giới hạn vì giữ đủ bằng chứng.
# /// Thiếu dữ liệu ghi 'không rõ', không thay null bằng 0; không ghi/commit/fetch/chạy AI.</summary>
function Get-WorkflowAcceptPack {
    param([object[]]$Items)
    throw [System.NotImplementedException]::new() # TODO(agent)
}

# /// <summary>NumStat là các dòng git diff --numstat từ commit khung tới HEAD (adapter lấy seed
# /// đúng như Invoke-Verify, nếu không có seed dùng merge-base). Cộng số thêm mới cột đầu;
# /// bỏ TaskFile và AcceptanceFiles đúng đường dẫn, bỏ dòng nhị phân '-', không đọc metrics.linesAdded.
# /// Trả int, rỗng trả 0. Không tự chạy git hoặc đổi file.</summary>
function Get-WorkflowAcceptLineCount {
    param([string[]]$NumStat, [string]$TaskFile, [string[]]$AcceptanceFiles)
    throw [System.NotImplementedException]::new() # TODO(agent)
}

# /// <summary>Before/After là dictionary đường dẫn → nội dung UTF-8 đã đọc nghiêm ngặt.
# /// Chỉ file TaskFile được đổi, chỉ nội dung ba mục ## Review (Claude), ## Kết quả,
# /// ## Đánh giá quy trình; tiêu đề, báo cáo, test, code và mọi byte ngoài mục phải nguyên.
# /// Không cho xóa mục, thêm file, đổi hoa thường đường dẫn hay chèn heading ## ngoài ba mục.
# /// Sai ném đúng 'Codex sửa ngoài mục nghiệm thu cho phép.'. Hợp lệ trả true.
# /// Caller còn kiểm BOM/CRLF byte gốc; không reset --hard/clean hay ghi đè sửa của người dùng.</summary>
function Test-WorkflowAcceptDocuments {
    param([System.Collections.IDictionary]$Before, [System.Collections.IDictionary]$After, [string]$TaskFile)
    throw [System.NotImplementedException]::new() # TODO(agent)
}

# /// <summary>Context={TaskId,Head,TaskFile,Files,Merged}; Files là snapshot sạch của nhánh task.
# /// Merged trả {Status='ALREADY_MERGED'} trước mọi callback, không thêm vòng.
# /// ReadEvidence nhận TaskId,Head. ĐẠT cần VerifyOk và QC PASS cùng Head, Sources đầy đủ;
# /// Required mở chưa được quản lý phân loại Optional, Blocker mở, thiếu/sai SHA ⇒ ném
# /// 'Chưa đủ bằng chứng nghiệm thu.'. Optional là chuỗi ID finding được hoãn kèm lý do;
# /// không tự chuyển mọi finding thành Optional. Reject khác rỗng bỏ luồng ĐẠT, không cần KT đạt.
# /// WriteReview nhận decision={Round,Verdict,Optional,Note,Reject,Evidence}, trả Files sau sửa;
# /// Round=max vòng trong riêng Review +1, Verdict=ĐẠT/CẦN SỬA. Gọi dưới Invoke-WorkflowCodexLock.
# /// Kiểm diff bằng Test-WorkflowAcceptDocuments; kiểm SHA vẫn Context.Head trước Publish.
# /// Publish nhận Files, trả {Ok,Head} sau commit/push chỉ tài liệu nhánh task; lỗi thì dừng.
# /// ĐẠT: CheckApproval nhận SHA SAU commit tài liệu, phải true; rồi Merge nhận SHA này,
# /// trả {Ok}; false/ngoại lệ ⇒ ném 'Không hoàn tất nghiệm thu.', không ghi nhật ký thành công.
# /// Reject: Publish xong mới Resume nhận $true,Reject; không CheckApproval/Merge.
# /// Trả {Status='MERGED'|'REJECTED',Head}; giữ snapshot/không phá dữ liệu khi lỗi.
# /// Callback giả là ranh giới I/O cho test; adapter thật gọi git/AI hữu hạn, không nhận diện test.</summary>
function Invoke-WorkflowAccept {
    param($Context, [scriptblock]$ReadEvidence, [scriptblock]$WriteReview,
          [scriptblock]$Publish, [scriptblock]$CheckApproval, [scriptblock]$Merge,
          [scriptblock]$ResumeTask, [string]$Optional = '', [string]$Note = '', [string]$Reject = '')
    throw [System.NotImplementedException]::new() # TODO(agent)
}
