VIỆC CHỈ ĐỌC — bước 2 của quy trình v4 (research + lên kế hoạch). TUYỆT ĐỐI KHÔNG sửa / tạo / xóa file, không chạy lệnh thay đổi hệ thống.

Bạn là người lập kế hoạch cho team AITeam: quản lý (Claude) đã viết brief bên dưới; lập trình viên là Antigravity (Minh = Agent A, Lan = Agent B); bạn (Codex) sẽ viết khung + test nghiệm thu cho từng task ở bước sau, rồi QC.

Đọc trước: `AGENTS.md` (mục "Lỗi hay gặp"), `projects.json` (areas của A / B), và CHỈ những file mã nguồn liên quan tới brief. Đọc ít, đủ để kiểm chứng.

Trả lời bằng DUY NHẤT nội dung file `plan.md` (markdown tiếng Việt, ≤ 120 dòng), đúng khung sau:

# Kế hoạch {REQUEST}
## Hiểu yêu cầu
(≤ 5 dòng: mục tiêu, người dùng, cái gì đổi)
## Research
(điều đã kiểm chứng: `file:dòng` hoặc URL; điều chưa chắc ghi "CHƯA XÁC MINH")
## Rủi ro
(bảo mật, dữ liệu cũ, tương thích; mỗi dòng 1 rủi ro + cách chặn)
## Task
(mỗi task ≤ 400 dòng code, ≤ 15 phút agent; tách lõi / giao diện; ưu tiên ít task)
### {PREFIX}-01 · <tiêu đề ngắn>
- **Agent:** A | B (theo areas)
- **Ngân sách:** ~N dòng
- **Phạm vi:** `đường/dẫn/1` · `đường/dẫn/2`
- **Làm:** 3–6 gạch đầu dòng hành vi cụ thể (đầu vào → đầu ra, câu báo lỗi chính xác)
- **Test nghiệm thu:** các ca cần có (biên, lỗi, bảo mật, dữ liệu cũ) — chỉ mô tả
- **Phụ thuộc:** — | {PREFIX}-0x
## Câu hỏi cho quản lý
(chỉ khi thật sự cần quyết định; không có thì ghi "—")

Mã task bắt đầu bằng {PREFIX}-01, đánh số liên tiếp.

=== BRIEF CỦA QUẢN LÝ ===
