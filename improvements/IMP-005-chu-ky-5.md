# IMP-005 · Chu kỳ cải tiến 5 (V4-11 · V5-01 · V5-04)

- **Trạng thái:** ✅ Đã áp dụng <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/V4-11, AITeam/V5-01, AITeam/V5-04
- **Ngày:** 25/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | C2 | C3 | C4 | C5 | Xu hướng |
|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | 5.0 | 5.0 | 5.0 | 5.0 | = |
| 2. Ý tưởng | 5.0 | 5.0 | 5.0 | 5.0 | = (PoC MCP thật trước khi thiết kế) |
| 3. Plan / thiết kế | 4.0 | 4.0 | 5.0 | **4.3** | ↓ (thiếu đồng thời + symlink; giờ hiển thị) |
| 4. Giao việc | 5.0 | 4.3 | 4.0 | **4.7** | ↑ (thử tham số mới trước) |
| 5. Thực hiện code | 5.0 | 4.3 | 4.7 | **5.0** | ↑ (Minh, Lan, Codex đều đúng đặc tả) |
| 6. Review / QC | 4.3 | 4.7 | 4.3 | **3.3** | ↓ (V4-11 = 1 điểm: vòng lặp 28 lượt, đã sửa ở IMP-004) |
| 7. Đánh giá & bàn giao | 5.0 | 5.0 | 5.0 | 4.7 | ↓ (commit cùng lệnh với test) |

Không tính V4-11, mục 6 = 4.5: vòng review AI tự sửa (V5-01) chạy trọn lần đầu.

## 2. Vấn đề
| # | Vấn đề | Giai đoạn | Nguyên nhân | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Đặc tả thiếu ca gọi đồng thời + đọc qua symlink (Codex review bắt) | 3. Plan | TK | 1 | V5-01 |
| 2 | Đặc tả hiển thị ghép giờ vào chữ ⇒ bàn Claude hiện 2 giờ | 3. Plan | TK | 1 | V5-04 |
| 3 | Dòng ghi "🔵 Chờ review" sau cổng KT bị mất ⇒ Văn phòng hiện agent đang làm mãi | 6. Review | CK | 1 | V5-04 |
| 4 | Claude chạy test và commit trong cùng 1 lệnh ⇒ commit trước khi thấy 1 test lỗi (chập chờn) | 7. Bàn giao | TK | 1 | V5-01 |

## 3. Giải pháp (đã áp dụng)
| # | Giải pháp | Sửa ở đâu | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|
| 1 | Mục kiểm tra mẫu task: symlink / đồng thời ⇒ khóa + ca test; hiển thị ⇒ dựng thử bằng dữ liệu đang có | `templates/task.md` | Lỗi TK loại này ở review | 0 (V5-05, V5-06 đã có ca đồng thời / 2 lần bấm) |
| 2 | Kiểm tra tĩnh: `Invoke-Assign` phải có dòng ghi 🔵 / 🔴 sau cổng KT | `tools/test-verify.ps1` | Lần mất trạng thái bảng việc | 0 |
| 3 | Test và commit là 2 lệnh riêng | `QUY-TRINH.md` bước 4 | Commit có test đỏ | 0 |

## 5. Kết quả của IMP-004 (đo trên chu kỳ 5)
| Giải pháp IMP-004 | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| Giới hạn 2 lượt + bỏ qua NU1900 | 27 lượt thừa | 0 (V5-01 2 lượt đúng luật, V5-04 1 lượt) | **Có** | Giữ |
| Kiểm tra tĩnh biến đè tham số | 2 lỗi | 0 | **Có** | Giữ |
| Thử tham số mới trước | 2 lỗi | 0 (V5-02 chạy thử 35s trước khi dùng) | **Có** | Giữ |

## 6. Kết quả (điền sau chu kỳ 6)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. Mục kiểm tra symlink / đồng thời / hiển thị | 2 lỗi TK | | | |
| 2. Kiểm tra tĩnh trạng thái bảng việc | 1 | | | |
| 3. Tách test và commit | 1 | | | |
