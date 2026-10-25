# Kế hoạch R-008
## Hiểu yêu cầu
Giảm thao tác của Claude xuống brief → duyệt plan → xem gói nghiệm thu → quyết định từng task.
Bổ sung điều phối tự động, thử lại lỗi máy, tổng hợp nghiệm thu và ghi nhật ký phiếu.
Tái dùng prep / assign / verify / QC / merge; không sửa giao diện Hub hoặc hợp đồng dữ liệu.
Chia 3 task lõi và đầu ra CLI; mỗi task ≤400 dòng triển khai, dự kiến ≤15 phút sau khi Codex dựng khung và test.

## Research
- `projects.json:23`: areas AITeam chỉ giao A phần dịch vụ/test Hub, B phần giao diện; chưa phân công `team.ps1` và `tools/`.
- `team.ps1:819`: prep đã tách task từ heading plan; `:826` chặn task có file; `:836` tiếp tục worktree chuẩn bị dở.
- `team.ps1:913`: prep có `-NoLaunch`, dùng được để tách bước Codex khỏi bước Antigravity.
- `team.ps1:441`: assign gọi verify; `:444` tự giao lại khi trượt; `:458` chỉ QC sau verify đạt; `:464` có giới hạn giao lại do QC.
- `team.ps1:871`: prep đã thử build lại khi khóa file; verify tại `:1282` chưa có nhánh thử lại tương ứng.
- `team.ps1:611`: QC kiểm tra kết luận JSON khớp mã thoát; cần giữ cơ chế coi kết quả không hợp lệ là BLOCKED.
- `team.ps1:1360`, `:1453`: bảng việc và metrics tự commit/push trên repo chung; chạy song song cần khóa các giao dịch Git này.
- `team.ps1:1418`: số dòng đã loại commit khung; `:1438` có thời gian/token; `:1532` có gói review nhưng chỉ cho một task.
- `team.ps1:1596`: merge có cổng duyệt theo SHA; `:1625` yêu cầu dòng review ĐẠT; `:1648` cập nhật bảng việc sau merge.
- `templates/task.md:80`, `:85`, `:88`: có sẵn ba mục cần điền khi nghiệm thu.
- `templates/request.md:22`: Nhật ký là bảng Giờ / Sự kiện / Ghi chú; liên kết phiếu qua `Yêu cầu:` tại `templates/task.md:7`.
- `plans/R-007/plan.md:124`: quy ước duyệt là mục “Quyết định của quản lý” chứa “ĐÃ DUYỆT”, không chứa “TỪ CHỐI”.
- `tools/test-verify.ps1:23`: đã có repo tạm, remote bare và worktree; `:75` kiểm tra biến cục bộ đè tham số PowerShell.
- `core.tests/AcceptanceV501McpTests.cs:116`: có test MCP stdio thật; `requests/R-006-quy-trinh-toi-uu-token-claude-y-tuong-re.md:32` ghi nhận test MCP chập chờn.
- CHƯA XÁC MINH: chữ ký lỗi MCP hạ tầng cụ thể, định dạng mọi log cũ và khả năng thực thi; bước này chỉ đọc, không chạy build/test.

## Rủi ro
- Plan hoặc tham số chứa đường dẫn/lệnh độc hại → kiểm tra mã, chuẩn hóa đường dẫn, chặn reparse point và truyền đối số riêng; không thực thi nội dung Markdown.
- Hai tiến trình ghi main làm mất nhật ký hoặc bảng việc → khóa liên tiến trình toàn bộ giao dịch đọc–sửa–commit–push, nhả khóa trong finally.
- Chạy trùng pipeline hoặc đọc plan vừa bị đổi → khóa theo dự án/phiếu, cố định hash plan đã duyệt, không chuẩn bị task hai lần.
- Nhận nhầm lỗi code là lỗi máy → phân loại từng lỗi, chỉ thử lại một lần; mọi kiểm tra vẫn phải đạt, không bỏ test MCP.
- Log cũ thiếu/hỏng hoặc QC thuộc commit trước → hiển thị “không rõ”; chặn nghiệm thu tự động khi bằng chứng không khớp SHA.
- Codex điền tài liệu làm đổi SHA đã được duyệt merge → giữ cổng duyệt hiện có; yêu cầu duyệt lại nếu SHA không còn khớp.
- PowerShell 5.1 hoặc dữ liệu cũ bị hỏng → giữ BOM/xuống dòng, không dùng cú pháp PS7, không thay log hỏng bằng dữ liệu rỗng để ghi đè.
- Vòng tự giao lại cộng dồn giữa cổng KT và QC → bộ đếm riêng, giới hạn rõ; thử lại lỗi máy không tính là lượt sửa code.

