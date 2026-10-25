# H7-01 · Dịch vụ Map và lưu nguyên tử

- **Agent:** A
- **Nhánh:** `agent-a/H7-01-dich-vu-map-va-luu-nguyen-tu`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~300 dòng
- **Yêu cầu:** R-009
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Chuẩn bị dịch vụ cho người dùng xem và lưu phân vai đủ 11 nghiệp vụ trên Map; chưa thêm giao diện trong task này.
- Cấu hình cũ chưa có phân vai sẽ dùng mặc định quy trình v4.
- Lưu phân vai không làm mất các cấu hình khác, kể cả khi Cài đặt đang lưu cùng lúc.
- Báo lỗi rõ ràng khi dữ liệu sai, file hỏng hoặc cấu hình đã bị thay đổi.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-009/brief.md` mục 6
- `hub/Services/WorkflowRolesService.cs`
- `hub/Services/TeamConfigService.cs`
- `hub/Program.cs`
- `hub/Services/HubPaths.cs`
- `hub/Services/Contracts.cs`
- `hub/Models/HubModels.cs`
- `hub.tests/AcceptanceH701WorkflowRolesTests.cs`
- `hub.tests/TeamConfigServiceTests.cs`

## Bối cảnh
Làm theo brief R-009 mục 6. SaveSettings hiện đọc rồi ghi trực tiếp team.json; cần phối hợp với Map để không mất cập nhật. Hợp đồng mới nằm riêng trong Services, không sửa hợp đồng chung. DI đã có trong Program; không cần trang mới.

## Mục tiêu
Hiện thực IWorkflowRolesService/GetRoles/SaveRoles đúng XML trong khung; điều chỉnh đường ghi Settings dùng chung cơ chế khóa và thay file nguyên tử. Ngân sách ~300 dòng code sản phẩm, không tính test nghiệm thu có sẵn.

## Phạm vi (được sửa)
- `hub/Services/WorkflowRolesService.cs`
- `hub/Services/TeamConfigService.cs`
- `hub/Program.cs`
- `tasks/H7-01-dich-vu-map-va-luu-nguyen-tu.md` (chỉ Báo cáo của agent)

## Không làm
- Không sửa test nghiệm thu `hub.tests/AcceptanceH701WorkflowRolesTests.cs` hoặc các test cũ.
- Không làm trang Map, menu, chuyển hướng, CLI hoặc thực thi phân vai trong team.ps1.
- Không sửa Contracts.cs, HubModels.cs, team.ps1, CLAUDE.md, cấu hình thật, skill/, tools/.
- Không gọi agy/codex hoặc tiến trình thật. Không chạy app/chụp ảnh.

## Ràng buộc
- JSON: `workflow.roles` là mảng `{ "code": "plan", "actor": "codex", "note": "" }`. Đây là định dạng mới được chốt cho R-009, không giả định đã có dữ liệu thật.
- 11 mã, thứ tự trả về: receive, idea, research, plan, approve_plan, prep, code, gate, qc, accept, publish. Đầu vào được phép đảo thứ tự; trả về thứ tự chuẩn.
- 6 mã tác nhân phân biệt hoa thường: claude, codex, minh, lan, gate, owner. Nhiều nghiệp vụ có thể cùng tác nhân; chỉ cấm trùng mã nghiệp vụ.
- Mặc định v4 đúng XML: Claude tiếp nhận/ý tưởng/duyệt kế hoạch/nghiệm thu/publish; Codex research/plan/prep/QC; Minh code; Cổng KT kiểm thử. Ghi chú mặc định rỗng.
- Chỉ thiếu file/workflow/roles mới dùng mặc định. workflow hoặc roles hiện diện mà null/sai kiểu/rỗng ⇒ lỗi. roles phải đủ đúng 11 dòng, code/actor/note đúng kiểu string; note không null, tối đa 500 đơn vị UTF-16, không trim.
- InvalidDataException: “Phân vai không hợp lệ.”; IOException khi JSON hỏng/root không object/không đọc được: “Không đọc được cấu hình phân vai.”. Kiểm tra JSON hỏng trước kiểm tra revision.
- Revision = SHA-256 hex toàn bộ byte file, file thiếu = `missing`; lưu trả snapshot mới. Revision null/rỗng là dữ liệu sai; revision cũ ⇒ InvalidOperationException “Cấu hình đã thay đổi. Vui lòng tải lại.”.
- Chỉ cập nhật workflow.roles, giữ khóa lạ mọi cấp ngoài roles và trạng thái BOM, file mới UTF-8 không BOM. Không ghi team.local.json từ Map; Settings vẫn tách đường dẫn riêng và tạo .bak như cũ.
- Khóa chung giữa mọi instance Map và Settings theo đường dẫn tuyệt đối chuẩn hóa (Windows không phân biệt hoa thường); giữ khóa từ đọc lại đến thay file. Kiểm tra lại revision ngay trước thay để phát hiện chỉnh sửa ngoài Hub; không khẳng định ngăn được tiến trình ngoài cố tình ghi sau lần kiểm tra cuối.
- Ghi tạm tên duy nhất cùng thư mục, flush rồi thay nguyên tử; file mới move không ghi đè. IOException “Không lưu được cấu hình phân vai.” khi lỗi ghi/quyền. Không xóa file gốc để thay, không để file tạm sau thất bại. Không yêu cầu giao dịch chung team.json + team.local.json.
- Không theo symlink/reparse point của file hoặc thư mục tổ tiên. Không lấy đường dẫn từ code/actor/note, không đưa dữ liệu gốc vào lỗi, không log bí mật. Chỉ thao tác HubPaths được truyền vào.
- File .cs lưu UTF-8 có BOM. Không triển khai thêm hợp đồng chung; helper nội bộ nếu cần đặt trong hai file Services thuộc phạm vi.

## Dữ liệu mẫu thật
Chưa xác minh trên máy thật, theo `plans/R-009/brief.md` mục 6. Dữ liệu cũ tối thiểu và khóa mở rộng được dựng trong test, không đọc cấu hình thật hay USERPROFILE. Không có fixture phân vai thật được chỉ định trong kế hoạch.

## Test nghiệm thu
`hub.tests/AcceptanceH701WorkflowRolesTests.cs`

## Tiêu chí hoàn thành
- [ ] Hiện thực đủ hợp đồng riêng, 11 mã, 6 tác nhân, mặc định v4 và giới hạn ghi chú; không còn TODO(agent).
- [ ] Đọc dữ liệu cũ không tự ghi file; dữ liệu sai/JSON hỏng trả đúng loại ngoại lệ và câu lỗi.
- [ ] Lưu chỉ thay roles, giữ khóa khác/BOM; lỗi ghi và xung đột giữ nguyên byte file cũ, dọn file tạm.
- [ ] Map và Settings dùng chung khóa và thay file nguyên tử; ca ghi đồng thời không mất dữ liệu; từ chối reparse point.
- [ ] `dotnet build AITeam.sln` 0 lỗi, 0 warning sau hiện thực.
- [ ] `dotnet test AITeam.sln` pass tất cả; không sửa/xóa/Skip test nghiệm thu.
- [ ] Báo cáo có bảng từng tiêu chí → bằng chứng thật, số test pass/warning, danh sách file và lý do, quyết định tự đưa ra và điều chưa xác minh.

## Báo cáo của agent
<!-- Antigravity điền sau hiện thực; không dùng kết quả build khung thay bằng chứng hiện thực. -->

| Tiêu chí | Bằng chứng (lệnh và kết quả thật) |
|---|---|
| Chưa thực hiện | Chờ Antigravity triển khai |

File đã đổi và lý do: chờ điền.
Quyết định tự đưa ra: chờ điền.
Điều chưa xác minh: chờ điền.

## Review (Claude)
<!-- Claude điền khi nghiệm thu. -->

## Kết quả
<!-- Claude điền sau review đạt. -->
## Kiểm tra khung (Codex — bước 4)
- Đã tạo hợp đồng + khung WorkflowRolesService (hai thân hàm chỉ ném NotImplementedException), test nghiệm thu; thêm DI và mô tả phối hợp ghi Settings, chưa hiện thực logic sản phẩm.
- File đã đổi: WorkflowRolesService.cs (hợp đồng/khung), TeamConfigService.cs (XML yêu cầu), Program.cs (DI), AcceptanceH701WorkflowRolesTests.cs (test), file task này (đặc tả).

| Tiêu chí chuẩn bị | Bằng chứng thật |
|---|---|
| Build toàn solution | Lần 1 `dotnet build AITeam.sln`: exit 1, 0 warning/0 error, không có chẩn đoán nguyên nhân. Lần 2 `dotnet build AITeam.sln -m:1 -v:normal`: thành công, 0 lỗi, 2 warning (CS9113 khung và CS8619 test), 36,29 giây. |
| Sửa warning test | Đã sửa kiểu Task<Exception?> thành Task<Exception> để khớp Record.Exception. Chưa build lại thay đổi chú thích nullable này vì giới hạn 2 lần build. CS9113 được chấp nhận cho khung. |
| Test nghiệm thu phải đỏ | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceH701WorkflowRolesTests --verbosity quiet`: 43 fail, 0 pass, 0 skip (bản build trước chỉnh nullable); thân hàm chưa hiện thực. |
| Không chạy toàn bộ test | Chưa chạy `dotnet test AITeam.sln`; đây là khâu chuẩn bị với test nghiệm thu cố ý đỏ, không phải báo cáo hoàn thành hiện thực. |

Quyết định: chốt mã nghiệp vụ/tác nhân, JSON mảng và revision SHA-256 trong hợp đồng riêng vì brief chỉ nêu tên hiển thị; ghi chú tính theo string.Length, không trim. Lỗi ghi có câu riêng “Không lưu được cấu hình phân vai.”.
Chưa xác minh: dữ liệu phân vai thật, kiểm tra reparse point trên máy thật, hành vi triển khai thay file nguyên tử (giao Antigravity hiện thực). Test không thay thế kiểm tra can thiệp của tiến trình ngoài sau lần so phiên bản cuối.