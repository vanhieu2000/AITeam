# V5-01 · MCP Gateway: `aiteam mcp` — agent đọc task, báo tiến độ, xem kết quả kiểm tra

- **Agent:** A
- **Nhánh:** `agent-a/V5-01-mcp-gateway`
- **Ưu tiên:** cao
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Review AI:** có
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — nền cho "Văn phòng mini": agent (agy / Codex) gọi công cụ của AITeam qua MCP trong lúc làm.
- Agent báo tiến độ / hỏi / báo xong ⇒ tin nhắn hiện trên Hub (V5-03, V5-04); đọc đúng task của mình và kết quả cổng kiểm tra.
- An toàn: mỗi máy chủ MCP gắn đúng 1 task, không chạy lệnh, không đọc file ngoài task, không mạng.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Mcp/McpTaskTools.cs` (khung — điền `TODO(agent)`)
- `core/AITeam.Cli/McpServerCommand.cs` (khung — điền `TODO(agent)`; chú thích có cách dùng SDK đã PoC)
- `core/AITeam.Core/Messages/MessageStore.cs` (chỉ đọc API)
- `core.tests/AcceptanceV501McpTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C1, E (PoC 25/10: agy không tương tác gọi được công cụ MCP C# stdio). SDK: gói NuGet `ModelContextProtocol` 2.3.0 (Claude đã thêm vào `AITeam.Cli.csproj`, `AITeam.Core.Tests.csproj`). `Program.cs` đã nối lệnh `mcp`.

## Ràng buộc
- stdout CHỈ dành cho JSON-RPC: mọi log ra stderr.
- Công cụ không ném lỗi ra client: lỗi ⇒ chuỗi "LỖI: …".
- Không đọc / liệt kê file ngoài `--task-file` và các file `{TaskId}-…`, `{TaskId}.…` trong `--logs`.

## Mục tiêu
Điền `McpTaskTools` và `McpServerCommand.RunAsync` theo chú thích khung.

## Phạm vi (được sửa)
- `core/AITeam.Core/Mcp/McpTaskTools.cs`
- `core/AITeam.Cli/McpServerCommand.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `MessageStore.cs`, `Program.cs`, `*.csproj`, `Providers/`, `Review/`, `Routing/`, `hub/`, `team.ps1`. Không thêm công cụ ngoài 4 công cụ đã định.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV501McpTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 15 ca nghiệm thu V5-01, có 1 ca chạy thật `aiteam mcp` qua client MCP).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 15 ca nghiệm thu V5-01, có 1 ca chạy thật `aiteam mcp` qua client MCP). | `dotnet build AITeam.sln` → `0 Warning(s), 0 Error(s)`. `dotnet test AITeam.sln --blame-hang-timeout 3m` → `Passed! 154/154 (AITeam.Core.Tests, gồm 15/15 AcceptanceV501McpTests + 1 ca đồng thời) + 289/289 (AITeam.Hub.Tests)`. |

- **Đã làm:** Hiện thực `McpTaskTools` (`ToolsFor`, `GetTask`, `PostMessage`, `GetTestResults`, `GetArtifacts`) và `McpServerCommand.RunAsync` (MCP stdio qua `ModelContextProtocol` 2.3.0, log stderr, kiểm tra tham số trước khi chạy).
- **File đã đổi:** `core/AITeam.Core/Mcp/McpTaskTools.cs` (logic 4 công cụ MCP theo task/vai trò, khóa đồng bộ bộ đếm tin nhắn phiên), `core/AITeam.Cli/McpServerCommand.cs` (khởi chạy MCP stdio server & kiểm tra tham số), `core.tests/McpTaskToolsConcurrencyTests.cs` (test gọi đồng thời ngưỡng 59 tin), `tasks/V5-01-mcp-gateway.md` (báo cáo).
- **Điều chưa chắc:** Không có.

## Review (Claude)
### Vòng 1 — ĐẠT (25/10/2026, Claude)
- Cổng kiểm tra ĐẠT; 15/15 ca nghiệm thu (có ca chạy thật `aiteam mcp` qua client MCP chính thức); 155 + 289 test; 183/220 dòng.
- **Vòng review AI tự động đầy đủ lần đầu:** Codex review 1 (52s) ⇒ CẦN SỬA: giới hạn 60 tin bị vượt khi gọi đồng thời ⇒ `assign` tự giao lại Minh ⇒ sửa trong 2 phút + test đồng thời. Codex review 2 (44s) ⇒ CẦN SỬA: đọc file qua symlink trong thư mục log.
- Claude sửa (~10 dòng + 1 test): công cụ từ chối đọc symlink / reparse point. Ghi chú: trên máy này user thường KHÔNG tạo được symlink (cần admin) và agent tự nó đã đọc được mọi file ⇒ rủi ro thấp; đây là lớp phòng thủ thêm.
- [TK] (quy trình) Claude commit bản sửa cùng lệnh chạy test ⇒ 1 lần test core báo hỏng lúc máy đang build song song nhánh khác; chạy lại 5 lần riêng: 155/155. Theo dõi test chập chờn.

Nhân viên: Minh (A) — 2 lượt (1 do review AI), ~10 phút, 183/220 dòng.

## Kết quả
- **Không có chức năng mới nhìn thấy ngay** — `aiteam mcp`: máy chủ MCP gắn với 1 task, 4 công cụ (đọc task, báo tiến độ / hỏi / báo xong, xem kết quả cổng KT, xem danh sách file của task), giới hạn 60 tin / lượt, không đọc ngoài task.
- **Kiểm chứng:** 15 ca nghiệm thu + chạy thật qua client MCP; Codex review 2 vòng.
- **Cần cải tiến:** nối vào `assign` để agent dùng thật (V5-02).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-005) | — | — | — |
| 2. Ý tưởng | Claude | 5 | ~20' | PoC thật trước khi thiết kế | — | Giữ |
| 3. Plan / thiết kế | Claude | 4 | ~25' | Đặc tả thiếu đồng thời + symlink (Codex bắt) | TK | Mục kiểm tra mẫu task: "đọc file theo đường dẫn ⇒ symlink? gọi đồng thời ⇒ khóa?" |
| 4. Giao việc | Claude | 5 | ~10' | — | — | — |
| 5. Thực hiện code | Minh (A) | 5 | ~10' (2 lượt) | Đúng đặc tả, sửa nhanh theo reviewer | — | — |
| 6. Review / QC | Cổng KT + Codex + Claude | 5 | ~15' | Vòng tự sửa theo review AI chạy trọn | — | Giữ |
| 7. Đánh giá & bàn giao | Claude | 4 | ~10' | Commit trước khi xem kết quả test | TK | Tách lệnh test và commit |