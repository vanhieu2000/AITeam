# R-001 · Hoàn tất kiểm tra bố cục giao diện tự động (C-011 Toolbox, T-005 HDV Tool)

- **Nhận lúc:** 19:45 21/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > 2 phần mềm của tôi sao rồi. Tôi thấy agent dừng không chạy mà 2 task cũ chạy mất 47 phút??

## 1. Phân tích (Claude)
- **Mục tiêu:** hoàn tất 2 task đang kẹt (bộ kiểm tra bố cục giao diện tự động cho 2 app) và tìm vì sao mỗi task mất 47 phút.
- **Phần bị ảnh hưởng:**
  - `ChinaAndroidToolbox` nhánh `agent-b/C-011-ui-layout-tests` (Lan);
  - `HDVTool` nhánh `agent-a/T-005-ui-layout-tests` (Minh);
  - công cụ `AITeam/team.ps1`.
- **Nguyên nhân gốc** (xác minh bằng log stderr, CPU tiến trình, chụp cửa sổ):
  - Test giao diện mở form thật bằng `form.Show()` trên luồng test MTA.
    - Toolbox: control có kéo-thả ⇒ .NET bật hộp "Unhandled exception — DragDrop registration did not succeed", chờ bấm Continue.
    - HDV Tool: dựng MainForm ⇒ bật hộp "Chào mừng đến với HDV Tool", chờ bấm Đã hiểu.
  - Agent chạy test ở chế độ nền ⇒ agy "waiting up to 30m0s for 1 background task" ⇒ chờ đủ 30 phút rồi cắt, chưa commit, chưa báo cáo.
  - Cổng kiểm tra tự động tự giao lại 1 lần ⇒ treo y hệt lần 2. Cổng kiểm tra cũng treo vì chạy `dotnet test` không có giới hạn thời gian.
- **Quyết định tự chọn:**
  - giữ phần code dở của agent (lưu WIP lên nhánh);
  - giao lại cho đúng người kèm hướng dẫn chính xác, vì sửa cần dò code khởi động app — vượt mức "lỗi nhỏ Claude tự sửa".

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| Chốt chặn treo trong `team.ps1` | AITeam | Claude | ~30 dòng | — | Lỗi công cụ (CK), nhỏ, tự làm ngay |
| C-011-ui-layout-tests vòng 3 | ChinaAndroidToolbox | Lan (B) | ~150 dòng sửa | mặc định | Lan đã viết bộ test, sửa luồng STA nhanh nhất |
| T-005-ui-layout-tests vòng 3 | HDVTool | Minh (A) | ~200 dòng sửa | mặc định | Cần dò MainForm / hộp chào mừng + 3 lỗi bố cục thật |

## 3. Nhật ký
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 18:23 | Giao C-011 (Lan), T-005 (Minh) — lượt 1 | Giao trước khi có quy trình mới |
| 19:05–19:06 | Lượt 1 kết thúc, chưa commit | Lan 12 phút, Minh 13 phút. Thật ra treo ở test giao diện, agy cắt |
| 19:08 | Cổng kiểm tra KHÔNG ĐẠT ⇒ tự giao lại lượt 2 | Chưa commit 3 file, chưa báo cáo; T-005 có 7 test bố cục trượt |
| 19:40–19:41 | Lượt 2 kết thúc sau ~47 phút | stderr: "waiting up to 30m0s for 1 background task" — 30 phút chờ lệnh treo |
| 19:41 | Cổng kiểm tra lượt 2 treo | testhost mở hộp thoại, CPU đứng yên |
| 19:45 | Chủ dự án hỏi | |
| 19:47 | Claude chẩn đoán xong | Log, CPU, chụp 2 cửa sổ hộp thoại |
| 19:48 | Dừng testhost treo; lưu WIP lên 2 nhánh | Toolbox +422 dòng, HDV Tool +593 dòng |
| 19:49 | Claude thêm chốt chặn treo vào `team.ps1` (commit 1880b73) | [CK] (test) cổng kiểm tra không giới hạn thời gian test · [TK] (test) task không quy định cách dựng form trong test |
| 19:50 | Ghi review vòng 2 + R-001 + ngân sách vào 2 file task | [TK] (test) ngân sách lúc đầu chưa tính phần WIP — sửa ngay |
| 19:51 | Giao lại thất bại cả 2 | [CK] (công cụ) `-Task` gõ tên đầy đủ khác lần trước ⇒ `team.ps1` tìm thư mục làm việc khác ⇒ lỗi. Đã sửa `team.ps1`: tự dùng lại thư mục của nhánh |
| 19:52 | Giao lại C-011 (Lan) vòng 3, T-005 (Minh) vòng 3 | Tiếp tục hội thoại cũ, có chốt chặn treo |

## 4. Đánh giá sau khi xong (Claude)
(điền khi xong)
