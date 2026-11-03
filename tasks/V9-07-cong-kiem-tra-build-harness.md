# V9-07 — Cổng kiểm tra build tuần tự và quyền sửa harness
- Nhánh: `agent-c/V9-07-cong-kiem-tra-build-harness`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Build | Invoke-ShellCapture thêm -nodeReuse:false cho dotnet build và đổi cờ nr/nodeReuse:true; mutex Global/AITeam.Build bảo vệ cùng máy, timeout 15 phút, nhả trong finally kể cả lỗi. |
| Harness | Chỉ file nghiệm thu được khai báo Cho phép sửa harness mới gọi policy; diff chặn Assert. kể cả xóa, thuộc tính Fact/Theory/data/Skip và điều kiện Need/Expect-Error. Thiếu CLI/diff lỗi thì chặn. |
| Test | 6 test V907 mới đạt; CLI thật chấp nhận diff setup mẫu, không sửa assert cũ. PowerShell AST 0 lỗi. |
| Build/toàn bộ | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning/0 error. Core 403/696 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền; /tmp/r012-v907-tests. |
| Giới hạn | Chưa chạy hai tiến trình verify thật trên Windows; policy dòng là cổng tối thiểu, Claude vẫn phải review phần đổi đúng dựng môi trường, không đảm bảo ngữ nghĩa của mọi biểu thức test. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chờ nghiệm thu PowerShell 5.1. |
