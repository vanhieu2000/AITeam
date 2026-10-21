# R-001 · Hoàn tất kiểm tra bố cục giao diện tự động (C-011 Toolbox, T-005 HDV Tool)

- **Nhận lúc:** 19:45 21/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** ✅ Xong 22:15 21/10/2026
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

| 20:32–20:37 | T-005 (Minh) lượt 4 xong, cổng kiểm tra ĐẠT | 245 test / 18 giây, hết treo |
| 20:41 | C-011 (Lan) cổng kiểm tra KHÔNG ĐẠT ⇒ tự giao lại | 648 test pass nhưng 1037/600 dòng + csproj ngoài phạm vi. [TK] (phạm vi) ngân sách Claude ước thấp, phạm vi thiếu csproj |
| 20:45 | Chủ dự án góp ý trang Task trên Hub (trùng lặp, tiêu chí không tích, sai agent) | Claude tự sửa Hub (~120 dòng): 3 tab, đọc tiêu chí từ worktree, lấy agent/nhánh từ file task |
| 20:55 | Review T-005: ĐẠT, merge, clone sạch 245/245 | Sót thẻ "Hệ thống" bị cắt ⇒ tách task nhỏ T-007 thay vì vòng 4 |
| 21:00 | Giao T-007 (Minh) | Ngân sách ~80 dòng |
| 21:02 | Dừng lượt tự giao lại của Lan (đang cắt code cho vừa ngân sách); review C-011: ĐẠT, merge, clone sạch 648/648 | Cải tiến: vượt ngân sách chỉ cảnh báo, Claude quyết |
| 21:08 | Ghi đánh giá Minh / Lan vào hồ sơ + lưu ý riêng; viết `BAN-GIAO.md` | |
| 21:15 | Lượt tự giao lại của Lan vẫn kịp chạy thêm (sau khi đã dừng 1 lần) ⇒ commit "gộp test bằng MemberData, giảm 354 dòng" | KHÔNG lấy: ngoài yêu cầu, mất 6 ca test (648 → 642). [CK] (công cụ) dừng agent nhưng wrapper `assign` vẫn tự giao lại — lần sau phải dừng cả tiến trình wrapper |

## 4. Đánh giá sau khi xong (Claude, 22:15 21/10/2026)
`.\team.ps1 request-report -Request R-001` (đã sửa lỗi đo cộng dồn khi `-Resume`):

| Task | Nhân viên | Lượt | Phút agent | Token | Dòng / ngân sách | Cổng KT lượt đầu | Vòng | Đạt |
|---|---|---|---|---|---|---|---|---|
| T-005 bộ kiểm tra bố cục HDV Tool | Minh (A) | 4* | 134 | 1,9 tr | 932 / 800 | trượt | 3 | ✅ |
| T-007 Giới thiệu bị cắt | Minh (A) | 1 | 73 | 2,3 tr | 51 / 80 | đạt | 1 | ✅ |
| C-011 bộ kiểm tra bố cục Toolbox | Lan (B) | 5 | 165 | 3,0 tr | ~1.037 / 600 | trượt | 3 | ✅ |

\* 2 lượt đầu của T-005 chạy trước khi bắt đầu ghi số đo (~13 + ~47 phút) — chưa cộng vào bảng.

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian | Chủ dự án hỏi 19:45 → xong 22:15 = **2 giờ 30 phút** (task bắt đầu từ 18:23 ⇒ 3 giờ 50 phút) |
| Thời gian Claude | ~75 phút: chẩn đoán 5 · chốt chặn / sửa công cụ 20 · sửa Hub theo góp ý 25 · review 3 task + tự sửa 25 |
| Thời gian agent | ~372 phút cộng dồn (2 agent song song), trong đó **~120 phút treo vô ích** (4 lượt × ~30 phút) |
| Lượt / vòng review | 10 lượt agent · vòng TB 2,33 · đạt vòng 1: 1/3 |
| Claude tự sửa | T-007: 4 dòng (cột phiên bản) · C-011: bỏ lượt tự giao lại, chấp nhận vượt ngân sách |
| Lỗi theo nguyên nhân | **TK 4** (không quy định cách dựng form trong test ×2, ngân sách ước sai, phạm vi thiếu csproj) · **AG 3** (form thật trong test ×2, sót chữ bị cắt) · **CK 5** (test không giới hạn thời gian, `-Task` khác tên ⇒ sai thư mục, dừng agent nhưng wrapper chạy tiếp, số đo cộng dồn, bộ kiểm tra bỏ sót) |

**Đánh giá nhân viên:**

| Nhân viên | Task | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|
| Minh (A) | T-005, T-007 | Sửa bố cục thật, tắt hộp chào mừng bằng cờ an toàn; T-007 qua cổng KT ngay lượt đầu | Form thật trong test; sót chữ bị cắt; T-007 thử bố cục rất lâu (73 phút / 51 dòng) | Lưu ý riêng đã thêm; task giao diện cần bố cục cụ thể |
| Lan (B) | C-011 | Bộ kiểm tra phủ mọi view + hộp thoại, test ~20 giây | Form thật trong test (lỗi DragDrop); tự làm thêm việc ngoài yêu cầu (gộp test, mất 6 ca) | Lưu ý riêng đã thêm |

**Team đang bất ổn ở đâu:**
- **Kỹ thuật:**
  - test giao diện WinForms dễ treo (STA, hộp thoại modal);
  - agy chờ lệnh nền tới 30 phút mà không báo;
  - số đo sai khi giao lại bằng `-Resume`;
  - bộ kiểm tra bố cục còn lỗ hổng.
- **Quy trình:**
  - dừng agent mà không dừng lệnh giao việc ⇒ agent tự chạy tiếp sau khi đã merge;
  - đặt tên task trùng / gõ `-Task` khác nhau giữa các lượt;
  - task giao diện chỉ mô tả mục tiêu, không có bố cục ⇒ agent thử mò.
- **Quản lý (Claude):**
  - giao 3 task sửa giao diện TRƯỚC khi có bộ kiểm tra tự động (đúng ra phải ngược lại);
  - task không quy định cách dựng form trong test;
  - ngân sách ước thấp;
  - trang Task trên Hub trình bày rối (chủ dự án phải góp ý).

- **Tốn thời gian nhất:** treo do test mở hộp thoại thật (~120 phút agent + 1,5 giờ chủ dự án chờ). Kế đến là T-007 thử bố cục 73 phút.
- **Làm tốt (giữ lại):**
  - tách phần sót thành task nhỏ (T-007) thay vì vòng 4;
  - Claude tự sửa lỗi nhỏ;
  - chốt chặn treo hiệu quả ngay (test còn 15–20 giây);
  - quyết định không bắt cắt code khi ngân sách do Claude ước sai.
- **Cải tiến áp dụng cho yêu cầu sau (quy trình phiên bản 2):**
  1. **Task giao diện phải có bố cục cụ thể** do Claude viết: cây control (TableLayoutPanel / FlowLayoutPanel, cột %, MinimumSize) + danh sách lỗi hiện có lấy từ `tools/UiSnapshot` / LayoutChecker trước khi giao. Agent không phải mò. (Sổ tay bước 4.)
  2. **`team.ps1 stop -Task X`** dừng hẳn task (cả lệnh assign lẫn agy). Số đo tính theo từng lượt (`conversationId`). (Đã làm trong `team.ps1`.)
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có
