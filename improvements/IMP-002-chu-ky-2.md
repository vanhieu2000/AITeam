# IMP-002 · Chu kỳ cải tiến 2 (H3-03 · H3-04 · H3-02)

- **Trạng thái:** 📏 Đã đo kết quả <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/H3-03, AITeam/H3-04, AITeam/H3-02
- **Ngày:** 24/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | Chu kỳ 1 (IMP-001) | Chu kỳ 2 | Xu hướng |
|---|---|---|---|
| 1. Tiếp nhận & quản lý | 5.0 | 5.0 | = |
| 2. Ý tưởng | 5.0 | 5.0 | = |
| 3. Plan / thiết kế | 4.3 | **4.0** | ↓ yếu nhất |
| 4. Giao việc | 3.7 | **5.0** | ↑ (mục kiểm tra trong mẫu task) |
| 5. Thực hiện code | 4.7 | 5.0 | ↑ |
| 6. Review / QC | 3.0 | **4.3** | ↑ (test hồi quy cổng KT) |
| 7. Đánh giá & bàn giao | 5.0 | 5.0 | = |

Hiệu suất: phút agent / task 7′ · 8′ · 13′ (TB 9,3′); đạt vòng 1 3/3; cổng KT đạt lần đầu 3/3, không báo nhầm.

## 2. Vấn đề trong chu kỳ
| # | Vấn đề | Giai đoạn | Nguyên nhân | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Đặc tả chỉ thử với dữ liệu mẫu nhỏ: khung giờ "Gần nhất" kéo 3 ngày; số đo cộng dồn theo phiên bị cộng thẳng (V4-06 51′ thay vì 23′) | 3. Plan | TK | 2 lần trong H3-02 (+ H3-05 ở chu kỳ 3) | Review H3-02 |
| 2 | Lỗi chỉ lộ ra khi chạy Hub thật, test nghiệm thu không bắt | 6. Review | TK | 3 lần (H3-02 ×2, H3-05) | Review H3-02, H3-05 |

## 3. Giải pháp (đã áp dụng ngay — đều nhỏ)
| # | Giải pháp | Sửa ở đâu | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|
| 1 | Trang / dịch vụ mới ⇒ đăng ký DI trong khung + thêm trang vào test dựng trang thật | `templates/task.md` (mục kiểm tra), `hub.tests/AntiforgerySecurityTests.cs` (đã thêm 5 trang v3) | Lỗi trắng trang / thiếu DI | 0 |
| 2 | Đọc dữ liệu thật ⇒ ≥ 1 ca nghiệm thu dựng từ bản sao dữ liệu THẬT, đủ dài / nhiều ngày | `templates/task.md` (mục kiểm tra) | Lỗi TK chỉ lộ khi chạy thật | ≤ 1 / chu kỳ |

## 4. Cần chủ dự án duyệt
Không (sửa mẫu task / test của Claude).

## 5. Kết quả (đo trên chu kỳ 3: H3-05, V4-07, V4-08)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. DI + test dựng trang | 1 lần (H3-05) | 0 (đã bắt cả trang mới P0-8 trong test) | **Có** | Giữ |
| 2. Ca nghiệm thu dữ liệu thật | 2 lần (H3-02) | 0 lỗi dữ liệu (V4-07 dùng câu lỗi thật Codex / agy) | **Có** | Giữ |
