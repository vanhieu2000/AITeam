# Bàn giao giữa các máy / phiên làm việc

Claude: ĐỌC FILE NÀY ĐẦU MỖI PHIÊN (sau `git pull`), làm tiếp từ mục "Việc đang dở".
Cuối mỗi phiên: cập nhật file này, commit + push.

Cập nhật lần cuối: **03/11/2026 ~01:15 · máy công ty (hieudvdmin)** · **quy trình v5: đọc mục đầu `QUY-TRINH.md`**

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
- **Sau MỖI task merge:** điền mục "## Đánh giá quy trình" (7 giai đoạn, điểm 1–5, nguyên nhân TK/AG/CK/NV/HT). **Cứ 3 task:** `improvements/IMP-xxx` (đã có IMP-001, IMP-002; chu kỳ 3 bắt đầu từ H3-05). Hub trang Đội ngũ / Quy trình & cải tiến tự đọc các mục này.
- **Việc cần chủ dự án quyết:** `.\team.ps1 approval-request -Kind merge|decision|improvement …` ⇒ chủ dự án bấm trên Hub trang **Cần anh duyệt** ⇒ `merge` tự chặn nếu chờ / từ chối / nhánh đổi sau duyệt. Quyết định nằm ở `approvals/A-xxx.json` (commit lên git).
- **Sửa `team.ps1`** ⇒ chạy `tools\test-verify.ps1` trước commit. **Claude sửa code** ⇒ build + toàn bộ test trước commit (Hub đang chạy khóa `hub\bin`: tạm dừng Hub, test, `team.ps1 hub`).

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
| **R-012 Gỡ task tắc + sửa Hub (v5: Codex + OpenCode code, Claude review/chốt)** | ✅ XONG 03/11 tối: mọi mã R-012 đã merge (main 22389c2, clone sạch: build 0 cảnh báo, Hub 837/837, Core 937/937). Đợt 2: Codex hết hạn mức ⇒ OpenCode (DeepSeek, quyền hẹp, không --auto) làm H8-01, W1-03, W1-04, V9-14 sửa, H8-12, H8-13, V9-15, H8-15. Cách chạy OpenCode: `opencode run "<lời nhắn>" --dir <worktree> --format json --title <Mã> -f <prompt>` (lời nhắn TRƯỚC `-f`), OPENCODE_CONFIG = `AITeam-work/AITeam/opencode.agent-quyen-han.json`; lời giao trong `AITeam-work/AITeam/oc-*.md` | Optional hàng chờ: Get-ReviewFindings gọi CLI mỗi lượt (chậm); research chọn AI nghỉ + Thủ công bị từ chối cả yêu cầu; sau redirect mọi AI hiện "Đang hỏi…". Dọn worktree `AITeam-work/AITeam/rv/*` khi rảnh. Chưa chạy `request-report` / `staff-report` cho R-012 |
| **R-013 OpenCode** | Đã cài máy công ty: OpenCode 1.19.5 (khóa DeepSeek free) + Codex CLI 0.166.0. Thử thật: H7-04 5 phút, H7-06 lượt 2 19 phút, W1-03 18 phút — đều đạt build/test | O-02…O-06 (`assign -Dev opencode`…) nằm trong `plans/R-012/YEU-CAU-CODEX.md`. Máy nhà: cài 2 CLI + chép `templates/opencode-config.jsonc` |
| **Kho AITeam đang PUBLIC trên GitHub** | ⚠️ trái sổ tay ("luôn private") | Chủ dự án quyết; không đưa bí mật vào repo |
| **R-005 Văn phòng mini (giám sát + điều khiển + MCP + Tài khoản AI)** | 🟡 ĐANG LÀM — xong 25/10 02:26: V5-01 MCP (`aiteam mcp`), V5-02 assign nối MCP cho agy, V5-04 Văn phòng là trang chủ, V5-05 nút điều khiển (5 lệnh cố định, xác nhận, `.state/audit.jsonl`), V5-06 trang Tài khoản AI; 09:15: V5-07a/b Phòng nghiên cứu (ChatGPT qua Codex hỏi thật được, trả lời tự về; thủ công copy / dán; lưu `research/R-xxx/`). 7/7 đạt vòng 1. IMP-006 xong. Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` | Phòng nghiên cứu chỉ ChatGPT (tự động) + Thủ công (AI bất kỳ) — chủ dự án quyết; V5-10/11 bỏ. Tiếp: điều tra test MCP chập chờn (`Real_stdio_server…`, việc tách riêng); V5-08 đếm vòng review + Playwright E2E, MCP cho Codex (đang `mcp_servers={}`). Optional: log job Hub tiếng Việt từ PowerShell; xóa tay thư mục thừa `D:\AITeam-v506` |
| **R-004 Codex developer dự phòng (P1)** | ✅ XONG 24/10 ~11:15: `assign -Dev codex` + tự chuyển Codex khi agy hết hạn mức (Codex chạy thật V4-11: đúng đặc tả, 82 giây); trang Provider trên Hub; hook chặn agent push main (3 dự án); cảnh báo trùng phạm vi; tối đa 2 lượt / assign | Máy công ty: chạy `.\team.ps1 install-hooks` 1 lần (hook pre-push). P1 còn: cảnh báo lỗi liên tiếp theo provider, Pester cho team.ps1, MEMORY.md mỗi repo |
| **R-003 Hub v3 toàn diện** | ✅ XONG 24/10 ~01:20 — H3-01…05 merge, 5/5 đạt vòng 1, 48 phút agent. Hub mới: Tổng quan (chỉ số + biểu đồ thời gian), Giao việc (chat theo phiếu), Cần anh duyệt, Đội ngũ, Quy trình & cải tiến, tab Dòng thời gian / Đánh giá quy trình trong task | Optional: `request-report` cột Dòng/ngân sách vẫn tính khung + test của Claude; biểu đồ so sánh giữa các chu kỳ IMP |
| **A-001 đã duyệt (chủ dự án bấm trên Hub 00:36)** | Tạo user Windows `aiteam-agy2` cho tài khoản Antigravity số 2 | Cần chủ dự án: bấm UAC tạo user + đăng nhập user đó 1 lần (thiết kế V4 mục B3b). Sau đó Claude làm `runAs` trong assign / stop |
| **R-002 AITeam v4 đa AI** | ✅ P0 XONG 24/10 ~09:40: định tuyến provider (cấm tự duyệt, nghỉ khi hết hạn mức — `.state/providers.json` theo máy), `aiteam review`, `assign` tự review AI sau cổng KT (task L / `Review AI: có`), Hub hiện kết luận review. Codex chạy thật bắt 2 lỗi trong V4-08 | P1 (sau 1 tuần chạy ổn): reviewer dự phòng (Claude CLI chưa cài), developer dự phòng codex khi agy hết hạn mức. User Windows thứ 2: xem dòng A-001 |
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
