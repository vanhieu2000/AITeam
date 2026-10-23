# Lan — nhân viên mã B

- **Tên:** Lan
- **Mã:** B (nhánh `agent-b/…`, lời giao việc / commit vẫn ghi "Agent B")
- **Chuyên môn:** Tính năng: module, màn hình, nghiệp vụ
- **Chạy bằng:** Antigravity CLI (agy), tài khoản đang đăng nhập.

Tên chỉ dùng trong log và đánh giá. Bảng điểm: `.\team.ps1 staff-report` (chỉ tính task có dòng **Yêu cầu:** R-xxx).

## Lưu ý riêng
<!-- Mỗi dòng "- ..." được tự đưa vào lời giao việc lần sau. Chỉ ghi lỗi AG đã lặp ≥ 2 lần hoặc nghiêm trọng (bảo mật). Bỏ dòng khi 3 nhiệm vụ liền không tái phạm. -->

## Nhận xét theo nhiệm vụ
| Ngày | Yêu cầu / Task | Kết quả (lượt · phút · vòng · dòng/ngân sách) | Làm tốt | Lỗi (AG) | Hướng cải thiện |
|---|---|---|---|---|---|
| 21/10 | R-001 / C-011-ui-layout-tests (ChinaAndroidToolbox) | 4 lượt · ~95 phút (2 lượt treo ~30 phút) · 3 vòng · 1037/600 dòng | Bộ kiểm tra soi mọi view + hộp thoại; sửa bố cục thật 10 file; test chạy ~20 giây | [AG] (test) test mở form thật trên luồng MTA (lỗi DragDrop) + chạy test nền ⇒ treo | Dùng helper UiThread; khi vượt ngân sách thì ghi lý do trong báo cáo |
| 23/10 | R-002 / V4-06 JobStore (AITeam) | 3 lượt (2 do lỗi công cụ) · ~13 phút · 1 vòng · 268/250 dòng | Ghi nguyên tử, khóa CreateNew, chặn tên nguy hiểm; giữ nguyên bảng trạng thái và ghi rõ lý do khi buộc phải gộp file | — | Tốt |

## Đề xuất cải tiến cho nhân viên này
<!-- Claude cập nhật sau mỗi kỳ: đổi model, chia task kiểu khác, thêm lưu ý riêng, đổi vùng code phụ trách... -->
| 24/10 | R-003 / H3-03 | 1 · 7′ · 1 · 255/280 | Đúng đặc tả + bố cục, giữ BOM, báo cáo đối chiếu từng mục | — | — |
| 24/10 | R-003 / H3-02 | 1 · 13′ · 1 · 370/330 | Đúng đặc tả, SVG chuẩn số thập phân bất biến | — | — |
