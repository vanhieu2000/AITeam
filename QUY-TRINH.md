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
   - khung code + **test nghiệm thu** commit trước lên nhánh;
   - **Bố cục giao diện** cụ thể nếu có UI (chạy `tools/UiSnapshot` trước, liệt kê lỗi hiện có);
   - việc đơn giản ghi `Model: gemini-3.8-flash-medium`.
   - Việc ≤ ~100 dòng: Claude làm thẳng, không giao.
3. `team.ps1 assign -Project X -Task T-xxx` (nền). Cổng kiểm tra tự chạy: commit, BOM, build 0 warning, test (chặn treo 3 phút), phạm vi; vượt ngân sách chỉ cảnh báo.
4. `team.ps1 review -Project X -Task T-xxx` → đọc gói review. Chụp ảnh bằng `tools/UiSnapshot` nếu có UI.
   - Lỗi ≤ ~50 dòng: **Claude tự sửa**.
   - Còn sót nhưng phần chính đạt: merge, tách **task nhỏ mới**.
   - Chỉ trả agent khi phải làm lại nhiều (`assign -Resume -Feedback`).
   - Lỗi ghi `- [AG|TK|CK] (loại) …`.
5. Ghi "### Vòng N — ĐẠT" + "## Kết quả" (cho chủ dự án) → `team.ps1 merge -Project X -Task T-xxx`: merge, bảng ✅, clone sạch, dọn worktree, làm mới `ban-chay-thu\`.
6. Dừng task: `team.ps1 stop -Task T-xxx` (dừng cả lệnh assign, không bị tự giao lại).
7. Xong yêu cầu: `team.ps1 request-report -Request R-xxx` + `staff-report` → đánh giá trong phiếu → 1 dòng `docs/HIEU-SUAT-LOG.md` → tối đa 2 cải tiến, sửa ngay vào công cụ / mẫu → tăng phiên bản quy trình.
8. Báo chủ dự án **ngắn, gạch đầu dòng**: sửa gì · làm được gì · kiểm chứng · cần cải tiến · cách test (`CHAY-THU.cmd`). Cuối phiên cập nhật `BAN-GIAO.md`.

## Tiết kiệm token / thời gian — đã cài vào công cụ
- Agent: giới hạn thời gian theo task; chỉ đọc file liệt kê; không ảnh / không chạy app; báo cáo ≤ 15 dòng; commit sớm; test có `--blame-hang-timeout`.
- Claude: đọc trang này thay vì sổ tay 167 dòng; `review` / `merge` thay chuỗi lệnh tay; tự sửa nhỏ thay vòng agent 20–30 phút; số đo tự ghi (`metrics/runs`).

## Mục tiêu đo ở R-002 (so với R-001)
| Chỉ số | R-001 | Mục tiêu R-002 |
|---|---|---|
| Phút agent / task | 124 (gồm treo) | ≤ 30 |
| Token / task | ~2,4 triệu | ≤ 800 nghìn |
| Đạt vòng 1 | 1/3 | ≥ 2/3 |
| Thời gian treo | ~120 phút | 0 |
