# V8-05 · Merge an toàn + cổng KT chặn lách test + sửa lỗi vận hành 25/10

- **Agent:** A
- **Nhánh:** `agent-a/V8-05-merge-an-toan-va-sua-loi-van-hanh`
- **Ưu tiên:** cao
- **Ngân sách:** ~250 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 30 phút
- **Review AI:** có
- **Độ khó:** M

## Tóm tắt
- Không có chức năng mới cho người dùng; sửa 7 lỗi vận hành gặp ngày 25/10 (đã duyệt trong plans/R-008/plan.md mục "Bổ sung").
- Quan trọng nhất: `merge` hiện push main TRƯỚC khi test clone sạch ⇒ test đỏ lọt lên main (S1-04). Phải kiểm clone sạch trước, đỏ thì không push.

## Đọc (CHỈ những file này)
- `team.ps1` (Invoke-Merge, Invoke-Verify, Invoke-Assign, Invoke-Prep, Select-FreeAgent, lệnh models)
- `tools/workflow-common.ps1` (Invoke-RepoTransaction, Test-InfrastructureFailure, Invoke-FailedTestRetry)
- `core.tests/Support/TestShell.cs` (BẮT BUỘC dùng cho test)

## Mục tiêu (7 điểm, mỗi điểm 1+ test)
1. **merge — clone sạch trước push:** merge --no-ff vào main LOCAL → clone từ repo LOCAL (đường dẫn) vào thư mục tạm riêng mỗi lần → build + test (dùng Invoke-FailedTestRetry) → ĐẠT mới push main; KHÔNG ĐẠT ⇒ `git reset --hard ORIG_HEAD` trên main local (chỉ khi commit merge CHƯA push), in "Clone sạch KHÔNG ĐẠT — đã hủy merge, main không đổi", bảng việc giữ 🔵, thoát mã 1.
2. **Khóa merge toàn máy:** mutex tên cố định quanh toàn bộ merge; merge thứ hai chờ tối đa 30 phút rồi báo "Đang có merge khác chạy". Thư mục clone sạch duy nhất mỗi lần (GUID), dọn sau khi xong.
3. **Cổng KT chặn xóa test:** diff `origin/main...HEAD --diff-filter=D` có file trong `*tests/**/*.cs` ⇒ PHẢI SỬA "Xóa file test: <đường dẫn>" (W1-05 25/10 xóa test hồi quy để qua cổng).
4. **Model không hợp lệ:** log agy có `invalid model selection` ⇒ dừng ngay: bảng "🔴 Lỗi agent · model không hợp lệ", KHÔNG build/verify, KHÔNG tự giao lại.
5. **Lệnh `models`:** dòng "Fetching available models..." trên stderr không phải lỗi; in danh sách id.
6. **prep thử lại lỗi máy:** build khung lỗi kiểu hạ tầng (Test-InfrastructureFailure: MSB3491/MSB3027/Access denied obj) ⇒ thử lại 1 lần trước khi "Build khung LỖI ⇒ KHÔNG giao" (3 lần 25/10 do 2 prep song song).
7. **Chia đều khi giao song song:** Select-FreeAgent tính cả task vừa đặt 🟡 trong 10 phút gần nhất ghi ở `.state/assign-recent.json` (ghi trong khóa), để 2 prep cách nhau vài phút không cùng về 1 agent.

8. **Sau lượt Codex code:** chạy `dotnet build-server shutdown` trước cổng KT; build lỗi MSB3491/Access denied obj ⇒ chờ 20 giây rồi thử lại (W1-06, V8-03 ngày 26/10: lỗi ngay sau Codex, build tay vài phút sau sạch).

## Phạm vi (được sửa những gì)
- `team.ps1`
- `tools/workflow-common.ps1`
- `core.tests/AcceptanceV805SafeMergeTests.cs`

## Không làm
- Không force-push; không reset main đã push; không đổi định dạng bảng việc / nhật ký.
- Không sửa test nghiệm thu của task khác.

## Ràng buộc
- `git reset --hard ORIG_HEAD` CHỈ khi `git rev-list origin/main..main` chứa đúng commit merge vừa tạo; ngược lại báo lỗi, không reset.
- Test dùng repo git tạm (TempRepo) + remote bare tạm; không chạm repo thật / GitHub.

## Test nghiệm thu
- `core.tests/AcceptanceV805SafeMergeTests.cs` (Codex viết cùng lượt): clone sạch đỏ ⇒ main local quay về, remote không đổi; xanh ⇒ push; 2 merge song song ⇒ cái sau chờ; xóa file test ⇒ PHẢI SỬA; log "invalid model selection" ⇒ dừng không verify; models bỏ qua "Fetching"; prep MSB3491 lần 1 ⇒ thử lại; chia đều với assign-recent.

## Tiêu chí hoàn thành
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

## Review (Claude)

## Kết quả
