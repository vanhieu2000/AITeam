# W1-05 · Parser tiến độ và quy tắc chuyển bước

- **Agent:** B
- **Nhánh:** `agent-b/W1-05-parser-tien-do-va-quy-tac-chuyen-buoc`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~280 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->

## Tóm tắt
- Bổ sung phần xử lý dữ liệu để Hub biết một việc đang ở bước nào trong chín bước.
- Đọc được nhật ký bảng cũ và dòng workflow, giữ nguyên dữ liệu nguồn.
- Hiện đúng bước cần sửa khi cổng kiểm tra hoặc QC lỗi, kể cả sau dòng Assign tổng kết.
- Chưa thêm chức năng trực tiếp trên giao diện; task này chuẩn bị dữ liệu cho trang Việc.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-011/brief.md` mục 3 và Ràng buộc.
- `hub/Services/WorkProgressParser.cs`
- `hub/Services/RequestService.cs`: tham khảo bảng nhật ký cũ, không sửa.
- `hub.tests/AcceptanceW105WorkProgressTests.cs`
- `hub.tests/Fixtures/WorkProgress/R-011-bo-testqc-chuan-codex-tra-ke-hoachmockup.md`
- `hub.tests/Fixtures/WorkProgress/README.md`

## Bối cảnh
Theo brief R-011 mục 3, trang Việc cần chín bước:
Tiếp nhận → Kế hoạch → Duyệt kế hoạch → Chuẩn bị → Code → Cổng KT → QC → Nghiệm thu → Merge.
Không đổi định dạng các lệnh khác đang ghi. Parser hiện có của RequestService chỉ đọc bảng,
không phải hợp đồng tính tiến độ; dùng model riêng trong file được giao.

## Phạm vi (được sửa)
- `hub/Services/WorkProgressParser.cs`

## Không làm
- Không sửa hợp đồng `hub/Models/HubModels.cs`, `hub/Services/Contracts.cs` hoặc RequestService.
- Không sửa test nghiệm thu và hai fixture được giao. Không thêm dependency, project hoặc đổi wiring.
- Không làm trang, CSS, render, chụp ảnh, chạy app hoặc đo trình duyệt.
- Không đọc/ghi đĩa, gọi PowerShell/git/agy/codex, truy cập hồ sơ người dùng hoặc đổi dữ liệu nguồn.
- Không sửa team.ps1, công cụ pipeline, danh sách task hoặc phiếu thật.
- Không chạy lệnh git trong phiên chuẩn bị này; hệ thống tự commit.

## Ràng buộc
- Giữ nguyên chữ ký public, enum/record và namespace trong khung; chỉ điền thân hai hàm và helper nội bộ.
  Model riêng không thay hợp đồng Hub. Lớp static thuần nên không đăng ký DI, không có CLI/Main.
- Ngân sách khoảng 280 dòng triển khai; không tính test/fixture chuẩn bị trước.
- `Parse(null/rỗng)` trả rỗng; `Calculate` nhận events null như rỗng. Không tạo giờ/người giả.
- Parser chỉ xử lý mục heading cấp 2 chứa “Nhật ký”, kể cả heading đánh số; dừng ở heading cấp 2 sau.
  Nhận BOM đầu chuỗi và CRLF/LF/CR; loại header, separator và dòng có ô Sự kiện rỗng.
- Bảng cũ thật: `| Giờ | Sự kiện | Ghi chú |`. Giữ sự kiện không phân loại được với Action/Outcome Unknown,
  TaskId rỗng và Round=0; giữ nội dung sự kiện và ghi chú trong Text, không đoán task/lượt/kết quả từ văn xuôi.
- Dòng workflow theo brief: `- <giờ> | <task> | Prep/Assign/Verify/Qc/Merge | lượt N | kết quả <!-- workflow:… -->`.
  N nguyên dương trong int; sai số cột/task/lượt/action bỏ dòng. Không dùng marker làm nguồn lệnh.
  TaskId chỉ mã task theo regex trong XML doc; marker là dữ liệu phụ, không tự đặt thêm cú pháp.
- Text bỏ marker workflow; kết quả chuẩn hóa theo nguyên từ đầu:
  ĐẠT/PASS/PASSED, CẦN SỬA/FAIL/FAILED/LỖI, BLOCKED; khác là Unknown (KHÔNG ĐẠT không được coi là đạt).
  Dòng trùng sau Trim chỉ lấy lần đầu; giữ thứ tự nguồn. Giữ nguyên văn nội dung không tin cậy, không render HTML.
- Giờ ISO giữ offset; `HH:mm dd/MM/yyyy` hiểu +07:00. Chỉ `HH:mm` thì lấy ngày receivedAt quy về +07:00;
  thiếu receivedAt hoặc giờ sai để null, không bỏ sự kiện. Nguồn không rõ người ⇒ Actor null.
- Chín bước xuất đúng thứ tự enum. Ban đầu Received Current; planExists chứng minh Received/Plan Done,
  PlanApproval Current; planApproved còn chứng minh PlanApproval Done và Prep Current.
  Thiếu bằng chứng không đánh Done bước khác. Hai cờ là dữ liệu từ caller, không mở file plan.
- Lọc đúng task không phân biệt hoa thường; chỉ dùng workflow có lượt ≥1 và Action/Outcome khác Unknown.
  Lượt mới nhất so theo số (10 sau 2), không so chuỗi. Trong lượt, sự kiện có giờ trước, sắp theo UTC;
  hòa giờ giữ thứ tự nguồn, thiếu giờ xếp sau theo thứ tự nguồn.
- Prep đạt → Prep Done, Code Current; Assign đạt → Code Current, không chứng minh Code Done.
  Verify đạt → Verify Done, QC Current; QC đạt → QC Done, Nghiệm thu Current.
- Failed/Blocked giữ chính bước tương ứng ở Failed (Assign ứng với Code), LatestError=Text.
  Gate lỗi chưa được đạt lại chặn bước tiếp; Assign tổng kết không che Verify/QC lỗi.
  Thành công mới hơn cùng bước xóa lỗi đó. Unknown không thay đổi tiến độ.
- Merge đạt mới hơn hoặc boardStatus chứa ✅ ⇒ Merge Done, CurrentStep Merge, IsMerged true, xóa lỗi cũ;
  không suy diễn Acceptance Done. Trạng thái 🟡/🔵/🔴 riêng lẻ không chứng minh gate đã đạt.
- TimeText dùng +07:00, `HH:mm dd/MM/yyyy`; thiếu giờ/người thì từng trường là “Chưa có dữ liệu”.
  LatestError null khi không có lỗi còn hiệu lực. Không đổi đầu vào hoặc giữ trạng thái dùng chung.
- Không có thông báo lỗi mới cho người dùng: dòng hỏng bỏ qua như trên; nội dung lỗi nguồn nằm trong LatestError.
- File .cs UTF-8 có BOM, comment/báo cáo tiếng Việt có dấu.
- Test không tạo tiến trình; nếu thêm test cần tiến trình phải dùng TestShell chung, không harness riêng.

## Dữ liệu mẫu thật
- `hub.tests/Fixtures/WorkProgress/R-011-bo-testqc-chuan-codex-tra-ke-hoachmockup.md`:
  chép nguyên byte từ `requests/R-011-bo-testqc-chuan-codex-tra-ke-hoachmockup.md`, giữ tên/BOM/CRLF nguồn.
- `hub.tests/Fixtures/WorkProgress/README.md`: chép nguyên byte từ `tasks/README.md`.
  Test dùng dòng H6-01 đã merge để xác minh trạng thái ✅ có thật.
- Phiếu R-011 thật hiện chỉ có dòng `| | Nhận yêu cầu | |` trong mục `## 3. Nhật ký (…)`.
  Các ca workflow là biến thể chèn vào bản sao phiếu, theo định dạng brief; chưa có log workflow thực thi
  trong mẫu được phép đọc. Không nhận đã đối chiếu được marker/người/kết quả với output pipeline thật.
