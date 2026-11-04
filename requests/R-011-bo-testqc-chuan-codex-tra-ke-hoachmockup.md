# R-011 · Bộ test/QC chuẩn + Codex trả kế hoạch/mockup trước khi làm + trang Việc theo dự án hiện bước đang chạy

- **Nhận lúc:** 21:11 25/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** ✅ Xong (đóng khi rà soát R-015, 04/11/2026: mọi task của phiếu đã merge)
- **Lời chủ dự án:**
  > (chép nguyên văn yêu cầu)

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/...`

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| | | A / B / Claude | ~N dòng | mặc định | |

Mỗi file task phải có dòng `- **Yêu cầu:** R-011` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
- 18:16 26/10/2026 (Codex quản lý phụ): W1-03 prep thành công lúc 16:55, code dừng QuotaExhausted lúc 17:00; không prep lại. Chờ H7-02 kết thúc để tránh vượt giới hạn 1 lượt Codex và sửa đồng thời team.ps1; W1-04 còn phụ thuộc W1-03. Chưa có cổng KT/QC mới.
- 16:46 26/10/2026: Codex quản lý phụ chuyển sang W1-03 theo kế hoạch đã duyệt, W1-01 đã merge. Chạy nền team.ps1 prep -Request R-011 -Task W1-03, PID 39704; log W1-03.deputy-20261026-164647.out.txt/.err.txt. Vòng sau kiểm tra kết quả, không giao trùng; W1-04 chờ W1-03.
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| | Nhận yêu cầu | |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

- 2026-10-25 21:30:55 +07:00 | W1-05 | Prep | lượt 1 | ĐẠT <!-- workflow:W1-05:Prep:1 -->
- 2026-10-25 21:32:25 +07:00 | W1-02 | Prep | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-02:Prep:1 -->
- 2026-10-25 22:05:18 +07:00 | W1-05 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:1 -->
- 2026-10-25 22:05:31 +07:00 | W1-05 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-05:Assign:1 -->
- 2026-10-25 22:06:52 +07:00 | W1-02 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-02:Verify:1 -->
- 2026-10-25 22:07:02 +07:00 | W1-02 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-02:Assign:1 -->
- 2026-10-25 22:22:01 +07:00 | W1-05 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:2 -->
- 2026-10-25 22:22:41 +07:00 | W1-02 | Verify | lượt 2 | ĐẠT <!-- workflow:W1-02:Verify:2 -->
- 2026-10-25 22:24:14 +07:00 | W1-02 | Qc | lượt 1 | CẦN SỬA <!-- workflow:W1-02:Qc:1 -->
- 2026-10-25 22:25:31 +07:00 | W1-05 | Qc | lượt 1 | CẦN SỬA <!-- workflow:W1-05:Qc:1 -->
- 2026-10-25 22:38:36 +07:00 | W1-02 | Verify | lượt 3 | KHÔNG ĐẠT <!-- workflow:W1-02:Verify:3 -->
- 2026-10-25 22:54:10 +07:00 | W1-05 | Verify | lượt 3 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:3 -->
- 2026-10-25 23:01:36 +07:00 | W1-02 | Verify | lượt 4 | ĐẠT <!-- workflow:W1-02:Verify:4 -->
- 2026-10-25 23:05:36 +07:00 | W1-05 | Verify | lượt 4 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:4 -->
- 2026-10-25 23:07:38 +07:00 | W1-02 | Verify | lượt 5 | KHÔNG ĐẠT <!-- workflow:W1-02:Verify:5 -->
- 2026-10-25 23:22:31 +07:00 | W1-02 | Verify | lượt 6 | ĐẠT <!-- workflow:W1-02:Verify:6 -->
- 2026-10-25 23:27:52 +07:00 | W1-02 | Merge | lượt 3 | ĐẠT <!-- workflow:W1-02:Merge:3 -->
- 2026-10-25 23:34:51 +07:00 | W1-05 | Verify | lượt 5 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:5 -->
- 2026-10-25 23:45:53 +07:00 | W1-05 | Verify | lượt 6 | ĐẠT <!-- workflow:W1-05:Verify:6 -->
- 2026-10-25 23:57:02 +07:00 | W1-05 | Verify | lượt 7 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:7 -->
- 2026-10-26 00:06:38 +07:00 | W1-05 | Verify | lượt 8 | ĐẠT <!-- workflow:W1-05:Verify:8 -->
- 2026-10-26 00:19:47 +07:00 | W1-05 | Verify | lượt 9 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:9 -->
- 2026-10-26 00:25:10 +07:00 | W1-05 | Verify | lượt 10 | KHÔNG ĐẠT <!-- workflow:W1-05:Verify:10 -->
- 2026-10-26 00:26:45 +07:00 | W1-05 | Merge | lượt 4 | ĐẠT <!-- workflow:W1-05:Merge:4 -->
- 2026-10-26 00:39:17 +07:00 | W1-06 | Prep | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-06:Prep:1 -->
- 2026-10-26 00:54:10 +07:00 | W1-06 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-06:Verify:1 -->
- 2026-10-26 00:54:18 +07:00 | W1-06 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-06:Assign:1 -->
- 2026-10-26 01:00:12 +07:00 | W1-06 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:W1-06:Verify:2 -->
- 2026-10-26 01:05:29 +07:00 | W1-06 | Verify | lượt 3 | ĐẠT <!-- workflow:W1-06:Verify:3 -->
- 2026-10-26 01:06:41 +07:00 | W1-06 | Qc | lượt 1 | CẦN SỬA <!-- workflow:W1-06:Qc:1 -->
- 2026-10-26 01:15:32 +07:00 | W1-06 | Verify | lượt 4 | KHÔNG ĐẠT <!-- workflow:W1-06:Verify:4 -->
- 2026-10-26 01:21:26 +07:00 | W1-06 | Verify | lượt 5 | KHÔNG ĐẠT <!-- workflow:W1-06:Verify:5 -->
- 2026-10-26 01:29:02 +07:00 | W1-06 | Verify | lượt 6 | ĐẠT <!-- workflow:W1-06:Verify:6 -->
- 2026-10-26 01:45:01 +07:00 | W1-06 | Verify | lượt 7 | KHÔNG ĐẠT <!-- workflow:W1-06:Verify:7 -->
- 2026-10-26 07:34:45 +07:00 | W1-07 | Prep | lượt 1 | ĐẠT <!-- workflow:W1-07:Prep:1 -->
- 2026-10-26 11:01:25 +07:00 | W1-07 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:1 -->
- 2026-10-26 11:01:33 +07:00 | W1-07 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-07:Assign:1 -->
- 2026-10-26 11:37:17 +07:00 | W1-07 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:2 -->
- 2026-10-26 11:51:19 +07:00 | W1-07 | Verify | lượt 3 | ĐẠT <!-- workflow:W1-07:Verify:3 -->
- 2026-10-26 11:52:12 +07:00 | W1-07 | Qc | lượt 1 | CẦN SỬA <!-- workflow:W1-07:Qc:1 -->
- 2026-10-26 12:10:00 +07:00 | W1-07 | Verify | lượt 4 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:4 -->
- 2026-10-26 12:23:53 +07:00 | W1-07 | Verify | lượt 5 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:5 -->
- 2026-10-26 12:34:32 +07:00 | W1-01 | Prep | lượt 1 | ĐẠT <!-- workflow:W1-01:Prep:1 -->
- 2026-10-26 12:37:36 +07:00 | W1-07 | Verify | lượt 6 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:6 -->
- 2026-10-26 12:50:11 +07:00 | W1-07 | Verify | lượt 7 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:7 -->
- 2026-10-26 13:02:13 +07:00 | W1-01 | Verify | lượt 1 | ĐẠT <!-- workflow:W1-01:Verify:1 -->
- 2026-10-26 13:03:33 +07:00 | W1-01 | Qc | lượt 1 | CẦN SỬA <!-- workflow:W1-01:Qc:1 -->
- 2026-10-26 13:03:42 +07:00 | W1-01 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-01:Assign:1 -->
- 2026-10-26 13:05:57 +07:00 | W1-07 | Verify | lượt 8 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:8 -->
- 2026-10-26 13:19:55 +07:00 | W1-07 | Verify | lượt 9 | KHÔNG ĐẠT <!-- workflow:W1-07:Verify:9 -->
- 2026-10-26 13:22:18 +07:00 | W1-01 | Verify | lượt 2 | ĐẠT <!-- workflow:W1-01:Verify:2 -->
- 2026-10-26 13:34:02 +07:00 | W1-07 | Merge | lượt 4 | ĐẠT <!-- workflow:W1-07:Merge:4 -->
- 2026-10-26 13:51:45 +07:00 | W1-01 | Verify | lượt 3 | ĐẠT <!-- workflow:W1-01:Verify:3 -->
- 2026-10-26 14:00:37 +07:00 | W1-01 | Merge | lượt 2 | ĐẠT <!-- workflow:W1-01:Merge:2 -->
- 2026-10-26 16:56:14 +07:00 | W1-03 | Prep | lượt 1 | ĐẠT <!-- workflow:W1-03:Prep:1 -->
- 2026-10-26 17:00:35 +07:00 | W1-03 | Assign | lượt 1 | LỖI <!-- workflow:W1-03:Assign:1 -->

- 18:52 26/10/2026: Codex quản lý phụ giao tiếp W1-03 bằng assign -Dev codex -Resume sau khi lượt H7-02 kết thúc; chạy nền PID 11172, log W1-03.deputy-20261026-185137.out.txt/.err.txt. Không prep lại; vòng sau kiểm tra cổng KT/QC, W1-04 tiếp tục chờ.

- 2026-10-26 19:44:16 +07:00 | W1-03 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:W1-03:Verify:1 -->
- 19:47 26/10/2026: W1-03 verify-1 KHÔNG ĐẠT lúc 19:44 (build 0 warning; Hub 721 pass; Core còn lỗi sau thử lại); lệnh nền PID 11172 tự giao sửa tiếp lúc 19:45. Không gọi thêm assign/prep/verify/QC; W1-04 chờ W1-03; vòng sau xem kết quả.

- 20:16 27/10/2026 (Codex quản lý phụ): W1-03 lượt 6 đã dừng QuotaExhausted lúc 19:53 26/10, lệnh nền đã kết thúc; cổng gần nhất verify-1 KHÔNG ĐẠT, chưa có KT/QC cho HEAD 84a1513. Quá 3 lượt, giữ tạm dừng; W1-04 tiếp tục chờ, không giao thêm.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-011`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | |
| Số lượt agent / số vòng review | |
| Dòng code / ngân sách | |
| Claude tự sửa: số lần / tổng dòng | |
| Lỗi theo nguyên nhân: TK / AG / CK | |

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật (công cụ, build, cổng kiểm tra, môi trường, model):
- Quy trình (thiết kế, chia task, giao việc, review, phối hợp 2 máy):
- Quản lý (Claude: lỗi TK, quyết định sai, chậm review):
- **Tốn thời gian nhất ở đâu:**
- **Cái gì làm tốt (giữ lại):**
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có / chưa
