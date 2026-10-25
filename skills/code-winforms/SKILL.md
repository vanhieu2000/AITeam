---
name: code-winforms
description: Viết và sửa WinForms theo thiết kế, giữ Designer và kiểm bố cục ẩn trên STA.
---
# code-winforms

## Mục tiêu
Tạo Form/UserControl co giãn theo nội dung và DPI, tránh cắt chữ, chồng lấn R-001.
Áp dụng khi task dùng WinForms; giữ kiến trúc, startup và TFM của dự án.

## Đầu vào
- Task đã duyệt, bố cục, tiêu chí và chuỗi tiếng Việt dài nhất.
- Partial chính, Designer, resx; quy ước AutoScaleMode hiện có.
- Helper UiThread, LayoutChecker và fixture được task chỉ định, nếu có.
- Thiếu helper/mẫu thật: báo rõ, không tự nhận đã kiểm dự án khác.

## Đầu ra (định dạng)
- Chỉ file trong phạm vi: partial hành vi, Designer/resx và test cần thiết.
- Báo cáo bảng tiêu chí → lệnh, số test/warning và phần chưa xác minh.
- Phân biệt test ẩn với kiểm Designer và UiSnapshot ở bước nghiệm thu.

## Các bước
1. Tách trạng thái Designer tuần tự hóa khỏi hành vi chạy thật.
   Giữ InitializeComponent đơn giản, field ở Designer; không gọi dịch vụ tại đó.
2. TableLayoutPanel chia hàng/cột; FlowLayoutPanel cho nhóm nút tự xuống dòng.
   AutoSize cho nhãn/hàng nội dung, Percent cho vùng co giãn; Dock/Anchor phù hợp.
3. Theo chuỗi cha-con để tìm Size/MaximumSize làm nội dung không lớn được.
   Không chỉ tăng kích thước cố định; bố trí cả phần tử bên cạnh tránh chồng lấn.
4. Giữ AutoScaleMode hiện có; chọn MinimumSize từ bố cục tối thiểu đọc được.
   Kiểm DPI 100%, 125%, 150%, 200%, chuỗi dài và resize về MinimumSize.
5. Logic async ở hàm trả Task để test await; handler chỉ gọi hàm đó.
   Cập nhật control trên UI thread; await InvokeAsync nếu TFM hỗ trợ và cần chuyển luồng.
6. Test nền dùng STA riêng hoặc UiThread sẵn, dựng form ẩn và dispose cùng luồng.
   Đặt Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException).
   Gọi CreateControl, PerformLayout, rồi LayoutChecker kiểm cắt chữ/chồng lấn.
   Không mở cửa sổ, MessageBox hay vòng Application.Run trong test nền.
   Tắt chào mừng qua tham số/cờ test sẵn; chuyển ngoại lệ STA về test để test đỏ.
7. Chạy test liên quan; nghiệm thu mới dùng UiSnapshot và Designer lưu/mở lại.
   Build không chứng minh Designer mở được; chưa làm thì ghi chưa kiểm.

