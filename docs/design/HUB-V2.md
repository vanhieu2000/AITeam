# Thiết kế AITeam Hub v2 — giao diện phẳng gọn + hạn mức & đổi tài khoản Antigravity

Ngày: 21/10/2026 · Người thiết kế: Claude · Chủ dự án đã duyệt mockup: [`HUB-V2-mockup.html`](HUB-V2-mockup.html) (mở bằng trình duyệt, bấm menu trái / thẻ task / dòng lịch sử chạy).

## 0. Vì sao
- Chủ dự án: "giao diện sơ sài, chỗ ngắn chỗ dài, scroll nhiều; hạn chế text mô tả; gọn, đủ chức năng, phẳng".
- Đo ở 1280×800: Task 8,2 màn hình; Cài đặt 2,6; Dự án 2,4; Agent 2,0; Tổng quan 1,8.
- Agent liên tục bị chặn "AI credits balance is too low" sau ~30 phút; phải tự `/logout` đổi tài khoản, không biết trước khi nào hết.

## 1. Nguyên tắc giao diện (mọi trang)
1. **Không cuộn trang ở 1280×800** (vùng nội dung 1072×748). Bảng / danh sách dài: phân trang hoặc tự cuộn trong khung của nó.
2. **Phẳng:** không đổ bóng, viền 1px `--line`, bo 6px, một màu nhấn `--accent`. Màu chỉ cho trạng thái: vàng = đang làm, xanh dương = chờ review, cam = cần sửa, xanh lá = đã merge / ổn, đỏ = lỗi.
3. **Ít chữ:** không câu mô tả dưới tiêu đề trang; giải thích chuyển vào biểu tượng ⓘ (`title`). Không emoji trong tiêu đề / nút.
4. Thanh trên cùng 52px: tiêu đề hoặc breadcrumb + nút hành động của trang. Thanh trái 208px.
5. Token màu, khoảng cách, thành phần (`.btn`, `.panel`, `.pill`, `.tabs`, `.seg`, `table`, `.drawer`, `.kv`, `.info`) lấy **nguyên** từ mockup, đặt trong `hub/wwwroot/css/hub.css` (thay bộ cũ). Giữ chế độ tối (`data-theme=dark`).
6. Tiếng Việt có dấu; HTMX giữ nguyên cho tự làm mới / gửi form.

## 2. Từng trang (theo mockup)
| Trang | Bố cục |
|---|---|
| Tổng quan | 4 ô chỉ số; trái: bảng dự án (đang làm / review / cần sửa / đã merge / hoạt động gần nhất) + **Hoạt động gần đây** (6 sự kiện mới nhất từ bảng việc + log chạy); phải: Agent đang chạy, Tình trạng máy dạng chip ("10/10 ổn", chip lỗi màu đỏ, chi tiết ở `title`). |
| Dự án | Thanh trên: tên + công nghệ + GitHub / Mở thư mục / + Task. Tab: Bảng việc · Kế hoạch · Lịch sử chạy. Bảng việc: 3 cột (Đang làm, Chờ review, Cần sửa) thẻ gọn + cột "Đã merge" dạng danh sách 1 dòng, 7 mục mới nhất + "Xem tất cả". |
| Task | Tiêu đề + pill trạng thái + agent; dòng meta (nhánh, thiết kế, ưu tiên, cổng kiểm tra). Tab: Tiêu chí (checklist) · Mô tả · Báo cáo agent · Review (từng vòng thu gọn được, nhận xét gắn nhãn BLOCKER / REQUIRED / OPTIONAL) · Lần chạy. Cột phải: bảng lần chạy; bấm dòng ⇒ **ngăn kéo phải** (lỗi, phản hồi cuối, token, thời lượng, nút Giao lại / Mở log). |
| Agent | Bảng đang chạy (nút Dừng). Bảng lịch sử: lọc Tất cả / Lỗi / Thành công + chọn dự án, **8 dòng / trang**; bấm dòng ⇒ ngăn kéo. Kết quả "Hết credits" nhận diện theo cùng quy tắc `Test-QuotaExhausted` của team.ps1. |
| Hộp thư | Trái: form (dự án + nội dung + Gửi). Phải: tab Chưa xử lý / Đã nhận. |
| Tài khoản | Xem mục 3. Cột phải: GitHub, Claude (gọn). |
| Bí mật | Trái: Thêm bí mật, Xuất .env.local. Phải: danh sách tên (không bao giờ hiện giá trị). |
| Cài đặt | Menu con: Team · từng dự án. Form 2 cột, ⓘ thay mô tả; Hoàn tác / Lưu. |
| Thanh trái | Tổng quan · Dự án (chấm trạng thái + số task đang làm) · Team (Agent + số đang chạy, Hộp thư) · Hệ thống (Tài khoản, Bí mật, Cài đặt) · **ô tài khoản Antigravity đang dùng** ("Pro 1 · 0 credits", chấm đỏ khi ≤ ngưỡng) · phiên bản + nút Tối/Sáng. |

## 3. Hạn mức & đổi tài khoản Antigravity — KHÔNG làm trong Hub
- Phần đọc phiên đăng nhập Antigravity, lưu token và ghi đè để đổi tài khoản đã bị dừng (21/10/2026): không xây trong AITeam.
- Dùng **AntigravityManager** (app chủ dự án đang dùng) để xem hạn mức và đổi tài khoản.
- Hub chỉ hiện trạng thái từ log agent: lần cuối "hết credits" (task, giờ) theo `Test-QuotaExhausted`, và nút mở hướng dẫn đổi tài khoản như hiện nay.
- Ô tài khoản ở thanh trái (mockup) đổi thành: "Antigravity · hết credits lúc 15:49" khi lần chạy gần nhất bị chặn, ngược lại "Antigravity · ổn".

## 4. Rủi ro
- API `v1internal` là nội bộ, không công khai: có thể đổi bất cứ lúc nào ⇒ mọi lỗi phân tích trả trạng thái "Không đọc được hạn mức", không làm hỏng trang.

## 5. Task
| Task | Agent | Nội dung | Phụ thuộc |
|---|---|---|---|
| H-006 | A | Khung v2: CSS token/thành phần, thanh trái (đếm + ô trạng thái Antigravity), thanh trên; trang Tổng quan | — |
| H-007 | A/B | Dự án + Task (tab, ngăn kéo lần chạy) | sau H-006 |
| H-008 | A/B | Agent, Hộp thư, Bí mật, Cài đặt | sau H-006 |
