# R-006 · Quy trình tối ưu token: Claude ý tưởng → research/kế hoạch → Claude duyệt → Codex chia việc → Antigravity code → Codex QC → Claude kiểm cuối

- **Nhận lúc:** 10:15 25/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > tôi cần mô hình tối ưu : bạn nhận yêu cầu lên ý tưởng => gửi research check và lên kế hoạch => bạn duyệt oke thì gửi cho codex nhận và giao việc lại cho gg anti làm => codex tự qc oke => gửi cho bạn check 1 lần nữa thì mới hoàn thành . tôi muốn tối ưu token nhất mỗi ai làm 1 việc nhỏ bên trong

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

Mỗi file task phải có dòng `- **Yêu cầu:** R-006` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 10:15 | Nhận yêu cầu | |
| 10:20 | Chủ dự án chọn: research = ChatGPT qua Codex; thử 1 yêu cầu nhỏ trước | |
| 10:45 | Claude: aiteam run --read-only, team.ps1 plan / prep, mẫu codex-plan / codex-prep, QUY-TRINH v4 (thử); test đạt; merge | |
| 10:50 | Bước 1: Claude viết plans/R-006/brief.md (giới hạn vòng review) | |
| 10:24 | Bước 2: Codex lập kế hoạch 92s (183k token vào) ⇒ 2 task; Claude duyệt | |
| 10:32 | Bước 4: Codex chuẩn bị V6-01 343s (~967k token vào) ⇒ tự giao Minh | |
| 10:30 | Hộp lỗi dotnet.exe trên máy chủ dự án: test của Codex gọi CLI khung (NotImplementedException) ⇒ luật CLI bắt mọi ngoại lệ | |
| 10:52 | Bước 5: Minh 2 lượt; cổng KT trượt (test MCP chập chờn) ⇒ Claude chạy lại ĐẠT | |
| 11:00 | Bước 6: Codex QC 47s ⇒ CẦN SỬA 3 lỗi thật (Minh đổi TEMP toàn tiến trình…) ⇒ giao lại | |
| 11:11 | Antigravity HẾT HẠN MỨC ⇒ tự chuyển Codex sửa 111s | |
| 11:25 | Claude sửa test nghiệm thu (thư mục tên DOS con); Codex QC vòng 2 còn 1 điểm hẹp ⇒ Claude xếp Optional; nghiệm thu ĐẠT; merge | |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

- 2026-10-26 07:23:36 +07:00 | V6-02 | Prep | lượt 1 | ĐẠT <!-- workflow:V6-02:Prep:1 -->
- 2026-10-26 07:24:03 +07:00 | V6-02 | Assign | lượt 1 | LỖI <!-- workflow:V6-02:Assign:1 -->
- 2026-10-26 07:47:54 +07:00 | V6-02 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:1 -->
- 2026-10-26 11:01:43 +07:00 | V6-02 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:2 -->
- 2026-10-26 11:20:06 +07:00 | V6-02 | Verify | lượt 3 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:3 -->
- 2026-10-26 11:37:10 +07:00 | V6-02 | Verify | lượt 4 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:4 -->
- 2026-10-26 12:00:56 +07:00 | V6-02 | Verify | lượt 5 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:5 -->
- 2026-10-26 12:19:18 +07:00 | V6-02 | Verify | lượt 6 | KHÔNG ĐẠT <!-- workflow:V6-02:Verify:6 -->
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-006`, rồi điền:

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
