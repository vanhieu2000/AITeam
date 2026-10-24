# Quy trình v3 — chốt từ đầu, agent chỉ code, Claude review

Áp dụng từ 21/10/2026 (sau đánh giá R-001). Một trang này là đủ cho mỗi phiên. `CLAUDE.md` chỉ để tra cứu chi tiết.

## Vai trò (cố định, không đổi giữa chừng)
| Ai | Làm | KHÔNG làm |
|---|---|---|
| **Claude** | Phân tích → thiết kế → **chốt task đầy đủ một lần** (khung code, test nghiệm thu, bố cục, file cần đọc, ngân sách, thời gian) → giao → review → tự sửa lỗi nhỏ → merge → chụp ảnh / chạy app → báo cáo | Không giao task mơ hồ để agent tự nghĩ |
| **Agent (Minh A, Lan B)** | **Chỉ viết code** trong file được giao cho test nghiệm thu pass; commit sớm; báo cáo ≤ 15 dòng | Không tự thiết kế, không lục tài liệu, không chụp ảnh, không chạy app, không làm thêm ngoài task |

## Các bước (mỗi yêu cầu của chủ dự án)
1. `team.ps1 new-request -Title "…"` → phiếu `requests/R-xxx`, ghi giờ nhận.
2. **Chốt task một lần** (`templates/task.md`), đủ các mục:
   - **Tóm tắt** cho chủ dự án;
   - **Đọc** (chỉ những file này);
   - **Ngân sách** dòng code; **Thời gian tối đa** (mặc định 25–30 phút, quá thì agy tự dừng);
   - khung code + **test nghiệm thu** commit trước lên nhánh — **thứ tự: file task lên `main` trước, rồi mới tạo nhánh từ `origin/main`** (ngược lại sẽ xung đột file task khi merge; `merge` tự xử lý nhưng tránh được thì hơn);
   - **Bố cục giao diện** cụ thể nếu có UI (chạy `tools/UiSnapshot` trước, liệt kê lỗi hiện có);
   - việc đơn giản ghi `Model: gemini-3.8-flash-medium`.
   - Việc ≤ ~100 dòng: Claude làm thẳng, không giao.
3. `team.ps1 assign -Project X -Task T-xxx` (nền). Cổng kiểm tra tự chạy: commit, BOM, build 0 warning, test (chặn treo 3 phút), phạm vi; vượt ngân sách chỉ cảnh báo.
4. `team.ps1 review -Project X -Task T-xxx` → đọc gói review. **Claude sửa code (dù 1 dòng) ⇒ chạy build + TOÀN BỘ test trước khi commit; test và commit là 2 lệnh riêng — đọc kết quả test rồi mới commit** (IMP-001, IMP-005; Hub đang chạy khóa `hub\bin` ⇒ tạm dừng Hub, test, bật lại). Chụp ảnh bằng `tools/UiSnapshot` nếu có UI.
   - Lỗi ≤ ~50 dòng: **Claude tự sửa**.
   - Còn sót nhưng phần chính đạt: merge, tách **task nhỏ mới**.
   - Chỉ trả agent khi phải làm lại nhiều (`assign -Resume -Feedback`).
   - Lỗi ghi `- [AG|TK|CK] (loại) …`.