## Quy ước của dự án
- HDVTool dựng bằng code: MainForm.BuildLayout sau AutoScaleMode.Dpi (96 DPI), MinimumSize(1000,640); không tìm thấy Designer tương ứng cho MainForm/HomeView/SettingsView trong cây App, không tự thêm Designer (HDVTool: `src/HDVTool.App/Shell/MainForm.cs:101`).
- HomeView dùng TableLayoutPanel AutoSize/GrowAndShrink, widget chia cột Percent; vùng gần đây ba cột Percent nhưng hàng vẫn cố định 56: chuỗi dài phải kiểm cả cha (HDVTool: `src/HDVTool.App/Views/HomeView.cs:28`, `src/HDVTool.App/Views/HomeView.cs:76`).
- SettingsView có FlowLayoutPanel `_pages` dọc không wrap, `_pageHost` Fill/AutoScroll; hàng đường dẫn dùng AutoEllipsis. MainForm.RunOnUi kiểm IsDisposed/InvokeRequired rồi BeginInvoke để cập nhật UI (HDVTool: `src/HDVTool.App/Views/SettingsView.cs:23`, `src/HDVTool.App/Views/SettingsView.cs:122`, `src/HDVTool.App/Shell/MainForm.cs:787`).
- ChinaAndroidToolbox giữ InitializeComponent trong Designer, hành vi trong constructor; MainForm dùng AutoScaleMode.Font, MinimumSize(1100,700), AttachUi với SynchronizationContext (ChinaAndroidToolbox: `src/ChinaAndroidToolbox.App/MainForm.cs:22`, `src/ChinaAndroidToolbox.App/MainForm.cs:62`, `src/ChinaAndroidToolbox.App/MainForm.Designer.cs:309`).
- DashboardView.tlpRoot Fill, hai hàng AutoSize, hàng cuối Percent; flpDeviceButtons AutoSize/RightToLeft, không wrap. Constructor gọi UiHelpers.UseVietnameseWrapping cho nhãn dài; chưa đọc thân helper này (ChinaAndroidToolbox: `src/ChinaAndroidToolbox.App/Views/DashboardView.Designer.cs:82`, `src/ChinaAndroidToolbox.App/Views/DashboardView.Designer.cs:169`, `src/ChinaAndroidToolbox.App/Views/DashboardView.cs:35`).
- SettingsView.tlpAdb AutoSize, cột nhãn 170/cột nhập Percent, hàng AutoSize; constructor gọi wrapping cho kết quả/đường dẫn (ChinaAndroidToolbox: `src/ChinaAndroidToolbox.App/Views/SettingsView.Designer.cs:233`, `src/ChinaAndroidToolbox.App/Views/SettingsView.cs:24`).
- Hai UiThread.Run(Action, timeoutMs=60000) đặt ThrowException, chạy STA và ném lại lỗi; chỉ HDVTool đặt IsBackground (HDVTool: `tests/HDVTool.Core.Tests/UiLayout/UiThread.cs:7`; ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/UiThread.cs:9`).
- Ca thật: HomeView_layout_has_no_issues/SettingsView_layout_has_no_issues, Hint_breaks_text_by_words_not_mid_word; phía China có MainForm_WithAdb_LongDeviceName và AcceptanceC011Tests.Text_too_long_for_fixed_size_label_and_button_is_reported (HDVTool: `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:171`, `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:190`, `tests/HDVTool.Core.Tests/UiLayout/VietnameseWordBreakTests.cs:19`; ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:175`, `tests/ChinaAndroidToolbox.Tests/AcceptanceC011Tests.cs:74`).
- **Hiện trạng khác yêu cầu test nền:** Host gọi Show ngoài màn hình; LayoutChecker.Check bỏ cây không Visible. Assert.Empty trên form ẩn có thể luôn xanh; test mới cần cơ chế kiểm ẩn chứng minh bắt được lỗi chủ động, không sao chép cách hiện form; nguồn HDVTool: `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:79`, `tests/HDVTool.Core.Tests/UiLayout/LayoutChecker.cs:17`; ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:37`, `tests/ChinaAndroidToolbox.Tests/UiLayout/LayoutChecker.cs:32`; yêu cầu assert: `AGENTS.md:53`.
- UiSnapshot là exe nghiệm thu STA có Show/DrawToBitmap, không phải helper test nền: HDVTool nhận outDir/rộng/cao và đặt LastRunVersion tránh chào mừng; China yêu cầu outDir, ghi scroll.txt/clip.txt (HDVTool: `tools/UiSnapshot/Program.cs:18`, `tools/UiSnapshot/Program.cs:78`, `tools/UiSnapshot/Program.cs:106`; ChinaAndroidToolbox: `tools/UiSnapshot/Program.cs:11`, `tools/UiSnapshot/Program.cs:104`).
- Các ca scale đã đọc là 1/1.25/1.5, chưa chứng minh DPI thật 200% hoặc Designer round trip (HDVTool: `tests/HDVTool.Core.Tests/UiLayout/ViewLayoutTests.cs:167`; ChinaAndroidToolbox: `tests/ChinaAndroidToolbox.Tests/UiLayout/ViewLayoutTests.cs:145`).

## Checklist bắt buộc
- [ ] R-001: không cắt chữ/chồng lấn ở DPI 100/125/150/200%, chuỗi dài và MinimumSize.
- [ ] TableLayoutPanel/FlowLayoutPanel co giãn hết chuỗi cha-con, không ghim tọa độ theo ảnh.
- [ ] Designer chỉ có dữ liệu tuần tự hóa; giữ field/resx và SuspendLayout/ResumeLayout.
- [ ] Constructor cho Designer không đọc dữ liệu thật hay khởi tạo dịch vụ sản phẩm.
- [ ] Test STA/UiThread dựng ẩn, CreateControl/PerformLayout, LayoutChecker có assert thật.
- [ ] UiSnapshot thuộc nghiệm thu; báo rõ nếu chưa kiểm runtime/Designer round trip.
- [ ] Dữ liệu hiển thị lấy từ model; kiểm null/rỗng/trùng và chuỗi lỗi dài.
- [ ] Giữ BOM UTF-8 file đang có BOM, không debug hoặc đường dẫn máy mình.
- [ ] Build/test theo task; kiểm lỗi async và dispose nếu thay hành vi tương ứng.

## Lỗi hay gặp
- R-001 / `plans/R-010/brief.md:6`: kiểm cắt chữ/chồng lấn, sửa cả chuỗi cha-con; AGENTS.md không ghi mã R-001.
- AGENTS.md: không dựng app thật trong test; thiếu Desktop/Designer phải báo chưa xác minh.
- winforms-expert: logic trong InitializeComponent có thể build nhưng Designer không đọc được.
- `AGENTS.md:53`: không test luôn pass, không nuốt ngoại lệ UI hoặc bỏ kiểm khi thiếu control.
- `AGENTS.md:49`: giữ nguyên BOM partial/Designer; chỉ sửa trong phạm vi task.

## Ví dụ
Tốt — khởi tạo bố cục; hành vi và test nằm ngoài Designer.
```csharp
var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
var caption = new Label { AutoSize = true, Text = titleFromModel };
var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
actions.Controls.Add(new Button { AutoSize = true, Text = "Lưu thay đổi" });
layout.Controls.Add(caption, 0, 0);
layout.Controls.Add(actions, 0, 1);
Controls.Add(layout);
MinimumSize = new Size(420, 280); // Xác nhận lại bằng ma trận DPI của task.
```
Xấu — kích thước chỉ hợp một chuỗi và một DPI.
```csharp
var caption = new Label { Text = titleFromModel, AutoSize = false, Width = 60, Height = 15 };
caption.Location = new Point(300, 10);
Controls.Add(caption);
```

## Nguồn
- Đã đọc `%USERPROFILE%/.claude/skills/winforms-expert/SKILL.md`: Designer Contract,
  Rules That Change the Answer, Implement the UI Structure, Validate in Increasing-Cost Order.
- `plans/R-010/brief.md`, Skill code: R-001, DPI, UiThread/LayoutChecker/UiSnapshot.
- `AGENTS.md`, Lỗi hay gặp: test UI, BOM, dữ liệu hiển thị và kiểm chứng thật.
- Nguồn HDVTool/ChinaAndroidToolbox và giới hạn kiểm chứng: xem Quy ước của dự án; không chạy test UI/UiSnapshot trong task tài liệu.
