# H8-06 — Hai đơn chờ duyệt trong dữ liệu giả
- Nhánh: `agent-c/H8-06-don-duyet-mau`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Dữ liệu mẫu | FakeApprovalService mới: 2 đơn Pending, loại merge/quyết định, nhãn dữ liệu mẫu; FakeServices đăng ký trước dịch vụ thật. Quyết định chỉ đổi bộ nhớ, không chạy lệnh/merge. |
| Kiểm thử | H806FakeApprovalTests đạt: HTTP thật hiện 2 nút approve/2 reject, duyệt/từ chối, chặn quyết định lại, cây file không đổi. |
| Build/test | Build -warnaserror -nodeReuse:false 0 warning/0 error; Hub 777/791 đạt, 14 lỗi nền; Core 397/690 đạt, 293 lỗi nền; không sửa assert cũ. |
| Ảnh thật | docs/screenshots/H8-06/approvals-1280.png, Chromium HUB_FAKE/gốc tạm, document 1280/1280, đã xem ảnh. |
| Giới hạn | Chưa nghiệm thu toàn bộ Windows; mẫu không tồn tại sau khi khởi động lại Hub (cố ý tránh ghi file thật). |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
