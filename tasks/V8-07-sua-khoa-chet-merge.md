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

| Tiêu chí | Lệnh / bằng chứng thực tế |
|---|---|
| Build 0 warning — chưa đạt | `dotnet build AITeam.sln --no-restore -m:1`: thành công, 0 lỗi, 4 warning NU1900 do NuGet không truy cập được; lượt build ban đầu chưa restore thất bại. |
| Test solution — chưa đạt | `dotnet test AITeam.sln --no-restore --no-build --blame-hang-timeout 2m --blame-hang-dump-type none -m:1`: Hub 644 pass/9 fail/0 skip (có lỗi quyền File.Replace); Core chưa kết thúc, đã dừng cả lệnh khi quá 3 phút. |
| Cổng kiểm tra — chưa đạt | `powershell -NoProfile -File tools\test-verify.ps1`: 2 kiểm tra tĩnh ĐẠT; kịch bản 1 lỗi Git sh.exe CreateFileMapping, Win32 error 5. |
| Cô lập mutex, chờ hữu hạn, hồi quy khóa thật | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceV805SafeMergeTests" --blame-hang-timeout 2m --blame-hang-dump-type none -m:1`: 10 pass/2 fail/0 skip; ca khóa thật pass, 2 ca Git tạm lỗi sandbox trước logic merge. |
| Mutex cấu hình, test clone riêng + timeout 5m | `team.ps1`: đọc biến môi trường, mặc định khóa thật; đổi biến riêng quanh test/retry và khôi phục bằng finally; Parser PowerShell: 0 lỗi. Chưa kiểm chứng clone thật vì sandbox. |
| BOM | Kiểm tra byte đầu bằng ReadAllBytes: `team.ps1` và `core.tests/AcceptanceV805SafeMergeTests.cs` đều còn EF BB BF. |
- File đổi: `team.ps1` sửa mutex/timeout; `core.tests/AcceptanceV805SafeMergeTests.cs` cô lập khóa, giữ assert cũ, thêm đúng 1 ca hồi quy và kiểm môi trường clone; file task này ghi báo cáo.
- Quyết định: sửa test nghiệm thu theo ngoại lệ có chủ đích ở mục Mục tiêu; nếu khóa thật đã bị tiến trình ngoài giữ thì ca hồi quy không chờ lấy khóa đó.
- Chưa xác minh: toàn bộ test pass, build 0 warning, cổng kiểm tra đầy đủ; cần chạy lại ngoài sandbox. Không chạy app/chụp ảnh.
- Không chạy lệnh git trực tiếp; chưa có commit được kiểm, hệ thống tự commit/push theo chỉ dẫn Codex.

## Review (Claude)

## Kết quả