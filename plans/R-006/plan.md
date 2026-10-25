# Kế hoạch R-006
## Hiểu yêu cầu
Chặn giao lại khi tổng số vòng review **≥ `review.maxRounds`**; hiện ngưỡng là 3.
Chủ dự án nhận mục “Cần anh duyệt” để quyết định giao tiếp, Claude tự sửa hoặc bỏ task.
Đếm cộng tiêu đề vòng trong task và log AI có kết luận CHANGES_REQUESTED, không lấy số vòng lớn nhất.
Tách thành lõi + CLI và nối luồng điều phối; không đổi trang Hub, không làm Playwright/E2E.
Đây là kế hoạch chỉ đọc; chưa chạy build/test, không tạo hoặc sửa file.

## Research
- `projects.json:23`: A phụ trách dịch vụ/dữ liệu Hub; B phụ trách giao diện Hub. `core/`, `team.ps1`, `tools/` chưa được phân areas.
- `team.json:19`: đã có `review.maxRounds = 3`; không cần thêm cấu hình.
- `team.ps1:290`: `Invoke-Assign` đọc task trước khi cập nhật bảng, tạo worktree và chạy agent; phù hợp đặt chốt chặn sớm.
- `team.ps1:391`: `-Resume` hiện nối lại hội thoại; phải giữ hành vi này khi chưa đạt ngưỡng.
- `team.ps1:442`: đã có giới hạn thử lại khi verify lỗi; đây là cơ chế riêng, không phải số vòng review.
- `team.ps1:459`: sau review AI mã 1, có nhánh tự gọi lại `Invoke-Assign` với Resume và feedback.
- `team.ps1:592`: log tổng kết AI nằm ngay trong thư mục logs, tên `<Task>.review-*.json`; không đếm đệ quy thư mục review con.
- `team.ps1:108`: logs lấy từ `$r.Logs` của dự án, không phải thư mục `logs/` cố định tại repo.
- `core/AITeam.Core/Review/ReviewRunner.cs:174`: JSON tổng kết có trường gốc `verdict`, gồm CHANGES_REQUESTED.
- `tasks/V5-07b-research-page.md:82`: mẫu task thật có `## Review (Claude)` và `### Vòng 1 — ĐẠT`.
- `team.ps1:1077`: đã có cách tách mục Markdown; `team.ps1:1724` đang lấy số vòng cho báo cáo, không đúng phép đếm mới.
- `core/AITeam.Cli/Program.cs:15`: CLI dùng switch điều phối; chưa có `review-rounds`; lỗi tham số của lệnh review trả mã 2 tại dòng 93.
- `team.ps1:1538`: `approval-request` hỗ trợ decision; dòng 1557 cho thấy decision hiện không gán trường task, nên cần ghi mã task trong title/detail.
- `team.ps1:1562`: tạo approval có commit/push; test phải cô lập cả root điều phối và remote Git.
- `tools/test-verify.ps1:23`: có bộ dựng repo/remote/worktree tạm; `core.tests/ReviewCliTests.cs:15` dùng DLL được chép kèm qua ProjectReference.
- CHƯA XÁC MINH: có log tổng kết review thật ngoài bộ test hay không; định dạng hiện được kiểm chứng bằng mã ghi JSON.

## Rủi ro
- Mã task chứa wildcard/đường dẫn → kiểm toàn chuỗi theo `^[A-Za-z0-9][A-Za-z0-9-]{0,63}$` trước tìm task/log hoặc gọi tiến trình.
- Đếm nhầm văn bản thường, mục khác hoặc mã Markdown → chỉ nhận tiêu đề cấp 3 đúng mẫu trong mục Review; bỏ qua khối mã.
- Dữ liệu cũ thiếu/hỏng → nguồn task lỗi đóng góp 0; từng JSON lỗi đóng góp 0; nguồn hợp lệ khác vẫn được cộng, không ghi đè dữ liệu.
- Task repo chính cũ hơn worktree → lần tự giao lại đọc task trong worktree vừa review; lần Resume dùng task đang được giải quyết theo luồng hiện có.
- CLI lỗi bị hiểu nhầm là 0 → chỉ chấp nhận mã thoát 0 và stdout là một số nguyên không âm; lỗi vận hành thì dừng giao lại.
- Gọi Resume nhiều lần tạo nhiều mục duyệt → tái dùng mục decision pending cùng dự án/task và nội dung giới hạn review.
- Test vô tình chạy agent hoặc push thật → dùng thư mục tạm, remote cục bộ và agent giả có dấu vết; không dùng cấu hình/profile thật.
- PowerShell 5.1, mất BOM hoặc đè biến tham số → giữ UTF-8 BOM cho `.ps1`, truyền mảng đối số và giữ kiểm tra AST hiện có.

