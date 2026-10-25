# R-010 · Bộ skill cho từng bước dây chuyền (tiếp nhận → publish git) + skill code WinForms / web / app, tự cập nhật sau đánh giá

- **Nhận lúc:** 12:33 25/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** ✅ Xong 25/10
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
- 17:16 25/10/2026 | Quản lý phụ | Xác nhận S1-04 lượt code 3 đang sửa theo QC CẦN SỬA; không giao trùng, chờ cổng KT/QC bản mới. Chưa có đề xuất nghiệm thu.

- 2026-10-25 17:28:42 +07:00 | S1-04 | Verify | lượt 3 | ĐẠT <!-- workflow:S1-04:Verify:3 -->
- 2026-10-25 17:32:10 +07:00 | S1-04 | Merge | lượt 1 | ĐẠT <!-- workflow:S1-04:Merge:1 -->
- 2026-10-25 17:45:27 +07:00 | S1-06 | Prep | lượt 1 | ĐẠT <!-- workflow:S1-06:Prep:1 -->
- 18:16 25/10/2026: Codex quản lý phụ kiểm tra: S1-04 đã merge; S1-06 Antigravity hết hạn mức, Codex lượt 2 đang chạy (PID 42808), chưa có cổng KT/QC. Không giao trùng; vòng sau kiểm tra tiếp.
- 2026-10-25 18:22:05 +07:00 | S1-06 | Verify | lượt 1 | KHÔNG ĐẠT <!-- workflow:S1-06:Verify:1 -->
- 2026-10-25 18:22:12 +07:00 | S1-06 | Assign | lượt 1 | KHÔNG ĐẠT <!-- workflow:S1-06:Assign:1 -->
- 2026-10-25 18:34:51 +07:00 | S1-06 | Verify | lượt 2 | KHÔNG ĐẠT <!-- workflow:S1-06:Verify:2 -->
- 2026-10-25 18:59:57 +07:00 | S1-06 | Merge | lượt 1 | ĐẠT <!-- workflow:S1-06:Merge:1 -->
- 2026-10-25 19:14:29 +07:00 | S1-07 | Prep | lượt 1 | ĐẠT <!-- workflow:S1-07:Prep:1 -->
- 2026-10-25 19:40:20 +07:00 | S1-07 | Verify | lượt 1 | ĐẠT <!-- workflow:S1-07:Verify:1 -->
- 2026-10-25 19:41:10 +07:00 | S1-07 | Qc | lượt 1 | BLOCKED <!-- workflow:S1-07:Qc:1 -->
- 2026-10-25 19:41:19 +07:00 | S1-07 | Assign | lượt 1 | ĐẠT <!-- workflow:S1-07:Assign:1 -->
- 2026-10-25 19:52:38 +07:00 | S1-07 | Merge | lượt 1 | ĐẠT <!-- workflow:S1-07:Merge:1 -->
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-010`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | 12:33 → ~19:00 25/10 (~6,5 giờ, xen R-008/R-009) |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | 212,7 phút · 12,2M token (S1-05 78′, S1-04 65′) |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | Antigravity hết hạn mức từ ~17:40 ⇒ Codex code dự phòng S1-06/S1-07 |
| Số lượt agent / số vòng review | 17 lượt / 8 task đạt vòng 1 (sau Claude sửa) |
| Dòng code / ngân sách | S1-05 1744/220 và S1-04 783/280 vượt (agent thêm mã lách test); còn lại dưới ngân sách |
| Claude tự sửa: số lần / tổng dòng | 5 lần / ~80 dòng (S1-04 harness, S1-06 harness ×3, S1-07 helper accept + test-verify, journal BOM) |
| Lỗi theo nguyên nhân: TK / AG / CK | 4 / 1 / 2 (TK: harness UTF-8, harness CLIXML/ReadOnly, thiếu hợp đồng accept, projects.json; AG: lách test; CK: verify không chạy test-verify, merge push trước clone sạch) |

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật (công cụ, build, cổng kiểm tra, môi trường, model):
- Quy trình (thiết kế, chia task, giao việc, review, phối hợp 2 máy):
- Quản lý (Claude: lỗi TK, quyết định sai, chậm review):
- **Tốn thời gian nhất ở đâu:** test nghiệm thu do Codex viết sai ở ranh giới tiến trình (mã hóa, CLIXML, ReadOnly) ⇒ agent lách hoặc đỏ hàng loạt.
- **Cái gì làm tốt (giữ lại):** Codex QC bắt đúng mã lách test + chỗ hở kế hoạch; Codex dự phòng khi hết hạn mức Antigravity viết đúng ngay.
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1. Cổng KT chạy `tools/test-verify.ps1` khi diff chạm `team.ps1` / `tools/` / `scripts/` (V8-03).
  2. `merge`: test clone sạch TRƯỚC khi push main (V8-03). Mẫu codex-prep đã thêm luật harness PowerShell.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có
