# V4-03 · Codex review chỉ đọc (ReviewService)

- **Agent:** A
- **Nhánh:** `agent-a/V4-03-codex-review`
- **Ưu tiên:** cao
- **Ngân sách:** ~250 dòng
- **Yêu cầu:** R-002
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không có chức năng mới cho người dùng** — phần lõi để AITeam tự nhờ **Codex review code** thay vì chỉ Claude.
- Dựng "gói review" (task, tiêu chí, diff, kết quả kiểm tra, báo cáo agent) → gọi Codex **chỉ đọc** → đọc kết luận PASS / CẦN SỬA / BLOCKED.
- Lỗi, quá giờ, hết hạn mức, kết quả hỏng, hay **tự duyệt chính code của mình** ⇒ luôn BLOCKED, không bao giờ coi là đạt.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Review/ReviewService.cs` (khung — điền các chỗ `TODO(agent)`)
- `core/AITeam.Core/Review/ReviewModels.cs`
- `core/AITeam.Core/Providers/ProviderModels.cs` (IAgentProvider, AgentRunRequest / Result)
- `core/AITeam.Core/Tasks/TaskFile.cs` (dùng `TaskFile.Parse` + `Section(...)` để lấy các mục task)
- `core.tests/AcceptanceP03ReviewTests.cs` (test nghiệm thu)

## Bối cảnh
Thiết kế: `docs/design/V4-MULTI-AI.md` mục B5. Codex đã chạy thật 23/10: `--output-schema` trả JSON đúng lược đồ.

## Mục tiêu
Điền thân 3 hàm `BuildPack`, `ParseOutput`, `ReviewAsync` đúng mô tả trong comment của khung. Giữ nguyên chữ ký public và hằng `OutputSchemaJson`.

## Phạm vi (được sửa)
- `core/AITeam.Core/Review/`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không gọi Codex thật trong test (dùng provider giả như test nghiệm thu). Không sửa `Providers/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceP03ReviewTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 2m` pass hết (gồm 20 ca nghiệm thu P0-3).

## Báo cáo của agent
<!-- Agent điền, tối đa 15 dòng -->

## Review (Claude)
<!-- Claude điền -->

## Kết quả
<!-- Claude điền khi review ĐẠT -->
