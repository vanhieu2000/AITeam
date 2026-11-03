### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 30/30 ca nghiệm thu; 59 + 189 test; build 0 warning; ~8 phút, 1 lượt.
- Code: regex gọn, chịu được dữ liệu hỏng (JSON hỏng, ô thiếu, file không có), gọi git bằng ArgumentList + chặn id lạ, đúng 9 bước ghép dữ liệu.
- [AG] (BOM) mất BOM UTF-8 ở 2 file khung `InsightParsers.cs`, `TeamInsightsService.cs`
- [CK] (BOM) cổng kiểm tra chỉ so BOM với `main` ⇒ không bắt được file khung mới chỉ có trên nhánh
- [CK] (phạm vi) cổng kiểm tra tính cả khung của Claude vào ngân sách ⇒ báo vượt nhầm 508/320 (thật: 285/320)
- [TK] (hiệu năng) khung không nói rõ cache giờ merge ⇒ mỗi phút gọi git lại cho mọi task đã merge
- Claude sửa (~10 dòng, 5 phút): trả BOM; đọc output git bất đồng bộ để giới hạn 10 giây có tác dụng; giờ merge đã biết thì nhớ luôn.
- Claude sửa cổng kiểm tra: so BOM với commit khung; ngân sách tính từ commit khung (chạy lại verify: 285/320, ĐẠT).
- Cảnh báo "ngồi chờ lệnh nền bị treo" là báo nhầm (lượt chạy 8 phút).

Nhân viên: Minh (A) — 1 lượt, ~8 phút, 285/320 dòng.

