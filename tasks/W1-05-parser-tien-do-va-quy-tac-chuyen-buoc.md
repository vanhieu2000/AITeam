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
Antigravity điền sau khi triển khai:

| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Parse dữ liệu thật và biến thể | ĐẠT — `dotnet test hub.tests/AITeam.Hub.Tests.csproj --filter "FullyQualifiedName~AcceptanceW105WorkProgressTests" --blame-hang-timeout 2m`: Passed 63, Failed 0, Skipped 0 |
| Quy tắc chín bước, lỗi và chuyển lượt | ĐẠT — `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceW105WorkProgressTests" --blame-hang-timeout 2m`: Passed 63, Failed 0, Skipped 0; đã sửa theo dõi lỗi theo từng bước (`Qc Failed → Verify Failed → Qc Passed` giữ `CurrentStep=Verify`, `LatestError` của Verify) |
| Thuần, an toàn dữ liệu và tương thích | ĐẠT — `Untrusted_text_remains_data_and_input_is_not_mutated` pass; không đổi đầu vào, giữ UTF-8 BOM (+231 dòng) |
| Build/test toàn solution | `dotnet build AITeam.sln --no-restore`: 0 warning, 0 error. `AITeam.Hub.Tests`: 590/590 pass (0 fail, 0 skip); `AITeam.Core.Tests`: 518/519 pass, 1 fail (`AcceptanceV801WorkflowTests.KhoaChanTienTrinhKhacVaTimeout` đỏ do máy quá tải khi chạy toàn cục; quản lý đã chạy lại riêng: PASS — không tự đánh dấu vượt tiêu chí test toàn solution) |

File đã đổi và lý do: `hub/Services/WorkProgressParser.cs` (triển khai `Parse`, `Calculate` và theo dõi lỗi còn hiệu lực theo từng bước); `tasks/W1-05-parser-tien-do-va-quy-tac-chuyen-buoc.md` (cập nhật báo cáo; đã hoàn tác `hub.tests/W105WorkProgressExtraTests.cs` vì cổng kiểm tra báo ngoài `## Phạm vi (được sửa)`).  
Quyết định tự đưa ra: Theo dõi lỗi đang hiệu lực theo từng bước (`errors[step]`); chỉ `Passed` cùng bước mới xóa lỗi của bước đó, và chỉ `Merge` `Passed` mới hơn hoặc `boardStatus` chứa `✅` mới xóa toàn bộ lỗi; `CurrentStep`/`LatestError` lấy theo cổng lỗi sớm nhất chưa được gỡ.  
Điều chưa xác minh: Cổng kiểm tra tự động chỉ cho phép sửa `hub/Services/WorkProgressParser.cs` theo mục Phạm vi (từ chối thêm `hub.tests/W105WorkProgressExtraTests.cs`, còn `AcceptanceW105WorkProgressTests.cs` là bất biến); test `KhoaChanTienTrinhKhacVaTimeout` (V8-01) đỏ khi máy quá tải (quản lý chạy riêng đã PASS); chưa có log workflow thực thi thật trong phiếu R-011 gốc.

### Kiểm tra chuẩn bị (Codex)
- Đã tạo khung hai hàm, chỉ throw NotImplementedException; chưa viết phần thân sản phẩm.
- Đã viết 63 ca dữ liệu (14 Fact + 49 InlineData), chưa xác minh discovery/chạy thực tế.
- SHA256 hai fixture bằng đúng nguồn; hai file .cs đã kiểm byte EF BB BF (UTF-8 BOM).
- `dotnet build AITeam.sln`: exit 1, “Build FAILED”, 0 warning/0 error trong output, chưa có chẩn đoán restore.
- `dotnet build AITeam.sln --no-restore -m:1 -v:normal`: exit 1, 0 warning/5 lỗi NETSDK1004;
  cả 5 project thiếu project.assets.json. Đã dùng đủ 2 lần build, không chạy thêm.
- `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceW105WorkProgressTests --blame-hang-timeout 2m`:
  exit 0 nhưng không có output/test count; KHÔNG coi là test pass hoặc đã khám phá test.
- Chưa xác minh biên dịch và test đỏ thực tế vì chưa build được; cần restore/build ở môi trường có dependency.
- Không có giao diện, DI, CLI, tiến trình ngoài hoặc phụ thuộc task khác.
- Giữ nguyên Agent/Nhánh/Yêu cầu/Review AI ở đầu phiếu (Agent B dù kế hoạch tóm tắt ghi A).
  Độ khó M, không sửa team.ps1/khóa/giao dịch git/chạy song song nên để model mặc định.

## Review (Claude)
<!-- Claude ghi kết quả review sau khi agent triển khai. -->

## Kết quả
<!-- Claude điền sau khi review ĐẠT. -->

## Đánh giá quy trình
<!-- Claude điền sau khi merge. -->