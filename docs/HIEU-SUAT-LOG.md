# Nhật ký hiệu suất — mỗi yêu cầu một mục

Đơn vị theo dõi: **một yêu cầu của chủ dự án (R-xxx)**. Phiếu chi tiết: `requests/R-xxx-*.md`; số liệu: `team.ps1 request-report -Request R-xxx`.

Sau mỗi yêu cầu, Claude thêm 1 mục theo mẫu dưới đây. Cột "Cải tiến" là thay đổi quy trình áp dụng từ yêu cầu kế tiếp. Yêu cầu sau phải đo lại xem cải tiến đó có tác dụng không.

| Yêu cầu | Ngày | Tổng thời gian | Claude / Agent (phút) | Lượt agent / vòng review | Tự sửa | Lỗi TK / AG / CK | Tốn nhất ở | Cải tiến áp dụng | Cải tiến trước có tác dụng? |
|---|---|---|---|---|---|---|---|---|---|
| R-001 Hoàn tất kiểm tra bố cục (C-011, T-005, T-007) | 21/10 | 2 giờ 30 phút (từ lúc hỏi) | ~75 / ~372 (120 treo) | 10 / 2,33 | 1 lần, 4 dòng | 4 / 3 / 5 | Test mở hộp thoại thật ⇒ treo 30 phút / lượt | Bố cục cụ thể trong task giao diện; `team.ps1 stop`; số đo theo lượt | — (yêu cầu đầu tiên) |
| R-003 Nâng cấp toàn diện Hub (H3-01…05) | 24/10 | ~1 giờ 15 phút | ~65 / 48 (2 agent song song, ~25 thực) | 5 / 1,00 | 5 lần, ~90 dòng | 6 / 2 / 3 | Claude viết khung + 90 ca nghiệm thu (~45′) | Trang mới ⇒ test dựng trang thật; ca nghiệm thu từ dữ liệu THẬT | **Có**: phút agent / task 124′ (R-001) → 9,6′; đạt vòng 1 1/3 → 5/5; treo 120′ → 0; cổng KT sau IMP-001 không báo nhầm, tự trả BOM |

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
