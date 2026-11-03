### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 19/19 ca nghiệm thu; 59 + 225 test; build 0 warning; ~13 phút, 1 lượt; 370/330 dòng (vượt nhẹ, chấp nhận: SVG + bảng lọc).
- Code đúng đặc tả; xem Hub thật (cổng 5058, dữ liệu thật): 5 chỉ số, biểu đồ, bảng 20 task, lọc dự án / trạng thái đúng bố cục.
- [TK] (giao diện) đặc tả "Gần nhất" của Claude lấy 12 task bất kể ngày ⇒ dữ liệu thật kéo khung từ 21/10 tới 24/10, thanh hôm nay chỉ vài px; mốc 00:00 lặp không ghi ngày.
- Claude sửa (~12 dòng + 2 ca nghiệm thu, 10 phút): "Gần nhất" chỉ lấy task có hoạt động trong 24 giờ tính từ hoạt động mới nhất; khung > 1 ngày thì mốc 00:00 ghi "dd/MM". 59 + 226 test pass.
- [TK] (dữ liệu) số đo các lượt cùng phiên agy là CỘNG DỒN theo thiết kế (team.ps1 tính chênh lệch lúc đọc, Get-RunDeltas) nhưng đặc tả H3-01 của Claude quên bước này ⇒ Hub hiện V4-06 51′ thay vì 23′. Claude sửa: `InsightParsers.ToDeltas` + `RunRecord.ConversationId` + 2 test số liệu thật (251 test pass).

Nhân viên: Lan (B) — 1 lượt, ~13 phút, 370/330 dòng.

