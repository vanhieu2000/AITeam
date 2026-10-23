# AITeam Hub v3 — bảng điều hành team toàn diện

Người viết: Claude. Ngày 24/10/2026. Yêu cầu chủ dự án (R-003): "nâng cấp toàn diện Hub team".
Mockup để duyệt: `docs/design/hub-v3-mockup.html` (dữ liệu mẫu lấy từ task thật đến 24/10).
Giữ nguyên: ASP.NET Core .NET 10, Razor Pages + HTMX, giao diện phẳng hiện tại, chỉ nghe 127.0.0.1, antiforgery.

## 1. Mục tiêu
Chủ dự án mở Hub là biết ngay, không phải hỏi Claude:
1. Đang làm gì, ai làm, mất bao lâu, khó cỡ nào — **danh sách nhiệm vụ + biểu đồ thời gian**.
2. Team làm tốt / dở ở đâu — **bảng đánh giá team** (Minh, Lan, Claude, Codex, cổng kiểm tra) + **điểm 7 giai đoạn quy trình** của từng task.
3. **Vấn đề của team**: nguyên nhân, giải pháp, đề xuất cải tiến, kết quả sau cải tiến (chu kỳ 3 task).
4. **Việc cần anh duyệt** — duyệt / từ chối ngay trên web.
5. **Giao việc mới** dạng hội thoại.

## 2. Trang
| Trang | Nội dung chính | Nguồn dữ liệu |
|---|---|---|
| **Tổng quan** | 5 chỉ số (đang chạy · chờ anh duyệt · đạt vòng 1 · phút TB / task · tình trạng provider); **biểu đồ thời gian** (mỗi task 1 thanh: giao → code → kiểm tra → review → merge); bảng nhiệm vụ (mã, tên, dự án, người, **độ khó S/M/L**, trạng thái, thời gian, vòng, điểm quy trình TB) | file task (`Độ khó`, `Yêu cầu`, Review, Đánh giá quy trình), `metrics/runs/*.json` (giờ + thời lượng từng lượt), git log (giờ merge), `aiteam providers` |
| **Nhiệm vụ** (chi tiết) | Tab: Tổng quan (sửa gì / tiến độ / kết quả) · **Dòng thời gian** · **Đánh giá quy trình** (7 giai đoạn) · Review (máy + AI + Claude, kết luận có cấu trúc) · Chi tiết kỹ thuật | như trên + `logs/*-review-*.json` + `JobStore` |
| **Đội ngũ** | Thẻ mỗi thành viên: số task, phút / lượt, token, đạt cổng KT lần đầu, vòng TB, đạt vòng 1, lỗi theo loại; **bản đồ nhiệt** task × 7 giai đoạn | `staff/`, `metrics/runs`, file task |
| **Quy trình & cải tiến** | Điểm TB từng giai đoạn theo chu kỳ (biểu đồ cột); danh sách **vấn đề** (giai đoạn, nguyên nhân TK/AG/CK/NV/HT, số lần lặp, giải pháp, trạng thái); danh sách **chu kỳ cải tiến** IMP-xxx (giải pháp, chỉ số trước / sau, có tác dụng không) | file task (Review + Đánh giá quy trình), `improvements/IMP-*.md` |
| **Cần duyệt** | Thẻ việc chờ duyệt: merge (kèm kết luận review + kết quả kiểm tra + tóm tắt diff), quyết định (ví dụ tạo user Windows), kế hoạch cải tiến quy trình. Nút **Duyệt / Từ chối** + ghi chú | `approvals/*.json` |
| **Giao việc** | Hội thoại: anh viết yêu cầu (chọn dự án, ưu tiên) → thành phiếu R-xxx; mỗi phiếu là 1 luồng, hiện phân tích / task con / trạng thái / kết quả | `inbox.md` (đã có), `requests/R-*.md` |

Giữ các trang đã có: Dự án, Agent, Tài khoản, Bí mật, Cài đặt (gộp vào menu "Hệ thống").

## 3. Dữ liệu mới (Markdown / JSON trên git — vẫn là nguồn sự thật dùng chung 2 máy)
- File task: thêm `- **Độ khó:** S|M|L`; mục `## Đánh giá quy trình` (bảng 7 dòng cố định: giai đoạn, ai, điểm 1–5, thời gian, vấn đề, nguyên nhân, đề xuất).
- `improvements/IMP-xxx-*.md` (mẫu `templates/improvement.md`), trạng thái trong dòng `- **Trạng thái:**`.
- `approvals/A-xxx.json`:
  ```json
  { "id": "A-001", "kind": "merge|decision|improvement", "title": "...", "detail": "markdown",
    "project": "AITeam", "task": "V4-04", "diffHash": "…", "links": ["tasks/V4-04-…md"],
    "requestedBy": "Claude", "requestedAt": "…", "status": "pending|approved|rejected",
    "decidedAt": null, "note": null }
  ```
  - Hub chỉ GHI `status` / `decidedAt` / `note`. Hub **không tự chạy** merge hay lệnh hệ thống.
  - `team.ps1 merge` đọc mục duyệt: `kind=merge` đã `approved` và `diffHash` khớp diff hiện tại thì mới merge (diff đổi sau khi duyệt ⇒ phải duyệt lại).
  - Ghi file: file tạm rồi đổi tên; kiểm tra id (`A-\d{3}`) chống ghi đè đường dẫn.

## 4. Giới hạn nói rõ (không hứa quá)
- **Giao việc qua chat không có AI trả lời tức thì.** Hub ghi yêu cầu vào `inbox.md` + tạo phiếu; Claude xử lý khi có phiên làm việc (đầu phiên đọc inbox). Câu trả lời hiện trong luồng khi Claude cập nhật phiếu. Muốn tự động hoàn toàn cần Claude CLI chạy ngầm — dùng chung hạn mức với phiên quản lý, chưa làm.
- **Duyệt trên web** chỉ ghi quyết định; hành động thật (merge, tạo user Windows) vẫn do `team.ps1` / chủ dự án thực hiện.
- Biểu đồ thời gian chỉ chính xác từ khi có số đo (21/10); task cũ chỉ có giờ merge.

## 5. Chia task (sau khi chủ dự án duyệt mockup)
| Task | Ai | Nội dung | Độ khó |
|---|---|---|---|
| H3-01 | Minh (A) | Dịch vụ đọc dữ liệu: parse `Đánh giá quy trình`, `Độ khó`, IMP, dòng thời gian (metrics + git), tổng hợp đội ngũ — lớp thuần có test | M |
| H3-02 | Lan (B) | Trang Tổng quan v3: chỉ số + biểu đồ thời gian (SVG, không thư viện) + bảng nhiệm vụ | M |
| H3-03 | Minh (A) | `approvals/`: dịch vụ đọc / ghi an toàn + trang Cần duyệt (HTMX, antiforgery) + `team.ps1 merge` kiểm tra duyệt | M |
| H3-04 | Lan (B) | Trang Đội ngũ + Quy trình & cải tiến (bản đồ nhiệt, biểu đồ cột) | M |
| H3-05 | Lan (B) | Trang Giao việc (hội thoại theo phiếu R-xxx) + tab Dòng thời gian / Đánh giá quy trình trong chi tiết task | M |
Mỗi task có khung code + test nghiệm thu + bố cục cụ thể (quy trình v3). H3-01 trước (dữ liệu), sau đó 02/03 song song, rồi 04/05.
