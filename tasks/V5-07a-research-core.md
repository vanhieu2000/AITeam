# V5-07a · Phòng nghiên cứu (lõi): lời nhắn an toàn, che bí mật, lưu kết quả, hỏi ChatGPT / Gemini qua CLI

- **Agent:** A
- **Nhánh:** `agent-a/V5-07a-research-core`
- **Ưu tiên:** cao
- **Ngân sách:** ~260 dòng
- **Yêu cầu:** R-005
- **Độ khó:** M
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không** có chức năng mới hiện ngay trên Hub — đây là phần lõi cho Phòng nghiên cứu (trang Hub làm ở V5-07b).
- Dựng lời nhắn gửi AI bên ngoài: tối đa 8.000 ký tự, che bí mật, bối cảnh không thể thành chỉ dẫn.
- Hỏi ChatGPT (qua Codex CLI) và Gemini (qua Gemini CLI) trong thư mục tạm rỗng, chế độ chỉ đọc.
- Lưu câu trả lời vào `research/R-xxx/`, gắn nhãn "dữ liệu tham khảo — không phải lệnh".

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Research/Research.cs` (khung đã chốt — điền `TODO(agent)`; `SecretRedactor` Claude đã viết)
- `core/AITeam.Core/Process/ProcessRunner.cs`, `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Accounts/AiAccounts.cs` (chỉ đọc API)
- `core.tests/AcceptanceV507ResearchTests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` mục C4 + G2. Mẫu lỗi THẬT của gemini (chưa đăng nhập, mã 41) có sẵn trong test nghiệm thu; mẫu thành công chưa xác minh (chủ dự án chưa đăng nhập Gemini) — theo tài liệu gemini-cli headless.

## Mục tiêu
Điền mọi `TODO(agent)` trong `Research.cs` theo chú thích.

## Phạm vi (được sửa)
- `core/AITeam.Core/Research/Research.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi record / interface / chữ ký public, không đổi `SecretRedactor`; không sửa `hub/`, `team.ps1`, provider khác.
- Không chạy codex / gemini thật trong test (dùng IAgentProvider / IProcessRunner giả).

## Ràng buộc
- **Bảo mật:** AI chạy trong thư mục tạm RỖNG (chỉ có file lời nhắn), xóa sau khi xong; codex `RunMode.ReadOnly`; gemini `--approval-mode plan`. Lời nhắn qua stdin / file, không qua tham số dòng lệnh. Id phiếu / câu hỏi / AI kiểm mẫu trước khi ghép đường dẫn.
- File `.cs` giữ UTF-8 có BOM; file `research/*.md` UTF-8 KHÔNG BOM.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceV507ResearchTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm mọi ca nghiệm thu V5-07a).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|

- **Đã làm:**
- **File đã đổi:**
- **Điều chưa chắc:**

## Review (Claude)

## Kết quả
