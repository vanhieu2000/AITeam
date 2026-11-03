# O-02 — OpenCode/DeepSeek làm người code thứ hai

- Nhánh: `agent-c/O-02-opencode-deepseek`; yêu cầu: R-012, C1.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md, QUY-TRINH.md v5.
- Phạm vi: provider OpenCode, cấu hình/CLI, team.ps1, test mới và fixture lỗi thật.
- Không làm: model ngoài DeepSeek, quyền tự động, sửa test nghiệm thu cũ, merge main.
- Ngân sách: chưa đo phút/token.

## Tiêu chí hoàn thành
- [x] Provider + assign/doctor, stdin đóng, prompt đính kèm, bản sao quyền, tiêu đề task, báo cáo cuối, lỗi quyền rõ ràng.
- [x] Chặn model ngoài DeepSeek; repo private/không rõ riêng tư + miễn phí bị từ chối; giữ trạng thái provider bị tắt.
- [x] Test giả không gọi mạng; token chưa đo giữ trạng thái không rõ.
- [ ] Chạy thật một task thành công có token trên Windows.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Tích hợp + bảo vệ | OpenCodeProvider/ProviderConfig/ProviderModels/DevRunner, CLI và team.ps1; cấu hình opencode.model/exe/paid; project phải ghi private=false để dùng miễn phí. |
| Test giả | 8 test O02 đạt, gồm tiến trình Node thật xác nhận stdin đóng và fixture proxy lỗi thật đã bỏ ID. Không sửa assert cũ. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Toàn bộ test | Core 405/698 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền Windows. Chi tiết /tmp/r012-c1-final-tests; chưa đạt nghiệm thu toàn bộ. |
| PowerShell | Parser AST 7.6: 0 lỗi; chưa kiểm runtime Windows PowerShell 5.1. |
| Chạy thật | OpenCode 1.19.5, opencode-check nhận phiên bản; gọi DeepSeek trong thư mục tạm bị proxy 403 sau khoảng 2 giây. Fixture chỉ ghi lỗi thật; không có token thành công. |
| Quyết định/giới hạn | Không đoán đăng nhập từ phiên bản; không thử model khác; native npm Windows ưu tiên bin/opencode.exe. Chưa xác minh máy Windows và mạng chạy model. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chờ kiểm mạng/Windows, không tuyên bố hoàn tất C1. |
