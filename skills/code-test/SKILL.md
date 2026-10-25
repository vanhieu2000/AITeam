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

## Quy ước của dự án
- Hub: PageTests inject FakeTeamConfigService/FakeTaskBoardService/FakeAgentRunService, gọi OnGet/handler trực tiếp, assert PartialViewResult.ViewName/Model; đây là test PageModel, không render Razor (`hub.tests/PageTests.cs:14`, `hub.tests/PageTests.cs:33`).
- AcceptanceV504OfficeTests.Page_loads_view_or_reports_error dùng FakeOffice kiểm dữ liệu/lỗi; Desks_handler_returns_partial kiểm tên partial và model. Razor_markup_follows_layout chỉ File.ReadAllText + Contains/DoesNotContain; chưa chứng minh HTML/token render thật (`hub.tests/AcceptanceV504OfficeTests.cs:151`, `hub.tests/AcceptanceV504OfficeTests.cs:169`, `hub.tests/AcceptanceV504OfficeTests.cs:177`).
- Kiểm render cần hạ tầng thực thi Razor rồi assert HTML/token/encode; hai file trên không gọi helper render. Dữ liệu đĩa phải dùng thư mục tạm + new HubPaths(tmpRoot,tmpProfile); không suy ra PageTests đã dùng HubPaths (`AGENTS.md:76`, `hub.tests/PageTests.cs:14`).
- HDVTool: ViewLayoutTests.HomeView_layout_has_no_issues/SettingsView_layout_has_no_issues dùng UiThread.Run, scale 1/1.25/1.5 và LayoutChecker.Check; VietnameseWordBreakTests.Hint_breaks_text_by_words_not_mid_word dùng chuỗi dài/nhiều chiều rộng (HDVTool: `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:157`, `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:190`, `tests/HDVTool.Core.Tests/UiLayout/VietnameseWordBreakTests.cs:11`).
- ChinaAndroidToolbox: ViewLayoutTests.MainForm_WithAdb_LongDeviceName/View_Dashboard/View_Settings dùng tên dài; TestUiContext.Create dùng thư mục tạm, ToolboxPaths và FakeProcessRunner; AcceptanceC011Tests kiểm lỗi chồng/tràn/cắt chữ cùng ca flow dài hợp lệ (ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:175`, `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:231`, `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:297`, `tests/ChinaAndroidToolbox.Tests/UiLayout/TestUiContext.cs:22`, `tests/ChinaAndroidToolbox.Tests/AcceptanceC011Tests.cs:37`).
- Hai UiThread.Run đặt ThrowException, STA, timeout 60 giây và ném lại lỗi; nhưng Host hiện gọi Show ngoài màn hình. Không sao chép cho test nền dựng ẩn. Hai LayoutChecker bỏ Visible=false: cần chứng minh ca cố tình sai bị phát hiện trước khi tin Assert.Empty (HDVTool: `tests/HDVTool.Core.Tests/UiLayout/UiThread.cs:7`, `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:79`, `tests/HDVTool.Core.Tests/UiLayout/LayoutChecker.cs:17`; ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/UiThread.cs:9`, `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:37`, `tests/ChinaAndroidToolbox.Tests/UiLayout/LayoutChecker.cs:32`; yêu cầu: `AGENTS.md:53`).
- UiSnapshot là exe nghiệm thu có Show/DrawToBitmap; không chạy trong test nền hoặc nhận là bằng chứng test ẩn (HDVTool: `tools/UiSnapshot/Program.cs:83`; ChinaAndroidToolbox: `tools/UiSnapshot/Program.cs:40`).
- CLI: ReviewCliTests.Missing_args_returns_2_and_auto_medium_task_returns_4_with_skip_json gọi ProcessRunner timeout 30 giây, kiểm 2/4 và JSON SKIPPED; DevRunnerCliTests kiểm thiếu tham số/unknown provider, helper đọc stdout/stderr đồng thời nhưng chưa có timeout (`core.tests/ReviewCliTests.cs:13`, `core.tests/DevRunnerCliTests.cs:14`, `core.tests/DevRunnerCliTests.cs:34`).
- Wiring thật: core.tests có ProjectReference tới CLI, lấy Path.Combine(AppContext.BaseDirectory,"aiteam.dll"); không thay bằng bin/Debug. Dữ liệu CLI ghi trong thư mục GUID tạm (`core.tests/AITeam.Core.Tests.csproj:19`, `core.tests/ReviewCliTests.cs:8`, `core.tests/ReviewCliTests.cs:16`).

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
- `AGENTS.md:53`: return sớm hoặc bắt lỗi bỏ qua luôn xanh, không bảo vệ hành vi mới.
- `AGENTS.md:51`: khóa ghi thẳng bị hook chặn; ghép lúc chạy, không tắt kiểm tra bí mật.
- `AGENTS.md:48` / V4-08: child DLL từ bin/Debug có thể cũ hoặc mất khi clone sạch.
- `AGENTS.md:54`: StartsWith sai khi lệnh có tiền tố; assert Contains nội dung có ý nghĩa.
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

