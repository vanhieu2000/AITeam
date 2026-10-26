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
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

## Review (Claude)

## Kết quả