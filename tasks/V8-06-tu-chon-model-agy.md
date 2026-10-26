# V8-06 · Tự chọn model agy có thật trên tài khoản đang đăng nhập

- **Agent:** A
- **Nhánh:** `agent-a/V8-06-tu-chon-model-agy`
- **Ưu tiên:** cao
- **Ngân sách:** ~80 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 20 phút
- **Review AI:** có
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới cho người dùng.
- Mỗi tài khoản Antigravity có bộ model khác nhau (25–26/10: `claude-opus-4-6-thinking` ↔ `claude-opus-5-5-medium` ↔ `gemini-4-argon-high`). Đổi tài khoản là task ghi model cũ chạy lỗi "invalid model selection".
- Sửa: trước khi chạy agy, lấy danh sách `agy models` (cache 1 giờ trong `.state/agy-models.json`); model của task không có ⇒ chọn model thay thế theo thứ tự ưu tiên, ghi 1 dòng "Model X không có trên tài khoản ⇒ dùng Y".

## Đọc (CHỈ những file này)
- `team.ps1` (Invoke-Assign: đọc dòng Model của task, gọi agy; lệnh models)
- `core.tests/Support/TestShell.cs`

## Mục tiêu
- Hàm thuần `Resolve-AgyModel($wanted, [string[]]$available)`: có trong danh sách ⇒ giữ; rỗng ⇒ rỗng (mặc định); không có ⇒ chọn phần tử đầu tiên có mặt theo thứ tự `team.json` → `agy.modelFallback` (mảng), mặc định: claude-opus-4-6-thinking, claude-opus-5-5-medium, gemini-4-argon-high, claude-sonnet-5-5-high, claude-sonnet-4-6, gemini-3.8-flash-high; không có cái nào ⇒ rỗng (mặc định của agy).
- `agy models` lỗi / quá 60 giây ⇒ không đổi model (giữ hành vi cũ).

## Phạm vi (được sửa những gì)
- `team.ps1`
- `team.json`
- `core.tests/AcceptanceV806AgyModelTests.cs`

## Không làm
- Không đổi file task; không gọi agy thật trong test.

## Test nghiệm thu
- `core.tests/AcceptanceV806AgyModelTests.cs` (Codex viết cùng lượt, dùng TestShell): có/không có/rỗng/danh sách rỗng/fallback từ team.json/parse output thật 2 tài khoản (danh sách tab-separated như `agy models`).

## Tiêu chí hoàn thành
- [ ] Build 0 warning
- [ ] `dotnet test AITeam.sln` pass + `powershell -File tools\test-verify.ps1` ĐẠT

## Báo cáo của agent

| Tiêu chí | Bằng chứng thật |
|---|---|
| Build 0 warning: chưa đạt | `dotnet build AITeam.sln -m:1`: thành công, 0 error, 8 warning NU1900 (không truy cập được NuGet). |
| Chọn/parse/fallback/cache/lỗi/quá hạn: đạt | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceV806AgyModelTests --blame-hang-timeout 2m -m:1`: 10 pass, 0 fail, 0 skip. |
| Test solution: chưa đạt | `dotnet test AITeam.sln --no-restore --blame-hang-timeout 2m -m:1`: Hub 644 pass/9 fail; core gặp lỗi Git Win32 error 5; đã ngắt lệnh khi chạm giới hạn 3 phút, chưa có tổng kết. |
| Cổng kiểm tra: chưa đạt | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: 2 kiểm tra tĩnh ĐẠT; dừng kịch bản 1 vì Git không tạo được signal pipe (Win32 error 5). |
| BOM: đạt | Kiểm byte đầu bằng PowerShell: `team.ps1` và test mới đều EF BB BF. |
- File đổi: `team.ps1` (~60 dòng logic chọn model/cache/tích hợp assign); `core.tests/AcceptanceV806AgyModelTests.cs` (103 dòng, 10 ca); phiếu này chỉ điền báo cáo theo chỉ dẫn trực tiếp.
- Quyết định: thêm tham số fallback tùy chọn để hàm chọn thuần, nhận `agy.modelFallback` từ cấu hình; không cần đổi `team.json`; cache lỗi/hết hạn và truy vấn lỗi giữ model cũ.
- Chưa xác minh: agy thật và output thật hai tài khoản không được cung cấp; test dùng danh sách tab-separated theo định dạng phiếu; không chạy app/chụp ảnh. Tổng dòng gồm test vượt 120, logic sản phẩm trong ngân sách.
- Không chạy lệnh git trực tiếp; hệ thống tự commit/push theo chỉ dẫn Codex dự phòng, chưa có commit được kiểm.

## Review (Claude)

## Kết quả