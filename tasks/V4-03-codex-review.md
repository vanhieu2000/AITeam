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
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 2m` pass hết (gồm 20 ca nghiệm thu P0-3).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh & kết quả thật) |
|---|---|
| Build 0 warning & Test pass hết (gồm 20 ca P0-3) | `dotnet build AITeam.sln`: 0 Warning(s), 0 Error(s). `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m`: Passed 40/40 (`AITeam.Core.Tests`, gồm 20/20 `AcceptanceP03ReviewTests`) + Passed 159/159 (`AITeam.Hub.Tests`). |

- **File đã đổi:**
  - `core/AITeam.Core/Review/ReviewService.cs`: Điền thân `BuildPack`, `ParseOutput`, `ReviewAsync` đúng đặc tả fail-safe.
  - `core.tests/ReviewServiceEdgeTests.cs`: Thêm test kiểm tra finding `optional` và verdict `BLOCKED`.
  - `tasks/V4-03-codex-review.md`: Checkout từ commit `c1b04013` trên `main` và cập nhật báo cáo.
- **Quyết định tự đưa ra:** Đặt tên file tạm trong `logDir` là `prompt-review.md` và `schema-review.json` để mẫu `review-*.json` chỉ khớp đúng file kết quả review.
- **Chưa chắc / chưa xác minh:** Không có.

## Review (Claude)
### Vòng 1 — ĐẠT (23/10/2026, Claude)
- Cổng kiểm tra ĐẠT ngay lượt đầu: **9 phút**, 40/40 test core + 159/159 Hub, build 0 warning, 321/250 dòng (dưới x1.5).
- Đọc code: đủ luật fail-safe (tự duyệt, không sandbox, provider lỗi, JSON hỏng ⇒ Blocked; PASS có blocker / required ⇒ ChangesRequested); không đưa review cũ vào gói; hash tính trên diff đầy đủ.
- **Chạy thật với Codex (dogfood)** ⇒ lộ 1 lỗi tích hợp mà test giả không bắt được:
  - [TK] (test) file lược đồ / lời nhắn ghi bằng `Encoding.UTF8` (có BOM) ⇒ Codex: "not valid JSON: expected value at line 1 column 1". Khung + test nghiệm thu của Claude không yêu cầu "không BOM". Claude sửa (2 dòng, `UTF8Encoding(false)`) + thêm test `Schema_and_prompt_files_are_written_without_bom`.
- Lần chạy thật thứ 2: Codex **hết hạn mức** ("usage limit… try again at Oct 24 4:19 AM") ⇒ hệ thống phân loại `QuotaExhausted` ⇒ **BLOCKED**, không coi là đạt (đúng thiết kế).
- Optional (hàng chờ): tên file tạm `prompt-review.md` cố định ⇒ 2 review song song cùng thư mục log sẽ ghi đè; tên file kết quả theo giây.

Nhân viên: Minh (A) — 1 lượt, 9 phút.

## Kết quả
- **Có gì mới:** AITeam tự dựng gói review và gọi Codex **chỉ đọc** để review code của agent; kết luận PASS / CẦN SỬA / BLOCKED có cấu trúc, lưu kèm hash của đúng bản diff được review.
- **An toàn:** tự duyệt, provider lỗi, quá giờ, hết hạn mức, kết quả hỏng ⇒ luôn BLOCKED (đã kiểm chứng cả bằng Codex thật hết hạn mức).
- **Kiểm chứng:** 41/41 test core, chạy thật với Codex.
- **Cần cải tiến:**
  - Codex có hạn mức nhỏ (hết sau vài lần chạy thử) ⇒ P0-4/5 phải chọn task nào đáng review bằng Codex + có reviewer dự phòng;
  - chưa nối vào `team.ps1` (P0-7).
