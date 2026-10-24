# V4-10 · `aiteam providers` hiện trạng thái nghỉ + cờ `--no-check`

- **Agent:** C
- **Nhánh:** `agent-c/V4-10-providers-rest-status`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~40 dòng
- **Yêu cầu:** R-004
- **Độ khó:** S
- **Thời gian tối đa:** 15 phút
- **Chạy bằng:** Codex (`assign -Dev codex`) — thử thật luồng Codex viết code

## Tóm tắt
- **Chức năng nhỏ:** lệnh `aiteam providers` cho biết AI nào đang "nghỉ" (hết hạn mức / chưa đăng nhập) tới giờ nào và vì sao.
- Cờ `--no-check` liệt kê nhanh mà không chạy thử từng AI (không tốn hạn mức, dùng trong script).

## Đọc (CHỈ những file này)
- `core/AITeam.Cli/Program.cs` (hàm `Providers()`)
- `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API `ProviderStateStore.RestOf`)
- `core.tests/AcceptanceV410ProvidersCliTests.cs` (test nghiệm thu — đặc tả chính xác)

## Mục tiêu
Sửa `Providers()` trong `Program.cs`:
1. `var state = new ProviderStateStore(root); var now = DateTime.Now; bool noCheck = args.Contains("--no-check");`
2. Provider `Enabled = false` ⇒ giữ nguyên dòng `  [TẮT] {id}`.
3. `--no-check`: KHÔNG gọi `CheckAsync`. Tag = `NGHỈ tới {Until:HH:mm dd/MM}` (InvariantCulture) nếu `state.RestOf(id, now)` có, ngược lại `SẴN SÀNG`. Không có `--no-check`: tag như cũ (OK / CHƯA CÀI / …) và nếu đang nghỉ thì thêm dòng nghỉ bên dưới.
4. Dòng chính giữ định dạng cũ: `  [{tag}] {id} ({Kind}) · vai trò: … · tối đa … job · model: …`. Đang nghỉ ⇒ thêm 1 dòng `         {Reason}: {Detail dòng đầu}`.
5. Mã thoát: `--no-check` luôn 0; không có cờ thì như cũ (có provider không OK ⇒ 1).
6. `Help()`: thêm `[--no-check]` vào dòng `aiteam providers`.

## Phạm vi (được sửa)
- `core/AITeam.Cli/Program.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `core/AITeam.Core/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV410ProvidersCliTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 3 ca nghiệm thu V4-10).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 3 ca nghiệm thu V4-10) | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)`. `dotnet test AITeam.sln --blame-hang-timeout 3m` → `AITeam.Core.Tests.dll`: Passed 127/127 (3/3 `AcceptanceV410ProvidersCliTests`), `AITeam.Hub.Tests.dll`: Passed 276/276. |

- **Đã làm:** Cập nhật `Providers()` trong `core/AITeam.Cli/Program.cs` đọc `ProviderStateStore.RestOf`, hỗ trợ `--no-check` (hiện `NGHỈ tới HH:mm dd/MM` hoặc `SẴN SÀNG`, không gọi `CheckAsync`, exit code 0), in dòng chi tiết nghỉ `{Reason}: {Detail dòng đầu}`, và thêm `[--no-check]` vào `Help()`.
- **File đã đổi:** `core/AITeam.Cli/Program.cs`, `tasks/V4-10-providers-rest-status.md`.
- **Điều chưa chắc:** Không có.

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 3/3 ca nghiệm thu; 127 + 276 test; build 0 warning; ~5 phút, 1 lượt; 20/40 dòng.
- **Lượt này thực tế do agy chạy, KHÔNG phải Codex** (log có conversation_id của agy, không có file `.codex.*`): trong `assign`, biến cục bộ `$dev` (thông tin developer) đè tham số `-Dev` vì PowerShell không phân biệt hoa thường ⇒ `-Dev codex` bị bỏ qua. Lần giao trước đó sập vì cùng biến này (null).
- [TK] (công cụ) Claude thử riêng `Invoke-CodexDevelop` nhưng chưa thử cả lệnh `assign -Dev codex` trước khi giao. Đã sửa (đổi tên `$devInfo`) + rà toàn bộ team.ps1: không còn biến cục bộ nào đè tham số mà còn được đọc.
- Code: đúng đặc tả, `--no-check` không gọi CLI ngoài, giờ in theo InvariantCulture.

Nhân viên: agy dưới mã C (Codex sẽ thử lại ở V4-11) — 1 lượt, ~5 phút, 20/40 dòng.

## Kết quả
- **Chức năng nhỏ:** `aiteam providers` hiện AI nào đang nghỉ tới giờ nào và vì sao; `--no-check` liệt kê nhanh không chạy thử từng AI.
- **Kiểm chứng:** 3 ca nghiệm thu chạy CLI thật.
- **Cần cải tiến:** thử lại luồng Codex viết code thật (V4-11).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-004) | — | — | — |
| 2. Ý tưởng | Claude | 5 | ~5' | Task nhỏ có ích để thử luồng Codex | — | — |
| 3. Plan / thiết kế | Claude | 5 | ~10' | Test CLI không gọi CLI ngoài, giờ bất biến văn hóa | — | — |
| 4. Giao việc | Claude | 2 | ~10' | `-Dev codex` không có tác dụng (biến đè tham số); lần đầu còn sập | TK | Thử cả lệnh `assign` với tham số mới (dry run) trước khi giao thật; rà biến đè tham số |
| 5. Thực hiện code | agy (mã C) | 5 | ~5' | Đúng đặc tả | — | — |
| 6. Review / QC | Cổng KT + Claude | 4 | ~10' | Claude phát hiện nhờ đọc log (không có file .codex) | — | Cổng KT ghi rõ provider thật đã chạy |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |