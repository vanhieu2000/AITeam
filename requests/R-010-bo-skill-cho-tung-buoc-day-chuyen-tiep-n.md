# R-010 · Bộ skill cho từng bước dây chuyền (tiếp nhận → publish git) + skill code WinForms / web / app, tự cập nhật sau đánh giá

- **Nhận lúc:** 12:33 25/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > và cần xây dựng bộ skill (giao cho codex làm ) cho từng côn gvieejc từ tiếp nahanj ý tưởng đến khi keiemr cuối và pull git. chứ trọng vào code nữa cần nghiên cứu kĩ code winform và cần những gì , code web hay app thì cần gì , về winform và web thì tôi nhứo tô iddax tạo skill cho bạn rồi hãy chia sẻ . tôi nghĩ các phần đều có skill thì ai sẽ làm chuẩn theo task nhỏ để ít bị sai . và sau mỗi lần đánh giá cần tiến hay gì cũng cập nhật vào bộ skill đó thì sẽ heieu jquar hơn nheieuf

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

Mỗi file task phải có dòng `- **Yêu cầu:** R-010` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 12:45 | Nhận yêu cầu | |
| 12:50 | Bước 1: brief plans/R-010/brief.md (nguồn: ~/.claude/skills winforms-expert, create-datadriven-aspnetcore, dotnet-webapi, maui-*, scaffold-dotnet-test-project) | |

| 16:52 25/10/2026 | Quản lý phụ kiểm tra S1-04: lượt 1 hết 25 phút, helper chưa commit; chưa có cổng KT/QC. | Chưa giao lại: prep/assign tự commit/push task hoặc metrics lên main ngoài danh sách được phép của vòng này; giữ S1-06/07 chờ phụ thuộc. |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

- 2026-10-25 16:51:45 +07:00 | S1-04 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:S1-04:Verify:1 -->
- 2026-10-25 16:51:56 +07:00 | S1-04 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:S1-04:Assign:1 -->
- 2026-10-25 17:01:47 +07:00 | S1-04 | Verify | lượt 2 | ĐẠT <!-- workflow:S1-04:Verify:2 -->
- 2026-10-25 17:03:04 +07:00 | S1-04 | Qc | lượt 1 | CẦN SỬA <!-- workflow:S1-04:Qc:1 -->
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-010`, rồi điền:

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