## Task
### V8-01 · Thử lại lỗi máy và ghi nhật ký
- **Agent:** A
- **Ngân sách:** ~320 dòng
- **Phạm vi:** `team.ps1` · `tools/workflow-common.ps1` · `tools/test-verify.ps1`
- **Làm:**
  - Giữ output build/test gốc để phân loại MSB3491, lỗi khóa file “Access … denied”; MCP chỉ thử lại khi khớp ca/chữ ký hạ tầng được Codex chốt trong khung.
  - Thử lại phần build/test gặp lỗi máy đúng một lần; lưu cả hai lần và kiểm tra lại kết quả đầy đủ; đạt sau thử lại vẫn gọi QC.
  - Tạo khóa giao dịch repo dùng chung cho cập nhật bảng việc, metrics và nhật ký; không giữ khóa trong lúc chờ AI.
  - Prep xong / assign xong / mỗi kết quả cổng KT / QC / merge → thêm dòng có giờ, task, lượt và kết quả vào đúng mục Nhật ký; commit cùng cập nhật bảng việc tương ứng.
  - Chống ghi trùng theo sự kiện/lượt; task cũ thiếu `Yêu cầu:` vẫn chạy; phiếu thiếu/hỏng báo “Không ghi được nhật ký phiếu.” và không ghi đè.
  - Giữ hành vi lệnh cũ ngoài phần thử lại/nhật ký được brief yêu cầu; giữ nguyên test nghiệm thu Codex viết trước.
- **Test nghiệm thu:** lỗi máy rồi đạt; lỗi máy hai lần; lỗi hỗn hợp/code không bị bỏ qua; MCP vẫn lỗi thì trượt; QC được gọi sau phục hồi; đủ năm sự kiện; nhật ký hai tiến trình không mất dòng; thiếu phiếu, dữ liệu cũ, BOM, đường dẫn thoát/reparse point; toàn bộ dùng repo tạm và tiến trình giả.
- **Phụ thuộc:** —

### V8-02 · Điều phối pipeline theo phụ thuộc
- **Agent:** A
- **Ngân sách:** ~380 dòng
- **Phạm vi:** `team.ps1` · `tools/workflow-pipeline.ps1` · `tools/test-verify.ps1`
- **Làm:**
  - Thêm `pipeline -Request R-xxx`; đọc cấu trúc plan hiện có và quy ước duyệt đã kiểm chứng; chưa duyệt báo “Kế hoạch chưa được duyệt.”.
  - Kiểm tra mã trùng, phụ thuộc không tồn tại, chu trình trước khi chạy; báo “Kế hoạch có phụ thuộc không hợp lệ.”; phụ thuộc chỉ sẵn sàng khi bảng việc origin/main xác nhận ✅ đã merge.
  - Task chưa có file và đủ phụ thuộc → prep `-NoLaunch` rồi assign Antigravity; task đã có file được báo trạng thái, không tự prep/assign lại.
  - Điều phối tối đa hai lượt Antigravity đồng thời, một Codex trên máy tại mọi thời điểm; khóa Codex dùng chung cho prep/QC và lệnh accept ở task sau.
  - Dùng tiến trình riêng cho từng task, bộ đếm riêng; cổng KT giao lại tối đa một lần, QC giao lại tối đa một lần; không tự chuyển sang Codex viết code trong pipeline.
  - Chờ các lượt đang chạy kết thúc rồi xét tiếp; hết task sẵn sàng thì dừng, không tự merge; giấu log dài khỏi console, in đúng một dòng kết quả cho mỗi task.
- **Test nghiệm thu:** hai task độc lập chạy chồng thời gian; lượt thứ ba phải chờ; prep/QC không chồng Codex; phụ thuộc chờ merge; plan chưa duyệt/từ chối/chu trình; task có sẵn; prep dở tiếp tục; hết hạn mức; hai pipeline cùng phiếu; tiến trình lỗi nhả khóa; kiểm số lượt thực tế bằng agent/Codex giả.
- **Phụ thuộc:** V8-01

