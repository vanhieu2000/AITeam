---
name: code-app
description: Viết MAUI hoặc CLI theo task với vòng đời, binding, DI và tiến trình có timeout.
---
# code-app

## Mục tiêu
Giữ MAUI ổn định qua lifecycle và CLI có mã thoát/lỗi rõ ràng.
Áp dụng phần MAUI hoặc console theo stack thực tế, không tự thêm nền tảng.

## Đầu vào
- Task, TFM/nền tảng, Main hoặc MauiProgram và hợp đồng dữ liệu hiện có.
- MAUI: page/viewmodel, cấu hình DI, Shell route và binding trong phạm vi.
- CLI: tham số, mã thoát, timeout, mẫu output/fixture thật và test sẵn.
- Thiếu workload/mẫu thật: báo chưa xác minh, không cài toàn hệ thống hay đoán định dạng.

## Đầu ra (định dạng)
- Chỉ file ứng dụng/test trong phạm vi, giữ chữ ký khung/interface.
- CLI thành công mã 0; lỗi ra stderr tiếng Việt với LỖI:, mã khác 0.
- Báo cáo tiêu chí → lệnh/test; nêu MAUI runtime/nền tảng chưa kiểm.

## Các bước
1. MAUI lifecycle theo Window Created/Activated/Stopped/Resumed/Destroying.
   Stopped hủy tác vụ chờ; Resumed làm mới phần cần thiết, đăng ký lại cân đối.
   Lưu trạng thái khôi phục kịp thời; không trông chờ luôn có sự kiện kết thúc.
2. Data binding nối view/viewmodel, giảm event trong code-behind.
   Gán BindingContext, thông báo PropertyChanged khi giá trị thay đổi.
   Sửa collection trên main thread; ObservableCollection không tự chuyển luồng.
3. DI đăng ký page/viewmodel/service tại MauiProgram theo lifetime cần dùng.
   Inject qua constructor; không tự new dependency hay thêm interface/DI ngoài task.
4. Shell navigation có route rõ, đăng ký trang chi tiết trước GoToAsync.
   Không trùng route; phân biệt route cây Shell và trang đẩy lên stack.
   Kiểm tham số navigation như dữ liệu ngoài, kiểm luồng quay lại theo thiết kế.
5. Main bao try/catch cả parse, khởi tạo, chạy và await; bắt mọi ngoại lệ.
   Console.Error.WriteLine ra stderr với LỖI: …, trả mã khác 0 thay vì văng ra Windows.
   Khung chưa làm bắt NotImplementedException riêng, in LỖI: chưa làm và trả 1.
6. ProcessStartInfo dùng ArgumentList, UseShellExecute = false và đường dẫn exe tin cậy.
   Không ghép shell hoặc chèn input vào dòng lệnh.
   Redirect stdout/stderr, đọc output async đồng thời từ lúc Start để không chặn.
   WaitForExitAsync có cancellation/timeout; quá hạn dừng cây tiến trình do mình tạo,
   chờ kết thúc, dispose và báo lỗi; giới hạn output thu vào bộ nhớ.
7. Test tiến trình dùng ProjectReference chép DLL tới AppContext.BaseDirectory.
   Không trỏ bin/Debug, không chạy agy/codex thật; dùng stub/fixture theo task.
8. Test các nhánh lỗi; MAUI chỉ kiểm nền tảng có công cụ và báo phần chưa chạy.

## Checklist bắt buộc
- [ ] MAUI lifecycle hủy/khôi phục tác vụ, subscription cân đối, resume không đăng ký trùng.
- [ ] Binding cập nhật viewmodel; sửa collection trên main thread; DI đúng lifetime.
- [ ] Shell navigation route duy nhất, kiểm tham số và trạng thái cần giữ khi quay lại.
- [ ] Main bắt mọi ngoại lệ; stderr LỖI: và mã khác 0; NotImplementedException trả 1.
- [ ] ArgumentList, UseShellExecute = false, timeout, đọc output không chặn và giới hạn output.
- [ ] Chuẩn hóa đường dẫn, chặn .., tuyệt đối, symlink/reparse point trước ghi; giới hạn tải.
- [ ] Dữ liệu hỏng báo lỗi, không nuốt rồi ghi đè; test chỉ dùng thư mục tạm/HubPaths.
- [ ] Đủ null/rỗng/trùng, lần đầu/dữ liệu cũ, bản cài/portable theo thiết kế, fixture thật.
- [ ] Giữ BOM UTF-8, bỏ debug/đường dẫn máy mình; không đổi cấu hình máy khi test.

## Lỗi hay gặp
- AGENTS.md / R-006: exception thoát Main có thể bật hộp lỗi dotnet.exe trên máy chủ.
- AGENTS.md / V4-08: bin/Debug có thể cũ; dùng ProjectReference và AppContext.BaseDirectory.
- AGENTS.md: không nuốt lỗi đọc rồi ghi đè; thiếu fixture thì báo, không đoán output.
- Microsoft MAUI: binding thông báo thuộc tính không đồng nghĩa collection an toàn mọi luồng.
- Microsoft MAUI: route trùng gây ArgumentException; resume cần tránh subscription lặp.

## Ví dụ
Tốt — Main bao cả async và khung chưa làm.
```csharp
static async Task<int> Main(string[] args)
{
    try
    {
        await RunAsync(args);
        return 0;
    }
    catch (NotImplementedException)
    {
        Console.Error.WriteLine("LỖI: chưa làm");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"LỖI: {ex.Message}");
        return 1;
    }
}
```
Xấu — lỗi async thoát điểm vào, không có chẩn đoán stderr.
```csharp
static async Task<int> Main(string[] args)
{
    await RunAsync(args);
    return 0;
}
```

## Nguồn
- Đã đọc [MAUI app-lifecycle](https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle).
- Đã đọc [MAUI data-binding](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/?view=net-maui-10.0).
- Đã đọc [MAUI dependency-injection](https://learn.microsoft.com/dotnet/maui/fundamentals/dependency-injection).
- Đã đọc [MAUI shell/navigation](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/navigation).
- `AGENTS.md`, Lỗi hay gặp R-006/V4-08; `plans/R-010/brief.md`, mục code-app.
- Không dùng skill maui-* cục bộ; task tài liệu không chạy workload/app MAUI thật.

