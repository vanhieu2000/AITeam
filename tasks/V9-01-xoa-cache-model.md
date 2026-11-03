# V9-01 — Xóa cache model sau lỗi và đổi tài khoản
- Nhánh: `agent-c/V9-01-xoa-cache-model`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Cache | team.ps1 Clear-AgyModelCache xóa đúng .state/agy-models.json khi invalid model selection và trước switch-account; không theo symlink/reparse point. Gọi lặp khi file không có không lỗi. |
| Test | 2 ca V901ModelCacheTests mới dựng cache tạm và stub tiến trình/AI, kiểm cả hai luồng và file khác còn nguyên. Chưa chạy được do thiếu powershell.exe trên Linux. |
| Build/cú pháp | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning/0 error; PS AST 0 lỗi. |
| Toàn bộ test | Core 397/692 đạt, 295 lỗi (293 nền + 2 test mới thiếu PowerShell); Hub 776/790 đạt, 14 lỗi nền. Chưa nghiệm thu Windows, không sửa test cũ. |
| Quyết định | Xóa cache trước khi mở cửa sổ đăng nhập để không dùng lại model tài khoản cũ; không đổi tài khoản thật trong kiểm thử. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