### V8-03 · Gói nghiệm thu và quyết định một lệnh
- **Agent:** A
- **Ngân sách:** ~390 dòng
- **Phạm vi:** `team.ps1` · `tools/workflow-accept.ps1` · `templates/codex-accept.md` · `tools/test-verify.ps1`
- **Làm:**
  - Thêm `accept-pack -Request R-xxx`, chỉ đọc: gom mọi task chờ nghiệm thu; mỗi task có cổng KT, QC, từng phát hiện mở, dòng/ngân sách, thay đổi giao diện/trang cần xem và phụ thuộc.
  - Tái dùng cách đếm dòng loại khung; đọc metrics và log theo task/lượt/SHA, không chọn bằng tiền tố gần giống; thiếu/hỏng → “không rõ”, không ném lỗi đọc log.
  - Thêm `accept -Project X -Task T [-Optional "..."] [-Note "..."]`; yêu cầu cổng KT đạt và QC hợp lệ trên SHA hiện tại; phát hiện bắt buộc còn mở chưa được quản lý phân loại → “Chưa đủ bằng chứng nghiệm thu.”.
  - Gọi Codex dưới khóa chung, chỉ cho điền Review / Kết quả / Đánh giá quy trình; ghi “### Vòng N — ĐẠT”, Optional/Note và số phút/token/lượt/QC có nguồn; thiếu số liệu hoặc điểm đánh giá → “không rõ”.
  - Kiểm diff chỉ đổi các mục cho phép, giữ báo cáo/test/code; commit/push tài liệu rồi gọi merge hiện có, giữ cổng duyệt SHA; không ghi nhật ký merge thành công nếu merge/push thất bại.
  - `accept … -Reject "lý do"` → ghi vòng CẦN SỬA, commit/push rồi assign `-Resume -Feedback`; không gọi luồng ĐẠT; gọi lại trên task đã merge không thêm vòng hoặc merge lại.
- **Test nghiệm thu:** gói nhiều task, không có task, log cũ/hỏng, SHA cũ, mã task gần giống, dòng loại khung, trang UI nhận diện được/không rõ; Codex giả sửa ngoài mục bị chặn; Optional/Note tiếng Việt và ký tự đặc biệt; reject; gọi lặp; approval chờ/từ chối/sai SHA; merge thất bại; log có điểm không biết không bị bịa.
- **Phụ thuộc:** V8-01 · V8-02

## Câu hỏi cho quản lý
- Xác nhận giao A phạm vi `team.ps1`, `tools/`, template của R-008 ngoài areas hiện tại; không cần sửa `projects.json`?
- Nếu mọi task/phát hiện mở vượt 40 dòng, cho phép báo “Gói nghiệm thu vượt 40 dòng.” kèm đường dẫn báo cáo đầy đủ, hay phải phân trang? Cần chốt trước test V8-03 để không che mất phát hiện.

## Quyết định của quản lý (Claude, 25/10 11:58)
- ĐÃ DUYỆT 3 task, tuần tự V8-01 → V8-02 → V8-03, Agent A (Minh) được làm `team.ps1`, `tools/`, `templates/` trong R-008; không sửa `projects.json`.
- Gói nghiệm thu > 40 dòng: in 40 dòng đầu (ưu tiên phát hiện bắt buộc còn mở) + dòng "Gói nghiệm thu vượt 40 dòng — đầy đủ: <đường dẫn>"; ghi bản đầy đủ ra logs.
- Cảnh báo "bước chỉ đọc làm thay đổi file" của `plan` báo nhầm khi tiến trình khác commit cùng lúc ⇒ V8-01 so `git status` chỉ trên file KHÔNG được commit bởi team.ps1 trong lúc chạy (hoặc so HEAD + status), thêm 1 ca test.
- V6-02 (R-006, chặn quá 3 vòng review) vẫn chờ: giao sau V8-01 vì cùng sửa `team.ps1`.
- Bổ sung 25/10 (Claude, từ đánh giá R-010) cho V8-03: (1) cổng KT chạy `tools/test-verify.ps1` khi diff chạm `team.ps1` / `tools/` / `scripts/`; (2) `merge` chạy build+test clone sạch TRƯỚC khi push main, đỏ thì không push. ĐÃ DUYỆT.
