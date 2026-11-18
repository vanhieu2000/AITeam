# Chuẩn test v6 cho mọi dự án (19/11/2026)

Mục này THẮNG các mục cũ bên dưới khi có khác biệt.

## Test những gì
Chỉ viết test có trong mục **Kế hoạch test** của `docs/KE-HOACH-DU-AN.md`. Mỗi chức năng `CN-xx` gồm:

| Loại ca | Số lượng | Kiểm tra |
|---|---|---|
| **Đúng** | 1 ca | Luồng chính cho kết quả đúng |
| **Lỗi** | Các ca lỗi quan trọng | Dữ liệu sai, thiếu quyền, lỗi đọc ghi; không mất dữ liệu, có thông báo đúng |
| **Liên kết** | 1 ca | Kích hoạt thao tác trên giao diện, chạy xuống backend thật, kết quả hiện lại đúng trên giao diện |

Không viết test bố cục, không chụp ảnh, không kiểm màu sắc: chủ dự án tự test giao diện ở bước cuối. Không viết các test na ná nhau.

## Chạy test khi nào
| Lúc nào | Ai chạy | Chạy gì | Giới hạn |
|---|---|---|---|
| Đang làm | Agent | Test của chức năng đang làm | Dưới 1 phút mỗi lần |
| Cổng kiểm tra | Tự động | Build và **Test vùng** của task | Dưới 3 phút |
| Merge | Tự động | Toàn bộ test trên bản clone sạch | **1 lần duy nhất** |
| Bước cuối | Chủ dự án | Thử giao diện | Không |

Chỉ chạy lặp khi đã có test sập ngẫu nhiên, và chỉ chạy nhóm test liên quan.

## Giữ test nhanh
- Mỗi test dưới 2 giây. Mỗi lớp test dưới 30 giây.
- Dữ liệu mẫu tốn thời gian (băm mật khẩu, CSDL lớn) tạo một lần rồi chép cho từng test.
- Test đỏ thì sửa code. Test sai so với kế hoạch thì báo Claude sửa kế hoạch trước.

## Review
- Tối đa 2 lượt. Lượt đầu gom hết lỗi và giao sửa một lần.
- Lượt 2 chỉ kiểm tra phần đã sửa. Chỉ lỗi mất dữ liệu, bảo mật hoặc hỏng chức năng chính mới được trả lại.
- Lỗi nhỏ ghi vào hàng chờ trong `docs/KE-HOACH-DU-AN.md`.

---

# Chuẩn kiểm thử AITeam

Mỗi thay đổi logic phải có test. Trước khi báo xong: `dotnet build AITeam.sln -warnaserror` và
`dotnet test AITeam.sln --no-build`. Test đỏ là phần phải sửa bằng code, không sửa assert.

Test mới không tự dựng `ProcessStartInfo`; dùng helper chung bên dưới. Cổng `team.ps1 verify`
chỉ cảnh báo (không chặn) khi file test mới thêm tự dựng tiến trình.

## Ca bắt buộc

### Lệnh `team.ps1`
- Thành công, thiếu/sai tham số, mã thoát, stderr tiếng Việt.
- Đường dẫn/tên có dấu, khoảng trắng; repo tạm, không đọc/ghi dữ liệu thật hay `%USERPROFILE%`.
- Git/AI phải là giả, không push thật.

### Dịch vụ C#
- Đầu vào null/rỗng/trùng, giá trị biên, lỗi I/O; không nuốt lỗi rồi ghi đè.
- Ghi file: giữ BOM/CRLF, chặn `..` và đường dẫn tuyệt đối trước khi ghi.

### Trang Hub
- GET/POST thật, thiếu/sai antiforgery, XSS còn encode, trạng thái lỗi.
- Không đọc profile/repo thật; host chỉ nghe loopback cổng tự chọn.

### Tài liệu và skill
- Đúng thứ tự mục, không TODO, trong giới hạn dòng; checklist bắt buộc đủ ý.
- Đọc file nguồn cố định, không dò dữ liệu vận hành.

## Helper có thật trong repo

| Loại | Helper |
|---|---|
| Tiến trình, timeout, UTF-8 | `core.tests/Support/TestShell.cs` |
| Nạp hàm `team.ps1`, fake agy/codex/git | `core.tests/Support/TeamScript.cs` |
| Repo tạm, git thật | `TempRepo` (trong `TestShell.cs`) |
| Chép file nguồn thật | `core.tests/Support/RealFile.cs` |
| Host Razor thật | `hub.tests/Support/HubTestHost.cs` |
| Render trang/partial | `hub.tests/Support/HubPageRenderer.cs` |

## Mẫu test ngắn

```csharp
[Fact]
public async Task Thieu_cau_hinh_thi_bao_loi()
{
    using var repo = new TempRepo(git: false);
    var r = await TestShell.PowerShell(repo.Root, "throw 'thiếu cấu hình'");
    Assert.NotEqual(0, r.Code);
    Assert.Contains("thiếu cấu hình", r.Error);
}
```

## Lưu ý
- Test nghiệm thu của Claude là bất biến: không xóa, không `Skip`, không sửa assert.
- Phần chưa chạy phải ghi rõ là chưa xác minh; không báo pass khi chưa chạy.