## Task
### V6-01 · Đếm vòng review và CLI
- **Agent:** A (đề xuất Minh; cần quản lý mở rộng areas như câu hỏi cuối)
- **Ngân sách:** ~320 dòng, tối đa 15 phút; gồm khung/test Codex chuẩn bị
- **Phạm vi:** `core/AITeam.Core/Review/ReviewRoundCounter.cs` · `core/AITeam.Cli/Program.cs` · `core.tests/AcceptanceV601ReviewRoundsTests.cs`
- **Làm:**
  - Viết hàm thuần nhận nội dung task và các nội dung log đã lọc → trả tổng số vòng; không đọc file hoặc cấu hình trong hàm.
  - Đếm số tiêu đề `### Vòng N` trong `## Review (Claude)` đến mục cấp 2 tiếp theo; chấp nhận LF/CRLF, hậu tố kết luận; không lấy max N, không lọc theo ĐẠT/CẦN SỬA.
  - Mỗi JSON hợp lệ có trường gốc `verdict` bằng `CHANGES_REQUESTED` đóng góp 1; PASS/BLOCKED/SKIPPED, thiếu trường, sai kiểu hoặc JSON hỏng đóng góp 0.
  - Thêm `aiteam review-rounds --task-file <f> --logs <d> --task <T>`; lớp CLI đọc nguồn, chỉ chọn file đúng tiền tố task tại logs, in duy nhất số vòng và trả 0; thiếu/hỏng nguồn được tính 0 riêng.
  - Thiếu tham số → stderr `LỖI: thiếu tham số bắt buộc (--task-file, --logs, --task).`; mã task sai → `LỖI: mã task không hợp lệ.`; cả hai trả 2.
- **Test nghiệm thu:** Codex viết trước: 0 vòng; số N không liên tiếp/trùng; chỉ đếm đúng mục, bỏ văn bản thường/khối mã; BOM/LF/CRLF; cộng hai nguồn; JSON lỗi không làm mất log tốt; thiếu file/thư mục; không đếm task khác/thư mục con; mã task dài 1/64 hợp lệ, 65/ký tự lạ/`../`/wildcard/xuống dòng bị từ chối; CLI stdout/exit code đúng, đường dẫn có dấu và khoảng trắng. Test tiến trình dùng `AppContext.BaseDirectory`, không trỏ `bin/Debug`.
- **Phụ thuộc:** —

### V6-02 · Chặn giao lại và xin quyết định
- **Agent:** A (đề xuất Minh; cùng phần điều phối, không có việc giao diện cho B)
- **Ngân sách:** ~330 dòng, tối đa 15 phút; gồm khung/test Codex chuẩn bị
- **Phạm vi:** `team.ps1` · `tools/test-verify.ps1`
- **Làm:**
  - Dùng CLI V6-01 làm nguồn đếm duy nhất; đọc ngưỡng từ cấu hình hợp nhất hiện có, mặc định 3 khi thiếu/không phải số nguyên dương.
  - Với `assign -Resume`, kiểm mã task trước tra cứu; kiểm ngưỡng trước ghi trạng thái đang làm, fetch/worktree, tạo prompt hoặc chạy agent; dưới ngưỡng giữ luồng hiện có.
  - Sau review AI CẦN SỬA, kiểm lại với log vừa ghi và task trong worktree, trước điều kiện thử lại; đạt ngưỡng thì dừng kể cả thiếu feedback hoặc đã thử lại một lần.
  - Khi tổng N đạt ngưỡng, gọi cơ chế `approval-request -Kind decision` hiện có, title `Cần anh duyệt · <Task> · quá N vòng review`; detail ghi dự án, task, tổng/ngưỡng và ba lựa chọn; tái dùng mục pending tương ứng.
  - Ghi bảng chính xác `⏸ Chờ anh quyết · quá N vòng review`, rồi return; không để nhánh trạng thái phía sau ghi đè, không tự thực hiện lựa chọn của chủ dự án.
  - CLI lỗi vận hành → báo `Không xác định được số vòng review; dừng giao lại.` và không chạy agent; giữ giới hạn verify riêng, không thêm tùy chọn vượt chốt bằng Force/NoVerify.
- **Test nghiệm thu:** Codex chuẩn bị trước đúng một kịch bản mới trong script: repo/remote/root tạm, ngưỡng 3, task có 3 vòng → gọi `assign -Resume`, agent giả không được gọi, có decision pending đúng nội dung, bảng đúng trạng thái; gọi lại không nhân đôi approval. Kiểm thử bổ sung quanh helper/nhánh: 0 và 2 được đi tiếp, 3 và 4 bị chặn, nguồn cộng 2+1, log AI mới làm chạm ngưỡng, CLI lỗi không chạy agent; bảo toàn các kịch bản verify cũ.
- **Phụ thuộc:** V6-01; chạy tuần tự. Trước bàn giao chạy `dotnet build AITeam.sln`, `dotnet test AITeam.sln` và `powershell -NoProfile -File tools/test-verify.ps1`; build 0 warning, toàn bộ test pass.

## Quyết định của quản lý (Claude, 25/10 10:30)
- ĐÃ DUYỆT. Giao tuần tự: V6-01 trước, V6-02 sau khi V6-01 merge.
- Areas: Minh (A) được làm `core/`, `core.tests/`, `team.ps1`, `tools/test-verify.ps1` trong R-006.
- V6-02: kịch bản test-verify dùng agent giả (không gọi agy thật); giữ nguyên mọi kịch bản cũ.

## Câu hỏi cho quản lý
- Có chốt mở rộng areas cho A (Minh) ở `core/`, `core.tests/`, `team.ps1`, `tools/test-verify.ps1` cho riêng R-006 không? Areas hiện tại chỉ bao phủ Hub; cả hai task trên được đề xuất có điều kiện, chưa thể giao đúng areas hiện hành.
