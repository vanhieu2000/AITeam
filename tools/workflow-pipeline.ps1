# Khung V8-02 — chỉ điền phần thân, giữ hợp đồng nghiệm thu.
# /// <summary>Đọc plan UTF-8 (BOM tùy bản cũ), giữ nguyên file. Chỉ chấp nhận ĐÃ DUYỆT
# /// trong mục Quyết định của quản lý, không có TỪ CHỐI tại mục đó; nếu không, ném
# /// "Kế hoạch chưa được duyệt.". Trả mảng {Id, Title, DependsOn:string[]} theo thứ tự plan.
# /// Mã theo ^[A-Za-z0-9]+-\d+[a-z]?$, duy nhất không phân biệt hoa thường; phụ thuộc
# /// phải có trong plan, không tự tham chiếu/chu trình. Sai ném "Kế hoạch có phụ thuộc không hợp lệ.".
# /// Không thực thi Markdown; chặn reparse point ở file/các thư mục cha trước đọc,
# /// ném "Đường dẫn kế hoạch không an toàn.".</summary>
function Read-WorkflowPipelinePlan {
    param([string]$PlanPath)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Trả các task chưa có file (TaskExists nhận Id, trả bool), mọi phụ thuộc
# /// có đúng hàng mã task trong BoardText với ✅ Đã merge; không chấp nhận tiền tố mã,
# /// Chờ review/PASS hoặc chỉ ✅. BoardText do adapter đọc origin/main, không đọc bản local.
# /// Không có phụ thuộc thì sẵn sàng. Không sửa Tasks/BoardText hoặc gọi AI.</summary>
function Get-WorkflowReadyTasks {
    param([object[]]$Tasks, [string]$BoardText, [scriptblock]$TaskExists)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Khóa liên tiến trình chung cho toàn bộ Codex trên máy, không theo repo/task.
# /// Chạy Action đúng một lần, trả nguyên kết quả, nhả trong finally kể cả Action ném lỗi
# /// hoặc chủ khóa trước chết (abandoned mutex). Hết TimeoutSeconds ném "Hết thời gian chờ Codex.".
# /// Không giữ khóa giao dịch repo trong khi chờ. Prep/QC/accept dùng cùng hàm này.</summary>
function Invoke-WorkflowCodexLock {
    param([scriptblock]$Action, [int]$TimeoutSeconds = 1800)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Một worker/task: Prep dưới khóa Codex trả {Ok}; Agent nhận Resume:boolean,
# /// Feedback:string và trả {Ok, Quota}; Verify trả {Ok, Feedback}; Qc dưới cùng khóa Codex
# /// trả {Verdict:PASS|CHANGES_REQUESTED|BLOCKED, Feedback}. Mỗi callback chạy đồng bộ.
# /// Agent chỉ Antigravity; quota/lỗi/Prep thất bại thì dừng. Verify trượt giao lại tối đa 1,
# /// QC CHANGES_REQUESTED giao lại tối đa 1; hai bộ đếm riêng không reset, tối đa 3 lượt agent.
# /// QC chỉ sau Verify đạt; BLOCKED/kết quả thiếu hoặc sai dừng, không báo PASS.
# /// Trả {Id, Status} với Status: PASS|VERIFY_FAILED|QC_FAILED|BLOCKED|QUOTA|ERROR.
# /// Callback ném lỗi trả ERROR và nhả khóa, không làm worker khác thất bại.</summary>
function Invoke-WorkflowPipelineTask {
    param([string]$TaskId, [scriptblock]$Prep, [scriptblock]$Agent,
          [scriptblock]$Verify, [scriptblock]$Qc)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}

# /// <summary>Lõi điều phối: kiểm RequestId ^R-\d{3}$ (sai: "Thiếu / sai -Request (vd. R-006)")
# /// và toàn bộ plan trước callback. Khóa liên tiến trình ProjectKey+RequestId; lượt trùng
# /// trả rỗng, không gọi callback. Cố định SHA256 plan, đổi giữa lượt thì dừng khởi chạy mới.
# /// ReadBoard trả văn bản bảng việc origin/main mới; TaskExists nhận Id, trả bool.
# /// StartTask nhận task, trả System.Diagnostics.Process đã Start, redirect log riêng, không AI thật trong test.
# /// Tối đa 2 tiến trình đồng thời, đợi lượt đang chạy rồi đọc lại bảng và xét tiếp.
# /// Mỗi Id chỉ khởi chạy 1 lần; task có sẵn trả EXISTING; thiếu merge trả WAITING;
# /// exit 0 trả DONE, khác 0 trả ERROR. Trả đúng một {Id,Status} mỗi task theo thứ tự plan,
# /// không phát log callback ra console. Không merge, không tự assign task có file.
# /// Nhả khóa trong finally; child lỗi không làm mất kết quả task khác.</summary>
function Invoke-WorkflowPipeline {
    param([string]$PlanPath, [string]$ProjectKey, [string]$RequestId,
          [scriptblock]$ReadBoard, [scriptblock]$TaskExists, [scriptblock]$StartTask)
    throw [System.NotImplementedException]::new(); # TODO(agent)
}
