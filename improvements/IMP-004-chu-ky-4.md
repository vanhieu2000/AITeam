# IMP-004 · Chu kỳ cải tiến 4 (V4-09 · H3-06 · V4-10)

- **Trạng thái:** ✅ Đã áp dụng <!-- 🟡 Đề xuất · 🔵 Chờ chủ dự án duyệt · ✅ Đã áp dụng · 📏 Đã đo kết quả -->
- **Task trong chu kỳ:** AITeam/V4-09, AITeam/H3-06, AITeam/V4-10
- **Ngày:** 24/10/2026

## 1. Điểm trung bình từng giai đoạn
| Giai đoạn | C1 | C2 | C3 | C4 | Xu hướng |
|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | 5.0 | 5.0 | 5.0 | 5.0 | = |
| 2. Ý tưởng | 5.0 | 5.0 | 5.0 | 5.0 | = |
| 3. Plan / thiết kế | 4.3 | 4.0 | 4.0 | **5.0** | ↑ (mục kiểm tra mẫu task + thử CLI thật trước) |
| 4. Giao việc | 3.7 | 5.0 | 4.3 | **4.0** | ↓ (tham số -Dev chưa thử cả lệnh) |
| 5. Thực hiện code | 4.7 | 5.0 | 4.3 | 4.7 | ↑ |
| 6. Review / QC | 3.0 | 4.3 | 4.7 | 4.3 | ↓ (mã thoát rò, chạy nhầm agy) |
| 7. Đánh giá & bàn giao | 5.0 | 5.0 | 5.0 | 5.0 | = |

Ngay sau chu kỳ: V4-11 lộ vòng lặp 28 lượt Codex (cổng KT coi NU1900 là lỗi code + cờ giới hạn không hoạt động).

## 2. Vấn đề
| # | Vấn đề | Giai đoạn | Nguyên nhân | Lặp lại? | Bằng chứng |
|---|---|---|---|---|---|
| 1 | Tham số mới `-Dev codex` không thử cả lệnh: sập (thiếu developer C), rồi chạy nhầm agy (`$dev` đè `-Dev`) | 4. Giao việc | TK | 2 lần | V4-10 |
| 2 | Lỗi môi trường bị coi là lỗi code + giới hạn tự giao lại không hoạt động ⇒ 28 lượt | 6. Review | CK | 1 (lỗi tiềm ẩn từ R-002) | V4-11 |
| 3 | Mã thoát của lệnh con rò thành mã thoát assign | 6. Review | CK | 1 | H3-06 |

## 3. Giải pháp (đã áp dụng)
| # | Giải pháp | Sửa ở đâu | Chỉ số đo | Mục tiêu |
|---|---|---|---|---|
| 1 | Giới hạn cứng 2 lượt / lệnh assign; bỏ qua NU1900 | `team.ps1`, `tools/test-verify.ps1` (kịch bản 5, 6) | Lượt agent thừa do công cụ | 0 |
| 2 | Kiểm tra tĩnh biến cục bộ đè tham số script | `tools/test-verify.ps1` | Lỗi kiểu `$dev`/`-Dev` | 0 |
| 3 | Tham số / nhánh mới của assign ⇒ chạy thử với 1 task nhỏ trước | `QUY-TRINH.md` | Lỗi glue khi giao thật | 0 |

## 5. Kết quả của IMP-003 (đo trên chu kỳ 4)
| Giải pháp IMP-003 | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| Không để team.ps1 sửa dở khi giao | 1 | 0 (sửa trong worktree claude/…) | **Có** | Giữ |
| Review AI cho task rủi ro | 2 lỗi chỉ Codex bắt | V4-09 Codex PASS, không lọt lỗi | **Có** | Giữ |
| Test tiến trình con dùng ProjectReference | 1 | 0 (V4-10 dùng đúng) | **Có** | Giữ |

## 6. Kết quả (điền sau chu kỳ 5)
| Giải pháp | Trước | Sau | Có tác dụng? | Giữ / bỏ |
|---|---|---|---|---|
| 1. Giới hạn lượt + NU1900 | 27 lượt thừa | | | |
| 2. Kiểm tra tĩnh biến đè tham số | 2 lỗi | | | |
| 3. Thử tham số mới trước | 2 lỗi | | | |