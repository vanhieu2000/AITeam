# R-012 — Giao Codex đợt 2 (Claude, 03/11/2026)

Đọc `docs/MO-TA-HE-THONG.md`, `plans/R-012/YEU-CAU-CODEX.md` và báo cáo đợt 1 `plans/R-012/BAO-CAO-CODEX.md`.

## Đã nghiệm thu và merge vào main (KHÔNG làm lại)

H8-14, V9-04, H7-02, O-02, V9-10, V9-13 (gồm V9-03), V9-06, V9-14, H8-03, H8-04, H8-06, V9-01, V9-02, V9-05, V9-07, H7-05, V6-02.
Kiểm chứng: build `-warnaserror` 0 cảnh báo, toàn bộ test Hub + Core xanh trên Windows (PowerShell 5.1).

## Quyết định của Claude về 7 xung đột test (đã cập nhật test nghiệm thu trên nhánh của từng mã)

Trước khi làm: `git fetch` rồi làm tiếp TRÊN NHÁNH CŨ của mã đó, và gộp `origin/main` mới nhất vào nhánh. Test nghiệm thu mới là commit "(Claude)" trên nhánh. Đỏ là đúng: đó là phần Codex phải làm.

| Mã | Quyết định | Codex làm |
|---|---|---|
| H8-01 | Thành công ⇒ `RedirectToPage` kèm `r` và `q` (mã câu hỏi); lỗi ⇒ vẫn `Page()` | `OnPostAsk` redirect, `OnGet(r, q)` hiện lại câu hỏi vừa gửi theo `q`. Làm tương tự cho bản nhúng ở Văn phòng (`OnPostResearchAsk` ⇒ redirect về `/?r=…`). Phần khóa nhà cung cấp đang nghỉ + giờ hồi: dùng dữ liệu hạn mức của V9-11 sau khi V9-11 vào main. |
| H8-05 | Giữ 200 job gần nhất và bỏ job cũ khỏi bộ nhớ, không chỉ cắt khi hiển thị | `CommandService`: giới hạn 200 khi thêm job; đọc log PowerShell 5.1 đúng code page (UTF-8 hai đầu). |
| V9-11 | File trạng thái hỏng: đổi tên thành `providers.json.hong-<giờ>` (giữ nguyên byte), rồi chạy tiếp từ rỗng. Không ghi đè, không làm kẹt dây chuyền | `ProviderStateStore` theo test `Broken_state_file_is_backed_up_then_rebuilt` (chỉ sao lưu 1 lần), rồi phần hạn mức + giờ hồi của B2. |
| V9-12 | 9 mã chuẩn; mã lạ (kể cả `NV` cũ) ⇒ `khác`, giữ nguyên nội dung | Parser chung dùng cho metrics, staff-report, Hub. |
| H7-05 | Đã merge | Không còn việc. |
| V6-02 | Đã merge (code nhánh `agent-a` cũ + harness đã sửa) | Không còn việc. |
| V9-16 | **CẦN SỬA** (xem dưới) | |

### V9-16 — Required
1. Kiểm "test nghiệm thu phải đỏ" CHỈ ở lần giao đầu. Khi `-Resume` (sửa sau review), test có thể đã xanh một phần; nếu chặn ở đây thì dây chuyền kẹt. Lần `-Resume` chỉ chạy phần kiểm cấu trúc (`Inspect`).
2. Task không có logic (chỉ tài liệu / JSON / mockup, hoặc có dòng `- **Test nghiệm thu:** Không — <lý do>`): không bắt buộc file test `.cs`, không build. Thêm test cho cả 2 ca.
3. Giữ nguyên các chặn còn lại (thiếu mục, Phạm vi thiếu đường dẫn, đường dẫn thoát gốc).

## Việc còn lại theo thứ tự

1. V9-11 → V9-12 → H8-05 → H8-01 (H8-01 cần dữ liệu hạn mức của V9-11).
2. V9-16 sửa theo mục trên.
3. V9-15 (hàng đợi + khóa file): làm sau khi V9-11 vào main. C3 (`DeveloperSelector`) và B4 (`RunDiagnostics`) đã có trên main.
4. V9-14 phần 2: nối `DeveloperSelector` vào `assign` khi không có `-Dev`, ghi lý do chọn vào nhật ký lượt.
5. W1-04: chờ Claude merge W1-03 (Claude sẽ báo).
6. D1–D4 (H8-10..13): CHỜ chủ dự án duyệt mockup. Không làm UI sản phẩm trước khi có duyệt.

## Luật
- Mỗi mã một nhánh, push nhánh, KHÔNG merge main. Không sửa assert. Cần sửa phần dựng môi trường test thì ghi `- **Cho phép sửa harness:** \`đường/dẫn\`` trong task và nêu lý do (V9-07). Cổng kiểm tra vẫn chặn sửa assert.
- Chạy test trên Windows nếu có. Nếu chỉ có Linux thì ghi rõ "chưa chạy Windows", không ghi đạt.
- Cuối đợt: bảng Mã · nhánh · trạng thái · phút · token (chưa đo ⇒ "chưa đo").
