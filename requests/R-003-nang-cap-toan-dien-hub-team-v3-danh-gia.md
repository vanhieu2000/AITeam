# R-003 · Nâng cấp toàn diện Hub team (v3) + đánh giá quy trình mỗi task

- **Nhận lúc:** 00:11 24/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > mỗi 1 task xong cần đánh giá từ quy tình quản lí gia ovieejc đến , quy trình plan, y tưởng , quy trình giao việc , quy tình thực hiện code, quy tình review qc ,.. quy trình đánh giá ,.. sau mỗi lần cần hoàn thiện sau 3 task lại đưa ra kế haochj cải tiến góp ý thay đổi quy trình hoàn thiện quy trình ,.. ở web cần show được cả thông tin cái dó, tổng qua nxem được dan hsachs nhiện vụ beieur dồ thời gian việc độ khó như nào ,.. bảng đnahs giá team ,.. các tab xem chi tiết , khu vục chat giao nhiêm jvuj mới , giao diện báo cáo những vấn đè trong team , nghuyên nhân, giải pháp phươn gán đề xuất cải tiến , kết quả cải tiến ... , có những job gì cần tô iduyeejt cũng có thể duyệt trên đó luôn ,.. tôi muốn nagw cấp toàn diện hub team

## 1. Phân tích (Claude)
- Mục tiêu hiểu được: (1) mỗi task đánh giá 7 giai đoạn quy trình, cứ 3 task 1 chu kỳ cải tiến IMP; (2) Hub v3: tổng quan + biểu đồ thời gian + độ khó, đội ngũ, vấn đề / cải tiến, duyệt việc trên web, giao việc dạng chat.
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/HUB-V3.md` + mockup `docs/design/hub-v3-mockup.html` (chờ duyệt)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| | | A / B / Claude | ~N dòng | mặc định | |

Mỗi file task phải có dòng `- **Yêu cầu:** R-003` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 00:11 | Nhận yêu cầu | |
| 00:20 | Quy trình 6b/6c + mẫu task/improvement (commit 37d788c) | đánh giá V4-03, V4-06 |
| 00:45 | Xong thiết kế HUB-V3.md + mockup | chờ chủ dự án duyệt mockup |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-003`, rồi điền:

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
