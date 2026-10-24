# R-004 · Codex CLI làm developer dự phòng + trang Provider + rào an toàn (P1)

- **Nhận lúc:** 09:18 24/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > Làm đi user thứ 2 tạm thời chưa tạo vội hãy tải codex cli và làm tiếp cho tôi

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/V4-MULTI-AI.md` mục B2 (Developer: agy → codex), C (P1)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| V4-09 lệnh aiteam run (developer bất kỳ provider) | AITeam | Minh (A) | ~170 | mặc định | lõi C#, nghiệm thu |
| H3-06 trang Provider trên Hub | AITeam | Lan (B) | ~260 | mặc định | song song V4-09, vùng hub/ |
| assign -Dev codex + tự chuyển Codex khi agy hết hạn mức | AITeam | Claude | ~80 | | glue PowerShell, sau V4-09 |
| Hook chặn push main trong worktree + cảnh báo trùng phạm vi khi giao song song | AITeam | Claude | ~60 | | nhỏ |

Mỗi file task phải có dòng `- **Yêu cầu:** R-004` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 09:18 | Nhận yêu cầu | |
| 09:18 | Kiểm tra: Codex CLI 0.164.0 đã là bản mới nhất. Thử thật Codex workspace-write trong worktree (58s): sửa file + dotnet build + test ĐƯỢC; git add / commit BỊ CHẶN (index.lock ngoài sandbox) | ⇒ team.ps1 commit thay |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-004`, rồi điền:

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
