# V9-14 — Định tuyến theo sáu luật
- Nhánh: `agent-c/V9-14-dinh-tuyen-theo-luat`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Bảng quyết định | DeveloperSelector mới, 9 test V914 cho Dev chỉ định, riêng tư, nghỉ, S/M/L, PowerShell/>300 dòng, tải, model Argon/DeepSeek, khóa và giờ hồi. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Test | Toàn bộ: Core 406/699 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền Windows; /tmp/r012-c3-tests. Không sửa assert cũ. |
| Chưa đủ | Hàm thuần đã có; chưa nối assign/nhật ký: main 759ddaf chưa có provider OpenCode C1, quan sát hạn mức B2 và cờ B4. Không tự nhập code các nhánh trước vào nhánh độc lập. |
| Quyết định | Luật độ khó ưu tiên trước cân tải; mặc định riêng tư không được coi là public; nghỉ không tự đổi Dev đã chỉ định. Cần review hợp đồng và tích hợp sau các nhánh nền. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chưa nghiệm thu điều phối tự động. |
