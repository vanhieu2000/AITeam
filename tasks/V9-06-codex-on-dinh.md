# V9-06 — Codex chạy ổn định trên các máy
- Nhánh: `agent-c/V9-06-codex-on-dinh`; R-012 C2.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: CodexProvider, ProviderConfig, Invoke-Assign/Invoke-CodexRetry, test mới.
- Không làm: sửa config.toml người dùng, sửa assert cũ, merge main.
- Ngân sách phút/token: chưa đo.
## Tiêu chí hoàn thành
- [x] Sandbox unelevated; baseUrl theo lần gọi; không ghi config toàn máy.
- [x] Fetch/merge worktree có kiểm mã lỗi; cleanup build server trong finally mỗi lần retry.
- [x] Giữ commit/push ở bộ điều phối; 5 test tham số/cấu hình mới.
- [ ] Task thật trên máy công ty không treo, build tiếp không Access denied.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Tham số | V906CodexPortabilityTests: sandbox vẫn giữ read-only, URL được quote TOML, URL đăng nhập/file bị chặn, local ghi đè chỉ Codex. |
| Điều phối | team.ps1 kiểm lỗi fetch/merge trước giao; finally shutdown cả lỗi/NoVerify, giữ mã thoát; commit/push hiện có được giữ. |
| Cú pháp | PowerShell AST 7.6: 0 lỗi; chưa có Windows PowerShell 5.1 để chạy. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Toàn bộ test | Core 402/695 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền Windows. 5 test V906 mới đạt; log /tmp/r012-c2-tests. |
| Giới hạn | Không chạy AI thật lại vì proxy 403 đã xác nhận; chưa kiểm máy công ty. Không sửa test nghiệm thu cũ. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chờ xác minh Windows trước nghiệm thu. |
