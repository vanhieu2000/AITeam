# Bàn giao giữa các máy / phiên làm việc

Claude: ĐỌC FILE NÀY ĐẦU MỖI PHIÊN (sau `git pull`), làm tiếp từ mục "Việc đang dở".
Cuối mỗi phiên: cập nhật file này, commit + push.

Cập nhật lần cuối: **22/10/2026 · máy công ty HieuDV** (trước đó 21/10 ~22:20 máy nhà DESKTOP-PP14EFS) · **quy trình v3: đọc `QUY-TRINH.md`**

## 1. Cách quản lý đang áp dụng (chủ dự án yêu cầu 21/10)
Chi tiết: `CLAUDE.md` (sổ tay), `docs/VAN-HANH-HIEU-SUAT.md`, `docs/HIEU-SUAT-LOG.md`.

- **Mỗi yêu cầu của chủ dự án = 1 phiếu** `requests/R-xxx-*.md` (`.\team.ps1 new-request -Title "…"`):
  - chép nguyên văn yêu cầu, phân tích, chia việc;
  - **nhật ký có giờ** cho mọi sự kiện;
  - xong thì đánh giá (thời gian Claude / agent / chờ; lỗi phân loại [AG] nhân viên · [TK] task / thiết kế của Claude · [CK] cổng kiểm tra) + tối đa 2 cải tiến quy trình áp dụng ngay.
- **Nhân viên:** Agent A = **Minh**, Agent B = **Lan**.
  - Tên CHỈ dùng trong log và đánh giá; lời giao việc / commit / Hub vẫn ghi "Agent A/B".
  - Hồ sơ: `staff/A-minh.md`, `staff/B-lan.md`. Mục "Lưu ý riêng" tự đưa vào lời giao việc.
  - Bảng điểm: `.\team.ps1 staff-report`.
- **Task nhỏ:** ≤ 10–15 phút agent, có `- **Ngân sách:** ~N dòng`, `- **Yêu cầu:** R-xxx`.
  - Mục **"## Tóm tắt"** viết đầu tiên cho chủ dự án: 3–5 gạch đầu dòng lời thường, có chức năng mới hay không.
  - Khi đạt: điền **"## Kết quả"** (làm được gì, kiểm chứng ra sao, còn cần cải tiến gì).
  - Hub hiện 2 mục này ở tab Tổng quan.
- **Lỗi nhỏ sau review (≤ ~50 dòng) Claude tự sửa.** Phần sót của task đã gần đạt ⇒ tách task nhỏ mới, không trả cả task về làm lại.
- **Báo chủ dự án ngắn gọn, gạch đầu dòng:** việc gì, sửa gì; xong thì làm được gì, cần cải tiến gì.

