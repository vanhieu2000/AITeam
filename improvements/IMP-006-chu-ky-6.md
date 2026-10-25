# IMP-006 · Chu kỳ cải tiến 6 (V5-05 · V5-06 · V5-07a · V5-07b)

- **Trạng thái:** ✅ Đã áp dụng <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/V5-05, AITeam/V5-06, AITeam/V5-07a, AITeam/V5-07b
- **Ngày:** 25/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | C3 | C4 | C5 | C6 | Xu hướng |
|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | 5.0 | 5.0 | 5.0 | 5.0 | = |
| 2. Ý tưởng | 5.0 | 5.0 | 5.0 | 5.0 | = |
| 3. Plan / thiết kế | 4.0 | 5.0 | 4.3 | **4.8** | ↑ |
| 4. Giao việc | 4.3 | 4.0 | 4.7 | **4.5** | ↓ (nhánh trước file task; hook chặn test của Claude) |
| 5. Thực hiện code | 4.3 | 4.7 | 5.0 | **5.0** | = (4/4 đạt vòng 1, 7–13 phút) |
| 6. Review / QC | 4.7 | 4.3 | 3.3 | **5.0** | ↑ (chạy Hub thật bắt lỗi xác nhận; hỏi AI thật) |
| 7. Đánh giá & bàn giao | 5.0 | 5.0 | 4.7 | **4.8** | ↑ |

## 2. Vấn đề
| # | Vấn đề | Giai đoạn | Nguyên nhân | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Đặc tả `hx-confirm` trên nút submit — HTMX 2 bỏ qua ⇒ nút Dừng không hỏi | 3. Plan | TK | 1 | V5-05 |
| 2 | Tạo nhánh trước khi đẩy file task lên main | 4. Giao việc | TK | 1 | V5-06 |
| 3 | Test nghiệm thu chứa chuỗi giống khóa API ⇒ hook chặn commit | 4. Giao việc | TK | 1 | V5-07a |
| 4 | Test MCP chạy tiến trình thật lỗi chập chờn khi cả solution chạy song song ngay sau build | 7. Bàn giao | HT | 2 (R-005) | V5-01, V5-07b |

## 3. Giải pháp (đã áp dụng)
| # | Giải pháp | Sửa ở đâu | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|
| 1 | `hx-confirm` đặt trên form | `AGENTS.md` (Lỗi hay gặp) | Nút thao tác không hỏi xác nhận | 0 |
| 2 | Chuỗi giống bí mật trong test ⇒ ghép lúc chạy | `AGENTS.md` + `templates/task.md` | Commit bị hook chặn vì test | 0 |
| 3 | Điều tra test chập chờn thành việc riêng (không nới test) | việc tách riêng (chip) | Lần chạy test cả solution lỗi chập chờn | 0 |

## 5. Kết quả của IMP-005 (đo trên chu kỳ 6)
| Giải pháp IMP-005 | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| Mục kiểm tra symlink / đồng thời / hiển thị | 2 lỗi TK | 0 (V5-05 khóa toàn bộ Start + ca 2 lần bấm; V5-07b khóa theo AI) | **Có** | Giữ |
| Kiểm tra tĩnh trạng thái bảng việc | 1 | 0 (4/4 task bảng việc đúng 🔵) | **Có** | Giữ |
| Tách test và commit | 1 | 0 (lần lỗi chập chờn V5-07b bị phát hiện TRƯỚC commit) | **Có** | Giữ |

## 6. Kết quả (điền sau chu kỳ 7)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. hx-confirm trên form | 1 | | | |
| 2. Chuỗi bí mật giả ghép lúc chạy | 1 | | | |
| 3. Test chập chờn | 2 | | | |
