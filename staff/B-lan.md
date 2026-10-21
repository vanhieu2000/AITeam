# Lan — nhân viên mã B

- **Tên:** Lan
- **Mã:** B (nhánh `agent-b/…`, lời giao việc / commit vẫn ghi "Agent B")
- **Chuyên môn:** Tính năng: module, màn hình, nghiệp vụ
- **Chạy bằng:** Antigravity CLI (agy), tài khoản đang đăng nhập.

Tên chỉ dùng trong log và đánh giá. Bảng điểm: `.\team.ps1 staff-report` (chỉ tính task có dòng **Yêu cầu:** R-xxx).

## Lưu ý riêng
<!-- Mỗi dòng "- ..." được tự đưa vào lời giao việc lần sau. Chỉ ghi lỗi AG đã lặp ≥ 2 lần hoặc nghiêm trọng (bảo mật). Bỏ dòng khi 3 nhiệm vụ liền không tái phạm. -->
- Test giao diện: KHÔNG Show()/ShowDialog()/MessageBox/Application.Run trên luồng test; control có kéo-thả (AllowDrop) bắt buộc luồng STA — luôn chạy qua helper `UiThread`; `dotnet test` luôn kèm `--blame-hang-timeout 2m`. (Lỗi 21/10 làm treo 2 lượt × 30 phút.)

## Nhận xét theo nhiệm vụ
| Ngày | Yêu cầu / Task | Kết quả (lượt · phút · vòng · dòng/ngân sách) | Làm tốt | Lỗi (AG) | Hướng cải thiện |
|---|---|---|---|---|---|
| 21/10 | R-001 / C-011-ui-layout-tests (ChinaAndroidToolbox) | 4 lượt · ~95 phút (2 lượt treo ~30 phút) · 3 vòng · 1037/600 dòng | Bộ kiểm tra soi mọi view + hộp thoại; sửa bố cục thật 10 file; test chạy ~20 giây | [AG] (test) test mở form thật trên luồng MTA (lỗi DragDrop) + chạy test nền ⇒ treo | Dùng helper UiThread; khi vượt ngân sách thì ghi lý do trong báo cáo |

## Đề xuất cải tiến cho nhân viên này
<!-- Claude cập nhật sau mỗi kỳ: đổi model, chia task kiểu khác, thêm lưu ý riêng, đổi vùng code phụ trách... -->