# V9-05 — Dispose singleton khi Build lỗi
- Nhánh: `agent-c/V9-05-dispose-khi-build-loi`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Sửa helper | HubTestHost giữ provider ngay sau CreateServiceProvider, trước khi WebApplicationBuilder.Build resolve IHost/logger; khi Build lỗi, cleanup cả singleton tạo từ factory và instance. Giữ kiểm scope/validate tương ứng môi trường. |
| Test mới | V905BuildCleanupTests dựng singleton thật, logger ném lỗi trong Build, xác nhận lỗi gốc giữ nguyên và cả 2 probe dispose đúng 1 lần. Đạt. |
| Build/test | Build -warnaserror -nodeReuse:false 0 warning/0 error; Hub 777/791 đạt, 14 lỗi nền; Core 397/690 đạt, 293 lỗi nền. Các test cleanup W1-02 cũ giữ nguyên. |
| File/giới hạn | Chỉ helper W1-02 và test mới; chưa nghiệm thu toàn bộ Windows. Không sửa assert test cũ. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
