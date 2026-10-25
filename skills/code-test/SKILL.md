---
name: code-test
description: Viết test xUnit và nghiệm thu có assert thực chất, cô lập dữ liệu và tiến trình.
---
# code-test

## Mục tiêu
Test phải thất bại khi bỏ hành vi cần bảo vệ, không phụ thuộc dữ liệu thật hoặc agent thật.
Giữ nguyên test nghiệm thu do quản lý viết; chỉ thêm ca trong tiêu chí được giao.

## Đầu vào
- Tiêu chí hoàn thành, test nghiệm thu, code được giao và fixture thật task chỉ định.
- Project xUnit hiện có, ProjectReference và entry point solution/CI của dự án.
- Hợp đồng CLI/file, biến thể dữ liệu, helper test sẵn; thiếu mẫu thật thì báo thiếu.

## Đầu ra (định dạng)
- Test xUnit tên mô tả hành vi, Arrange/Act/Assert rõ, đủ ca cho tiêu chí.
- Bảng tiêu chí → lệnh + số pass/fail/skip/warning thật và điều chưa xác minh.
- Không tạo project/test mẫu trùng khi wiring sẵn; không sửa/Skip test nghiệm thu.

## Các bước
1. Tái dùng project test, framework/package theo repo; không tự đổi runner/TFM.
   Nếu task yêu cầu sửa wiring: chỉ sửa ProjectReference/đăng ký solution còn thiếu.
   Kiểm discovery qua entry point thật; project chạy riêng chưa chứng minh CI tìm thấy.
2. Mỗi ca gọi hành vi thật, assert kết quả/side effect cụ thể; lỗi phải làm test đỏ.
   Không return sớm khi thiếu điều kiện, không Assert.True(true), không catch nuốt exception.
   Kiểm ngoại lệ bằng Assert.Throws/ThrowsAsync đúng loại và kết quả phù hợp.
3. Dùng Theory cho dữ liệu biên, không nhân bản hàng chục ca giống nhau.
   Kiểm null/rỗng/trùng, lần đầu/dữ liệu cũ, bản cài/portable khi thiết kế có các luồng đó.
4. Tạo thư mục tạm riêng mỗi test, truyền new HubPaths(tmpRoot, tmpProfile).
   Không đọc/ghi hồ sơ người dùng, file AITeam thật, registry HKLM hoặc cấu hình máy.
   Dọn tài nguyên test trong finally/Dispose; không xóa ngoài thư mục tạm của test.
5. Tên thiết bị DOS: CON, PRN, AUX, NUL, COM1, LPT1,
   cả phần mở rộng (con.txt) và khác hoa thường; kiểm tên hợp lệ để tránh chặn quá mức.
6. Giá trị giống bí mật phải ghép lúc chạy bằng string.Concat và new string.
   Kiểm lệnh bằng Contains phần cốt lõi thay StartsWith vì có thể có tiền tố hợp lệ.
7. Tiến trình con dùng ProjectReference chép DLL, lấy từ AppContext.BaseDirectory.
   Không trỏ bin/Debug, không chạy agy/codex thật; dùng tiến trình giả theo task.
   Đọc stdout/stderr async đồng thời, timeout hữu hạn, dừng cây tiến trình test khi quá hạn.
8. UI chỉ dựng ẩn trên STA/UiThread, CreateControl/PerformLayout, assert LayoutChecker.
   Đặt Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException).
   Không gọi Show/ShowDialog, MessageBox hay Application.Run; tắt chào mừng bằng cờ sẵn.
9. Test liên quan dùng --no-build sau build tương ứng, filter lớp, --blame-hang-timeout 2m.
   Cuối task chạy build/test solution; lệnh quá 3 phút phải dừng và điều tra.

## Checklist bắt buộc
- [ ] Assert thật, bỏ code mới thì test đỏ; không return sớm/Assert.True(true)/nuốt ngoại lệ.
- [ ] Chuẩn hóa đường dẫn ngoài trước ghi; chặn .. và đường dẫn tuyệt đối.
- [ ] Kiểm symlink/reparse point ở đích/các thư mục cha, không chỉ so tiền tố chuỗi đường dẫn.
- [ ] Áp giới hạn tải/giải nén/output; dữ liệu hỏng báo lỗi, không coi rỗng rồi ghi đè.
- [ ] Chỉ dùng thư mục tạm và HubPaths(tmpRoot, tmpProfile), không hồ sơ người dùng/dữ liệu thật.
- [ ] Đủ null/rỗng/trùng, lần đầu/dữ liệu cũ, bản cài/portable theo thiết kế.
- [ ] Đọc fixture thật; thiếu thì báo chưa xác minh, không bịa định dạng output.
- [ ] DOS names có phần mở rộng/khác hoa thường; bí mật ghép string.Concat lúc chạy.
- [ ] Contains cốt lõi lệnh, không StartsWith; ProjectReference + AppContext.BaseDirectory.
- [ ] Giữ BOM UTF-8 file đang có BOM; không debug, đường dẫn máy mình hoặc dữ liệu cá nhân.
- [ ] Không đổi test nghiệm thu, không test agent thật, không mở app/thay cấu hình hệ thống.
- [ ] Báo lệnh, số pass/fail/skip, warning thật; chưa chạy thì ghi chưa chạy.

## Lỗi hay gặp
- AGENTS.md: return sớm hoặc bắt lỗi bỏ qua luôn xanh, không bảo vệ hành vi mới.
- AGENTS.md: khóa ghi thẳng bị hook chặn; ghép lúc chạy, không tắt kiểm tra bí mật.
- AGENTS.md / V4-08: child DLL từ bin/Debug có thể cũ hoặc mất khi clone sạch.
- AGENTS.md: StartsWith sai khi lệnh có tiền tố; assert Contains nội dung có ý nghĩa.
- AGENTS.md: nuốt lỗi đọc rồi ghi lại có thể phá dữ liệu; assert báo lỗi, giữ dữ liệu cũ.
- scaffold-dotnet-test-project: CI không khám phá là lỗi wiring, không tạo project trùng.

## Ví dụ
Tốt — ghép bí mật lúc chạy, kiểm nội dung lệnh có tiền tố.
```csharp
[Fact]
public void Redact_hides_secret_in_prefixed_command()
{
    var secret = string.Concat("sk-", new string('a', 32));
    var command = string.Concat("runner --token ", secret, " --mode test");
    var redacted = Redact(command);
    Assert.DoesNotContain(secret, redacted);
    Assert.Contains("--mode test", redacted);
}
```
Xấu — bỏ kiểm ở nhánh lỗi, còn lại không kiểm hành vi.
```csharp
[Fact]
public void Redact_test()
{
    if (!Directory.Exists(testRoot)) return;
    Assert.True(true);
}
```

## Nguồn
- Đã đọc `%USERPROFILE%/.claude/skills/scaffold-dotnet-test-project/SKILL.md`, Route the Request,
  Establish the Repository Contract, Repair Only the Missing Edge, Verify the Repaired Path.
- `AGENTS.md`, Quy tắc/Test nghiệm thu/Lỗi hay gặp/Tuyệt đối không: cô lập, BOM, bí mật, tiến trình.
- `plans/R-010/brief.md`, code-test; `tasks/S1-03-bon-skill-code-theo-stack.md`, Ràng buộc.
- Ví dụ chỉ minh họa test Redact ở dự án áp dụng; không tự thêm API này vào sản phẩm.

