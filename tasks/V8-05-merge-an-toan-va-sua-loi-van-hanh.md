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
- [x] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

| Tiêu chí | Bằng chứng thật |
|---|---|
| 1. Clone trước push, hủy merge đỏ | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceV805SafeMergeTests&FullyQualifiedName!~Merge_checks_local_clone" --blame-hang-timeout 2m -m:1`: 9/9 pass; 4 ca giả lập kiểm thứ tự/reset; 2 ca repo bare bị chặn `sh.exe` Win32 error 5. |
| 2. Mutex, clone GUID, dọn clone | Cùng lượt 9/9: ca hai tiến trình chờ mutex và nhả khi lỗi; các ca merge kiểm clone đã dọn. |
| 3. Chặn xóa test | Cùng lượt 9/9: xóa `core.tests/Regression.cs` bị ghi lỗi, file thường không bị chặn. |
| 4. Model không hợp lệ | Cùng lượt 9/9: trạng thái đúng, nhánh xử lý return trước verify. |
| 5. Models bỏ stderr tiến độ | Cùng lượt 9/9: còn `model-id`, bỏ dòng Fetching, khôi phục ErrorActionPreference. |
| 6. Prep retry lỗi máy | Cùng lượt 9/9: MSB3491 gọi build 2 lần, shutdown 1 lần; nhận MSB3027, không nhận lỗi code kèm theo. |
| 7. Chia đều gần nhất | Cùng lượt 9/9: A/B, giữ đặt chỗ cùng task, không đếm trùng bảng, hết hạn 10 phút. |
| 8. Shutdown và đợi retry | Cùng lượt 9/9: sau Codex có shutdown, retry build đợi đúng 20 giây (mock thời gian). |
| Build 0 warning | `dotnet restore AITeam.sln --disable-parallel -v minimal -m:1` đạt; `dotnet build AITeam.sln --no-restore -m:1`: 0 lỗi, 0 warning; hai script parse 0 lỗi, cả 3 file mã giữ/có BOM. |
| Test solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 597 pass/9 fail (Access denied khi ReplaceFile và kiểm lưu); Core có 2 ca Git lỗi quyền, chưa tổng kết; chủ động dừng gần 3 phút, mã 1. |
| Cổng kiểm tra | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: mã 1; báo `$note` trùng tham số tại Invoke-CodexDevelop (không sửa ngoài mục tiêu), rồi clone repo tạm bị Win32 error 5. |
| File đổi | `team.ps1`: 8 mục tiêu; `tools/workflow-common.ps1`: nhận MSB3027; `core.tests/AcceptanceV805SafeMergeTests.cs`: 11 ca; file task này: báo cáo. Không sửa test task khác. |
| Quyết định / chưa chắc | Dùng `rev-list --first-parent origin/main..main` để chỉ xét commit trên main; từ chối main bẩn và pull ff-only trước thao tác có thể reset. Chưa nghiệm thu Git thật/toàn solution; không chạy git trên repo làm việc, chưa có commit được kiểm; hệ thống tự commit/push. |

## Review (Claude)

## Kết quả
