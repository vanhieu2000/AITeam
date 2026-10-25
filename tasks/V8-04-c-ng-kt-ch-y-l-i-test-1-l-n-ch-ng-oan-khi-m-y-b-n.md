# V8-04 · Cổng KT chạy lại test đỏ 1 lần (chống đỏ oan khi máy bận)

- **Agent:** A
- **Nhánh:** `agent-a/V8-04-c-ng-kt-ch-y-l-i-test-1-l-n-ch-ng-oan-khi-m-y-b-n`
- **Ưu tiên:** cao
- **Ngân sách:** ~120 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới cho người dùng.
- Cổng KT (`verify`) và bước clone sạch của `merge` báo đỏ oan khi máy chạy nhiều việc song song. Ngày 25/10 có 3 lần: V8-01 KhoaChanTienTrinhKhacVaTimeout, V6-01 Invalid_task_id_returns_exact_error, 1 test sau merge V8-02; chạy lại riêng đều đạt.
- Sửa: test đỏ ⇒ chạy lại RIÊNG các test đỏ đúng 1 lần. Lần 2 đạt ⇒ ĐẠT kèm cảnh báo "Test chập chờn: <tên>". Lần 2 vẫn đỏ ⇒ KHÔNG ĐẠT như cũ.

## Đọc (CHỈ những file này)
- `team.ps1`: hàm chạy test của cổng KT (verify) và bước clone sạch trong merge.
- `tools/workflow-common.ps1`: Invoke-WorkflowCheck, Test-InfrastructureFailure (V8-01, cách thử lại lỗi máy đang có).
- `core.tests/Support/TestShell.cs`: BẮT BUỘC dùng cho test, không tự viết harness.

## Mục tiêu
- Hàm thuần `Get-FailedTestNames($output)`: lấy tên đầy đủ các test đỏ từ output `dotnet test` (dòng `Failed <Tên.Đầy.Đủ>(tham số) [x s]` và `... [FAIL]`), bỏ trùng; không có ⇒ mảng rỗng.
- Cổng KT + clone sạch của merge: test đỏ ⇒ chạy lại `dotnet test <project> --no-build --filter "FullyQualifiedName~<tên không tham số>"` cho từng test (tối đa 10 tên; quá 10 ⇒ không chạy lại, KHÔNG ĐẠT).
- Kết quả cuối: lần 2 đạt hết ⇒ ĐẠT + dòng cảnh báo trong báo cáo verify; còn đỏ ⇒ KHÔNG ĐẠT, báo cáo liệt kê test vẫn đỏ.
- Lỗi build (không phải test đỏ) giữ nguyên xử lý hiện có của V8-01.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `tools/workflow-common.ps1`
- `core.tests/AcceptanceV804FlakyRetryTests.cs`

## Không làm
- Không đổi định dạng báo cáo verify ngoài việc thêm dòng cảnh báo.
- Không chạy lại cả bộ test; không thử lại quá 1 lần.
- Không sửa test của task khác.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Tên test lấy từ output là dữ liệu ngoài: chỉ nhận ký tự `[A-Za-z0-9_.]` cho phần đưa vào `--filter`; tên lạ ⇒ bỏ qua (không chạy lại) và coi như vẫn đỏ.
- Dùng output THẬT: lấy mẫu từ `D:\AITeam-work\AITeam\logs\W1-05-verify-2.md`, `W1-02-verify-3.md` (dòng `Failed AITeam.Core.Tests...`).

## Test nghiệm thu
- `core.tests/AcceptanceV804FlakyRetryTests.cs` (Codex viết cùng lượt, dùng TestShell): parse mẫu thật 1 / nhiều / có tham số / trùng / tên lạ; lần 2 đạt ⇒ ĐẠT + cảnh báo; lần 2 đỏ ⇒ KHÔNG ĐẠT; >10 test đỏ ⇒ không chạy lại; lỗi build không bị coi là test đỏ.

## Tiêu chí hoàn thành
- [x] Build `dotnet build AITeam.sln` 0 warning
- [ ] Test `dotnet test AITeam.sln` pass hết + `powershell -File tools\test-verify.ps1` ĐẠT
- [x] Báo cáo verify có dòng "Test chập chờn: …" khi lần 2 đạt

## Báo cáo của agent

| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln --no-restore -m:1 -nr:false`: đạt, 0 warning, 0 error. Restore tuần tự dùng `-p:NuGetAudit=false` vì mạng NuGet gây NU1900. |
| Parse, thử lại một lần, giới hạn 10, tên lạ, lỗi build | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceV804FlakyRetryTests --blame-hang-timeout 2m`: 14/14 pass, 0 skip. |
| Cảnh báo trong báo cáo verify | Test `Verify_them_canh_bao_vao_bao_cao_va_merge_dung_cung_co_che` đạt; chạy đoạn verify thật, xác nhận `Test chập chờn: A.B.C` trong nội dung báo cáo. |
| Toàn bộ test | `dotnet test AITeam.sln --no-build --no-restore -m:1 -nr:false --blame-hang-timeout 2m`: Hub 518 pass/9 fail/0 skip; lỗi quyền `File.Replace`, đã dừng lệnh ở khoảng 3 phút theo chỉ dẫn chống treo, core chưa trả tổng kết. |
| Cổng KT | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: 2 kiểm tra tĩnh đạt; kịch bản 1 bị Git chặn signal pipe, Win32 error 5. |
File đổi: `tools/workflow-common.ps1` tách tên/thử lại; `team.ps1` nối verify + clone sạch; `core.tests/AcceptanceV804FlakyRetryTests.cs` thêm 14 ca; file task cập nhật báo cáo. BOM của ba file mã đã kiểm: có đủ.
Tự quyết: lượt chạy lại phải có số test pass >0; tên không an toàn giữ kết quả đỏ; không đổi xử lý lỗi hạ tầng V8-01.
Chưa xác minh: toàn bộ solution/cổng KT xanh trong môi trường không bị sandbox; không chạy app/chụp ảnh. Không chạy git trực tiếp; hệ thống tự commit/push, chưa có commit được kiểm.

## Review (Claude)
### Vòng 1 — ĐẠT (Claude nghiệm thu, Codex code)
- Cổng KT lượt 1 ĐẠT: build 0 warning, Hub + Core 571/571 (gồm AcceptanceV804FlakyRetryTests).
- Đọc diff: Get-FailedTestNames chỉ nhận tên [A-Za-z0-9_.]; chạy lại từng test đúng 1 lần; >10 test / lỗi build / treo ⇒ giữ KHÔNG ĐẠT; áp dụng cho verify và clone sạch của merge; cảnh báo 'Test chập chờn: <tên>'.


## Kết quả
- Cổng KT và merge không còn chặn oan khi máy bận: test đỏ được chạy lại riêng 1 lần, đạt thì ghi cảnh báo 'Test chập chờn'.
- Kiểm chứng: test nghiệm thu V8-04 + toàn bộ test.

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 5. Thực hiện code | Codex | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
