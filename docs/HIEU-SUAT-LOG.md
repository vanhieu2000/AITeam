# Nhật ký hiệu suất — mỗi yêu cầu một mục

Đơn vị theo dõi: **một yêu cầu của chủ dự án (R-xxx)**. Phiếu chi tiết: `requests/R-xxx-*.md`; số liệu: `team.ps1 request-report -Request R-xxx`.

Sau mỗi yêu cầu, Claude thêm 1 mục theo mẫu dưới đây. Cột "Cải tiến" là thay đổi quy trình áp dụng từ yêu cầu kế tiếp. Yêu cầu sau phải đo lại xem cải tiến đó có tác dụng không.

| Yêu cầu | Ngày | Tổng thời gian | Claude / Agent (phút) | Lượt agent / vòng review | Tự sửa | Lỗi TK / AG / CK | Tốn nhất ở | Cải tiến áp dụng | Cải tiến trước có tác dụng? |
|---|---|---|---|---|---|---|---|---|---|
| R-001 Hoàn tất kiểm tra bố cục (C-011, T-005, T-007) | 21/10 | 2 giờ 30 phút (từ lúc hỏi) | ~75 / ~372 (120 treo) | 10 / 2,33 | 1 lần, 4 dòng | 4 / 3 / 5 | Test mở hộp thoại thật ⇒ treo 30 phút / lượt | Bố cục cụ thể trong task giao diện; `team.ps1 stop`; số đo theo lượt | — (yêu cầu đầu tiên) |
| R-003 Nâng cấp toàn diện Hub (H3-01…05) | 24/10 | ~1 giờ 15 phút | ~65 / 48 (2 agent song song, ~25 thực) | 5 / 1,00 | 5 lần, ~90 dòng | 6 / 2 / 3 | Claude viết khung + 90 ca nghiệm thu (~45′) | Trang mới ⇒ test dựng trang thật; ca nghiệm thu từ dữ liệu THẬT | **Có**: phút agent / task 124′ (R-001) → 9,6′; đạt vòng 1 1/3 → 5/5; treo 120′ → 0; cổng KT sau IMP-001 không báo nhầm, tự trả BOM |
| R-002 AITeam đa AI — P0 (V4-03, V4-06, V4-07, V4-08 + P0-1/2/7/8 Claude) | 23–24/10 | ~1 ngày (xen R-003, nghỉ đêm) | ~180 / 46 | 6 / 1,00 | 5 lần, ~60 dòng | 6 / 2 / 2 (+1 HT) | Thiết kế + chạy thử CLI thật (Codex, agy) | Không sửa team.ps1 dở khi giao việc; Review AI cho task rủi ro | **Có**: phút agent / task 11,5′ (mục tiêu ≤ 30′); token / task 125k (mục tiêu ≤ 800k); đạt vòng 1 4/4 (mục tiêu ≥ 2/3); treo 0 |
| R-004 Codex developer dự phòng + trang Provider + rào an toàn (V4-09, H3-06, V4-10, V4-11) | 24/10 | ~2 giờ 15 phút | ~90 / 66 (41 là vòng lặp Codex) | 31 / 1,00 | 0 trong code agent; ~120 dòng công cụ | 2 / 1 / 3 | Vòng lặp 28 lượt Codex do cổng KT | Giới hạn 2 lượt / assign + bỏ qua NU1900; kiểm tra tĩnh biến đè tham số; thử tham số assign mới trước | **Có** (IMP-003 cả 3 có tác dụng); đạt vòng 1 4/4 |
| R-010 Bộ skill từng bước + skill code (S1-01…08) | 25/10 | ~6,5 giờ (xen việc khác) | ~60 / 213 | 17 / 1,00 | 5 lần, ~80 dòng | 4 / 1 / 2 | Test nghiệm thu Codex sai ranh giới tiến trình | Luật harness PowerShell trong codex-prep; cổng KT chạy test-verify + clone sạch trước push (V8-03) | Đo ở R-008 V8-02/03 |
| R-012 Gỡ task tắc + sửa Hub (30 mã; Codex rồi OpenCode code, Claude nghiệm thu) | 02–03/11 | ~18 giờ (có nghỉ đêm, ~6 giờ chờ hạn mức / chủ dự án) | ~300 / ~505 (Codex ~180 song song + OpenCode 325) | OpenCode 11 lượt · 8 mã · vòng TB 1,38 | 3 lần, ~120 dòng (chỉ test/harness hợp đồng) | 5 / 1 / 1 | Đổi hợp đồng không sửa test cũ ⇒ 2 đợt dừng hàng loạt; chờ hạn mức Codex | Rà `git grep` test theo hợp đồng cũ cùng commit đổi hợp đồng; chạy ngoài `assign` vẫn phải ghi lượt cho `request-report` | Không đo được: `request-report` chỉ thấy 10/30 task |
| R-014 Hub thành văn phòng AI (UI-01..09 Claude; V10-01..05, W2-01/02, K1-01..03) | 03–04/11 | ~17 giờ 45 phút (gồm đêm) | ~600 / ~206 (+~3 giờ AI treo) | ~16 / 1,15 | 6 lần, ~130 dòng (test / công cụ) | 7 / 3 / 7 (+3 HT) | AI treo im lặng + chờ hạn mức (~7 giờ) | Thử thật hành vi môi trường trước khi ghi thiết kế (v5.3); git pull --no-rebase sau merge; bộ canh treo cho mọi AI (K1-02 xong, K1-04 hàng chờ) | Đo ở yêu cầu sau |

## Quy trình hiện hành (cập nhật dần sau mỗi yêu cầu)
Phiên bản 3 — 21/10/2026 (chủ dự án chốt): **`QUY-TRINH.md`** — Claude chốt task đầy đủ một lần, agent CHỈ code (chỉ đọc file liệt kê, không ảnh / không chạy app, báo cáo ≤ 15 dòng, giới hạn thời gian theo task); Claude dùng `team.ps1 review` / `merge`. Mục tiêu R-002: ≤ 30 phút và ≤ 800 nghìn token / task, đạt vòng 1 ≥ 2/3.

Phiên bản 2 — 21/10/2026 sau R-001: thêm (a) task giao diện phải có bố cục cụ thể + danh sách lỗi hiện có từ UiSnapshot / LayoutChecker; (b) dừng task bằng `team.ps1 stop`; (c) số đo tính theo từng lượt khi -Resume.

Phiên bản 1 — 21/10/2026, trước yêu cầu đầu tiên được theo dõi:
1. Nhận yêu cầu ⇒ `team.ps1 new-request -Title "…"`, chép nguyên văn lời chủ dự án, ghi giờ.
2. Phân tích + thiết kế: Claude.
3. Chia việc:
   - mỗi task ≤ ~500 dòng, có ngân sách, khung code + test nghiệm thu viết trước;
   - A và B làm song song ở vùng code khác nhau;
   - việc ≤ ~100 dòng Claude tự làm.
4. Giao ⇒ cổng kiểm tra tự động ⇒ Claude review. Lỗi ≤ ~50 dòng Claude tự sửa ngay; còn lại trả agent kèm Blocker / Required.
5. Mỗi sự kiện ghi 1 dòng vào nhật ký phiếu yêu cầu. Mỗi lỗi ghi nguyên nhân gốc TK / AG / CK.
6. Xong ⇒ merge, clone sạch, báo chủ dự án ⇒ đánh giá trong phiếu ⇒ 1 dòng trong bảng trên ⇒ áp dụng tối đa 2 cải tiến (sửa sổ tay / mẫu / `team.ps1` ngay) ⇒ tăng số phiên bản quy trình ở mục này.
