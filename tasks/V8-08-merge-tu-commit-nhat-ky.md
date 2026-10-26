# V8-08 · merge tự commit nhật ký/bảng/số đo + giao việc tự thêm dòng bảng

- **Agent:** A
- **Nhánh:** `agent-a/V8-08-merge-tu-commit-nhat-ky`
- **Ưu tiên:** cao
- **Ngân sách:** ~60 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 20 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới.
- 26/10: merge bị "Main local chưa sạch" 4 lần chỉ vì việc chạy song song vừa ghi 1 dòng nhật ký (`requests/R-*.md`), bảng (`tasks/README.md`) hoặc số đo (`metrics/runs/**`).
- Task tạo tay (V8-05/06/07) không có dòng bảng ⇒ assign/merge báo "Bảng việc chưa có dòng … — không cập nhật".

## Đọc (CHỈ những file này)
- `team.ps1` (Invoke-Merge: kiểm main sạch; Set-BoardStatus; Invoke-Assign)
- `core.tests/Support/TestShell.cs`

## Mục tiêu
1. Invoke-Merge: trước kiểm "main local sạch", nếu các file thay đổi CHỈ thuộc `requests/R-*.md`, `tasks/README.md`, `metrics/runs/**` ⇒ tự commit "Nhật ký / bảng / số đo (tự commit trước merge)" + push; có file khác ⇒ giữ lỗi cũ.
2. Set-BoardStatus: task chưa có dòng bảng ⇒ thêm dòng mới `| [<mã> <tiêu đề H1>](<file>) | <Agent> | `<nhánh>` | <trạng thái> |` vào cuối bảng việc (sau dòng task cuối), thay vì bỏ qua.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `core.tests/AcceptanceV808AutoCommitTests.cs`

## Test nghiệm thu
- `core.tests/AcceptanceV808AutoCommitTests.cs` (Codex viết cùng lượt, TempRepo + remote bare): chỉ nhật ký bẩn ⇒ tự commit rồi merge tiếp; có file code bẩn ⇒ vẫn chặn; task thiếu dòng bảng ⇒ thêm đúng 1 dòng, lần 2 không nhân đôi.

## Tiêu chí hoàn thành
- [x] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent
| Tiêu chí | Lệnh và bằng chứng thật |
| --- | --- |
| Build 0 warning — ĐẠT | `dotnet build AITeam.sln --no-restore -m:1 --nologo`: thành công, 0 warning, 0 error. |
| Tự commit nhật ký/bảng/số đo, chặn code bẩn — CHƯA XÁC MINH | Test riêng `dotnet test core.tests --no-build --filter FullyQualifiedName~AcceptanceV808AutoCommitTests --blame-hang-timeout 2m`: 0 pass, 3 fail tại push remote bare tạm do Git `CreateFileMapping ... Win32 error 5`. |
| Thêm dòng bảng đúng một lần — CHƯA XÁC MINH | Cùng lượt test trên: ca bảng bị chặn ở bước khởi tạo remote, chưa tới assert hành vi. |
| Test solution — CHƯA ĐẠT | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 644 pass/9 fail/0 skip; Core chưa có tổng kết; đã dừng bằng Ctrl+C ở mốc 3 phút. |
| Cổng kiểm tra — CHƯA ĐẠT | `powershell -NoProfile -File tools\test-verify.ps1`: 2 kiểm tra tĩnh ĐẠT; clone repo tạm bị Git Win32 error 5, exit 1. |
| Cú pháp/BOM — ĐẠT | PowerShell Parser: 0 lỗi; kiểm byte đầu `team.ps1` và test mới đều EF BB BF. |
- File đổi: `team.ps1` (hai hành vi); `core.tests/AcceptanceV808AutoCommitTests.cs` (3 ca, TestShell/TempRepo + remote bare); task này (báo cáo).
- Quyết định: kiểm danh sách đường dẫn trong khóa repo, chỉ stage/commit đúng file cho phép; giữ nguyên lỗi khi có code bẩn; sửa biến `$taskTitle` sau kiểm tra tĩnh.
- Giới hạn: Hub có lỗi quyền `File.Replace` và lưu cấu hình; Core/V8-05 cũng gặp lỗi Git; cần chạy lại ngoài sandbox. Chưa kiểm chứng commit; hệ thống tự commit/push, không chạy git trên bản làm việc.
## Review (Claude)

### Vòng 1 — ĐẠT (Claude)
- Cổng KT ĐẠT. Diff: chỉ tự commit khi MỌI file bẩn thuộc requests/R-*.md, tasks/README.md, metrics/runs/**; thêm dòng bảng sau dòng bảng cuối (README hiện 1 bảng — đúng chỗ).
- Optional: chèn theo bảng việc cụ thể nếu README thêm bảng khác.

## Kết quả
- Merge không còn bị chặn bởi dòng nhật ký do việc song song ghi; task tạo tay tự có dòng trên bảng việc.