- Fixture nằm trong thư mục đã được csproj chép tới output; test chỉ đọc bản sao, không đọc file AITeam thật.

## Test nghiệm thu
`hub.tests/AcceptanceW105WorkProgressTests.cs`

## Tiêu chí hoàn thành
- [x] Triển khai đúng XML doc và các quy tắc trên trong duy nhất file thuộc phạm vi; không còn TODO/NotImplementedException.
- [x] Parse được bảng cũ thật, BOM/CRLF/heading đánh số; loại trùng/sai; chuẩn hóa giờ, task, lượt, kết quả.
- [x] Chín bước đúng, phân biệt Waiting/Current/Done/Failed; không suy diễn đã nghiệm thu.
- [x] Lượt 10 sau 2; lỗi rồi thành công, QC BLOCKED, Assign che lỗi, Merge sau lỗi cũ đều đúng.
- [x] Dữ liệu độc hại chỉ là chuỗi, hàm thuần không đổi đầu vào; thiếu giờ/người hiện đúng câu đã quy định.
- [x] `dotnet build AITeam.sln` thành công, 0 warning mới.
- [ ] `dotnet test AITeam.sln` thành công; nghiệm thu giữ nguyên và có bằng chứng số pass/fail/skip.
- [x] Báo cáo từng tiêu chí → lệnh/kết quả thật, file/lý do, quyết định tự đưa ra và điều chưa xác minh.

## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Đúng phạm vi, hết TODO | `Select-String hub/Services/WorkProgressParser.cs -Pattern 'TODO|NotImplementedException'`: không khớp; 264 dòng; gỡ file hồi quy ngoài phạm vi theo review. |
| Parse bảng thật, BOM/CRLF, chuẩn hóa | Lệnh N: 63 pass/0 fail/0 skip, gồm toàn bộ ca Parse và fixture thật. |
| Chín bước, Waiting/Current/Done/Failed | Lệnh N: 63 pass/0 fail/0 skip; chỉ hoàn thành bước có bằng chứng, không suy diễn Code hoặc Acceptance Done. |
| Lượt, lỗi rồi đạt, BLOCKED, Assign, Merge | Lệnh N: 63 pass/0 fail/0 skip; giữ lỗi chưa giải quyết, chọn bước theo sự kiện thay vì bước đầu chưa Done. |
| Hàm thuần, dữ liệu độc hại, metadata | Lệnh N: 63 pass/0 fail/0 skip, ca không đổi đầu vào và metadata pass. |
| Build | `dotnet build AITeam.sln --no-restore -m:1 -p:UseSharedCompilation=false --nologo`: exit 0, 0 warning/0 error. |
| Test toàn solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m --nologo --logger "console;verbosity=quiet"`: Hub 597 pass/9 fail/0 skip; Core không có tổng kết/output mới, dừng Ctrl+C sau hơn 3 phút theo giới hạn task, exit 1. |
| BOM, nghiệm thu bất biến, báo cáo | Parser EF-BB-BF; SHA256 nghiệm thu giữ nguyên `83515705FD8E75C306C7DD0467841F17BF47E80470C53A15753C541182E95F04`; không sửa fixture/test nghiệm thu. |
N = `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceW105WorkProgressTests" --blame-hang-timeout 2m --nologo`.
File/lý do: sửa `hub/Services/WorkProgressParser.cs` để chuyển bước đúng; xóa `hub.tests/W105RuleRegressionTests.cs` theo review; cập nhật phiếu này.
Quyết định/chưa xác minh: không sửa ngoài phạm vi; chạy riêng ca SettingsPageTests lỗi 1/1: UnauthorizedAccessException tại File.Replace trong TeamConfigService.SaveSettings; Core chưa xác định được ca chạy lâu vì không có output; fixture chưa có workflow thực thi thật; không chạy git, hệ thống tự commit/push nên chưa có commit được kiểm.
## Review (Claude)
<!-- Claude ghi kết quả review sau khi agent triển khai. -->

## Kết quả
<!-- Claude điền sau khi review ĐẠT. -->

## Đánh giá quy trình
<!-- Claude điền sau khi merge. -->