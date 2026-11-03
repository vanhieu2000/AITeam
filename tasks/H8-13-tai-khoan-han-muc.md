# H8-13 — Tài khoản và hạn mức
- Nhánh: `agent-c/H8-13-tai-khoan-han-muc`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Mockup | docs/design/H8-13-mockup.html: HTML tĩnh tiếng Việt, ghi rõ số minh họa, chưa nối dữ liệu. |
| Kiểm bố cục | Chromium thật 1000×800: document scrollWidth=clientWidth=1000; mọi nút không tràn. Ảnh docs/screenshots/H8-13/mockup-1000.png. |
| Trạng thái | ĐÃ PUSH MOCKUP, DỪNG CHỜ CHỦ DỰ ÁN DUYỆT theo R-012; chưa sửa UI sản phẩm, không merge main. |
| Giới hạn | Chưa xác minh số liệu thật hoặc luồng backend; nút chỉ minh họa. Không chạy build/test vì chỉ thêm HTML/ảnh/tài liệu. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — OpenCode
| Tiêu chí | Bằng chứng |
|---|---|
| Đủ 4 thẻ Codex, OpenCode, Antigravity, Claude | `hub.tests/H813AccountQuotaTests.cs` 6 test pass; trang 200 có `card-codex`, `card-opencode`, `card-antigravity`, `card-claude`. |
| Hạn mức, giờ hồi, nguồn provider/ước tính/không rõ, cảnh báo < 20% | Test `Quota_source...`, `Resting...`; HTML có "Nguồn: không rõ", "Nguồn: ước tính", "dưới 20%". |
| Token hôm nay/tuần, thiếu dữ liệu là null (không thay 0) | Test `Tokens_are_null...`: 1200 hôm nay, 2000 tuần; OpenCode null. |
| Link https từ `team.json` → `accounts[].links`, không lộ bí mật | Test `Links_skip...` loại `javascript:`/`http:`; test trang không có chuỗi `sk-`. |
| OpenCode + HUB_FAKE không ghi file thật | `OpenCodeService`, `FakeAiAccountsService`; `HubV3Registration` chọn fake theo `HUB_FAKE=1`. |
| Build | `dotnet build AITeam.sln -warnaserror -nodeReuse:false`: 0 warning, 0 error. |
| Test | `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m`: Hub 828 + Core 907 pass, 0 fail (lần đầu 1 test Core flake do PowerShell quá giờ; chạy lại đơn và full đều pass). |
- File đổi: `hub/Services/AiAccountsService.cs`, `hub/Services/HubV3Registration.cs`, `hub/Pages/AiAccounts.cshtml(.cs)`, `hub/wwwroot/css/ai-accounts.css`, `hub.tests/H813AccountQuotaTests.cs`.
- Chưa xác minh: chưa chụp ảnh Chromium sản phẩm ở 1000 px (môi trường không có trình duyệt); đã kiểm HTML/CSS grid co giãn, không đặt chiều rộng cố định.
