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
