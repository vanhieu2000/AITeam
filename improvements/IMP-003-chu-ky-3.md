# IMP-003 · Chu kỳ cải tiến 3 (H3-05 · V4-07 · V4-08)

- **Trạng thái:** 📏 Đã đo kết quả (xem IMP-004 mục 5) <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/H3-05, AITeam/V4-07, AITeam/V4-08
- **Ngày:** 24/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | Chu kỳ 1 | Chu kỳ 2 | Chu kỳ 3 | Xu hướng |
|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | 5.0 | 5.0 | 5.0 | = |
| 2. Ý tưởng | 5.0 | 5.0 | 5.0 | = |
| 3. Plan / thiết kế | 4.3 | 4.0 | **4.0** | = yếu nhất (Claude) |
| 4. Giao việc | 3.7 | 5.0 | **4.3** | ↓ (giao khi team.ps1 sửa dở) |
| 5. Thực hiện code | 4.7 | 5.0 | 4.3 | ↓ (BOM ×2) |
| 6. Review / QC | 3.0 | 4.3 | **4.7** | ↑ (Codex chạy thật bắt 2 lỗi) |
| 7. Đánh giá & bàn giao | 5.0 | 5.0 | 5.0 | = |

Hiệu suất: 11′ · 6′ · 7′ agent / task; đạt vòng 1 3/3; cổng KT đạt lần đầu 3/3.

## 2. Vấn đề trong chu kỳ
| # | Vấn đề | Giai đoạn | Nguyên nhân | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Đặc tả thiếu nhánh lỗi bất ngờ / tham số lạ (Arg nhận "--auto", RunAsync sập) | 3. Plan | TK | 2 lần (V4-08) | Codex review thật |
| 2 | Giao việc khi `team.ps1` còn sửa dở ⇒ wrapper chạy bản dở | 4. Giao việc | TK | 1 lần | V4-08 |
| 3 | Mất BOM file khung | 5. Code | AG | 2 lần (Minh H3-05, Lan V4-08) — cổng KT tự trả | verify |
| 4 | Test CLI trỏ bin\Debug của project khác ⇒ clean-clone hỏng | 5. Code | AG | 1 lần | merge V4-08 |

## 3. Giải pháp (đã áp dụng)
| # | Giải pháp | Sửa ở đâu | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|
| 1 | Không để `team.ps1` sửa dở khi giao việc; commit + test-verify trước | `QUY-TRINH.md` | Lỗi do script dở | 0 |
| 2 | Task có bảo mật / tham số dòng lệnh / dữ liệu ngoài ⇒ `Review AI: có` (Codex chạy thật) | `templates/task.md`, `team.ps1 assign` (P0-7) | Lỗi bảo mật / độ bền lọt qua review | 0 |
| 3 | Test chạy tiến trình con dùng file đi kèm bản build (ProjectReference) | `AGENTS.md` (Lỗi hay gặp) | Clean-clone hỏng vì thứ tự build | 0 |

## 4. Cần chủ dự án duyệt
Không.

## 5. Kết quả (điền sau chu kỳ 4)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. team.ps1 không sửa dở | 1 | | | |
| 2. Review AI cho task rủi ro | 2 lỗi chỉ Codex bắt | | | |
| 3. Test tiến trình con | 1 | | | |