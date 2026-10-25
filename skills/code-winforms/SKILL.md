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
- R-001 / plans/R-010/brief.md: AutoSize ở con vẫn bị cha cố định cắt chữ; sửa cả chuỗi.
- AGENTS.md: không dựng app thật trong test; thiếu Desktop/Designer phải báo chưa xác minh.
- winforms-expert: logic trong InitializeComponent có thể build nhưng Designer không đọc được.
- AGENTS.md: không test luôn pass, không nuốt ngoại lệ UI hoặc bỏ kiểm khi thiếu control.
- AGENTS.md: giữ nguyên BOM partial/Designer; chỉ sửa trong phạm vi task.

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
- Chưa đọc code HDVTool/ChinaAndroidToolbox vì ngoài danh sách; không khẳng định helper API.