## 1b. Chủ dự án test app thế nào
- **HDV Tool:** bấm đúp `D:\HDVTool\CHAY-THU.cmd` (tự lấy bản mới, build ra `D:\HDVTool\ban-chay-thu\`, mở app) — hoặc mở thẳng `D:\HDVTool\ban-chay-thu\HDVTool.exe`.
- **China Android Toolbox:** `D:\ChinaAndroidToolbox\CHAY-THU.cmd` hoặc `ban-chay-thu\ChinaAndroidToolbox.exe`.
- **AITeam Hub (web):** bấm đúp `D:\AITeam\CHAY-HUB.cmd`.
- Đây luôn là bản ĐÃ MERGE; agent đang làm ở thư mục riêng nên test lúc nào cũng được. Claude làm mới `ban-chay-thu\` sau mỗi lần merge.
- Máy khác (công ty) lần đầu: bấm `CHAY-THU.cmd` để tạo thư mục `ban-chay-thu\` (thư mục này không lên git).
- Máy công ty: các dự án nằm ở `D:\HIEUDV\<dự án>` (ví dụ `D:\HIEUDV\HDVTool\CHAY-THU.cmd`, `D:\HIEUDV\AITeam\CHAY-HUB.cmd`), thư mục agent `D:\HIEUDV\AITeam-work`.

## 2. Việc đang dở
| Việc | Trạng thái | Làm tiếp thế nào |
|---|---|---|
| **R-002 AITeam v4 đa AI** (`requests/R-002-…`, `docs/design/V4-MULTI-AI.md`) | P0-1, P0-2 (Claude), V4-03 Codex review (Minh), V4-06 trạng thái job (Lan) ĐÃ MERGE 24/10 00:15. Codex hết hạn mức tới 4:19 sáng 24/10 | Tiếp: P0-4/5 (chọn provider theo hạn mức + dự phòng, Codex chỉ cho task lớn/rủi ro), P0-7 (review tự động trong assign), P0-8 (Hub). User Windows thứ 2: chờ chủ dự án |
| **R-001** | ✅ XONG 22:15 — C-011, T-005, T-007 đã merge; đã đánh giá (quy trình lên phiên bản 2) | — |
| **Máy công ty 21/10 (chưa có phiếu R)** | ✅ XONG — làm theo quy trình cũ trước khi có v3, nên không có phiếu `requests/` | Không cần làm gì; chi tiết review trong từng file task |
| AITeam H-006 / H-007 / H-008 — Hub v2 (khung phẳng, Tổng quan, Dự án, Task, Agent, Hộp thư, Tài khoản, Bí mật, Cài đặt) | ✅ Đã merge vòng 1. H-007/H-008 agent hết credits giữa chừng ⇒ Claude chụp ảnh, sửa, điền báo cáo. Claude sửa: XSS qua `?drawer=`, nhãn tab hiện mã HTML, nhãn "Cổng kiểm tra ĐẠT" sai nghĩa, ngăn kéo cuộn ngang, cột Agent ghi cứng "A", nút lọc `.seg a`. 159/159 test, clone sạch | Optional: vòng review hiện trùng (danh sách + Markdown gốc); review cũ không có `###` gộp 1 khối; `PaginationSummary` ghi cứng 8; cột phải Tổng quan bị cắt chữ khi cửa sổ hẹp |
| ChinaAndroidToolbox C-012 — trang dài (Sửa lỗi nhanh, Tùy biến chia tab, bỏ banner adb) | ✅ Đã merge vòng 2. Agent hết credits lúc đang thử dò lỗi ⇒ Claude hoàn tác phần thử nghiệm (debug, tạm gỡ 2 tab Chẩn đoán), chụp ảnh, điền báo cáo. 571/571 test, clone sạch | Optional: tab "Bàn phím & Ngôn ngữ" bị cắt chữ; ô tìm kiếm Sửa lỗi nhanh không thấy chữ gợi ý trong ảnh |
| Antigravity trên máy công ty | ⚠️ `agy` báo hết credits (21/10 17:08) dù chủ dự án đã đổi tài khoản trong AntigravityManager — đổi ở app đó không áp dụng cho `agy` | Chạy `team.ps1 switch-account` rồi `team.ps1 doctor` trước khi giao việc |
| Hàng chờ HDVTool (giao diện) | Lồng tiếng video, Tóm tắt cuộc họp, Công cụ IT còn bị cắt ở cửa sổ nhỏ nhất; dòng cuối Nhật ký thay đổi | Theo quy trình v2: chạy UiSnapshot, viết bố cục cụ thể rồi mới giao |
| Hàng chờ Toolbox | C-006 tối ưu theo ROM — chờ chủ dự án cắm vivo X200 Pro chạy "Khảo sát ROM" + "Học cài đặt" | |
| Hàng chờ HDVTool | Trang Giới thiệu hiện người đăng nhập + vai trò; nhật ký thao tác quản trị | Chưa thiết kế |
| Optional | Toolbox: menu trái hiện thanh cuộn ngang ở cửa sổ nhỏ nhất | Việc nhỏ, Claude có thể tự làm |

## 3. Bài học hôm nay (đã sửa vào công cụ)
- Test giao diện mở form / hộp thoại thật ⇒ treo, agy chờ 30 phút / lượt.
  - Cổng kiểm tra chạy test với `--blame-hang-timeout 3m`.
  - Lời giao việc cấm form thật, bắt buộc helper `UiThread` (STA + timeout).
- Vượt ngân sách: cổng kiểm tra chỉ CẢNH BÁO, Claude quyết (ngân sách có thể do Claude ước thấp, như C-011).
- `-Task` phải gõ giống các lần trước; `team.ps1` giờ tự dùng lại thư mục làm việc của nhánh. Mã khớp nhiều file ⇒ báo lỗi.
- Hub đọc file task từ worktree của agent ⇒ thấy tiêu chí được tích ngay trong lúc làm.
