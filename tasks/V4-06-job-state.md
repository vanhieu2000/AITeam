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
- [ ] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 2m` pass hết (gồm 17 ca nghiệm thu P0-6).

## Báo cáo của agent
<!-- Agent điền, tối đa 15 dòng -->

## Review (Claude)
<!-- Claude điền -->

## Kết quả
<!-- Claude điền khi review ĐẠT -->
