# Chuẩn kiểm thử v6 cho mọi dự án (19/11/2026)

## Test những gì
Chỉ viết test có trong mục **Kế hoạch test** của `docs/KE-HOACH-DU-AN.md`. Mỗi chức năng `CN-xx` gồm:

| Loại ca | Số lượng | Kiểm tra |
|---|---|---|
| **Đúng** | 1 ca | Luồng chính cho kết quả đúng |
| **Lỗi** | Các ca quan trọng | Dữ liệu sai, thiếu quyền, lỗi đọc ghi; không mất dữ liệu, thông báo đúng |
| **Liên kết** | 1 ca | Thao tác trên giao diện chạy xuống backend thật, kết quả hiện lại đúng |

Không viết test bố cục, không chụp ảnh, không viết test na ná nhau. Chủ dự án tự test giao diện ở bước cuối.

## Chạy test khi nào
| Lúc nào | Ai chạy | Chạy gì | Giới hạn |
|---|---|---|---|
| Đang làm | Agent | Test của chức năng đang làm | Dưới 1 phút |
| Cổng kiểm tra | Tự động | Build và **Test vùng** của task | Dưới 3 phút |
| Merge | Tự động | Toàn bộ test trên clone sạch | **1 lần duy nhất** |

Chỉ chạy lặp khi đã có test sập ngẫu nhiên, và chỉ chạy nhóm test liên quan.

## Giữ test nhanh và đúng
- Mỗi test dưới 2 giây, mỗi lớp test dưới 30 giây.
- Dữ liệu mẫu tốn thời gian tạo một lần rồi chép cho từng test.
- Test đỏ thì sửa code. Test sai so với kế hoạch thì báo Claude sửa kế hoạch trước.
- Test nghiệm thu là bất biến: không xóa, không `Skip`, không sửa assert.
- Phần chưa chạy phải ghi rõ là chưa xác minh.

## Review
- Tối đa 2 lượt. Lượt đầu gom hết lỗi và giao sửa một lần.
- Lượt 2 chỉ kiểm tra phần đã sửa. Chỉ lỗi mất dữ liệu, bảo mật hoặc hỏng chức năng chính mới được trả lại.
- Lỗi nhỏ ghi vào hàng chờ trong `docs/KE-HOACH-DU-AN.md`.

## Ca bắt buộc riêng của AITeam
- **Lệnh `team.ps1`:** thành công, sai tham số, mã thoát, đường dẫn có dấu; repo tạm, git và AI giả, không push thật.
- **Dịch vụ C#:** null, rỗng, trùng, biên, lỗi I/O; giữ BOM và CRLF; chặn `..` và đường dẫn tuyệt đối.
- **Trang Hub:** GET và POST thật, antiforgery, XSS; không đọc profile hay repo thật.

Test mới không tự dựng `ProcessStartInfo`, dùng helper chung:

| Loại | Helper |
|---|---|
| Tiến trình, timeout, UTF-8 | `core.tests/Support/TestShell.cs` |
| Nạp hàm `team.ps1`, fake agy/codex/git | `core.tests/Support/TeamScript.cs` |
| Repo tạm, git thật | `TempRepo` (trong `TestShell.cs`) |
| Chép file nguồn thật | `core.tests/Support/RealFile.cs` |
| Host Razor thật | `hub.tests/Support/HubTestHost.cs` |
| Render trang/partial | `hub.tests/Support/HubPageRenderer.cs` |
