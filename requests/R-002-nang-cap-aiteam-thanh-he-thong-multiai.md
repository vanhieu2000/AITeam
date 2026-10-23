# R-002 · Nang cap AITeam thanh he thong multi-AI

- **Nhận lúc:** 23:16 23/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > NHIỆM VỤ: NÂNG CẤP AITEAM THÀNH HỆ THỐNG MULTI-AI TỐI ƯU CHO CÁ NHÂN (bản đầy đủ 17 mục do chủ dự án dán vào chat 23/10: audit, kiến trúc Role ↔ Provider, Codex review độc lập, cross-provider review, adapter, job state, Hub, hiệu suất, memory, bảo mật, nghiên cứu Agent Office, roadmap P0/P1/P2, triển khai P0)

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/V4-MULTI-AI.md` (audit + kiến trúc + lộ trình)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| | | A / B / Claude | ~N dòng | mặc định | |

Mỗi file task phải có dòng `- **Yêu cầu:** R-002` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 09:00 | Nhận yêu cầu | |
| 09:20 | Audit xong: đọc mã AITeam + mã Agent Office (clone nông, chỉ đọc), kiểm tra CLI trên máy | Máy chưa có Codex CLI, chưa có Claude CLI; agy lưu đăng nhập ở Credential Manager; GitHub không bảo vệ nhánh được (gói miễn phí) |
| 09:35 | Viết `docs/design/V4-MULTI-AI.md` | Chờ chủ dự án chốt 3 quyết định kiến trúc |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-002`, rồi điền:

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
