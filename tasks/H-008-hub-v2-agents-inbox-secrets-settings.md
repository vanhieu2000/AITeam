# H-008 · Hub v2: trang Agent, Hộp thư, Tài khoản, Bí mật, Cài đặt

- **Agent:** A
- **Nhánh:** `agent-a/H-008-hub-v2-other-pages`
- **Ưu tiên:** trung bình
- **Làm sau:** H-006 đã merge.

## Bối cảnh
- Làm theo `docs/design/HUB-V2.md` **mục 1, 2, 3** và mockup (màn "Agent", "Hộp thư", "Tài khoản", "Bí mật", "Cài đặt").
- Hiện tại: Cài đặt 2,6 màn hình (3 form dự án xếp chồng, mỗi ô kèm 1 dòng mô tả); Agent 2,0 màn hình (20 dòng, mỗi dòng thêm "Xem phản hồi / lỗi").
- **Trang Tài khoản KHÔNG có phần hạn mức / đổi tài khoản** (thiết kế mục 3): giữ GitHub, Antigravity (kiểm tra đăng nhập, nút mở hướng dẫn đổi tài khoản như hiện nay, dòng "Lần cuối hết credits: <giờ> · <task>" từ `AntigravityStatuses`), Claude.

## Mục tiêu
1. **Agent:** bảng Đang chạy (Dừng). Bảng Lịch sử: lọc `Tất cả / Lỗi / Thành công` + chọn dự án, **8 dòng / trang** (`RunHistory.Page`), pill theo `RunVerdicts`; bấm dòng ⇒ ngăn kéo (dùng lại partial ngăn kéo của H-007 nếu đã có, chưa có thì làm và H-007 dùng lại).
2. **Hộp thư:** trái form (dự án + nội dung + Gửi); phải tab Chưa xử lý / Đã nhận.
3. **Tài khoản:** 3 khối gọn (mục Bối cảnh); mô tả dài chuyển vào ⓘ.
4. **Bí mật:** trái Thêm bí mật + Xuất .env.local; phải danh sách tên. Không bao giờ hiện giá trị.
5. **Cài đặt:** menu con trái (Team · từng dự án), form 2 cột, ⓘ thay mô tả, Hoàn tác / Lưu. Mỗi lần chỉ hiện 1 form.
6. **Lớp thuần mới** trong `hub/Services/HubViews.cs` (chỉ THÊM):
   ```csharp
   public enum RunFilter { All, Errors, Success }
   public sealed record RunPage(IReadOnlyList<AgentRun> Items, int Total, int Page, int PageCount);
   public static class RunHistory
   {
       // Lọc: Errors = Error + CreditsExhausted; Success = Success; All = mọi lần đã kết thúc (bỏ Running).
       // project null/rỗng ⇒ mọi dự án. Sắp Time giảm dần. page bắt đầu từ 1; page ngoài khoảng ⇒ kẹp về [1, PageCount];
       // không có dòng ⇒ PageCount = 1, Items rỗng.
       public static RunPage Page(IEnumerable<AgentRun> runs, RunFilter filter, string? project, int page, int pageSize = 8);
   }
   ```

## Phạm vi (được sửa)
- `hub/Pages/Agents.cshtml(.cs)`, `Inbox.cshtml(.cs)`, `Accounts.cshtml(.cs)`, `Vault.cshtml(.cs)` (trang Bí mật), `Settings.cshtml(.cs)`, partial liên quan trong `hub/Pages/Shared/`, `hub/Services/HubViews.cs` (chỉ THÊM), `hub/wwwroot/css/hub.css` (chỉ THÊM), `hub.tests/`, `docs/screenshots/H-008/`.

## Không làm
- KHÔNG đọc / ghi phiên đăng nhập Antigravity, Credential Manager, token; KHÔNG gọi API hạn mức của Google.
- Không đổi dịch vụ dữ liệu, định dạng `team.json` / `projects.json` / `inbox.md` / `vault.json`.

## Ràng buộc
- Giữ nguyên chống CSRF (antiforgery) và kiểm tra hợp lệ hiện có của các form (có test sẵn: `AntiforgerySecurityTests`, `SettingsPageTests`, `SecretsPageTests` phải vẫn pass).
- Dừng agent: vẫn chỉ cho PID là `agy.exe` (hành vi `IAgentRunService.Stop` giữ nguyên).

## Giao diện
- 1280×800: 5 trang không cuộn trang (Agent với ≥ 27 lần chạy ⇒ phân trang). Ảnh nộp `docs/screenshots/H-008/`: `agents.png`, `agents-errors.png` (lọc Lỗi), `inbox.png`, `accounts.png`, `secrets.png`, `settings-team.png`, `settings-project.png`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH008Tests.cs` — có sẵn trên nhánh (commit đầu tiên).

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln` pass hết, gồm `AcceptanceH008Tests` (không sửa) + test sẵn có + test của agent
- [ ] 5 trang đúng mockup, không cuộn ở 1280×800; 7 ảnh đã nộp
- [ ] Giữ BOM UTF-8

## Báo cáo của agent
(Agent dừng 2 lần vì hết credits Antigravity sau khi xong code 5/6 bước; theo yêu cầu chủ dự án, Claude hoàn tất phần ảnh + review.)
- Đã làm: `RunFilter`, `RunPage`, `RunHistory` (HubViews.cs); trang Agent (bảng đang chạy, lịch sử lọc Tất cả / Lỗi / Thành công + theo dự án, phân trang 8 dòng, ngăn kéo chi tiết qua HTMX); Hộp thư + Bí mật 2 cột phẳng; Tài khoản 3 khối (GitHub, Antigravity kèm trạng thái hết credits từ log, Claude); Cài đặt với menu con Team / từng dự án; `AgentsV2PageTests.cs`.
- Ảnh: `docs/screenshots/H-008/` (Claude chụp bằng Edge headless 1280×800; ảnh trang Bí mật đặt tên `bi-mat.png` vì bộ quét bí mật chặn tên `secrets.png`).

## Review (Claude)
### Vòng 1 — ĐẠT
Đã kiểm tra: gộp `main` (đã có H-007) vào nhánh — xung đột `HubViews.cs` do hai bên cùng thêm lớp, Claude giữ cả hai; build 0 warning, 159/159 test, test nghiệm thu nguyên vẹn; đọc diff: trang Tài khoản không đọc / ghi phiên đăng nhập Antigravity, ngăn kéo lấy dự án qua cấu hình (không ghép đường dẫn từ query), giá trị hiển thị qua Razor encode; chạy Hub từ nhánh ở 1280×800 với dữ liệu thật: 5 trang không cuộn trang, lịch sử 32 lần chạy chia 4 trang, lọc Lỗi ra 17, bấm dòng mở ngăn kéo đúng lần chạy.

**Required** (Claude sửa):
1. `_AgentsRunningTablePartial.cshtml` — cột Agent ghi cứng "A" (C-012 của Agent B hiện "A"). Sửa: lấy A/B từ thư mục worktree trong dòng lệnh agy, không rõ thì "—".
2. `hub.css` — nút lọc là thẻ `a` nhưng CSS `.seg` chỉ có cho `span` ⇒ ba nút dính liền, không thấy nút đang chọn. Thêm quy tắc `.seg a`.

**Optional** (hàng chờ): `PaginationSummary` ghi cứng cỡ trang 8; trang Agent đọc toàn bộ log mỗi lần mở (ổn với vài trăm lần chạy).

Claude sửa: 2 lỗi giao diện trên + gộp xung đột.
