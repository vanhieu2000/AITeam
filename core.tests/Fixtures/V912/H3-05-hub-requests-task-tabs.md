### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 14/14 ca nghiệm thu; build 0 warning; ~11 phút, 1 lượt; 348/380 dòng.
- Code: đọc phiếu chịu được dữ liệu thiếu, chặn mã phiếu lạ, kiểm dự án / độ dài khi gửi, không `Html.Raw` mới.
- Xem Hub thật (cổng 5058, dữ liệu thật): trang Giao việc hiện 3 phiếu R-001…R-003 với nhật ký; tab Dòng thời gian, Đánh giá quy trình của V4-06 đúng.
- [AG] (BOM) mất BOM UTF-8 ở 3 file khung — lần 2 (H3-01) ⇒ thêm vào Lưu ý riêng của Minh. Cổng KT mới (IMP-001) tự trả lại, không tốn lượt.
- [TK] (chất lượng) khung của Claude quên đăng ký `IRequestService` ⇒ trang /Requests trắng (lỗi 500); test nghiệm thu chỉ thử model, không dựng trang thật.
- Claude sửa (~10 dòng, 10 phút): đăng ký `IRequestService`; thêm 5 trang v3 (/, /Approvals, /Team, /Process, /Requests) vào test dựng trang thật `Pages_render_successfully_with_fake_services`; chữ lời chủ dự án bị khoảng trống thừa (`pre-wrap` cả khối) ⇒ chỉ áp cho phần chữ. 59 + 248 test pass.

Nhân viên: Minh (A) — 1 lượt, ~11 phút, 348/380 dòng.

