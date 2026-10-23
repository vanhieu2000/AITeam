# V4-06 · Trạng thái job + phục hồi sau sự cố (JobStore)

- **Agent:** B
- **Nhánh:** `agent-b/V4-06-job-state`
- **Ưu tiên:** cao
- **Ngân sách:** ~250 dòng
- **Yêu cầu:** R-002
- **Thời gian tối đa:** 25 phút

## Tóm tắt
- **Không có chức năng mới cho người dùng** — phần lõi để AITeam biết chính xác mỗi task đang ở bước nào (chạy, kiểm tra, review, chờ duyệt, đã merge…).
- Chống chạy trùng một task; máy / tiến trình sập thì task được đánh dấu "bị gián đoạn" để chạy tiếp, không treo mãi.
- Chặn nhảy cóc (ví dụ merge khi chưa kiểm tra / review); có lịch sử từng lần chuyển trạng thái.

## Đọc (CHỈ những file này)
- `core/AITeam.Core/Jobs/JobStore.cs` (khung — điền các chỗ `TODO(agent)`)
- `core/AITeam.Core/Jobs/JobModels.cs` (bảng chuyển trạng thái đã chốt — KHÔNG đổi)
- `core.tests/AcceptanceP06JobStoreTests.cs` (test nghiệm thu)

## Bối cảnh
Thiết kế: `docs/design/V4-MULTI-AI.md` mục B4. Lưu cục bộ từng máy; nguồn sự thật giữa các máy vẫn là file task + bảng việc trên git.

## Mục tiêu
Điền thân các hàm của `JobStore` đúng mô tả trong comment của khung (bố cục file trên đĩa, ghi file tạm rồi đổi tên, khóa tạo nguyên tử, chặn tên có `..` / `/` / `\` / `:`). Dùng System.Text.Json; không thêm thư viện.

## Phạm vi (được sửa)
- `core/AITeam.Core/Jobs/JobStore.cs`
- `core.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `JobModels.cs` (bảng chuyển trạng thái là quyết định thiết kế). Không sửa `Review/`, `Providers/`, `hub/`, `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceP06JobStoreTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 2m` pass hết (gồm 17 ca nghiệm thu P0-6).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s), 0 Error(s)` |
| Test pass hết (gồm 17 ca P0-6) | `dotnet test AITeam.sln --blame-hang-timeout 2m` → `AITeam.Core.Tests: Passed 36/36 (17 ca P0-6 pass)`, `AITeam.Hub.Tests: Passed 159/159`, Failed 0 |

- **Đã làm:** Hoàn thiện `Create`, `Get`, `Transition`, `TryAcquireRun`, `ReleaseRun`, `RecoverInterrupted`, `History` trong `JobStore` (ghi atomic qua `.tmp` + `File.Move`, tạo `.lock` bằng `FileMode.CreateNew`, kiểm tra tên chỉ nhận `[A-Za-z0-9._-]`, bỏ qua dòng hỏng trong `.events.jsonl`).
- **File đã đổi:** `core/AITeam.Core/Jobs/JobStore.cs`, `tasks/V4-06-job-state.md`.
- **Quyết định tự đưa ra / Điều chưa chắc:** Lấy `tasks/V4-06-job-state.md` từ commit `c1b0401` trên `main` để điền báo cáo (vì nhánh được tách ở `245242a` ngay trước commit file task). Không còn điều chưa chắc.

## Review (Claude)
<!-- Claude điền -->

## Kết quả
<!-- Claude điền khi review ĐẠT -->
