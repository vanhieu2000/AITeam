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
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 15 ca nghiệm thu V5-01, có 1 ca chạy thật `aiteam mcp` qua client MCP).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| | |

- **Đã làm:**
- **File đã đổi:**
- **Điều chưa chắc:**

## Review (Claude)

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Minh (A) | | | | | |
| 6. Review / QC | Cổng KT + Codex + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
