# V8-07 · GẤP: khóa chết merge ↔ test V8-05 (tên mutex thật trong test)

- **Agent:** A
- **Nhánh:** `agent-a/V8-07-sua-khoa-chet-merge`
- **Ưu tiên:** cao
- **Ngân sách:** ~60 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 20 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới. Sửa lỗi treo hàng giờ (26/10: merge W1-06 treo 5 giờ, V8-06 + cổng KT V6-02/W1-07 treo 3 giờ).
- Gốc: `merge` giữ mutex `Global\AITeam_Merge` rồi chạy test clone sạch; `core.tests/AcceptanceV805SafeMergeTests.cs` dùng ĐÚNG tên mutex thật với `WaitOne()` không giới hạn ⇒ khóa chết. Cổng KT chạy song song cũng kẹt.

## Đọc (CHỈ những file này)
- `team.ps1` (Invoke-Merge: mutex + bước test clone sạch)
- `core.tests/AcceptanceV805SafeMergeTests.cs`
- `core.tests/Support/TestShell.cs`

## Mục tiêu
1. `team.ps1`: tên mutex merge lấy từ biến môi trường `AITEAM_MERGE_MUTEX`, mặc định `Global\AITeam_Merge`.
2. Bước test clone sạch của merge: chạy test với `AITEAM_MERGE_MUTEX` = tên riêng `Global\AITeam_Merge_test_<guid>` cho tiến trình con, và thêm `--blame-hang-timeout 5m --blame-hang-dump-type none`.
3. `AcceptanceV805SafeMergeTests.cs`: KHÔNG dùng tên mutex thật — mỗi test đặt `AITEAM_MERGE_MUTEX` = tên GUID riêng cho tiến trình PowerShell con; mọi `WaitOne` trong script test có giới hạn (≤ 60 giây). Giữ nguyên ý nghĩa assert (đây là sửa test có chủ đích do quản lý giao).
4. Thêm 1 test: khi có tiến trình khác giữ `Global\AITeam_Merge` thật, test V8-05 vẫn chạy xong (không chờ).

## Phạm vi (được sửa những gì)
- `team.ps1`
- `core.tests/AcceptanceV805SafeMergeTests.cs`

## Không làm
- Không đổi luồng merge khác (clone trước push, reset ORIG_HEAD giữ nguyên).

## Test nghiệm thu
- `core.tests/AcceptanceV805SafeMergeTests.cs`

## Tiêu chí hoàn thành
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

## Review (Claude)

## Kết quả