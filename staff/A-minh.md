# Minh — nhân viên mã A

- **Tên:** Minh
- **Mã:** A (nhánh `agent-a/…`, lời giao việc / commit vẫn ghi "Agent A")
- **Chuyên môn:** Nền tảng: kiến trúc, build, đóng gói, CSDL, hạ tầng
- **Chạy bằng:** Antigravity CLI (agy), tài khoản đang đăng nhập.

Tên chỉ dùng trong log và đánh giá. Bảng điểm: `.\team.ps1 staff-report` (chỉ tính task có dòng **Yêu cầu:** R-xxx).

## Lưu ý riêng
<!-- Mỗi dòng "- ..." được tự đưa vào lời giao việc lần sau. Chỉ ghi lỗi AG đã lặp ≥ 2 lần hoặc nghiêm trọng (bảo mật). Bỏ dòng khi 3 nhiệm vụ liền không tái phạm. -->
- Test giao diện: KHÔNG Show()/ShowDialog()/MessageBox/Application.Run trên luồng test; luôn chạy qua helper `UiThread` (STA + timeout); `dotnet test` luôn kèm `--blame-hang-timeout 2m`. (Lỗi 21/10 làm treo 2 lượt × 30 phút.)

## Nhận xét theo nhiệm vụ
| Ngày | Yêu cầu / Task | Kết quả (lượt · phút · vòng · dòng/ngân sách) | Làm tốt | Lỗi (AG) | Hướng cải thiện |
|---|---|---|---|---|---|
| 21/10 | R-001 / T-005-ui-layout-tests (HDVTool) | 4 lượt · ~95 phút (2 lượt treo ~30 phút) · 3 vòng · 803/800 dòng | Bộ kiểm tra bố cục đủ 3 tỉ lệ DPI; sửa bố cục thật (ô tìm kiếm, thẻ Trang chủ, Người dùng); tắt hộp chào mừng bằng cờ internal, không đổi app thật | [AG] (test) test mở form/hộp thoại thật trên luồng MTA + chạy test nền ⇒ treo · [AG] (giao diện) sót thẻ "Hệ thống" bị cắt ở Giới thiệu | Dùng helper UiThread cho mọi test giao diện; xem kỹ ảnh chụp trước khi nộp |
| 23/10 | R-002 / V4-03 Codex review (AITeam) | 1 lượt · 9 phút · 1 vòng · 321/250 dòng | Qua cổng kiểm tra lượt đầu; đủ luật fail-safe | — (lỗi BOM là do test nghiệm thu của Claude thiếu) | Giữ phong độ; tên file tạm nên duy nhất |

## Đề xuất cải tiến cho nhân viên này
<!-- Claude cập nhật sau mỗi kỳ: đổi model, chia task kiểu khác, thêm lưu ý riêng, đổi vùng code phụ trách... -->