# R-021 · VietMusic: giao dien 20 man (Claude) + chuc nang da nguon (Antigravity)

- **Nhận lúc:** 22:13 26/11/2026 · máy HIEUDV
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > "Làm xong tất cả giao diện UI và các màn hình điều hướng cài đặt trước . chức năng giao cho gg anti làm . gg anti có model mưới rồi đấy hãy kiểm tra và cập nhật việc giao cho model mới nhất của gg anti làm" — sau đó: "Duyệt cả hai" (mockup v1 + đăng ký dự án vào AITeam).

## 1. Phân tích (Claude)
- Mục tiêu hiểu được: VietMusic (Android) theo spec R-VM-03; giao diện 20 màn làm trước (Claude, U-01), chức năng do Antigravity model mới nhất làm (BE-01..BE-11).
- Dự án / phần bị ảnh hưởng: VietMusic (`D:\HIEUDVpp_music`, repo private vanhieu2000/VietMusic); AITeam `team.json` (model), `projects.json`.
- Quyết định đã tự chọn (không hỏi): model mặc định `gemini-4-hydrogen-high` (XHigh cho task khó); BE-01 chạy song song với U-01 vì khác vùng code; giao `-NoVerify` vì cổng KT chưa hỗ trợ Test vùng Gradle → Claude tự chạy test khi review.
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/KE-HOACH-DU-AN.md` (R-VM-03), `docs/design/mockup-v1.html` (đã duyệt 26/11).

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| U-01 | VietMusic | Claude | ~2500 dòng | — | Chủ dự án chỉ định giao diện làm trước; v6 dự phòng Claude |
| BE-01 | VietMusic | A | ~900 dòng | gemini-4-hydrogen-high | Backend + test, khác vùng U-01 nên chạy song song |

Mỗi file task phải có dòng `- **Yêu cầu:** R-021` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 21:55 | Nhận yêu cầu | Kiểm tra `team.ps1 models`: có Gemini 4 Hydrogen |
| 22:00 | Đổi model | team.json → gemini-4-hydrogen-high (commit 0484ffe) |
| 22:05 | Mockup | docs/design/mockup-v1.html, chủ dự án duyệt |
| 22:12 | add-project | repo private vanhieu2000/VietMusic |
| 22:20 | Viết task BE-01 | |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-021`, rồi điền:

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

- 27/11 01:45 — Hoàn tất: U-01 (Claude) + BE-01..BE-11, BE-04B, BE-09B, BE-06 (Antigravity, tất cả đạt vòng 1). Clone sạch: 271 test xanh (có gọi Audius thật). Còn: test máy thật, BE-07/YouTube thật chờ khóa, BE-12.
