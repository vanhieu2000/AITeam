# Minh — nhân viên mã A

- **Tên:** Minh
- **Mã:** A (nhánh `agent-a/…`, lời giao việc / commit vẫn ghi "Agent A")
- **Chuyên môn:** Nền tảng: kiến trúc, build, đóng gói, CSDL, hạ tầng
- **Chạy bằng:** Antigravity CLI (agy), tài khoản đang đăng nhập.

Tên chỉ dùng trong log và đánh giá. Bảng điểm: `.\team.ps1 staff-report` (chỉ tính task có dòng **Yêu cầu:** R-xxx).

## Lưu ý riêng
<!-- Mỗi dòng "- ..." được tự đưa vào lời giao việc lần sau. Chỉ ghi lỗi AG đã lặp ≥ 2 lần hoặc nghiêm trọng (bảo mật). Bỏ dòng khi 3 nhiệm vụ liền không tái phạm. -->
- Giữ BOM UTF-8 của MỌI file .cs / .cshtml đang có BOM, kể cả file khung Claude vừa tạo: sửa bằng công cụ ghi lại đúng mã hóa, không ghi đè cả file bằng UTF-8 không BOM. (Mất BOM ở H3-01 và H3-05.)

## Nhận xét theo nhiệm vụ
| Ngày | Yêu cầu / Task | Kết quả (lượt · phút · vòng · dòng/ngân sách) | Làm tốt | Lỗi (AG) | Hướng cải thiện |
|---|---|---|---|---|---|
| 21/10 | R-001 / T-005-ui-layout-tests (HDVTool) | 4 lượt · ~95 phút (2 lượt treo ~30 phút) · 3 vòng · 803/800 dòng | Bộ kiểm tra bố cục đủ 3 tỉ lệ DPI; sửa bố cục thật (ô tìm kiếm, thẻ Trang chủ, Người dùng); tắt hộp chào mừng bằng cờ internal, không đổi app thật | [AG] (test) test mở form/hộp thoại thật trên luồng MTA + chạy test nền ⇒ treo · [AG] (giao diện) sót thẻ "Hệ thống" bị cắt ở Giới thiệu | Dùng helper UiThread cho mọi test giao diện; xem kỹ ảnh chụp trước khi nộp |
| 23/10 | R-002 / V4-03 Codex review (AITeam) | 1 lượt · 9 phút · 1 vòng · 321/250 dòng | Qua cổng kiểm tra lượt đầu; đủ luật fail-safe | — (lỗi BOM là do test nghiệm thu của Claude thiếu) | Giữ phong độ; tên file tạm nên duy nhất |

## Đề xuất cải tiến cho nhân viên này
<!-- Claude cập nhật sau mỗi kỳ: đổi model, chia task kiểu khác, thêm lưu ý riêng, đổi vùng code phụ trách... -->
| 24/10 | R-003 / H3-01 | 1 · 8′ · 1 · 285/320 | Đúng đặc tả, chịu dữ liệu hỏng, gọi git an toàn | BOM | Giữ BOM file khung |
| 24/10 | R-003 / H3-04 | 1 · 8′ · 1 · 330/320 | Đúng đặc tả, giữ BOM | — | — |
| 24/10 | R-003 / H3-05 | 1 · 11′ · 1 · 348/380 | Đúng đặc tả, bảo mật đầu vào tốt | BOM (lần 2) | Lưu ý riêng đã thêm |
| 24/10 | R-002 / V4-07 | 1 · 7′ · 1 · 176/230 | Regex chống treo, giờ qua tham số, giữ BOM | — | — |
| 24/10 | R-004 / V4-09 | 1 · 8′ · 1 · 155/170 | Đúng đặc tả; Codex review tự động PASS | — | — |
| 25/10 | R-005 / V5-01 | 2 · 10′ · 1 | 4 công cụ MCP + phân quyền; sửa nhanh theo Codex review | — | Tốt |
| 25/10 | R-005 / V5-07a | 1 · 7′ · 1 · 242/260 | Đúng đặc tả, luôn dọn thư mục tạm, hủy thì ném lại | — | Tốt |
| 25/10 | R-005 / V5-06 | 1 · 9′ · 1 · 381/420 | Đủ 11 luật trạng thái; không đọc nội dung tệp đăng nhập; tự phát hiện file task thiếu trên nhánh | — | Tốt |
