# H-004 · Hub: Cài đặt, Tài khoản, Bí mật + lệnh `team.ps1 hub`

- **Agent:** A
- **Nhánh:** `agent-a/H-004-hub-settings`
- **Ưu tiên:** trung bình. Giao **sau** H-003 vì cả hai cùng sửa `_Layout` / CSS.

## Bối cảnh
- Thiết kế: `docs/HUB-DESIGN.md` mục 4.5, 4.6, 4.7 và mục 5.
- Dịch vụ đã có (H-001): `ITeamConfigService.SaveSettings / SaveProject`, `IHealthService` (`CheckAgyLoginAsync`, `OpenSwitchAccountWindow`, `InstallClaudeIntegration`), `IVaultService`.

## Mục tiêu
1. **`/settings`**: form sửa
   - `DefaultModel`, `MaxReviewRounds`, chuyên môn của từng developer;
   - `ProjectsRoot`, `WorkRoot`, `AgyExe` (ghi vào `team.local.json`);
   - mỗi dự án: `build`, `test`, `stack`, `areas`.
   - Kiểm tra đầu vào: `MaxReviewRounds` 1–10; `AgyExe` phải là đường dẫn đầy đủ tới `agy.exe` có thật (dùng `HealthService.IsValidAgyExe`); chuỗi không chứa ký tự xuống dòng. Lưu xong báo "Đã lưu (có bản .bak)".
2. **`/accounts`**:
   - GitHub: kết quả từ `IHealthService.Check()`;
   - Antigravity: nút "Kiểm tra đăng nhập" (HTMX, có trạng thái chờ ≤ 2 phút) và nút "Đổi tài khoản", kèm hướng dẫn `/logout` → đăng nhập → `/quit`; danh sách nhãn tài khoản;
   - Claude: trạng thái skill / CLAUDE.md + nút "Cài / sửa".
3. **`/secrets`**:
   - danh sách TÊN; form thêm (tên + `type=password autocomplete=off`); xóa có xác nhận;
   - form "Xuất .env.local": chọn dự án + tick tên;
   - lỗi hiện bằng tiếng Việt (kho hỏng, chưa gitignore, tên sai).
   - **Không có đường nào trả giá trị bí mật về trình duyệt; không ghi log giá trị.**
4. **`team.ps1 hub`**: build Hub nếu chưa có bản build hoặc mã nguồn mới hơn, chạy ở nền (cửa sổ ẩn), đợi cổng 5057 mở, rồi mở trình duyệt. Chạy lại khi Hub đang chạy thì chỉ mở trình duyệt.

## Phạm vi (được sửa)
- `hub/Pages/Settings.*`, `hub/Pages/Accounts.*`, `hub/Pages/Vault.*` (route `/secrets`), `hub/wwwroot/css/hub.css` (chỉ thêm), `hub.tests/`, `team.ps1` (chỉ thêm lệnh `hub`, lưu UTF-8 có BOM), `README.md` (mục lệnh).

## Không làm
- Không sửa `hub/Services/` trừ khi thiếu thật sự (ghi lý do), không sửa `Contracts.cs` / `HubModels.cs`.

## Tiêu chí hoàn thành
- [ ] Build 0 warning; test pass. Test PageModel:
  - Settings: dữ liệu sai bị từ chối, không ghi file;
  - Secrets: thêm / xóa / xuất gọi đúng dịch vụ; response và HTML không chứa giá trị bí mật (test bằng chuỗi giá trị đặc biệt);
  - mọi POST thiếu antiforgery bị từ chối.
- [ ] Chạy `HUB_FAKE=1`: 3 trang hiển thị đúng. KHÔNG bấm lưu / bí mật trên dữ liệu thật khi test.
- [ ] `team.ps1 hub` chạy được; ghi cách đã thử vào báo cáo.

## Báo cáo của agent
<!-- Agent điền -->

## Review (Claude)
<!-- Claude điền -->
