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

| Tiêu chí | Lệnh / bằng chứng thực tế lượt sửa cổng KT |
|---|---|
| Khắc phục lỗi sửa test — bị chặn | Phản hồi cấm sửa `AcceptanceV805SafeMergeTests.cs`, nhưng Mục tiêu 3–4 và Phạm vi yêu cầu sửa chính file này; Review (Claude) trống. Chưa có bản gốc được xác minh để khôi phục; không sửa thêm test hoặc né cổng KT. |
| Build 0 warning — chưa đạt | `dotnet build AITeam.sln`: mã 1, 0 warning/0 error nhưng không in nguyên nhân; chạy lại `dotnet build AITeam.sln --no-restore -m:1`: mã 0, 0 lỗi, 4 warning NU1900 (không truy cập NuGet). |
| Test solution — chưa đạt | `dotnet test AITeam.sln --no-build --no-restore --blame-hang-timeout 2m --blame-hang-dump-type none -m:1`: Hub 644 pass/9 fail/0 skip (có lỗi quyền File.Replace); Core có 2 ca V8-05 fail do Git CreateFileMapping error 5, chưa có tổng kết. Dừng tại giới hạn 3 phút. |
| Cổng kiểm tra — chưa đạt | `powershell -NoProfile -File tools\test-verify.ps1`: mã 1; 2 kiểm tra tĩnh ĐẠT; kịch bản 1 dừng do Git sh.exe CreateFileMapping, Win32 error 5. |
| Cú pháp và BOM | Parser PowerShell `team.ps1`: 0 lỗi; ReadAllBytes xác nhận `team.ps1` và file test còn EF BB BF. |
- File đổi lượt này: chỉ file task, cập nhật kết quả thật; giữ nguyên mã sản phẩm và test từ lượt trước.
- Quyết định: không hoàn tác yêu cầu cô lập mutex khi chưa có chỉ dẫn giải quyết mâu thuẫn; cần quản lý xác định ngoại lệ cổng KT hoặc sửa thiết kế/phạm vi.
- Chưa xác minh: toàn bộ test pass, build 0 warning, cổng KT đạt, bản gốc test; không chạy app/chụp ảnh, không tạo công cụ phụ.
- Không chạy lệnh git trực tiếp; chưa có commit được kiểm, hệ thống tự commit/push theo chỉ dẫn Codex.
## Review (Claude)

## Kết quả