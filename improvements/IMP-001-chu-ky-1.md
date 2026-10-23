# IMP-001 · Chu kỳ cải tiến 1 (V4-03 · V4-06 · H3-01)

- **Trạng thái:** 📏 Đã đo kết quả <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/V4-03, AITeam/V4-06, AITeam/H3-01
- **Ngày:** 24/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | Chu kỳ trước | Chu kỳ này | Xu hướng |
|---|---|---|---|
| 1. Tiếp nhận & quản lý | — | 5.0 | (chu kỳ đầu) |
| 2. Ý tưởng | — | 5.0 | |
| 3. Plan / thiết kế | — | 4.3 | |
| 4. Giao việc | — | 3.7 | yếu |
| 5. Thực hiện code | — | 4.7 | |
| 6. Review / QC | — | 3.0 | yếu nhất |
| 7. Đánh giá & bàn giao | — | 5.0 | |

Hiệu suất so với R-001 (trước quy trình v3): phút agent / task 124′ → **10′** (9, 13, 8); đạt vòng 1: 1/3 → **3/3**; treo: 120′ → 0.

## 2. Vấn đề trong chu kỳ
| # | Vấn đề | Giai đoạn | Nguyên nhân (TK/AG/CK/NV/HT) | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Cổng kiểm tra / `merge` sai với luồng "khung trên nhánh": `merge` lộ 3 lỗi lần đầu; file khung bị tính "ngoài phạm vi" ⇒ tự giao lại 2 lần vô ích; lọt mất BOM file khung; tính khung vào ngân sách | 6. Review / QC | CK | **3/3 task** | V4-03, V4-06, H3-01 (Review) |
| 2 | Task / thiết kế thiếu chi tiết ở chỗ giao tiếp với bên ngoài: BOM file đưa cho Codex CLI, quên file khung trong Phạm vi, không nói cache khi gọi git | 3–4. Plan / Giao việc | TK | **3/3 task** | V4-03, V4-06, H3-01 |
| 3 | Claude commit DI + menu trước khi chạy hết test ⇒ 3 test dựng trang hỏng (sửa trong 5′) | 6. Review / QC | TK | 1 lần | R-003 nhật ký |
| 4 | Codex hết hạn mức giữa lúc review | 6. Review / QC | HT | 1 lần | V4-03 |
| 5 | Mất BOM file .cs | 5. Code | AG | 1/3 (lịch sử 33%) | H3-01 — cổng KT nay tự trả BOM |

## 3. Giải pháp (tối đa 3)
| # | Giải pháp | Sửa ở đâu | Chi phí | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|---|
| 1 | **Test hồi quy cho cổng kiểm tra**: script dựng repo tạm với commit khung + commit agent (mất BOM ở file khung, khung không đổi, vượt ngân sách, ngoài phạm vi) và kiểm kết quả `verify` | `tools/test-verify.ps1` (mới), chạy mỗi khi sửa `team.ps1` | ~1 giờ Claude | Lỗi CK / chu kỳ | 3 → 0 |
| 2 | **Mục kiểm tra "Hợp đồng ngoài & khung" trong mẫu task**: mã hóa / BOM file đưa cho tiến trình ngoài; gọi tiến trình ngoài: giới hạn thời gian + cache; mọi file khung có trong Phạm vi hoặc ghi "KHÔNG đổi" | `templates/task.md` | 10′ | Lỗi TK / task | 1,0 → ≤ 0,3 |
| 3 | **Claude chạy toàn bộ test trước MỌI commit code** (kể cả sửa nhỏ, kể cả khi Hub đang chạy: build ra thư mục riêng chỉ để biên dịch, test chạy tại chỗ sau khi tạm dừng Hub) | `QUY-TRINH.md` bước 4 | 0 | Commit làm hỏng test | 1 → 0 |

Không đưa vào: Codex hết hạn mức (đã nằm trong kế hoạch P0-4/5: Codex chỉ cho task L / bảo mật + reviewer dự phòng); BOM của agent (cổng KT đã tự trả, không tốn vòng).

## 4. Cần chủ dự án duyệt
Không — cả 3 là sửa công cụ / mẫu / thói quen của Claude, không đổi quy trình với chủ dự án. Đã áp dụng cả 3 ngày 24/10:
- (1) `tools/test-verify.ps1`: 4 kịch bản, 12 kiểm tra. Chạy với `team.ps1` TRƯỚC khi sửa ⇒ bắt đúng 3 lỗi CK của H3-01; bản hiện tại ⇒ đạt hết.
- (2) mục kiểm tra trong `templates/task.md` (Ràng buộc).
- (3) `QUY-TRINH.md` bước 4.

## 5. Kết quả (đo trên chu kỳ 2: H3-03, H3-04, H3-02 + H3-05)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. Test hồi quy cổng KT | 3 lỗi CK / 3 task | 0 báo nhầm; H3-05 tự trả BOM 3 file khung (trước đây lọt) | **Có** | Giữ |
| 2. Mục "Hợp đồng ngoài & khung" | 1,0 TK / task | 0,75 TK / task (3 lỗi / 4 task: khung giờ, số đo cộng dồn, quên đăng ký DI — loại lỗi MỚI, không lặp loại cũ) | Một phần | Giữ + thêm 2 mục (DI + test dựng trang; dữ liệu thật) |
| 3. Test trước mọi commit | 1 | 0 | **Có** | Giữ |
