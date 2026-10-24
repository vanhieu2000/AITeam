# R-004 · Codex CLI làm developer dự phòng + trang Provider + rào an toàn (P1)

- **Nhận lúc:** 09:18 24/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** ✅ Xong 10:58 24/10/2026
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
| V4-10 aiteam providers trạng thái nghỉ (thử Codex) | AITeam | Codex (C) → thực tế agy | ~40 | | lỗi $dev đè -Dev |
| V4-11 ReviewService tên file riêng (thử Codex lần 2) | AITeam | Codex (C) | ~25 | | Codex thật |
| Hook chặn push main trong worktree + cảnh báo trùng phạm vi khi giao song song | AITeam | Claude | ~60 | | nhỏ |

Mỗi file task phải có dòng `- **Yêu cầu:** R-004` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 09:18 | Nhận yêu cầu | |
| 09:18 | Kiểm tra: Codex CLI 0.164.0 đã là bản mới nhất. Thử thật Codex workspace-write trong worktree (58s): sửa file + dotnet build + test ĐƯỢC; git add / commit BỊ CHẶN (index.lock ngoài sandbox) | ⇒ team.ps1 commit thay |
| 09:30 | V4-09 Minh xong 8', cổng KT ĐẠT, **review AI tự động lần đầu: Codex PASS 39s**; merge | P0-7 chạy trọn |
| 09:32 | Rào an toàn: hook pre-push chặn agent push main (thử 3 trường hợp), báo động sau lượt, cảnh báo trùng phạm vi; merge + install-hooks 3 dự án | |
| 09:36 | Giao V4-10 -Dev codex: SẬP ngay (developer C chưa có trong team.json ⇒ $dev null) [TK]; sửa + mặc định | |
| 09:37 | H3-06 Lan xong 12', cổng KT ĐẠT; lệnh báo mã 4 (mã bỏ qua review rò ra) [CK]; sửa | |
| 09:44 | V4-10 xong 5' nhưng do **agy** chạy: $dev đè -Dev (PowerShell không phân biệt hoa thường) [TK]; sửa ``; merge | |
| 09:50 | Review + merge H3-06 (Kiểm tra provider thật: agy + Codex OK); menu Provider AI | |
| 09:55–10:51 | V4-11 -Dev codex: Codex viết code thật OK nhưng cổng KT trượt vì NU1900 (mất mạng NuGet) + cờ tự giao lại không bật ⇒ **28 lượt Codex**, 41', ~6,9 triệu token vào [CK ×2] | |
| 10:58 | Sửa: bỏ qua NU1900, cờ + giới hạn cứng 2 lượt; test-verify +2 kịch bản + kiểm tra tĩnh biến đè tham số; merge V4-11 | |
| 10:58 | Đánh giá R-004, IMP-004 | |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
`request-report -Request R-004`: V4-09 (Minh 8′), H3-06 (Lan 12′), V4-10 (agy dưới mã C, 5′), V4-11 (Codex: 28 lượt, lượt cuối 82 giây). Đạt vòng 1: 4/4.

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | ~2 giờ 15 phút (08:55 → ~11:10) |
| Thời gian Claude | ~1 giờ 30 phút (thử Codex thật, 4 khung + 27 ca nghiệm thu, glue PowerShell, rào an toàn, xử lý 4 lỗi công cụ) |
| Thời gian agent | Minh 8′ · Lan 12′ · agy 5′ · Codex 41′ (27 lượt thừa) |
| Thời gian chờ không ai làm | ~0 (agent song song) |
| Số lượt agent / số vòng review | 31 / 4 (27 lượt thừa của Codex do công cụ) |
| Dòng code / ngân sách (agent viết) | 451 / 495 |
| Claude tự sửa | 0 trong code agent; ~120 dòng team.ps1 / test-verify (lỗi công cụ) |
| Lỗi theo nguyên nhân: TK / AG / CK | 2 / 1 / 3 |

**Đánh giá nhân viên:**

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|
| Minh (A) | V4-09 | 1 · 8′ | 1 | 155 / 170 | Codex review PASS 0 phát hiện | — | — |
| Lan (B) | H3-06 | 1 · 12′ | 1 | 265 / 260 | Đúng bố cục; Kiểm tra provider thật chạy đúng | BOM (lần 2) | Lưu ý riêng đã thêm |
| Codex (C) | V4-11 (+V4-10 thực tế agy) | 1 lượt hữu ích · 82 giây | 1 | 11 / 25 | Đúng đặc tả từng dòng, nhanh nhất team | — | Chỉ việc nhỏ (hạn mức) |

**Team đang bất ổn ở đâu:**
- Kỹ thuật: cổng KT coi lỗi mạng (NU1900) là lỗi code; cờ giới hạn tự giao lại chưa từng hoạt động (lỗi có từ R-002) ⇒ 28 lượt Codex. Đã sửa + test.
- Quy trình: tham số mới của `assign` (`-Dev codex`) chưa chạy thử cả lệnh trước khi giao thật ⇒ 2 lần hỏng (sập, chạy nhầm agy).
- Quản lý (Claude): 2 lỗi TK đều ở glue PowerShell (`$dev` đè `-Dev`, thiếu developer C).
- **Tốn thời gian nhất:** vòng lặp 28 lượt Codex (41′) — chỉ phát hiện khi lệnh kết thúc.
- **Làm tốt (giữ lại):** thử thật Codex trước khi thiết kế (biết Codex không commit được); review AI tự động chạy trọn; Codex viết đúng đặc tả từng dòng trong 82 giây; rào an toàn chạy thật trên 3 dự án.
- **Cải tiến áp dụng cho yêu cầu sau:**
  1. Giới hạn cứng số lượt agent mỗi lệnh `assign` (2) + cổng KT phân biệt lỗi môi trường (NU1900) — `team.ps1`, `tools/test-verify.ps1` (đã làm).
  2. Kiểm tra tĩnh "biến cục bộ đè tham số" trong `tools/test-verify.ps1`; mỗi tham số / nhánh mới của `assign` phải chạy thử với 1 task nhỏ trước khi dùng thật — ghi vào `QUY-TRINH.md`.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có