5. Ghi "### Vòng N — ĐẠT" + "## Kết quả" (cho chủ dự án) → (việc cần chủ dự án quyết: `team.ps1 approval-request -Kind merge|decision|improvement …` ⇒ chủ dự án bấm trên Hub trang Cần duyệt; `merge` tự chặn nếu chờ / từ chối / nhánh đổi sau khi duyệt) → `team.ps1 merge -Project X -Task T-xxx`: merge, bảng ✅, clone sạch, dọn worktree, làm mới `ban-chay-thu\`.
6. Dừng task: `team.ps1 stop -Task T-xxx` (dừng cả lệnh assign, không bị tự giao lại).
6b. **Sau MỖI task (ngay khi merge):** điền mục "## Đánh giá quy trình" trong file task — 7 giai đoạn (tiếp nhận & quản lý · ý tưởng · plan · giao việc · code · review/QC · đánh giá & bàn giao), điểm 1–5, thời gian, vấn đề, nguyên nhân (TK/AG/CK/NV/HT), đề xuất.
6c. **Cứ 3 task merge:** tạo `improvements/IMP-xxx-*.md` (mẫu `templates/improvement.md`): điểm TB từng giai đoạn so chu kỳ trước, vấn đề lặp lại, tối đa 3 giải pháp kèm chỉ số đo. Nhỏ ⇒ áp dụng ngay; lớn (quy trình / kiến trúc) ⇒ chờ chủ dự án duyệt. Chu kỳ sau điền mục "Kết quả" (có tác dụng không ⇒ giữ / bỏ).
7. Xong yêu cầu: `team.ps1 request-report -Request R-xxx` + `staff-report` → đánh giá trong phiếu → 1 dòng `docs/HIEU-SUAT-LOG.md` → tối đa 2 cải tiến, sửa ngay vào công cụ / mẫu → tăng phiên bản quy trình.
8. Báo chủ dự án **ngắn, gạch đầu dòng**: sửa gì · làm được gì · kiểm chứng · cần cải tiến · cách test (`CHAY-THU.cmd`). Cuối phiên cập nhật `BAN-GIAO.md`.

## Tiết kiệm token / thời gian — đã cài vào công cụ
- Agent: giới hạn thời gian theo task; chỉ đọc file liệt kê; không ảnh / không chạy app; báo cáo ≤ 15 dòng; commit sớm; test có `--blame-hang-timeout`.
- Claude sửa `team.ps1` ⇒ chạy `tools\test-verify.ps1` (test hồi quy cổng kiểm tra, IMP-001) trước khi commit. **Không để `team.ps1` sửa dở khi giao việc** — `assign` chạy đúng file trên đĩa (R-002: bảng việc ghi nhầm "AI review PASS").
- **Codex viết code** (R-004): `assign -Dev codex` (việc NHỎ — hạn mức Codex nhỏ); agy hết hạn mức ⇒ tự chuyển Codex (`-Dev agy` để tắt). Codex không ghi được .git ⇒ team.ps1 commit + push thay; task do Codex code ⇒ reviewer AI không có ⇒ Claude review. Mỗi lệnh assign tối đa 2 lượt agent.
- **Thêm tham số / nhánh mới cho `assign`** ⇒ chạy thử cả lệnh với 1 task nhỏ trước khi dùng thật; `tools\test-verify.ps1` có kiểm tra tĩnh biến cục bộ đè tham số (R-004: `$dev` đè `-Dev`).
- **Review AI tự động** (R-002): sau cổng KT, `assign` gọi `aiteam review --auto` cho task `Độ khó: L` hoặc có `- **Review AI:** có`; reviewer luôn khác agy; CẦN SỬA ⇒ tự giao lại 1 lần; BLOCKED ⇒ Claude review. Kết quả: `<workDir>\logs\<Task>.review-n.json`, Hub tab Báo cáo & review. Tắt: `-NoReview`.
- Claude: đọc trang này thay vì sổ tay 167 dòng; `review` / `merge` thay chuỗi lệnh tay; tự sửa nhỏ thay vòng agent 20–30 phút; số đo tự ghi (`metrics/runs`).

## Mục tiêu đo ở R-002 (so với R-001)
| Chỉ số | R-001 | Mục tiêu R-002 |
|---|---|---|
| Phút agent / task | 124 (gồm treo) | ≤ 30 |
| Token / task | ~2,4 triệu | ≤ 800 nghìn |
| Đạt vòng 1 | 1/3 | ≥ 2/3 |
| Thời gian treo | ~120 phút | 0 |
