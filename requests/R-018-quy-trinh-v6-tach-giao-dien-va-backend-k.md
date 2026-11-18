# R-018 · Quy trinh v6: tach giao dien va backend, ke hoach chung, chuan test

- **Nhận lúc:** 01:03 19/11/2026 · máy HIEUDV
- **Trạng thái:** ✅ Xong
- **Lời chủ dự án:**
  > cải tiến các tiếp nhận và giao việc đi . tôi thấy rất hay lỗi ở giao diện cứ sửa đi sửa lại. bây giờ bắt buộc tách ra . chỉ có bạn hoặc codex thực hiện giao diện theo mockup đề ra , còn google anti và opencode làm back end chức năng . và việc test cũng phải có bộ quy chuẩn test
  >
  > đầu tiên bạn hoặc codex sẽ làm phần UI ưu tiên codex nếu hết hạn thì bạn làm , còn plan và phân công thì bạn vẫn lên , còn back end do google anti làm và đảm bảo chức năng phải link được với front end chứ không phải bạn phải đi nối , chỉ là bỏ không cho google anti thiết kế và làm UI nữa . test thì cũng vậy ưu tiên cho codex làm . plan và test như nào lên kế hoạch hoàn bộ ngay từ đầu vào file chung của dự án , có yêu cầu mới hay sửa cập nhật vào để tiếp tục làm sau. tôi muốn viết plan ngay khi nhân bao gồm cả mockup chức năng để toàn bộ AI đều tuân theo mục mình phải làm là gì , phải test chức năng gì . cập nhật quy tắc này cho toàn bộ dự án của tôi

## 1. Phân tích (Claude)
- Mục tiêu hiểu được: tách giao diện và backend theo AI, có một file kế hoạch chung cho mỗi dự án, chuẩn hóa để giảm test lặp.
- Dự án / phần bị ảnh hưởng: AITeam, HDVTool, ChinaAndroidToolbox, MyControls.
- Quyết định đã tự chọn (không hỏi): OpenCode không vào phân công vì công cụ cấm bản miễn phí chạy trên repo private; Antigravity hết hạn mức thì task BE dừng chờ.
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/...`

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| Q6-01 | AITeam | Codex | ~450 dòng | mặc định | Sửa công cụ giao việc và cổng kiểm tra |

Mỗi file task phải có dòng `- **Yêu cầu:** R-018` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
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

- 2026-11-19 01:24:03 +05:30 | Q6-01 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:Q6-01:Verify:1 -->
| 01:24 19/11/2026 | Cờ tự động AITeam/Q6-01 lượt 1 | TO |
- 2026-11-19 01:24:18 +05:30 | Q6-01 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:Q6-01:Assign:1 -->
- 2026-11-19 01:32:55 +05:30 | Q6-01 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:Q6-01:Verify:2 -->
- 2026-11-19 01:45:41 +05:30 | Q6-01 | Verify | lượt 3 | ĐẠT <!-- workflow:Q6-01:Verify:3 -->
- 2026-11-19 02:02:26 +05:30 | Q6-01 | Merge | lượt 2 | ĐẠT <!-- workflow:Q6-01:Merge:2 -->
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-018`, rồi điền:

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
